namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for DressGirlAI (Ghost Girl).
    /// Abilities: Haunt player, teleport.
    /// 
    public class DressGirlController : IEnemyController<DressGirlAI>
    {
        private enum State
        {
            Idle = 0,
            Chasing = 1
        }

        public void UsePrimarySkill(DressGirlAI enemy)
        {
            // Enter chase mode
            enemy.SetBehaviourState(State.Chasing);
        }

        public void UseSecondarySkill(DressGirlAI enemy)
        {
            // Stop chasing
            enemy.SetBehaviourState(State.Idle);
        }

        public void OnReleaseControl(DressGirlAI enemy)
        {
            // Clear haunting when releasing control
            enemy.hauntingPlayer = null;
        }

        public string? GetPrimarySkillName(DressGirlAI _) => "Chase";

        public string? GetSecondarySkillName(DressGirlAI _) => "Stop";

        public float InteractRange(DressGirlAI _) => 3.0f;
    }
}
