using System;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMenu.Cheats;
using LethalMenu.Menu;
using LethalMenu.Util;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace LethalMenu
{
    /// <summary>
    /// Main mod MonoBehaviour - manages cheats, patches, and game state.
    /// </summary>
    public class LethalMenuMod : MonoBehaviour
    {
        private const string HarmonyId = "com.lethalmenu.mod";

        // Singleton instance
        public static LethalMenuMod? Instance { get; private set; }

        // Harmony instance for patching
        private Harmony? _harmony;

        // Active cheats
        private readonly List<CheatBase> _cheats = new();

        // Menu system
        private HackMenu? _menu;

        // Fog tracking
        private LocalVolumetricFog[]? _fogObjects;
        private bool _fogWasDisabled = false;

        // Game state references
        public static PlayerControllerB? LocalPlayer { get; set; }
        public static StartOfRound? GameInstance { get; set; }
        public static QuickMenuManager? QuickMenu { get; set; }
        public static Terminal? GameTerminal { get; set; }
        public static HUDManager? HUD { get; set; }

        // Game object caches (populated by ObjectManager)
        public static List<PlayerControllerB> Players { get; } = new();
        public static List<EnemyAI> Enemies { get; } = new();
        public static List<GrabbableObject> Items { get; } = new();
        public static List<DoorLock> DoorLocks { get; } = new();
        public static List<Landmine> Landmines { get; } = new();
        public static List<Turret> Turrets { get; } = new();
        public static List<EntranceTeleport> Entrances { get; } = new();
        public static List<ShipTeleporter> Teleporters { get; } = new();

        private void Awake()
        {
            Instance = this;
            Loader.Log("[LethalMenu] LethalMenuMod.Awake() called");

            try
            {
                // Initialize menu FIRST (OnGUI can be called during Awake)
                InitializeMenu();
                InitializeHarmony();
                InitializeCheats();
                Loader.Log("[LethalMenu] Mod initialized successfully.");
            }
            catch (Exception ex)
            {
                Loader.LogError($"[LethalMenu] Initialization failed: {ex}");
            }
        }

        private void InitializeHarmony()
        {
            Loader.Log("[LethalMenu] Initializing Harmony...");
            _harmony = new Harmony(HarmonyId);

            // Patch all classes with [HarmonyPatch] attributes
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                if (!type.IsDefined(typeof(HarmonyPatch), false)) continue;

                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                    Loader.Log($"[LethalMenu] Patched: {type.Name}");
                }
                catch (Exception ex)
                {
                    Loader.LogError($"[LethalMenu] Failed to patch {type.Name}: {ex.Message}");
                }
            }
            Loader.Log("[LethalMenu] Harmony patching complete.");
        }

        private void InitializeCheats()
        {
            Loader.Log("[LethalMenu] Registering cheats...");
            // Register all cheats
            _cheats.Add(new GodModeCheat());
            _cheats.Add(new InfiniteStaminaCheat());
            _cheats.Add(new SpeedHackCheat());
            _cheats.Add(new JumpHackCheat());
            _cheats.Add(new NoClipCheat());
            _cheats.Add(new NightVisionCheat());
            _cheats.Add(new NoFallDamageCheat());
            _cheats.Add(new InfiniteBatteryCheat());
            _cheats.Add(new NoWeightCheat());
            _cheats.Add(new ESPCheat());
            _cheats.Add(new EnemyControlCheat());
            _cheats.Add(new FreeCamCheat());
            _cheats.Add(new SpectatePlayerCheat());
            Loader.Log($"[LethalMenu] Registered {_cheats.Count} cheats.");

            // Load config on startup
            Settings.LoadConfig();
        }

        private void InitializeMenu()
        {
            Loader.Log("[LethalMenu] Initializing menu...");
            _menu = new HackMenu();
            Loader.Log("[LethalMenu] Menu initialized.");
        }

        private void Update()
        {
            // Update game state references
            UpdateGameState();

            // Check INSERT using New Input System (what Lethal Company uses)
            bool insertPressed = false;
            try
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    insertPressed = keyboard.insertKey.wasPressedThisFrame;
                }
            }
            catch { }

            if (insertPressed)
            {
                Settings.ShowMenu = !Settings.ShowMenu;
                ToggleCursor(Settings.ShowMenu);
            }

            // Update all cheats (they internally check if enabled)
            foreach (var cheat in _cheats)
            {
                try
                {
                    cheat.OnUpdate();
                }
                catch (Exception ex)
                {
                    Loader.LogError($"[LethalMenu] Cheat {cheat.Name} update error: {ex.Message}");
                }
            }

            // KillClick - kill enemy on left click
            UpdateKillClick();

            // NoFog control
            UpdateFogState();

            // Update new runtime features
            UpdateRuntimeFeatures();

            // Process continuous spam toggles
            Cheats.NetworkCheats.ProcessSpamToggles();

            // Collect game objects periodically
            ObjectManager.CollectObjects();
        }

        private void UpdateRuntimeFeatures()
        {
            // AlwaysShowClock - Find and enable the clock UI
            if (Settings.AlwaysShowClock)
            {
                // The clock is typically hidden in certain states - we need to find it in the HUD hierarchy
                var clockObj = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/Clock");
                if (clockObj != null) clockObj.SetActive(true);
            }

            // FOV
            if (LocalPlayer != null && LocalPlayer.gameplayCamera != null)
            {
                if (LocalPlayer.inTerminalMenu)
                {
                    LocalPlayer.gameplayCamera.fieldOfView = 66f;
                }
                else if (Settings.FOV)
                {
                    LocalPlayer.gameplayCamera.fieldOfView = Settings.FOVValue;
                }
                else if (LocalPlayer.gameplayCamera.fieldOfView != 66f)
                {
                    LocalPlayer.gameplayCamera.fieldOfView = 66f;
                }
            }

            // NoVisor
            var visor = GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel/");
            if (visor != null)
            {
                visor.SetActive(!Settings.NoVisor);
            }

            // Unlimited TZP
            if (Settings.UnlimitedTZP && LocalPlayer != null)
            {
                var heldItem = LocalPlayer.currentlyHeldObjectServer;
                if (heldItem is TetraChemicalItem tzp && tzp != null)
                {
                    // Use reflection to set fuel
                    var fuelField = tzp.GetType().GetField("fuel", 
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    fuelField?.SetValue(tzp, 1f);
                }
            }

            // No TZP Effects
            if (Settings.NoTZPEffects && LocalPlayer != null && HUD != null)
            {
                var heldItem = LocalPlayer.currentlyHeldObjectServer;
                if (heldItem is TetraChemicalItem tzp && tzp != null)
                {
                    LocalPlayer.drunknessInertia = 0f;
                    LocalPlayer.increasingDrunknessThisFrame = false;
                    HUD.gasHelmetAnimator.SetBool("gasEmitting", false);
                    
                    // Set emittingGas to false via reflection
                    var emitField = tzp.GetType().GetField("emittingGas",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    emitField?.SetValue(tzp, false);
                }
            }

            // Eggs always explode
            if (Settings.EggsAlwaysExplode && !Settings.EggsNeverExplode && LocalPlayer != null)
            {
                var heldItem = LocalPlayer.currentlyHeldObjectServer;
                if (heldItem is StunGrenadeItem egg && egg != null && egg.explodeSFX?.name == "EasterEggPop")
                {
                    egg.SetExplodeOnThrowClientRpc(true);
                }
            }

            // Minigun shotgun
            UpdateMinigunShotgun();

            // Breadcrumbs
            UpdateBreadcrumbs();
        }

        private void UpdateMinigunShotgun()
        {
            if (!Settings.MinigunShotgun || LocalPlayer == null) return;

            var shotgun = LocalPlayer.currentlyHeldObjectServer as ShotgunItem;
            if (shotgun == null) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed) return;

            var pos = LocalPlayer.transform.position - LocalPlayer.gameplayCamera.transform.up * 0.45f;
            var dir = LocalPlayer.gameplayCamera.transform.forward;
            shotgun.ShootGunServerRpc(pos, dir);
        }

        // Breadcrumbs tracking
        private readonly List<Vector3> _breadcrumbs = new List<Vector3>();
        private float _lastBreadcrumbTime = 0f;

        private void UpdateBreadcrumbs()
        {
            // Clear breadcrumbs when not in game or player is dead
            if (LocalPlayer == null || LocalPlayer.isPlayerDead)
            {
                if (_breadcrumbs.Count > 0) _breadcrumbs.Clear();
                return;
            }

            // Check if breadcrumbs are enabled
            if (!Settings.Breadcrumbs) return;

            if (Time.time - _lastBreadcrumbTime >= Settings.BreadcrumbInterval)
            {
                _lastBreadcrumbTime = Time.time;
                var pos = LocalPlayer.transform.position;
                pos.y -= 0.5f;
                _breadcrumbs.Add(pos);
                
                // Limit max breadcrumbs to prevent memory issues
                if (_breadcrumbs.Count > 1000)
                {
                    _breadcrumbs.RemoveAt(0);
                }
            }
        }

        private void UpdateKillClick()
        {
            if (!Settings.KillClick) return;
            if (Settings.ShowMenu) return; // Don't kill while menu open
            if (LocalPlayer == null) return;

            // Check for left mouse click
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            // Raycast from camera
            var camera = LocalPlayer.gameplayCamera;
            if (camera == null) return;

            var ray = new Ray(camera.transform.position, camera.transform.forward);
            var hits = Physics.RaycastAll(ray, 100f);

            foreach (var hit in hits)
            {
                // Check for enemy collision detect component
                var enemyCollider = hit.collider.GetComponent<EnemyAICollisionDetect>();
                if (enemyCollider != null && enemyCollider.mainScript != null)
                {
                    var enemy = enemyCollider.mainScript;
                    
                    // Take ownership and kill
                    enemy.ChangeEnemyOwnerServerRpc(LocalPlayer.actualClientId);
                    
                    if (enemy is NutcrackerEnemyAI nutcracker)
                    {
                        nutcracker.KillEnemy();
                    }
                    else
                    {
                        enemy.KillEnemyServerRpc(true);
                    }
                    
                    Loader.Log($"Killed {enemy.enemyType?.enemyName ?? "enemy"}");
                    break;
                }
            }
        }

        private void UpdateFogState()
        {
            if (Settings.NoFog && !_fogWasDisabled)
            {
                // Find and disable all fog
                _fogObjects = UnityEngine.Object.FindObjectsOfType<LocalVolumetricFog>();
                foreach (var fog in _fogObjects)
                {
                    if (fog != null)
                    {
                        fog.enabled = false;
                    }
                }
                _fogWasDisabled = true;
            }
            else if (!Settings.NoFog && _fogWasDisabled)
            {
                // Re-enable fog
                if (_fogObjects != null)
                {
                    foreach (var fog in _fogObjects)
                    {
                        if (fog != null)
                        {
                            fog.enabled = true;
                        }
                    }
                }
                _fogWasDisabled = false;
            }
        }

        private void FixedUpdate()
        {
            foreach (var cheat in _cheats)
            {
                if (cheat.IsEnabled)
                {
                    try
                    {
                        cheat.OnFixedUpdate();
                    }
                    catch
                    {
                        // Silently ignore fixed update errors
                    }
                }
            }
        }

        private void OnGUI()
        {
            // Draw menu
            if (Settings.ShowMenu)
            {
                if (_menu != null)
                {
                    try
                    {
                        _menu.Draw();
                    }
                    catch (Exception ex)
                    {
                        Loader.LogError($"[LethalMenu] Menu draw error: {ex}");
                    }
                }
            }

            // Draw ESP overlays
            foreach (var cheat in _cheats)
            {
                if (cheat.IsEnabled)
                {
                    try
                    {
                        cheat.OnGUI();
                    }
                    catch
                    {
                        // Silently ignore GUI errors
                    }
                }
            }

            // Draw crosshair
            if (Settings.Crosshair)
            {
                DrawCrosshair();
            }

            // Draw breadcrumbs
            if (Settings.Breadcrumbs)
            {
                DrawBreadcrumbs();
            }

            // Draw HP display
            if (Settings.HPDisplay && LocalPlayer != null)
            {
                DrawHPDisplay();
            }
        }

        private static Texture2D? _crosshairTexture;
        
        private void DrawCrosshair()
        {
            if (_crosshairTexture == null)
            {
                _crosshairTexture = new Texture2D(1, 1);
                _crosshairTexture.SetPixel(0, 0, Color.white);
                _crosshairTexture.Apply();
            }

            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            float scale = Settings.CrosshairScale;
            float thickness = Settings.CrosshairThickness;

            GUI.color = Settings.CrosshairColor;

            // Horizontal line
            GUI.DrawTexture(new Rect(centerX - scale, centerY - thickness / 2, scale * 2, thickness), _crosshairTexture);
            // Vertical line
            GUI.DrawTexture(new Rect(centerX - thickness / 2, centerY - scale, thickness, scale * 2), _crosshairTexture);

            GUI.color = Color.white;
        }

        private void UpdateGameState()
        {
            GameInstance = StartOfRound.Instance;
            if (GameInstance == null) return;

            LocalPlayer = GameInstance.localPlayerController;
            QuickMenu = LocalPlayer?.quickMenuManager;
            GameTerminal = UnityEngine.Object.FindObjectOfType<Terminal>();
        }

        private void ToggleCursor(bool show)
        {
            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;

            // Disable player input when menu is open
            if (LocalPlayer != null)
            {
                if (show)
                    LocalPlayer.playerActions.Disable();
                else
                    LocalPlayer.playerActions.Enable();
            }
        }

        /// <summary>
        /// Get all registered cheats.
        /// </summary>
        public IReadOnlyList<CheatBase> GetCheats() => _cheats;

        /// <summary>
        /// Cleanup when unloading.
        /// </summary>
        public void Cleanup()
        {
            _harmony?.UnpatchAll(HarmonyId);

            foreach (var cheat in _cheats)
            {
                cheat.IsEnabled = false;
                cheat.OnDisable();
            }

            _cheats.Clear();
            Instance = null;
        }

        private static GUIStyle? _breadcrumbStyle;

        private void DrawBreadcrumbs()
        {
            if (_breadcrumbStyle == null)
            {
                _breadcrumbStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _breadcrumbStyle.normal.textColor = Color.yellow;
            }

            var camera = LocalPlayer?.gameplayCamera;
            if (camera == null) return;

            for (int i = 0; i < _breadcrumbs.Count; i++)
            {
                var worldPos = _breadcrumbs[i];
                
                // Use WorldToViewportPoint for correct conversion (like reference cheats)
                var viewport = camera.WorldToViewportPoint(worldPos);
                
                // Check if behind camera
                if (viewport.z <= 0) continue;
                
                // Check distance (z is distance in viewport space)
                if (viewport.z > 100f) continue;
                
                // Convert viewport (0-1) to screen coordinates
                float screenX = viewport.x * Screen.width;
                float screenY = (1f - viewport.y) * Screen.height; // Flip Y for GUI coordinates
                
                // Skip if off screen
                if (screenX < 0 || screenX > Screen.width || screenY < 0 || screenY > Screen.height) continue;

                // Draw a circle/dot marker
                var rect = new Rect(screenX - 8, screenY - 8, 16, 16);
                GUI.color = new Color(1f, 0.9f, 0f, 0.8f); // Yellow with some transparency
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                
                // Draw the number
                GUI.color = Color.black;
                GUI.Label(new Rect(screenX - 15, screenY - 10, 30, 20), i.ToString(), _breadcrumbStyle);
            }
            GUI.color = Color.white;
        }

        private static GUIStyle? _hpStyle;

        private void DrawHPDisplay()
        {
            if (_hpStyle == null)
            {
                _hpStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            GUI.color = Color.green;
            if (LocalPlayer.health < 50) GUI.color = Color.yellow;
            if (LocalPlayer.health < 25) GUI.color = Color.red;

            GUI.Label(new Rect(20, 80, 100, 40), $"HP: {LocalPlayer.health}", _hpStyle);
            GUI.color = Color.white;
        }
    }
}

