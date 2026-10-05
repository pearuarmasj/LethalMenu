using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Hoarding Bug. Vanilla attack = angry chase, behaviour state 2 entered by LateUpdate.DetectAndLookAtPlayers
    /// when IsHoarderBugAngry() (angryTimer > 0 with angryAtPlayer set) via SwitchToBehaviourState(2); state 2
    /// then speeds the bug to 18 and OnCollideWithPlayer (inChase) does DamagePlayer(30) + HitPlayerServerRpc, so
    /// the hit replicates normally. Engage sets angryAtPlayer/angryTimer/targetPlayer and enters state 2 the same
    /// way, dropping a held item first like the vanilla state-2 DoAIInterval does. Update drains angryTimer and
    /// leaves state 2 when it hits 0, and LateUpdate retargets watchingPlayer/targetPlayer to whoever the bug
    /// sees, so AfterUpdate re-applies the anger and the target every frame. With no target it pulls an
    /// attack the bug started on its own (e.g. against the local player) back to state 0.
    public sealed class HoarderBugDirectiveAdapter : DirectiveAdapter<HoarderBugAI>
    {
        private const int RoamState = 0;
        private const int ChaseState = 2;
        private const float AngerTime = 4f;

        public override void Engage(HoarderBugAI enemy, PlayerControllerB target)
        {
            if (enemy.heldItem != null)
                enemy.DropItemAndCallDropRPC(enemy.heldItem.itemGrabbableObject.GetComponent<NetworkObject>(), droppedInNest: false);

            Anger(enemy, target);
            if (enemy.searchForPlayer.inProgress) enemy.StopSearch(enemy.searchForPlayer);
            if (enemy.searchForItems.inProgress) enemy.StopSearch(enemy.searchForItems);
            if (enemy.currentBehaviourStateIndex != ChaseState)
                enemy.SwitchToBehaviourState(ChaseState);
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(HoarderBugAI enemy, PlayerControllerB? target)
        {
            if (target == null)
            {
                if (enemy.currentBehaviourStateIndex == ChaseState) Disengage(enemy);
                return;
            }
            Anger(enemy, target);
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(HoarderBugAI enemy)
        {
            enemy.angryTimer = 0f;
            enemy.angryAtPlayer = null;
            enemy.watchingPlayer = null;
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(HoarderBugAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex == ChaseState) Disengage(enemy);
            if (enemy.searchForItems.inProgress) enemy.StopSearch(enemy.searchForItems);
            if (enemy.searchForPlayer.inProgress) enemy.StopSearch(enemy.searchForPlayer);
            enemy.targetItem = null;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(HoarderBugAI enemy) => Disengage(enemy);

        private static void Anger(HoarderBugAI enemy, PlayerControllerB target)
        {
            enemy.angryAtPlayer = target;
            enemy.watchingPlayer = target;
            enemy.angryTimer = Mathf.Max(enemy.angryTimer, AngerTime);
            enemy.lostPlayerInChase = false;
            enemy.timeSinceSeeingAPlayer = 0f;
        }
    }
}
