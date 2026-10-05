using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region World Tab

        private void DrawWorldTab()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Weather Manager", _buttonStyle, GUILayout.Height(28)))
                _weatherManager.IsOpen = !_weatherManager.IsOpen;
            if (GUILayout.Button("Loot Manager", _buttonStyle, GUILayout.Height(28)))
                _lootManager.IsOpen = !_lootManager.IsOpen;
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            DrawSection("Ship", () =>
            {
                DrawHackToggle(Hack.ShipDoorInSpace, "Ship Door In Space", "Open ship door in space");
                DrawHackToggle(Hack.NoShipDoorClose, "No Ship Door Close", "Block host from closing the ship door");
                DrawHackToggle(Hack.VehicleGodMode, "Vehicle God Mode", "Cruiser ignores damage");
                DrawHackToggle(Hack.TriggerGun, "Trigger Gun", "Middle-click activates whatever the camera points at (mine/turret/door/etc.). Hold E to possess enemies.");
            });

            DrawSection("Fusebox Control", () =>
            {
                var breakerBoxes = LethalMenuMod.BreakerBoxes;
                if (breakerBoxes == null || breakerBoxes.Count == 0)
                {
                    GUILayout.Label("No fuseboxes found", _labelStyle);
                    return;
                }

                for (int boxIndex = 0; boxIndex < breakerBoxes.Count; boxIndex++)
                {
                    var box = breakerBoxes[boxIndex];
                    if (box == null) continue;

                    // Header row
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(box.isPowerOn ? "POWER ON" : "POWER OFF", _labelStyle);
                    if (GUILayout.Button("All ON", _buttonStyle, GUILayout.Width(60)))
                        SetAllSwitches(box, true);
                    if (GUILayout.Button("All OFF", _buttonStyle, GUILayout.Width(60)))
                        SetAllSwitches(box, false);
                    GUILayout.EndHorizontal();

                    // Switches
                    if (box.breakerSwitches != null)
                    {
                        for (int i = 0; i < box.breakerSwitches.Length; i++)
                        {
                            var anim = box.breakerSwitches[i];
                            if (anim == null) continue;
                            var trig = anim.gameObject.GetComponent<AnimatedObjectTrigger>();
                            if (trig == null) continue;

                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"  {i + 1}. {(trig.boolValue ? "ON" : "OFF")}", _labelStyle);
                            if (GUILayout.Button("Toggle", _buttonStyle, GUILayout.Width(60)))
                                ToggleSwitch(box, i);
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            });

            DrawSection("Environment", () =>
            {
                DrawHackToggle(Hack.BridgeNeverFalls, "Bridge Never Falls", "Bridges don't collapse");
                DrawHackToggle(Hack.AutoOpenDropship, "Auto-Open Dropship", "Dropship opens on landing");
                DrawHackToggle(Hack.Shoplifter, "Shoplifter", "Terminal items cost $0");
                DrawHackToggle(Hack.AntiJeb, "Anti-Jeb", "Company desk won't attack");
                DrawHackToggle(Hack.BuildAnywhere, "Build Anywhere", "Place furniture outside ship");
                DrawHackToggle(Hack.InstantInteract, "Instant Interact", "No hold-to-interact delay");
            });
            // Hazard controls moved to Network tab -> Hazard Control section
        }

        #region Fusebox Control Helpers

        private void ToggleSwitch(BreakerBox box, int switchIndex)
        {
            if (box == null || box.breakerSwitches == null) return;
            if (switchIndex < 0 || switchIndex >= box.breakerSwitches.Length) return;

            var switchAnimator = box.breakerSwitches[switchIndex];
            if (switchAnimator == null) return;

            var trigger = switchAnimator.gameObject.GetComponent<AnimatedObjectTrigger>();
            if (trigger == null) return;

            bool wasOn = trigger.boolValue;

            // Toggle the switch - TriggerAnimationNonPlayer handles animation, boolValue, audio,
            // AND fires onTriggerBool event which calls BreakerBox.SwitchBreaker automatically
            trigger.TriggerAnimationNonPlayer(false, false, false);

            Loader.Log($"[LethalMenu] Toggled switch {switchIndex}: {(wasOn ? "ON->OFF" : "OFF->ON")}");
        }

        private void SetAllSwitches(BreakerBox box, bool targetState)
        {
            if (box == null || box.breakerSwitches == null) return;

            for (int i = 0; i < box.breakerSwitches.Length; i++)
            {
                var switchAnimator = box.breakerSwitches[i];
                if (switchAnimator == null) continue;

                var trigger = switchAnimator.gameObject.GetComponent<AnimatedObjectTrigger>();
                if (trigger == null) continue;

                bool currentState = trigger.boolValue;

                // Only toggle if current state differs from target
                if (currentState != targetState)
                {
                    // TriggerAnimationNonPlayer handles everything including the BreakerBox counter update
                    trigger.TriggerAnimationNonPlayer(false, false, false);
                }
            }

            Loader.Log($"[LethalMenu] Set all switches to {(targetState ? "ON" : "OFF")}");
        }

        #endregion

        #endregion
    }
}
