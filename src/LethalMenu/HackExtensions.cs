using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem.Controls;

namespace LethalMenu
{
    public static class HackExtensions
    {
        public static readonly Dictionary<Hack, bool> ToggleFlags = new();
        public static readonly Dictionary<Hack, Delegate> Executors = new();
        public static readonly Dictionary<Hack, ButtonControl> KeyBinds = new();
        private static readonly Dictionary<Hack, string> DisplayNames = new();

        public static void InitializeDefaults()
        {
            foreach (Hack hack in Enum.GetValues(typeof(Hack)))
                ToggleFlags[hack] = false;
        }

        public static bool IsEnabled(this Hack hack) =>
            ToggleFlags.TryGetValue(hack, out bool val) && val;

        public static bool CanBeToggled(this Hack hack) =>
            ToggleFlags.ContainsKey(hack);

        public static void Toggle(this Hack hack)
        {
            if (!ToggleFlags.TryGetValue(hack, out bool current)) return;
            bool next = !current;
            ToggleFlags[hack] = next;
            if (next) return;
        }

        public static void SetEnabled(this Hack hack, bool enabled)
        {
            if (!ToggleFlags.ContainsKey(hack)) return;
            ToggleFlags[hack] = enabled;
        }

        public static void Execute(this Hack hack, params object[] args)
        {
            if (!Executors.TryGetValue(hack, out Delegate del)) return;
            del.DynamicInvoke(args.Length == 0 ? null : args);
        }

        public static string GetDisplayName(this Hack hack)
        {
            if (DisplayNames.TryGetValue(hack, out string cached)) return cached;
            string name = Regex.Replace(hack.ToString(), "([a-z])([A-Z])", "$1 $2");
            DisplayNames[hack] = name;
            return name;
        }

        public static void SetKeyBind(this Hack hack, ButtonControl button)
        {
            if (button == null)
                KeyBinds.Remove(hack);
            else
                KeyBinds[hack] = button;
        }

        public static ButtonControl? GetKeyBind(this Hack hack) =>
            KeyBinds.TryGetValue(hack, out ButtonControl btn) ? btn : null;

        public static void RegisterExecutor(this Hack hack, Action action) =>
            Executors[hack] = action;

        public static void RegisterExecutor<T>(this Hack hack, Action<T> action) =>
            Executors[hack] = action;

        public static void CheckKeyBinds()
        {
            foreach (var kv in KeyBinds)
            {
                if (kv.Value == null || !kv.Value.wasPressedThisFrame) continue;
                if (kv.Key.CanBeToggled())
                    kv.Key.Toggle();
                else
                    kv.Key.Execute();
            }
        }
    }
}
