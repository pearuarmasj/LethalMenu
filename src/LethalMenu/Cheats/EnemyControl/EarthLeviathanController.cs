namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Controller for SandWormAI (Earth Leviathan).
    /// Abilities: Emerge from ground.
    /// 
    public class EarthLeviathanController : IEnemyController<SandWormAI>
    {
        private static bool IsEmerged(SandWormAI enemy) => enemy.inEmergingState || enemy.emerged;

        public void UseSecondarySkill(SandWormAI enemy)
        {
            if (IsEmerged(enemy)) return;
            enemy.StartEmergeAnimation();
        }

        public string? GetSecondarySkillName(SandWormAI _) => "Emerge";

        public bool CanUseEntranceDoors(SandWormAI _) => false;

        public float InteractRange(SandWormAI _) => 0.0f;

        public bool SyncAnimationSpeedEnabled(SandWormAI _) => false;
    }
}

