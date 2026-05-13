namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for RadMechAI (Old Bird).
    /// Primary: Fire missile/gun (host only — SetAimingGun + StartShootGun).
    /// </summary>
    public class RadMechController : IEnemyController<RadMechAI>
    {
        public void UsePrimarySkill(RadMechAI enemy)
        {
            if (!enemy.IsHost && !enemy.IsServer) return;
            enemy.SetAimingGun(true);
            enemy.StartShootGun();
            enemy.SetAimingGun(false);
        }

        public string? GetPrimarySkillName(RadMechAI _) => "Fire";
        public string? GetSecondarySkillName(RadMechAI _) => "";

        public bool CanUseEntranceDoors(RadMechAI _) => false;
        public float InteractRange(RadMechAI _) => 10f;
    }
}
