using UnityEngine;
using LethalMenu.Mixins;

namespace LethalMenu.Menu.Popup
{
    public class EnemyManagerPopup : PopupMenu, IEnemyPrompter
    {
        private int _selectedEnemyIndex;
        private string[]? _cachedEnemyNames;
        private int _selectedMimicPlayerIndex;
        private int _spawnCount = 1;
        private int _spawnSideMode;

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
                this.KillAllEnemies();
            if (GUILayout.Button("Stun All", GUILayout.Width(80)))
                this.StunAllEnemies();
            if (GUILayout.Button("TP All Away", GUILayout.Width(100)))
                this.TeleportAllEnemiesAway();
            GUILayout.EndHorizontal();

            DrawSpawner();

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

        private void DrawSpawner()
        {
            GUILayout.Space(10);
            GUILayout.Label("--- Spawn Enemy ---");

            if (LethalMenuMod.LocalPlayer?.IsHost != true)
            {
                GUILayout.Label("Host only.");
                return;
            }

            if (_cachedEnemyNames == null || _cachedEnemyNames.Length == 0)
                _cachedEnemyNames = Cheats.NetworkCheats.GetAvailableEnemyNames();

            if (_cachedEnemyNames.Length == 0)
            {
                GUILayout.Label("No enemies available (land on a moon)");
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(30)))
                _selectedEnemyIndex = (_selectedEnemyIndex - 1 + _cachedEnemyNames.Length) % _cachedEnemyNames.Length;
            _selectedEnemyIndex = Mathf.Clamp(_selectedEnemyIndex, 0, _cachedEnemyNames.Length - 1);
            GUILayout.Label(_cachedEnemyNames[_selectedEnemyIndex], GUILayout.Width(150));
            if (GUILayout.Button(">", GUILayout.Width(30)))
                _selectedEnemyIndex = (_selectedEnemyIndex + 1) % _cachedEnemyNames.Length;
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                _cachedEnemyNames = Cheats.NetworkCheats.GetAvailableEnemyNames();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Count: {_spawnCount}", GUILayout.Width(70));
            _spawnCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(_spawnCount, 1, 10, GUILayout.Width(130)));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Side:", GUILayout.Width(45));
            if (GUILayout.Button(_spawnSideMode == 0 ? "[Listed]" : "Listed", GUILayout.Width(70)))
                _spawnSideMode = 0;
            if (GUILayout.Button(_spawnSideMode == 1 ? "[Inside]" : "Inside", GUILayout.Width(70)))
                _spawnSideMode = 1;
            if (GUILayout.Button(_spawnSideMode == 2 ? "[Outside]" : "Outside", GUILayout.Width(80)))
                _spawnSideMode = 2;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn At Me"))
            {
                var pos = LethalMenuMod.LocalPlayer?.transform.position ?? Vector3.zero;
                SpawnSelectedEnemy(pos);
            }
            if (GUILayout.Button("Spawn At Camera"))
            {
                var pos = Camera.main?.transform.position ?? Vector3.zero;
                SpawnSelectedEnemy(pos);
            }
            GUILayout.EndHorizontal();

            var players = Cheats.NetworkCheats.GetAllPlayers();
            if (players.Length == 0) return;

            _selectedMimicPlayerIndex = Mathf.Clamp(_selectedMimicPlayerIndex, 0, players.Length - 1);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(30)))
                _selectedMimicPlayerIndex = (_selectedMimicPlayerIndex - 1 + players.Length) % players.Length;
            GUILayout.Label(players[_selectedMimicPlayerIndex].playerUsername ?? "Unknown", GUILayout.Width(150));
            if (GUILayout.Button(">", GUILayout.Width(30)))
                _selectedMimicPlayerIndex = (_selectedMimicPlayerIndex + 1) % players.Length;
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Spawn Mimic of Selected"))
                Cheats.NetworkCheats.SpawnMimic(players[_selectedMimicPlayerIndex]);
        }

        private void SpawnSelectedEnemy(Vector3 position)
        {
            if (_cachedEnemyNames == null || _cachedEnemyNames.Length == 0)
                return;

            string enemyName = _cachedEnemyNames[_selectedEnemyIndex];
            bool listedOutside = enemyName.StartsWith("[O]");
            bool isOutside = _spawnSideMode == 0 ? listedOutside : _spawnSideMode == 2;
            int count = Mathf.Clamp(_spawnCount, 1, 10);

            for (int i = 0; i < count; i++)
            {
                var offset = new Vector3((i % 5) * 1.5f, 0f, (i / 5) * 1.5f);
                Cheats.NetworkCheats.SpawnEnemy(enemyName, position + offset, isOutside);
            }
        }
    }
}
