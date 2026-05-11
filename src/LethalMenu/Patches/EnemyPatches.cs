using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Patches for EnemyAI.
    [HarmonyPatch(typeof(EnemyAI))]
    public static class EnemyPatches
    {
        /// Make enemies unable to target local player.
        [HarmonyPatch("PlayerIsTargetable")]
        [HarmonyPostfix]
        public static void PlayerIsTargetablePostfix(ref bool __result, PlayerControllerB playerScript)
        {
            if (Hack.Untargetable.IsEnabled() && playerScript == LethalMenuMod.LocalPlayer)
            {
                __result = false;
            }
        }

        /// Clear enemy target if it's the local player.
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool EnemyUpdatePrefix(EnemyAI __instance)
        {
            if (!Hack.Untargetable.IsEnabled()) return true;
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
            if (!Hack.Untargetable.IsEnabled()) return true;
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
            if (!Hack.Untargetable.IsEnabled()) return true;
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
            if (!Hack.Untargetable.IsEnabled()) return true;
            
            if (__instance.targetPlayer == LethalMenuMod.LocalPlayer)
            {
                __instance.targetPlayer = null;
                return false;
            }
            return true;
        }
    }

    /// Anti Ghost Girl patches.
    [HarmonyPatch(typeof(DressGirlAI))]
    public static class AntiGhostGirlPatches
    {
        [HarmonyPatch("ChoosePlayerToHaunt")]
        [HarmonyPostfix]
        public static void ChoosePlayerToHauntPostfix(DressGirlAI __instance)
        {
            if (!Hack.AntiGhostGirl.IsEnabled() || __instance == null) return;
            if (__instance.hauntingLocalPlayer)
            {
                __instance.hauntingPlayer = null;
            }
        }

        [HarmonyPatch("BeginChasing")]
        [HarmonyPostfix]
        public static void BeginChasingPostfix(DressGirlAI __instance)
        {
            if (!Hack.AntiGhostGirl.IsEnabled() || __instance == null) return;
            if (__instance.hauntingLocalPlayer)
            {
                __instance.hauntingPlayer = null;
            }
        }
    }

    /// Ghost mode - enemies cannot see you (alternative to Untargetable).
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.PlayerIsTargetable))]
    public static class GhostModePatches
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerControllerB playerScript)
        {
            if (Hack.GhostMode.IsEnabled() && LethalMenuMod.LocalPlayer != null && 
                LethalMenuMod.LocalPlayer.playerClientId == playerScript.playerClientId)
            {
                return false;
            }
            return true;
        }
    }
}
