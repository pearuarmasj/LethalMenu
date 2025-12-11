using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for BaboonBirdAI (Baboon Hawk).
    /// Abilities: Grab/drop items, use held items (fire shotgun!), camp management.
    /// 
    public class BaboonBirdController : IEnemyController<BaboonBirdAI>
    {
        private enum State
        {
            Scouting = 0,
            Returning = 1,
            Aggressive = 2
        }

        // Custom camp position far away to prevent AI from returning to original camp
        private static readonly Vector3 CustomCamp = new Vector3(1000.0f, 0.0f, 0.0f);
        private Vector3 _originalCamp = Vector3.zero;

        /// 
        /// Grab an item and sync to network.
        /// 
        private static void GrabItemAndSync(BaboonBirdAI enemy, GrabbableObject item)
        {
            if (!item.TryGetComponent(out NetworkObject networkObject)) return;
            enemy.GrabItemAndSync(networkObject);
        }

        public void OnTakeControl(BaboonBirdAI enemy)
        {
            // Move camp position far away so the AI doesn't try to return to it
            if (BaboonBirdAI.baboonCampPosition != CustomCamp)
            {
                _originalCamp = BaboonBirdAI.baboonCampPosition;
                BaboonBirdAI.baboonCampPosition = CustomCamp;
            }
        }

        public void OnReleaseControl(BaboonBirdAI enemy)
        {
            // Restore original camp position
            if (BaboonBirdAI.baboonCampPosition == CustomCamp && _originalCamp != Vector3.zero)
            {
                BaboonBirdAI.baboonCampPosition = _originalCamp;
            }
        }

        public void OnDeath(BaboonBirdAI enemy)
        {
            // Drop held scrap on death
            if (enemy.heldScrap != null)
            {
                enemy.DropHeldItemAndSync();
            }
        }

        public void UsePrimarySkill(BaboonBirdAI enemy)
        {
            // If not holding anything, try to grab nearby item
            if (enemy.heldScrap == null)
            {
                var nearbyItem = enemy.FindNearbyItem(2.0f);
                if (nearbyItem != null)
                {
                    GrabItemAndSync(enemy, nearbyItem);
                }
                return;
            }

            // If holding a shotgun, fire it!
            if (enemy.heldScrap is ShotgunItem shotgun)
            {
                var pos = enemy.transform.position + enemy.transform.up * 0.45f;
                var forward = enemy.transform.forward;
                shotgun.ShootGunServerRpc(pos, forward);
            }
        }

        public void UseSecondarySkill(BaboonBirdAI enemy)
        {
            // Drop held item
            if (enemy.heldScrap == null) return;
            enemy.DropHeldItemAndSync();
        }

        public string? GetPrimarySkillName(BaboonBirdAI enemy) =>
            enemy.heldScrap != null ? (enemy.heldScrap is ShotgunItem ? "Fire" : "") : "Grab Item";

        public string? GetSecondarySkillName(BaboonBirdAI enemy) =>
            enemy.heldScrap != null ? "Drop Item" : "";

        public float InteractRange(BaboonBirdAI _) => 1.5f;

        public bool CanUseEntranceDoors(BaboonBirdAI _) => false;
    }
}
