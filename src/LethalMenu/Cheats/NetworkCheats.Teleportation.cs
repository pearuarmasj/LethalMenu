using System;
using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Teleportation

        /// Teleports a player through an entrance using the game's EntranceTeleport system.
        ///
        /// For the LOCAL player: invokes entrance.TeleportPlayer() — the game's intended local-side
        /// entry point. It moves the player to the matching exit-side entrancePoint, updates
        /// isInsideFactory + item-slot flags, and broadcasts the move to other clients via
        /// TeleportPlayerServerRpc. Calling the ServerRpc directly does NOT work for the local
        /// player because the resulting ClientRpc early-returns on the caller's machine
        /// (assuming the local already moved themselves via TeleportPlayer).
        ///
        /// For a REMOTE player: invokes TeleportPlayerServerRpc on the entrance. All non-target
        /// clients see them teleported; the target player's own view does not update (no way to
        /// force a remote client to teleport themselves without their cooperation).
        ///
        /// EntranceTeleport.FindExitPoint is Harmony-patched to use includeInactive so it can
        /// resolve the matching exit door when one side of the door pair is in an inactive
        /// scene chunk.
        /// <param name="target">Player to teleport</param>
        /// <param name="toMainEntrance">If true, teleport via main entrance (id 0). If false, via any fire exit (id != 0).</param>
        public static void TeleportPlayerToEntrance(PlayerControllerB target, bool toMainEntrance = true)
        {
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToEntrance: Target is null or dead.");
                return;
            }

            var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: true);
            if (entrances == null || entrances.Length == 0)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToEntrance: No entrances found.");
                return;
            }

            // Pick the entrance with the requested id (0 = main, != 0 = fire exit).
            var candidates = toMainEntrance
                ? entrances.Where(e => e != null && e.entranceId == 0).ToArray()
                : entrances.Where(e => e != null && e.entranceId != 0).ToArray();

            if (candidates.Length == 0)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToEntrance: No matching entrance found.");
                return;
            }

            // TeleportPlayer() teleports you to the OTHER side of the door. Pick the entrance
            // on the player's CURRENT side so they end up on the opposite side.
            //   - Player outside  (isInsideFactory=false) → pick isEntranceToBuilding=true  (outside-side door) → lands inside
            //   - Player inside   (isInsideFactory=true)  → pick isEntranceToBuilding=false (inside-side door)  → lands outside
            EntranceTeleport? entrance = candidates.FirstOrDefault(e => e.isEntranceToBuilding != target.isInsideFactory)
                ?? candidates.FirstOrDefault();

            if (entrance == null)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToEntrance: No suitable entrance side found.");
                return;
            }

            bool isLocal = target == LethalMenuMod.LocalPlayer;
            try
            {
                if (isLocal)
                {
                    // Game's intended local-player flow: handles local position, isInsideFactory,
                    // item-slot flags, audio, and broadcasts the move to other clients.
                    entrance.TeleportPlayer();
                }
                else
                {
                    // Remote target: server broadcasts to all clients, but the target's own view
                    // will not update because the ClientRpc skips the caller's local player.
                    entrance.TeleportPlayerServerRpc((int)target.playerClientId);
                }
                Debug.Log($"[NetworkCheats] Teleported {target.playerUsername} via {(toMainEntrance ? "main entrance" : "fire exit")}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkCheats] TeleportPlayerToEntrance failed: {e}");
            }
        }

        /// Teleports a player to the ship using direct position teleport.
        /// Only works reliably for local player.
        public static void TeleportToShip(PlayerControllerB? target = null)
        {
            target ??= LethalMenuMod.LocalPlayer;
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportToShip: Target is null or dead.");
                return;
            }

            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null || startOfRound.playerSpawnPositions == null || startOfRound.playerSpawnPositions.Length == 0)
            {
                Debug.Log("[NetworkCheats] TeleportToShip: Ship spawn positions not found.");
                return;
            }

            Vector3 shipPos = startOfRound.playerSpawnPositions[0].position;

            // Only local player can be directly teleported
            if (target == LethalMenuMod.LocalPlayer)
            {
                target.TeleportPlayer(shipPos);
                target.isInElevator = false;
                target.isInHangarShipRoom = true;
                target.isInsideFactory = false;
                Debug.Log($"[NetworkCheats] Teleported {target.playerUsername} to ship.");
            }
            else
            {
                Debug.Log("[NetworkCheats] TeleportToShip: Can only teleport local player directly. Use TeleportPlayerViaShipTeleporter for other players.");
            }
        }

        /// Teleports ANY player to the ship using the ship teleporter item.
        /// Requires the ship to have a normal teleporter (not inverse).
        /// Works by switching radar target and pressing the teleport button.
        public static void TeleportPlayerViaShipTeleporter(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerViaShipTeleporter: Target is null or dead.");
                return;
            }

            // Find the ship teleporter (not inverse)
            var teleporters = UnityEngine.Object.FindObjectsOfType<ShipTeleporter>(includeInactive: true);
            var normalTeleporter = teleporters.FirstOrDefault(t => t != null && !t.isInverseTeleporter);

            if (normalTeleporter == null)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerViaShipTeleporter: No ship teleporter found. Buy one from the store.");
                return;
            }

            // Check cooldown
            if (normalTeleporter.cooldownTime > 0f)
            {
                Debug.Log($"[NetworkCheats] TeleportPlayerViaShipTeleporter: Teleporter on cooldown ({normalTeleporter.cooldownTime:F1}s remaining).");
                return;
            }

            var startOfRound = StartOfRound.Instance;
            if (startOfRound?.mapScreen == null)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerViaShipTeleporter: Map screen not found.");
                return;
            }

            // Find the player's radar target index
            int targetIndex = -1;
            for (int i = 0; i < startOfRound.mapScreen.radarTargets.Count; i++)
            {
                if (startOfRound.mapScreen.radarTargets[i].transform == target.transform)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                Debug.Log($"[NetworkCheats] TeleportPlayerViaShipTeleporter: {target.playerUsername} not found in radar targets.");
                return;
            }

            // Switch radar target to the player and use teleporter
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(TeleportPlayerCoroutine(target, normalTeleporter, targetIndex));
            }
        }

        /// Coroutine that switches radar target and teleports.
        private static IEnumerator TeleportPlayerCoroutine(PlayerControllerB target, ShipTeleporter teleporter, int targetIndex)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound?.mapScreen == null) yield break;

            // Switch radar target
            startOfRound.mapScreen.SwitchRadarTargetAndSync(targetIndex);
            Debug.Log($"[NetworkCheats] Switched radar target to {target.playerUsername}.");

            // Wait a frame for the radar to update
            yield return null;
            yield return null;

            // Press teleport button
            teleporter.PressTeleportButtonServerRpc();
            Debug.Log($"[NetworkCheats] Teleported {target.playerUsername} to ship via teleporter.");
        }

        /// Teleports local player to a specific position.
        public static void TeleportToPosition(Vector3 position)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null || localPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportToPosition: Local player is null or dead.");
                return;
            }

            localPlayer.TeleportPlayer(position);
            Debug.Log($"[NetworkCheats] Teleported to position {position}.");
        }

        #endregion
    }
}
