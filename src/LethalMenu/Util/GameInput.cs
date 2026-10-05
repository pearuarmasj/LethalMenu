using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace LethalMenu.Util
{
    /// Reference-counted block on the game's own input. v81 reads gameplay input from the
    /// project-wide InputSystem.actions (Move, Jump, Interact, ActivateItem, ...) and mouse look from
    /// PlayerControllerB.playerActions.Movement, so both are disabled while any owner (menu open,
    /// enemy possession, phantom camera) holds a block. Raw device reads (Keyboard.current /
    /// Mouse.current) used by the menu and cheats are unaffected.
    public static class GameInput
    {
        private static readonly HashSet<object> Blockers = new();

        public static bool IsBlocked => Blockers.Count > 0;

        public static void SetBlocked(object owner, bool blocked)
        {
            bool changed = blocked ? Blockers.Add(owner) : Blockers.Remove(owner);
            if (changed) Apply();
        }

        /// Drops every block (mod unload).
        public static void Reset()
        {
            if (Blockers.Count == 0) return;
            Blockers.Clear();
            Apply();
        }

        private static void Apply()
        {
            bool block = IsBlocked;

            var actions = InputSystem.actions;
            if (actions != null)
            {
                if (block) actions.Disable();
                else actions.Enable();
            }

            var player = LethalMenuMod.LocalPlayer;
            if (player?.playerActions != null)
            {
                if (block) player.playerActions.Movement.Disable();
                else player.playerActions.Movement.Enable();
            }
        }
    }
}
