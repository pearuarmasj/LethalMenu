using LethalMenu.Util;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for CentipedeAI (Snare Flea).
    /// Primary: Drop from ceiling (HIDING → CHASING).
    /// Secondary: Attach to ceiling (raycast + HIDING state).
    /// Movement gated while clinging to anything.
    /// </summary>
    public class CentipedeController : IEnemyController<CentipedeAI>
    {
        private enum State
        {
            Searching = 0,
            Hiding = 1,
            Chasing = 2,
            Clinging = 3
        }

        public void UsePrimarySkill(CentipedeAI enemy)
        {
            if (!enemy.IsBehaviourState(State.Hiding)) return;
            enemy.SetBehaviourState(State.Chasing);
        }

        public void UseSecondarySkill(CentipedeAI enemy)
        {
            if (IsClingingToSomething(enemy)) return;
            enemy.Reflect().Invoke("RaycastToCeiling");
            enemy.SetBehaviourState(State.Hiding);
        }

        public bool IsAbleToMove(CentipedeAI enemy) => !IsClingingToSomething(enemy);
        public bool IsAbleToRotate(CentipedeAI enemy) => !IsClingingToSomething(enemy);

        public string? GetPrimarySkillName(CentipedeAI _) => "Drop";
        public string? GetSecondarySkillName(CentipedeAI _) => "Attach to Ceiling";

        public float InteractRange(CentipedeAI _) => 1.5f;
        public bool CanUseEntranceDoors(CentipedeAI _) => false;
        public bool SyncAnimationSpeedEnabled(CentipedeAI _) => false;

        private static bool IsClingingToSomething(CentipedeAI enemy)
        {
            var reflector = enemy.Reflect();
            return enemy.clingingToPlayer != null
                || enemy.inSpecialAnimation
                || reflector.GetField<bool>("clingingToDeadBody")
                || reflector.GetField<bool>("clingingToCeiling")
                || reflector.GetField<bool>("startedCeilingAnimationCoroutine")
                || reflector.GetField<bool>("inDroppingOffPlayerAnim");
        }
    }
}
