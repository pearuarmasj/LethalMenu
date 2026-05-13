using System;
using System.Collections.Generic;

namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Registry of enemy controllers.
    /// Maps EnemyAI types to their specific controllers.
    /// 
    public static class EnemyControllerRegistry
    {
        private static readonly Dictionary<Type, IEnemyController> Controllers = new()
        {
            { typeof(FlowermanAI), new FlowermanController() },
            { typeof(ForestGiantAI), new ForestGiantController() },
            { typeof(NutcrackerEnemyAI), new NutcrackerController() },
            { typeof(JesterAI), new JesterController() },
            { typeof(MouthDogAI), new MouthDogController() },
            { typeof(CrawlerAI), new CrawlerController() },
            { typeof(SpringManAI), new SpringManController() },
            { typeof(CentipedeAI), new CentipedeController() },
            { typeof(BaboonBirdAI), new BaboonBirdController() },
            { typeof(HoarderBugAI), new HoarderBugController() },
            { typeof(BlobAI), new BlobController() },
            { typeof(PufferAI), new PufferController() },
            { typeof(SandSpiderAI), new SandSpiderController() },
            { typeof(MaskedPlayerEnemy), new MaskedPlayerController() },
            { typeof(DressGirlAI), new DressGirlController() },
            { typeof(ButlerEnemyAI), new ButlerController() },
            // New controllers
            { typeof(RedLocustBees), new RedLocustBeesController() },
            { typeof(SandWormAI), new EarthLeviathanController() },
            { typeof(LassoManAI), new LassoManController() },
            { typeof(ButlerBeesEnemyAI), new ButlerBeesController() },
            { typeof(CaveDwellerAI), new CaveDwellerController() },
            { typeof(ClaySurgeonAI), new ClaySurgeonController() },
            { typeof(DocileLocustBeesAI), new DocileLocustBeesController() },
            { typeof(DoublewingAI), new DoublewingController() },
            { typeof(FlowerSnakeEnemy), new FlowerSnakeController() },
            { typeof(RadMechAI), new RadMechController() },
            { typeof(GiantKiwiAI), new GiantKiwiController() },
            { typeof(CadaverBloomAI), new CadaverBloomController() },
            { typeof(CadaverGrowthAI), new CadaverGrowthController() },
            { typeof(StingrayAI), new StingrayController() },
            { typeof(PumaAI), new PumaController() },
        };

        /// 
        /// Get the controller for a specific enemy type.
        /// 
        public static IEnemyController? GetController(EnemyAI enemy)
        {
            if (enemy == null) return null;
            
            var enemyType = enemy.GetType();
            if (Controllers.TryGetValue(enemyType, out var controller))
            {
                return controller;
            }

            return null;
        }

        /// 
        /// Check if we have a controller for this enemy type.
        /// 
        public static bool HasController(EnemyAI enemy)
        {
            if (enemy == null) return false;
            return Controllers.ContainsKey(enemy.GetType());
        }

        /// 
        /// Get all supported enemy type names.
        /// 
        public static IEnumerable<string> GetSupportedEnemyTypes()
        {
            foreach (var type in Controllers.Keys)
            {
                yield return type.Name;
            }
        }

        /// 
        /// Get count of supported enemy types.
        /// 
        public static int SupportedCount => Controllers.Count;
    }
}
