using UnityEngine;

namespace LethalMenu.Cheats.Directives
{
    public static class DirectiveRouting
    {
        /// True while the enemy is being routed to the other side of the facility wall.
        public static bool Route(EnemyAI enemy, IDirectiveAdapter adapter, Vector3 goal, bool goalOutside) => false;

        public static void Forget(EnemyAI enemy) { }
    }
}
