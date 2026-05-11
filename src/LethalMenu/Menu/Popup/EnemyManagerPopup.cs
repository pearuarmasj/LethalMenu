using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class EnemyManagerPopup : PopupMenu
    {
        public EnemyManagerPopup() : base("Enemy Manager", 20002, 400, 400) { }

        protected override void DrawBody()
        {
            int aliveCount = 0;
            foreach (var e in LethalMenuMod.Enemies)
                if (e != null && !e.isEnemyDead) aliveCount++;

            GUILayout.Label($"Alive Enemies: {aliveCount} / {LethalMenuMod.Enemies.Count}");
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill All", GUILayout.Width(80)))
                Cheats.NetworkCheats.KillAllEnemies();
            if (GUILayout.Button("Stun All", GUILayout.Width(80)))
                Cheats.NetworkCheats.StunAllEnemies();
            if (GUILayout.Button("TP All Away", GUILayout.Width(100)))
            {
                var farPos = new Vector3(0, -500, 0);
                foreach (var enemy in LethalMenuMod.Enemies)
                    if (enemy != null && !enemy.isEnemyDead)
                        enemy.transform.position = farPos;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("--- Enemy List ---");

            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                var name = enemy.enemyType?.enemyName ?? "Unknown";
                float dist = LethalMenuMod.LocalPlayer != null
                    ? Vector3.Distance(enemy.transform.position, LethalMenuMod.LocalPlayer.transform.position)
                    : 0f;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{name} [{dist:F0}m]", GUILayout.Width(200));
                if (GUILayout.Button("Kill", GUILayout.Width(50)))
                    enemy.KillEnemyOnOwnerClient(true);
                if (GUILayout.Button("TP Away", GUILayout.Width(60)))
                    enemy.transform.position = new Vector3(0, -500, 0);
                GUILayout.EndHorizontal();
            }
        }
    }
}
