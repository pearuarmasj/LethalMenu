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
            string name = Regex.Replace(hack.ToString(), "([a-z])([A-Z])", "$1 $2");
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
                kv.Key.Execute();
            }
        }
    }
}
