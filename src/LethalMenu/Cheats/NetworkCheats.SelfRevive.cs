using System;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Self-Revive (Client-Side Respawn)

        /// Respawns the local player after death. This is a client-side respawn
        /// that resets all player states locally without needing host permissions.
        /// Note: Other players will still see you as dead. The body may remain.
        /// Based on StartOfRound.ReviveDeadPlayers logic.
        public static void SelfRevive()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.Log("[NetworkCheats] SelfRevive: Local player not found.");
                return;
            }

            if (!localPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] SelfRevive: Player is not dead.");
                return;
            }

            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] SelfRevive: Not in game.");
                return;
            }

            try
            {
                // Clear the global "all players dead" flag
                startOfRound.allPlayersDead = false;

                // Reset blood objects
                localPlayer.ResetPlayerBloodObjects(localPlayer.isPlayerDead);

                // Reset climbing state
                localPlayer.isClimbingLadder = false;
                localPlayer.ResetZAndXRotation();

                // Enable character controller
                localPlayer.thisController.enabled = true;

                // Restore health
                localPlayer.health = 100;
                localPlayer.disableLookInput = false;

                // Reset death state
                localPlayer.isPlayerDead = false;
                localPlayer.isPlayerControlled = true;
                localPlayer.isInElevator = true;
                localPlayer.isInHangarShipRoom = true;
                localPlayer.isInsideFactory = false;

                // Disable extrapolation
                startOfRound.SetPlayerObjectExtrapolate(false);

                // Teleport to ship spawn position
                if (startOfRound.playerSpawnPositions != null && startOfRound.playerSpawnPositions.Length > 0)
                {
                    localPlayer.TeleportPlayer(startOfRound.playerSpawnPositions[0].position, false, 0f, false, true);
                }

                localPlayer.setPositionOfDeadPlayer = false;

                // Re-enable player model
                localPlayer.DisablePlayerModel(startOfRound.allPlayerObjects[localPlayer.playerClientId], true, true);

                // Reset visual states
                localPlayer.helmetLight.enabled = false;
                localPlayer.Crouch(false);
                localPlayer.criticallyInjured = false;
                localPlayer.playerBodyAnimator?.SetBool("Limp", false);
                localPlayer.bleedingHeavily = false;
                localPlayer.activatingItem = false;
                localPlayer.twoHanded = false;
                localPlayer.inSpecialInteractAnimation = false;
                localPlayer.disableSyncInAnimation = false;
                localPlayer.inAnimationWithEnemy = null;
                localPlayer.holdingWalkieTalkie = false;
                localPlayer.speakingToWalkieTalkie = false;
                localPlayer.isSinking = false;
                localPlayer.isUnderwater = false;
                localPlayer.sinkingValue = 0f;
                localPlayer.statusEffectAudio?.Stop();
                localPlayer.DisableJetpackControlsLocally();

                // Reset radar dot animation
                localPlayer.mapRadarDotAnimator?.SetBool("dead", false);

                // Owner-specific resets (should always be true for local player)
                if (localPlayer.IsOwner)
                {
                    var hud = HUDManager.Instance;
                    if (hud != null)
                    {
                        hud.gasHelmetAnimator?.SetBool("gasEmitting", false);
                        hud.RemoveSpectateUI();
                        hud.gameOverAnimator?.SetTrigger("revive");
                        hud.UpdateHealthUI(100, false);
                        hud.audioListenerLowPass.enabled = false;
                    }

                    localPlayer.hasBegunSpectating = false;
                    localPlayer.hinderedMultiplier = 1f;
                    localPlayer.isMovementHindered = 0;
                    localPlayer.sourcesCausingSinking = 0;
                    localPlayer.reverbPreset = startOfRound.shipReverb;
                }

                // Reset audio effects
                var soundManager = SoundManager.Instance;
                if (soundManager != null)
                {
                    soundManager.earsRingingTimer = 0f;
                    soundManager.playerVoicePitchTargets[localPlayer.playerClientId] = 1f;
                    soundManager.SetPlayerPitch(1f, (int)localPlayer.playerClientId);
                }

                localPlayer.voiceMuffledByEnemy = false;

                // Refresh voice chat
                if (localPlayer.currentVoiceChatIngameSettings == null)
                {
                    startOfRound.RefreshPlayerVoicePlaybackObjects();
                }

                if (localPlayer.currentVoiceChatIngameSettings?.voiceAudio != null)
                {
                    var occludeAudio = localPlayer.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>();
                    if (occludeAudio != null)
                    {
                        occludeAudio.overridingLowPass = false;
                    }
                }

                // Reset spectating state
                localPlayer.spectatedPlayerScript = null;
                startOfRound.SetSpectateCameraToGameOverMode(false, localPlayer);

                // Update living player count
                startOfRound.livingPlayers = startOfRound.connectedPlayersAmount + 1;
                startOfRound.allPlayersDead = false;
                startOfRound.UpdatePlayerVoiceEffects();
                startOfRound.shipAnimator?.ResetTrigger("ShipLeave");

                Debug.Log("[NetworkCheats] SelfRevive: Successfully respawned local player.");
                HUDManager.Instance?.DisplayTip("Self Revive", "You have been respawned at the ship!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkCheats] SelfRevive failed: {ex.Message}");
                HUDManager.Instance?.DisplayTip("Self Revive", "Failed to respawn. Try again.");
            }
        }

        #endregion
    }
}
