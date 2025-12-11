using System;

namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Base interface for all enemy controllers.
    /// Defines the contract for controlling different enemy types.
    /// 
    public interface IEnemyController
    {
        /// Default sprint speed multiplier.
        const float DefaultSprintMultiplier = 2.8f;

        /// Default interaction range.
        const float DefaultInteractRange = 2.5f;

        /// Called when taking control of an enemy.
        void OnTakeControl(EnemyAI enemy);

        /// Called when releasing control of an enemy.
        void OnReleaseControl(EnemyAI enemy);

        /// Called when the controlled enemy dies.
        void OnDeath(EnemyAI enemy);

        /// Called every frame while controlling the enemy.
        void Update(EnemyAI enemy, bool isAIControlled);

        /// Execute primary skill (left click).
        void UsePrimarySkill(EnemyAI enemy);

        /// Called while secondary skill button is held.
        void OnSecondarySkillHold(EnemyAI enemy);

        /// Execute secondary skill (right click).
        void UseSecondarySkill(EnemyAI enemy);

        /// Called when secondary skill button is released.
        void ReleaseSecondarySkill(EnemyAI enemy);

        /// Called when enemy moves.
        void OnMovement(EnemyAI enemy, bool isMoving, bool isSprinting);

        /// Whether the enemy can move in current state.
        bool IsAbleToMove(EnemyAI enemy);

        /// Whether the enemy can rotate in current state.
        bool IsAbleToRotate(EnemyAI enemy);

        /// Whether the enemy can use entrance doors.
        bool CanUseEntranceDoors(EnemyAI enemy);

        /// Get display name for primary skill.
        string? GetPrimarySkillName(EnemyAI enemy);

        /// Get display name for secondary skill.
        string? GetSecondarySkillName(EnemyAI enemy);

        /// Interaction range for this enemy type.
        float InteractRange(EnemyAI enemy);

        /// Sprint speed multiplier for this enemy type.
        float SprintMultiplier(EnemyAI enemy);

        /// Whether to sync animation speed with movement.
        bool SyncAnimationSpeedEnabled(EnemyAI enemy);
    }

    /// 
    /// Generic enemy controller interface with type-safe methods.
    /// Inherit from this for specific enemy types.
    /// 
    public interface IEnemyController<T> : IEnemyController where T : EnemyAI
    {
        // Default implementations for type-safe versions
        void OnTakeControl(T enemy) { }
        void OnReleaseControl(T enemy) { }
        void OnDeath(T enemy) { }
        void Update(T enemy, bool isAIControlled) { }
        void UsePrimarySkill(T enemy) { }
        void OnSecondarySkillHold(T enemy) { }
        void UseSecondarySkill(T enemy) { }
        void ReleaseSecondarySkill(T enemy) { }
        void OnMovement(T enemy, bool isMoving, bool isSprinting) { }
        bool IsAbleToMove(T enemy) => true;
        bool IsAbleToRotate(T enemy) => true;
        bool CanUseEntranceDoors(T enemy) => true;
        string? GetPrimarySkillName(T enemy) => null;
        string? GetSecondarySkillName(T enemy) => null;
        float InteractRange(T enemy) => IEnemyController.DefaultInteractRange;
        float SprintMultiplier(T enemy) => IEnemyController.DefaultSprintMultiplier;
        bool SyncAnimationSpeedEnabled(T enemy) => true;

        // Explicit interface implementations that delegate to type-safe versions
        void IEnemyController.OnTakeControl(EnemyAI enemy) => OnTakeControl((T)enemy);
        void IEnemyController.OnReleaseControl(EnemyAI enemy) => OnReleaseControl((T)enemy);
        void IEnemyController.OnDeath(EnemyAI enemy) => OnDeath((T)enemy);
        void IEnemyController.Update(EnemyAI enemy, bool isAIControlled) => Update((T)enemy, isAIControlled);
        void IEnemyController.UsePrimarySkill(EnemyAI enemy) => UsePrimarySkill((T)enemy);
        void IEnemyController.OnSecondarySkillHold(EnemyAI enemy) => OnSecondarySkillHold((T)enemy);
        void IEnemyController.UseSecondarySkill(EnemyAI enemy) => UseSecondarySkill((T)enemy);
        void IEnemyController.ReleaseSecondarySkill(EnemyAI enemy) => ReleaseSecondarySkill((T)enemy);
        void IEnemyController.OnMovement(EnemyAI enemy, bool isMoving, bool isSprinting) => OnMovement((T)enemy, isMoving, isSprinting);
        bool IEnemyController.IsAbleToMove(EnemyAI enemy) => IsAbleToMove((T)enemy);
        bool IEnemyController.IsAbleToRotate(EnemyAI enemy) => IsAbleToRotate((T)enemy);
        bool IEnemyController.CanUseEntranceDoors(EnemyAI enemy) => CanUseEntranceDoors((T)enemy);
        string? IEnemyController.GetPrimarySkillName(EnemyAI enemy) => GetPrimarySkillName((T)enemy);
        string? IEnemyController.GetSecondarySkillName(EnemyAI enemy) => GetSecondarySkillName((T)enemy);
        float IEnemyController.InteractRange(EnemyAI enemy) => InteractRange((T)enemy);
        float IEnemyController.SprintMultiplier(EnemyAI enemy) => SprintMultiplier((T)enemy);
        bool IEnemyController.SyncAnimationSpeedEnabled(EnemyAI enemy) => SyncAnimationSpeedEnabled((T)enemy);
    }
}
