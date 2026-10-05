using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Baboon Hawk. Vanilla attack = focus on a threat, behaviour state 2, entered by DoLOSCheck ->
    /// ReactToThreat: a Threat entry for the IVisibleThreat (the target player), focusedThreat /
    /// focusedThreatTransform, StartFocusOnThreatServerRpc (RequireOwnership; the ClientRpc switches every
    /// client to state 2), then DoAIInterval case 2 escalates to aggressiveMode 2 (SetAggressiveMode ->
    /// SetAggressiveModeServerRpc) and runs at the threat's predicted position. Engage mirrors that: it keeps
    /// the Threat entry fresh (DoLOSCheck, which refreshes timeLastSeen, is skipped), forces aggressiveMode 2
    /// and sets agent.speed (DoAIInterval owns it). The hit is the victim's own OnCollideWithPlayer ->
    /// DamagePlayer(20) -> StabPlayerDeathAnimServerRpc on death, so it replicates normally.
    /// AfterUpdate keeps focusingOnThreat set; Update itself never leaves state 2 (StopFocusingThreat is only
    /// reached from DoAIInterval).
    public sealed class BaboonBirdDirectiveAdapter : DirectiveAdapter<BaboonBirdAI>
    {
        private const int RoamState = 0;
        private const int FocusState = 2;
        private const int AggressiveMode = 2;
        private const float AttackSpeed = 9f;
        private const float RoamSpeed = 10f;
        private const float LeadSeconds = 10f;

        public override void Engage(BaboonBirdAI enemy, PlayerControllerB target)
        {
            if (enemy.stunNormalizedTimer > 0f || enemy.inSpecialAnimation) return;

            var threat = (IVisibleThreat)target;
            Vector3 seenAt = target.transform.position + Vector3.up * 0.5f;

            if (!enemy.threats.TryGetValue(target.transform, out var focused))
            {
                focused = new Threat();
                enemy.threats[target.transform] = focused;
            }
            focused.type = threat.type;
            focused.timeLastSeen = Time.realtimeSinceStartup;
            focused.lastSeenPosition = seenAt;
            focused.distanceToThreat = Vector3.Distance(enemy.eye.position, target.transform.position);
            focused.threatLevel = threat.GetThreatLevel(enemy.eye.position);
            focused.threatScript = threat;
            focused.interestLevel = threat.GetInterestLevel();

            if (enemy.currentBehaviourStateIndex != FocusState || enemy.focusedThreat != focused)
            {
                enemy.fightTimer = 0f;
                enemy.focusingOnThreat = true;
                enemy.focusedThreat = focused;
                enemy.focusedThreatTransform = threat.GetThreatLookTransform();
                enemy.StartFocusOnThreatServerRpc(target.NetworkObject);
                enemy.SwitchToBehaviourStateOnLocalClient(FocusState);
                if (enemy.previousBehaviourState != FocusState)
                {
                    // DoAIInterval's state 2 entry block.
                    enemy.timeSpentFocusingOnThreat = 0f;
                    enemy.creatureAnimator.SetBool("sleep", false);
                    enemy.creatureAnimator.SetBool("sit", false);
                    enemy.EnemyGetUpServerRpc();
                    enemy.previousBehaviourState = FocusState;
                }
            }

            enemy.focusLevel = 3;
            enemy.SetThreatInView(true);
            enemy.SetAggressiveMode(AggressiveMode);
            if (enemy.scoutingSearchRoutine.inProgress) enemy.StopSearch(enemy.scoutingSearchRoutine, clear: false);
            if (enemy.heldScrap != null)
                enemy.DropHeldItemAndSync(sync: true);
            enemy.focusedScrap = null;

            enemy.agent.speed = AttackSpeed;
            if (!enemy.SetDestinationToPosition(target.transform.position + threat.GetThreatVelocity() * LeadSeconds, checkForPath: true))
                enemy.SetDestinationToPosition(target.transform.position);
        }

        public override void AfterUpdate(BaboonBirdAI enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.focusingOnThreat = true;
        }

        public override void Disengage(BaboonBirdAI enemy)
        {
            enemy.SetAggressiveMode(0);
            enemy.SetThreatInView(false);
            enemy.focusLevel = 0;
            enemy.focusingOnThreat = false;
            enemy.focusedThreat = null;
            enemy.focusedThreatTransform = null;
            if (enemy.currentBehaviourStateIndex == FocusState)
                enemy.StopFocusingThreat();
            else if (enemy.currentBehaviourStateIndex != RoamState)
                enemy.SwitchToBehaviourState(RoamState);
        }

        public override void Follow(BaboonBirdAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != RoamState || enemy.focusingOnThreat)
                Disengage(enemy);
            if (enemy.stunNormalizedTimer > 0f || enemy.inSpecialAnimation) return;
            if (enemy.scoutingSearchRoutine.inProgress) enemy.StopSearch(enemy.scoutingSearchRoutine, clear: false);
            if (enemy.creatureAnimator.GetBool("sit"))
            {
                enemy.EnemyGetUpServerRpc();
                enemy.creatureAnimator.SetBool("sit", false);
            }
            enemy.agent.speed = RoamSpeed;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(BaboonBirdAI enemy) => Disengage(enemy);
    }
}
