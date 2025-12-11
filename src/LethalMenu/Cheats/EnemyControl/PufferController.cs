namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for PufferAI (Spore Lizard).
    /// Abilities: Release spores.
    /// 
    public class PufferController : IEnemyController<PufferAI>
    {
        private enum State
        {
            Idle = 0,
            Alert = 1,
            Fleeing = 2
        }

        public void UsePrimarySkill(PufferAI enemy)
        {
            // Puff spores
            enemy.ShakeTailServerRpc();
        }

        public void UseSecondarySkill(PufferAI enemy)
        {
            // Toggle alert mode
            if (enemy.IsBehaviourState(State.Alert))
            {
                enemy.SetBehaviourState(State.Idle);
            }
            else
            {
                enemy.SetBehaviourState(State.Alert);
            }
        }

        public string? GetPrimarySkillName(PufferAI _) => "Puff Spores";

        public string? GetSecondarySkillName(PufferAI enemy) =>
            enemy.IsBehaviourState(State.Alert) ? "Relax" : "Alert";

        public float InteractRange(PufferAI _) => 2.0f;
    }
}
