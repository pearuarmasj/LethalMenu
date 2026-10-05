using System;
using System.Collections.Generic;

namespace LethalMenu.Cheats.Directives
{
    /// One adapter per concrete EnemyAI type. Exact-type lookup, like EnemyControllerRegistry.
    public static class DirectiveAdapterRegistry
    {
        private static readonly Dictionary<Type, IDirectiveAdapter> Adapters = new()
        {
            { typeof(BaboonBirdAI), new Adapters.BaboonBirdDirectiveAdapter() },
            { typeof(BlobAI), new Adapters.BlobDirectiveAdapter() },
            { typeof(BushWolfEnemy), new Adapters.BushWolfDirectiveAdapter() },
            { typeof(ButlerBeesEnemyAI), new Adapters.ButlerBeesDirectiveAdapter() },
            { typeof(ButlerEnemyAI), new Adapters.ButlerDirectiveAdapter() },
            { typeof(CadaverBloomAI), new Adapters.CadaverBloomDirectiveAdapter() },
            { typeof(CadaverGrowthAI), new Adapters.CadaverGrowthDirectiveAdapter() },
            { typeof(CaveDwellerAI), new Adapters.CaveDwellerDirectiveAdapter() },
            { typeof(CentipedeAI), new Adapters.CentipedeDirectiveAdapter() },
            { typeof(ClaySurgeonAI), new Adapters.ClaySurgeonDirectiveAdapter() },
            { typeof(CrawlerAI), new Adapters.CrawlerDirectiveAdapter() },
            { typeof(DocileLocustBeesAI), new Adapters.DocileLocustBeesDirectiveAdapter() },
            { typeof(DoublewingAI), new Adapters.DoublewingDirectiveAdapter() },
            { typeof(DressGirlAI), new Adapters.DressGirlDirectiveAdapter() },
            { typeof(FlowerSnakeEnemy), new Adapters.FlowerSnakeDirectiveAdapter() },
            { typeof(FlowermanAI), new Adapters.FlowermanDirectiveAdapter() },
            { typeof(ForestGiantAI), new Adapters.ForestGiantDirectiveAdapter() },
            { typeof(GiantKiwiAI), new Adapters.GiantKiwiDirectiveAdapter() },
            { typeof(HoarderBugAI), new Adapters.HoarderBugDirectiveAdapter() },
            { typeof(JesterAI), new Adapters.JesterDirectiveAdapter() },
            { typeof(LassoManAI), new Adapters.LassoManDirectiveAdapter() },
            { typeof(MaskedPlayerEnemy), new Adapters.MaskedPlayerEnemyDirectiveAdapter() },
            { typeof(MouthDogAI), new Adapters.MouthDogDirectiveAdapter() },
            { typeof(NutcrackerEnemyAI), new Adapters.NutcrackerDirectiveAdapter() },
            { typeof(PufferAI), new Adapters.PufferDirectiveAdapter() },
            { typeof(PumaAI), new Adapters.PumaDirectiveAdapter() },
            { typeof(RadMechAI), new Adapters.RadMechDirectiveAdapter() },
            { typeof(RedLocustBees), new Adapters.RedLocustBeesDirectiveAdapter() },
            { typeof(SandSpiderAI), new Adapters.SandSpiderDirectiveAdapter() },
            { typeof(SandWormAI), new Adapters.SandWormDirectiveAdapter() },
            { typeof(SpringManAI), new Adapters.SpringManDirectiveAdapter() },
            { typeof(StingrayAI), new Adapters.StingrayDirectiveAdapter() },
        };

        public static IDirectiveAdapter? Get(EnemyAI enemy) =>
            enemy != null && Adapters.TryGetValue(enemy.GetType(), out var adapter) ? adapter : null;
    }
}
