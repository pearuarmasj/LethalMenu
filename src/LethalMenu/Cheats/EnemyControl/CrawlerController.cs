namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for CrawlerAI (Thumper).
    /// Abilities: Wall collision attack, screech.
    /// 
    public class CrawlerController : IEnemyController<CrawlerAI>
    {
        public void UsePrimarySkill(CrawlerAI enemy)
        {
            // Slam into wall
            enemy.CollideWithWallServerRpc();
        }

        public void UseSecondarySkill(CrawlerAI enemy)
        {
            // Screech
            enemy.MakeScreechNoiseServerRpc();
        }

        public string? GetPrimarySkillName(CrawlerAI _) => "Wall Slam";

        public string? GetSecondarySkillName(CrawlerAI _) => "Screech";

        public float InteractRange(CrawlerAI _) => 1.5f;

        public bool SyncAnimationSpeedEnabled(CrawlerAI _) => false;

        public bool CanUseEntranceDoors(CrawlerAI _) => false;
    }
}
