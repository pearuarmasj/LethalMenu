using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// FreeCam - detach camera and fly around freely.
    /// Uses New Input System.
    /// 
    /// Enhanced Phantom Mode:
    /// - Arrow Left/Right: Snap camera to other players
    /// - Shift + Disable: Teleport player to camera position
    public class FreeCamCheat : CheatBase
    {
        public override string Name => "FreeCam";
        public override Hack HackType => Hack.FreeCam;

        private Camera? _freeCam;
        private AudioListener? _audioListener;
        private Light? _light;
        private Vector3 _position;
        private Vector2 _rotation;
        private bool _wasEnabled;
        private Camera? _originalCamera;
        
        // Phantom mode - player cycling
        private int _targetPlayerIndex = -1;
        private static FreeCamCheat? _instance;

        public FreeCamCheat()
        {
            _instance = this;
        }

        public override void OnUpdate()
        {
            bool shouldEnable = IsEnabled;

            // Handle toggle on/off
            if (shouldEnable && !_wasEnabled)
            {
                EnableFreeCam();
            }
            else if (!shouldEnable && _wasEnabled)
            {
                // Check if shift is held - teleport player to camera
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.leftShiftKey.isPressed && _freeCam != null)
                {
                    TeleportPlayerToCamera();
                }
                
                DisableFreeCam();
            }

            _wasEnabled = shouldEnable;

            if (!shouldEnable || _freeCam == null) return;

            // Handle movement
            HandleMovement();
            HandleRotation();
            HandlePlayerSnap();
        }

        private void EnableFreeCam()
        {
            if (LethalMenuMod.LocalPlayer == null) return;

            // Get original camera
            _originalCamera = LethalMenuMod.LocalPlayer.gameplayCamera;
            if (_originalCamera == null) return;

            // Store initial position from player camera
            _position = _originalCamera.transform.position;
            _rotation = new Vector2(
                _originalCamera.transform.eulerAngles.x,
                _originalCamera.transform.eulerAngles.y
            );

            // Create freecam
            var camObj = new GameObject("LethalMenuFreeCam");
            _freeCam = camObj.AddComponent<Camera>();
            
            // Copy settings but NOT the target texture
            _freeCam.cullingMask = _originalCamera.cullingMask;
            _freeCam.clearFlags = CameraClearFlags.SolidColor;
            _freeCam.backgroundColor = Color.black;
            _freeCam.depth = _originalCamera.depth + 1; // Render on top
            _freeCam.transform.position = _position;
            _freeCam.transform.rotation = _originalCamera.transform.rotation;
            _freeCam.nearClipPlane = 0.01f;
            _freeCam.farClipPlane = 1000f;
            _freeCam.fieldOfView = Hack.CustomFOV.IsEnabled() ? Settings.FOVValue : _originalCamera.fieldOfView;
            // Render directly to screen, not to a render texture
            _freeCam.targetTexture = null;

            // Add audio listener
            _audioListener = camObj.AddComponent<AudioListener>();

            // Add light for night vision in freecam
            var lightObj = new GameObject("FreeCamLight");
            lightObj.transform.SetParent(camObj.transform);
            lightObj.transform.localPosition = Vector3.zero;
            _light = lightObj.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = Settings.NightVisionRange;
            _light.intensity = Hack.NightVision.IsEnabled() ? Settings.NightVisionIntensity : 0f;
            _light.color = Color.white;

            // Disable original camera
            _originalCamera.enabled = false;

            // Disable player controls
            LethalMenuMod.LocalPlayer.enabled = false;
            LethalMenuMod.LocalPlayer.isFreeCamera = true;

            // Disable original audio listener
            if (LethalMenuMod.LocalPlayer.activeAudioListener != null)
            {
                LethalMenuMod.LocalPlayer.activeAudioListener.enabled = false;
            }

            // Reset target player
            _targetPlayerIndex = -1;

            Loader.Log("FreeCam enabled");
            HUDManager.Instance?.DisplayTip("FreeCam", "Arrow keys: snap to players\nShift+Disable: teleport to camera");
        }

        private void DisableFreeCam()
        {
            if (_freeCam != null)
            {
                Object.Destroy(_freeCam.gameObject);
                _freeCam = null;
            }

            _audioListener = null;
            _light = null;

            // Re-enable original camera and player
            if (_originalCamera != null)
            {
                _originalCamera.enabled = true;
            }

            if (LethalMenuMod.LocalPlayer != null)
            {
                LethalMenuMod.LocalPlayer.enabled = true;
                LethalMenuMod.LocalPlayer.isFreeCamera = false;

                if (LethalMenuMod.LocalPlayer.activeAudioListener != null)
                {
                    LethalMenuMod.LocalPlayer.activeAudioListener.enabled = true;
                }
            }

            Loader.Log("FreeCam disabled");
        }

        private void HandleMovement()
        {
            if (_freeCam == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float speed = Settings.FreeCamSpeed * Time.deltaTime;
            Vector3 move = Vector3.zero;

            // Forward/back (WASD)
            if (keyboard.wKey.isPressed)
                move += _freeCam.transform.forward;
            if (keyboard.sKey.isPressed)
                move -= _freeCam.transform.forward;

            // Left/right
            if (keyboard.aKey.isPressed)
                move -= _freeCam.transform.right;
            if (keyboard.dKey.isPressed)
                move += _freeCam.transform.right;

            // Up/down
            if (keyboard.spaceKey.isPressed)
                move += Vector3.up;
            if (keyboard.leftCtrlKey.isPressed)
                move -= Vector3.up;

            // Speed boost
            if (keyboard.leftShiftKey.isPressed)
                speed *= 3f;

            _position += move.normalized * speed;
            _freeCam.transform.position = _position;

            // Update light if night vision is toggled
            if (_light != null)
            {
                _light.intensity = Hack.NightVision.IsEnabled() ? Settings.NightVisionIntensity * 0.001f : 0f;
            }
        }

        private void HandleRotation()
        {
            if (_freeCam == null) return;

            // Only rotate when menu is closed
            if (Settings.ShowMenu) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            // Get mouse delta using New Input System
            Vector2 mouseDelta = mouse.delta.ReadValue();
            float mouseX = mouseDelta.x * 0.1f;
            float mouseY = mouseDelta.y * 0.1f;

            _rotation.y += mouseX;
            _rotation.x -= mouseY;
            _rotation.x = Mathf.Clamp(_rotation.x, -90f, 90f);

            _freeCam.transform.rotation = Quaternion.Euler(_rotation.x, _rotation.y, 0f);
        }

        /// Handle arrow keys to snap camera to other players.
        private void HandlePlayerSnap()
        {
            if (_freeCam == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var players = StartOfRound.Instance?.allPlayerScripts;
            if (players == null || players.Length == 0) return;

            // Count valid players (alive and controlled)
            int validCount = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && (players[i].isPlayerControlled || players[i].isPlayerDead))
                    validCount++;
            }
            if (validCount == 0) return;

            bool snapNext = keyboard.rightArrowKey.wasPressedThisFrame;
            bool snapPrev = keyboard.leftArrowKey.wasPressedThisFrame;
            bool snapToLocal = keyboard.downArrowKey.wasPressedThisFrame;

            if (snapToLocal)
            {
                // Snap back to local player
                if (LethalMenuMod.LocalPlayer != null)
                {
                    SnapToPlayer(LethalMenuMod.LocalPlayer);
                    _targetPlayerIndex = -1;
                    HUDManager.Instance?.DisplayTip("FreeCam", "Snapped to: You");
                }
                return;
            }

            if (!snapNext && !snapPrev) return;

            // Find next/prev valid player
            int startIndex = _targetPlayerIndex < 0 ? 0 : _targetPlayerIndex;
            int direction = snapNext ? 1 : -1;
            
            for (int i = 1; i <= players.Length; i++)
            {
                int checkIndex = (startIndex + i * direction + players.Length) % players.Length;
                var player = players[checkIndex];
                
                if (player != null && (player.isPlayerControlled || player.isPlayerDead))
                {
                    _targetPlayerIndex = checkIndex;
                    SnapToPlayer(player);
                    
                    string playerName = player.playerUsername ?? $"Player {checkIndex}";
                    if (player == LethalMenuMod.LocalPlayer)
                        playerName += " (You)";
                    if (player.isPlayerDead)
                        playerName += " [DEAD]";
                    
                    HUDManager.Instance?.DisplayTip("FreeCam", $"Snapped to: {playerName}");
                    break;
                }
            }
        }

        /// Snap camera position to a player's head.
        private void SnapToPlayer(GameNetcodeStuff.PlayerControllerB player)
        {
            if (_freeCam == null || player == null) return;

            // Position camera at player's head
            Vector3 targetPos;
            if (player.isPlayerDead && player.deadBody != null)
            {
                targetPos = player.deadBody.transform.position + Vector3.up * 1f;
            }
            else if (player.gameplayCamera != null)
            {
                targetPos = player.gameplayCamera.transform.position;
            }
            else
            {
                targetPos = player.transform.position + Vector3.up * 1.6f;
            }

            _position = targetPos;
            _freeCam.transform.position = _position;

            // Match player's look direction
            if (player.gameplayCamera != null && !player.isPlayerDead)
            {
                _rotation = new Vector2(
                    player.gameplayCamera.transform.eulerAngles.x,
                    player.gameplayCamera.transform.eulerAngles.y
                );
                _freeCam.transform.rotation = Quaternion.Euler(_rotation.x, _rotation.y, 0f);
            }
        }

        /// Teleport the local player to the camera's current position.
        /// Called when shift is held while disabling freecam.
        private void TeleportPlayerToCamera()
        {
            if (_freeCam == null || LethalMenuMod.LocalPlayer == null) return;

            Vector3 teleportPos = _freeCam.transform.position;
            LethalMenuMod.LocalPlayer.TeleportPlayer(teleportPos);
            
            Debug.Log($"[FreeCam] Teleported player to {teleportPos}");
            HUDManager.Instance?.DisplayTip("FreeCam", "Teleported to camera position!");
        }

        /// Static method to teleport player to camera (can be called from UI).
        public static void TeleportToCameraPosition()
        {
            _instance?.TeleportPlayerToCamera();
        }

        /// Get current camera position (for UI display or other uses).
        public static Vector3? GetCameraPosition()
        {
            return _instance?._freeCam?.transform.position;
        }

        public void ForceDisable()
        {
            Hack.FreeCam.SetEnabled(false);
            DisableFreeCam();
            _wasEnabled = false;
        }
    }
}
