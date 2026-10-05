using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Ghost Girl. Vanilla chase = behaviour state 1 entered through the private BeginChasing() (local state
    /// switch, lights RPCs, SetMovingTowardsTargetPlayer(hauntingPlayer)). Her whole state machine in Update only
    /// runs on the client whose local player IS hauntingPlayer, and the kill is OnCollideWithPlayer on that
    /// victim's client (hauntingLocalPlayer && state 1 -> KillPlayer). Directed, hauntingPlayer is always another
    /// player, so on our client Update falls through to the ownership branch (ChangeOwnershipOfEnemy, suppressed
    /// while directed) and the state machine never runs: no stare warps, no chase timer, no StopChasing.
    /// BeginChasing only switches the state locally, so Engage also sends SwitchToBehaviourServerRpc(1) to put the
    /// victim's client into state 1. AfterUpdate keeps hauntingPlayer on the target (the server re-picks it through
    /// ChooseNewHauntingPlayerClientRpc when the haunted player is gone) and keeps it off the local player, so
    /// DressGirlUntargetablePatches (which only act on a haunt aimed at the local player) never have to fight us.
    /// Limit: the kill fires only when the victim's own client also resolved hauntingPlayer to the victim (derived
    /// from the shared map seed, not synced); otherwise the chase is harassment only. The mesh is shown locally
    /// only, as in vanilla.
    public sealed class DressGirlDirectiveAdapter : DirectiveAdapter<DressGirlAI>
    {
        private const int RoamState = 0;
        private const int ChaseState = 1;
        private const float WalkSpeed = 5.25f;

        /// The player vanilla had chosen, restored on Release so ownership does not ping-pong afterwards.
        private static readonly ConditionalWeakTable<DressGirlAI, PlayerControllerB> Original = new();

        public override void Engage(DressGirlAI enemy, PlayerControllerB target)
        {
            Remember(enemy);
            Aim(enemy, target);
            ShowMesh(enemy);
            if (enemy.currentBehaviourStateIndex != ChaseState)
            {
                enemy.BeginChasing();
                enemy.SwitchToBehaviourServerRpc(ChaseState);
            }
            enemy.agent.speed = WalkSpeed;
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(DressGirlAI enemy, PlayerControllerB? target)
        {
            Remember(enemy);
            if (target != null)
            {
                Aim(enemy, target);
                base.AfterUpdate(enemy, target);
            }
            else
            {
                KeepOffLocalPlayer(enemy);
            }
        }

        public override void Disengage(DressGirlAI enemy)
        {
            LeaveChase(enemy);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        /// Escort walk: visible, calm (state 0), towards the ring point.
        public override void Follow(DressGirlAI enemy, Vector3 position)
        {
            Remember(enemy);
            KeepOffLocalPlayer(enemy);
            LeaveChase(enemy);
            ShowMesh(enemy);
            enemy.agent.speed = WalkSpeed;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(DressGirlAI enemy)
        {
            LeaveChase(enemy);
            enemy.EnableEnemyMesh(false, true, true);
            enemy.enemyMeshEnabled = false;
            enemy.creatureAnimator.SetBool("Walk", false);
            enemy.movingTowardsTargetPlayer = false;
            enemy.moveTowardsDestination = false;
            enemy.targetPlayer = null;
            enemy.timer = 0f;
            enemy.staringInHaunt = false;
            if (Original.TryGetValue(enemy, out var original))
            {
                enemy.hauntingPlayer = original;
                Original.Remove(enemy);
            }
            enemy.hauntingLocalPlayer = enemy.hauntingPlayer == LethalMenuMod.LocalPlayer;
        }

        public override float SightRange(DressGirlAI enemy) => 60f;
        public override float SightAngle(DressGirlAI enemy) => 80f;

        private static void Remember(DressGirlAI enemy)
        {
            if (enemy.hauntingPlayer != null && !Original.TryGetValue(enemy, out _))
                Original.Add(enemy, enemy.hauntingPlayer);
        }

        /// hauntingLocalPlayer stays false: the local player is never the victim, and OnCollideWithPlayer on our
        /// client must not kill us.
        private static void Aim(DressGirlAI enemy, PlayerControllerB target)
        {
            enemy.hauntingPlayer = target;
            enemy.hauntingLocalPlayer = false;
        }

        /// Without a target hauntingPlayer may still be the local player (vanilla choice). Swap it to any other
        /// valid player; if the local player is alone, Update runs the haunt branch for us, so hold its timer at 0
        /// (TryFindingHauntPosition is what would stare at us) and make the kill inert.
        private static void KeepOffLocalPlayer(DressGirlAI enemy)
        {
            enemy.hauntingLocalPlayer = false;
            if (enemy.hauntingPlayer != LethalMenuMod.LocalPlayer) return;

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!DirectiveTargeting.IsValidTarget(player)) continue;
                enemy.hauntingPlayer = player;
                return;
            }
            enemy.timer = 0f;
            enemy.staringInHaunt = false;
        }

        private static void ShowMesh(DressGirlAI enemy)
        {
            if (!enemy.enemyMeshEnabled)
            {
                enemy.EnableEnemyMesh(true, true);
                enemy.enemyMeshEnabled = true;
            }
            enemy.creatureAnimator.SetBool("Walk", true);
        }

        /// StopChasing is the local exit (state 0, mesh off, voice off); the RPC puts the victim's client back.
        private static void LeaveChase(DressGirlAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != ChaseState) return;
            enemy.StopChasing();
            enemy.enemyMeshEnabled = false;
            enemy.SwitchToBehaviourServerRpc(RoamState);
        }
    }
}
