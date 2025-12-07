using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// <summary>
    /// FreeCam - detach camera and fly around freely.
    /// Uses New Input System.
    /// </summary>
    public class FreeCamCheat : CheatBase
    {
        public override string Name => "FreeCam";

        private Camera? _freeCam;
        private AudioListener? _audioListener;
        private Light? _light;
        private Vector3 _position;
        private Vector2 _rotation;
        private bool _wasEnabled;
        private Camera? _originalCamera;

        public override void OnUpdate()
        {
            bool shouldEnable = Settings.FreeCam;

            // Handle toggle on/off
            if (shouldEnable && !_wasEnabled)
            {
                EnableFreeCam();
            }
            else if (!shouldEnable && _wasEnabled)
            {
                DisableFreeCam();
            }

            _wasEnabled = shouldEnable;

            if (!shouldEnable || _freeCam == null) return;

            // Handle movement
            HandleMovement();
            HandleRotation();
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
            _freeCam.fieldOfView = Settings.FOV ? Settings.FOVValue : _originalCamera.fieldOfView;
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
            _light.intensity = Settings.NightVision ? Settings.NightVisionIntensity : 0f;
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

            Loader.Log("FreeCam enabled");
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
                _light.intensity = Settings.NightVision ? Settings.NightVisionIntensity * 0.001f : 0f;
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

        public void ForceDisable()
        {
            Settings.FreeCam = false;
            DisableFreeCam();
            _wasEnabled = false;
        }
    }
}
