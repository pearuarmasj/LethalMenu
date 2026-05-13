using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Snaps the target object to the bag holder before TryAddObjectToBag's LOS/distance check passes.
    /// Sibling to LootAnyItemBeltBagPatch. Restores position via postfix.
    [HarmonyPatch(typeof(BeltBagItem), "TryAddObjectToBag")]
    internal static class LootThroughWallsBeltBagPatch
    {
        private static UnityEngine.Vector3 _originalPosition;
        private static bool _moved;

        [HarmonyPrefix]
        private static void Prefix(BeltBagItem __instance, GrabbableObject gObject)
        {
            _moved = false;
            if (!Hack.LootThroughWallsBeltBag.IsEnabled()) return;
            if (gObject == null || __instance == null || __instance.playerHeldBy == null) return;

            _originalPosition = gObject.transform.position;
            gObject.transform.position = __instance.playerHeldBy.transform.position;
            _moved = true;
        }

        [HarmonyPostfix]
        private static void Postfix(GrabbableObject gObject)
        {
            if (!_moved || gObject == null) return;
            gObject.transform.position = _originalPosition;
            _moved = false;
        }
    }
}
