using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// <summary>
    /// Patches for PlayerControllerB to implement various cheats.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerPatches
    {
        /// <summary>
        /// God mode - prevent damage.
        /// </summary>
        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        public static bool DamagePlayerPrefix(PlayerControllerB __instance)
        {
            // Only block damage for local player when god mode is enabled
            if (Settings.GodMode && __instance == LethalMenuMod.LocalPlayer)
            {
                return false; // Skip original method
            }
            return true; // Run original method
        }

        /// <summary>
        /// God mode - prevent kill.
        /// </summary>
        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        public static bool KillPlayerPrefix(PlayerControllerB __instance)
        {
            if (Settings.GodMode && __instance == LethalMenuMod.LocalPlayer)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// No fall damage.
        /// </summary>
        [HarmonyPatch("PlayerHitGroundEffects")]
        [HarmonyPrefix]
        public static bool PlayerHitGroundPrefix(PlayerControllerB __instance)
        {
            if (Settings.NoFallDamage && __instance == LethalMenuMod.LocalPlayer)
            {
                // Set fall value to 0 to prevent damage calculation
                __instance.fallValue = 0f;
                __instance.fallValueUncapped = 0f;
            }
            return true;
        }

        /// <summary>
        /// Player update - various runtime cheats.
        /// </summary>
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(PlayerControllerB __instance)
        {
            if (__instance != LethalMenuMod.LocalPlayer) return;

            if (Settings.InfiniteStamina)
            {
                __instance.sprintMeter = 1f;
            }

            if (Settings.SuperSpeed)
            {
                __instance.movementSpeed = 10f; // Default is ~4.6
            }

            if (Settings.FastClimb)
            {
                __instance.climbSpeed = 8f; // Default is ~4
            }

            if (Settings.UnlimitedOxygen)
            {
                __instance.drunkness = 0f;
                __instance.drunknessInertia = 0f;
            }

            if (Settings.Reach)
            {
                __instance.grabDistance = 30f; // Default is 5, extended significantly
            }
        }

        /// <summary>
        /// One-handed items - forces twoHanded to false.
        /// </summary>
        [HarmonyPatch("LateUpdate")]
        [HarmonyPostfix]
        public static void LateUpdatePostfix(PlayerControllerB __instance)
        {
            if (__instance != LethalMenuMod.LocalPlayer) return;

            if (Settings.OneHanded)
            {
                __instance.twoHanded = false;
            }
        }

        /// <summary>
        /// TauntSlide - allow emoting while moving.
        /// </summary>
        [HarmonyPatch("CheckConditionsForEmote")]
        [HarmonyPostfix]
        public static void CheckEmotePostfix(ref bool __result, PlayerControllerB __instance)
        {
            if (Settings.TauntSlide && __instance == LethalMenuMod.LocalPlayer)
            {
                // Override walking check - allow emoting while walking
                if (__instance.isPlayerControlled && !__instance.isPlayerDead && !__instance.inSpecialInteractAnimation)
                {
                    __result = true;
                }
            }
        }

        /// <summary>
        /// GrabInLobby - allow grabbing items before game starts.
        /// </summary>
        [HarmonyPatch("GrabObjectServerRpc")]
        [HarmonyPrefix]
        public static void GrabObjectPrefix(PlayerControllerB __instance)
        {
            if (Settings.GrabInLobby && __instance == LethalMenuMod.LocalPlayer)
            {
                // Force game to think we can grab
                if (StartOfRound.Instance != null)
                {
                    StartOfRound.Instance.localPlayerUsingController = false;
                }
            }
        }

        /// <summary>
        /// Unlimited jump - allows jumping in air.
        /// </summary>
        [HarmonyPatch("Jump_performed")]
        [HarmonyPrefix]
        public static bool JumpPrefix(PlayerControllerB __instance)
        {
            if (!Settings.UnlimitedJump) return true;
            if (__instance != LethalMenuMod.LocalPlayer) return true;
            if (!__instance.isPlayerControlled) return false;
            if (__instance.inSpecialInteractAnimation) return false;
            if (__instance.isTypingChat) return false;
            if (__instance.quickMenuManager?.isMenuOpen == true) return false;

            // Consume a bit of stamina
            __instance.sprintMeter = Mathf.Clamp(__instance.sprintMeter - 0.08f, 0f, 1f);
            
            // Play jump sound
            if (__instance.movementAudio != null && StartOfRound.Instance?.playerJumpSFX != null)
            {
                __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerJumpSFX);
            }
            
            __instance.playerSlidingTimer = 0f;
            __instance.isJumping = true;

            // Stop existing jump coroutine
            if (__instance.jumpCoroutine != null)
            {
                __instance.StopCoroutine(__instance.jumpCoroutine);
            }

            // Start new jump
            __instance.jumpCoroutine = __instance.StartCoroutine(__instance.PlayerJump());

            return false; // Skip original
        }
    }

    /// <summary>
    /// Patches for StartOfRound to track game state.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    public static class StartOfRoundPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePostfix(StartOfRound __instance)
        {
            LethalMenuMod.GameInstance = __instance;
            Debug.Log("[LethalMenu] StartOfRound instance captured.");
        }
    }

    /// <summary>
    /// Patches for EnemyAI.
    /// </summary>
    [HarmonyPatch(typeof(EnemyAI))]
    public static class EnemyPatches
    {
        /// <summary>
        /// Make enemies unable to target local player.
        /// </summary>
        [HarmonyPatch("PlayerIsTargetable")]
        [HarmonyPostfix]
        public static void PlayerIsTargetablePostfix(ref bool __result, PlayerControllerB playerScript)
        {
            if (Settings.Untargetable && playerScript == LethalMenuMod.LocalPlayer)
            {
                __result = false;
            }
        }

        /// <summary>
        /// Clear enemy target if it's the local player.
        /// </summary>
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool EnemyUpdatePrefix(EnemyAI __instance)
        {
            if (!Settings.Untargetable) return true;
            if (__instance.targetPlayer != LethalMenuMod.LocalPlayer) return true;

            __instance.targetPlayer = null;
            __instance.movingTowardsTargetPlayer = false;
            return true;
        }

        /// <summary>
        /// Block noise detection from local player.
        /// </summary>
        [HarmonyPatch("DetectNoise")]
        [HarmonyPrefix]
        public static bool DetectNoisePrefix(EnemyAI __instance, Vector3 noisePosition)
        {
            if (!Settings.Untargetable) return true;
            if (LethalMenuMod.LocalPlayer == null) return true;

            // Block noise if it's within 5m of local player
            float distToPlayer = Vector3.Distance(noisePosition, LethalMenuMod.LocalPlayer.transform.position);
            if (distToPlayer < 5f)
            {
                return false; // Skip noise detection
            }
            return true;
        }
    }

    /// <summary>
    /// MouthDog-specific patches - they are blind and use sound.
    /// </summary>
    [HarmonyPatch(typeof(MouthDogAI))]
    public static class MouthDogPatches
    {
        /// <summary>
        /// Block MouthDog from detecting local player's noise.
        /// </summary>
        [HarmonyPatch("DetectNoise")]
        [HarmonyPrefix]
        public static bool DetectNoisePrefix(MouthDogAI __instance, Vector3 noisePosition)
        {
            if (!Settings.Untargetable) return true;
            if (LethalMenuMod.LocalPlayer == null) return true;

            // Block noise if it's within 8m of local player (dogs have better hearing)
            float distToPlayer = Vector3.Distance(noisePosition, LethalMenuMod.LocalPlayer.transform.position);
            if (distToPlayer < 8f)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Block MouthDog enrage towards local player.
        /// </summary>
        [HarmonyPatch("EnterLunge")]
        [HarmonyPrefix]
        public static bool EnterLungePrefix(MouthDogAI __instance)
        {
            if (!Settings.Untargetable) return true;
            
            // Check if lunging at local player
            if (__instance.targetPlayer == LethalMenuMod.LocalPlayer)
            {
                __instance.targetPlayer = null;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Turret patches - make turrets ignore local player when Untargetable.
    /// </summary>
    [HarmonyPatch(typeof(Turret))]
    public static class TurretPatches
    {
        /// <summary>
        /// Return null if turret would target local player.
        /// </summary>
        [HarmonyPatch("CheckForPlayersInLineOfSight")]
        [HarmonyPostfix]
        public static void CheckForPlayersPostfix(ref PlayerControllerB __result)
        {
            if (!Settings.Untargetable) return;
            if (__result == LethalMenuMod.LocalPlayer)
            {
                __result = null!;
            }
        }

        /// <summary>
        /// Clear turret target if it's the local player.
        /// </summary>
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(Turret __instance)
        {
            if (!Settings.Untargetable) return;
            if (__instance.targetPlayerWithRotation == LethalMenuMod.LocalPlayer)
            {
                __instance.targetPlayerWithRotation = null;
            }
        }
    }

    /// <summary>
    /// Anti-flash patches for HUDManager.
    /// </summary>
    [HarmonyPatch(typeof(HUDManager))]
    public static class HUDPatches
    {
        /// <summary>
        /// Disable flash filter (stun grenades).
        /// </summary>
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(HUDManager __instance)
        {
            LethalMenuMod.HUD = __instance;

            if (Settings.AntiFlash)
            {
                __instance.flashFilter = 0f;
            }
        }
    }

    /// <summary>
    /// Anti-flash patches for SoundManager (ears ringing).
    /// </summary>
    [HarmonyPatch(typeof(SoundManager))]
    public static class SoundPatches
    {
        /// <summary>
        /// Disable ears ringing from stun grenades.
        /// </summary>
        [HarmonyPatch("SetEarsRinging")]
        [HarmonyPrefix]
        public static bool SetEarsRingingPrefix()
        {
            return !Settings.AntiFlash; // Skip if anti-flash enabled
        }
    }

    /// <summary>
    /// Patches for GrabbableObject (items).
    /// </summary>
    [HarmonyPatch(typeof(GrabbableObject))]
    public static class ItemPatches
    {
        /// <summary>
        /// Infinite battery for held items.
        /// </summary>
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(GrabbableObject __instance)
        {
            if (!Settings.InfiniteBattery) return;

            // Check if this item is held by local player
            if (__instance.playerHeldBy == LethalMenuMod.LocalPlayer)
            {
                if (__instance.insertedBattery != null)
                {
                    __instance.insertedBattery.charge = 1f;
                }
            }
        }
    }

    /// <summary>
    /// Patches for QuickMenuManager to prevent menu interference.
    /// </summary>
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

    /// <summary>
    /// No Quicksand patch - prevents sinking/slowing.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class NoQuicksandPatch
    {
        [HarmonyPatch("CheckConditionsForSinkingInQuicksand")]
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, PlayerControllerB __instance)
        {
            if (Settings.NoQuicksand && __instance == LethalMenuMod.LocalPlayer)
            {
                __result = false; // Never sink
            }
        }
    }

    /// <summary>
    /// Shovel patches for super shovel.
    /// </summary>
    [HarmonyPatch(typeof(Shovel))]
    public static class ShovelPatches
    {
        [HarmonyPatch("HitShovel")]
        [HarmonyPrefix]
        public static void HitShovelPrefix(Shovel __instance)
        {
            if (Settings.SuperShovel && __instance.playerHeldBy == LethalMenuMod.LocalPlayer)
            {
                __instance.shovelHitForce = 100; // One-shot most enemies
            }
        }
    }

    /// <summary>
    /// Shotgun patches for unlimited ammo.
    /// </summary>
    [HarmonyPatch(typeof(ShotgunItem))]
    public static class ShotgunPatches
    {
        [HarmonyPatch("ShootGun")]
        [HarmonyPostfix]
        public static void ShootGunPostfix(ShotgunItem __instance)
        {
            if (Settings.UnlimitedAmmo && __instance.playerHeldBy == LethalMenuMod.LocalPlayer)
            {
                __instance.shellsLoaded = 2; // Always full
            }
        }
    }

    /// <summary>
    /// Terminal patches for shoplifter (free items).
    /// </summary>
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

    /// <summary>
    /// Deposit desk patches to prevent Jeb attacks.
    /// </summary>
    [HarmonyPatch(typeof(DepositItemsDesk))]
    public static class DepositDeskPatches
    {
        [HarmonyPatch("Attack")]
        [HarmonyPrefix]
        public static bool AttackPrefix()
        {
            return !Settings.JebAttackPrevention; // Skip attack if enabled
        }

        [HarmonyPatch("AttackPlayersServerRpc")]
        [HarmonyPrefix]
        public static bool AttackServerPrefix()
        {
            return !Settings.JebAttackPrevention;
        }
    }

    /// <summary>
    /// Build anywhere - allow placing ship objects outside ship bounds.
    /// </summary>
    [HarmonyPatch(typeof(ShipBuildModeManager))]
    public static class BuildModePatches
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(ShipBuildModeManager __instance)
        {
            if (Settings.BuildAnywhere && __instance.InBuildMode)
            {
                __instance.CanConfirmPosition = true; // Always allow placement
            }
        }
    }

    /// <summary>
    /// Instant interact - skip hold interaction delay.
    /// </summary>
    [HarmonyPatch(typeof(HUDManager))]
    public static class InstantInteractPatches
    {
        [HarmonyPatch("HoldInteractionFill")]
        [HarmonyPrefix]
        public static bool HoldInteractionFillPrefix(ref bool __result)
        {
            if (Settings.InstantInteract)
            {
                __result = true; // Instantly complete the hold
                return false; // Skip original
            }
            return true;
        }
    }

    /// <summary>
    /// Voice patches for hearing everyone.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    public static class VoicePatches
    {
        [HarmonyPatch("UpdatePlayerVoiceEffects")]
        [HarmonyPrefix]
        public static bool UpdateVoicePrefix()
        {
            // Skip voice distance/filter effects if HearEveryone enabled
            return !Settings.HearEveryone;
        }
    }

    /// <summary>
    /// Invisibility - sends fake position to server, restores on client.
    /// Other players see you at y=-100 (underground).
    /// </summary>
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
            if (!Settings.Invisibility) return;
            if (__instance != LethalMenuMod.LocalPlayer) return;

            // Save real values
            _lastRealPos = newPos;
            _lastInElevator = inElevator;
            _lastInShipRoom = inShipRoom;
            _lastExhausted = exhausted;
            _lastGrounded = isPlayerGrounded;

            // Send fake position (underground)
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
            if (!Settings.Invisibility) return;
            if (__instance != LethalMenuMod.LocalPlayer) return;

            // Restore real values for local client
            newPos = _lastRealPos;
            inElevator = _lastInElevator;
            isInShip = _lastInShipRoom;
            exhausted = _lastExhausted;
            isPlayerGrounded = _lastGrounded;
        }
    }

    /// <summary>
    /// Anti-kick patches - allows rejoining lobbies after being kicked.
    /// </summary>
    [HarmonyPatch]
    public static class AntiKickPatches
    {
        /// <summary>
        /// Track disconnections to detect kicks.
        /// </summary>
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        public static void DisconnectPostfix(GameNetworkManager __instance)
        {
            // disconnectReason 1 = host quit, 3 = kicked
            if (GameNetworkManager.Instance.disconnectReason == 1)
            {
                if (!Settings.HostQuit && Settings.CurrentLobbyId != 0)
                {
                    Settings.KickedFromLobbies.Add(Settings.CurrentLobbyId);
                }
                Settings.HostQuit = false;
            }
            else if (GameNetworkManager.Instance.disconnectReason == 3)
            {
                if (Settings.CurrentLobbyId != 0)
                {
                    Settings.KickedFromLobbies.Add(Settings.CurrentLobbyId);
                }
            }
        }

        /// <summary>
        /// Detect when host disconnects (not a kick).
        /// </summary>
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientDisconnect))]
        [HarmonyPostfix]
        public static void OnClientDisconnectPostfix(StartOfRound __instance, ulong clientId)
        {
            // Check if host (player index 0) disconnected
            if (StartOfRound.Instance.ClientPlayerList.TryGetValue(clientId, out var playerIndex))
            {
                if (playerIndex == 0 && !GameNetworkManager.Instance.isDisconnecting)
                {
                    Settings.HostQuit = true;
                }
            }
        }

        /// <summary>
        /// Track lobby ID when joining.
        /// </summary>
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.JoinLobby))]
        [HarmonyPostfix]
        public static void JoinLobbyPostfix(GameNetworkManager __instance, Steamworks.Data.Lobby lobby, Steamworks.SteamId id)
        {
            Settings.CurrentLobbyId = lobby.Id;
            if (Settings.AntiKick && Settings.KickedFromLobbies.Contains(lobby.Owner.Id))
            {
                Settings.WasKicked = true;
            }
        }

        /// <summary>
        /// Override player values to rejoin after kick.
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesServerRpc")]
        [HarmonyPrefix]
        public static bool SendNewPlayerValuesServerRpcPrefix(PlayerControllerB __instance, ulong newPlayerSteamId)
        {
            if (Settings.AntiKick && Settings.WasKicked)
            {
                __instance.sentPlayerValues = true;
                ulong[] playerSteamIds = new ulong[__instance.playersManager.allPlayerScripts.Length];
                for (int i = 0; i < __instance.playersManager.allPlayerScripts.Length; i++)
                {
                    playerSteamIds[i] = __instance.playersManager.allPlayerScripts[i].playerSteamId;
                }
                playerSteamIds[__instance.playerClientId] = Steamworks.SteamClient.SteamId;

                // Use reflection to call the RPC
                var method = __instance.GetType().GetMethod("SendNewPlayerValuesClientRpc",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                method?.Invoke(__instance, new object[] { playerSteamIds });

                Settings.WasKicked = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Update local player values after receiving client RPC.
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesClientRpc")]
        [HarmonyPostfix]
        public static void SendNewPlayerValuesClientRpcPostfix(PlayerControllerB __instance)
        {
            if (!Settings.AntiKick) return;
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

    /// <summary>
    /// Anti Ghost Girl patches.
    /// </summary>
    [HarmonyPatch(typeof(DressGirlAI))]
    public static class AntiGhostGirlPatches
    {
        [HarmonyPatch("ChoosePlayerToHaunt")]
        [HarmonyPostfix]
        public static void ChoosePlayerToHauntPostfix(DressGirlAI __instance)
        {
            if (!Settings.AntiGhostGirl || __instance == null) return;
            if (__instance.hauntingLocalPlayer)
            {
                __instance.hauntingPlayer = null;
            }
        }

        [HarmonyPatch("BeginChasing")]
        [HarmonyPostfix]
        public static void BeginChasingPostfix(DressGirlAI __instance)
        {
            if (!Settings.AntiGhostGirl || __instance == null) return;
            if (__instance.hauntingLocalPlayer)
            {
                __instance.hauntingPlayer = null;
            }
        }
    }

    /// <summary>
    /// Ghost mode - enemies cannot see you (alternative to Untargetable).
    /// </summary>
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.PlayerIsTargetable))]
    public static class GhostModePatches
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerControllerB playerScript)
        {
            if (Settings.GhostMode && LethalMenuMod.LocalPlayer != null && 
                LethalMenuMod.LocalPlayer.playerClientId == playerScript.playerClientId)
            {
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Bridge never falls patches.
    /// </summary>
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

    /// <summary>
    /// Open dropship on land.
    /// </summary>
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

    /// <summary>
    /// Open ship door in space.
    /// </summary>
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

    /// <summary>
    /// No camera shake.
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), "ShakeCamera")]
    public static class NoCameraShakePatches
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !Settings.NoCameraShake;
        }
    }

    /// <summary>
    /// No depth of field.
    /// </summary>
    [HarmonyPatch(typeof(UnityEngine.Rendering.HighDefinition.DepthOfField), "IsActive")]
    public static class NoFieldOfDepthPatches
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !Settings.NoFieldOfDepth;
        }
    }

    /// <summary>
    /// Eggs always explode.
    /// </summary>
    [HarmonyPatch]
    public static class EggsPatches
    {
        [HarmonyPatch(typeof(StunGrenadeItem), nameof(StunGrenadeItem.SetExplodeOnThrowClientRpc))]
        [HarmonyPrefix]
        public static bool SetExplodePrefix(StunGrenadeItem __instance)
        {
            // Never explode takes priority
            if (Settings.EggsNeverExplode && !Settings.EggsAlwaysExplode)
            {
                if (LethalMenuMod.LocalPlayer?.currentlyHeldObjectServer?.name == "EasterEgg(Clone)")
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Super knife damage.
    /// </summary>
    [HarmonyPatch(typeof(KnifeItem), "HitKnife")]
    public static class SuperKnifePatches
    {
        [HarmonyPrefix]
        public static void Prefix(KnifeItem __instance)
        {
            __instance.knifeHitForce = Settings.SuperKnife ? 1000 : 1;
        }
    }

    /// <summary>
    /// Super jump force.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "PlayerJump")]
    public static class SuperJumpPatches
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerControllerB __instance)
        {
            if (LethalMenuMod.LocalPlayer == null || __instance != LethalMenuMod.LocalPlayer) return;
            __instance.jumpForce = Settings.SuperJump ? Settings.SuperJumpForce : 13f;
        }
    }

    /// <summary>
    /// Strong hands - two-handed items can be held one-handed.
    /// </summary>
    [HarmonyPatch]
    public static class StrongHandsPatches
    {
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        public static void Postfix(PlayerControllerB __instance)
        {
            if (LethalMenuMod.LocalPlayer == null || __instance.playerClientId != LethalMenuMod.LocalPlayer.playerClientId) return;
            
            var heldObject = __instance.currentlyHeldObjectServer;
            if (Settings.StrongHands && heldObject != null)
            {
                __instance.twoHanded = false;
            }
        }
    }

    /// <summary>
    /// Teleport with items - don't drop items when teleporting.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "DropAllHeldItems")]
    public static class TeleportWithItemsPatches
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !Settings.TeleportWithItems;
        }
    }

    /// <summary>
    /// Unlimited zap gun patches.
    /// </summary>
    [HarmonyPatch]
    public static class UnlimitedZapGunPatches
    {
        [HarmonyPatch(typeof(PatcherTool), "ShiftBendRandomizer")]
        [HarmonyPostfix]
        public static void ShiftBendPostfix(ref float ___bendMultiplier)
        {
            if (Settings.UnlimitedZapGun)
            {
                ___bendMultiplier = 0f;
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), "RequireCooldown")]
        [HarmonyPostfix]
        public static void CooldownPostfix(GrabbableObject __instance)
        {
            if (Settings.UnlimitedZapGun && __instance is PatcherTool)
            {
                __instance.currentUseCooldown = 0f;
            }
        }
    }

    /// <summary>
    /// Grab nutcracker's shotgun.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "BeginGrabObject")]
    public static class GrabNutcrackerShotgunPatches
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerControllerB __instance)
        {
            if (!Settings.GrabNutcrackerShotgun) return;
            
            // Use reflection to get the currently grabbing object
            var field = __instance.GetType().GetField("currentlyGrabbingObject", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) return;
            
            var grabbableObject = field.GetValue(__instance) as GrabbableObject;
            if (grabbableObject == null) return;
            
            var shotgun = grabbableObject as ShotgunItem;
            if (shotgun == null) return;
            
            // Get heldByEnemy field
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

    /// <summary>
    /// Through walls patches.
    /// </summary>
    [HarmonyPatch]
    public static class ThroughWallsPatches
    {
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        public static void ThroughWallsPostfix(PlayerControllerB __instance)
        {
            if (__instance != LethalMenuMod.LocalPlayer) return;

            if (Settings.LootThroughWalls || Settings.InteractThroughWalls)
            {
                __instance.grabDistance = 10000f;
                int mask = 0;
                if (Settings.LootThroughWalls)
                    mask = LayerMask.GetMask("Props");
                if (Settings.InteractThroughWalls)
                    mask = LayerMask.GetMask("InteractableObject");
                if (Settings.LootThroughWalls && Settings.InteractThroughWalls)
                    mask = LayerMask.GetMask("Props", "InteractableObject");

                // Set via reflection
                var field = __instance.GetType().GetField("interactableObjectsMask",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(__instance, mask);
            }
            else if (!Settings.Reach)
            {
                // Reset to defaults
                var field = __instance.GetType().GetField("interactableObjectsMask",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(__instance, 832);
                __instance.grabDistance = 5f;
            }
        }
    }

    /// <summary>
    /// Death notification patches.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerClientRpc")]
    public static class DeathNotificationPatches
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerControllerB __instance, int playerId, int causeOfDeath)
        {
            if (!Settings.DeathNotifications) return;
            
            var died = __instance.playersManager.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            string causeName = ((CauseOfDeath)causeOfDeath).ToString();
            
            HUDManager.Instance?.DisplayTip("Death", $"{died.playerUsername} died: {causeName}");
        }
    }

    /// <summary>
    /// Hear dead people patches.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
    public static class HearDeadPeoplePatches
    {
        [HarmonyPostfix]
        public static void Postfix(StartOfRound __instance)
        {
            if (!Settings.HearDeadPeople || StartOfRound.Instance.shipIsLeaving) return;

            for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB player = __instance.allPlayerScripts[i];
                if (player == null || player.currentVoiceChatAudioSource == null || !player.isPlayerDead) continue;

                var audioSource = player.currentVoiceChatAudioSource;
                var lowPass = audioSource.GetComponent<AudioLowPassFilter>();
                var highPass = audioSource.GetComponent<AudioHighPassFilter>();

                if (lowPass != null) lowPass.enabled = false;
                if (highPass != null) highPass.enabled = false;

                audioSource.panStereo = 0f;
                SoundManager.Instance.playerVoicePitchTargets[i] = 1f;
                SoundManager.Instance.SetPlayerPitch(1f, i);
                audioSource.spatialBlend = 0f;
                player.currentVoiceChatIngameSettings.set2D = true;
                player.voicePlayerState.Volume = 1f;
            }
        }
    }

    /// <summary>
    /// Loot before game starts patches.
    /// </summary>
    [HarmonyPatch]
    public static class LootBeforeGameStartsPatches
    {
        private static readonly System.Collections.Generic.Dictionary<GrabbableObject, bool> ModifiedItems = 
            new System.Collections.Generic.Dictionary<GrabbableObject, bool>();

        [HarmonyPatch(typeof(PlayerControllerB), "BeginGrabObject")]
        [HarmonyPrefix]
        public static void BeginGrabPrefix(PlayerControllerB __instance)
        {
            if (!Settings.LootBeforeGameStarts) return;

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

    /// <summary>
    /// Full render resolution - increases render texture to screen resolution.
    /// Applied via Harmony patch on PlayerControllerB.Start.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "Start")]
    public static class FullRenderResolutionPatch
    {
        private static RenderTexture? _originalTexture;
        private static RenderTexture? _highResTexture;
        private static bool _applied = false;
        
        [HarmonyPostfix]
        public static void Postfix(PlayerControllerB __instance)
        {
            if (__instance.IsOwner)
            {
                ApplyResolution(__instance);
            }
        }
        
        public static void ApplyResolution(PlayerControllerB? player)
        {
            if (player?.gameplayCamera == null) return;
            
            var camera = player.gameplayCamera;
            var currentTexture = camera.targetTexture;
            
            if (Settings.FullRenderResolution)
            {
                // Store original if we haven't already
                if (_originalTexture == null && currentTexture != null)
                {
                    _originalTexture = currentTexture;
                }
                
                // Create high-res texture if needed
                if (_highResTexture == null || _highResTexture.width != Screen.width || _highResTexture.height != Screen.height)
                {
                    // Release old one if exists
                    if (_highResTexture != null)
                    {
                        _highResTexture.Release();
                        UnityEngine.Object.Destroy(_highResTexture);
                    }
                    
                    // Create new high-res render texture with same format as original
                    if (_originalTexture != null)
                    {
                        _highResTexture = new RenderTexture(Screen.width, Screen.height, _originalTexture.depth, _originalTexture.format)
                        {
                            filterMode = FilterMode.Point,
                            name = "LethalMenu_HighResRT"
                        };
                        _highResTexture.Create();
                    }
                    else
                    {
                        _highResTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32)
                        {
                            filterMode = FilterMode.Point,
                            name = "LethalMenu_HighResRT"
                        };
                        _highResTexture.Create();
                    }
                }
                
                // Apply high-res texture
                if (_highResTexture != null && camera.targetTexture != _highResTexture)
                {
                    camera.targetTexture = _highResTexture;
                    _applied = true;
                    Debug.Log($"[FullRenderRes] Applied high-res texture: {Screen.width}x{Screen.height}");
                }
            }
            else if (_applied && _originalTexture != null)
            {
                // Restore original texture
                camera.targetTexture = _originalTexture;
                _applied = false;
                Debug.Log("[FullRenderRes] Restored original texture");
            }
        }
        
        public static void Reset()
        {
            if (_highResTexture != null)
            {
                _highResTexture.Release();
                UnityEngine.Object.Destroy(_highResTexture);
                _highResTexture = null;
            }
            _originalTexture = null;
            _applied = false;
        }
    }
}
