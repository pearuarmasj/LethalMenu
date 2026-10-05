using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives
{
    /// Per-enemy-type driver used by EnemyDirector while an enemy is directed. The director has already
    /// skipped the vanilla DoAIInterval and owns the enemy when any of these run.
    public interface IDirectiveAdapter
    {
        /// Non-hostile movement towards a point (escort follow ring).
        void Follow(EnemyAI enemy, Vector3 position);
        /// Pursue and attack the target using the enemy's real attack path. Called every AI interval.
        void Engage(EnemyAI enemy, PlayerControllerB target);
        /// Leave the attack state without releasing the directive (escort target lost).
        void Disengage(EnemyAI enemy);
        /// After the enemy's vanilla Update ran this frame: undo any re-targeting/state reset it did.
        void AfterUpdate(EnemyAI enemy, PlayerControllerB? target);
        /// Directive removed: restore a clean vanilla state so the normal AI can resume.
        void Release(EnemyAI enemy);
        float SightRange(EnemyAI enemy);
        float SightAngle(EnemyAI enemy);
        bool CanUseEntrances(EnemyAI enemy);
    }

    /// Typed base. Adapters derive from this; the explicit interface methods do the cast.
    public abstract class DirectiveAdapter<T> : IDirectiveAdapter where T : EnemyAI
    {
        public abstract void Follow(T enemy, Vector3 position);
        public abstract void Engage(T enemy, PlayerControllerB target);
        public abstract void Disengage(T enemy);
        public abstract void Release(T enemy);

        /// Default: keep the vanilla chase pointed at our target.
        public virtual void AfterUpdate(T enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.targetPlayer = target;
            enemy.movingTowardsTargetPlayer = true;
        }

        public virtual float SightRange(T enemy) => 40f;
        public virtual float SightAngle(T enemy) => 60f;
        public virtual bool CanUseEntrances(T enemy) => true;

        void IDirectiveAdapter.Follow(EnemyAI enemy, Vector3 position) => Follow((T)enemy, position);
        void IDirectiveAdapter.Engage(EnemyAI enemy, PlayerControllerB target) => Engage((T)enemy, target);
        void IDirectiveAdapter.Disengage(EnemyAI enemy) => Disengage((T)enemy);
        void IDirectiveAdapter.AfterUpdate(EnemyAI enemy, PlayerControllerB? target) => AfterUpdate((T)enemy, target);
        void IDirectiveAdapter.Release(EnemyAI enemy) => Release((T)enemy);
        float IDirectiveAdapter.SightRange(EnemyAI enemy) => SightRange((T)enemy);
        float IDirectiveAdapter.SightAngle(EnemyAI enemy) => SightAngle((T)enemy);
        bool IDirectiveAdapter.CanUseEntrances(EnemyAI enemy) => CanUseEntrances((T)enemy);
    }
}
