using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// EntranceTeleport.FindExitPoint() uses FindObjectsOfType<EntranceTeleport>() without
    /// includeInactive. Lethal Company disables interior GameObjects when no client is near
    /// the dungeon, so the matching exit-side entrance is invisible to the default lookup —
    /// FindExitPoint returns false, TeleportPlayer aborts with "The entrance appears to be
    /// blocked." This Prefix replaces the body with an includeInactive-aware search.
    [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.FindExitPoint))]
    internal static class EntranceTeleportFindExitPointPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EntranceTeleport __instance, ref bool __result)
        {
            var array = Object.FindObjectsOfType<EntranceTeleport>(includeInactive: true);
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].isEntranceToBuilding != __instance.isEntranceToBuilding &&
                    array[i].entranceId == __instance.entranceId)
                {
                    __instance.exitScript = array[i];
                }
            }
            __result = __instance.exitScript != null;
            return false;
        }
    }
}
