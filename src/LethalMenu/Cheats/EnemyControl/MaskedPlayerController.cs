namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for MaskedPlayerEnemy.
    /// Primary: Toggle "Hands Out" (zombie-style grab pose).
    /// Secondary: Toggle crouching (sneak pose).
    /// </summary>
    public class MaskedPlayerController : IEnemyController<MaskedPlayerEnemy>
    {
        public void UsePrimarySkill(MaskedPlayerEnemy enemy)
            => enemy.SetHandsOutServerRpc(!enemy.creatureAnimator.GetBool("HandsOut"));

        public void UseSecondarySkill(MaskedPlayerEnemy enemy)
            => enemy.SetCrouchingServerRpc(!enemy.creatureAnimator.GetBool("Crouching"));

        public string? GetPrimarySkillName(MaskedPlayerEnemy enemy)
            => enemy.creatureAnimator.GetBool("HandsOut") ? "Hands In" : "Hands Out";

        public string? GetSecondarySkillName(MaskedPlayerEnemy enemy)
            => enemy.creatureAnimator.GetBool("Crouching") ? "Stand" : "Crouch";

        public float InteractRange(MaskedPlayerEnemy _) => 1.0f;
        public bool SyncAnimationSpeedEnabled(MaskedPlayerEnemy _) => false;
        public bool CanUseEntranceDoors(MaskedPlayerEnemy _) => true;
    }
}
