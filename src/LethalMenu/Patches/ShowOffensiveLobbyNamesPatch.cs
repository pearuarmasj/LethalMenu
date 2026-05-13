using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Clears SteamLobbyManager.censorOffensiveLobbyNames every Update so the lobby browser
    /// shows uncensored names. Update fires every frame so toggling the cheat takes effect immediately.
    /// Note: SteamLobbyManager has no Awake; OnEnable + Update are the lifecycle hooks.
    [HarmonyPatch(typeof(SteamLobbyManager), "Update")]
    internal static class ShowOffensiveLobbyNamesPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SteamLobbyManager __instance)
        {
            if (Hack.ShowOffensiveLobbyNames.IsEnabled())
                __instance.censorOffensiveLobbyNames = false;
        }
    }
}
