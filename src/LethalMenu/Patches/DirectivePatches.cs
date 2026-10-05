using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LethalMenu.Cheats.Directives;

namespace LethalMenu.Patches
{
    /// Every declaration of `name` in the EnemyAI hierarchy (EnemyAI's own plus each override), looked up
    /// with DeclaredOnly. Type.GetMethod without it returns an inherited method whose ReflectedType is the
    /// derived type; Harmony refuses those ("You can only patch implemented methods"), which fails the
    /// whole patch class, and such MethodInfos never compare equal to the declaring type's own.
    internal static class EnemyMethods
    {
        private const BindingFlags Declared =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static IEnumerable<MethodBase> AllDeclared(string name) =>
            AccessTools.GetTypesFromAssembly(typeof(EnemyAI).Assembly)
                .Where(t => typeof(EnemyAI).IsAssignableFrom(t))
                .Select(t => t.GetMethod(name, Declared, null, Type.EmptyTypes, null))
                .Where(m => m != null && !m.IsAbstract)
                .Cast<MethodBase>();

        /// The type whose declaration of `name` runs first for an instance of `type` (the outermost override).
        public static Type OutermostDeclarer(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                if (t.GetMethod(name, Declared, null, Type.EmptyTypes, null) != null)
                    return t;
            }
            return typeof(EnemyAI);
        }
    }

    /// Directed enemies skip their vanilla AI tick; EnemyDirector.Tick runs instead. Patched on every
    /// declaration: the outermost override's prefix returns false, so nothing below it (including
    /// base.DoAIInterval) runs and Tick fires once per interval.
    [HarmonyPatch]
    internal static class DirectiveIntervalPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() => EnemyMethods.AllDeclared(nameof(EnemyAI.DoAIInterval));

        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance)
        {
            if (!EnemyDirector.IsDirected(__instance)) return true;
            EnemyDirector.Tick(__instance);
            return false;
        }
    }

    /// Lets the adapter undo per-frame re-targeting the vanilla Update did. Overrides call base.Update(),
    /// so the postfix fires once per hierarchy level; only the outermost declaration's postfix (which
    /// finishes last) triggers AfterUpdate.
    [HarmonyPatch]
    internal static class DirectiveUpdatePatch
    {
        private static readonly Dictionary<Type, Type> Outermost = new();

        private static IEnumerable<MethodBase> TargetMethods() => EnemyMethods.AllDeclared(nameof(EnemyAI.Update));

        [HarmonyPostfix]
        private static void Postfix(EnemyAI __instance, MethodBase __originalMethod)
        {
            if (!EnemyDirector.IsDirected(__instance)) return;
            var type = __instance.GetType();
            if (!Outermost.TryGetValue(type, out var declarer))
            {
                declarer = EnemyMethods.OutermostDeclarer(type, nameof(EnemyAI.Update));
                Outermost[type] = declarer;
            }
            if (__originalMethod.DeclaringType != declarer) return;
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
