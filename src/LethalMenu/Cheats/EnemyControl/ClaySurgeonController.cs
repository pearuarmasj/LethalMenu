namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for ClaySurgeonAI (Barber).
    /// No primary/secondary. Gates movement during jump-and-snip state.
    /// </summary>
    public class ClaySurgeonController : IEnemyController<ClaySurgeonAI>
    {
        public void OnTakeControl(ClaySurgeonAI enemy)
        {
            enemy.SyncMasterClaySurgeonClientRpc();
        }

        public string? GetPrimarySkillName(ClaySurgeonAI _) => "";
        public string? GetSecondarySkillName(ClaySurgeonAI _) => "";

        public void OnMovement(ClaySurgeonAI enemy, bool isMoving, bool isSprinting)
        {
            if (isSprinting || isMoving && enemy != null && !IsJumpingAndSnipping(enemy)) return;
        }

        public bool CanUseEntranceDoors(ClaySurgeonAI _) => false;
        public float InteractRange(ClaySurgeonAI _) => 2.5f;

        private static bool IsJumpingAndSnipping(ClaySurgeonAI enemy)
            => enemy.isJumping && enemy.agent.speed > 0.0;
    }
}
