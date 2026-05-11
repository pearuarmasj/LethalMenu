using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    ///
    /// God mode - prevents damage and death.
    /// Implemented via Harmony patches in GamePatches.cs.
    ///
    public class GodModeCheat : CheatBase
    {
        public override string Name => "God Mode";
        public override Hack HackType => Hack.GodMode;

        public override void OnUpdate()
        {
            if (IsEnabled && LethalMenuMod.LocalPlayer != null)
            {
                if (LethalMenuMod.LocalPlayer.health < 100)
                {
                    LethalMenuMod.LocalPlayer.health = 100;
                }

                LethalMenuMod.LocalPlayer.criticallyInjured = false;
            }
        }
    }

    ///
    /// Demi-God mode - allows damage but auto-heals when health drops.
    /// Unlike God Mode, this doesn't block damage - you can still die from instant kills.
    /// Uses negative damage RPC exploit to heal via network.
    /// Can be applied to ANY player, not just local player.
    ///
    public class DemiGodCheat : CheatBase
    {
        public override string Name => "Demi-God Mode";
        public override Hack HackType => Hack.DemiGod;

        private float _lastHealTime = 0f;
        private const float HealCooldown = 0.5f;
        private bool _restoredFromConfig = false;

        public override void OnUpdate()
        {
            if (!_restoredFromConfig && Hack.DemiGod.IsEnabled() && LethalMenuMod.LocalPlayer != null)
            {
                Settings.DemiGodPlayers.Add(LethalMenuMod.LocalPlayer.playerClientId);
                _restoredFromConfig = true;
            }

            if (Settings.DemiGodPlayers.Count == 0) return;

            float currentTime = UnityEngine.Time.time;
            if (currentTime - _lastHealTime < HealCooldown) return;

            foreach (var player in LethalMenuMod.Players)
            {
                if (player == null || player.isPlayerDead) continue;
                if (!Settings.IsDemiGod(player)) continue;

                if (player.health < 100)
                {
                    _lastHealTime = currentTime;
                    HealPlayerViaNetwork(player);
                }

                if (player.criticallyInjured && player.health > 10)
                {
                    player.criticallyInjured = false;
                }
            }
        }

        private void HealPlayerViaNetwork(GameNetcodeStuff.PlayerControllerB player)
        {
            if (player == null || player.isPlayerDead) return;

            int healthNeeded = 100 - player.health;
            if (healthNeeded <= 0) return;

            player.DamagePlayerFromOtherClientServerRpc(
                -healthNeeded,
                UnityEngine.Vector3.zero,
                (int)player.playerClientId
            );
        }
    }

    ///
    /// Infinite stamina - prevents stamina drain.
    ///
    public class InfiniteStaminaCheat : CheatBase
    {
        public override string Name => "Infinite Stamina";
        public override Hack HackType => Hack.InfiniteStamina;

        public override void OnUpdate()
        {
            if (IsEnabled && LethalMenuMod.LocalPlayer != null)
            {
                LethalMenuMod.LocalPlayer.sprintMeter = 1f;
            }
        }
    }

    ///
    /// Speed hack - increases movement speed.
    ///
    public class SpeedHackCheat : CheatBase
    {
        public override string Name => "Speed Hack";
        public override Hack HackType => Hack.SpeedHack;

        private float _originalSpeed = 4.6f;
        private bool _speedCaptured = false;

        public override void OnUpdate()
        {
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

    ///
    /// Jump hack - increases jump force.
    ///
    public class JumpHackCheat : CheatBase
    {
        public override string Name => "Jump Hack";
        public override Hack HackType => Hack.JumpHack;

        private float _originalJumpForce = 13f;
        private bool _jumpForceCaptured = false;

        public override void OnUpdate()
        {
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

    ///
    /// No clip - pass through walls.
    ///
    public class NoClipCheat : CheatBase
    {
        public override string Name => "No Clip";
        public override Hack HackType => Hack.NoClip;

        public override void OnUpdate()
        {
            if (LethalMenuMod.LocalPlayer == null) return;

            var controller = LethalMenuMod.LocalPlayer.GetComponent<UnityEngine.CharacterController>();
            if (controller != null)
            {
                controller.enabled = !IsEnabled;
            }

            if (IsEnabled)
            {
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

    ///
    /// Night vision - enhances visibility in dark areas.
    ///
    public class NightVisionCheat : CheatBase
    {
        public override string Name => "Night Vision";
        public override Hack HackType => Hack.NightVision;

        public override void OnUpdate()
        {
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

    ///
    /// No fall damage.
    /// Implemented via Harmony patches in GamePatches.cs.
    ///
    public class NoFallDamageCheat : CheatBase
    {
        public override string Name => "No Fall Damage";
        public override Hack HackType => Hack.NoFallDamage;
    }

    ///
    /// Infinite battery for held items.
    /// Implemented via Harmony patches in GamePatches.cs.
    ///
    public class InfiniteBatteryCheat : CheatBase
    {
        public override string Name => "Infinite Battery";
        public override Hack HackType => Hack.InfiniteBattery;
    }

    ///
    /// No weight - removes carry weight penalty.
    ///
    public class NoWeightCheat : CheatBase
    {
        public override string Name => "No Weight";
        public override Hack HackType => Hack.NoWeight;

        public override void OnUpdate()
        {
            if (IsEnabled && LethalMenuMod.LocalPlayer != null)
            {
                LethalMenuMod.LocalPlayer.carryWeight = 1f;
            }
        }
    }
}
