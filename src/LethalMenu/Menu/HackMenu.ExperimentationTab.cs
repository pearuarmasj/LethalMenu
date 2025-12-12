using UnityEngine;
using System.Linq;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Experimentation Tab

        private string _expLevelInput = "1";
        private string _expUnlockableInput = "0";
        private int _expTurretMode = 3;
        private string _expSpawnEnemyName = "Bracken";
        private string _expChatMsg = "";
        private string _expSysMsgInput = "";

        private void DrawExperimentationTab()
        {
            GUILayout.Label("WARNING: Private/protected calls. Host recommended. May desync.", _labelStyle);
            bool isHost = Cheats.NetworkCheats.IsHost();
            GUILayout.Label($"Status: {(isHost ? "HOST" : "CLIENT")}", _labelStyle);
            GUILayout.Space(4);

            // Player selector
            var players = Cheats.NetworkCheats.GetAllPlayers();
            var playerNames = players.Select(p => p.playerUsername ?? "Unknown").ToArray();
            if (playerNames.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target:", _labelStyle, GUILayout.Width(50));
                _selectedPlayerIndex = Mathf.Clamp(_selectedPlayerIndex, 0, playerNames.Length - 1);
                if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                    _selectedPlayerIndex = (_selectedPlayerIndex - 1 + playerNames.Length) % playerNames.Length;
                GUILayout.Label(playerNames[_selectedPlayerIndex], _labelStyle, GUILayout.Width(120));
                if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                    _selectedPlayerIndex = (_selectedPlayerIndex + 1) % playerNames.Length;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);

            // ========== CATEGORY A: LOCAL-ONLY PRIVATE ==========
            GUILayout.Label("A: Local-Only Private (No RPC, may desync):", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Detonate Landmines (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalDetonateLandmines();
            }
            if (GUILayout.Button("Turrets Berserk (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalTurretsBerserk();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Turret Mode: {_expTurretMode}", _labelStyle, GUILayout.Width(130));
            _expTurretMode = Mathf.RoundToInt(GUILayout.HorizontalSlider(_expTurretMode, 0, 3, GUILayout.Width(120)));
            if (GUILayout.Button("Apply Mode", _buttonStyle, GUILayout.Width(140)))
            {
                Cheats.NetworkCheats.ExperimentalSetTurretMode(_expTurretMode);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Turrets OFF (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalToggleTurretsLocal(false);
            }
            if (GUILayout.Button("Turrets ON (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalToggleTurretsLocal(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mines OFF (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalToggleLandminesLocal(false);
            }
            if (GUILayout.Button("Mines ON (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalToggleLandminesLocal(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mine Animation (local)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalPlayMineAnimation();
            }
            if (playerNames.Length > 0 && GUILayout.Button("Clear Target Hand (local)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalClearHeldItemLocal(players[_selectedPlayerIndex]);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ========== CATEGORY B: PRIVATE SERVERRPC ==========
            GUILayout.Label("B: Private ServerRpc (May require ownership):", _labelStyle);
            if (playerNames.Length > 0)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Private Kill RPC", _buttonStyle, GUILayout.Width(130)))
                {
                    Cheats.NetworkCheats.ExperimentalKillPlayerPrivate(players[_selectedPlayerIndex]);
                }
                if (GUILayout.Button("TP Target -> Void", _buttonStyle, GUILayout.Width(130)))
                {
                    Cheats.NetworkCheats.TeleportPlayerToVoid(players[_selectedPlayerIndex]);
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Force Drop Items (private RPC)", _buttonStyle))
                {
                    Cheats.NetworkCheats.ExperimentalDespawnHeldItem(players[_selectedPlayerIndex]);
                }
            }

            // Chat impersonation
            GUILayout.BeginHorizontal();
            _expChatMsg = GUILayout.TextField(_expChatMsg, GUILayout.Width(180));
            if (GUILayout.Button("Chat as Target (private)", _buttonStyle, GUILayout.Width(160)) && playerNames.Length > 0)
            {
                Cheats.NetworkCheats.ExperimentalChatAsPlayer(_expChatMsg, _selectedPlayerIndex);
            }
            GUILayout.EndHorizontal();

            // System message
            GUILayout.BeginHorizontal();
            _expSysMsgInput = GUILayout.TextField(_expSysMsgInput, GUILayout.Width(180));
            if (GUILayout.Button("System Msg (private)", _buttonStyle, GUILayout.Width(160)))
            {
                Cheats.NetworkCheats.ExperimentalSystemMessage(_expSysMsgInput);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ========== CATEGORY C: HOST-ONLY ==========
            GUILayout.Label("C: Game Flow (public RPCs already in main menu):", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Game (public RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalStartGame();
            }
            if (GUILayout.Button("End Game (public RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalEndGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Level ID:", _labelStyle, GUILayout.Width(70));
            _expLevelInput = GUILayout.TextField(_expLevelInput, GUILayout.Width(60));
            if (GUILayout.Button("Change Level (host)", _buttonStyle))
            {
                if (int.TryParse(_expLevelInput, out int levelId))
                {
                    Cheats.NetworkCheats.ExperimentalChangeLevel(levelId);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Unlock ID:", _labelStyle, GUILayout.Width(70));
            _expUnlockableInput = GUILayout.TextField(_expUnlockableInput, GUILayout.Width(60));
            if (GUILayout.Button("Spawn Unlockable (host)", _buttonStyle))
            {
                if (int.TryParse(_expUnlockableInput, out int unlockId))
                {
                    Cheats.NetworkCheats.ExperimentalSpawnUnlockable(unlockId);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Enemy:", _labelStyle, GUILayout.Width(50));
            _expSpawnEnemyName = GUILayout.TextField(_expSpawnEnemyName, GUILayout.Width(140));
            if (GUILayout.Button("Spawn Enemy (host)", _buttonStyle))
            {
                var local = LethalMenuMod.LocalPlayer;
                if (local != null)
                {
                    Cheats.NetworkCheats.SpawnEnemy(_expSpawnEnemyName, local.transform.position);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ========== CATEGORY D: PUBLIC RPC RequireOwnership=false ==========
            GUILayout.Label("D: Public RPC (RequireOwnership=false):", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Explode Mines (RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExplodeMinesViaRpc();
            }
            if (GUILayout.Button("Turrets Berserk (RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.TurretsBerserkViaRpc();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Turrets OFF (RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ToggleTurretsViaRpc(false);
            }
            if (GUILayout.Button("Turrets ON (RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ToggleTurretsViaRpc(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ========== CATEGORY E: RPC EXEC STAGE SPOOF ==========
            GUILayout.Label("E: RPC Exec Stage (Advanced):", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set exec_stage=Execute (self)", _buttonStyle))
            {
                if (LethalMenuMod.LocalPlayer != null)
                {
                    Cheats.NetworkCheats.ExperimentalForceRpcExecStageExecute(LethalMenuMod.LocalPlayer);
                }
            }
            if (playerNames.Length > 0 && GUILayout.Button("Set exec_stage=Execute (target)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalForceRpcExecStageExecute(players[_selectedPlayerIndex]);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset exec_stage (self)", _buttonStyle))
            {
                if (LethalMenuMod.LocalPlayer != null)
                {
                    Cheats.NetworkCheats.ExperimentalResetRpcExecStage(LethalMenuMod.LocalPlayer);
                }
            }
            if (playerNames.Length > 0 && GUILayout.Button("Reset exec_stage (target)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalResetRpcExecStage(players[_selectedPlayerIndex]);
            }
            GUILayout.EndHorizontal();
        }

        #endregion
    }
}
