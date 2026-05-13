namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for DocileLocustBeesAI — friendly amber swarm that escorts Manticoils.
    /// Passive. No skills.
    /// </summary>
    public class DocileLocustBeesController : IEnemyController<DocileLocustBeesAI>
    {
        public bool CanUseEntranceDoors(DocileLocustBeesAI _) => false;
        public float InteractRange(DocileLocustBeesAI _) => 2.5f;
        public bool SyncAnimationSpeedEnabled(DocileLocustBeesAI _) => false;
    }
}
