using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Feiopar. Vanilla attack = behaviour state 2 (stalk state 1 -> attack when the player is within 2.5 m or
    /// looks at it up close; a drop from the tree lands in state 1 and Update(state 0) goes straight to 2).
    /// Engage skips the stalk and switches to state 2 with SwitchToBehaviourState (synced) and targetPlayer set;
    /// PumaAI.Update state 2 then runs the charge (speed, stopping distance, scream) and the chase through
    /// NavigateTowardsTargetPlayer. The scratch is Update's Scratching animator bool -> AnimationEventC
    /// (timeAtLastScratch) -> the victim's own OnCollideWithPlayer -> DamagePlayer(7) + PumaDamagePlayerRpc.
    /// A puma clinging to a tree is first dropped with StartTreeDropOnLocalClient + StartTreeDropRpc (the call
    /// RunTreeMode makes); EndDroppingDownAnimationOnLocalClient then lands it in state 1 and Engage moves on
    /// to 2. targetPlayer stays null while clinging so RunTreeMode's leap/drop logic (gated on it) stays idle.
    /// AfterUpdate resets what ends state 2 inside Update: timeSpentAttacking (5 s without a scratch) and
    /// scaredMeter (flees from a sprinting target); PlayerIsTargetable (target in the other area) still ends it
    /// and the director re-engages once the routing put them on the same side. While following, it forces
    /// TargetTree null (the state 0 run-to-tree would climb) and a walking agent.speed (state 0 sets 24).
    /// On the victim's client Update sets Scratching from the distance to the OWNER's player (that client's
    /// targetPlayer comes from ClientPlayerList[OwnerClientId]); ProxyScratch covers the case where the director
    /// is farther than that.
    public sealed class PumaDirectiveAdapter : DirectiveAdapter<PumaAI>
    {
        private const int RoamState = 0;
        private const int AttackState = 2;
        private const float FollowSpeed = 9f;

        public override void Engage(PumaAI enemy, PlayerControllerB target)
        {
            if (LeaveTree(enemy)) return;

            enemy.TargetTree = null;
            enemy.SetMovingTowardsTargetPlayer(target);
            if (enemy.currentBehaviourStateIndex != AttackState)
                enemy.SwitchToBehaviourState(AttackState);
        }

        public override void AfterUpdate(PumaAI enemy, PlayerControllerB? target)
        {
            if (enemy.clingingToTree)
            {
                enemy.targetPlayer = null;
                enemy.movingTowardsTargetPlayer = false;
                return;
            }

            enemy.TargetTree = null;
            if (target == null)
            {
                if (enemy.currentBehaviourStateIndex == RoamState && enemy.agent.enabled)
                    enemy.agent.speed = FollowSpeed;
                return;
            }

            enemy.timeSpentAttacking = 0f;
            enemy.scaredMeter = 0f;
            base.AfterUpdate(enemy, target);
            ProxyScratch(enemy, target);
        }

        /// The victim's client only plays the scratch (and so only takes the hit in its OnCollideWithPlayer)
        /// while the OWNER's player is within 6 m of the puma. When the director is farther away, each scratch
        /// our client animates (AnimationEventC stamps timeAtLastScratch) that reaches the target is delivered
        /// through DamagePlayerFromOtherClientServerRpc (RequireOwnership = false) with the vanilla 7 damage.
        private static void ProxyScratch(PumaAI enemy, PlayerControllerB target)
        {
            var local = LethalMenuMod.LocalPlayer;
            if (local == null || enemy.currentBehaviourStateIndex != AttackState) return;
            if (Vector3.Distance(enemy.transform.position, local.transform.position) < 6f) return;

            float scratch = enemy.timeAtLastScratch;
            if (scratch <= 0f || (LastProxied.TryGetValue(enemy, out float done) && done == scratch)) return;
            if (Vector3.Distance(enemy.transform.position, target.transform.position) > ScratchReach) return;

            LastProxied[enemy] = scratch;
            Vector3 push = Vector3.Normalize(target.transform.position - enemy.transform.position);
            target.DamagePlayerFromOtherClientServerRpc(7, push, (int)local.playerClientId);
            enemy.PumaDamagePlayerRpc((int)target.playerClientId);
        }

        private const float ScratchReach = 3.2f;
        private static readonly System.Collections.Generic.Dictionary<PumaAI, float> LastProxied = new();

        public override void Disengage(PumaAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(PumaAI enemy, Vector3 position)
        {
            if (LeaveTree(enemy)) return;
            Disengage(enemy);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(PumaAI enemy) => Disengage(enemy);

        /// True while the puma is still in its tree. Starts the drop once it is idle up there.
        private static bool LeaveTree(PumaAI enemy)
        {
            if (!enemy.clingingToTree) return false;
            enemy.targetPlayer = null;
            if (enemy.treeState == TreeState.Idle && enemy.TargetTree != null && enemy.StartTreeDropOnLocalClient())
                enemy.StartTreeDropRpc(enemy.clingIdlePosition, enemy.leapFromPosition, enemy.treeClingRotation, relocate: false);
            return true;
        }
    }
}
