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
        public static int SpectatePlayerIndex { get; set; } = -1;
        public static float BreadcrumbInterval { get; set; } = 3f;
        public static string SpamMessage { get; set; } = "SPAM";

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

                var config = new JObject
                {
                    ["Version"] = 2,
                    ["ToggleFlags"] = toggles,

                    ["ItemSlotCount"] = ItemSlotCount,
                    ["SpeedMultiplier"] = SpeedMultiplier,
                    ["JumpMultiplier"] = JumpMultiplier,
                    ["SuperJumpForce"] = SuperJumpForce,
                    ["FreeCamSpeed"] = FreeCamSpeed,
                    ["ThirdPersonDistance"] = ThirdPersonDistance,
                    ["CrosshairScale"] = CrosshairScale,
                    ["CrosshairThickness"] = CrosshairThickness,
                    ["FOVValue"] = FOVValue,
                    ["BreadcrumbInterval"] = BreadcrumbInterval,
                    ["NightVisionIntensity"] = NightVisionIntensity,
                    ["NightVisionRange"] = NightVisionRange,

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
                CrosshairScale = config["CrosshairScale"]?.Value<float>() ?? CrosshairScale;
                CrosshairThickness = config["CrosshairThickness"]?.Value<float>() ?? CrosshairThickness;
                FOVValue = config["FOVValue"]?.Value<float>() ?? FOVValue;
                BreadcrumbInterval = config["BreadcrumbInterval"]?.Value<float>() ?? BreadcrumbInterval;
                NightVisionIntensity = config["NightVisionIntensity"]?.Value<float>() ?? NightVisionIntensity;
                NightVisionRange = config["NightVisionRange"]?.Value<float>() ?? NightVisionRange;

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
