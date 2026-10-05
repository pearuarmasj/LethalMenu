using System;
using System.Collections.Generic;

namespace LethalMenu.Cheats.Directives
{
    /// One adapter per concrete EnemyAI type. Exact-type lookup, like EnemyControllerRegistry.
    public static class DirectiveAdapterRegistry
    {
        private static readonly Dictionary<Type, IDirectiveAdapter> Adapters = new()
        {
            { typeof(CrawlerAI), new Adapters.CrawlerDirectiveAdapter() },
            { typeof(FlowermanAI), new Adapters.FlowermanDirectiveAdapter() },
        };

        public static IDirectiveAdapter? Get(EnemyAI enemy) =>
            enemy != null && Adapters.TryGetValue(enemy.GetType(), out var adapter) ? adapter : null;
    }
}
