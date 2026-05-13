using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for GiantKiwiAI.
    /// Primary: Attack nearest player as a threat.
    /// Secondary: Toggle pecking animation.
    /// </summary>
    public class GiantKiwiController : IEnemyController<GiantKiwiAI>
    {
        private bool _pecking;

        public void UsePrimarySkill(GiantKiwiAI enemy)
        {
            var target = FindNearestPlayer(enemy);
            if (target == null) return;
            if (!target.TryGetComponent(out NetworkObject targetNet)) return;

            var localId = (int)(LethalMenuMod.LocalPlayer?.playerClientId ?? 0);
            enemy.StartAttackingThreatServerRpc(
                new NetworkObjectReference(targetNet),
                localId,
                System.Array.Empty<int>(),
                -1);
        }

        public void UseSecondarySkill(GiantKiwiAI enemy)
        {
            _pecking = !_pecking;
            enemy.PeckTreeServerRpc(_pecking);
        }

        public string? GetPrimarySkillName(GiantKiwiAI _) => "Attack Threat";
        public string? GetSecondarySkillName(GiantKiwiAI _) => _pecking ? "Stop Pecking" : "Peck";

        public bool CanUseEntranceDoors(GiantKiwiAI _) => false;
        public float InteractRange(GiantKiwiAI _) => 6f;

        private static PlayerControllerB? FindNearestPlayer(GiantKiwiAI enemy)
        {
            if (StartOfRound.Instance == null) return null;
            return StartOfRound.Instance.allPlayerScripts
                .Where(p => p != null && p.isPlayerControlled && !p.isPlayerDead)
                .OrderBy(p => Vector3.Distance(enemy.transform.position, p.transform.position))
                .FirstOrDefault();
        }
    }
}
