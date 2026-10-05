using System.Collections.Generic;
using System.Reflection.Emit;
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

    /// Shoplifter (free purchases). Terminal.LoadNewNodeIfAffordable computes totalCostOfItems and then uses it for
    /// the affordability check and for `groupCredits -= totalCostOfItems`, all before the credits are synced to the
    /// server (BuyItemsServerRpc / ChangeLevelServerRpc / BuyShipUnlockableServerRpc / vehicle order), so the
    /// server only ever sees the unchanged credit value. Every load of the field in that method (items, vehicles,
    /// moon routing and ship unlockables all share it) is routed through Cost, which reports 0 while enabled.
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNodeIfAffordable))]
    public static class ShoplifterPatches
    {
        private static int Cost(int totalCostOfItems) => Hack.Shoplifter.IsEnabled() ? 0 : totalCostOfItems;

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var field = AccessTools.Field(typeof(Terminal), nameof(Terminal.totalCostOfItems));
            var cost = AccessTools.Method(typeof(ShoplifterPatches), nameof(Cost));
            foreach (var instr in instructions)
            {
                yield return instr;
                if (instr.LoadsField(field)) yield return new CodeInstruction(OpCodes.Call, cost);
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
        /// Only undoes what this patch did: when the toggle turns off, the buttons it force-enabled are disabled once.
        private static bool _enabledButtons;

        [HarmonyPatch(typeof(HangarShipDoor), nameof(HangarShipDoor.Update))]
        [HarmonyPrefix]
        public static bool HangarDoorUpdatePrefix(HangarShipDoor __instance)
        {
            if (!StartOfRound.Instance.inShipPhase) return true;

            if (Hack.ShipDoorInSpace.IsEnabled())
            {
                if (!__instance.buttonsEnabled)
                {
                    __instance.SetDoorButtonsEnabled(true);
                    _enabledButtons = true;
                }
            }
            else if (_enabledButtons)
            {
                _enabledButtons = false;
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
