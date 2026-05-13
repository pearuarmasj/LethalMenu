using UnityEngine;
using Random = UnityEngine.Random;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for SandSpiderAI (Bunker Spider).
    /// Primary: Place a web trap at a randomly-projected nearby anchor point.
    /// Movement: forces walk/run speed up so the spider is actually controllable.
    /// </summary>
    public class SandSpiderController : IEnemyController<SandSpiderAI>
    {
        private const float WalkSpeed = 6.0f;
        private const float SprintSpeed = 8.0f;

        public void OnMovement(SandSpiderAI enemy, bool isMoving, bool isSprinting)
        {
            enemy.creatureAnimator.SetBool("moving", true);
            float speed = isSprinting ? SprintSpeed : WalkSpeed;
            enemy.agent.speed = speed;
            enemy.spiderSpeed = speed;
            enemy.SyncMeshContainerPositionToClients();
        }

        public void UsePrimarySkill(SandSpiderAI enemy) => PlaceWebTrap(enemy);

        public string? GetPrimarySkillName(SandSpiderAI _) => "Place Web";
        public string? GetSecondarySkillName(SandSpiderAI _) => "";

        public float InteractRange(SandSpiderAI _) => 2.0f;
        public bool CanUseEntranceDoors(SandSpiderAI _) => false;

        private static void PlaceWebTrap(SandSpiderAI enemy)
        {
            if (StartOfRound.Instance == null) return;

            Vector3 randomDirection = Random.onUnitSphere;
            randomDirection.y = Mathf.Min(0.0f, randomDirection.y * Random.Range(0.5f, 1.0f));

            var ray = new Ray(enemy.abdomen.position + Vector3.up * 0.4f, randomDirection);
            if (!Physics.Raycast(ray, out RaycastHit anchorHit, 7.0f, StartOfRound.Instance.collidersAndRoomMask)) return;
            if (anchorHit.distance < 2.0f) return;
            if (!Physics.Raycast(enemy.abdomen.position, Vector3.down, out RaycastHit groundHit, 10.0f, StartOfRound.Instance.collidersAndRoomMask)) return;

            Vector3 floorPosition = groundHit.point + Vector3.up * 0.2f;
            enemy.SpawnWebTrapServerRpc(floorPosition, anchorHit.point);
        }
    }
}
