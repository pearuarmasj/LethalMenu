using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Nutcracker. Vanilla attack = behaviour state 2 entered through SeeMovingThreatServerRpc(playerId, true)
    /// (RequireOwnership = false; the ClientRpc does SwitchTargetToPlayer + SwitchToBehaviourStateOnLocalClient(2)),
    /// retargeted with SwitchTargetServerRpc. The shot is AimGunServerRpc(enemyPos) -> AimGunClientRpc ->
    /// AimGun coroutine -> FireGunServerRpc (owner) -> FireGunClientRpc -> gun.ShootGun; vanilla only aims from
    /// Update on the victim's own client, so Engage drives the same aim call from the owner when the target is
    /// in the gun's cone. Kicks are the victim's OnCollideWithPlayer -> LegKickPlayerServerRpc.
    /// Update's state 2 tail retargets lastPlayerSeenMoving to the local player (and would aim at them), so
    /// AfterUpdate puts the target back and cancels any aim begun against the local player; with no target it
    /// pulls the inspection / attack states back to patrol.
    public sealed class NutcrackerDirectiveAdapter : DirectiveAdapter<NutcrackerEnemyAI>
    {
        private const int PatrolState = 0;
        private const int AttackState = 2;
        private const float AimConeDegrees = 30f;
        private const float FireRange = 25f;
        private const float AimSuppressSeconds = 0.5f;

        private static readonly Dictionary<NutcrackerEnemyAI, float> SuppressAimUntil = new();

        public override void Engage(NutcrackerEnemyAI enemy, PlayerControllerB target)
        {
            if (enemy.gun == null) return;
            int id = (int)target.playerClientId;

            if (enemy.patrol.inProgress) enemy.StopSearch(enemy.patrol);

            if (enemy.currentBehaviourStateIndex != AttackState)
                enemy.SeeMovingThreatServerRpc(id, true);
            else if (enemy.lastPlayerSeenMoving != id)
                enemy.SwitchTargetServerRpc(id);

            enemy.lostPlayerInChase = false;
            enemy.reachedStrafePosition = false;
            enemy.agent.stoppingDistance = 1f;
            enemy.SetDestinationToPosition(target.transform.position);

            Vector3 aimPoint = target.gameplayCamera.transform.position;
            if (!enemy.CheckLineOfSightForPosition(aimPoint, 70f, (int)FireRange, 1f)) return;
            enemy.lastSeenPlayerPos = target.transform.position;

            if (enemy.currentBehaviourStateIndex != AttackState) return;
            if (SuppressAimUntil.TryGetValue(enemy, out float until) && Time.time < until) return;
            if (enemy.timeSinceFiringGun > 0.75f && !enemy.reloadingGun && !enemy.aimingGun &&
                enemy.timeSinceHittingPlayer > 1f &&
                Vector3.Angle(enemy.gun.shotgunRayPoint.forward, aimPoint - enemy.gun.shotgunRayPoint.position) < AimConeDegrees)
            {
                enemy.timeSinceFiringGun = 0f;
                enemy.agent.speed = 0f;
                enemy.AimGunServerRpc(enemy.transform.position);
            }
        }

        public override void AfterUpdate(NutcrackerEnemyAI enemy, PlayerControllerB? target)
        {
            if (target == null)
            {
                if (enemy.currentBehaviourStateIndex != PatrolState) Disengage(enemy);
                return;
            }

            int id = (int)target.playerClientId;
            if (enemy.currentBehaviourStateIndex == AttackState && enemy.lastPlayerSeenMoving != id)
            {
                // Update switched the target (usually to the local player). Restore it, resync the other
                // clients, and cancel an aim that may already be in flight against the wrong player.
                enemy.lastPlayerSeenMoving = id;
                enemy.SwitchTargetServerRpc(id);
                SuppressAimUntil[enemy] = Time.time + AimSuppressSeconds;
            }
            if (SuppressAimUntil.TryGetValue(enemy, out float until) && Time.time < until && enemy.aimingGun)
                enemy.StopAimingGun();

            enemy.lostPlayerInChase = false;
            enemy.timeSinceSeeingTarget = 0f;
        }

        public override void Disengage(NutcrackerEnemyAI enemy)
        {
            enemy.StopInspection();
            if (enemy.currentBehaviourStateIndex != PatrolState) enemy.SwitchToBehaviourState(PatrolState);
            enemy.lastPlayerSeenMoving = -1;
            enemy.lostPlayerInChase = false;
        }

        public override void Follow(NutcrackerEnemyAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != PatrolState) Disengage(enemy);
            if (enemy.patrol.inProgress) enemy.StopSearch(enemy.patrol);
            enemy.agent.stoppingDistance = 0.02f;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(NutcrackerEnemyAI enemy)
        {
            SuppressAimUntil.Remove(enemy);
            Disengage(enemy);
        }

        public override float SightRange(NutcrackerEnemyAI enemy) => 30f;
        public override float SightAngle(NutcrackerEnemyAI enemy) => 70f;
    }
}
