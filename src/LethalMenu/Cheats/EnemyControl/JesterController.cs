namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for JesterAI.
    /// Abilities: Close box, crank, pop out.
    /// 
    public class JesterController : IEnemyController<JesterAI>
    {
        private enum State
        {
            Closed = 0,
            Cranking = 1,
            Open = 2
        }

        private void SetNoPlayerChaseTimer(JesterAI enemy, float value)
        {
            enemy.SetPrivateField("noPlayersToChaseTimer", value);
        }

        public void UsePrimarySkill(JesterAI enemy)
        {
            // Close the box
            enemy.SetBehaviourState(State.Closed);
            SetNoPlayerChaseTimer(enemy, 0.0f);
        }

        public void OnSecondarySkillHold(JesterAI enemy)
        {
            if (!enemy.IsBehaviourState(State.Closed)) return;
            enemy.SetBehaviourState(State.Cranking);
        }

        public void ReleaseSecondarySkill(JesterAI enemy)
        {
            if (!enemy.IsBehaviourState(State.Cranking)) return;
            enemy.SetBehaviourState(State.Open);
        }

        public void Update(JesterAI enemy, bool isAIControlled)
        {
            // Keep timer high so it doesn't auto-revert
            SetNoPlayerChaseTimer(enemy, 100.0f);
        }

        public void OnReleaseControl(JesterAI enemy)
        {
            SetNoPlayerChaseTimer(enemy, 5.0f);
        }

        public bool IsAbleToMove(JesterAI enemy) => !enemy.IsBehaviourState(State.Cranking);

        public bool IsAbleToRotate(JesterAI enemy) => !enemy.IsBehaviourState(State.Cranking);

        public string? GetPrimarySkillName(JesterAI _) => "Close Box";

        public string? GetSecondarySkillName(JesterAI _) => "(HOLD) Crank";

        public float InteractRange(JesterAI _) => 1.0f;

        public bool CanUseEntranceDoors(JesterAI _) => false;
    }
}
