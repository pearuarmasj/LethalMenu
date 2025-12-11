namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for FlowermanAI (Bracken).
    /// Abilities: Stealth/stand mode, drop carried body.
    /// 
    public class FlowermanController : IEnemyController<FlowermanAI>
    {
        private enum State
        {
            Scouting = 0,
            Stand = 1,
            Anger = 2
        }

        public void UsePrimarySkill(FlowermanAI enemy)
        {
            if (!enemy.carryingPlayerBody)
            {
                // Enter rage mode
                enemy.SetBehaviourState(State.Anger);
            }

            // Drop body if carrying
            enemy.DropPlayerBodyServerRpc();
        }

        public void UseSecondarySkill(FlowermanAI enemy)
        {
            enemy.SetBehaviourState(State.Stand);
        }

        public void ReleaseSecondarySkill(FlowermanAI enemy)
        {
            enemy.SetBehaviourState(State.Scouting);
        }

        public bool IsAbleToMove(FlowermanAI enemy) => !enemy.inSpecialAnimation;

        public string? GetPrimarySkillName(FlowermanAI enemy) => 
            enemy.carryingPlayerBody ? "Drop Body" : "Rage Mode";

        public string? GetSecondarySkillName(FlowermanAI _) => "(HOLD) Stand";

        public float InteractRange(FlowermanAI _) => 1.5f;

        public bool SyncAnimationSpeedEnabled(FlowermanAI _) => false;

        public bool CanUseEntranceDoors(FlowermanAI _) => false;
    }
}
