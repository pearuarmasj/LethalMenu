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
            if (Hack.SuperShovel.IsEnabled() && __instance.playerHeldBy == LethalMenuMod.LocalPlayer)
            {
                __instance.shovelHitForce = 100;
            }
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

    /// Grab nutcracker's shotgun.
    [HarmonyPatch(typeof(PlayerControllerB), "BeginGrabObject")]
    public static class GrabNutcrackerShotgunPatches
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerControllerB __instance)
        {
            if (!Hack.GrabNutcrackerShotgun.IsEnabled()) return;
            
            var field = __instance.GetType().GetField("currentlyGrabbingObject", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) return;
            
            var grabbableObject = field.GetValue(__instance) as GrabbableObject;
            if (grabbableObject == null) return;
            
            var shotgun = grabbableObject as ShotgunItem;
            if (shotgun == null) return;
            
            var enemyField = shotgun.GetType().GetField("heldByEnemy",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (enemyField == null) return;
            
            var enemy = enemyField.GetValue(shotgun) as EnemyAI;
            if (enemy == null) return;
            
            var nutcracker = enemy as NutcrackerEnemyAI;
            if (nutcracker == null || nutcracker.gunPoint == null) return;
            
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;
            
            nutcracker.ChangeEnemyOwnerServerRpc(localPlayer.actualClientId);
            nutcracker.DropGunServerRpc(nutcracker.gunPoint.position);
        }
    }

    /// Eggs always explode.
    [HarmonyPatch]
    public static class EggsPatches
    {
        [HarmonyPatch(typeof(StunGrenadeItem), nameof(StunGrenadeItem.SetExplodeOnThrowClientRpc))]
        [HarmonyPrefix]
        public static bool SetExplodePrefix(StunGrenadeItem __instance)
        {
            if (Hack.EggsNeverExplode.IsEnabled() && !Hack.EggsAlwaysExplode.IsEnabled())
            {
                if (LethalMenuMod.LocalPlayer?.currentlyHeldObjectServer?.name == "EasterEgg(Clone)")
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// Loot before game starts patches.
    [HarmonyPatch]
    public static class LootBeforeGameStartsPatches
    {
        private static readonly System.Collections.Generic.Dictionary<GrabbableObject, bool> ModifiedItems = 
            new System.Collections.Generic.Dictionary<GrabbableObject, bool>();

        [HarmonyPatch(typeof(PlayerControllerB), "BeginGrabObject")]
        [HarmonyPrefix]
        public static void BeginGrabPrefix(PlayerControllerB __instance)
        {
            if (!Hack.LootBeforeGameStarts.IsEnabled()) return;

            var field = __instance.GetType().GetField("currentlyGrabbingObject",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) return;

            var grabbable = field.GetValue(__instance) as GrabbableObject;
            if (grabbable?.itemProperties == null || grabbable.itemProperties.canBeGrabbedBeforeGameStart) return;
            if (GameNetworkManager.Instance.gameHasStarted) return;

            ModifiedItems[grabbable] = grabbable.itemProperties.canBeGrabbedBeforeGameStart;
            grabbable.itemProperties.canBeGrabbedBeforeGameStart = true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "DiscardHeldObject")]
        [HarmonyPrefix]
        public static void DiscardPrefix(PlayerControllerB __instance)
        {
            var heldItem = __instance.currentlyHeldObjectServer;
            if (heldItem != null && ModifiedItems.TryGetValue(heldItem, out bool original))
            {
                heldItem.itemProperties.canBeGrabbedBeforeGameStart = original;
                ModifiedItems.Remove(heldItem);
            }
        }
    }
}
