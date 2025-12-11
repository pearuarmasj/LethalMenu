namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for NutcrackerEnemyAI.
    /// Abilities: Fire shotgun, sentry mode.
    /// 
    public class NutcrackerController : IEnemyController<NutcrackerEnemyAI>
    {
        private enum State
        {
            Walking = 0,
            Sentry = 1
        }

        private bool _inSentryMode = false;

        public void Update(NutcrackerEnemyAI enemy, bool isAIControlled)
        {
            if (isAIControlled) return;
            if (_inSentryMode) return;
            enemy.SwitchToBehaviourServerRpc((int)State.Walking);
        }

        public void UsePrimarySkill(NutcrackerEnemyAI enemy)
        {
            if (enemy.gun == null) return;
            
            // Lower audio volume to not earrape yourself
            if (enemy.gun is ShotgunItem shotgun)
            {
                shotgun.gunShootAudio.volume = 0.25f;
            }
            
            enemy.FireGunServerRpc();
        }

        public void OnSecondarySkillHold(NutcrackerEnemyAI enemy)
        {
            enemy.SetBehaviourState(State.Sentry);
            _inSentryMode = true;
        }

        public void ReleaseSecondarySkill(NutcrackerEnemyAI enemy)
        {
            enemy.SetBehaviourState(State.Walking);
            _inSentryMode = false;
        }

        public void OnReleaseControl(NutcrackerEnemyAI enemy)
        {
            _inSentryMode = false;
        }

        public bool IsAbleToRotate(NutcrackerEnemyAI enemy) => !enemy.IsBehaviourState(State.Sentry);

        public bool IsAbleToMove(NutcrackerEnemyAI enemy) => !enemy.IsBehaviourState(State.Sentry);

        public string? GetPrimarySkillName(NutcrackerEnemyAI enemy) => 
            enemy.gun == null ? "" : "Fire";

        public string? GetSecondarySkillName(NutcrackerEnemyAI _) => "(HOLD) Sentry Mode";

        public float InteractRange(NutcrackerEnemyAI _) => 1.5f;

        public bool CanUseEntranceDoors(NutcrackerEnemyAI _) => false;
    }
}
