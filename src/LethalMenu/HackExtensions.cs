using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LethalMenu
{
    public static class HackExtensions
    {
        public static readonly Dictionary<Hack, bool> ToggleFlags = new();
        public static readonly Dictionary<Hack, Delegate> Executors = new();
        public static readonly Dictionary<Hack, ButtonControl> KeyBinds = new();
        /// Names shown in the keybind list. Seeded where the menu label differs from the spaced enum name.
        private static readonly Dictionary<Hack, string> DisplayNames = new()
        {
            [Hack.BHop] = "BHop",
            [Hack.AntiFlash] = "Anti-Flash",
            [Hack.AntiGhostGirl] = "Anti-Ghost Girl",
            [Hack.AntiJeb] = "Anti-Jeb",
            [Hack.AntiKick] = "Anti-Kick",
            [Hack.OneHanded] = "One-Handed",
            [Hack.Reach] = "Extended Reach",
            [Hack.LootBeforeGameStarts] = "Loot Before Start",
            [Hack.GrabNutcrackerShotgun] = "Grab Nutcracker Gun",
            [Hack.LootAnyItemBeltBag] = "Loot Any Item (Belt Bag)",
            [Hack.LootThroughWallsBeltBag] = "Loot Through Walls (Belt Bag)",
            [Hack.EnableESP] = "ESP",
            [Hack.EnableChams] = "Chams",
            [Hack.DoorESP] = "Entrance ESP",
            [Hack.MineESP] = "Landmine ESP",
            [Hack.FuseboxESP] = "Breaker Box ESP",
            [Hack.BreakerChams] = "Breaker Box Chams",
            [Hack.RadarPatch] = "Radar+",
            [Hack.EnemyDeathNotification] = "Enemy Death Notifications",
            [Hack.DeathNotifications] = "Player Death Notifications",
            [Hack.AutoOpenDropship] = "Auto-Open Dropship",
            [Hack.ShowKickedLobbies] = "Show Kicked Hosts",
            [Hack.HornSpam] = "Ship Horn Spam",
            [Hack.DoorSpam] = "Ship Door Spam",
            [Hack.SignalSpam] = "Signal Translator Spam",
            [Hack.RPCLagSpam] = "RPC Lag",
            [Hack.EarrapeSpam] = "Terminal Earrape",
            [Hack.CarHornSpam] = "Cruiser Horn Spam",
            [Hack.DeskDoorSpam] = "Company Desk Door Spam",
            [Hack.TPAllItemsToShip] = "Teleport Loot To Ship",
            [Hack.TPNearbyItems] = "Teleport Nearby Loot To Me",
            [Hack.FlickerLights] = "Flicker Ship Lights",
            [Hack.MaxChaos] = "Horn + Lights + Doors + Signal",
            [Hack.ForceStart] = "Land Ship",
            [Hack.ForceEnd] = "End Round",
            [Hack.SetCredits] = "Max Credits",
            [Hack.ReconnectFromClipboard] = "Reconnect (lobby ID from clipboard)",
            [Hack.ReleaseAllDirected] = "Release All Escorts And Hunts",
        };

        public static void InitializeDefaults()
        {
            ToggleFlags.Clear();
            foreach (Hack hack in Enum.GetValues(typeof(Hack)))
            {
                if (!hack.IsAction())
                    ToggleFlags[hack] = false;
            }
        }

        public static bool IsAction(this Hack hack) =>
            hack >= Hack.SelfRevive;

        public static bool IsEnabled(this Hack hack) =>
            ToggleFlags.TryGetValue(hack, out bool val) && val;

        public static bool CanBeToggled(this Hack hack) =>
            !hack.IsAction() && ToggleFlags.ContainsKey(hack);

        public static void Toggle(this Hack hack)
        {
            if (!ToggleFlags.TryGetValue(hack, out bool current)) return;
            ToggleFlags[hack] = !current;
        }

        public static void SetEnabled(this Hack hack, bool enabled)
        {
            if (!hack.CanBeToggled()) return;
            ToggleFlags[hack] = enabled;
        }

        public static void Execute(this Hack hack, params object[] args)
        {
            if (hack.CanBeToggled())
            {
                hack.Toggle();
                return;
            }

            if (Executors.TryGetValue(hack, out Delegate del))
                del.DynamicInvoke(args.Length == 0 ? null : args);
        }

        public static string GetDisplayName(this Hack hack)
        {
            if (DisplayNames.TryGetValue(hack, out string cached)) return cached;
            string name = Regex.Replace(hack.ToString(), "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
            DisplayNames[hack] = name;
            return name;
        }

        public static void SetKeyBind(this Hack hack, ButtonControl? button)
        {
            if (button == null)
                KeyBinds.Remove(hack);
            else
                KeyBinds[hack] = button;
        }

        public static bool SetKeyBind(this Hack hack, string? keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                hack.SetKeyBind((ButtonControl?)null);
                return true;
            }

            if (!Enum.TryParse(keyName, true, out Key key))
                return false;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            var control = keyboard[key];
            if (control == null)
                return false;

            hack.SetKeyBind(control);
            return true;
        }

        public static ButtonControl? GetKeyBind(this Hack hack) =>
            KeyBinds.TryGetValue(hack, out ButtonControl btn) ? btn : null;

        public static string GetKeyBindCode(this Hack hack)
        {
            if (!KeyBinds.TryGetValue(hack, out ButtonControl btn))
                return string.Empty;

            return btn is KeyControl keyControl ? keyControl.keyCode.ToString() : btn.name;
        }

        public static string GetKeyBindDisplayName(this Hack hack)
        {
            if (!KeyBinds.TryGetValue(hack, out ButtonControl btn))
                return "Unbound";

            return string.IsNullOrWhiteSpace(btn.displayName) ? btn.name : btn.displayName;
        }

        public static KeyControl? GetPressedKeyboardKey()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return null;

            foreach (var key in keyboard.allKeys)
            {
                if (key.wasPressedThisFrame)
                    return key;
            }

            return null;
        }

        public static void ClearKeyBinds() => KeyBinds.Clear();

        public static void RegisterExecutor(this Hack hack, Action action) =>
            Executors[hack] = action;

        public static void RegisterExecutor<T>(this Hack hack, Action<T> action) =>
            Executors[hack] = action;

        public static void CheckKeyBinds()
        {
            foreach (var kv in KeyBinds)
            {
                if (kv.Value == null || !kv.Value.wasPressedThisFrame) continue;
                kv.Key.Execute();
            }
        }
    }
}
