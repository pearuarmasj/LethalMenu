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
                DrawHackToggle(Hack.GodMode, "God Mode", "Prevents all damage");
                DrawHackToggle(Hack.InfiniteStamina, "Infinite Stamina", "Never run out of sprint");
                DrawHackToggle(Hack.NoFallDamage, "No Fall Damage", "Take no damage from falls");
                DrawHackToggle(Hack.NoWeight, "No Weight", "Carry unlimited items without slowdown");

                DrawHackToggle(Hack.ExtraItemSlots, "Extra Item Slots", "Expand inventory (requires restart)");
                if (Hack.ExtraItemSlots.IsEnabled())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Slots: {Settings.ItemSlotCount}", _labelStyle, GUILayout.Width(80));
                    Settings.ItemSlotCount = (int)GUILayout.HorizontalSlider(Settings.ItemSlotCount, 4, 20, GUILayout.Width(120));
                    GUILayout.EndHorizontal();
                    GUILayout.Label("  Changes apply on game restart", _labelStyle);
                }

                DrawHackToggle(Hack.UnlimitedOxygen, "Unlimited Oxygen", "No drowning");
                DrawHackToggle(Hack.AntiFlash, "Anti-Flash", "Block stun grenade effects");
                DrawHackToggle(Hack.NoQuicksand, "No Quicksand", "No sinking/slowing");

                if (LethalMenuMod.LocalPlayer?.isPlayerDead == true)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button("Self Revive", _buttonStyle, GUILayout.Height(28)))
                    {
                        Cheats.NetworkCheats.SelfRevive();
                    }
                    GUILayout.Label("  Respawn at ship (client-side)", _labelStyle);
                }

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
                DrawHackToggle(Hack.NoClip, "No Clip", "Fly through walls (WASD + Space/Ctrl)");
                DrawHackToggle(Hack.SpeedHack, "Speed Hack", "Move faster");

                if (Hack.SpeedHack.IsEnabled())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Speed: {Settings.SpeedMultiplier:F1}x", _labelStyle, GUILayout.Width(80));
                    Settings.SpeedMultiplier = GUILayout.HorizontalSlider(Settings.SpeedMultiplier, 1f, 10f, GUILayout.Width(200));
                    GUILayout.EndHorizontal();
                }

                DrawHackToggle(Hack.JumpHack, "Jump Hack", "Jump higher");

                if (Hack.JumpHack.IsEnabled())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Jump: {Settings.JumpMultiplier:F1}x", _labelStyle, GUILayout.Width(80));
                    Settings.JumpMultiplier = GUILayout.HorizontalSlider(Settings.JumpMultiplier, 1f, 10f, GUILayout.Width(200));
                    GUILayout.EndHorizontal();
                }

                DrawHackToggle(Hack.SuperSpeed, "Super Speed", "Move much faster");
                DrawHackToggle(Hack.SuperJump, "Super Jump", "Jump much higher");
                DrawHackToggle(Hack.UnlimitedJump, "Unlimited Jump", "Jump in mid-air");
                DrawHackToggle(Hack.FastClimb, "Fast Climb", "Climb ladders faster");
                DrawHackToggle(Hack.TauntSlide, "Taunt Slide", "Emote while moving");
            });

            DrawSection("Vision", () =>
            {
                DrawHackToggle(Hack.NightVision, "Night Vision", "See in the dark");
            });

            DrawSection("Teleport", () =>
            {
                DrawHackToggle(Hack.TeleportWithItems, "Teleport With Items", "Keep items when teleporting");

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
