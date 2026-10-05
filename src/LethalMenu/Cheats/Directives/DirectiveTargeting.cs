using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives
{
    public static class DirectiveTargeting
    {
        private const float ProximityAwareness = 5f;

        /// Never the local player, a friend, or someone not alive and in the game.
        public static bool IsValidTarget(PlayerControllerB? player) =>
            player != null && player != LethalMenuMod.LocalPlayer && player.isPlayerControlled &&
            !player.isPlayerDead && !Settings.IsFriend(player);

        /// Closest valid player the enemy can see. The currently engaged target is kept while it is still
        /// visible so the escort doesn't flip between two players every interval.
        public static PlayerControllerB? FindEscortTarget(EnemyAI enemy, IDirectiveAdapter adapter, PlayerControllerB? current)
        {
            if (IsValidTarget(current) && CanSee(enemy, adapter, current!)) return current;

            PlayerControllerB? best = null;
            float bestDistance = float.MaxValue;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!IsValidTarget(player) || !CanSee(enemy, adapter, player)) continue;
                float distance = Vector3.Distance(enemy.transform.position, player.transform.position);
                if (distance < bestDistance)
                {
                    best = player;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static bool CanSee(EnemyAI enemy, IDirectiveAdapter adapter, PlayerControllerB player)
        {
            var eye = enemy.eye != null ? enemy.eye : enemy.transform;
            Vector3 from = eye.position;
            Vector3 to = player.gameplayCamera.transform.position;
            float distance = Vector3.Distance(from, to);
            if (distance > adapter.SightRange(enemy)) return false;
            if (distance > ProximityAwareness && Vector3.Angle(eye.forward, to - from) > adapter.SightAngle(enemy)) return false;
            return !Physics.Linecast(from, to, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
        }
    }
}
