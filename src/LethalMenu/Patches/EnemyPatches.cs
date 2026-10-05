using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Patches for EnemyAI.
    [HarmonyPatch(typeof(EnemyAI))]
    public static class EnemyPatches
    {
        /// Make enemies unable to target local player (Untargetable, or the enemy is directed).
        [HarmonyPatch("PlayerIsTargetable")]
        [HarmonyPostfix]
        public static void PlayerIsTargetablePostfix(EnemyAI __instance, ref bool __result, PlayerControllerB playerScript)
        {
            if (UntargetableSightPatches.IsHiddenFrom(__instance, playerScript))
            {
                __result = false;
            }
        }

        /// Clear enemy target if it's the local player.
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool EnemyUpdatePrefix(EnemyAI __instance)
        {
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer)) return true;
            if (__instance.targetPlayer != LethalMenuMod.LocalPlayer) return true;

            __instance.targetPlayer = null;
            __instance.movingTowardsTargetPlayer = false;
            return true;
        }

        /// Block noise detection from local player.
        [HarmonyPatch("DetectNoise")]
        [HarmonyPrefix]
        public static bool DetectNoisePrefix(EnemyAI __instance, Vector3 noisePosition)
        {
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer)) return true;
            if (LethalMenuMod.LocalPlayer == null) return true;

            float distToPlayer = Vector3.Distance(noisePosition, LethalMenuMod.LocalPlayer.transform.position);
            if (distToPlayer < 5f)
            {
                return false;
            }
            return true;
        }
    }

    /// MouthDog-specific patches - they are blind and use sound.
    [HarmonyPatch(typeof(MouthDogAI))]
    public static class MouthDogPatches
    {
        /// Block MouthDog from detecting local player's noise.
        [HarmonyPatch("DetectNoise")]
        [HarmonyPrefix]
        public static bool DetectNoisePrefix(MouthDogAI __instance, Vector3 noisePosition)
        {
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer)) return true;
            if (LethalMenuMod.LocalPlayer == null) return true;

            float distToPlayer = Vector3.Distance(noisePosition, LethalMenuMod.LocalPlayer.transform.position);
            if (distToPlayer < 8f)
            {
                return false;
            }
            return true;
        }

        /// Block MouthDog enrage towards local player.
        [HarmonyPatch("EnterLunge")]
        [HarmonyPrefix]
        public static bool EnterLungePrefix(MouthDogAI __instance)
        {
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer)) return true;

            if (__instance.targetPlayer == LethalMenuMod.LocalPlayer)
            {
                __instance.targetPlayer = null;
                return false;
            }
            return true;
        }
    }
}
