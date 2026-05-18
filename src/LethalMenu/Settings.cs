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

        public static void ToggleDemiGod(PlayerControllerB player)
        {
            if (player == null) return;

            if (DemiGodPlayers.Contains(player.playerClientId))
                DemiGodPlayers.Remove(player.playerClientId);
            else
                DemiGodPlayers.Add(player.playerClientId);
        }

        public static void SetDemiGod(PlayerControllerB player, bool enabled)
        {
            if (player == null) return;

            if (enabled)
                DemiGodPlayers.Add(player.playerClientId);
            else
                DemiGodPlayers.Remove(player.playerClientId);

            if (LethalMenuMod.LocalPlayer != null &&
                player.playerClientId == LethalMenuMod.LocalPlayer.playerClientId)
            {
                Hack.DemiGod.SetEnabled(enabled);
            }
        }

        // Non-boolean settings (float/int/string/Color/HashSet)
        public static int ItemSlotCount { get; set; } = 4;
        public static bool SpinCamera { get; set; } = true;
        public static bool SpinModel { get; set; } = true;
        public static float SpinDuration { get; set; } = 10f;
        public static float SpeedMultiplier { get; set; } = 2.0f;
        public static float JumpMultiplier { get; set; } = 2.0f;
        public static float CrosshairScale { get; set; } = 10f;
        public static float CrosshairThickness { get; set; } = 2f;
        public static Color CrosshairColor { get; set; } = Color.white;
        public static float FOVValue { get; set; } = 90f;
        public static float SuperJumpForce { get; set; } = 20f;
        public static float NightVisionIntensity { get; set; } = 10000f;
        public static float NightVisionRange { get; set; } = 10000f;
        public static float FreeCamSpeed { get; set; } = 10f;
        public static float ThirdPersonDistance { get; set; } = 3f;
        public static float VisibleBodyCameraOffset { get; set; } = 0.15f;
        public static int SpectatePlayerIndex { get; set; } = -1;
        public static float BreadcrumbInterval { get; set; } = 3f;
        public static string SpamMessage { get; set; } = "SPAM";

        // D+E tunables
        public static float PhantomMoveSpeed { get; set; } = 20f;
        public static bool PhantomTeleportOnExit { get; set; } = true;
        public static float FollowDelaySeconds { get; set; } = 1.0f;
        public static float FollowMaxDistance { get; set; } = 1.0f;
        public static float PJSpammerRate { get; set; } = 5f;

        // Theme settings
        public static string ThemeName { get; set; } = "Default";
        public static float MenuAlpha { get; set; } = 1f;
        public static int MenuFontSize { get; set; } = 14;
        public static int SliderWidth { get; set; } = 80;
        public static int TextboxWidth { get; set; } = 80;
        public static bool HackHighlight { get; set; } = true;

        // Fake death state (runtime only, not persisted)
        public static bool FakeDeath { get; set; } = false;

        // Networking state
        public static HashSet<ulong> KickedHostIds { get; set; } = new HashSet<ulong>();
        internal static bool WasKicked { get; set; } = false;
        internal static bool HostQuit { get; set; } = false;
        public static ulong CurrentLobbyId { get; set; } = 0;
        public static ulong CurrentLobbyOwnerId { get; set; } = 0;

        // ESP colors
        public static Color PlayerColor { get; set; } = Color.green;
        public static Color EnemyColor { get; set; } = Color.red;
        public static Color ItemColor { get; set; } = Color.yellow;
        public static Color DoorColor { get; set; } = Color.cyan;
        public static Color MineColor { get; set; } = new Color(1f, 0.5f, 0f);
        public static Color TurretColor { get; set; } = Color.magenta;
        public static Color FuseboxColor { get; set; } = new Color(1f, 1f, 0.5f);

        // Cham system
        public static Color ChamColor { get; set; } = Color.white;
        public static bool UseSingleChamColor { get; set; } = false;
        public static float ChamDistance { get; set; } = 0f;

        public static Color PlayerChamColor { get; set; } = Color.green;
        public static Color EnemyChamColor { get; set; } = Color.red;
        public static Color ItemChamColor { get; set; } = Color.yellow;
        public static Color LandmineChamColor { get; set; } = new Color(1f, 0.5f, 0f);
        public static Color TurretChamColor { get; set; } = new Color(1f, 0.25f, 0f);
        public static Color DoorChamColor { get; set; } = new Color(0.5f, 0.5f, 1f);
        public static Color BigDoorChamColor { get; set; } = new Color(0.25f, 0.25f, 1f);
        public static Color ShipDoorChamColor { get; set; } = Color.white;
        public static Color BreakerChamColor { get; set; } = Color.magenta;
        public static Color EnemyVentChamColor { get; set; } = new Color(0.5f, 0f, 0.5f);
        public static Color ItemDropshipChamColor { get; set; } = Color.cyan;
        public static Color CruiserChamColor { get; set; } = new Color(1f, 0.78f, 0f);
        public static Color MoldSporeChamColor { get; set; } = new Color(0f, 0.78f, 0f);
        public static Color MineshaftElevatorChamColor { get; set; } = new Color(0.7f, 0.7f, 0.7f);
        public static Color EntranceChamColor { get; set; } = new Color(0f, 0.5f, 1f);
        public static Color SpikeRoofTrapChamColor { get; set; } = new Color(1f, 0f, 0.25f);
        public static Color SteamValveChamColor { get; set; } = Color.white;

        // Debug
        public static string DebugMessage { get; set; } = "";

        // UI persistence
        public static HashSet<string> CollapsedSections { get; set; } = new HashSet<string>();
        public static float WindowX { get; set; } = 50f;
        public static float WindowY { get; set; } = 50f;
        public static float WindowWidth { get; set; } = 500f;
        public static float WindowHeight { get; set; } = 400f;

        #region Config Save/Load

        // v1 property names that differ from Hack enum names
        private static readonly Dictionary<string, Hack> V1NameMapping = new()
        {
            ["ESP"] = Hack.EnableESP,
            ["FOV"] = Hack.CustomFOV,
            ["NoFieldOfDepth"] = Hack.NoDepthOfField,
            ["OpenDropShipLand"] = Hack.AutoOpenDropship,
            ["OpenShipDoorSpace"] = Hack.ShipDoorInSpace,
            ["JebAttackPrevention"] = Hack.AntiJeb,
            ["ChatSpamLoop"] = Hack.ChatSpam,
            ["TerminalEarrapeSpam"] = Hack.EarrapeSpam,
        };

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
                    ["Version"] = 2,
                    ["ToggleFlags"] = toggles,
                    ["KeyBinds"] = keyBinds,

                    ["ItemSlotCount"] = ItemSlotCount,
                    ["SpeedMultiplier"] = SpeedMultiplier,
                    ["JumpMultiplier"] = JumpMultiplier,
                    ["SuperJumpForce"] = SuperJumpForce,
                    ["FreeCamSpeed"] = FreeCamSpeed,
                    ["ThirdPersonDistance"] = ThirdPersonDistance,
                    ["VisibleBodyCameraOffset"] = VisibleBodyCameraOffset,
                    ["CrosshairScale"] = CrosshairScale,
                    ["CrosshairThickness"] = CrosshairThickness,
                    ["FOVValue"] = FOVValue,
                    ["BreadcrumbInterval"] = BreadcrumbInterval,
                    ["NightVisionIntensity"] = NightVisionIntensity,
                    ["NightVisionRange"] = NightVisionRange,

                    ["PhantomMoveSpeed"] = PhantomMoveSpeed,
                    ["PhantomTeleportOnExit"] = PhantomTeleportOnExit,
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

                int version = config["Version"]?.Value<int>() ?? 1;

                if (version >= 2)
                {
                    LoadV2(config);
                }
                else
                {
                    LoadV1Legacy(config);
                }

                // Non-boolean settings (shared across versions)
                ItemSlotCount = config["ItemSlotCount"]?.Value<int>() ?? ItemSlotCount;
                SpeedMultiplier = config["SpeedMultiplier"]?.Value<float>() ?? SpeedMultiplier;
                JumpMultiplier = config["JumpMultiplier"]?.Value<float>() ?? JumpMultiplier;
                SuperJumpForce = config["SuperJumpForce"]?.Value<float>() ?? SuperJumpForce;
                FreeCamSpeed = config["FreeCamSpeed"]?.Value<float>() ?? FreeCamSpeed;
                ThirdPersonDistance = config["ThirdPersonDistance"]?.Value<float>() ?? ThirdPersonDistance;
                VisibleBodyCameraOffset = config["VisibleBodyCameraOffset"]?.Value<float>() ?? VisibleBodyCameraOffset;
                CrosshairScale = config["CrosshairScale"]?.Value<float>() ?? CrosshairScale;
                CrosshairThickness = config["CrosshairThickness"]?.Value<float>() ?? CrosshairThickness;
                FOVValue = config["FOVValue"]?.Value<float>() ?? FOVValue;
                BreadcrumbInterval = config["BreadcrumbInterval"]?.Value<float>() ?? BreadcrumbInterval;
                NightVisionIntensity = config["NightVisionIntensity"]?.Value<float>() ?? NightVisionIntensity;
                NightVisionRange = config["NightVisionRange"]?.Value<float>() ?? NightVisionRange;

                PhantomMoveSpeed = config["PhantomMoveSpeed"]?.Value<float>() ?? PhantomMoveSpeed;
                PhantomTeleportOnExit = config["PhantomTeleportOnExit"]?.Value<bool>() ?? PhantomTeleportOnExit;
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

                Loader.Log($"Config loaded (v{version}) from {ConfigPath}");
            }
            catch (Exception ex)
            {
                Loader.Log($"Failed to load config: {ex.Message}");
            }
        }

        private static void LoadV2(JObject config)
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

        private static void LoadV1Legacy(JObject config)
        {
            foreach (Hack hack in Enum.GetValues(typeof(Hack)))
            {
                // Skip action-type hacks (not toggles in v1)
                if (hack >= Hack.SelfRevive) continue;

                string key = hack.ToString();

                // Check renamed properties first
                string? v1Key = null;
                foreach (var mapping in V1NameMapping)
                {
                    if (mapping.Value == hack)
                    {
                        v1Key = mapping.Key;
                        break;
                    }
                }

                bool loaded = false;
                if (v1Key != null && config[v1Key] != null)
                {
                    hack.SetEnabled(config[v1Key]!.Value<bool>());
                    loaded = true;
                }

                if (!loaded && config[key] != null)
                {
                    hack.SetEnabled(config[key]!.Value<bool>());
                }
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

                SpeedMultiplier = 2.0f;
                JumpMultiplier = 2.0f;
                FOVValue = 90f;
                SuperJumpForce = 20f;
                FreeCamSpeed = 10f;
                ThirdPersonDistance = 3f;
                CrosshairScale = 10f;
                CrosshairThickness = 2f;
                BreadcrumbInterval = 3f;
                NightVisionIntensity = 10000f;
                NightVisionRange = 10000f;
                ItemSlotCount = 4;
                SliderWidth = 80;
                TextboxWidth = 80;
                HackHighlight = true;
                CrosshairColor = Color.white;
                PlayerColor = Color.green;
                EnemyColor = Color.red;
                ItemColor = Color.yellow;
                DoorColor = Color.cyan;
                MineColor = new Color(1f, 0.5f, 0f);
                TurretColor = Color.magenta;
                FuseboxColor = new Color(1f, 1f, 0.5f);
                HackExtensions.ClearKeyBinds();

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
