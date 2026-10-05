using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Cadaver Growth. A stationary plant colony: Update pins destination to its own position, there is no chase
    /// state and no collision handler. Its only attack is infection. Vanilla InfectPlayers (Update, every
    /// InfectIntervalTime) rolls an infection for the LOCAL player standing among plants and then runs
    /// InfectPlayer(local, severe, spores) + InfectPlayerRpc(id, severe, spores) (NotMe, RequireOwnership false);
    /// the victim's own client progresses the fever and bursts (BurstFromPlayer kills through the Bloom), so the
    /// RPC carries it. Engage does the same for the target, without the dice, once the target is within
    /// InfectionRange of a living plant (non-eradicated tile, plantsInTile > 0) and not already infected. Follow,
    /// Disengage and Release do nothing (it cannot move) and CanUseEntrances is false. Update has no targeting, so
    /// AfterUpdate is a no-op. Side effect: the colony's own growth runs in DoAIInterval, which is skipped while
    /// directed, so it stops spreading until released.
    public sealed class CadaverGrowthDirectiveAdapter : DirectiveAdapter<CadaverGrowthAI>
    {
        private const float InfectionRange = 8f;
        private const bool Severe = true;
        private const bool EmitSpores = true;

        public override void Engage(CadaverGrowthAI enemy, PlayerControllerB target)
        {
            int id = (int)target.playerClientId;
            if (!target.isPlayerControlled || target.isPlayerDead || !target.isInsideFactory) return;
            if (enemy.playerInfections[id].infected) return;
            if (!NearPlant(enemy, target.transform.position)) return;

            enemy.InfectPlayer(target, Severe, EmitSpores);
            enemy.InfectPlayerRpc(id, Severe, EmitSpores);
        }

        public override void AfterUpdate(CadaverGrowthAI enemy, PlayerControllerB? target) { }

        public override void Disengage(CadaverGrowthAI enemy) { }
        public override void Follow(CadaverGrowthAI enemy, Vector3 position) { }
        public override void Release(CadaverGrowthAI enemy) { }

        public override float SightRange(CadaverGrowthAI enemy) => 15f;
        public override float SightAngle(CadaverGrowthAI enemy) => 180f;
        public override bool CanUseEntrances(CadaverGrowthAI enemy) => false;

        private static bool NearPlant(CadaverGrowthAI enemy, Vector3 position)
        {
            float rangeSqr = InfectionRange * InfectionRange;
            foreach (var tile in enemy.GrowthTiles)
            {
                if (tile.eradicated || tile.plantsInTile <= 0) continue;
                foreach (var plant in tile.plantPositions)
                    if ((plant - position).sqrMagnitude < rangeSqr) return true;
            }
            return false;
        }
    }
}
