using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Patches for StartOfRound to track game state.
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

    /// Voice patches for hearing everyone.
    [HarmonyPatch(typeof(StartOfRound))]
    public static class VoicePatches
    {
        [HarmonyPatch("UpdatePlayerVoiceEffects")]
        [HarmonyPrefix]
        public static bool UpdateVoicePrefix()
        {
            return !Hack.HearEveryone.IsEnabled();
        }
    }

    /// Hear dead people patches.
    [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
    public static class HearDeadPeoplePatches
    {
        [HarmonyPostfix]
        public static void Postfix(StartOfRound __instance)
        {
            if (!Hack.HearDeadPeople.IsEnabled() || StartOfRound.Instance.shipIsLeaving) return;

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

    /// Death notification patches.
    [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerClientRpc")]
    public static class DeathNotificationPatches
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerControllerB __instance, int playerId, int causeOfDeath)
        {
            if (!Hack.DeathNotifications.IsEnabled()) return;
            
            var died = __instance.playersManager.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            string causeName = ((CauseOfDeath)causeOfDeath).ToString();
            
            HUDManager.Instance?.DisplayTip("Death", $"{died.playerUsername} died: {causeName}");
        }
    }

    /// Fake death patches - prevents local player from actually dying when FakeDeath is enabled.
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class FakeDeathPatches
    {
        /// When FakeDeath is active, skip the actual KillPlayer execution on local client.
        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        public static bool KillPlayerFakeDeathPrefix(PlayerControllerB __instance)
        {
            if (Settings.FakeDeath && __instance == LethalMenuMod.LocalPlayer)
            {
                return false;
            }
            return true;
        }

        /// Also block the ClientRpc from killing us when FakeDeath is active.
        [HarmonyPatch("KillPlayerClientRpc")]
        [HarmonyPrefix]
        public static bool KillPlayerClientRpcPrefix(PlayerControllerB __instance, int playerId)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return true;

            if (Settings.FakeDeath && playerId == (int)localPlayer.playerClientId)
            {
                return false;
            }
            return true;
        }
    }
}
