namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for CentipedeAI (Snare Flea).
    /// Abilities: Hide (ceiling), chase mode, cling to players.
    /// 
    public class CentipedeController : IEnemyController<CentipedeAI>
    {
        private enum State
        {
            Searching = 0,
            Hiding = 1,
            Chasing = 2
        }

        public void UsePrimarySkill(CentipedeAI enemy)
        {
            // Drop from ceiling/player
            if (enemy.clingingToPlayer != null)
            {
                enemy.StopClingingServerRpc(false);
            }
        }

        public void OnSecondarySkillHold(CentipedeAI enemy)
        {
            // Enter chase mode
            enemy.SetBehaviourState(State.Chasing);
        }

        public void ReleaseSecondarySkill(CentipedeAI enemy)
        {
            // Return to hiding
            enemy.SetBehaviourState(State.Hiding);
        }

        public bool IsAbleToMove(CentipedeAI enemy) => enemy.clingingToPlayer == null;

        public string? GetPrimarySkillName(CentipedeAI enemy) =>
            enemy.clingingToPlayer != null ? "Release" : null;

        public string? GetSecondarySkillName(CentipedeAI _) => "(HOLD) Chase";

        public float InteractRange(CentipedeAI _) => 1.5f;
    }
}
