using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// <summary>
    /// God mode - prevents damage and death.
    /// Implemented via Harmony patches in GamePatches.cs.
    /// </summary>
    public class GodModeCheat : CheatBase
    {
        public override string Name => "God Mode";

        public override void OnUpdate()
        {
            IsEnabled = Settings.GodMode;

            // Additional god mode logic - heal player
            if (IsEnabled && LethalMenuMod.LocalPlayer != null)
            {
                if (LethalMenuMod.LocalPlayer.health < 100)
                {
                    LethalMenuMod.LocalPlayer.health = 100;
                }

                // Prevent critical injury state
                LethalMenuMod.LocalPlayer.criticallyInjured = false;
            }
        }
    }

    /// <summary>
    /// Infinite stamina - prevents stamina drain.
    /// </summary>
    public class InfiniteStaminaCheat : CheatBase
    {
        public override string Name => "Infinite Stamina";

        public override void OnUpdate()
        {
            IsEnabled = Settings.InfiniteStamina;

            if (IsEnabled && LethalMenuMod.LocalPlayer != null)
            {
                LethalMenuMod.LocalPlayer.sprintMeter = 1f;
            }
        }
    }

    /// <summary>
    /// Speed hack - increases movement speed.
    /// </summary>
    public class SpeedHackCheat : CheatBase
    {
        public override string Name => "Speed Hack";

        private float _originalSpeed = 4.6f;
        private bool _speedCaptured = false;

        public override void OnUpdate()
        {
            IsEnabled = Settings.SpeedHack;

            if (LethalMenuMod.LocalPlayer == null) return;

            if (!_speedCaptured)
            {
                _originalSpeed = LethalMenuMod.LocalPlayer.movementSpeed;
                _speedCaptured = true;
            }

            if (IsEnabled)
            {
                LethalMenuMod.LocalPlayer.movementSpeed = _originalSpeed * Settings.SpeedMultiplier;
            }
            else if (_speedCaptured)
            {
                LethalMenuMod.LocalPlayer.movementSpeed = _originalSpeed;
            }
        }

        public override void OnDisable()
        {
            if (LethalMenuMod.LocalPlayer != null && _speedCaptured)
            {
                LethalMenuMod.LocalPlayer.movementSpeed = _originalSpeed;
            }
        }
    }

    /// <summary>
    /// Jump hack - increases jump force.
    /// </summary>
    public class JumpHackCheat : CheatBase
    {
        public override string Name => "Jump Hack";

        private float _originalJumpForce = 13f;
        private bool _jumpForceCaptured = false;

        public override void OnUpdate()
        {
            IsEnabled = Settings.JumpHack;

            if (LethalMenuMod.LocalPlayer == null) return;

            if (!_jumpForceCaptured)
            {
                _originalJumpForce = LethalMenuMod.LocalPlayer.jumpForce;
                _jumpForceCaptured = true;
            }

            if (IsEnabled)
            {
                LethalMenuMod.LocalPlayer.jumpForce = _originalJumpForce * Settings.JumpMultiplier;
            }
            else if (_jumpForceCaptured)
            {
                LethalMenuMod.LocalPlayer.jumpForce = _originalJumpForce;
            }
        }

        public override void OnDisable()
        {
            if (LethalMenuMod.LocalPlayer != null && _jumpForceCaptured)
            {
                LethalMenuMod.LocalPlayer.jumpForce = _originalJumpForce;
            }
        }
    }

    /// <summary>
    /// No clip - pass through walls.
    /// </summary>
    public class NoClipCheat : CheatBase
    {
        public override string Name => "No Clip";

        public override void OnUpdate()
        {
            IsEnabled = Settings.NoClip;

            if (LethalMenuMod.LocalPlayer == null) return;

            var controller = LethalMenuMod.LocalPlayer.GetComponent<UnityEngine.CharacterController>();
            if (controller != null)
            {
                controller.enabled = !IsEnabled;
            }

            if (IsEnabled)
            {
                // Allow flying movement using New Input System
                var moveDirection = UnityEngine.Vector3.zero;
                var keyboard = Keyboard.current;

                if (keyboard != null)
                {
                    if (keyboard.wKey.isPressed)
                        moveDirection += LethalMenuMod.LocalPlayer.gameplayCamera.transform.forward;
                    if (keyboard.sKey.isPressed)
                        moveDirection -= LethalMenuMod.LocalPlayer.gameplayCamera.transform.forward;
                    if (keyboard.aKey.isPressed)
                        moveDirection -= LethalMenuMod.LocalPlayer.gameplayCamera.transform.right;
                    if (keyboard.dKey.isPressed)
                        moveDirection += LethalMenuMod.LocalPlayer.gameplayCamera.transform.right;
                    if (keyboard.spaceKey.isPressed)
                        moveDirection += UnityEngine.Vector3.up;
                    if (keyboard.leftCtrlKey.isPressed)
                        moveDirection -= UnityEngine.Vector3.up;
                }

                float speed = 10f * Settings.SpeedMultiplier;
                LethalMenuMod.LocalPlayer.transform.position += moveDirection.normalized * speed * UnityEngine.Time.deltaTime;
            }
        }

        public override void OnDisable()
        {
            if (LethalMenuMod.LocalPlayer == null) return;

            var controller = LethalMenuMod.LocalPlayer.GetComponent<UnityEngine.CharacterController>();
            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }

    /// <summary>
    /// Night vision - enhances visibility in dark areas.
    /// </summary>
    public class NightVisionCheat : CheatBase
    {
        public override string Name => "Night Vision";

        public override void OnUpdate()
        {
            IsEnabled = Settings.NightVision;

            if (LethalMenuMod.LocalPlayer?.nightVision == null) return;

            var nightVisionLight = LethalMenuMod.LocalPlayer.nightVision;

            if (IsEnabled)
            {
                nightVisionLight.enabled = true;
                nightVisionLight.intensity = Settings.NightVisionIntensity;
                nightVisionLight.range = Settings.NightVisionRange;
            }
            else
            {
                nightVisionLight.enabled = false;
            }
        }

        public override void OnDisable()
        {
            if (LethalMenuMod.LocalPlayer?.nightVision != null)
            {
                LethalMenuMod.LocalPlayer.nightVision.enabled = false;
            }
        }
    }

    /// <summary>
    /// No fall damage.
    /// Implemented via Harmony patches in GamePatches.cs.
    /// </summary>
    public class NoFallDamageCheat : CheatBase
    {
        public override string Name => "No Fall Damage";

        public override void OnUpdate()
        {
            IsEnabled = Settings.NoFallDamage;
        }
    }

    /// <summary>
    /// Infinite battery for held items.
    /// Implemented via Harmony patches in GamePatches.cs.
    /// </summary>
    public class InfiniteBatteryCheat : CheatBase
    {
        public override string Name => "Infinite Battery";

        public override void OnUpdate()
        {
            IsEnabled = Settings.InfiniteBattery;
        }
    }

    /// <summary>
    /// No weight - removes carry weight penalty.
    /// </summary>
    public class NoWeightCheat : CheatBase
    {
        public override string Name => "No Weight";

        public override void OnUpdate()
        {
            IsEnabled = Settings.NoWeight;

            if (IsEnabled && LethalMenuMod.LocalPlayer != null)
            {
                // 1.0 = baseline (no weight penalty)
                LethalMenuMod.LocalPlayer.carryWeight = 1f;
            }
        }
    }
}
