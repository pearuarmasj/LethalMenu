using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace LethalMenu.Cheats
{
    /// 
    /// Third-person camera - view your player from behind.
    /// Based on the Verity 3rdPerson mod, adapted for LethalMenu.
    /// 
    /// The view is shown while the toggle is on and the local player is alive and not in the terminal
    /// or pause menu; those states are polled every frame, so the toggle survives them and the view
    /// comes back on its own afterwards. V flips the toggle.
    public class ThirdPersonCheat : CheatBase
    {
        public override string Name => "Third Person";
        public override Hack HackType => Hack.ThirdPerson;

        private static Camera? _thirdPersonCamera;
        private static bool _isActive;

        // Culling mask for third-person camera (excludes player arms, visor, etc.)
        private const int ThirdPersonCullingMask = 557520895;

        public static bool ViewState => _isActive;

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.wasPressedThisFrame && player != null &&
                !Settings.ShowMenu && !player.isTypingChat && !player.inTerminalMenu &&
                !(player.quickMenuManager != null && player.quickMenuManager.isMenuOpen))
            {
                Hack.ThirdPerson.Toggle();
            }

            bool shouldShow = IsEnabled && player != null && !player.isPlayerDead && !player.inTerminalMenu &&
                              !(player.quickMenuManager != null && player.quickMenuManager.isMenuOpen);
            if (shouldShow != _isActive)
                SetView(shouldShow);

            if (_isActive)
                UpdateCameraPosition();
        }

        public override void OnDisable()
        {
            if (_isActive) SetView(false);
        }

        private static void EnsureCamera()
        {
            if (_thirdPersonCamera != null) return;

            var camObj = new GameObject("LethalMenuThirdPersonCamera");
            _thirdPersonCamera = camObj.AddComponent<Camera>();
            _thirdPersonCamera.hideFlags = HideFlags.HideAndDontSave;
            _thirdPersonCamera.nearClipPlane = 0.1f;
            _thirdPersonCamera.cullingMask = ThirdPersonCullingMask;
            _thirdPersonCamera.enabled = false;
            Object.DontDestroyOnLoad(camObj);
        }

        private static void SetView(bool active)
        {
            var player = LethalMenuMod.LocalPlayer;
            _isActive = active && player != null;
            if (player == null) return;

            EnsureCamera();
            var panelObj = GameObject.Find("Systems/UI/Canvas/Panel/");
            var canvas = GameObject.Find("Systems/UI/Canvas/")?.GetComponent<Canvas>();
            var visor = GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel/");

            if (_isActive)
            {
                player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                if (panelObj != null) panelObj.SetActive(false);
                if (canvas != null)
                {
                    canvas.worldCamera = _thirdPersonCamera;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                }
                if (visor != null) visor.SetActive(false);

                player.thisPlayerModelArms.enabled = false;
                player.gameplayCamera.enabled = false;
                _thirdPersonCamera!.enabled = true;
            }
            else
            {
                if (!Hack.VisibleBody.IsEnabled())
                    player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                if (panelObj != null) panelObj.SetActive(true);
                if (canvas != null)
                {
                    var uiCamera = GameObject.Find("UICamera")?.GetComponent<Camera>();
                    if (uiCamera != null) canvas.worldCamera = uiCamera;
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                if (visor != null) visor.SetActive(!Hack.NoVisor.IsEnabled());

                if (!EnemyControlCheat.IsControlling)
                    player.thisPlayerModelArms.enabled = true;
                player.gameplayCamera.enabled = true;
                _thirdPersonCamera!.enabled = false;
            }
        }

        /// Behind and slightly right of the gameplay camera, matching its FOV.
        private static void UpdateCameraPosition()
        {
            var gameplayCamera = LethalMenuMod.LocalPlayer?.gameplayCamera;
            if (_thirdPersonCamera == null || gameplayCamera == null) return;

            var tf = gameplayCamera.transform;
            _thirdPersonCamera.fieldOfView = gameplayCamera.fieldOfView;
            _thirdPersonCamera.transform.position =
                tf.position - tf.forward * Settings.ThirdPersonDistance + tf.right * 0.6f + Vector3.up * 0.1f;
            _thirdPersonCamera.transform.rotation = Quaternion.LookRotation(tf.forward);
        }

        /// The active third-person camera, or null when the first-person view is showing.
        public static Camera? ActiveCamera => _isActive ? _thirdPersonCamera : null;
    }
}
