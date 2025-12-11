using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;

namespace LethalMenu.Util
{
    /// Utility class for invoking private/protected methods and accessing private fields via reflection.
    /// This bypasses all visibility restrictions in the game's code.
    public static class ReflectionHelper
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags AllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags All = AllInstance | AllStatic;

        #region Method Invocation

        /// Invokes a private/protected instance method on an object.
        /// <param name="obj">The object to invoke the method on.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">Arguments to pass to the method.</param>
        /// <returns>The return value of the method, or null if void.</returns>
        public static object? InvokePrivate(object obj, string methodName, params object?[]? args)
        {
            if (obj == null)
            {
                Debug.LogError($"[ReflectionHelper] InvokePrivate: Object is null for method '{methodName}'");
                return null;
            }

            try
            {
                var method = obj.GetType().GetMethod(methodName, AllInstance);
                if (method == null)
                {
                    Debug.LogError($"[ReflectionHelper] Method '{methodName}' not found on type '{obj.GetType().Name}'");
                    return null;
                }

                return method.Invoke(obj, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to invoke '{methodName}': {e.Message}");
                return null;
            }
        }

        /// Invokes a private/protected instance method with specific parameter types.
        /// Use this when there are overloaded methods.
        public static object? InvokePrivate(object obj, string methodName, Type[] paramTypes, params object?[]? args)
        {
            if (obj == null)
            {
                Debug.LogError($"[ReflectionHelper] InvokePrivate: Object is null for method '{methodName}'");
                return null;
            }

            try
            {
                var method = obj.GetType().GetMethod(methodName, AllInstance, null, paramTypes, null);
                if (method == null)
                {
                    Debug.LogError($"[ReflectionHelper] Method '{methodName}' with specified params not found on type '{obj.GetType().Name}'");
                    return null;
                }

                return method.Invoke(obj, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to invoke '{methodName}': {e.Message}");
                return null;
            }
        }

        /// Invokes a private/protected static method on a type.
        public static object? InvokePrivateStatic(Type type, string methodName, params object?[]? args)
        {
            if (type == null)
            {
                Debug.LogError($"[ReflectionHelper] InvokePrivateStatic: Type is null for method '{methodName}'");
                return null;
            }

            try
            {
                var method = type.GetMethod(methodName, AllStatic);
                if (method == null)
                {
                    Debug.LogError($"[ReflectionHelper] Static method '{methodName}' not found on type '{type.Name}'");
                    return null;
                }

                return method.Invoke(null, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to invoke static '{methodName}': {e.Message}");
                return null;
            }
        }

        /// Invokes a private/protected static method on a type by generic type parameter.
        public static object? InvokePrivateStatic<T>(string methodName, params object?[]? args)
        {
            return InvokePrivateStatic(typeof(T), methodName, args);
        }

        #endregion

        #region Field Access

        /// Gets the value of a private/protected field.
        public static T? GetPrivateField<T>(object obj, string fieldName)
        {
            if (obj == null)
            {
                Debug.LogError($"[ReflectionHelper] GetPrivateField: Object is null for field '{fieldName}'");
                return default;
            }

            try
            {
                var field = obj.GetType().GetField(fieldName, AllInstance);
                if (field == null)
                {
                    Debug.LogError($"[ReflectionHelper] Field '{fieldName}' not found on type '{obj.GetType().Name}'");
                    return default;
                }

                return (T?)field.GetValue(obj);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to get field '{fieldName}': {e.Message}");
                return default;
            }
        }

        /// Sets the value of a private/protected field.
        public static bool SetPrivateField(object obj, string fieldName, object? value)
        {
            if (obj == null)
            {
                Debug.LogError($"[ReflectionHelper] SetPrivateField: Object is null for field '{fieldName}'");
                return false;
            }

            try
            {
                var field = obj.GetType().GetField(fieldName, AllInstance);
                if (field == null)
                {
                    Debug.LogError($"[ReflectionHelper] Field '{fieldName}' not found on type '{obj.GetType().Name}'");
                    return false;
                }

                field.SetValue(obj, value);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to set field '{fieldName}': {e.Message}");
                return false;
            }
        }

