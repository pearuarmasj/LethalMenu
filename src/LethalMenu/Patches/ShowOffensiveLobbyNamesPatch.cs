using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Clears SteamLobbyManager.censorOffensiveLobbyNames so the lobby browser shows uncensored names.
    /// Applied on Awake and on every Update so toggling the cheat takes effect immediately.
    [HarmonyPatch(typeof(SteamLobbyManager))]
    internal static class ShowOffensiveLobbyNamesPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void OnAwake(SteamLobbyManager __instance)
        {
            if (Hack.ShowOffensiveLobbyNames.IsEnabled())
                __instance.censorOffensiveLobbyNames = false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void OnUpdate(SteamLobbyManager __instance)
        {
            if (Hack.ShowOffensiveLobbyNames.IsEnabled())
                __instance.censorOffensiveLobbyNames = false;
        }
    }
}
