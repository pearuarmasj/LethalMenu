namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for BlobAI (Hygrodere/Slime).
    /// Abilities: Movement speed control.
    /// 
    public class BlobController : IEnemyController<BlobAI>
    {
        private enum State
        {
            Idle = 0,
            Chasing = 1
        }

        public void UseSecondarySkill(BlobAI enemy)
        {
            // Toggle chase mode
            if (enemy.IsBehaviourState(State.Chasing))
            {
                enemy.SetBehaviourState(State.Idle);
            }
            else
            {
                enemy.SetBehaviourState(State.Chasing);
            }
        }

        public string? GetSecondarySkillName(BlobAI enemy) =>
            enemy.IsBehaviourState(State.Chasing) ? "Stop Chase" : "Chase";

        public float InteractRange(BlobAI _) => 1.5f;
    }
}
