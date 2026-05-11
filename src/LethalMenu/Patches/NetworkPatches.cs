using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Invisibility - sends fake position to server, restores on client.
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class InvisibilityPatches
    {
        private static Vector3 _lastRealPos;
        private static bool _lastInElevator;
        private static bool _lastInShipRoom;
        private static bool _lastExhausted;
        private static bool _lastGrounded;

        [HarmonyPatch("UpdatePlayerPositionServerRpc")]
        [HarmonyPrefix]
        public static void UpdatePositionServerPrefix(
            PlayerControllerB __instance,
            ref Vector3 newPos,
            ref bool inElevator,
            ref bool inShipRoom,
            ref bool exhausted,
            ref bool isPlayerGrounded)
        {
            if (!Hack.Invisibility.IsEnabled()) return;
            if (__instance != LethalMenuMod.LocalPlayer) return;

            _lastRealPos = newPos;
            _lastInElevator = inElevator;
            _lastInShipRoom = inShipRoom;
            _lastExhausted = exhausted;
            _lastGrounded = isPlayerGrounded;

            newPos = new Vector3(0f, -100f, 0f);
            inElevator = false;
            inShipRoom = false;
            exhausted = false;
            isPlayerGrounded = true;
        }

        [HarmonyPatch("UpdatePlayerPositionClientRpc")]
        [HarmonyPrefix]
        public static void UpdatePositionClientPrefix(
            PlayerControllerB __instance,
            ref Vector3 newPos,
            ref bool inElevator,
            ref bool isInShip,
            ref bool exhausted,
            ref bool isPlayerGrounded)
        {
            if (!Hack.Invisibility.IsEnabled()) return;
            if (__instance != LethalMenuMod.LocalPlayer) return;

            newPos = _lastRealPos;
            inElevator = _lastInElevator;
            isInShip = _lastInShipRoom;
            exhausted = _lastExhausted;
            isPlayerGrounded = _lastGrounded;
        }
    }

    /// Anti-kick patches - allows rejoining lobbies after being kicked.
    [HarmonyPatch]
    public static class AntiKickPatches
    {
        /// Track disconnections to detect kicks.
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        public static void DisconnectPostfix(GameNetworkManager __instance)
        {
            if (GameNetworkManager.Instance.disconnectReason == 1)
            {
                if (!Settings.HostQuit && Settings.CurrentLobbyOwnerId != 0)
                {
                    Settings.KickedHostIds.Add(Settings.CurrentLobbyOwnerId);
                    Debug.Log($"[LethalMenu] Marked host {Settings.CurrentLobbyOwnerId} as kicked (host disconnect)");
                }
                Settings.HostQuit = false;
            }
            else if (GameNetworkManager.Instance.disconnectReason == 3)
            {
                if (Settings.CurrentLobbyOwnerId != 0)
                {
                    Settings.KickedHostIds.Add(Settings.CurrentLobbyOwnerId);
                    Debug.Log($"[LethalMenu] Marked host {Settings.CurrentLobbyOwnerId} as kicked (explicit kick)");
                }
            }
            
            Settings.CurrentLobbyOwnerId = 0;
            Settings.CurrentLobbyId = 0;
        }

        /// Detect when host disconnects (not a kick).
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientDisconnect))]
        [HarmonyPostfix]
        public static void OnClientDisconnectPostfix(StartOfRound __instance, ulong clientId)
        {
            if (StartOfRound.Instance.ClientPlayerList.TryGetValue(clientId, out var playerIndex))
            {
                if (playerIndex == 0 && !GameNetworkManager.Instance.isDisconnecting)
                {
                    Settings.HostQuit = true;
                }
            }
        }

        /// Track lobby ID and owner ID when joining.
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.JoinLobby))]
        [HarmonyPostfix]
        public static void JoinLobbyPostfix(GameNetworkManager __instance, Steamworks.Data.Lobby lobby, Steamworks.SteamId id)
        {
            Settings.CurrentLobbyId = lobby.Id;
            Settings.CurrentLobbyOwnerId = lobby.Owner.Id;
            
            if (Hack.AntiKick.IsEnabled() && Settings.KickedHostIds.Contains(lobby.Owner.Id))
            {
                Settings.WasKicked = true;
            }
        }

        /// Override player values to rejoin after kick.
        [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesServerRpc")]
        [HarmonyPrefix]
        public static bool SendNewPlayerValuesServerRpcPrefix(PlayerControllerB __instance, ulong newPlayerSteamId)
        {
            if (Hack.AntiKick.IsEnabled() && Settings.WasKicked)
            {
                __instance.sentPlayerValues = true;
                ulong[] playerSteamIds = new ulong[__instance.playersManager.allPlayerScripts.Length];
                for (int i = 0; i < __instance.playersManager.allPlayerScripts.Length; i++)
                {
                    playerSteamIds[i] = __instance.playersManager.allPlayerScripts[i].playerSteamId;
                }
                playerSteamIds[__instance.playerClientId] = Steamworks.SteamClient.SteamId;

                var method = __instance.GetType().GetMethod("SendNewPlayerValuesClientRpc",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                method?.Invoke(__instance, new object[] { playerSteamIds });

                Settings.WasKicked = false;
                return false;
            }
            return true;
        }

        /// Update local player values after receiving client RPC.
        [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesClientRpc")]
        [HarmonyPostfix]
        public static void SendNewPlayerValuesClientRpcPostfix(PlayerControllerB __instance)
        {
            if (!Hack.AntiKick.IsEnabled()) return;
            if (LethalMenuMod.LocalPlayer == null) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            localPlayer.playerSteamId = Steamworks.SteamClient.SteamId;
            localPlayer.playerUsername = Steamworks.SteamClient.Name;
            localPlayer.usernameBillboardText.text = Steamworks.SteamClient.Name;

            int playerIndex = (int)localPlayer.playerClientId;
            if (playerIndex >= 0 && playerIndex < __instance.playersManager.mapScreen.radarTargets.Count)
            {
                __instance.playersManager.mapScreen.radarTargets[playerIndex].name = localPlayer.playerUsername;
            }

            __instance.quickMenuManager.AddUserToPlayerList(
                localPlayer.playerSteamId,
                localPlayer.playerUsername,
                playerIndex);
        }
    }
}
