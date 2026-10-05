using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Cheats
{
    /// God mode - keeps health topped up. Damage/kill blocking lives in PlayerPatches.
    public class GodModeCheat : CheatBase
    {
        public override string Name => "God Mode";
        public override Hack HackType => Hack.GodMode;

        public override void OnUpdate()
        {
            if (!IsEnabled || LethalMenuMod.LocalPlayer == null) return;

            if (LethalMenuMod.LocalPlayer.health < 100)
                LethalMenuMod.LocalPlayer.health = 100;
            LethalMenuMod.LocalPlayer.criticallyInjured = false;
        }
    }

    public class InfiniteStaminaCheat : CheatBase
    {
        public override string Name => "Infinite Stamina";
        public override Hack HackType => Hack.InfiniteStamina;

        public override void OnUpdate()
        {
            if (IsEnabled && LethalMenuMod.LocalPlayer != null)
                LethalMenuMod.LocalPlayer.sprintMeter = 1f;
        }
    }

    /// Base for cheats that override one float field on the local player and must hand the
    /// original back on disable. The original is captured per player instance, so a lobby change
    /// (new PlayerControllerB) re-captures instead of writing a stale value onto the new player.
    public abstract class PlayerFloatOverrideCheat : CheatBase
    {
        private PlayerControllerB? _capturedFor;
        private float _original;

        protected abstract float Read(PlayerControllerB player);
        protected abstract void Write(PlayerControllerB player, float value);
        protected abstract float Override(float original);

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (!IsEnabled || player == null) return;

            if (_capturedFor != player)
            {
                _capturedFor = player;
                _original = Read(player);
            }
            Write(player, Override(_original));
        }

        public override void OnDisable()
        {
            if (_capturedFor != null && _capturedFor == LethalMenuMod.LocalPlayer)
                Write(_capturedFor, _original);
            _capturedFor = null;
        }
    }

    public class SpeedHackCheat : PlayerFloatOverrideCheat
    {
        public override string Name => "Speed Hack";
        public override Hack HackType => Hack.SpeedHack;

        protected override float Read(PlayerControllerB player) => player.movementSpeed;
        protected override void Write(PlayerControllerB player, float value) => player.movementSpeed = value;
        protected override float Override(float original) => original * Settings.SpeedMultiplier;
    }

    public class JumpHackCheat : PlayerFloatOverrideCheat
    {
        public override string Name => "Jump Hack";
        public override Hack HackType => Hack.JumpHack;

        protected override float Read(PlayerControllerB player) => player.jumpForce;
        protected override void Write(PlayerControllerB player, float value) => player.jumpForce = value;
        protected override float Override(float original) => original * Settings.JumpMultiplier;
    }

    public class FastClimbCheat : PlayerFloatOverrideCheat
    {
        public override string Name => "Fast Climb";
        public override Hack HackType => Hack.FastClimb;

        protected override float Read(PlayerControllerB player) => player.climbSpeed;
        protected override void Write(PlayerControllerB player, float value) => player.climbSpeed = value;
        protected override float Override(float original) => original * 2f;
    }

    /// No clip - pass through walls. Disables the CharacterController while enabled and flies the
    /// player with WASD/Space/Ctrl.
    public class NoClipCheat : CheatBase
    {
        public override string Name => "No Clip";
        public override Hack HackType => Hack.NoClip;

        private PlayerControllerB? _disabledOn;

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (!IsEnabled || player == null) return;

            if (player.thisController != null && player.thisController.enabled)
            {
                player.thisController.enabled = false;
                _disabledOn = player;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null || Settings.ShowMenu || player.isTypingChat) return;

            var cam = player.gameplayCamera.transform;
            var move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += cam.forward;
            if (keyboard.sKey.isPressed) move -= cam.forward;
            if (keyboard.aKey.isPressed) move -= cam.right;
            if (keyboard.dKey.isPressed) move += cam.right;
            if (keyboard.spaceKey.isPressed) move += Vector3.up;
            if (keyboard.leftCtrlKey.isPressed) move -= Vector3.up;

            player.transform.position += move.normalized * (10f * Settings.SpeedMultiplier * Time.deltaTime);
        }

        public override void OnDisable()
        {
            if (_disabledOn != null && _disabledOn == LethalMenuMod.LocalPlayer && _disabledOn.thisController != null)
                _disabledOn.thisController.enabled = true;
            _disabledOn = null;
        }
    }

    /// Night vision - overrides the local player's night-vision light while enabled; on disable
    /// restores the captured intensity/range and hands the enabled state back to the game's own rule.
    public class NightVisionCheat : CheatBase
    {
        public override string Name => "Night Vision";
        public override Hack HackType => Hack.NightVision;

        private PlayerControllerB? _capturedFor;
        private float _originalIntensity;
        private float _originalRange;

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (!IsEnabled || player?.nightVision == null) return;

            if (_capturedFor != player)
            {
                _capturedFor = player;
                _originalIntensity = player.nightVision.intensity;
                _originalRange = player.nightVision.range;
            }

            player.nightVision.enabled = true;
            player.nightVision.intensity = Settings.NightVisionIntensity;
            player.nightVision.range = Settings.NightVisionRange;
        }

        public override void OnDisable()
        {
            var player = _capturedFor;
            _capturedFor = null;
            if (player == null || player != LethalMenuMod.LocalPlayer || player.nightVision == null) return;

            player.nightVision.intensity = _originalIntensity;
            player.nightVision.range = _originalRange;
            player.SetNightVisionEnabled(isNotLocalClient: false);
        }
    }

    /// No fall damage. Implemented in PlayerPatches.PlayerHitGroundPrefix.
    public class NoFallDamageCheat : CheatBase
    {
        public override string Name => "No Fall Damage";
        public override Hack HackType => Hack.NoFallDamage;
    }

    /// Infinite battery. Implemented in ItemPatches.UpdatePostfix.
    public class InfiniteBatteryCheat : CheatBase
    {
        public override string Name => "Infinite Battery";
        public override Hack HackType => Hack.InfiniteBattery;
    }

    public class NoWeightCheat : CheatBase
    {
        public override string Name => "No Weight";
        public override Hack HackType => Hack.NoWeight;

        public override void OnUpdate()
        {
            if (IsEnabled && LethalMenuMod.LocalPlayer != null)
                LethalMenuMod.LocalPlayer.carryWeight = 1f;
        }
    }

    /// Unlimited oxygen - keeps the underwater drowning timer full and clears the drunk effect.
    public class UnlimitedOxygenCheat : CheatBase
    {
        public override string Name => "Unlimited Oxygen";
        public override Hack HackType => Hack.UnlimitedOxygen;

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (!IsEnabled || player == null || StartOfRound.Instance == null) return;

            StartOfRound.Instance.drowningTimer = 1f;
            player.drunkness = 0f;
            player.drunknessInertia = 0f;
        }
    }

    /// Owns PlayerControllerB.grabDistance and interactableObjectsMask for the four reach-style
    /// toggles so they compose instead of overwriting each other. Vanilla values are captured per
    /// player and restored when the last of them turns off.
    public class GrabReachCheat : CheatBase
    {
        public override string Name => "Grab Reach";
        public override Hack HackType => Hack.Reach;

        private const float ReachDistance = 30f;
        private const float InfiniteDistance = 10000f;

        private PlayerControllerB? _capturedFor;
        private float _originalGrabDistance;
        private int _originalMask;

        private static bool AnyEnabled =>
            Hack.Reach.IsEnabled() || Hack.InfiniteGrab.IsEnabled() ||
            Hack.LootThroughWalls.IsEnabled() || Hack.InteractThroughWalls.IsEnabled();

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (!AnyEnabled)
            {
                Restore();
                return;
            }
            if (player == null) return;

            if (_capturedFor != player)
            {
                Restore();
                _capturedFor = player;
                _originalGrabDistance = player.grabDistance;
                _originalMask = player.interactableObjectsMask;
            }

            bool throughWalls = Hack.LootThroughWalls.IsEnabled() || Hack.InteractThroughWalls.IsEnabled();
            player.grabDistance = throughWalls || Hack.InfiniteGrab.IsEnabled()
                ? InfiniteDistance
                : Hack.Reach.IsEnabled() ? ReachDistance : _originalGrabDistance;

            if (throughWalls)
            {
                int mask = 0;
                if (Hack.LootThroughWalls.IsEnabled()) mask |= LayerMask.GetMask("Props");
                if (Hack.InteractThroughWalls.IsEnabled()) mask |= LayerMask.GetMask("InteractableObject");
                player.interactableObjectsMask = mask;
            }
            else
            {
                player.interactableObjectsMask = _originalMask;
            }
        }

        public override void OnDisable() => Restore();

        private void Restore()
        {
            if (_capturedFor != null && _capturedFor == LethalMenuMod.LocalPlayer)
            {
                _capturedFor.grabDistance = _originalGrabDistance;
                _capturedFor.interactableObjectsMask = _originalMask;
            }
            _capturedFor = null;
        }
    }
}
