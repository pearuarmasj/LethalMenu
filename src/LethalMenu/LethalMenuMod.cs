using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMenu.Cheats;
using LethalMenu.Menu;
using LethalMenu.Mixins;
using LethalMenu.Util;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu
{
    /// <summary>
    /// Main mod MonoBehaviour - manages cheats, patches, and game state.
    /// </summary>
    public class LethalMenuMod : MonoBehaviour, IItemManipulator, IEnemyPrompter
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
        public static List<BreakerBox> BreakerBoxes { get; } = new();
        public static List<SteamValveHazard> SteamValves { get; } = new();
        public static List<TerminalAccessibleObject> BigDoors { get; } = new();
        public static List<HangarShipDoor> HangarShipDoors { get; } = new();
        public static List<EnemyVent> EnemyVents { get; } = new();
        public static List<ItemDropship> ItemDropships { get; } = new();
        public static List<VehicleController> Vehicles { get; } = new();
        public static List<GameObject> MoldSpores { get; } = new();
        public static List<MineshaftElevatorController> MineshaftElevators { get; } = new();
        public static List<GameObject> SpikeRoofTraps { get; } = new();

        private bool _minesEnabled = true;
        private bool _turretsEnabled = true;

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
            HackExtensions.InitializeDefaults();
            RegisterActionExecutors();

            var cheatTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(CheatBase).IsAssignableFrom(t) && !t.IsAbstract)
                .OrderBy(t => t.FullName);

            foreach (var type in cheatTypes)
            {
                try
                {
                    if (Activator.CreateInstance(type) is CheatBase cheat)
                        _cheats.Add(cheat);
                }
                catch (Exception ex)
                {
                    Loader.LogError($"[LethalMenu] Failed to create cheat {type.FullName}: {ex.Message}");
                }
            }

            Loader.Log($"[LethalMenu] Registered {_cheats.Count} cheats.");

            // Load config on startup
            Settings.LoadConfig();
        }

        private void RegisterActionExecutors()
        {
            Hack.DisconnectMod.RegisterExecutor(Cheats.NetworkCheats.DisconnectFromLobby);
            Hack.ReconnectFromClipboard.RegisterExecutor(Cheats.NetworkCheats.ReconnectFromClipboard);
            Hack.SelfRevive.RegisterExecutor(Cheats.NetworkCheats.SelfRevive);
            Hack.FakeDeath.RegisterExecutor(Cheats.NetworkCheats.FakeDeath);
            Hack.CancelFakeDeath.RegisterExecutor(Cheats.NetworkCheats.CancelFakeDeath);
            Hack.TeleportToShip.RegisterExecutor(() => Cheats.NetworkCheats.TeleportToShip());
            Hack.TeleportToEntrance.RegisterExecutor(() =>
            {
                if (LocalPlayer != null) Cheats.NetworkCheats.TeleportPlayerToEntrance(LocalPlayer, true);
            });
            Hack.TeleportToFireExit.RegisterExecutor(() =>
            {
                if (LocalPlayer != null) Cheats.NetworkCheats.TeleportPlayerToEntrance(LocalPlayer, false);
            });
            Hack.KillAllEnemies.RegisterExecutor(Cheats.NetworkCheats.KillAllEnemies);
            Hack.StunAllEnemies.RegisterExecutor(Cheats.NetworkCheats.StunAllEnemies);
            Hack.TeleportAllEnemiesAway.RegisterExecutor(() => this.TeleportAllEnemiesAway());
            Hack.TPAllItemsToShip.RegisterExecutor(() => this.TeleportAllItemsToShip());
            Hack.TPNearbyItems.RegisterExecutor(() => this.TeleportNearbyItemsToPlayer(15f));
            Hack.UnlockAllDoors.RegisterExecutor(Cheats.NetworkCheats.UnlockAllDoors);
            Hack.BlowUpAllMines.RegisterExecutor(Cheats.NetworkCheats.BlowUpAllLandmines);
            Hack.ToggleMines.RegisterExecutor(() =>
            {
                _minesEnabled = !_minesEnabled;
                Cheats.NetworkCheats.ToggleAllLandmines(_minesEnabled);
            });
            Hack.ToggleTurrets.RegisterExecutor(() =>
            {
                _turretsEnabled = !_turretsEnabled;
                Cheats.NetworkCheats.ToggleAllTurrets(_turretsEnabled);
            });
            Hack.BerserkTurrets.RegisterExecutor(Cheats.NetworkCheats.BerserkAllTurrets);
            Hack.FlickerLights.RegisterExecutor(Cheats.NetworkCheats.FlickerShipLights);
            Hack.MaxChaos.RegisterExecutor(Cheats.NetworkCheats.MaxChaos);
            Hack.ForceShipLeave.RegisterExecutor(Cheats.NetworkCheats.ForceShipLeave);
            Hack.EjectAllPlayers.RegisterExecutor(Cheats.NetworkCheats.EjectAllPlayers);
            Hack.ForceStart.RegisterExecutor(Cheats.NetworkCheats.ForceStartGame);
            Hack.ForceEnd.RegisterExecutor(Cheats.NetworkCheats.ForceEndGame);
            Hack.ReviveAllPlayers.RegisterExecutor(Cheats.NetworkCheats.ReviveAllPlayers);
            Hack.TeleportAllToMe.RegisterExecutor(Cheats.NetworkCheats.TeleportAllToMe);
            Hack.SetCredits.RegisterExecutor(() => Cheats.NetworkCheats.SetCredits(999999));
            Hack.SellQuota.RegisterExecutor(Cheats.NetworkCheats.SellQuota);
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

            if (!Settings.ShowMenu)
                HackExtensions.CheckKeyBinds();

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

            // Update new runtime features
            UpdateRuntimeFeatures();

            // Process continuous spam toggles
            Cheats.NetworkCheats.ProcessSpamToggles();

            // Collect game objects periodically
            ObjectManager.CollectObjects();
        }

        private void UpdateRuntimeFeatures()
        {
            // NoVisor
            var visor = GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel/");
            if (visor != null)
            {
                visor.SetActive(!Hack.NoVisor.IsEnabled());
            }

            // Unlimited TZP
            if (Hack.UnlimitedTZP.IsEnabled() && LocalPlayer != null)
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
            if (Hack.NoTZPEffects.IsEnabled() && LocalPlayer != null && HUD != null)
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
            if (Hack.EggsAlwaysExplode.IsEnabled() && !Hack.EggsNeverExplode.IsEnabled() && LocalPlayer != null)
            {
                var heldItem = LocalPlayer.currentlyHeldObjectServer;
                if (heldItem is StunGrenadeItem egg && egg != null && egg.explodeSFX?.name == "EasterEggPop")
                {
                    egg.SetExplodeOnThrowClientRpc(true);
                }
            }

            // Minigun shotgun
            UpdateMinigunShotgun();
        }

        private void UpdateMinigunShotgun()
        {
            if (!Hack.MinigunShotgun.IsEnabled() || LocalPlayer == null) return;

            var shotgun = LocalPlayer.currentlyHeldObjectServer as ShotgunItem;
            if (shotgun == null) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed) return;

            var pos = LocalPlayer.transform.position - LocalPlayer.gameplayCamera.transform.up * 0.45f;
            var dir = LocalPlayer.gameplayCamera.transform.forward;
            shotgun.ShootGunServerRpc(pos, dir);
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

            ApplyHudSkin();

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
            if (Hack.Crosshair.IsEnabled())
            {
                DrawCrosshair();
            }

            if (Hack.HPDisplay.IsEnabled() && LocalPlayer != null)
            {
                DrawHPDisplay();
            }
        }

        private static void ApplyHudSkin()
        {
            if (Theme.ThemeLoader.Skin == null)
                Theme.ThemeLoader.SetTheme(Settings.ThemeName);

            if (Theme.ThemeLoader.Skin != null)
                GUI.skin = Theme.ThemeLoader.Skin;
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
            HackExtensions.InitializeDefaults();
            Instance = null;
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

            if (LocalPlayer == null) return;
            
            GUI.color = Color.green;
            if (LocalPlayer.health < 50) GUI.color = Color.yellow;
            if (LocalPlayer.health < 25) GUI.color = Color.red;

            GUI.Label(new Rect(20, 80, 100, 40), $"HP: {LocalPlayer.health}", _hpStyle);
            GUI.color = Color.white;
        }
    }
}

