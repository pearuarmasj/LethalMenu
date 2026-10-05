using System;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives
{
    /// Owns every directive. Runs on the client that directs; the enemy must be owned by this client for
    /// the tick to act (enemy AI only runs on its owner).
    public static class EnemyDirector
    {
        private sealed class Directive
        {
            public DirectiveKind Kind;
            public PlayerControllerB? HuntTarget;
            public PlayerControllerB? Engaged;
            public bool Disengaged = true;
            public Vector3? FollowPoint;
            public float NextOwnershipRequest;
        }

        private const float OwnershipRequestInterval = 0.5f;
        private const float FollowRingMin = 4f;
        private const float FollowRingMax = 8f;
        private const float FollowPointReached = 2f;
        private const float FollowPointMaxDrift = 12f;

        private static readonly Dictionary<EnemyAI, Directive> Directives = new();
        private static readonly HashSet<string> LoggedErrors = new();

        public static bool IsDirected(EnemyAI? enemy) => enemy != null && Directives.ContainsKey(enemy);

        public static string Describe(EnemyAI enemy)
        {
            if (!Directives.TryGetValue(enemy, out var d)) return "";
            return d.Kind == DirectiveKind.Escort
                ? (d.Engaged != null ? $"Escort -> {d.Engaged.playerUsername}" : "Escort")
                : $"Hunt {d.HuntTarget?.playerUsername}";
        }

        public static bool Escort(EnemyAI enemy) => Assign(enemy, new Directive { Kind = DirectiveKind.Escort });

        public static bool Hunt(EnemyAI enemy, PlayerControllerB target)
        {
            if (!DirectiveTargeting.IsValidTarget(target))
            {
                HUDManager.Instance?.DisplayTip("Hunt", "That player can't be a hunt target.");
                return false;
            }
            return Assign(enemy, new Directive { Kind = DirectiveKind.Hunt, HuntTarget = target });
        }

        public static int EscortAll() => LethalMenuMod.Enemies.Count(e => e != null && !e.isEnemyDead && Escort(e));

        public static int HuntAll(PlayerControllerB target) =>
            LethalMenuMod.Enemies.Count(e => e != null && !e.isEnemyDead && Hunt(e, target));

        public static void Release(EnemyAI enemy)
        {
            if (enemy == null || !Directives.ContainsKey(enemy)) return;
            var adapter = DirectiveAdapterRegistry.Get(enemy);
            Remove(enemy);
            if (adapter == null || !enemy.IsOwner || enemy.isEnemyDead) return;
            try
            {
                adapter.Release(enemy);
            }
            catch (Exception ex)
            {
                string key = $"{enemy.GetType().Name}|release|{ex.GetType().Name}|{ex.Message}";
                if (LoggedErrors.Add(key))
                    Loader.LogError($"[Directives] {enemy.GetType().Name} release failed: {ex}");
            }
        }

        public static void ReleaseAll()
        {
            foreach (var enemy in Directives.Keys.ToList())
                Release(enemy);
            Directives.Clear();
        }

        /// Lobby change: the enemies are gone, just forget them.
        public static void Clear()
        {
            Directives.Clear();
            DirectiveAuthority.Clear();
        }

        /// Drop destroyed / dead enemies.
        public static void Prune()
        {
            if (Directives.Count == 0) return;
            foreach (var enemy in Directives.Keys.Where(e => e == null || e.isEnemyDead).ToList())
                Remove(enemy);
        }

        /// Replaces the vanilla DoAIInterval for a directed enemy (DirectivePatches).
        public static void Tick(EnemyAI enemy)
        {
            if (!Directives.TryGetValue(enemy, out var d)) return;
            if (enemy.isEnemyDead) { Remove(enemy); return; }
            var adapter = DirectiveAdapterRegistry.Get(enemy)!;
            var local = LethalMenuMod.LocalPlayer;

            try
            {
                if (!enemy.IsOwner)
                {
                    RequestOwnership(enemy, d);
                    return;
                }

                PlayerControllerB? target;
                if (d.Kind == DirectiveKind.Hunt)
                {
                    if (!DirectiveTargeting.IsValidTarget(d.HuntTarget))
                    {
                        Release(enemy);
                        return;
                    }
                    target = d.HuntTarget;
                }
                else
                {
                    target = DirectiveTargeting.FindEscortTarget(enemy, adapter, d.Engaged);
                }

                if (target == null && local == null)
                {
                    BaseInterval(enemy);
                    return;
                }

                Vector3 goal = target != null ? target.transform.position : FollowPoint(enemy, d, local!);
                bool goalOutside = target != null ? !target.isInsideFactory : !local!.isInsideFactory;
                if (DirectiveRouting.Route(enemy, adapter, goal, goalOutside))
                {
                    BaseInterval(enemy);
                    return;
                }

                if (target != null)
                {
                    adapter.Engage(enemy, target);
                    d.Engaged = target;
                    d.Disengaged = false;
                }
                else
                {
                    if (!d.Disengaged)
                    {
                        adapter.Disengage(enemy);
                        d.Disengaged = true;
                        d.Engaged = null;
                    }
                    adapter.Follow(enemy, goal);
                }

                BaseInterval(enemy);
            }
            catch (Exception ex)
            {
                string key = $"{enemy.GetType().Name}|{ex.GetType().Name}|{ex.Message}";
                if (LoggedErrors.Add(key))
                    Loader.LogError($"[Directives] {enemy.GetType().Name} tick failed, releasing: {ex}");
                Release(enemy);
            }
        }

        /// Before the enemy's vanilla Update (DirectivePatches).
        public static void BeforeUpdate(EnemyAI enemy)
        {
            if (!Directives.ContainsKey(enemy) || !enemy.IsOwner || enemy.isEnemyDead) return;
            if (DirectiveAdapterRegistry.Get(enemy)?.RunsServerLogic(enemy) == true)
                DirectiveAuthority.Elevate(enemy);
        }

        /// After the enemy's vanilla Update (DirectivePatches).
        public static void AfterUpdate(EnemyAI enemy)
        {
            if (!Directives.TryGetValue(enemy, out var d) || !enemy.IsOwner || enemy.isEnemyDead) return;
            try
            {
                // Vanilla Update paths (stun retaliation, cling, anger) can retarget an escort that has no
                // engaged target onto the director or a friend; drop such a target before the adapter runs.
                if (enemy.targetPlayer != null && !DirectiveTargeting.IsValidTarget(enemy.targetPlayer))
                {
                    enemy.targetPlayer = null;
                    enemy.movingTowardsTargetPlayer = false;
                }
                DirectiveAdapterRegistry.Get(enemy)?.AfterUpdate(enemy, d.Engaged);
            }
            catch (Exception ex)
            {
                string key = $"{enemy.GetType().Name}|update|{ex.GetType().Name}|{ex.Message}";
                if (LoggedErrors.Add(key))
                    Loader.LogError($"[Directives] {enemy.GetType().Name} AfterUpdate failed, releasing: {ex}");
                Release(enemy);
            }
        }

        private static bool Assign(EnemyAI enemy, Directive directive)
        {
            if (enemy == null || enemy.isEnemyDead) return false;
            if (DirectiveAdapterRegistry.Get(enemy) == null)
            {
                HUDManager.Instance?.DisplayTip("Directives", $"No directive adapter for {enemy.enemyType?.enemyName ?? enemy.GetType().Name}.");
                return false;
            }

            Directives[enemy] = directive;
            RequestOwnership(enemy, directive);
            return true;
        }

        private static void Remove(EnemyAI enemy)
        {
            Directives.Remove(enemy);
            DirectiveRouting.Forget(enemy);
            DirectiveAuthority.Forget(enemy);
        }

        private static void RequestOwnership(EnemyAI enemy, Directive d)
        {
            var local = LethalMenuMod.LocalPlayer;
            if (local == null || enemy.IsOwner || Time.time < d.NextOwnershipRequest) return;
            d.NextOwnershipRequest = Time.time + OwnershipRequestInterval;
            enemy.ChangeEnemyOwnerServerRpc(local.actualClientId);
        }

        /// A point on a 4-8 m ring around the local player, kept until reached or the player moved away.
        private static Vector3 FollowPoint(EnemyAI enemy, Directive d, PlayerControllerB local)
        {
            Vector3 anchor = local.transform.position;
            if (d.FollowPoint is Vector3 current &&
                Vector3.Distance(enemy.transform.position, current) > FollowPointReached &&
                Vector3.Distance(anchor, current) < FollowPointMaxDrift)
                return current;

            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(FollowRingMin, FollowRingMax);
            Vector3 point = anchor + new Vector3(offset.x, 0f, offset.y);
            point = RoundManager.Instance.GetNavMeshPosition(point, RoundManager.Instance.navHit, 5f, enemy.agentMask);
            d.FollowPoint = point;
            return point;
        }

        /// EnemyAI.DoAIInterval's body (the base call every vanilla override ends with): apply the
        /// destination to the agent and sync position.
        private static void BaseInterval(EnemyAI enemy)
        {
            if (enemy.inSpecialAnimation) return;
            if (enemy.moveTowardsDestination && enemy.agent.enabled && enemy.agent.isOnNavMesh &&
                enemy.destination != enemy.prevDestination && enemy.agent.SetDestination(enemy.destination))
                enemy.prevDestination = enemy.destination;
            enemy.SyncPositionToClients();
        }
    }
}
