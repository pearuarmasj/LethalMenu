using GameNetcodeStuff;
using LethalMenu.Components;
using LethalMenu.Util;
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

        private PlayerControllerB? _player;
        private Transform? _originalCameraParent;
        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;
        private KeyboardFreeFly? _freeFly;
        private MouseInput? _mouseInput;
        private int _spectatorIndex;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;

            // Enabled before a local player existed (e.g. restored from config): start once one does.
            if (_player == null) Detach();
            if (_player == null) return;

            var kb = Keyboard.current;
            if (kb == null || Settings.ShowMenu) return;
            if (kb.rightArrowKey.wasPressedThisFrame) LookAtPlayer(1);
            if (kb.leftArrowKey.wasPressedThisFrame) LookAtPlayer(-1);
        }

        public override void OnEnable()
        {
            if (Hack.FreeCam.IsEnabled())
                Hack.FreeCam.SetEnabled(false);
            Detach();
        }

        public override void OnDisable() => Reattach();

        private void Detach()
        {
            var player = LethalMenuMod.LocalPlayer;
            var cam = player?.gameplayCamera;
            if (player == null || cam == null) return;

            _player = player;
            _originalCameraParent = cam.transform.parent;
            _originalLocalPosition = cam.transform.localPosition;
            _originalLocalRotation = cam.transform.localRotation;
            cam.transform.SetParent(null, worldPositionStays: true);

            _freeFly = cam.gameObject.GetComponent<KeyboardFreeFly>() ?? cam.gameObject.AddComponent<KeyboardFreeFly>();
            _freeFly.BaseSpeed = Settings.PhantomMoveSpeed;
            _freeFly.enabled = true;
            _freeFly.Resync();

            _mouseInput = cam.gameObject.GetComponent<MouseInput>() ?? cam.gameObject.AddComponent<MouseInput>();
            _mouseInput.SyncFrom(cam.transform);
            _mouseInput.enabled = true;

            player.isFreeCamera = true;
            if (player.playerBodyAnimator != null) player.playerBodyAnimator.enabled = false;
            if (player.thisController != null) player.thisController.enabled = false;
            GameInput.SetBlocked(this, true);

            HUDManager.Instance?.DisplayTip("Phantom", "WASD/space/ctrl fly. Arrows snap to players. Shift+toggle-off teleports body.");
        }

        private void Reattach()
        {
            var player = _player;
            _player = null;

            if (_freeFly != null) { _freeFly.enabled = false; _freeFly = null; }
            if (_mouseInput != null) { _mouseInput.enabled = false; _mouseInput = null; }
            GameInput.SetBlocked(this, false);

            if (player == null || player != LethalMenuMod.LocalPlayer || player.gameplayCamera == null) return;
            var cam = player.gameplayCamera;

            bool shiftHeld = Keyboard.current?.leftShiftKey.isPressed ?? false;
            if (shiftHeld && Settings.PhantomTeleportOnExit)
                player.TeleportPlayer(cam.transform.position);

            if (_originalCameraParent != null)
            {
                cam.transform.SetParent(_originalCameraParent, worldPositionStays: false);
                cam.transform.localPosition = _originalLocalPosition;
                cam.transform.localRotation = _originalLocalRotation;
            }
            _originalCameraParent = null;

            player.isFreeCamera = false;
            if (player.playerBodyAnimator != null) player.playerBodyAnimator.enabled = true;
            if (player.thisController != null) player.thisController.enabled = true;
        }

        private void LookAtPlayer(int delta)
        {
            var cam = _player?.gameplayCamera;
            var players = StartOfRound.Instance?.allPlayerScripts;
            if (cam == null || players == null || players.Length == 0) return;

            for (int step = 0; step < players.Length; step++)
            {
                _spectatorIndex = ((_spectatorIndex + delta) % players.Length + players.Length) % players.Length;
                var target = players[_spectatorIndex];
                if (target == null || !target.isPlayerControlled) continue;

                cam.transform.position = target.playerEye != null ? target.playerEye.position : target.transform.position;
                _freeFly?.Resync();
                return;
            }
        }
    }
}
