using System.Collections;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Signal Translator Spam (RequireOwnership = false)

        /// Sends a message via signal translator (shows on ship's monitor).
        /// Uses HUDManager.UseSignalTranslatorServerRpc - RequireOwnership = false.
        public static void SendSignalTranslatorMessage(string message)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.Log("[NetworkCheats] HUD not found.");
                return;
            }

            // Max 10 characters for signal translator
            string truncated = message.Length > 10 ? message.Substring(0, 10) : message;
            hud.UseSignalTranslatorServerRpc(truncated);
            Debug.Log($"[NetworkCheats] Signal: {truncated}");
        }

        /// Spams signal translator with messages (very annoying, shows on everyone's ship monitor).
        public static void SpamSignalTranslator(int iterations = 20)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamSignalTranslatorCoroutine(iterations));
            }
        }

        private static IEnumerator SpamSignalTranslatorCoroutine(int iterations)
        {
            var hud = HUDManager.Instance;
            if (hud == null) yield break;

            string[] messages = { "AAAAAAAAAA", "TROLLED", "HACKED", "LOLOLOL", "GOTCHA", "OWNED", "REKT", "GG EZ" };

            for (int i = 0; i < iterations; i++)
            {
                string msg = messages[i % messages.Length];
                hud.UseSignalTranslatorServerRpc(msg);
                yield return new WaitForSeconds(0.3f);
            }

            Debug.Log($"[NetworkCheats] Spammed signal translator {iterations} times.");
        }

        #endregion

        #region Ship Horn Spam (RequireOwnership = false)

        /// Pulls the ship alarm cord (honks the horn).
        /// Uses ShipAlarmCord.PullCordServerRpc - RequireOwnership = false.
        public static void PullShipHorn()
        {
            var alarmCord = Object.FindObjectOfType<ShipAlarmCord>();
            if (alarmCord == null)
            {
                Debug.Log("[NetworkCheats] Ship alarm cord not found.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            alarmCord.PullCordServerRpc(playerId);
            Debug.Log("[NetworkCheats] Pulled ship horn.");
        }

        /// Stops the ship horn.
        public static void StopShipHorn()
        {
            var alarmCord = Object.FindObjectOfType<ShipAlarmCord>();
            if (alarmCord == null) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            alarmCord.StopPullingCordServerRpc(playerId);
            Debug.Log("[NetworkCheats] Stopped ship horn.");
        }

        /// Spams the ship horn on/off rapidly (extremely annoying).
        public static void SpamShipHorn(int iterations = 15)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamShipHornCoroutine(iterations));
            }
        }

        private static IEnumerator SpamShipHornCoroutine(int iterations)
        {
            var alarmCord = Object.FindObjectOfType<ShipAlarmCord>();
            if (alarmCord == null) yield break;

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            for (int i = 0; i < iterations; i++)
            {
                alarmCord.PullCordServerRpc(playerId);
                yield return new WaitForSeconds(0.2f);
                alarmCord.StopPullingCordServerRpc(playerId);
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"[NetworkCheats] Spammed ship horn {iterations} times.");
        }

        #endregion

        #region Shotgun Spam

        /// Fire all shotguns on the map.
        public static void ShootAllShotguns()
        {
            var shotguns = Object.FindObjectsOfType<ShotgunItem>(includeInactive: true);
            if (shotguns == null || shotguns.Length == 0)
            {
                Debug.Log("[NetworkCheats] No shotguns found.");
                return;
            }

            int count = 0;
            foreach (var shotgun in shotguns)
            {
                if (shotgun != null && shotgun.shellsLoaded > 0)
                {
                    var pos = shotgun.transform.position + shotgun.transform.up * 0.45f;
                    var forward = shotgun.transform.forward;
                    shotgun.ShootGunServerRpc(pos, forward);
                    count++;
                }
            }
            Debug.Log($"[NetworkCheats] Fired {count} shotguns.");
        }

        /// Spam fire all shotguns repeatedly.
        public static void SpamShootAllShotguns(int iterations = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamShotgunsCoroutine(iterations));
            }
        }

        private static IEnumerator SpamShotgunsCoroutine(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                ShootAllShotguns();
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log($"[NetworkCheats] Spam fired shotguns {iterations} times.");
        }

        #endregion

        #region Terminal Sound Spam

        /// Spam terminal sounds (includes earrape invalid indices).
        public static void SpamTerminalSound(int iterations = 20)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamTerminalSoundCoroutine(iterations));
            }
        }

        private static IEnumerator SpamTerminalSoundCoroutine(int iterations)
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null) yield break;

            for (int i = 0; i < iterations; i++)
            {
                // Normal sound
                terminal.PlayTerminalAudioServerRpc(1);
                // Earrape invalid index (cash register sound)
                terminal.PlayTerminalAudioServerRpc(-1);
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log($"[NetworkCheats] Spammed terminal sound + earrape {iterations} times.");
        }

        /// Pure earrape spam - only invalid indices.
        public static void SpamTerminalEarrape(int iterations = 30)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamTerminalEarrapeCoroutine(iterations));
            }
        }

        private static IEnumerator SpamTerminalEarrapeCoroutine(int iterations)
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null) yield break;

            for (int i = 0; i < iterations; i++)
            {
                terminal.PlayTerminalAudioServerRpc(-1);
                terminal.PlayTerminalAudioServerRpc(-99999);
                terminal.PlayTerminalAudioServerRpc(int.MaxValue);
                yield return new WaitForSeconds(0.05f);
            }
            Debug.Log($"[NetworkCheats] Earrape spam {iterations} times.");
        }

        #endregion

        #region Continuous Spam Toggles

        // Timers for continuous spam (to avoid spamming every frame)
        private static float _lastHornSpam = 0f;
        private static float _lastDoorSpam = 0f;
        private static float _lastSignalSpam = 0f;
        private static float _lastRPCSpam = 0f;
        private static float _lastTerminalSpam = 0f;
        private static float _lastEarrapeSpam = 0f;
        private static float _lastChatSpam = 0f;
        private static float _lastCarHornSpam = 0f;
        private static float _lastDeskDoorSpam = 0f;
        private static int _spamCounter = 0;

        /// Call this every frame from Update() to process continuous spam toggles.
        public static void ProcessSpamToggles()
        {
            float time = Time.time;
            var hud = HUDManager.Instance;
            var terminal = Object.FindObjectOfType<Terminal>();

            // Horn Spam (every 0.15s)
            if (Hack.HornSpam.IsEnabled() && time - _lastHornSpam > 0.15f)
            {
                _lastHornSpam = time;
                hud?.AlarmHornServerRpc();
            }

            // Door Spam (every 0.2s)
            if (Hack.DoorSpam.IsEnabled() && time - _lastDoorSpam > 0.2f)
            {
                _lastDoorSpam = time;
                _spamCounter++;
                var startOfRound = StartOfRound.Instance;
                if (startOfRound != null)
                {
                    bool closed = (_spamCounter % 2 == 0);
                    startOfRound.SetDoorsClosedServerRpc(closed);
                }
            }

            // Signal Translator Spam (every 0.3s - has cooldown but we try anyway)
            if (Hack.SignalSpam.IsEnabled() && time - _lastSignalSpam > 0.3f)
            {
                _lastSignalSpam = time;
                _spamCounter++;
                hud?.UseSignalTranslatorServerRpc($"S{_spamCounter % 100:D2}");
            }

            // RPC Lag Spam (every 0.05s - aggressive) - Full chaos: signal, chat, horn, damage, AND terminal sounds
            if (Hack.RPCLagSpam.IsEnabled() && time - _lastRPCSpam > 0.05f)
            {
                _lastRPCSpam = time;
                _spamCounter++;

                if (hud != null)
                {
                    // Signal spam
                    hud.UseSignalTranslatorServerRpc($"L{_spamCounter % 100:D2}");
                    // Chat spam (hidden)
                    hud.AddTextToChatOnServer($"<size=0>{_spamCounter}</size>");
                    // Horn spam every 3rd iteration
                    if (_spamCounter % 3 == 0)
                    {
                        hud.AlarmHornServerRpc();
                    }
                }

                // Terminal audio spam - cycle through indices for variety
                if (terminal != null)
                {
                    terminal.PlayTerminalAudioServerRpc(_spamCounter % 5); // Cycle 0-4
                    if (_spamCounter % 2 == 0)
                    {
                        terminal.PlayTerminalAudioServerRpc(-1); // Invalid for earrape
                    }
                }

                // Damage spam
                var localPlayer = LethalMenuMod.LocalPlayer;
                if (localPlayer != null && !localPlayer.isPlayerDead && _spamCounter % 5 == 0)
                {
                    localPlayer.DamagePlayerFromOtherClientServerRpc(0, Vector3.zero, -1);
                }
            }

            // Terminal Sound Spam (every 0.08s) - FULL combo: index 0 (cash register) + index 1 (beep) + invalid indices
            if (Hack.TerminalSoundSpam.IsEnabled() && time - _lastTerminalSpam > 0.08f)
            {
                _lastTerminalSpam = time;
                _spamCounter++;
                if (terminal != null)
                {
                    // Index 0 = cash register sound (the one Terminal Crash button uses)
                    terminal.PlayTerminalAudioServerRpc(0);
                    // Index 1 = beep
                    terminal.PlayTerminalAudioServerRpc(1);
                    // Invalid indices = earrape
                    terminal.PlayTerminalAudioServerRpc(-1);
                    terminal.PlayTerminalAudioServerRpc(-99999);
                }
            }

            // Terminal Earrape Spam (every 0.03s) - Pure invalid index spam + index 0 for max noise
            if (Hack.EarrapeSpam.IsEnabled() && time - _lastEarrapeSpam > 0.03f)
            {
                _lastEarrapeSpam = time;
                _spamCounter++;
                if (terminal != null)
                {
                    // Index 0 = cash register (the actual sound)
                    terminal.PlayTerminalAudioServerRpc(0);
                    terminal.PlayTerminalAudioServerRpc(0);
                    terminal.PlayTerminalAudioServerRpc(0);
                    // Invalid indices
                    terminal.PlayTerminalAudioServerRpc(-1);
                    terminal.PlayTerminalAudioServerRpc(-99999);
                    terminal.PlayTerminalAudioServerRpc(int.MaxValue);
                }
            }

            // Chat Spam Loop (every 0.5s)
            if (Hack.ChatSpam.IsEnabled() && time - _lastChatSpam > 0.5f)
            {
                _lastChatSpam = time;
                _spamCounter++;
                string msg = Settings.SpamMessage;
                if (msg.Length > 45) msg = msg.Substring(0, 45);
                hud?.AddTextToChatOnServer($"{msg}[{_spamCounter % 1000}]");
            }

            // Car Horn Spam (every 0.15s)
            if (Hack.CarHornSpam.IsEnabled() && time - _lastCarHornSpam > 0.15f)
            {
                _lastCarHornSpam = time;
                _spamCounter++;
                var cars = Object.FindObjectsOfType<VehicleController>(includeInactive: true);
                var localPlayer = LethalMenuMod.LocalPlayer;
                int playerId = localPlayer != null ? (int)localPlayer.playerClientId : -1;
                foreach (var car in cars)
                {
                    if (car != null)
                    {
                        car.SetHonkServerRpc(_spamCounter % 2 == 0, playerId);
                    }
                }
            }

            // Desk Door Spam (every 0.2s)
            if (Hack.DeskDoorSpam.IsEnabled() && time - _lastDeskDoorSpam > 0.2f)
            {
                _lastDeskDoorSpam = time;
                var desk = Object.FindObjectOfType<DepositItemsDesk>();
                desk?.OpenShutDoorClientRpc();
            }
        }

        #endregion
    }
}
