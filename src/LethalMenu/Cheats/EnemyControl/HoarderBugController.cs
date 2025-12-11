using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for HoarderBugAI.
    /// Abilities: Grab/drop items, use held items (fire shotgun!), aggressive mode.
    /// 
    public class HoarderBugController : IEnemyController<HoarderBugAI>
    {
        private enum State
        {
            Idle = 0,
            SearchingForItems = 1,
            ReturningToNest = 2,
            ChasingPlayer = 3,
            WatchingPlayer = 4,
            AtNest = 5
        }

        /// 
        /// Use the currently held item (e.g., fire shotgun).
        /// 
        private static void UseHeldItem(HoarderBugAI enemy)
        {
            if (enemy.heldItem?.itemGrabbableObject == null) return;

            var grabbable = enemy.heldItem.itemGrabbableObject;

            // If holding a shotgun, fire it!
            if (grabbable is ShotgunItem shotgun)
            {
                var pos = enemy.transform.position + enemy.transform.up * 0.45f;
                var forward = enemy.transform.forward;
                shotgun.ShootGunServerRpc(pos, forward);
            }
            // Add more item types here as needed
        }

        /// 
        /// Grab an item with proper network sync.
        /// 
        private static void GrabItem(HoarderBugAI enemy, GrabbableObject item)
        {
            if (!item.TryGetComponent(out NetworkObject networkObject)) return;

            enemy.sendingGrabOrDropRPC = true;
            enemy.GrabItem(networkObject);
            enemy.SwitchToBehaviourServerRpc(1);
            enemy.GrabItemServerRpc(networkObject);
        }

        public void Update(HoarderBugAI enemy, bool isAIControlled)
        {
            if (isAIControlled) return;
            if (enemy.heldItem?.itemGrabbableObject == null) return;

            // Keep calm while holding item
            enemy.angryTimer = 0.0f;
            enemy.SetBehaviourState(State.Idle);
        }

        public void OnDeath(HoarderBugAI enemy)
        {
            // Drop item on death
            if (enemy.heldItem?.itemGrabbableObject == null) return;
            if (!enemy.heldItem.itemGrabbableObject.TryGetComponent(out NetworkObject networkObject)) return;

            enemy.DropItemAndCallDropRPC(networkObject, false);
        }

        public void UsePrimarySkill(HoarderBugAI enemy)
        {
            // Reset angry state first
            if (enemy.angryTimer > 0.0f)
            {
                enemy.angryTimer = 0.0f;
                enemy.angryAtPlayer = null;
                enemy.SetBehaviourState(State.Idle);
            }

            // If not holding anything, try to grab nearby item
            if (enemy.heldItem == null)
            {
                var nearbyItem = enemy.FindNearbyItem(2.0f);
                if (nearbyItem != null)
                {
                    GrabItem(enemy, nearbyItem);
                }
            }
            else
            {
                // Use held item (e.g., fire shotgun)
                UseHeldItem(enemy);
            }
        }

        public void UseSecondarySkill(HoarderBugAI enemy)
        {
            // If holding item, drop it
            if (enemy.heldItem?.itemGrabbableObject != null)
            {
                if (enemy.heldItem.itemGrabbableObject.TryGetComponent(out NetworkObject networkObject))
                {
                    enemy.DropItemAndCallDropRPC(networkObject, false);
                }
                return;
            }

            // Otherwise, toggle aggressive mode (chase host player)
            if (enemy.IsBehaviourState(State.ChasingPlayer))
            {
                enemy.angryTimer = 0.0f;
                enemy.angryAtPlayer = null;
                enemy.SetBehaviourState(State.Idle);
            }
            else
            {
                // Target the first player (host)
                var players = LethalMenuMod.Players;
                if (players.Count > 0 && players[0] != null)
                {
                    enemy.watchingPlayer = players[0];
                    enemy.angryAtPlayer = players[0];
                    enemy.angryTimer = 15.0f;
                    enemy.SetBehaviourState(State.ChasingPlayer);
                }
            }
        }

        public string? GetPrimarySkillName(HoarderBugAI enemy) =>
            enemy.heldItem != null ? "Use Item" : "Grab Item";

        public string? GetSecondarySkillName(HoarderBugAI enemy) =>
            enemy.heldItem != null ? "Drop Item" : (enemy.IsBehaviourState(State.ChasingPlayer) ? "Calm Down" : "Aggressive");

        public float InteractRange(HoarderBugAI _) => 1.0f;
    }
}
