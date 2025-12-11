namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for SpringManAI (Coil-Head).
    /// Abilities: Freeze (stop), go (resume movement).
    /// 
    public class SpringManController : IEnemyController<SpringManAI>
    {
        private bool GetStoppingMovement(SpringManAI enemy)
        {
            return enemy.GetPrivateField<bool>("stoppingMovement");
        }

        public void OnSecondarySkillHold(SpringManAI enemy)
        {
            // Resume movement animation
            enemy.SetAnimationGoServerRpc();
        }

        public void ReleaseSecondarySkill(SpringManAI enemy)
        {
            // Stop movement animation
            enemy.SetAnimationStopServerRpc();
        }

        public bool IsAbleToMove(SpringManAI enemy) => !GetStoppingMovement(enemy);

        public bool IsAbleToRotate(SpringManAI enemy) => !GetStoppingMovement(enemy);

        public string? GetSecondarySkillName(SpringManAI _) => "(HOLD) Move";

        public float InteractRange(SpringManAI _) => 1.5f;

        public bool CanUseEntranceDoors(SpringManAI _) => false;
    }
}
