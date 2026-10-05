using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Util
{
    /// Access to NetworkBehaviour's internal RPC exec stage. GameLibs publicizes the game assemblies but
    /// not Unity.Netcode.Runtime, so this is the one place that still needs reflection.
    public static class ReflectionHelper
    {
        private static readonly FieldInfo? ExecStageField =
            typeof(NetworkBehaviour).GetField("__rpc_exec_stage", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Type? ExecStageEnum =
            typeof(NetworkBehaviour).GetNestedType("__RpcExecStage", BindingFlags.NonPublic);

        /// Force __rpc_exec_stage to Execute.
        public static bool ForceRpcExecStageExecute(NetworkBehaviour behaviour) => SetRpcExecStage(behaviour, "Execute");

        /// Reset __rpc_exec_stage to Send — the idle value the generated RPC code restores after executing
        /// (this NGO version's enum has no None).
        public static bool ResetRpcExecStage(NetworkBehaviour behaviour) => SetRpcExecStage(behaviour, "Send");

        private static bool SetRpcExecStage(NetworkBehaviour behaviour, string stageName)
        {
            if (behaviour == null || ExecStageField == null || ExecStageEnum == null)
            {
                Debug.LogError("[ReflectionHelper] NetworkBehaviour.__rpc_exec_stage not found.");
                return false;
            }

            ExecStageField.SetValue(behaviour, Enum.Parse(ExecStageEnum, stageName, ignoreCase: true));
            return true;
        }
    }
}
