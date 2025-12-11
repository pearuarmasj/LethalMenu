using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace LethalMenu.Cheats
{
    /// 
    /// Third-person camera - view your player from behind.
    /// Based on the Verity 3rdPerson mod, adapted for LethalMenu.
    /// 
    public class ThirdPersonCheat : CheatBase
    {
        public override string Name => "Third Person";

        // Camera instance
        private static Camera? _thirdPersonCamera;
        private static bool _isActive = false;
        private static bool _previousStateBeforeMenu = false;

        // Culling mask for third-person camera (excludes player arms, visor, etc.)
        private const int ThirdPersonCullingMask = 557520895;

        public override void OnUpdate()
        {
            // Don't update while in menus, terminal, or dead - patches handle those transitions
            var player = LethalMenuMod.LocalPlayer;
            if (player == null) return;
            if (player.quickMenuManager?.isMenuOpen == true) return;
            if (player.inTerminalMenu) return;
            if (player.isPlayerDead) return;

            // Check for toggle key (V by default, configurable)
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                Settings.ThirdPerson = !Settings.ThirdPerson;
                Toggle();
            }

            // Update camera position if active
            if (_isActive && _thirdPersonCamera != null && player.gameplayCamera != null)
            {
                UpdateCameraPosition();
            }
        }

        /// 
        /// Initialize the third-person camera.
        /// 
        public static void Initialize()
        {
            if (_thirdPersonCamera != null) return;

            var camObj = new GameObject("LethalMenuThirdPersonCamera");
            _thirdPersonCamera = camObj.AddComponent<Camera>();
            _thirdPersonCamera.hideFlags = HideFlags.HideAndDontSave;
            _thirdPersonCamera.fieldOfView = 66f;
            _thirdPersonCamera.nearClipPlane = 0.1f;
            _thirdPersonCamera.cullingMask = ThirdPersonCullingMask;
            _thirdPersonCamera.enabled = false;
            Object.DontDestroyOnLoad(camObj);

            Loader.Log("[ThirdPerson] Camera initialized");
        }

        /// 
        /// Toggle third-person view on/off.
        /// 
        public static void Toggle()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (player == null) return;
            if (player.isTypingChat) return;
            if (player.quickMenuManager?.isMenuOpen == true) return;
            if (player.inTerminalMenu) return;
            if (player.isPlayerDead) return;

            // Ensure camera exists
            Initialize();
            if (_thirdPersonCamera == null) return;

            _isActive = Settings.ThirdPerson;

            // Find UI elements
            var panelObj = GameObject.Find("Systems/UI/Canvas/Panel/");
            var canvas = GameObject.Find("Systems/UI/Canvas/")?.GetComponent<Canvas>();
            var visor = GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel/");

            if (_isActive)
            {
                // Enable third-person mode
                player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                
                if (panelObj != null) panelObj.SetActive(false);
                if (canvas != null)
                {
                    canvas.worldCamera = _thirdPersonCamera;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                }
                if (visor != null) visor.SetActive(false);

                // Hide first-person arms
                player.thisPlayerModelArms.enabled = false;
                
                // Disable gameplay camera, enable third-person camera
                player.gameplayCamera.enabled = false;
                _thirdPersonCamera.enabled = true;

                Loader.Log("[ThirdPerson] Enabled");
            }
            else
            {
                // Disable third-person mode
                player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                
                if (panelObj != null) panelObj.SetActive(true);
                if (canvas != null)
                {
                    var uiCamera = GameObject.Find("UICamera")?.GetComponent<Camera>();
                    if (uiCamera != null)
                    {
                        canvas.worldCamera = uiCamera;
                    }
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                
                // Restore visor based on NoVisor setting
                if (visor != null) visor.SetActive(!Settings.NoVisor);

                // Restore first-person arms (unless enemy controlling)
                if (!EnemyControlCheat.IsControlling)
                {
                    player.thisPlayerModelArms.enabled = true;
                }
                
                // Enable gameplay camera, disable third-person camera
                player.gameplayCamera.enabled = true;
                _thirdPersonCamera.enabled = false;

                Loader.Log("[ThirdPerson] Disabled");
            }
        }

        /// 
        /// Update the third-person camera position based on player camera.
        /// 
        private static void UpdateCameraPosition()
        {
            if (_thirdPersonCamera == null) return;
            
            var player = LethalMenuMod.LocalPlayer;
            if (player?.gameplayCamera == null) return;

            Camera gameplayCamera = player.gameplayCamera;
            
            // Calculate position behind and slightly to the right of player
            Vector3 backward = gameplayCamera.transform.forward * -1f;
            Vector3 right = gameplayCamera.transform.TransformDirection(Vector3.right) * 0.6f;
            Vector3 up = Vector3.up * 0.1f;
            float distance = Settings.ThirdPersonDistance;

            _thirdPersonCamera.transform.position = gameplayCamera.transform.position + 
                backward * distance + right + up;
            _thirdPersonCamera.transform.rotation = Quaternion.LookRotation(gameplayCamera.transform.forward);
        }

        /// 
        /// Get the current view state.
        /// 
        public static bool ViewState => _isActive;

        /// 
        /// Store and restore state for menu/terminal transitions.
        /// 
        public static void StoreAndDisable()
        {
            _previousStateBeforeMenu = _isActive;
            if (_isActive)
            {
                Settings.ThirdPerson = false;
                Toggle();
            }
        }

        /// 
        /// Restore previous state after menu/terminal closes.
        /// 
        public static void RestoreState()
        {
            if (_previousStateBeforeMenu)
            {
                Settings.ThirdPerson = true;
                Toggle();
            }
        }

        /// 
        /// Force disable third-person (used by patches).
        /// 
        public static void ForceDisable()
        {
            if (_isActive)
            {
                Settings.ThirdPerson = false;
                Toggle();
            }
        }

        /// 
        /// Cleanup on disable.
        /// 
        public override void OnDisable()
        {
            if (_isActive)
            {
                Settings.ThirdPerson = false;
                Toggle();
            }
        }
    }
}

