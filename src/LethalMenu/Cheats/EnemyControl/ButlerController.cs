namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for ButlerEnemyAI.
    /// Abilities: Knife attack, sweep mode.
    /// 
    public class ButlerController : IEnemyController<ButlerEnemyAI>
    {
        private enum State
        {
            Sweeping = 0,
            PreMurder = 1,
            Murder = 2
        }

        public void UsePrimarySkill(ButlerEnemyAI enemy)
        {
            // Enter murder mode
            enemy.SetBehaviourState(State.Murder);
        }

        public void UseSecondarySkill(ButlerEnemyAI enemy)
        {
            // Return to sweeping
            enemy.SetBehaviourState(State.Sweeping);
        }

        public bool IsAbleToMove(ButlerEnemyAI enemy) => !enemy.IsBehaviourState(State.Murder);

        public string? GetPrimarySkillName(ButlerEnemyAI _) => "Attack";

        public string? GetSecondarySkillName(ButlerEnemyAI _) => "Sweep";

        public float InteractRange(ButlerEnemyAI _) => 1.5f;
    }
}
