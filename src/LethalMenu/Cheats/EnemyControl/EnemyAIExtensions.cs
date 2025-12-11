using System;
using System.Reflection;

namespace LethalMenu.Cheats.EnemyControl
{
    /// 
    /// Extension methods for EnemyAI to support enemy control system.
    /// 
    public static class EnemyAIExtensions
    {
        /// 
        /// Check if enemy is in a specific behaviour state.
        /// 
        public static bool IsBehaviourState(this EnemyAI enemy, Enum state)
        {
            return enemy.currentBehaviourStateIndex == Convert.ToInt32(state);
        }

        /// 
        /// Set enemy to a specific behaviour state via ServerRpc.
        /// 
        public static void SetBehaviourState(this EnemyAI enemy, Enum state)
        {
            if (enemy.IsBehaviourState(state)) return;
            enemy.SwitchToBehaviourServerRpc(Convert.ToInt32(state));
        }

        /// 
        /// Get a private field value using reflection.
        /// 
        public static T GetPrivateField<T>(this object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, 
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                Loader.LogError($"[EnemyControl] Field '{fieldName}' not found on {obj.GetType().Name}");
                return default!;
            }
            return (T)field.GetValue(obj)!;
        }

        /// 
        /// Set a private field value using reflection.
        /// 
        public static void SetPrivateField<T>(this object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, 
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                Loader.LogError($"[EnemyControl] Field '{fieldName}' not found on {obj.GetType().Name}");
                return;
            }
            field.SetValue(obj, value);
        }

        /// 
        /// Find a nearby item within grab range.
        /// 
        public static GrabbableObject? FindNearbyItem(this EnemyAI enemy, float grabRange = 1.0f)
        {
            foreach (var collider in UnityEngine.Physics.OverlapSphere(enemy.transform.position, grabRange))
            {
                if (collider.TryGetComponent(out GrabbableObject item))
                {
                    if (item.TryGetComponent(out Unity.Netcode.NetworkObject _))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        /// 
        /// Check if enemy is outside the facility.
        /// 
        public static bool IsOutside(this EnemyAI enemy)
        {
            if (RoundManager.Instance?.outsideAINodes == null || 
                RoundManager.Instance.insideAINodes == null ||
                RoundManager.Instance.outsideAINodes.Length == 0 ||
                RoundManager.Instance.insideAINodes.Length == 0)
            {
                return false;
            }

            float minOutsideDist = float.MaxValue;
            float minInsideDist = float.MaxValue;

            foreach (var node in RoundManager.Instance.outsideAINodes)
            {
                float dist = UnityEngine.Vector3.Distance(enemy.serverPosition, node.transform.position);
                if (dist < minOutsideDist) minOutsideDist = dist;
            }

            foreach (var node in RoundManager.Instance.insideAINodes)
            {
                float dist = UnityEngine.Vector3.Distance(enemy.serverPosition, node.transform.position);
                if (dist < minInsideDist) minInsideDist = dist;
            }

            return minOutsideDist < minInsideDist;
        }

        /// 
        /// Set the enemy's outside/inside status and update their AI nodes.
        /// This affects which AI nodes the enemy uses for pathfinding.
        /// 
        public static void SetOutsideStatus(this EnemyAI enemy, bool isOutside)
        {
            if (enemy.isOutside == isOutside) return;

            enemy.isOutside = isOutside;
            enemy.allAINodes = UnityEngine.GameObject.FindGameObjectsWithTag(isOutside ? "OutsideAINode" : "AINode");
        }
    }
}
