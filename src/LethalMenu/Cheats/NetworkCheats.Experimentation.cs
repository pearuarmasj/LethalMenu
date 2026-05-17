using System;
using GameNetcodeStuff;
using LethalMenu.Util;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Experimentation (Reflection / Private Calls)

        // ============================================================
        // CATEGORY A: LOCAL-ONLY PRIVATE METHODS (NO RPC - WORKS LOCALLY)
        // These are actual private methods that execute locally without network validation.
        // May cause desync between clients.
        // ============================================================

        /// Detonate all landmines via private TriggerMineOnLocalClientByExiting (LOCAL ONLY).
        public static void ExperimentalDetonateLandmines()
        {
            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>(includeInactive: true);
            if (mines == null || mines.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in mines)
            {
                if (mine.hasExploded) continue;
                try
                {
                    // private void TriggerMineOnLocalClientByExiting() - triggers locally
                    ReflectionHelper.InvokePrivate(mine, "TriggerMineOnLocalClientByExiting");
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Mine detonation failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Triggered {count} landmines (local).");
        }

        /// Play the landmine detonation animation locally without network RPC.
        public static void ExperimentalPlayMineAnimation()
        {
            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>(includeInactive: true);
            if (mines == null || mines.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in mines)
            {
                try
                {
                    mine.SetOffMineAnimation();
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Mine animation failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Played animation on {count} mines (local).");
        }

        /// Set turret mode via private SwitchTurretMode (LOCAL ONLY).
        /// Modes: 0=Detection, 1=Charging, 2=Firing, 3=Berserk
        public static void ExperimentalSetTurretMode(int mode)
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>(includeInactive: true);
            if (turrets == null || turrets.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No turrets found.");
                return;
            }

            string[] modeNames = { "Detection", "Charging", "Firing", "Berserk" };
            string modeName = mode >= 0 && mode < modeNames.Length ? modeNames[mode] : "Unknown";

            int count = 0;
            foreach (var turret in turrets)
            {
                try
                {
                    // private void SwitchTurretMode(int mode) - local state change
                    ReflectionHelper.InvokePrivate(turret, "SwitchTurretMode", mode);
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Turret mode failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Set {count} turrets to {modeName} (local).");
        }

        /// Force all turrets into mode 3 (berserk) via private SwitchTurretMode (LOCAL ONLY).
        public static void ExperimentalTurretsBerserk()
        {
            ExperimentalSetTurretMode(3);
        }

        /// Toggle turret enabled state locally via private ToggleTurretEnabledLocalClient (LOCAL ONLY).
        public static void ExperimentalToggleTurretsLocal(bool enabled)
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>(includeInactive: true);
            if (turrets == null || turrets.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No turrets found.");
                return;
            }

            int count = 0;
            foreach (var turret in turrets)
            {
                try
                {
                    // private void ToggleTurretEnabledLocalClient(bool enabled)
                    ReflectionHelper.InvokePrivate(turret, "ToggleTurretEnabledLocalClient", enabled);
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Turret toggle failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"{(enabled ? "Enabled" : "Disabled")} {count} turrets (local).");
        }

        /// Toggle landmine enabled state locally (LOCAL ONLY).
        public static void ExperimentalToggleLandminesLocal(bool enabled)
        {
            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>(includeInactive: true);
            if (mines == null || mines.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in mines)
            {
                try
                {
                    // public void ToggleMineEnabledLocalClient(bool enabled)
                    ReflectionHelper.InvokePrivate(mine, "ToggleMineEnabledLocalClient", enabled);
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Mine toggle failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"{(enabled ? "Enabled" : "Disabled")} {count} mines (local).");
        }

        /// Clear a player's held item locally via DespawnHeldObjectOnClient (LOCAL ONLY).
        public static void ExperimentalClearHeldItemLocal(PlayerControllerB target)
        {
            if (target == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No target player.");
                return;
            }

            try
            {
                ReflectionHelper.InvokePrivate(target, "DespawnHeldObjectOnClient");
                HUDManager.Instance?.DisplayTip("Experiment", $"Cleared {target.playerUsername}'s held item (local).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Local despawn failed: {ex.Message}");
            }
        }

        // ============================================================
        // CATEGORY B: PRIVATE SERVERRPC METHODS (MAY REQUIRE OWNERSHIP)
        // These are actually private ServerRpc methods. Calling them via reflection
        // sends the RPC, but server may reject based on ownership validation.
        // ============================================================

        /// Call the private KillPlayerServerRpc via reflection (PRIVATE RPC - may fail).
        public static void ExperimentalKillPlayerPrivate(PlayerControllerB target)
        {
            if (target == null) return;

            try
            {
                // private void KillPlayerServerRpc(int playerId, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation, Vector3 positionOffset)
                ReflectionHelper.InvokePrivate(target,
                    "KillPlayerServerRpc",
                    (int)target.playerClientId,
                    true,
                    Vector3.zero,
                    (int)CauseOfDeath.Unknown,
                    0,
                    Vector3.zero);

                Debug.Log($"[NetworkCheats] KillPlayerServerRpc invoked on {target.playerUsername}");
                HUDManager.Instance?.DisplayTip("Experiment", $"Kill RPC sent for {target.playerUsername}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Kill RPC failed: {ex.Message}");
            }
        }

        /// Send chat message as any player via private AddPlayerChatMessageServerRpc (PRIVATE RPC).
        public static void ExperimentalChatAsPlayer(string message, int playerId)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.LogWarning("[NetworkCheats] HUDManager not found.");
                return;
            }

            try
            {
                // private void AddPlayerChatMessageServerRpc(string chatMessage, int playerId)
                ReflectionHelper.InvokePrivate(hud, "AddPlayerChatMessageServerRpc", message, playerId);
                Debug.Log($"[NetworkCheats] Chat as player {playerId}: {message}");
                HUDManager.Instance?.DisplayTip("Experiment", $"Chat RPC sent as player {playerId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Chat RPC failed: {ex.Message}");
            }
        }

        /// Send system message via private AddTextMessageServerRpc (PRIVATE RPC).
        public static void ExperimentalSystemMessage(string message)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.LogWarning("[NetworkCheats] HUDManager not found.");
                return;
            }

            try
            {
                // private void AddTextMessageServerRpc(string chatMessage)
                ReflectionHelper.InvokePrivate(hud, "AddTextMessageServerRpc", message);
                Debug.Log($"[NetworkCheats] System message: {message}");
                HUDManager.Instance?.DisplayTip("Experiment", "System message RPC sent");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] System message RPC failed: {ex.Message}");
            }
        }

        /// Despawn the target's held item via private DespawnHeldObjectServerRpc (PRIVATE RPC).
        public static void ExperimentalDespawnHeldItem(PlayerControllerB target)
        {
            if (target == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No target player.");
                return;
            }

            try
            {
                // private void DespawnHeldObjectServerRpc() - no parameters
                ReflectionHelper.InvokePrivate(target, "DespawnHeldObjectServerRpc");
                HUDManager.Instance?.DisplayTip("Experiment", $"Despawn RPC sent for {target.playerUsername}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Despawn RPC failed: {ex.Message}");
            }
        }

        /// Force player position update via private UpdatePlayerPositionServerRpc (PRIVATE RPC).
        public static void ExperimentalForcePosition(PlayerControllerB target, Vector3 position)
        {
            if (target == null) return;

            try
            {
                // private void UpdatePlayerPositionServerRpc(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
                ReflectionHelper.InvokePrivate(target, "UpdatePlayerPositionServerRpc",
                    position, false, false, false, true);
                HUDManager.Instance?.DisplayTip("Experiment", $"Position RPC sent for {target.playerUsername}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Position RPC failed: {ex.Message}");
            }
        }

        // ============================================================
        // CATEGORY C: GAME FLOW (PUBLIC RPC; works today in main menu)
        // These are the same public ServerRpc calls already used in the main menu.
        // ============================================================

        /// Force StartGame (HOST ONLY - has IsServer check).
        public static void ExperimentalStartGame()
        {
            var round = StartOfRound.Instance;
            if (round == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "Not in game.");
                return;
            }

            // Use the same public RPC already available elsewhere.
            round.StartGameServerRpc();
            HUDManager.Instance?.DisplayTip("Experiment", "StartGameServerRpc invoked (public RPC).");
        }

        /// Force EndGame (HOST ONLY - has IsServer check).
        public static void ExperimentalEndGame()
        {
            var round = StartOfRound.Instance;
            var local = LethalMenuMod.LocalPlayer;
            if (round == null || local == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "Not in game.");
                return;
            }

            round.EndGameServerRpc((int)local.playerClientId);
            HUDManager.Instance?.DisplayTip("Experiment", "EndGameServerRpc invoked (public RPC).");
        }

        /// Force moon change (HOST ONLY).
        public static void ExperimentalChangeLevel(int levelId)
        {
            var round = StartOfRound.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (round == null || terminal == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "Not in game / no terminal.");
                return;
            }

            try
            {
                ReflectionHelper.InvokePrivate(round, "ChangeLevelServerRpc", levelId, terminal.groupCredits);
                HUDManager.Instance?.DisplayTip("Experiment", $"ChangeLevel to {levelId}." + (IsHost() ? "" : " (Not host)"));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] ChangeLevel failed: {ex.Message}");
            }
        }

        /// Spawn a ship unlockable (HOST ONLY).
        public static void ExperimentalSpawnUnlockable(int unlockableId)
        {
            var round = StartOfRound.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (round == null || terminal == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "Not in game / no terminal.");
                return;
            }

            try
            {
                ReflectionHelper.InvokePrivate(round, "BuyShipUnlockableServerRpc", unlockableId, terminal.groupCredits);
                HUDManager.Instance?.DisplayTip("Experiment", $"Unlock {unlockableId}." + (IsHost() ? "" : " (Not host)"));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Unlock failed: {ex.Message}");
            }
        }

        // ============================================================
        // CATEGORY D: PUBLIC RPC WITH RequireOwnership = false (ANY CLIENT)
        // These are public ServerRpc methods that explicitly allow any client to call.
        // Note: TeleportPlayerToVoid() already defined elsewhere in this class.
        // ============================================================

        /// Explode all landmines via PUBLIC ExplodeMineServerRpc (RequireOwnership=false).
        public static void ExplodeMinesViaRpc()
        {
            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>(includeInactive: true);
            if (mines == null || mines.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in mines)
            {
                if (mine.hasExploded) continue;
                try
                {
                    mine.ExplodeMineServerRpc(); // Public, RequireOwnership=false
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Mine RPC explode failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Exploded {count} mines via RPC.");
        }

        /// Force turrets berserk via PUBLIC EnterBerserkModeServerRpc (RequireOwnership=false).
        public static void TurretsBerserkViaRpc()
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>(includeInactive: true);
            if (turrets == null || turrets.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No turrets found.");
                return;
            }

            int count = 0;
            var local = LethalMenuMod.LocalPlayer;
            int playerId = local != null ? (int)local.playerClientId : 0;

            foreach (var turret in turrets)
            {
                try
                {
                    turret.EnterBerserkModeServerRpc(playerId); // Public, RequireOwnership=false
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Turret berserk RPC failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Berserked {count} turrets via RPC.");
        }

        /// Toggle all turrets via PUBLIC ToggleTurretServerRpc (RequireOwnership=false).
        public static void ToggleTurretsViaRpc(bool enabled)
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>(includeInactive: true);
            if (turrets == null || turrets.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No turrets found.");
                return;
            }

            int count = 0;
            foreach (var turret in turrets)
            {
                try
                {
                    turret.ToggleTurretServerRpc(enabled); // Public, RequireOwnership=false
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Turret toggle RPC failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"{(enabled ? "Enabled" : "Disabled")} {count} turrets via RPC.");
        }

        // ============================================================
        // CATEGORY E: RPC EXEC STAGE SPOOF (ADVANCED)
        // Manipulates __rpc_exec_stage on NetworkBehaviours to mimic RPC execution.
        // ============================================================

        /// Set __rpc_exec_stage to Execute on any NetworkBehaviour.
        public static void ExperimentalForceRpcExecStageExecute(NetworkBehaviour behaviour)
        {
            if (behaviour == null)
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "No target NetworkBehaviour.");
                return;
            }

            if (ReflectionHelper.ForceRpcExecStageExecute(behaviour))
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "exec_stage set to Execute.");
                Debug.Log($"[NetworkCheats] exec_stage set to Execute on {behaviour.GetType().Name}");
            }
            else
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "Failed to set exec_stage.");
            }
        }

        /// Reset __rpc_exec_stage to None/default on any NetworkBehaviour.
        public static void ExperimentalResetRpcExecStage(NetworkBehaviour behaviour)
        {
            if (behaviour == null)
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "No target NetworkBehaviour.");
                return;
            }

            if (ReflectionHelper.ResetRpcExecStage(behaviour))
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "exec_stage reset.");
                Debug.Log($"[NetworkCheats] exec_stage reset on {behaviour.GetType().Name}");
            }
            else
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "Failed to reset exec_stage.");
            }
        }

        #endregion
    }
}
