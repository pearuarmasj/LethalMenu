namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for MaskedPlayerEnemy.
    /// Abilities: Mimic player behavior, chase.
    /// 
    public class MaskedPlayerController : IEnemyController<MaskedPlayerEnemy>
    {
        private enum State
        {
            Idle = 0,
            Roaming = 1,
            Chasing = 2
        }

        public void UsePrimarySkill(MaskedPlayerEnemy enemy)
        {
            // Enter chase mode
            enemy.SetBehaviourState(State.Chasing);
        }

        public void UseSecondarySkill(MaskedPlayerEnemy enemy)
        {
            // Toggle roaming
            if (enemy.IsBehaviourState(State.Roaming))
            {
                enemy.SetBehaviourState(State.Idle);
            }
            else
            {
                enemy.SetBehaviourState(State.Roaming);
            }
        }

        public string? GetPrimarySkillName(MaskedPlayerEnemy _) => "Chase";

        public string? GetSecondarySkillName(MaskedPlayerEnemy enemy) =>
            enemy.IsBehaviourState(State.Roaming) ? "Stop" : "Roam";

        public float InteractRange(MaskedPlayerEnemy _) => 2.0f;
    }
}
