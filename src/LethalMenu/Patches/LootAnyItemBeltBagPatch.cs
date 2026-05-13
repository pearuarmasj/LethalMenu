using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Forces the belt bag to accept any GrabbableObject by flipping isScrap=true during TryAddObjectToBag.
    [HarmonyPatch(typeof(BeltBagItem), "TryAddObjectToBag")]
    internal static class LootAnyItemBeltBagPatch
    {
        private static bool _flipped;

        [HarmonyPrefix]
        private static void Prefix(GrabbableObject gObject)
        {
            _flipped = false;
            if (!Hack.LootAnyItemBeltBag.IsEnabled()) return;
            if (gObject == null || gObject.itemProperties == null) return;
            if (!gObject.itemProperties.isScrap)
            {
                gObject.itemProperties.isScrap = true;
                _flipped = true;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(GrabbableObject gObject)
        {
            if (!_flipped) return;
            if (gObject != null && gObject.itemProperties != null)
                gObject.itemProperties.isScrap = false;
            _flipped = false;
        }
    }
}
