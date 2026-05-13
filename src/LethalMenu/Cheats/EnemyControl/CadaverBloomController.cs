namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for CadaverBloomAI — rooted plant emitter.
    /// Stub: possessable but cannot move. Player observes the world from the bloom.
    /// </summary>
    public class CadaverBloomController : IEnemyController<CadaverBloomAI>
    {
        public bool IsAbleToMove(CadaverBloomAI _) => false;
        public bool IsAbleToRotate(CadaverBloomAI _) => false;
        public bool CanUseEntranceDoors(CadaverBloomAI _) => false;
        public float InteractRange(CadaverBloomAI _) => 2.5f;
    }
}
