using System;
using System.Collections;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Troll Effects

        /// Spin all ship objects. Kind of lame but it's in the reference.
        public static void SpinShipObjects(float duration = 5f)
        {
            var shipObjects = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
            if (shipObjects == null || shipObjects.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Spin", "No ship objects found.");
                return;
            }

            foreach (var obj in shipObjects)
            {
                if (obj != null)
                {
                    LethalMenuMod.Instance?.StartCoroutine(SpinObjectCoroutine(obj, duration));
                }
            }

            HUDManager.Instance?.DisplayTip("Spin", $"Spinning {shipObjects.Length} ship objects!");
        }

        private static IEnumerator SpinObjectCoroutine(PlaceableShipObject obj, float duration)
        {
            float elapsed = 0f;
            Vector3 originalPos = obj.transform.position;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float rotation = elapsed * 810f;
                
                ShipBuildModeManager.Instance?.PlaceShipObject(
                    originalPos,
                    new Vector3(0f, rotation, 0f),
                    obj,
                    false
                );
                
                yield return null;
            }
        }

        private static System.Collections.Generic.Dictionary<ulong, Coroutine> _activeSpinCoroutines = new System.Collections.Generic.Dictionary<ulong, Coroutine>();

        /// Spin a player - camera, model, or both.
        public static void SpinPlayer(PlayerControllerB targetPlayer, float duration, bool spinCamera, bool spinModel)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Spin", "No target player.");
                return;
            }

            if (!spinCamera && !spinModel)
            {
                HUDManager.Instance?.DisplayTip("Spin", "Select camera or model to spin.");
                return;
            }

            StopSpinPlayer(targetPlayer);

            var coroutine = LethalMenuMod.Instance?.StartCoroutine(SpinPlayerCoroutine(targetPlayer, duration, spinCamera, spinModel));
            if (coroutine != null)
            {
                _activeSpinCoroutines[targetPlayer.playerClientId] = coroutine;
            }

            string mode = spinCamera && spinModel ? "camera+model" : (spinCamera ? "camera" : "model");
            HUDManager.Instance?.DisplayTip("Spin", $"Spinning {targetPlayer.playerUsername} ({mode}) for {duration}s!");
        }

        /// Stop spinning a player and reset their camera and model.
        public static void StopSpinPlayer(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null) return;

            if (_activeSpinCoroutines.TryGetValue(targetPlayer.playerClientId, out var coroutine))
            {
                if (coroutine != null && LethalMenuMod.Instance != null)
                {
                    LethalMenuMod.Instance.StopCoroutine(coroutine);
                }
                _activeSpinCoroutines.Remove(targetPlayer.playerClientId);
                
                if (targetPlayer.gameplayCamera != null)
                {
                    var cam = targetPlayer.gameplayCamera.transform;
                    cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, 0f);
                }
                
                if (targetPlayer.thisPlayerModel != null)
                {
                    targetPlayer.thisPlayerModel.transform.localRotation = Quaternion.identity;
                }
            }
        }

        /// Check if player is being spun.
        public static bool IsSpinning(PlayerControllerB player)
        {
            return player != null && _activeSpinCoroutines.ContainsKey(player.playerClientId);
        }

        private static IEnumerator SpinPlayerCoroutine(PlayerControllerB player, float duration, bool spinCamera, bool spinModel)
        {
            float elapsed = 0f;
            float spinSpeed = 720f;
            
            Transform? modelTransform = player?.thisPlayerModel?.transform;

            while (elapsed < duration && player != null && !player.isPlayerDead)
            {
                elapsed += Time.deltaTime;
                float rotation = elapsed * spinSpeed;

                if (spinCamera && player.gameplayCamera != null)
                {
                    var cam = player.gameplayCamera.transform;
                    cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, rotation % 360f);
                }

                if (spinModel && modelTransform != null)
                {
                    modelTransform.localRotation = Quaternion.Euler(
                        modelTransform.localEulerAngles.x,
                        rotation % 360f,
                        modelTransform.localEulerAngles.z
                    );
                    
                    player.UpdatePlayerRotationServerRpc((short)0, (short)(rotation % 360f));
                }

                if (spinModel && !spinCamera && player.gameplayCamera != null)
                {
                    var cam = player.gameplayCamera.transform;
                    cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, 0f);
                }

                yield return null;
            }

            if (spinModel && modelTransform != null)
            {
                modelTransform.localRotation = Quaternion.identity;
            }

            if (player?.gameplayCamera != null)
            {
                var cam = player.gameplayCamera.transform;
                cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, 0f);
            }

            if (player != null)
            {
                _activeSpinCoroutines.Remove(player.playerClientId);
            }
        }

        #endregion
    }
}
