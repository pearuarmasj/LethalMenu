using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Self Tab

        private void DrawSelfTab()
        {
            DrawSection("Player Cheats", () =>
            {
                Settings.GodMode = DrawToggle("God Mode", Settings.GodMode, "Prevents all damage");
                // Demi-God is now per-player in the Players tab
                Settings.InfiniteStamina = DrawToggle("Infinite Stamina", Settings.InfiniteStamina, "Never run out of sprint");
                Settings.NoFallDamage = DrawToggle("No Fall Damage", Settings.NoFallDamage, "Take no damage from falls");
                Settings.NoWeight = DrawToggle("No Weight", Settings.NoWeight, "Carry unlimited items without slowdown");

                // Extra item slots
                Settings.ExtraItemSlots = DrawToggle("Extra Item Slots", Settings.ExtraItemSlots, "Expand inventory (requires restart)");
                if (Settings.ExtraItemSlots)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Slots: {Settings.ItemSlotCount}", _labelStyle, GUILayout.Width(80));
                    Settings.ItemSlotCount = (int)GUILayout.HorizontalSlider(Settings.ItemSlotCount, 4, 20, GUILayout.Width(120));
                    GUILayout.EndHorizontal();
                    GUILayout.Label("  Changes apply on game restart", _labelStyle);
                }

                Settings.UnlimitedOxygen = DrawToggle("Unlimited Oxygen", Settings.UnlimitedOxygen, "No drowning");
                Settings.AntiFlash = DrawToggle("Anti-Flash", Settings.AntiFlash, "Block stun grenade effects");
                Settings.NoQuicksand = DrawToggle("No Quicksand", Settings.NoQuicksand, "No sinking/slowing");

                // Self-Revive button (only show when dead)
                if (LethalMenuMod.LocalPlayer?.isPlayerDead == true)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button("Self Revive", _buttonStyle, GUILayout.Height(28)))
                    {
                        Cheats.NetworkCheats.SelfRevive();
                    }
                    GUILayout.Label("  Respawn at ship (client-side)", _labelStyle);
                }

                // Fake Death button (only show when alive)
                if (LethalMenuMod.LocalPlayer?.isPlayerDead == false)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Settings.FakeDeath ? "Cancel Fake Death" : "Fake Death", _buttonStyle, GUILayout.Height(28)))
                    {
                        if (Settings.FakeDeath)
                            Cheats.NetworkCheats.CancelFakeDeath();
                        else
                            Cheats.NetworkCheats.FakeDeath();
                    }
                    GUILayout.EndHorizontal();
                    if (Settings.FakeDeath)
                    {
                        GUILayout.Label("  Others see you dead. Will die when ship leaves.", _labelStyle);
                    }
                    else
                    {
                        GUILayout.Label("  Appear dead to others, stay alive", _labelStyle);
                    }
                }
            });

            DrawSection("Movement", () =>
            {
                Settings.NoClip = DrawToggle("No Clip", Settings.NoClip, "Fly through walls (WASD + Space/Ctrl)");
                Settings.SpeedHack = DrawToggle("Speed Hack", Settings.SpeedHack, "Move faster");

                if (Settings.SpeedHack)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Speed: {Settings.SpeedMultiplier:F1}x", _labelStyle, GUILayout.Width(80));
                    Settings.SpeedMultiplier = GUILayout.HorizontalSlider(Settings.SpeedMultiplier, 1f, 10f, GUILayout.Width(200));
                    GUILayout.EndHorizontal();
                }

                Settings.JumpHack = DrawToggle("Jump Hack", Settings.JumpHack, "Jump higher");

                if (Settings.JumpHack)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Jump: {Settings.JumpMultiplier:F1}x", _labelStyle, GUILayout.Width(80));
                    Settings.JumpMultiplier = GUILayout.HorizontalSlider(Settings.JumpMultiplier, 1f, 10f, GUILayout.Width(200));
                    GUILayout.EndHorizontal();
                }

                Settings.SuperSpeed = DrawToggle("Super Speed", Settings.SuperSpeed, "Move much faster");
                Settings.SuperJump = DrawToggle("Super Jump", Settings.SuperJump, "Jump much higher");
                Settings.UnlimitedJump = DrawToggle("Unlimited Jump", Settings.UnlimitedJump, "Jump in mid-air");
                Settings.FastClimb = DrawToggle("Fast Climb", Settings.FastClimb, "Climb ladders faster");
                Settings.TauntSlide = DrawToggle("Taunt Slide", Settings.TauntSlide, "Emote while moving");
            });

            DrawSection("Vision", () =>
            {
                Settings.NightVision = DrawToggle("Night Vision", Settings.NightVision, "See in the dark");
            });

            DrawSection("Teleport", () =>
            {
                Settings.TeleportWithItems = DrawToggle("Teleport With Items", Settings.TeleportWithItems, "Keep items when teleporting");

                if (GUILayout.Button("Teleport to Ship", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportToShip();
                }

                if (GUILayout.Button("Teleport to Main Entrance", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportToEntrance(true);
                }

                if (GUILayout.Button("Teleport to Fire Exit", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportToEntrance(false);
                }
            });
        }

        // Actions
        private void TeleportToShip()
        {
            if (LethalMenuMod.LocalPlayer == null || LethalMenuMod.GameInstance == null) return;

            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal != null)
            {
                LethalMenuMod.LocalPlayer.TeleportPlayer(terminal.transform.position);
            }
        }

        private void TeleportToEntrance(bool mainEntrance)
        {
            if (LethalMenuMod.LocalPlayer == null) return;

            var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
            if (entrances == null || entrances.Length == 0) return;

            EntranceTeleport? target = null;
            foreach (var entrance in entrances)
            {
                if (entrance == null) continue;

                // entranceId 0 = main entrance, 1+ = fire exits
                if (mainEntrance && entrance.entranceId == 0)
                {
                    target = entrance;
                    break;
                }
                else if (!mainEntrance && entrance.entranceId != 0)
                {
                    target = entrance;
                    break;
                }
            }

            if (target != null)
            {
                LethalMenuMod.LocalPlayer.transform.position = target.entrancePoint?.position ?? target.transform.position;
            }
        }

        #endregion
    }
}
