using LethalMenu.Mixins;
using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Players Tab

        private void DrawPlayersTab()
        {
            DrawSection("Players", () =>
            {
                if (LethalMenuMod.Players.Count == 0)
                {
                    GUILayout.Label("No players in game", _labelStyle);
                    return;
                }

                foreach (var player in LethalMenuMod.Players)
                {
                    if (player == null) continue;

                    bool isLocal = player == LethalMenuMod.LocalPlayer;
                    bool isDead = player.isPlayerDead;
                    float dist = 0f;

                    if (!isLocal && LethalMenuMod.LocalPlayer != null)
                    {
                        dist = Vector3.Distance(LethalMenuMod.LocalPlayer.transform.position, player.transform.position);
                    }

                    string status = isDead ? " [DEAD]" : "";
                    string localTag = isLocal ? " (You)" : "";
                    string distText = isLocal ? "" : $" - {dist:F0}m";

                    GUILayout.BeginVertical(_boxStyle);

                    // Player info row
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{player.playerUsername ?? "Unknown"}{localTag}{status}{distText}", _labelStyle, GUILayout.Width(220));

                    if (!isLocal && !isDead)
                    {
                        if (GUILayout.Button("TP To", _buttonStyle, GUILayout.Width(55)))
                        {
                            this.TeleportTo(player.transform.position);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Per-player cheats row (only for alive players)
                    if (!isDead)
                    {
                        GUILayout.BeginHorizontal();

                        // Demi-God toggle for this player
                        bool hasDemiGod = Settings.IsDemiGod(player);
                        bool newDemiGod = GUILayout.Toggle(hasDemiGod, "Demi-God", _buttonStyle, GUILayout.Width(80));
                        if (newDemiGod != hasDemiGod)
                        {
                            Settings.SetDemiGod(player, newDemiGod);
                        }

                        // Heal button
                        if (GUILayout.Button("Heal", _buttonStyle, GUILayout.Width(50)))
                        {
                            HealPlayer(player);
                        }

                        // Kill button (only for other players)
                        if (!isLocal)
                        {
                            if (GUILayout.Button("Kill", _buttonStyle, GUILayout.Width(50)))
                            {
                                KillPlayer(player);
                            }
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();
                }
            });
        }

        /// 
        /// Heal a player using negative damage exploit
        /// 
        private void HealPlayer(GameNetcodeStuff.PlayerControllerB player)
        {
            if (player == null || player.isPlayerDead) return;

            int healthNeeded = 100 - player.health;
            if (healthNeeded <= 0) return;

            player.DamagePlayerFromOtherClientServerRpc(
                -healthNeeded,
                Vector3.zero,
                (int)player.playerClientId
            );
        }

        /// 
        /// Kill a player using damage exploit
        /// 
        private void KillPlayer(GameNetcodeStuff.PlayerControllerB player)
        {
            if (player == null || player.isPlayerDead) return;

            // Deal massive damage
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            player.DamagePlayerFromOtherClientServerRpc(
                999,
                Vector3.zero,
                (int)localPlayer.playerClientId
            );
        }

        #endregion
    }
}
