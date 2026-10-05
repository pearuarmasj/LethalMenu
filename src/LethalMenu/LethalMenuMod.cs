using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMenu.Cheats;
using LethalMenu.Menu;
using LethalMenu.Util;
using UnityEngine;
using UnityEngine.InputSystem;

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

        // Active cheats and their enabled state as of the last dispatched transition
        private readonly List<CheatBase> _cheats = new();
        private readonly List<bool> _cheatWasEnabled = new();

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
                    {
                        _cheats.Add(cheat);
                        _cheatWasEnabled.Add(false);
                    }
                }
                catch (Exception ex)
                {
                    Loader.LogError($"[LethalMenu] Failed to create cheat {type.FullName}: {ex.Message}");
                }
            }

            Loader.Log($"[LethalMenu] Registered {_cheats.Count} cheats.");

            // Load config on startup
            Settings.LoadConfig();

            // NOTE: ChamHandler.Setup() is deferred to first ChamCheat.OnUpdate tick.
            // Creating a Material from a built-in shader during Awake (before Unity's rendering subsystem
            // is fully ready) caused injection crashes. Lazy init from Update is safe.
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
            Hack.TeleportAllEnemiesAway.RegisterExecutor(Cheats.NetworkCheats.TeleportAllEnemiesAway);
            Hack.TPAllItemsToShip.RegisterExecutor(Cheats.NetworkCheats.TeleportAllItemsToShip);
            Hack.TPNearbyItems.RegisterExecutor(() => Cheats.NetworkCheats.TeleportNearbyItemsToPlayer(15f));
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

            DispatchTransitions();

            // Update all cheats (they internally check if enabled)
            foreach (var cheat in _cheats)
            {
                try
                {
                    cheat.OnUpdate();
                }
                catch (Exception ex)
                {
                    LogCheatError(cheat, "update", ex);
                }
            }

            Cheats.NetworkCheats.ProcessSpamToggles();
            Cheats.NetworkCheats.ProcessDemiGod();

            // Collect game objects periodically
            ObjectManager.CollectObjects();
        }

        private readonly HashSet<string> _loggedCheatErrors = new();

        /// Logs a cheat exception once per (cheat, phase, exception) so a persistent per-frame fault
        /// doesn't flood the log file.
        private void LogCheatError(CheatBase cheat, string phase, Exception ex)
        {
            if (_loggedCheatErrors.Add($"{cheat.Name}|{phase}|{ex.GetType().Name}|{ex.Message}"))
                Loader.LogError($"[LethalMenu] Cheat {cheat.Name} {phase} error: {ex}");
        }

        /// Fires OnEnable/OnDisable for every cheat whose flag changed since the last frame.
        private void DispatchTransitions()
        {
            for (int i = 0; i < _cheats.Count; i++)
            {
                var cheat = _cheats[i];
                bool now = cheat.IsEnabled;
                if (now == _cheatWasEnabled[i]) continue;
                _cheatWasEnabled[i] = now;

                try
                {
                    if (now) cheat.OnEnable();
                    else cheat.OnDisable();
                }
                catch (Exception ex)
                {
                    LogCheatError(cheat, now ? "enable" : "disable", ex);
                }
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
                    catch (Exception ex)
                    {
                        LogCheatError(cheat, "fixed-update", ex);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            foreach (var cheat in _cheats)
            {
                try
                {
                    cheat.OnLateUpdate();
                }
                catch (Exception ex)
                {
                    LogCheatError(cheat, "late-update", ex);
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
                    catch (Exception ex)
                    {
                        LogCheatError(cheat, "GUI", ex);
                    }
                }
            }
        }

        private static void ApplyHudSkin()
        {
            if (Theme.ThemeLoader.Skin == null)
                Theme.ThemeLoader.SetTheme(Settings.ThemeName);

            if (Theme.ThemeLoader.Skin != null)
                GUI.skin = Theme.ThemeLoader.Skin;
        }

        private void UpdateGameState()
        {
            GameInstance = StartOfRound.Instance;
            if (GameInstance == null) return;

            LocalPlayer = GameInstance.localPlayerController;
            QuickMenu = LocalPlayer?.quickMenuManager;
            if (GameTerminal == null)
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

            for (int i = 0; i < _cheats.Count; i++)
            {
                if (!_cheatWasEnabled[i]) continue;
                try
                {
                    _cheats[i].OnDisable();
                }
                catch (Exception ex)
                {
                    LogCheatError(_cheats[i], "disable", ex);
                }
            }

            _cheats.Clear();
            _cheatWasEnabled.Clear();
            HackExtensions.InitializeDefaults();
            Instance = null;
        }
    }
}

