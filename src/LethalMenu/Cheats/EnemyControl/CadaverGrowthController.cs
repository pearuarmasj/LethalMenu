namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for CadaverGrowthAI — active spawn from cadaver blooms.
    /// Primary: Cough spores (noise + AOE infection trigger near other players).
    /// </summary>
    public class CadaverGrowthController : IEnemyController<CadaverGrowthAI>
    {
        public void UsePrimarySkill(CadaverGrowthAI enemy)
        {
            var local = LethalMenuMod.LocalPlayer;
            if (local == null) return;
            enemy.CoughSporesRpc(0.8f, (int)local.playerClientId);
        }

        public string? GetPrimarySkillName(CadaverGrowthAI _) => "Cough Spores";
        public string? GetSecondarySkillName(CadaverGrowthAI _) => "";

        public bool CanUseEntranceDoors(CadaverGrowthAI _) => false;
        public float InteractRange(CadaverGrowthAI _) => 2.5f;
    }
}
