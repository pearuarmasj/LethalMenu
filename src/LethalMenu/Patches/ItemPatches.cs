using System.Collections;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Patches for GrabbableObject (items).
    [HarmonyPatch(typeof(GrabbableObject))]
    public static class ItemPatches
    {
        /// Infinite battery for held items.
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(GrabbableObject __instance)
        {
            if (!Hack.InfiniteBattery.IsEnabled()) return;

            if (__instance.playerHeldBy == LethalMenuMod.LocalPlayer)
            {
                if (__instance.insertedBattery != null)
                {
                    __instance.insertedBattery.charge = 1f;
                    // An already-drained battery keeps `empty` set and the item stays unusable otherwise.
                    __instance.insertedBattery.empty = false;
                }
            }
        }
    }

    /// Shovel patches for super shovel.
    [HarmonyPatch(typeof(Shovel))]
    public static class ShovelPatches
    {
        [HarmonyPatch("HitShovel")]
        [HarmonyPrefix]
        public static void HitShovelPrefix(Shovel __instance)
        {
            if (__instance.playerHeldBy != LethalMenuMod.LocalPlayer) return;
            // 1 is Shovel.shovelHitForce's vanilla value; restoring it makes the toggle fully reversible.
            __instance.shovelHitForce = Hack.SuperShovel.IsEnabled() ? 100 : 1;
        }
    }

    /// Shotgun patches for unlimited ammo.
    [HarmonyPatch(typeof(ShotgunItem))]
    public static class ShotgunPatches
    {
        [HarmonyPatch("ShootGun")]
        [HarmonyPostfix]
        public static void ShootGunPostfix(ShotgunItem __instance)
        {
            if (Hack.UnlimitedAmmo.IsEnabled() && __instance.playerHeldBy == LethalMenuMod.LocalPlayer)
            {
                __instance.shellsLoaded = 2;
            }
        }
    }

    /// Super knife damage.
    [HarmonyPatch(typeof(KnifeItem), "HitKnife")]
    public static class SuperKnifePatches
    {
        [HarmonyPrefix]
        public static void Prefix(KnifeItem __instance)
        {
            __instance.knifeHitForce = Hack.SuperKnife.IsEnabled() ? 1000 : 1;
        }
    }

    /// Unlimited zap gun patches.
    [HarmonyPatch]
    public static class UnlimitedZapGunPatches
    {
        [HarmonyPatch(typeof(PatcherTool), "ShiftBendRandomizer")]
        [HarmonyPostfix]
        public static void ShiftBendPostfix(ref float ___bendMultiplier)
        {
            if (Hack.UnlimitedZapGun.IsEnabled())
            {
                ___bendMultiplier = 0f;
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), "RequireCooldown")]
        [HarmonyPostfix]
        public static void CooldownPostfix(GrabbableObject __instance)
        {
            if (Hack.UnlimitedZapGun.IsEnabled() && __instance is PatcherTool)
            {
                __instance.currentUseCooldown = 0f;
            }
        }
    }

    /// Grab nutcracker's shotgun. The game assigns currentlyGrabbingObject inside BeginGrabObject (raycast), so this
    /// must be a postfix. A gun held by a Nutcracker has grabbable = false and is only released by DropGunClientRpc,
    /// which the nutcracker's owner requests via DropGunServerRpc (it requires ownership). We take ownership of the
    /// nutcracker, wait until the ownership change reaches this client, then request the drop; the gun becomes
    /// grabbable once the RPC round-trips, so the player grabs it with the next click.
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.BeginGrabObject))]
    public static class GrabNutcrackerShotgunPatches
    {
        private static NutcrackerEnemyAI? _pending;

        [HarmonyPostfix]
        public static void Postfix(PlayerControllerB __instance)
        {
            if (!Hack.GrabNutcrackerShotgun.IsEnabled()) return;
            if (__instance != LethalMenuMod.LocalPlayer) return;

            // currentlyGrabbingObject is stale when the raycast missed; only act on what the ray actually hit.
            var grabbing = __instance.currentlyGrabbingObject;
            var collider = __instance.hit.collider;
            if (grabbing == null || collider == null || collider.gameObject != grabbing.gameObject) return;

            if (grabbing is not ShotgunItem shotgun || !shotgun.isHeldByEnemy) return;
            if (shotgun.heldByEnemy is not NutcrackerEnemyAI nutcracker || nutcracker.gunPoint == null) return;
            if (_pending == nutcracker) return;

            _pending = nutcracker;
            if (!nutcracker.IsOwner) nutcracker.ChangeEnemyOwnerServerRpc(__instance.actualClientId);
            nutcracker.StartCoroutine(DropGunWhenOwner(nutcracker));
        }

        private static IEnumerator DropGunWhenOwner(NutcrackerEnemyAI nutcracker)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (nutcracker != null && !nutcracker.IsOwner && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (nutcracker != null && nutcracker.IsOwner && nutcracker.gun != null && nutcracker.gun.isHeldByEnemy)
                nutcracker.DropGunServerRpc(nutcracker.gunPoint.position);

            _pending = null;
        }
    }

    /// Easter eggs are the StunGrenadeItems with chanceToExplode < 100 (the same test the game uses). Every client
    /// decides locally in ExplodeStunGrenade via the private explodeOnThrow, so both toggles are per-client
    /// effects: other players still see their own roll. Never: skip the explosion exactly like the vanilla early
    /// return (clearing activatingItem). Always (or Never off): force explodeOnThrow before the original runs.
    [HarmonyPatch(typeof(StunGrenadeItem), nameof(StunGrenadeItem.ExplodeStunGrenade))]
    public static class EggsPatches
    {
        [HarmonyPrefix]
        public static bool Prefix(StunGrenadeItem __instance)
        {
            if (__instance.chanceToExplode >= 100f) return true;

            if (Hack.EggsAlwaysExplode.IsEnabled())
            {
                __instance.explodeOnThrow = true;
                return true;
            }

            if (!Hack.EggsNeverExplode.IsEnabled()) return true;

            if (!__instance.hasExploded && __instance.playerThrownBy != null)
                __instance.playerThrownBy.activatingItem = false;
            return false;
        }
    }

    /// Loot before game starts. BeginGrabObject's only gate is
    /// `!gameHasStarted && !itemProperties.canBeGrabbedBeforeGameStart && testRoom == null`, so the local player's
    /// call sees gameHasStarted = true. The finalizer restores it even if the original throws.
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.BeginGrabObject))]
    public static class LootBeforeGameStartsPatches
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerControllerB __instance, out bool __state)
        {
            var network = GameNetworkManager.Instance;
            __state = network.gameHasStarted;
            if (Hack.LootBeforeGameStarts.IsEnabled() && __instance == LethalMenuMod.LocalPlayer)
                network.gameHasStarted = true;
        }

        [HarmonyFinalizer]
        public static void Finalizer(bool __state) => GameNetworkManager.Instance.gameHasStarted = __state;
    }
}
