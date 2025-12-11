namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for LassoManAI.
    /// Abilities: Screech attack.
    /// 
    public class LassoManController : IEnemyController<LassoManAI>
    {
        public void UsePrimarySkill(LassoManAI enemy)
        {
            enemy.MakeScreechNoiseServerRpc();
        }

        public string? GetPrimarySkillName(LassoManAI _) => "Screech";

        public bool SyncAnimationSpeedEnabled(LassoManAI _) => false;
    }
}

