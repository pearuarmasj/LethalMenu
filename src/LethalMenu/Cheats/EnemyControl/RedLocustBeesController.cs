namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for RedLocustBees (Circuit Bees).
    /// Abilities: Attack zap mode, return to idle.
    /// 
    public class RedLocustBeesController : IEnemyController<RedLocustBees>
    {
        private enum State
        {
            Idle = 0,
            Defensive = 1,
            Attack = 2
        }

        public void UsePrimarySkill(RedLocustBees enemy)
        {
            // Enter attack zap mode
            enemy.SetBehaviourState(State.Attack);
            enemy.EnterAttackZapModeServerRpc(-1);
        }

        public void UseSecondarySkill(RedLocustBees enemy)
        {
            // Return to idle
            enemy.SetBehaviourState(State.Idle);
        }

        public string? GetPrimarySkillName(RedLocustBees _) => "Attack";

        public string? GetSecondarySkillName(RedLocustBees _) => "Calm Down";

        public bool CanUseEntranceDoors(RedLocustBees _) => true;

        public float InteractRange(RedLocustBees _) => 2.5f;

        public bool SyncAnimationSpeedEnabled(RedLocustBees _) => false;
    }
}

