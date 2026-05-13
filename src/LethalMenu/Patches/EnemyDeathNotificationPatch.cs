using HarmonyLib;

namespace LethalMenu.Patches
{
    /// HUD-tip on every enemy death when EnemyDeathNotification is enabled.
    [HarmonyPatch(typeof(EnemyAI), "KillEnemyOnOwnerClient")]
    internal static class EnemyDeathNotificationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EnemyAI __instance)
        {
            if (!Hack.EnemyDeathNotification.IsEnabled()) return;
            if (__instance == null) return;
            var name = __instance.enemyType?.enemyName ?? __instance.GetType().Name;
            HUDManager.Instance?.DisplayTip("Enemy Death", $"{name} killed");
        }
    }
}
