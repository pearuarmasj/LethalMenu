using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Re-enables a gift box after it is opened so it can be opened repeatedly.
    [HarmonyPatch(typeof(GiftBoxItem), "OpenGiftBoxServerRpc")]
    internal static class UnlimitedPresentsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GiftBoxItem __instance)
        {
            if (!Hack.UnlimitedPresents.IsEnabled()) return;
            if (__instance == null) return;

            __instance.grabbable = true;
            __instance.grabbableToEnemies = true;

            var trigger = __instance.GetComponentInChildren<InteractTrigger>();
            if (trigger != null)
            {
                trigger.interactable = true;
                trigger.hoverTip = "Open: [E]";
            }
        }
    }
}
