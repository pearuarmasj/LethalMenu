namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for SandSpiderAI.
    /// Abilities: Spawn web, attack.
    /// 
    public class SandSpiderController : IEnemyController<SandSpiderAI>
    {
        private enum State
        {
            Idle = 0,
            Patrolling = 1,
            Chasing = 2
        }

        public void UsePrimarySkill(SandSpiderAI enemy)
        {
            // Enter attack mode
            enemy.SetBehaviourState(State.Chasing);
        }

        public void UseSecondarySkill(SandSpiderAI enemy)
        {
            // Spawn web at current position
            var ray = new UnityEngine.Ray(enemy.transform.position, UnityEngine.Vector3.down);
            if (UnityEngine.Physics.Raycast(ray, out var hit, 10f, StartOfRound.Instance.collidersAndRoomMask))
            {
                enemy.SpawnWebTrapServerRpc(hit.point, hit.point);
            }
        }

        public string? GetPrimarySkillName(SandSpiderAI _) => "Attack";

        public string? GetSecondarySkillName(SandSpiderAI _) => "Spawn Web";

        public float InteractRange(SandSpiderAI _) => 2.0f;

        public bool CanUseEntranceDoors(SandSpiderAI _) => false;
    }
}
