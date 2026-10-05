using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LethalMenu.Cheats.Directives;

namespace LethalMenu.Patches
{
    /// Every concrete enemy type's most-derived declaration of `name` (EnemyAI's own when not overridden).
    internal static class EnemyMethods
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static IEnumerable<MethodBase> MostDerived(string name) =>
            typeof(EnemyAI).Assembly.GetTypes()
                .Where(t => typeof(EnemyAI).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => (MethodBase)t.GetMethod(name, Instance, null, Type.EmptyTypes, null))
                .Where(m => m != null && !m.IsAbstract)
                .Distinct();
    }

    /// Directed enemies skip their vanilla AI tick; EnemyDirector.Tick runs instead. Patched on every
    /// override because a prefix on EnemyAI.DoAIInterval alone only intercepts the base call.
    [HarmonyPatch]
    internal static class DirectiveIntervalPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() => EnemyMethods.MostDerived(nameof(EnemyAI.DoAIInterval));

        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance)
        {
            if (!EnemyDirector.IsDirected(__instance)) return true;
            EnemyDirector.Tick(__instance);
            return false;
        }
    }

    /// Lets the adapter undo per-frame re-targeting the vanilla Update did. Overrides call base.Update(),
    /// so a postfix on EnemyAI.Update would also fire mid-frame for them; only the instance's
    /// most-derived Update triggers AfterUpdate.
    [HarmonyPatch]
    internal static class DirectiveUpdatePatch
    {
        private static readonly Dictionary<Type, MethodBase> MostDerivedUpdate = new();

        private static IEnumerable<MethodBase> TargetMethods() => EnemyMethods.MostDerived(nameof(EnemyAI.Update));

        [HarmonyPostfix]
        private static void Postfix(EnemyAI __instance, MethodBase __originalMethod)
        {
            if (!EnemyDirector.IsDirected(__instance)) return;
            var type = __instance.GetType();
            if (!MostDerivedUpdate.TryGetValue(type, out var update))
            {
                update = type.GetMethod(nameof(EnemyAI.Update), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!;
                MostDerivedUpdate[type] = update;
            }
            if (__originalMethod != update) return;
            EnemyDirector.AfterUpdate(__instance);
        }
    }

    /// Many enemies hand ownership to the player they chase (ChangeOwnershipOfEnemy). A directed enemy
    /// must stay owned by the director's client.
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.ChangeOwnershipOfEnemy))]
    internal static class DirectiveOwnershipPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance) => !EnemyDirector.IsDirected(__instance);
    }
}
