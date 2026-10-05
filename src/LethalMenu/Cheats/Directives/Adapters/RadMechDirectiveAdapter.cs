using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Old Bird. Vanilla attack = threat chase, behaviour state 1, entered by CheckSightForThreat:
    /// SetTargetedThreat(IVisibleThreat) + focusedThreatTransform + SwitchToBehaviourStateOnLocalClient(1) +
    /// SetTargetToThreatClientRpc. The target player is its own IVisibleThreat. Engage then runs the state 1
    /// half of DoAIInterval (MoveTowardsThreat: line of sight, alert timer, destination at the threat's last
    /// seen position; alert -> SetChargingForward / SetAimingGun). The gun (AimAndShootCycle -> StartShootGun ->
    /// missile explosion) and the grab (AttemptGrabIfClose -> StartGrabAttempt -> victim's OnCollideWithPlayer ->
    /// GrabPlayerServerRpc -> torch) run in RadMechAI.Update and are gated on IsServer, as are the footstep
    /// cycle that drives agent.speed and the charge/aim decisions here: a non-host director only moves the
    /// state and destination, the full attack needs the host.
    /// AfterUpdate keeps lostCreatureInChase clear (the MoveTowardsThreat mirror in Engage reads it) and, while
    /// not engaged, stops the state 0 search Update restarts every frame (its FinishedCurrentSearchRoutine
    /// would otherwise send the mech into flight, state 2).
    public sealed class RadMechDirectiveAdapter : DirectiveAdapter<RadMechAI>
    {
        private const int RoamState = 0;
        private const int ChaseState = 1;
        private const int FlightState = 2;

        public override void Engage(RadMechAI enemy, PlayerControllerB target)
        {
            if (enemy.inSpecialAnimation || enemy.attemptingGrab || enemy.currentBehaviourStateIndex == FlightState) return;

            var threat = (IVisibleThreat)target;
            Vector3 seenAt = target.transform.position + Vector3.up * 0.5f;

            if (enemy.currentBehaviourStateIndex != ChaseState || enemy.targetedThreat.threatScript != threat)
            {
                enemy.SetTargetedThreat(threat, seenAt, Vector3.Distance(enemy.eye.position, target.transform.position));
                enemy.focusedThreatTransform = threat.GetThreatTransform();
                enemy.SwitchToBehaviourState(ChaseState);
                if (enemy.IsServer)
                    enemy.SetTargetToThreatClientRpc(target.NetworkObject, seenAt);
            }

            // RadMechAI.MoveTowardsThreat, chase half.
            enemy.lostCreatureInChase = false;
            enemy.lostCreatureInChaseDebounce = false;
            enemy.losTimer = 0f;
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);

            enemy.targetedThreat.lastSeenPosition = seenAt;
            enemy.targetedThreat.timeLastSeen = Time.realtimeSinceStartup;
            enemy.hasLOS = enemy.CheckLineOfSightForPosition(target.transform.position);
            enemy.alertTimer += enemy.AIIntervalTime * Mathf.Max(threat.GetVisibility(), 0.3f);
            if (!enemy.SetDestinationToPosition(seenAt, checkForPath: true))
                enemy.SetDestinationToPosition(target.transform.position);

            float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
            if (!enemy.isAlerted)
            {
                if (enemy.alertTimer > 1.4f || (enemy.hasLOS && distance < 8f) || distance < 4f)
                    enemy.SetMechAlertedToThreat();
            }
            else if (enemy.IsServer && !enemy.aimingGun && enemy.shootCooldown <= 1f && enemy.hasLOS)
            {
                // DoAIInterval case 1, alerted branch.
                if (distance > 38f) enemy.SetChargingForward(true);
                else if (!enemy.chargingForward) enemy.SetAimingGun(true);
                else if (distance < 25f) enemy.SetAimingGun(true);
            }
        }

        public override void AfterUpdate(RadMechAI enemy, PlayerControllerB? target)
        {
            if (target == null)
            {
                if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
                return;
            }
            enemy.lostCreatureInChase = false;
        }

        public override void Disengage(RadMechAI enemy)
        {
            if (enemy.currentBehaviourStateIndex == ChaseState) enemy.SwitchToBehaviourState(RoamState);
            enemy.lostCreatureInChase = false;
            enemy.focusedThreatTransform = null;
            enemy.targetedThreat.threatScript = null;
        }

        public override void Follow(RadMechAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex == FlightState) return;
            Disengage(enemy);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(RadMechAI enemy) => Disengage(enemy);

        public override bool CanUseEntrances(RadMechAI enemy) => false;
        public override float SightRange(RadMechAI enemy) => 60f;
        public override float SightAngle(RadMechAI enemy) => enemy.fov;
    }
}
