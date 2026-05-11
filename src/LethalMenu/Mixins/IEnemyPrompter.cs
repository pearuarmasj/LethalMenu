using UnityEngine;

namespace LethalMenu.Mixins
{
    public interface IEnemyPrompter { }

    public static class EnemyPrompterMixin
    {
        public static void LureAllEnemies(this IEnemyPrompter _, Vector3 target)
        {
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                enemy.SetDestinationToPosition(target);
            }
        }

        public static void KillAllEnemies(this IEnemyPrompter _)
        {
            var player = LethalMenuMod.LocalPlayer;
            if (player == null) return;

            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                enemy.ChangeEnemyOwnerServerRpc(player.actualClientId);
                enemy.KillEnemyServerRpc(true);
            }
        }

        public static void StunAllEnemies(this IEnemyPrompter _, float duration = 5f)
        {
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                enemy.SetEnemyStunned(true, duration);
            }
        }
    }
}
