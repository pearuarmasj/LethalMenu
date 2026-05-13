namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for ButlerBeesEnemyAI — passive bee swarm that spawns from butler death.
    /// No skills.
    /// </summary>
    public class ButlerBeesController : IEnemyController<ButlerBeesEnemyAI>
    {
        public bool CanUseEntranceDoors(ButlerBeesEnemyAI _) => false;
        public float InteractRange(ButlerBeesEnemyAI _) => 2.5f;
    }
}
