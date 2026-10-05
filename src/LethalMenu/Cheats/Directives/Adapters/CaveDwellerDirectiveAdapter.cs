using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Maneater. The baby (state 0) has no attack: Engage first makes it an adult exactly as CaveDwellerController does
    /// (SwitchToBehaviourStateOnLocalClient(1) + TurnIntoAdultServerRpc + StartTransformationAnim) and waits out the
    /// transformation (inSpecialAnimation). Adult chase = behaviour state 3, entered by DoAIIntervalChaseLogic when within
    /// attackDistance (+16 outside) with a clear line or under 4.5 m; Update state 3 then screams, leaps (DoLeapServerRpc) and
    /// chases after the leap by itself. The kill is the victim's own OnCollideWithPlayer during the leap ->
    /// KillPlayerAnimationServerRpc. Farther away the adult approaches directly in state 1 (the vanilla state 2 sneak routing
    /// is cave-bound). Update's host branch retargets the closest player every 0.3 s (its ownership hand-off is suppressed by
    /// the director); AfterUpdate restores the target. Baby escort walks to the follow point at the baby's own speed.
    public sealed class CaveDwellerDirectiveAdapter : DirectiveAdapter<CaveDwellerAI>
    {
        private const int BabyState = 0;
        private const int RoamState = 1;
        private const int ChaseState = 3;
        private const float PointBlankDistance = 4.5f;
        private const float OutsideAttackBonus = 16f;
        private const float BabyWalkSpeed = 4.2f;

        public override void Engage(CaveDwellerAI enemy, PlayerControllerB target)
        {
            if (enemy.inSpecialAnimation || enemy.inKillAnimation) return;
            if (enemy.currentBehaviourStateIndex == BabyState)
            {
                BecomeAdult(enemy);
                return;
            }

            enemy.SetMovingTowardsTargetPlayer(target);
            if (enemy.searchRoutine.inProgress) enemy.StopSearch(enemy.searchRoutine);
            if (enemy.currentBehaviourStateIndex != ChaseState && InAttackRange(enemy, target))
                enemy.SwitchToBehaviourState(ChaseState);
            else if (enemy.currentBehaviourStateIndex == 2)
                enemy.SwitchToBehaviourState(RoamState);
        }

        public override void AfterUpdate(CaveDwellerAI enemy, PlayerControllerB? target)
        {
            if (enemy.currentBehaviourStateIndex == BabyState) return;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(CaveDwellerAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != BabyState && enemy.currentBehaviourStateIndex != RoamState)
                enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
        }

        public override void Follow(CaveDwellerAI enemy, Vector3 position)
        {
            if (enemy.inSpecialAnimation || enemy.inKillAnimation) return;
            enemy.movingTowardsTargetPlayer = false;
            if (enemy.currentBehaviourStateIndex == BabyState)
            {
                if (enemy.holdingBaby || enemy.rolledOver) return;
                if (enemy.babySearchRoutine.inProgress) enemy.StopSearch(enemy.babySearchRoutine);
                enemy.agent.speed = BabyWalkSpeed;
            }
            else
            {
                if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
                if (enemy.searchRoutine.inProgress) enemy.StopSearch(enemy.searchRoutine);
            }
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(CaveDwellerAI enemy) => Disengage(enemy);

        /// Same calls as CaveDwellerController.TransformIntoAdult.
        private static void BecomeAdult(CaveDwellerAI enemy)
        {
            enemy.SwitchToBehaviourStateOnLocalClient(RoamState);
            enemy.TurnIntoAdultServerRpc();
            enemy.StartTransformationAnim();
        }

        /// DoAIIntervalChaseLogic's trigger for entering state 3.
        private static bool InAttackRange(CaveDwellerAI enemy, PlayerControllerB target)
        {
            float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
            if (distance < PointBlankDistance) return true;
            float range = enemy.attackDistance + (enemy.isOutside ? OutsideAttackBonus : 0f);
            return distance < range && !Physics.Linecast(enemy.transform.position + Vector3.up * 0.25f,
                target.transform.position + Vector3.up * 0.25f,
                StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
        }
    }
}
