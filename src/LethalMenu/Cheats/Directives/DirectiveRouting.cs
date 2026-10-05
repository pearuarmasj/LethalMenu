using System.Collections.Generic;
using LethalMenu.Cheats.EnemyControl;
using UnityEngine;

namespace LethalMenu.Cheats.Directives
{
    /// Enemies can't path between the facility and the surface. When the goal is on the other side the
    /// enemy walks to the nearest entrance on its own side and is warped to the matching door
    /// (EntranceTeleport pair: same entranceId, opposite isEntranceToBuilding). Masked crosses with its
    /// own synced TeleportMaskedEnemyAndSync. If no entrance is reachable for 5 s it warps to the goal.
    public static class DirectiveRouting
    {
        private const float DoorReachDistance = 2.5f;
        private const float StuckTimeout = 5f;

        private static readonly Dictionary<EnemyAI, float> UnreachableSince = new();

        /// True while the enemy is being routed to the other side (the caller must not engage/follow).
        public static bool Route(EnemyAI enemy, IDirectiveAdapter adapter, Vector3 goal, bool goalOutside)
        {
            if (enemy.isOutside == goalOutside || !adapter.CanUseEntrances(enemy))
            {
                UnreachableSince.Remove(enemy);
                return false;
            }

            var door = NearestDoorOnSide(enemy.transform.position, enemy.isOutside);
            var exit = door != null ? MatchingDoor(door) : null;
            if (door == null || exit == null)
            {
                Cross(enemy, goal, goalOutside);
                return true;
            }

            Vector3 doorPoint = door.entrancePoint.position;
            if (Vector3.Distance(enemy.transform.position, doorPoint) < DoorReachDistance)
            {
                Cross(enemy, exit.entrancePoint.position, goalOutside);
                UnreachableSince.Remove(enemy);
                return true;
            }

            if (enemy.SetDestinationToPosition(doorPoint, checkForPath: true))
            {
                UnreachableSince.Remove(enemy);
            }
            else if (!UnreachableSince.TryGetValue(enemy, out float since))
            {
                UnreachableSince[enemy] = Time.time;
            }
            else if (Time.time - since > StuckTimeout)
            {
                Cross(enemy, goal, goalOutside);
                UnreachableSince.Remove(enemy);
            }
            return true;
        }

        public static void Forget(EnemyAI enemy) => UnreachableSince.Remove(enemy);

        /// Outside doors are the ones with isEntranceToBuilding = true.
        private static EntranceTeleport? NearestDoorOnSide(Vector3 from, bool outside)
        {
            EntranceTeleport? best = null;
            float bestDistance = float.MaxValue;
            foreach (var entrance in LethalMenuMod.Entrances)
            {
                if (entrance == null || entrance.entrancePoint == null || entrance.isEntranceToBuilding != outside) continue;
                float distance = Vector3.Distance(from, entrance.entrancePoint.position);
                if (distance < bestDistance)
                {
                    best = entrance;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static EntranceTeleport? MatchingDoor(EntranceTeleport door)
        {
            foreach (var entrance in LethalMenuMod.Entrances)
            {
                if (entrance != null && entrance.entrancePoint != null && entrance.entranceId == door.entranceId &&
                    entrance.isEntranceToBuilding != door.isEntranceToBuilding)
                    return entrance;
            }
            return null;
        }

        private static void Cross(EnemyAI enemy, Vector3 position, bool outside)
        {
            if (enemy is MaskedPlayerEnemy masked)
            {
                masked.TeleportMaskedEnemyAndSync(position, outside);
                return;
            }

            position = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 3f, enemy.agentMask);
            enemy.serverPosition = position;
            if (enemy.agent != null && enemy.agent.enabled)
                enemy.agent.Warp(position);
            enemy.transform.position = position;
            enemy.SetOutsideStatus(outside);
            enemy.SyncPositionToClients();
        }
    }
}
