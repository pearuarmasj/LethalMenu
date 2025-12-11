namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for MouthDogAI (Eyeless Dog).
    /// Abilities: Roam, chase (sprint), lunge attack.
    /// 
    public class MouthDogController : IEnemyController<MouthDogAI>
    {
        private enum State
        {
            Roaming = 0,
            Suspicious = 1,
            Chase = 2,
            Lunge = 3
        }

        public void OnMovement(MouthDogAI enemy, bool isMoving, bool isSprinting)
        {
            if (!isSprinting)
            {
                if (!isMoving) return;
                enemy.SetBehaviourState(State.Roaming);
            }
            else
            {
                enemy.SetBehaviourState(State.Chase);
            }
        }

        public void UseSecondarySkill(MouthDogAI enemy)
        {
            enemy.SetBehaviourState(State.Lunge);
        }

        public string? GetSecondarySkillName(MouthDogAI _) => "Lunge";

        public float InteractRange(MouthDogAI _) => 2.5f;

        public bool CanUseEntranceDoors(MouthDogAI _) => false;
    }
}
