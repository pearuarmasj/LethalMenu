using HarmonyLib;
using Unity.Netcode;

namespace LethalMenu.Patches
{
    /// Vanilla bug: a HoarderBug killed without going through KillEnemy() (e.g. crushed by a
    /// hazard, server-side kill RPC) keeps its heldItem reference. The carried scrap stays
    /// parented to the dead bug and is unreachable. This Prefix on HitEnemy detects the
    /// dead-but-still-holding state and force-drops the item.
    [HarmonyPatch(typeof(HoarderBugAI), nameof(HoarderBugAI.HitEnemy))]
    internal static class HoarderBugAIFixPatch
    {
        [HarmonyPrefix]
        private static void Prefix(HoarderBugAI __instance)
        {
            if (!__instance.isEnemyDead) return;
            if (__instance.heldItem == null) return;
            if (__instance.heldItem.itemGrabbableObject == null) return;
            if (!__instance.heldItem.itemGrabbableObject.TryGetComponent(out NetworkObject networkObject)) return;

            __instance.DropItemAndCallDropRPC(networkObject, droppedInNest: false);
        }
    }
}
