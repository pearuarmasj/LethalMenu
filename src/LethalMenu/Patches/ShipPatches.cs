using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Patches for QuickMenuManager to prevent menu interference.
    [HarmonyPatch(typeof(QuickMenuManager))]
    public static class QuickMenuPatches
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(QuickMenuManager __instance)
        {
            LethalMenuMod.QuickMenu = __instance;
        }
    }

    /// Terminal patches for shoplifter (free items).
    [HarmonyPatch(typeof(Terminal))]
    public static class TerminalPatches
    {
        [HarmonyPatch("BuyItemsServerRpc")]
        [HarmonyPrefix]
        public static void BuyItemsPrefix(Terminal __instance)
        {
            if (Settings.Shoplifter)
            {
                __instance.groupCredits = 999999;
            }
        }
    }

    /// Deposit desk patches to prevent Jeb attacks.
    [HarmonyPatch(typeof(DepositItemsDesk))]
    public static class DepositDeskPatches
    {
        [HarmonyPatch("Attack")]
        [HarmonyPrefix]
        public static bool AttackPrefix()
        {
            return !Settings.JebAttackPrevention;
        }

        [HarmonyPatch("AttackPlayersServerRpc")]
        [HarmonyPrefix]
        public static bool AttackServerPrefix()
        {
            return !Settings.JebAttackPrevention;
        }
    }

    /// Build anywhere - allow placing ship objects outside ship bounds.
    [HarmonyPatch(typeof(ShipBuildModeManager))]
    public static class BuildModePatches
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(ShipBuildModeManager __instance)
        {
            if (Settings.BuildAnywhere && __instance.InBuildMode)
            {
                __instance.CanConfirmPosition = true;
            }
        }
    }

    /// Open dropship on land.
    [HarmonyPatch(typeof(ItemDropship), "ShipLandedAnimationEvent")]
    public static class OpenDropShipPatches
    {
        [HarmonyPostfix]
        public static void Postfix(ItemDropship __instance)
        {
            if (!Settings.OpenDropShipLand || __instance == null || __instance.shipDoorsOpened) return;
            __instance.OpenShipServerRpc();
        }
    }

    /// Open ship door in space.
    [HarmonyPatch]
    public static class OpenShipDoorSpacePatches
    {
        [HarmonyPatch(typeof(HangarShipDoor), "Update")]
        [HarmonyPrefix]
        public static bool HangarDoorUpdatePrefix(HangarShipDoor __instance)
        {
            if (Settings.OpenShipDoorSpace && !__instance.buttonsEnabled && StartOfRound.Instance.inShipPhase)
            {
                __instance.SetDoorButtonsEnabled(true);
            }
            else if (!Settings.OpenShipDoorSpace && __instance.buttonsEnabled && StartOfRound.Instance.inShipPhase)
            {
                __instance.SetDoorButtonsEnabled(false);
            }
            return true;
        }

        [HarmonyPatch(typeof(StartOfRound), "TeleportPlayerInShipIfOutOfRoomBounds")]
        [HarmonyPrefix]
        public static bool TeleportBoundsPrefix()
        {
            return !Settings.OpenShipDoorSpace;
        }
    }

    /// Bridge never falls patches.
    [HarmonyPatch]
    public static class BridgePatches
    {
        [HarmonyPatch(typeof(BridgeTrigger), "BridgeFallClientRpc")]
        [HarmonyPrefix]
        public static bool BridgeFallPrefix()
        {
            return !Settings.BridgeNeverFalls;
        }

        [HarmonyPatch(typeof(BridgeTriggerType2), "AddToBridgeInstabilityServerRpc")]
        [HarmonyPrefix]
        public static bool AddInstabilityPrefix()
        {
            return !Settings.BridgeNeverFalls;
        }
    }
}
