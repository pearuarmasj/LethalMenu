using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameNetcodeStuff;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LethalMenu
{
    public static class Settings
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LethalMenu", "config.json");

        // Menu state
        public static bool ShowMenu { get; set; } = false;

        // Per-player Demi-God tracking
        public static HashSet<ulong> DemiGodPlayers { get; set; } = new HashSet<ulong>();

        public static bool IsDemiGod(PlayerControllerB player)
        {
            return player != null && DemiGodPlayers.Contains(player.playerClientId);
        }

        public static void SetDemiGod(PlayerControllerB player, bool enabled)
        {
            if (player == null) return;

            if (enabled)
                DemiGodPlayers.Add(player.playerClientId);
            else
                DemiGodPlayers.Remove(player.playerClientId);
        }

        // Non-boolean settings (float/int/string/Color/HashSet)
        public static int ItemSlotCount { get; set; }
        public static bool SpinCamera { get; set; }
        public static bool SpinModel { get; set; }
        public static float SpinDuration { get; set; }
        public static float SpeedMultiplier { get; set; }
        public static float JumpMultiplier { get; set; }
        public static float CrosshairScale { get; set; }
        public static float CrosshairThickness { get; set; }
        public static Color CrosshairColor { get; set; }
        public static float FOVValue { get; set; }
        public static float NightVisionIntensity { get; set; }
        public static float NightVisionRange { get; set; }
        public static float FreeCamSpeed { get; set; }
        public static float ThirdPersonDistance { get; set; }
        public static int SpectatePlayerIndex { get; set; } = -1;
        public static float BreadcrumbInterval { get; set; }
        public static string SpamMessage { get; set; } = default!;

        // D+E tunables
        public static float FollowDelaySeconds { get; set; }
        public static float FollowMaxDistance { get; set; }
        public static float PJSpammerRate { get; set; }

        // Theme settings
        public static string ThemeName { get; set; } = default!;
        public static float MenuAlpha { get; set; }
        public static int MenuFontSize { get; set; }
        public static int SliderWidth { get; set; }
        public static int TextboxWidth { get; set; }
        public static bool HackHighlight { get; set; }

        // Fake death state (runtime only, not persisted)
        public static bool FakeDeath { get; set; } = false;

        // Networking state
        public static HashSet<ulong> KickedHostIds { get; set; } = new HashSet<ulong>();

        // Players escorts ignore and that can't be hunt targets, by Steam ID so it survives lobbies.
        public static HashSet<ulong> FriendSteamIds { get; set; } = new HashSet<ulong>();

        public static bool IsFriend(PlayerControllerB? player) =>
            player != null && player.playerSteamId != 0 && FriendSteamIds.Contains(player.playerSteamId);

        public static void SetFriend(PlayerControllerB player, bool friend)
        {
            if (player == null || player.playerSteamId == 0) return;
            if (friend) FriendSteamIds.Add(player.playerSteamId);
            else FriendSteamIds.Remove(player.playerSteamId);
        }

        internal static bool WasKicked { get; set; } = false;
        internal static bool HostQuit { get; set; } = false;
        public static ulong CurrentLobbyId { get; set; } = 0;
        public static ulong CurrentLobbyOwnerId { get; set; } = 0;

        // ESP colors
        public static Color PlayerColor { get; set; }
        public static Color EnemyColor { get; set; }
        public static Color ItemColor { get; set; }
        public static Color DoorColor { get; set; }
        public static Color MineColor { get; set; }
        public static Color TurretColor { get; set; }
        public static Color FuseboxColor { get; set; }

        // Cham system
        public static Color ChamColor { get; set; }
        public static bool UseSingleChamColor { get; set; }
        public static float ChamDistance { get; set; }

        public static Color PlayerChamColor { get; set; }
        public static Color EnemyChamColor { get; set; }
        public static Color ItemChamColor { get; set; }
        public static Color LandmineChamColor { get; set; }
        public static Color TurretChamColor { get; set; }
        public static Color DoorChamColor { get; set; }
        public static Color BigDoorChamColor { get; set; }
        public static Color ShipDoorChamColor { get; set; }
        public static Color BreakerChamColor { get; set; }
        public static Color EnemyVentChamColor { get; set; }
        public static Color ItemDropshipChamColor { get; set; }
        public static Color CruiserChamColor { get; set; }
        public static Color MoldSporeChamColor { get; set; }
        public static Color MineshaftElevatorChamColor { get; set; }
        public static Color EntranceChamColor { get; set; }
        public static Color SpikeRoofTrapChamColor { get; set; }
        public static Color SteamValveChamColor { get; set; }

        // Debug
        public static string DebugMessage { get; set; } = "";

        // UI persistence
        public static HashSet<string> CollapsedSections { get; set; } = default!;
        public static float WindowX { get; set; }
        public static float WindowY { get; set; }
        public static float WindowWidth { get; set; }
        public static float WindowHeight { get; set; }

        #region Config Save/Load

        /// Single source of truth for user-preference defaults; used at startup and by ResetConfig.
        private static void ApplyDefaults()
        {
            ItemSlotCount = 4;
            SpinCamera = true;
            SpinModel = true;
            SpinDuration = 10f;
            SpeedMultiplier = 2.0f;
            JumpMultiplier = 2.0f;
            CrosshairScale = 10f;
            CrosshairThickness = 2f;
            CrosshairColor = Color.white;
            FOVValue = 90f;
            NightVisionIntensity = 10000f;
            NightVisionRange = 10000f;
            FreeCamSpeed = 10f;
            ThirdPersonDistance = 3f;
            BreadcrumbInterval = 3f;
            SpamMessage = "SPAM";
            FollowDelaySeconds = 1.0f;
            FollowMaxDistance = 1.0f;
            PJSpammerRate = 5f;
            ThemeName = "Default";
            MenuAlpha = 1f;
            MenuFontSize = 14;
            SliderWidth = 80;
            TextboxWidth = 80;
            HackHighlight = true;
            PlayerColor = Color.green;
            EnemyColor = Color.red;
            ItemColor = Color.yellow;
            DoorColor = Color.cyan;
            MineColor = new Color(1f, 0.5f, 0f);
            TurretColor = Color.magenta;
            FuseboxColor = new Color(1f, 1f, 0.5f);
            ChamColor = Color.white;
            UseSingleChamColor = false;
            ChamDistance = 0f;
            PlayerChamColor = Color.green;
            EnemyChamColor = Color.red;
            ItemChamColor = Color.yellow;
            LandmineChamColor = new Color(1f, 0.5f, 0f);
            TurretChamColor = new Color(1f, 0.25f, 0f);
            DoorChamColor = new Color(0.5f, 0.5f, 1f);
            BigDoorChamColor = new Color(0.25f, 0.25f, 1f);
            ShipDoorChamColor = Color.white;
            BreakerChamColor = Color.magenta;
            EnemyVentChamColor = new Color(0.5f, 0f, 0.5f);
            ItemDropshipChamColor = Color.cyan;
            CruiserChamColor = new Color(1f, 0.78f, 0f);
            MoldSporeChamColor = new Color(0f, 0.78f, 0f);
            MineshaftElevatorChamColor = new Color(0.7f, 0.7f, 0.7f);
            EntranceChamColor = new Color(0f, 0.5f, 1f);
            SpikeRoofTrapChamColor = new Color(1f, 0f, 0.25f);
            SteamValveChamColor = Color.white;
            CollapsedSections = new HashSet<string>();
            WindowX = 50f;
            WindowY = 50f;
            WindowWidth = 500f;
            WindowHeight = 400f;
        }

        static Settings() => ApplyDefaults();

        public static void SaveConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var toggles = new JObject();
                foreach (var kv in HackExtensions.ToggleFlags)
                {
                    toggles[kv.Key.ToString()] = kv.Value;
                }

                var keyBinds = new JObject();
                foreach (var kv in HackExtensions.KeyBinds)
                {
                    string keyCode = kv.Key.GetKeyBindCode();
                    if (!string.IsNullOrWhiteSpace(keyCode))
                        keyBinds[kv.Key.ToString()] = keyCode;
                }

                var config = new JObject
                {
                    ["ToggleFlags"] = toggles,
                    ["KeyBinds"] = keyBinds,

                    ["ItemSlotCount"] = ItemSlotCount,
                    ["SpeedMultiplier"] = SpeedMultiplier,
                    ["JumpMultiplier"] = JumpMultiplier,
                    ["FreeCamSpeed"] = FreeCamSpeed,
                    ["ThirdPersonDistance"] = ThirdPersonDistance,
                    ["CrosshairScale"] = CrosshairScale,
                    ["CrosshairThickness"] = CrosshairThickness,
                    ["FOVValue"] = FOVValue,
                    ["BreadcrumbInterval"] = BreadcrumbInterval,
                    ["NightVisionIntensity"] = NightVisionIntensity,
                    ["NightVisionRange"] = NightVisionRange,

                    ["FollowDelaySeconds"] = FollowDelaySeconds,
                    ["FollowMaxDistance"] = FollowMaxDistance,
                    ["PJSpammerRate"] = PJSpammerRate,

                    ["ThemeName"] = ThemeName,
                    ["MenuAlpha"] = MenuAlpha,
                    ["MenuFontSize"] = MenuFontSize,
                    ["SliderWidth"] = SliderWidth,
                    ["TextboxWidth"] = TextboxWidth,
                    ["HackHighlight"] = HackHighlight,

                    ["CrosshairColor"] = ColorToJson(CrosshairColor),
                    ["PlayerColor"] = ColorToJson(PlayerColor),
                    ["EnemyColor"] = ColorToJson(EnemyColor),
                    ["ItemColor"] = ColorToJson(ItemColor),
                    ["DoorColor"] = ColorToJson(DoorColor),
                    ["MineColor"] = ColorToJson(MineColor),
                    ["TurretColor"] = ColorToJson(TurretColor),
                    ["FuseboxColor"] = ColorToJson(FuseboxColor),

                    ["ChamColor"] = ColorToJson(ChamColor),
                    ["PlayerChamColor"] = ColorToJson(PlayerChamColor),
                    ["EnemyChamColor"] = ColorToJson(EnemyChamColor),
                    ["ItemChamColor"] = ColorToJson(ItemChamColor),
                    ["LandmineChamColor"] = ColorToJson(LandmineChamColor),
                    ["TurretChamColor"] = ColorToJson(TurretChamColor),
                    ["DoorChamColor"] = ColorToJson(DoorChamColor),
                    ["BigDoorChamColor"] = ColorToJson(BigDoorChamColor),
                    ["ShipDoorChamColor"] = ColorToJson(ShipDoorChamColor),
                    ["BreakerChamColor"] = ColorToJson(BreakerChamColor),
                    ["EnemyVentChamColor"] = ColorToJson(EnemyVentChamColor),
                    ["ItemDropshipChamColor"] = ColorToJson(ItemDropshipChamColor),
                    ["CruiserChamColor"] = ColorToJson(CruiserChamColor),
                    ["MoldSporeChamColor"] = ColorToJson(MoldSporeChamColor),
                    ["MineshaftElevatorChamColor"] = ColorToJson(MineshaftElevatorChamColor),
                    ["EntranceChamColor"] = ColorToJson(EntranceChamColor),
                    ["SpikeRoofTrapChamColor"] = ColorToJson(SpikeRoofTrapChamColor),
                    ["SteamValveChamColor"] = ColorToJson(SteamValveChamColor),
                    ["UseSingleChamColor"] = UseSingleChamColor,
                    ["ChamDistance"] = ChamDistance,

                    ["KickedHostIds"] = new JArray(KickedHostIds.Select(id => (long)id)),
                    ["FriendSteamIds"] = new JArray(FriendSteamIds.Select(id => (long)id)),

                    ["CollapsedSections"] = new JArray(CollapsedSections),
                    ["WindowX"] = WindowX,
                    ["WindowY"] = WindowY,
                    ["WindowWidth"] = WindowWidth,
                    ["WindowHeight"] = WindowHeight,
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

                LoadToggles(config);

                // Non-boolean settings (shared across versions)
                ItemSlotCount = config["ItemSlotCount"]?.Value<int>() ?? ItemSlotCount;
                SpeedMultiplier = config["SpeedMultiplier"]?.Value<float>() ?? SpeedMultiplier;
                JumpMultiplier = config["JumpMultiplier"]?.Value<float>() ?? JumpMultiplier;
                FreeCamSpeed = config["FreeCamSpeed"]?.Value<float>() ?? FreeCamSpeed;
                ThirdPersonDistance = config["ThirdPersonDistance"]?.Value<float>() ?? ThirdPersonDistance;
                CrosshairScale = config["CrosshairScale"]?.Value<float>() ?? CrosshairScale;
                CrosshairThickness = config["CrosshairThickness"]?.Value<float>() ?? CrosshairThickness;
                FOVValue = config["FOVValue"]?.Value<float>() ?? FOVValue;
                BreadcrumbInterval = config["BreadcrumbInterval"]?.Value<float>() ?? BreadcrumbInterval;
                NightVisionIntensity = config["NightVisionIntensity"]?.Value<float>() ?? NightVisionIntensity;
                NightVisionRange = config["NightVisionRange"]?.Value<float>() ?? NightVisionRange;

                FollowDelaySeconds = config["FollowDelaySeconds"]?.Value<float>() ?? FollowDelaySeconds;
                FollowMaxDistance = config["FollowMaxDistance"]?.Value<float>() ?? FollowMaxDistance;
                PJSpammerRate = config["PJSpammerRate"]?.Value<float>() ?? PJSpammerRate;

                ThemeName = config["ThemeName"]?.Value<string>() ?? ThemeName;
                MenuAlpha = config["MenuAlpha"]?.Value<float>() ?? MenuAlpha;
                MenuFontSize = config["MenuFontSize"]?.Value<int>() ?? MenuFontSize;
                SliderWidth = config["SliderWidth"]?.Value<int>() ?? SliderWidth;
                TextboxWidth = config["TextboxWidth"]?.Value<int>() ?? TextboxWidth;
                HackHighlight = config["HackHighlight"]?.Value<bool>() ?? HackHighlight;

                CrosshairColor = LoadColor(config, "CrosshairColor", CrosshairColor);
                PlayerColor = LoadColor(config, "PlayerColor", PlayerColor);
                EnemyColor = LoadColor(config, "EnemyColor", EnemyColor);
                ItemColor = LoadColor(config, "ItemColor", ItemColor);
                DoorColor = LoadColor(config, "DoorColor", DoorColor);
                MineColor = LoadColor(config, "MineColor", MineColor);
                TurretColor = LoadColor(config, "TurretColor", TurretColor);
                FuseboxColor = LoadColor(config, "FuseboxColor", FuseboxColor);

                ChamColor = LoadColor(config, "ChamColor", ChamColor);
                PlayerChamColor = LoadColor(config, "PlayerChamColor", PlayerChamColor);
                EnemyChamColor = LoadColor(config, "EnemyChamColor", EnemyChamColor);
                ItemChamColor = LoadColor(config, "ItemChamColor", ItemChamColor);
                LandmineChamColor = LoadColor(config, "LandmineChamColor", LandmineChamColor);
                TurretChamColor = LoadColor(config, "TurretChamColor", TurretChamColor);
                DoorChamColor = LoadColor(config, "DoorChamColor", DoorChamColor);
                BigDoorChamColor = LoadColor(config, "BigDoorChamColor", BigDoorChamColor);
                ShipDoorChamColor = LoadColor(config, "ShipDoorChamColor", ShipDoorChamColor);
                BreakerChamColor = LoadColor(config, "BreakerChamColor", BreakerChamColor);
                EnemyVentChamColor = LoadColor(config, "EnemyVentChamColor", EnemyVentChamColor);
                ItemDropshipChamColor = LoadColor(config, "ItemDropshipChamColor", ItemDropshipChamColor);
                CruiserChamColor = LoadColor(config, "CruiserChamColor", CruiserChamColor);
                MoldSporeChamColor = LoadColor(config, "MoldSporeChamColor", MoldSporeChamColor);
                MineshaftElevatorChamColor = LoadColor(config, "MineshaftElevatorChamColor", MineshaftElevatorChamColor);
                EntranceChamColor = LoadColor(config, "EntranceChamColor", EntranceChamColor);
                SpikeRoofTrapChamColor = LoadColor(config, "SpikeRoofTrapChamColor", SpikeRoofTrapChamColor);
                SteamValveChamColor = LoadColor(config, "SteamValveChamColor", SteamValveChamColor);

                UseSingleChamColor = config["UseSingleChamColor"]?.Value<bool>() ?? UseSingleChamColor;
                ChamDistance = config["ChamDistance"]?.Value<float>() ?? ChamDistance;

                if (config["KickedHostIds"] is JArray kickedArray)
                {
                    KickedHostIds = new HashSet<ulong>(kickedArray.Select(t => (ulong)(long)t));
                }

                if (config["FriendSteamIds"] is JArray friendArray)
                {
                    FriendSteamIds = new HashSet<ulong>(friendArray.Select(t => (ulong)(long)t));
                }

                var collapsedArray = config["CollapsedSections"] as JArray;
                if (collapsedArray != null)
                {
                    CollapsedSections = new HashSet<string>(
                        collapsedArray.Select(t => t.Value<string>()).Where(s => !string.IsNullOrEmpty(s))!
                    );
                }

                WindowX = config["WindowX"]?.Value<float>() ?? WindowX;
                WindowY = config["WindowY"]?.Value<float>() ?? WindowY;
                WindowWidth = config["WindowWidth"]?.Value<float>() ?? WindowWidth;
                WindowHeight = config["WindowHeight"]?.Value<float>() ?? WindowHeight;

                LoadKeyBinds(config);

                Loader.Log($"Config loaded from {ConfigPath}");
            }
            catch (Exception ex)
            {
                Loader.Log($"Failed to load config: {ex.Message}");
            }
        }

        private static void LoadToggles(JObject config)
        {
            var toggles = config["ToggleFlags"] as JObject;
            if (toggles == null) return;

            foreach (var prop in toggles.Properties())
            {
                if (Enum.TryParse<Hack>(prop.Name, out Hack hack))
                {
                    hack.SetEnabled(prop.Value.Value<bool>());
                }
            }
        }

        private static void LoadKeyBinds(JObject config)
        {
            HackExtensions.ClearKeyBinds();

            if (config["KeyBinds"] is not JObject keyBinds)
                return;

            foreach (var prop in keyBinds.Properties())
            {
                if (!Enum.TryParse(prop.Name, out Hack hack))
                    continue;

                string? keyName = prop.Value.Value<string>();
                if (!hack.SetKeyBind(keyName))
                    Loader.Log($"Failed to load keybind {prop.Name}: {keyName}");
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

                HackExtensions.InitializeDefaults();
                HackExtensions.ClearKeyBinds();
                ApplyDefaults();

                Loader.Log("Config reset to defaults");
            }
            catch (Exception ex)
            {
                Loader.Log($"Failed to reset config: {ex.Message}");
            }
        }

        private static JObject ColorToJson(Color color)
        {
            return new JObject
            {
                ["r"] = color.r,
                ["g"] = color.g,
                ["b"] = color.b,
                ["a"] = color.a,
            };
        }

        private static Color LoadColor(JObject config, string key, Color fallback)
        {
            if (config[key] is not JObject color)
                return fallback;

            return new Color(
                color["r"]?.Value<float>() ?? fallback.r,
                color["g"]?.Value<float>() ?? fallback.g,
                color["b"]?.Value<float>() ?? fallback.b,
                color["a"]?.Value<float>() ?? fallback.a);
        }

        #endregion
    }
}
