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
            if (Hack.Shoplifter.IsEnabled())
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
            return !Hack.AntiJeb.IsEnabled();
        }

        [HarmonyPatch("AttackPlayersServerRpc")]
        [HarmonyPrefix]
        public static bool AttackServerPrefix()
        {
            return !Hack.AntiJeb.IsEnabled();
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
            if (Hack.BuildAnywhere.IsEnabled() && __instance.InBuildMode)
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
            if (!Hack.AutoOpenDropship.IsEnabled() || __instance == null || __instance.shipDoorsOpened) return;
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
            if (Hack.ShipDoorInSpace.IsEnabled() && !__instance.buttonsEnabled && StartOfRound.Instance.inShipPhase)
            {
                __instance.SetDoorButtonsEnabled(true);
            }
            else if (!Hack.ShipDoorInSpace.IsEnabled() && __instance.buttonsEnabled && StartOfRound.Instance.inShipPhase)
            {
                __instance.SetDoorButtonsEnabled(false);
            }
            return true;
        }

        [HarmonyPatch(typeof(StartOfRound), "TeleportPlayerInShipIfOutOfRoomBounds")]
        [HarmonyPrefix]
        public static bool TeleportBoundsPrefix()
        {
            return !Hack.ShipDoorInSpace.IsEnabled();
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
            return !Hack.BridgeNeverFalls.IsEnabled();
        }

        [HarmonyPatch(typeof(BridgeTriggerType2), "AddToBridgeInstabilityServerRpc")]
        [HarmonyPrefix]
        public static bool AddInstabilityPrefix()
        {
            return !Hack.BridgeNeverFalls.IsEnabled();
        }
    }
}
