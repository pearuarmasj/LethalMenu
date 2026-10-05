using HarmonyLib;

namespace LethalMenu.Patches
{
    /// HUD-tip once per actual enemy death on every client when EnemyDeathNotification is enabled. Hooks the base
    /// EnemyAI.KillEnemy, which every override calls and which both the owner (KillEnemyOnOwnerClient) and everyone
    /// else (KillEnemyClientRpc) run; the isEnemyDead flip separates real deaths from no-ops (canDie false, destroy path).
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.KillEnemy))]
    internal static class EnemyDeathNotificationPatch
    {
        [HarmonyPrefix]
        private static void Prefix(EnemyAI __instance, out bool __state) => __state = __instance.isEnemyDead;

        [HarmonyPostfix]
        private static void Postfix(EnemyAI __instance, bool __state)
        {
            if (__state || !__instance.isEnemyDead) return;
            if (!Hack.EnemyDeathNotification.IsEnabled()) return;
            var name = __instance.enemyType?.enemyName ?? __instance.GetType().Name;
            HUDManager.Instance?.DisplayTip("Enemy Death", $"{name} killed");
        }
    }
}
