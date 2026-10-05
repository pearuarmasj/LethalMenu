using HarmonyLib;
using LethalMenu.Cheats.Directives;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Old Bird missiles of a directed mech.
    [HarmonyPatch]
    internal static class DirectiveRadMechPatches
    {
        /// A director who is not the host fires missiles that exist on their client only (ShootGunClientRpc
        /// sends nothing from a client). RadMechAI.StartExplosion would explode them locally; send the impact
        /// through SetExplosionServerRpc (RequireOwnership = false) instead, which the host relays to every
        /// client as a real explosion.
        [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.StartExplosion))]
        [HarmonyPrefix]
        private static bool StartExplosionPrefix(RadMechAI __instance, Vector3 explosionPosition, Vector3 forwardRotation)
        {
            if (DirectiveAuthority.IsRealServer || !__instance.IsOwner || !EnemyDirector.IsDirected(__instance)) return true;
            __instance.SetExplosionServerRpc(explosionPosition, forwardRotation);
            return false;
        }

        /// RadMechAI.SetExplosion on the director's client: same effect, sound and blast mark, with the blast
        /// itself reduced to nothing so a directed mech's missile never hurts the player directing it.
        [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.SetExplosion))]
        [HarmonyPrefix]
        private static bool SetExplosionPrefix(RadMechAI __instance, Vector3 explosionPosition, Vector3 forwardRotation)
        {
            if (!EnemyDirector.IsDirected(__instance)) return true;

            Landmine.SpawnExplosion(explosionPosition - forwardRotation * 0.1f, spawnExplosionEffect: true, 0f, 0f, 0, 0f, __instance.explosionPrefab);
            __instance.explosionAudio.transform.position = explosionPosition + Vector3.up * 0.5f;
            RoundManager.PlayRandomClip(__instance.explosionAudio, __instance.largeExplosionSFX);
            if (Vector3.Distance(__instance.previousExplosionPosition, explosionPosition) >= 4f)
                __instance.SpawnBlastMark(explosionPosition, Quaternion.Euler(forwardRotation));
            return false;
        }
    }
}
