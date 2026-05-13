using LethalMenu.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// Phantom — detach the existing gameplay camera and free-fly it. The body freezes in place
    /// rather than being fully disabled (lc-hax PhantomMod semantics). Shift-toggle-off teleports
    /// the body to the camera position. Arrow keys cycle "look at" target.
    /// Mutex with FreeCam: each disables the other on enable.
    public class PhantomCheat : CheatBase
    {
        public override string Name => "Phantom";
        public override Hack HackType => Hack.Phantom;

        private static Transform? _originalCameraParent;
        private static KeyboardFreeFly? _freeFly;
        private static MouseInput? _mouseInput;
        private static bool _wasEnabled;
        private int _spectatorIndex;

        public override void OnUpdate()
        {
            bool now = IsEnabled;
            if (now != _wasEnabled)
            {
                _wasEnabled = now;
                if (now) Enable(); else Disable();
            }

            if (!now) return;

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.rightArrowKey.wasPressedThisFrame) LookAtPlayer(1);
            if (kb.leftArrowKey.wasPressedThisFrame) LookAtPlayer(-1);
        }

        private void Enable()
        {
            // Mutex with FreeCam.
            if (Hack.FreeCam.IsEnabled())
                Hack.FreeCam.SetEnabled(false);

            var player = LethalMenuMod.LocalPlayer;
            var cam = player?.gameplayCamera;
            if (player == null || cam == null) return;

            _originalCameraParent = cam.transform.parent;
            cam.transform.SetParent(null, worldPositionStays: true);

            _freeFly = cam.gameObject.GetComponent<KeyboardFreeFly>() ?? cam.gameObject.AddComponent<KeyboardFreeFly>();
            _freeFly.BaseSpeed = Settings.PhantomMoveSpeed;
            _freeFly.enabled = true;
            _freeFly.Resync();

            _mouseInput = cam.gameObject.GetComponent<MouseInput>() ?? cam.gameObject.AddComponent<MouseInput>();
            _mouseInput.enabled = true;

            player.isFreeCamera = true;
            if (player.playerBodyAnimator != null) player.playerBodyAnimator.enabled = false;
            if (player.thisController != null) player.thisController.enabled = false;

            HUDManager.Instance?.DisplayTip("Phantom", "WASD/space/ctrl fly. Arrows snap to players. Shift+toggle-off teleports body.");
        }

        private void Disable()
        {
            var player = LethalMenuMod.LocalPlayer;
            var cam = player?.gameplayCamera;
            if (player == null || cam == null) { Cleanup(); return; }

            bool shiftHeld = Keyboard.current?.leftShiftKey.isPressed ?? false;
            if (shiftHeld && Settings.PhantomTeleportOnExit)
            {
                player.TeleportPlayer(cam.transform.position);
            }

            if (_originalCameraParent != null)
            {
                cam.transform.SetParent(_originalCameraParent, worldPositionStays: false);
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
            }

            player.isFreeCamera = false;
            if (player.playerBodyAnimator != null) player.playerBodyAnimator.enabled = true;
            if (player.thisController != null) player.thisController.enabled = true;

            Cleanup();
        }

        private static void Cleanup()
        {
            if (_freeFly != null) { _freeFly.enabled = false; _freeFly = null; }
            if (_mouseInput != null) { _mouseInput.enabled = false; _mouseInput = null; }
            _originalCameraParent = null;
        }

        private void LookAtPlayer(int delta)
        {
            var cam = LethalMenuMod.LocalPlayer?.gameplayCamera;
            if (cam == null || StartOfRound.Instance == null) return;
            var players = StartOfRound.Instance.allPlayerScripts;
            if (players == null || players.Length == 0) return;

            _spectatorIndex = ((_spectatorIndex + delta) % players.Length + players.Length) % players.Length;
            var target = players[_spectatorIndex];
            if (target == null || !target.isPlayerControlled) return;

            cam.transform.position = target.playerEye != null ? target.playerEye.position : target.transform.position;
            _freeFly?.Resync();
        }
    }
}
