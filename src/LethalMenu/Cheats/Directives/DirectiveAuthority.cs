using System;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats.Directives
{
    /// What a director who is not the host needs to run a directed enemy's host-gated logic and land its hits.
    public static class DirectiveAuthority
    {
        // NetworkBehaviour.IsServer is an auto-property with a private setter; Unity.Netcode is not publicized.
        private static readonly Action<NetworkBehaviour, bool>? SetIsServer = CreateIsServerSetter();
        private static readonly HashSet<EnemyAI> Elevated = new();
        private static readonly Dictionary<EnemyAI, float> NextStrike = new();

        /// Kills from full health (DamagePlayer clamps health to 0..100).
        public const int LethalDamage = 100;

        public static bool IsRealServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        /// Makes the enemy's own `base.IsServer` read true on this client until Drop. Held only across the
        /// enemy's Update (DirectiveUpdatePatch), so RPC handlers, which run outside it, still see the real
        /// value. ClientRpcs called meanwhile send nothing (the generated code checks NetworkManager.IsServer).
        public static void Elevate(EnemyAI enemy)
        {
            if (IsRealServer || SetIsServer == null || !Elevated.Add(enemy)) return;
            SetIsServer(enemy, true);
        }

        public static void Drop(EnemyAI enemy)
        {
            if (Elevated.Remove(enemy)) SetIsServer!(enemy, false);
        }

        /// Damage any player from this client: DamagePlayerFromOtherClientServerRpc (RequireOwnership = false)
        /// -> the victim's own DamagePlayer.
        public static void Strike(PlayerControllerB target, int damage, Vector3 direction)
        {
            var local = LethalMenuMod.LocalPlayer;
            if (local == null || target == null || target.isPlayerDead) return;
            target.DamagePlayerFromOtherClientServerRpc(damage, direction, (int)local.playerClientId);
        }

        /// Strike on behalf of `enemy`, pushed away from it, at most once per `cooldown` seconds.
        public static bool TryStrike(EnemyAI enemy, PlayerControllerB target, int damage, float cooldown)
        {
            if (NextStrike.TryGetValue(enemy, out float next) && Time.time < next) return false;
            NextStrike[enemy] = Time.time + cooldown;
            Strike(target, damage, Vector3.Normalize(target.transform.position - enemy.transform.position));
            return true;
        }

        public static void Forget(EnemyAI enemy)
        {
            NextStrike.Remove(enemy);
            Drop(enemy);
        }

        public static void Clear()
        {
            NextStrike.Clear();
            Elevated.Clear();
        }

        private static Action<NetworkBehaviour, bool>? CreateIsServerSetter()
        {
            var setter = typeof(NetworkBehaviour)
                .GetProperty("IsServer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetSetMethod(nonPublic: true);
            if (setter == null)
            {
                Loader.LogError("[Directives] NetworkBehaviour.IsServer setter not found.");
                return null;
            }
            return (Action<NetworkBehaviour, bool>)Delegate.CreateDelegate(typeof(Action<NetworkBehaviour, bool>), setter);
        }
    }
}
