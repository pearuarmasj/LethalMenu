namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for PufferAI (Spore Lizard).
    /// Primary: Stomp (intimidate + damage close-range).
    /// Secondary: Release spore smoke.
    /// </summary>
    public class PufferController : IEnemyController<PufferAI>
    {
        private enum State
        {
            Idle = 0,
            Alerted = 1,
            Hostile = 2
        }

        public void UsePrimarySkill(PufferAI enemy)
        {
            enemy.SetBehaviourState(State.Hostile);
            enemy.StompServerRpc();
        }

        public void UseSecondarySkill(PufferAI enemy)
        {
            enemy.SetBehaviourState(State.Hostile);
            enemy.ShakeTailServerRpc();
        }

        public string? GetPrimarySkillName(PufferAI _) => "Stomp";
        public string? GetSecondarySkillName(PufferAI _) => "Smoke";

        public float InteractRange(PufferAI _) => 2.5f;
        public bool CanUseEntranceDoors(PufferAI _) => false;
    }
}
