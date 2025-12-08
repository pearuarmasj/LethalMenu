using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LethalMenu
{
    /// <summary>
    /// Global settings and state for the mod.
    /// </summary>
    public static class Settings
    {
        // Config file path
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LethalMenu", "config.json");

        // Menu state
        public static bool ShowMenu { get; set; } = false;

        // Self cheats
        public static bool GodMode { get; set; } = false;
        public static bool InfiniteStamina { get; set; } = false;
        public static bool InfiniteBattery { get; set; } = false;
        public static bool NoFallDamage { get; set; } = false;
        public static bool NoClip { get; set; } = false;
        public static bool NightVision { get; set; } = false;
        public static bool SpeedHack { get; set; } = false;
        public static float SpeedMultiplier { get; set; } = 2.0f;
        public static bool JumpHack { get; set; } = false;
        public static float JumpMultiplier { get; set; } = 2.0f;
        public static bool NoWeight { get; set; } = false;

        // Non-linear cheats (Harmony patches)
        public static bool Untargetable { get; set; } = false;
        public static bool AntiFlash { get; set; } = false;
        public static bool OneHanded { get; set; } = false;
        public static bool UnlimitedJump { get; set; } = false;
        public static bool NoQuicksand { get; set; } = false;
        public static bool SuperShovel { get; set; } = false;
        public static bool SuperSpeed { get; set; } = false;
        public static bool UnlimitedAmmo { get; set; } = false;
        public static bool FastClimb { get; set; } = false;
        public static bool UnlimitedOxygen { get; set; } = false;
        public static bool Shoplifter { get; set; } = false;
        public static bool GrabInLobby { get; set; } = false;
        public static bool JebAttackPrevention { get; set; } = false;
        public static bool TauntSlide { get; set; } = false;
        public static bool Reach { get; set; } = false;

        // Medium complexity cheats
        public static bool BuildAnywhere { get; set; } = false;
        public static bool HearEveryone { get; set; } = false;
        public static bool InstantInteract { get; set; } = false;
        public static bool KillClick { get; set; } = false;
        public static bool Invisibility { get; set; } = false;

        // New features batch 1 - Visual/HUD
        public static bool AlwaysShowClock { get; set; } = false;
        public static bool Crosshair { get; set; } = false;
        public static float CrosshairScale { get; set; } = 10f;
        public static float CrosshairThickness { get; set; } = 2f;
        public static UnityEngine.Color CrosshairColor { get; set; } = UnityEngine.Color.white;
        public static bool NoVisor { get; set; } = false;
        public static bool NoCameraShake { get; set; } = false;
        public static bool NoFieldOfDepth { get; set; } = false;
        public static bool FOV { get; set; } = false;
        public static float FOVValue { get; set; } = 90f;

        // New features batch 2 - Enemy related
        public static bool AntiGhostGirl { get; set; } = false;
        public static bool GhostMode { get; set; } = false;
        public static bool GrabNutcrackerShotgun { get; set; } = false;
        public static bool EnemyControl { get; set; } = false;

        // New features batch 3 - Environment
        public static bool BridgeNeverFalls { get; set; } = false;
        public static bool OpenDropShipLand { get; set; } = false;
        public static bool OpenShipDoorSpace { get; set; } = false;
        public static bool NoShipDoorClose { get; set; } = false;

        // New features batch 4 - Items/Equipment
        public static bool EggsAlwaysExplode { get; set; } = false;
        public static bool EggsNeverExplode { get; set; } = false;
        public static bool SuperKnife { get; set; } = false;
        public static bool SuperJump { get; set; } = false;
        public static float SuperJumpForce { get; set; } = 20f;
        public static bool StrongHands { get; set; } = false;
        public static bool TeleportWithItems { get; set; } = false;
        public static bool UnlimitedTZP { get; set; } = false;
        public static bool NoTZPEffects { get; set; } = false;
        public static bool UnlimitedZapGun { get; set; } = false;
        public static bool UnlimitedPresents { get; set; } = false;

        // New features batch 5 - Through walls
        public static bool LootThroughWalls { get; set; } = false;
        public static bool InteractThroughWalls { get; set; } = false;

        // New features batch 6 - Notifications
        public static bool DeathNotifications { get; set; } = false;

        // New features batch 7 - More cheats
        public static bool HearDeadPeople { get; set; } = false;
        public static bool MinigunShotgun { get; set; } = false;
        public static bool LootBeforeGameStarts { get; set; } = false;
        public static bool Breadcrumbs { get; set; } = false;
        public static float BreadcrumbInterval { get; set; } = 3f;
        public static bool FullRenderResolution { get; set; } = false;
        public static bool HPDisplay { get; set; } = false;

        // Camera features
        public static bool FreeCam { get; set; } = false;
        public static float FreeCamSpeed { get; set; } = 10f;
        public static bool SpectatePlayer { get; set; } = false;
        public static int SpectatePlayerIndex { get; set; } = -1;

        // Networking / Anti-kick
        public static bool AntiKick { get; set; } = false;
        public static System.Collections.Generic.HashSet<ulong> KickedFromLobbies { get; } = new System.Collections.Generic.HashSet<ulong>();
        internal static bool WasKicked { get; set; } = false;
        internal static bool HostQuit { get; set; } = false;
        public static ulong CurrentLobbyId { get; set; } = 0;

        // Spam/Troll toggles (continuous while enabled)
        public static bool HornSpam { get; set; } = false;
        public static bool DoorSpam { get; set; } = false;
        public static bool SignalSpam { get; set; } = false;
        public static bool RPCLagSpam { get; set; } = false;
        public static bool TerminalSoundSpam { get; set; } = false;
        public static bool TerminalEarrapeSpam { get; set; } = false;
        public static bool ChatSpamLoop { get; set; } = false;
        public static bool CarHornSpam { get; set; } = false;
        public static bool DeskDoorSpam { get; set; } = false;
        public static string SpamMessage { get; set; } = "SPAM";

        // Visual cheats
        public static bool ESP { get; set; } = false;
        public static bool PlayerESP { get; set; } = true;
        public static bool EnemyESP { get; set; } = true;
        public static bool ItemESP { get; set; } = true;
        public static bool DoorESP { get; set; } = false;
        public static bool MineESP { get; set; } = true;
        public static bool TurretESP { get; set; } = true;
        public static bool FuseboxESP { get; set; } = true;
        public static bool NoFog { get; set; } = false;
        public static bool PlayerHealthBars { get; set; } = true;

        // Night vision settings
        public static float NightVisionIntensity { get; set; } = 10000f;
        public static float NightVisionRange { get; set; } = 10000f;

        // ESP colors
        public static UnityEngine.Color PlayerColor { get; set; } = UnityEngine.Color.green;
        public static UnityEngine.Color EnemyColor { get; set; } = UnityEngine.Color.red;
        public static UnityEngine.Color ItemColor { get; set; } = UnityEngine.Color.yellow;
        public static UnityEngine.Color DoorColor { get; set; } = UnityEngine.Color.cyan;
        public static UnityEngine.Color MineColor { get; set; } = new UnityEngine.Color(1f, 0.5f, 0f); // Orange
        public static UnityEngine.Color TurretColor { get; set; } = UnityEngine.Color.magenta;
        public static UnityEngine.Color FuseboxColor { get; set; } = new UnityEngine.Color(1f, 1f, 0.5f); // Light yellow

        // Debug
        public static string DebugMessage { get; set; } = "";
        
        // UI - Collapsed sections (persisted)
        public static System.Collections.Generic.HashSet<string> CollapsedSections { get; set; } = new System.Collections.Generic.HashSet<string>();

        #region Config Save/Load
        public static void SaveConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var config = new JObject
                {
                    // Player cheats
                    ["GodMode"] = GodMode,
                    ["InfiniteStamina"] = InfiniteStamina,
                    ["InfiniteBattery"] = InfiniteBattery,
                    ["NoFallDamage"] = NoFallDamage,
                    ["NoWeight"] = NoWeight,
                    ["SpeedHack"] = SpeedHack,
                    ["SpeedMultiplier"] = SpeedMultiplier,
                    ["JumpHack"] = JumpHack,
                    ["JumpMultiplier"] = JumpMultiplier,
                    
                    // Movement
                    ["SuperSpeed"] = SuperSpeed,
                    ["SuperJump"] = SuperJump,
                    ["SuperJumpForce"] = SuperJumpForce,
                    ["UnlimitedJump"] = UnlimitedJump,
                    ["FastClimb"] = FastClimb,
                    ["TauntSlide"] = TauntSlide,
                    ["FreeCamSpeed"] = FreeCamSpeed,
                    
                    // Patches
                    ["Untargetable"] = Untargetable,
                    ["AntiFlash"] = AntiFlash,
                    ["OneHanded"] = OneHanded,
                    ["NoQuicksand"] = NoQuicksand,
                    ["SuperShovel"] = SuperShovel,
                    ["UnlimitedAmmo"] = UnlimitedAmmo,
                    ["UnlimitedOxygen"] = UnlimitedOxygen,
                    ["Shoplifter"] = Shoplifter,
                    ["GrabInLobby"] = GrabInLobby,
                    ["JebAttackPrevention"] = JebAttackPrevention,
                    ["Reach"] = Reach,
                    ["BuildAnywhere"] = BuildAnywhere,
                    ["InstantInteract"] = InstantInteract,
                    
                    // Enemy
                    ["GhostMode"] = GhostMode,
                    ["AntiGhostGirl"] = AntiGhostGirl,
                    ["GrabNutcrackerShotgun"] = GrabNutcrackerShotgun,
                    
                    // Items
                    ["SuperKnife"] = SuperKnife,
                    ["StrongHands"] = StrongHands,
                    ["TeleportWithItems"] = TeleportWithItems,
                    ["UnlimitedTZP"] = UnlimitedTZP,
                    ["NoTZPEffects"] = NoTZPEffects,
                    ["UnlimitedZapGun"] = UnlimitedZapGun,
                    ["EggsAlwaysExplode"] = EggsAlwaysExplode,
                    ["EggsNeverExplode"] = EggsNeverExplode,
                    ["MinigunShotgun"] = MinigunShotgun,
                    ["LootThroughWalls"] = LootThroughWalls,
                    ["InteractThroughWalls"] = InteractThroughWalls,
                    ["LootBeforeGameStarts"] = LootBeforeGameStarts,
                    
                    // Environment
                    ["BridgeNeverFalls"] = BridgeNeverFalls,
                    ["OpenDropShipLand"] = OpenDropShipLand,
                    ["OpenShipDoorSpace"] = OpenShipDoorSpace,
                    
                    // Visual
                    ["AlwaysShowClock"] = AlwaysShowClock,
                    ["Crosshair"] = Crosshair,
                    ["CrosshairScale"] = CrosshairScale,
                    ["CrosshairThickness"] = CrosshairThickness,
                    ["NoVisor"] = NoVisor,
                    ["NoCameraShake"] = NoCameraShake,
                    ["NoFieldOfDepth"] = NoFieldOfDepth,
                    ["FullRenderResolution"] = FullRenderResolution,
                    ["FOV"] = FOV,
                    ["FOVValue"] = FOVValue,
                    ["Breadcrumbs"] = Breadcrumbs,
                    ["BreadcrumbInterval"] = BreadcrumbInterval,
                    ["HPDisplay"] = HPDisplay,
                    ["NoFog"] = NoFog,
                    
                    // ESP
                    ["ESP"] = ESP,
                    ["PlayerESP"] = PlayerESP,
                    ["EnemyESP"] = EnemyESP,
                    ["ItemESP"] = ItemESP,
                    ["DoorESP"] = DoorESP,
                    ["MineESP"] = MineESP,
                    ["TurretESP"] = TurretESP,
                    ["FuseboxESP"] = FuseboxESP,
                    ["PlayerHealthBars"] = PlayerHealthBars,
                    
                    // Night vision
                    ["NightVisionIntensity"] = NightVisionIntensity,
                    ["NightVisionRange"] = NightVisionRange,
                    
                    // Network
                    ["AntiKick"] = AntiKick,
                    ["HearEveryone"] = HearEveryone,
                    ["Invisibility"] = Invisibility,
                    ["DeathNotifications"] = DeathNotifications,
                    ["HearDeadPeople"] = HearDeadPeople,
                    
                    // UI
                    ["CollapsedSections"] = new JArray(CollapsedSections),
                };

                File.WriteAllText(ConfigPath, config.ToString(Formatting.Indented));
                Loader.Log($"Config saved to {ConfigPath}");
            }
            catch (Exception ex)
            {
                Loader.Log($"Failed to save config: {ex.Message}");
            }
        }
        public static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Loader.Log("No config file found, using defaults");
                    return;
                }

                var json = File.ReadAllText(ConfigPath);
                var config = JObject.Parse(json);

                // Player cheats
                GodMode = config["GodMode"]?.Value<bool>() ?? GodMode;
                InfiniteStamina = config["InfiniteStamina"]?.Value<bool>() ?? InfiniteStamina;
                InfiniteBattery = config["InfiniteBattery"]?.Value<bool>() ?? InfiniteBattery;
                NoFallDamage = config["NoFallDamage"]?.Value<bool>() ?? NoFallDamage;
                NoWeight = config["NoWeight"]?.Value<bool>() ?? NoWeight;
                SpeedHack = config["SpeedHack"]?.Value<bool>() ?? SpeedHack;
                SpeedMultiplier = config["SpeedMultiplier"]?.Value<float>() ?? SpeedMultiplier;
                JumpHack = config["JumpHack"]?.Value<bool>() ?? JumpHack;
                JumpMultiplier = config["JumpMultiplier"]?.Value<float>() ?? JumpMultiplier;
                
                // Movement
                SuperSpeed = config["SuperSpeed"]?.Value<bool>() ?? SuperSpeed;
                SuperJump = config["SuperJump"]?.Value<bool>() ?? SuperJump;
                SuperJumpForce = config["SuperJumpForce"]?.Value<float>() ?? SuperJumpForce;
                UnlimitedJump = config["UnlimitedJump"]?.Value<bool>() ?? UnlimitedJump;
                FastClimb = config["FastClimb"]?.Value<bool>() ?? FastClimb;
                TauntSlide = config["TauntSlide"]?.Value<bool>() ?? TauntSlide;
                FreeCamSpeed = config["FreeCamSpeed"]?.Value<float>() ?? FreeCamSpeed;
                
                // Patches
                Untargetable = config["Untargetable"]?.Value<bool>() ?? Untargetable;
                AntiFlash = config["AntiFlash"]?.Value<bool>() ?? AntiFlash;
                OneHanded = config["OneHanded"]?.Value<bool>() ?? OneHanded;
                NoQuicksand = config["NoQuicksand"]?.Value<bool>() ?? NoQuicksand;
                SuperShovel = config["SuperShovel"]?.Value<bool>() ?? SuperShovel;
                UnlimitedAmmo = config["UnlimitedAmmo"]?.Value<bool>() ?? UnlimitedAmmo;
                UnlimitedOxygen = config["UnlimitedOxygen"]?.Value<bool>() ?? UnlimitedOxygen;
                Shoplifter = config["Shoplifter"]?.Value<bool>() ?? Shoplifter;
                GrabInLobby = config["GrabInLobby"]?.Value<bool>() ?? GrabInLobby;
                JebAttackPrevention = config["JebAttackPrevention"]?.Value<bool>() ?? JebAttackPrevention;
                Reach = config["Reach"]?.Value<bool>() ?? Reach;
                BuildAnywhere = config["BuildAnywhere"]?.Value<bool>() ?? BuildAnywhere;
                InstantInteract = config["InstantInteract"]?.Value<bool>() ?? InstantInteract;
                
                // Enemy
                GhostMode = config["GhostMode"]?.Value<bool>() ?? GhostMode;
                AntiGhostGirl = config["AntiGhostGirl"]?.Value<bool>() ?? AntiGhostGirl;
                GrabNutcrackerShotgun = config["GrabNutcrackerShotgun"]?.Value<bool>() ?? GrabNutcrackerShotgun;
                
                // Items
                SuperKnife = config["SuperKnife"]?.Value<bool>() ?? SuperKnife;
                StrongHands = config["StrongHands"]?.Value<bool>() ?? StrongHands;
                TeleportWithItems = config["TeleportWithItems"]?.Value<bool>() ?? TeleportWithItems;
                UnlimitedTZP = config["UnlimitedTZP"]?.Value<bool>() ?? UnlimitedTZP;
                NoTZPEffects = config["NoTZPEffects"]?.Value<bool>() ?? NoTZPEffects;
                UnlimitedZapGun = config["UnlimitedZapGun"]?.Value<bool>() ?? UnlimitedZapGun;
                EggsAlwaysExplode = config["EggsAlwaysExplode"]?.Value<bool>() ?? EggsAlwaysExplode;
                EggsNeverExplode = config["EggsNeverExplode"]?.Value<bool>() ?? EggsNeverExplode;
                MinigunShotgun = config["MinigunShotgun"]?.Value<bool>() ?? MinigunShotgun;
                LootThroughWalls = config["LootThroughWalls"]?.Value<bool>() ?? LootThroughWalls;
                InteractThroughWalls = config["InteractThroughWalls"]?.Value<bool>() ?? InteractThroughWalls;
                LootBeforeGameStarts = config["LootBeforeGameStarts"]?.Value<bool>() ?? LootBeforeGameStarts;
                
                // Environment
                BridgeNeverFalls = config["BridgeNeverFalls"]?.Value<bool>() ?? BridgeNeverFalls;
                OpenDropShipLand = config["OpenDropShipLand"]?.Value<bool>() ?? OpenDropShipLand;
                OpenShipDoorSpace = config["OpenShipDoorSpace"]?.Value<bool>() ?? OpenShipDoorSpace;
                
                // Visual
                AlwaysShowClock = config["AlwaysShowClock"]?.Value<bool>() ?? AlwaysShowClock;
                Crosshair = config["Crosshair"]?.Value<bool>() ?? Crosshair;
                CrosshairScale = config["CrosshairScale"]?.Value<float>() ?? CrosshairScale;
                CrosshairThickness = config["CrosshairThickness"]?.Value<float>() ?? CrosshairThickness;
                NoVisor = config["NoVisor"]?.Value<bool>() ?? NoVisor;
                NoCameraShake = config["NoCameraShake"]?.Value<bool>() ?? NoCameraShake;
                NoFieldOfDepth = config["NoFieldOfDepth"]?.Value<bool>() ?? NoFieldOfDepth;
                FullRenderResolution = config["FullRenderResolution"]?.Value<bool>() ?? FullRenderResolution;
                FOV = config["FOV"]?.Value<bool>() ?? FOV;
                FOVValue = config["FOVValue"]?.Value<float>() ?? FOVValue;
                Breadcrumbs = config["Breadcrumbs"]?.Value<bool>() ?? Breadcrumbs;
                BreadcrumbInterval = config["BreadcrumbInterval"]?.Value<float>() ?? BreadcrumbInterval;
                HPDisplay = config["HPDisplay"]?.Value<bool>() ?? HPDisplay;
                NoFog = config["NoFog"]?.Value<bool>() ?? NoFog;
                
                // ESP
                ESP = config["ESP"]?.Value<bool>() ?? ESP;
                PlayerESP = config["PlayerESP"]?.Value<bool>() ?? PlayerESP;
                EnemyESP = config["EnemyESP"]?.Value<bool>() ?? EnemyESP;
                ItemESP = config["ItemESP"]?.Value<bool>() ?? ItemESP;
                DoorESP = config["DoorESP"]?.Value<bool>() ?? DoorESP;
                MineESP = config["MineESP"]?.Value<bool>() ?? MineESP;
                TurretESP = config["TurretESP"]?.Value<bool>() ?? TurretESP;
                FuseboxESP = config["FuseboxESP"]?.Value<bool>() ?? FuseboxESP;
                PlayerHealthBars = config["PlayerHealthBars"]?.Value<bool>() ?? PlayerHealthBars;
                
                // Night vision
                NightVisionIntensity = config["NightVisionIntensity"]?.Value<float>() ?? NightVisionIntensity;
                NightVisionRange = config["NightVisionRange"]?.Value<float>() ?? NightVisionRange;
                
                // Network
                AntiKick = config["AntiKick"]?.Value<bool>() ?? AntiKick;
                HearEveryone = config["HearEveryone"]?.Value<bool>() ?? HearEveryone;
                Invisibility = config["Invisibility"]?.Value<bool>() ?? Invisibility;
                DeathNotifications = config["DeathNotifications"]?.Value<bool>() ?? DeathNotifications;
                HearDeadPeople = config["HearDeadPeople"]?.Value<bool>() ?? HearDeadPeople;
                
                // UI
                var collapsedArray = config["CollapsedSections"] as JArray;
                if (collapsedArray != null)
                {
                    CollapsedSections = new System.Collections.Generic.HashSet<string>(
                        collapsedArray.Select(t => t.Value<string>()).Where(s => !string.IsNullOrEmpty(s))!
                    );
                }

                Loader.Log($"Config loaded from {ConfigPath}");
            }
            catch (Exception ex)
            {
                Loader.Log($"Failed to load config: {ex.Message}");
            }
        }

        public static void ResetConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    File.Delete(ConfigPath);
                }
                // Reset all to defaults by recreating
                GodMode = false;
                InfiniteStamina = false;
                InfiniteBattery = false;
                NoFallDamage = false;
                NoWeight = false;
                SpeedHack = false;
                SpeedMultiplier = 2.0f;
                JumpHack = false;
                JumpMultiplier = 2.0f;
                // ... etc
                Loader.Log("Config reset to defaults");
            }
            catch (Exception ex)
            {
                Loader.Log($"Failed to reset config: {ex.Message}");
            }
        }

        #endregion
    }
}