        /// Gets the value of a private/protected static field.
        public static T? GetPrivateStaticField<T>(Type type, string fieldName)
        {
            if (type == null)
            {
                Debug.LogError($"[ReflectionHelper] GetPrivateStaticField: Type is null for field '{fieldName}'");
                return default;
            }

            try
            {
                var field = type.GetField(fieldName, AllStatic);
                if (field == null)
                {
                    Debug.LogError($"[ReflectionHelper] Static field '{fieldName}' not found on type '{type.Name}'");
                    return default;
                }

                return (T?)field.GetValue(null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to get static field '{fieldName}': {e.Message}");
                return default;
            }
        }

        /// Sets the value of a private/protected static field.
        public static bool SetPrivateStaticField(Type type, string fieldName, object? value)
        {
            if (type == null)
            {
                Debug.LogError($"[ReflectionHelper] SetPrivateStaticField: Type is null for field '{fieldName}'");
                return false;
            }

            try
            {
                var field = type.GetField(fieldName, AllStatic);
                if (field == null)
                {
                    Debug.LogError($"[ReflectionHelper] Static field '{fieldName}' not found on type '{type.Name}'");
                    return false;
                }

                field.SetValue(null, value);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to set static field '{fieldName}': {e.Message}");
                return false;
            }
        }

        #endregion

        #region Property Access

        /// Gets the value of a private/protected property.
        public static T? GetPrivateProperty<T>(object obj, string propertyName)
        {
            if (obj == null)
            {
                Debug.LogError($"[ReflectionHelper] GetPrivateProperty: Object is null for property '{propertyName}'");
                return default;
            }

            try
            {
                var prop = obj.GetType().GetProperty(propertyName, AllInstance);
                if (prop == null)
                {
                    Debug.LogError($"[ReflectionHelper] Property '{propertyName}' not found on type '{obj.GetType().Name}'");
                    return default;
                }

                return (T?)prop.GetValue(obj);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to get property '{propertyName}': {e.Message}");
                return default;
            }
        }

        /// Sets the value of a private/protected property.
        public static bool SetPrivateProperty(object obj, string propertyName, object? value)
        {
            if (obj == null)
            {
                Debug.LogError($"[ReflectionHelper] SetPrivateProperty: Object is null for property '{propertyName}'");
                return false;
            }

            try
            {
                var prop = obj.GetType().GetProperty(propertyName, AllInstance);
                if (prop == null)
                {
                    Debug.LogError($"[ReflectionHelper] Property '{propertyName}' not found on type '{obj.GetType().Name}'");
                    return false;
                }

                prop.SetValue(obj, value);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to set property '{propertyName}': {e.Message}");
                return false;
            }
        }

        #endregion

        #region NetworkBehaviour Helpers

        /// Gets a MethodInfo for a specific RPC method, useful for calling RPCs directly.
        public static MethodInfo? GetRpcMethod(Type type, string rpcMethodName)
        {
            try
            {
                return type.GetMethod(rpcMethodName, AllInstance);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Failed to get RPC method '{rpcMethodName}': {e.Message}");
                return null;
            }
        }

        /// Gets the __rpc_exec_stage field from a NetworkBehaviour, useful for RPC manipulation.
        public static object? GetRpcExecStage(object networkBehaviour)
        {
            return GetPrivateField<object>(networkBehaviour, "__rpc_exec_stage");
        }

        /// Sets the __rpc_exec_stage field on a NetworkBehaviour.
        public static bool SetRpcExecStage(object networkBehaviour, object value)
        {
            return SetPrivateField(networkBehaviour, "__rpc_exec_stage", value);
        }

        /// Resolves the private NetworkBehaviour.__RpcExecStage enum value by name.
        /// Returns null if the enum or value cannot be found.
        public static object? GetRpcExecStageValue(string stageName)
        {
            if (string.IsNullOrWhiteSpace(stageName))
                return null;

            var enumType = typeof(NetworkBehaviour).GetNestedType("__RpcExecStage", BindingFlags.NonPublic);
            if (enumType == null)
            {
                Debug.LogError("[ReflectionHelper] __RpcExecStage enum not found on NetworkBehaviour.");
                return null;
            }

            try
            {
                return Enum.Parse(enumType, stageName, ignoreCase: true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionHelper] Invalid __rpc_exec_stage '{stageName}': {e.Message}");
                return null;
            }
        }

        /// Convenience overload to set __rpc_exec_stage by enum name (e.g., \"Execute\", \"None\").
        public static bool SetRpcExecStage(object networkBehaviour, string stageName)
        {
            var stageValue = GetRpcExecStageValue(stageName);
            return stageValue != null && SetRpcExecStage(networkBehaviour, stageValue);
        }

        /// Force __rpc_exec_stage to Execute.
        public static bool ForceRpcExecStageExecute(object networkBehaviour)
        {
            var stageValue = GetRpcExecStageValue("Execute");
            return stageValue != null && SetRpcExecStage(networkBehaviour, stageValue);
        }

        /// Reset __rpc_exec_stage to None/Default if present.
        public static bool ResetRpcExecStage(object networkBehaviour)
        {
            var stageValue = GetRpcExecStageValue("None");
            return stageValue != null && SetRpcExecStage(networkBehaviour, stageValue);
        }

        #endregion

        #region Generic Helpers

        /// Checks if a type has a specific method.
        public static bool HasMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName, All) != null;
        }

        /// Checks if a type has a specific field.
        public static bool HasField(Type type, string fieldName)
        {
            return type.GetField(fieldName, All) != null;
        }

        /// Gets all methods matching a pattern (for discovery/debugging).
        public static MethodInfo[] GetAllMethods(Type type, string? nameContains = null)
        {
            var methods = type.GetMethods(All);
            if (string.IsNullOrEmpty(nameContains))
                return methods;
            
            return Array.FindAll(methods, m => m.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase));
        }

        /// Gets all fields matching a pattern (for discovery/debugging).
        public static FieldInfo[] GetAllFields(Type type, string? nameContains = null)
        {
            var fields = type.GetFields(All);
            if (string.IsNullOrEmpty(nameContains))
                return fields;
            
            return Array.FindAll(fields, f => f.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}

