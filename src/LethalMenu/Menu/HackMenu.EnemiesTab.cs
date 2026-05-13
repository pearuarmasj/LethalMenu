using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Enemies Tab

        private void DrawEnemiesTab()
        {
            if (GUILayout.Button("Open Enemy Manager", _buttonStyle, GUILayout.Height(28)))
                _enemyManager.IsOpen = !_enemyManager.IsOpen;
            GUILayout.Space(5);

            DrawSection("Enemy Protection", () =>
            {
                DrawHackToggle(Hack.Untargetable, "Untargetable", "Enemies ignore you");
                DrawHackToggle(Hack.GhostMode, "Ghost Mode", "Enemies can't target you (EnemyAI)");
                DrawHackToggle(Hack.AntiGhostGirl, "Anti-Ghost Girl", "Ghost Girl won't haunt you");
            });

            DrawSection("Enemy Control", () =>
            {
                bool isPossessing = Cheats.EnemyControlCheat.IsControlling;
                var controlledEnemy = Cheats.EnemyControlCheat.GetControlledEnemy();

                if (isPossessing && controlledEnemy != null)
                {
                    string enemyName = controlledEnemy.enemyType?.enemyName ?? "Unknown";
                    string aiStatus = Cheats.EnemyControlCheat.IsAIControlled ? "AI" : "Manual";
                    string noClipStatus = Cheats.EnemyControlCheat.NoClipEnabled ? " | NoClip" : "";

                    GUILayout.BeginVertical(_boxStyle);
                    GUILayout.Label($"Possessing: {enemyName}", _labelStyle);
                    GUILayout.Label($"Mode: {aiStatus}{noClipStatus}", _labelStyle);

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Release (Z)", _buttonStyle, GUILayout.Width(90)))
                    {
                        Cheats.EnemyControlCheat.StopControl();
                    }
                    if (GUILayout.Button("Kill (Del)", _buttonStyle, GUILayout.Width(90)))
                    {
                        controlledEnemy.KillEnemyOnOwnerClient(false);
                        Cheats.EnemyControlCheat.StopControl();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
                else
                {
                    DrawHackToggle(Hack.EnemyControl, "Enemy Control", "RMB to possess enemy you're looking at");
                    if (Hack.EnemyControl.IsEnabled())
                    {
                        GUILayout.Label("  Close menu, look at enemy, RMB to possess", _labelStyle);
                    }
                }

                GUILayout.Space(5);
                DrawHackToggle(Hack.KillClick, "Kill Click", "LMB kills enemies (close menu first)");
                DrawHackToggle(Hack.StunClick, "Stun Click", "MMB stuns enemies/turrets/mines");

                if (Hack.EnemyControl.IsEnabled() && !isPossessing)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Controls while possessing:", _labelStyle);
                    GUILayout.Label("  WASD=Move | Shift=Sprint | Space=Jump", _labelStyle);
                    GUILayout.Label("  LMB=Primary | RMB=Secondary | E=Door", _labelStyle);
                    GUILayout.Label("  N=NoClip | F9=AI Toggle | Z=Release | Del=Kill", _labelStyle);
                }
            });
        }

        #endregion
    }
}
