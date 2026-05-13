namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for DoublewingAI (Manticoil).
    /// Passive flying bird. No skills.
    /// </summary>
    public class DoublewingController : IEnemyController<DoublewingAI>
    {
        public bool CanUseEntranceDoors(DoublewingAI _) => false;
        public float InteractRange(DoublewingAI _) => 2.5f;
    }
}
