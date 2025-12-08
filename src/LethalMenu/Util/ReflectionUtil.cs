using System;
using System.Reflection;
using UnityEngine;

namespace LethalMenu.Util
{
    /// <summary>
    /// Utility for calling private/internal methods via reflection.
    /// </summary>
    public class ReflectionUtil<T>
    {
        // Include public methods too since some ServerRpc methods are public
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags AllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private T _object;
        private Type _type;

        public ReflectionUtil(T obj)
        {
            _object = obj;
            _type = obj?.GetType() ?? typeof(T); // Use actual runtime type, not declared type
        }

        /// <summary>
        /// Get a private field value.
        /// </summary>
        public TResult? GetField<TResult>(string fieldName, bool isStatic = false)
        {
            try
            {
                var flags = isStatic ? AllStatic : AllInstance;
                var field = _type.GetField(fieldName, flags);
                if (field == null)
                {
                    Debug.LogWarning($"[ReflectionUtil] Field '{fieldName}' not found on type '{_type.Name}'");
                    return default;
                }
                return (TResult)field.GetValue(isStatic ? null : (object?)_object)!;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReflectionUtil] GetField '{fieldName}' failed: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Set a private field value.
        /// </summary>
        public ReflectionUtil<T> SetField(string fieldName, object value, bool isStatic = false)
        {
            try
            {
                var flags = isStatic ? AllStatic : AllInstance;
                var field = _type.GetField(fieldName, flags);
                if (field == null)
                {
                    Debug.LogWarning($"[ReflectionUtil] Field '{fieldName}' not found on type '{_type.Name}'");
                    return this;
                }
                field.SetValue(isStatic ? null : (object?)_object, value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReflectionUtil] SetField '{fieldName}' failed: {ex.Message}");
            }
            return this;
        }

        /// <summary>
        /// Get a private property value.
        /// </summary>
        public TResult? GetProperty<TResult>(string propertyName, bool isStatic = false)
        {
            try
            {
                var flags = isStatic ? AllStatic : AllInstance;
                var prop = _type.GetProperty(propertyName, flags);
                if (prop == null)
                {
                    Debug.LogWarning($"[ReflectionUtil] Property '{propertyName}' not found on type '{_type.Name}'");
                    return default;
                }
                return (TResult)prop.GetValue(isStatic ? null : (object?)_object)!;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReflectionUtil] GetProperty '{propertyName}' failed: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Set a private property value.
        /// </summary>
        public ReflectionUtil<T> SetProperty(string propertyName, object value, bool isStatic = false)
        {
            try
            {
                var flags = isStatic ? AllStatic : AllInstance;
                var prop = _type.GetProperty(propertyName, flags);
                if (prop == null)
                {
                    Debug.LogWarning($"[ReflectionUtil] Property '{propertyName}' not found on type '{_type.Name}'");
                    return this;
                }
                prop.SetValue(isStatic ? null : (object?)_object, value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReflectionUtil] SetProperty '{propertyName}' failed: {ex.Message}");
            }
            return this;
        }

        /// <summary>
        /// Invoke a method with return value.
        /// </summary>
        public TResult? Invoke<TResult>(string methodName, bool isStatic = false, params object[] args)
        {
            try
            {
                var flags = isStatic ? AllStatic : AllInstance;
                var method = FindMethod(methodName, flags, args);
                if (method == null)
                {
                    Debug.LogWarning($"[ReflectionUtil] Method '{methodName}' not found on type '{_type.Name}'");
                    return default;
                }
                return (TResult)method.Invoke(isStatic ? null : (object?)_object, args)!;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReflectionUtil] Invoke<TResult> '{methodName}' failed: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Invoke a method (void return).
        /// </summary>
        public ReflectionUtil<T> Invoke(string methodName, params object[] args)
        {
            try
            {
                var flags = AllInstance;
                var method = FindMethod(methodName, flags, args);
                if (method == null)
                {
                    Debug.LogWarning($"[ReflectionUtil] Method '{methodName}' not found on type '{_type.Name}'");
                    return this;
                }
                method.Invoke(_object, args);
                Debug.Log($"[ReflectionUtil] Successfully invoked '{methodName}' on '{_type.Name}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReflectionUtil] Invoke '{methodName}' failed: {ex.Message}");
            }
            return this;
        }

        /// <summary>
        /// Find method by name and parameter types (handles overloads).
        /// </summary>
        private MethodInfo? FindMethod(string methodName, BindingFlags flags, object[] args)
        {
            // First try direct match
            var method = _type.GetMethod(methodName, flags);
            if (method != null) return method;

            // If not found, try matching by parameter count and types
            var methods = _type.GetMethods(flags);
            foreach (var m in methods)
            {
                if (m.Name != methodName) continue;
                var parameters = m.GetParameters();
                if (parameters.Length != args.Length) continue;

                bool match = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (args[i] != null && !parameters[i].ParameterType.IsAssignableFrom(args[i].GetType()))
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return m;
            }
            return null;
        }
    }

    /// <summary>
    /// Extension method to create a ReflectionUtil wrapper.
    /// </summary>
    public static class ReflectionExtensions
    {
        public static ReflectionUtil<T> Reflect<T>(this T obj) => new ReflectionUtil<T>(obj);
    }
}
