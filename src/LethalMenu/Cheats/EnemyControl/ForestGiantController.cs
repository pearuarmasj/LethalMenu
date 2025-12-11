namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for ForestGiantAI.
    /// Abilities: Chase mode (sprinting), eat players on contact.
    /// 
    public class ForestGiantController : IEnemyController<ForestGiantAI>
    {
        private enum State
        {
            Default = 0,
            Chase = 1
        }

        private bool _isUsingSecondarySkill = false;

        public void OnMovement(ForestGiantAI enemy, bool isMoving, bool isSprinting)
        {
            if (!_isUsingSecondarySkill)
            {
                enemy.SetBehaviourState(State.Default);
            }
        }

        public void OnSecondarySkillHold(ForestGiantAI enemy)
        {
            _isUsingSecondarySkill = true;
            enemy.SetBehaviourState(State.Chase);
        }

        public void ReleaseSecondarySkill(ForestGiantAI enemy)
        {
            _isUsingSecondarySkill = false;
            enemy.SetBehaviourState(State.Default);
        }

        public void OnReleaseControl(ForestGiantAI enemy)
        {
            _isUsingSecondarySkill = false;
        }

        public bool IsAbleToMove(ForestGiantAI enemy)
        {
            // Can't move during eating animation
            return !enemy.GetPrivateField<bool>("inEatingPlayerAnimation");
        }

        public string? GetSecondarySkillName(ForestGiantAI _) => "(HOLD) Chase";

        public bool CanUseEntranceDoors(ForestGiantAI _) => false;

        public float InteractRange(ForestGiantAI _) => 0.0f;

        public bool SyncAnimationSpeedEnabled(ForestGiantAI _) => false;
    }
}
