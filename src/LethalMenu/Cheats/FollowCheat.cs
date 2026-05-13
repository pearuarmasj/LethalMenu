using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    /// FollowMod — auto-walk behind a target player at a delay.
    /// Replays target's position + rotation + animation state ~1s late with small rotation deviation.
    public class FollowCheat : CheatBase
    {
        public override string Name => "Follow Player";
        public override Hack HackType => Hack.FollowPlayer;

        public static PlayerControllerB? TargetPlayer { get; set; }

        private struct CopiedState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public int[] AnimationLayerHashes;
            public float AnimationSpeed;
        }

        private readonly Queue<CopiedState> _states = new Queue<CopiedState>();
        private Quaternion _deviationRotation = Quaternion.identity;
        private float _deviationTimer;
        private float _animationBroadcastTimer;
        private float _instantTeleTimer;
        private bool _wasEnabled;

        public override void OnUpdate()
        {
            if (!IsEnabled)
            {
                if (_wasEnabled) { _states.Clear(); _deviationRotation = Quaternion.identity; _wasEnabled = false; }
                return;
            }
            _wasEnabled = true;

            var local = LethalMenuMod.LocalPlayer;
            var target = TargetPlayer;
            if (local == null || target == null) return;

            if (local.isPlayerDead || target.isPlayerDead || !target.isPlayerControlled)
            {
                TargetPlayer = null;
                Hack.FollowPlayer.SetEnabled(false);
                HUDManager.Instance?.DisplayTip("Follow", "Target unavailable.");
                return;
            }

            local.fallValue = 0f;
            _instantTeleTimer -= Time.deltaTime;

            if (target.isClimbingLadder)
            {
                _instantTeleTimer = Settings.FollowDelaySeconds;
                _states.Clear();
            }

            if (_instantTeleTimer > 0f)
            {
                local.transform.position = target.thisPlayerBody.position;
                return;
            }

            _deviationTimer -= Time.deltaTime;
            _animationBroadcastTimer -= Time.deltaTime;

            var anim = target.playerBodyAnimator;
            int layerCount = anim != null ? anim.layerCount : 0;
            int[] hashes = new int[layerCount];
            if (anim != null)
            {
                for (int i = 0; i < layerCount; i++)
                    hashes[i] = anim.GetCurrentAnimatorStateInfo(i).fullPathHash;
            }

            _states.Enqueue(new CopiedState
            {
                Position = target.thisPlayerBody.position,
                Rotation = target.thisPlayerBody.rotation,
                AnimationLayerHashes = hashes,
                AnimationSpeed = target.playerBodyAnimator != null
                    ? target.playerBodyAnimator.GetFloat("animationSpeed")
                    : 1f
            });

            float requiredFrames = Settings.FollowDelaySeconds / Mathf.Max(Time.deltaTime, 0.0001f);
            if (_states.Count <= requiredFrames) return;

            var state = _states.Dequeue();

            Quaternion previousRotation = local.transform.rotation;
            local.transform.rotation = state.Rotation * _deviationRotation;
            local.UpdatePlayerRotationServerRpc((short)local.cameraUp, (short)local.thisPlayerBody.eulerAngles.y);

            if (_deviationTimer < 0f)
            {
                _deviationRotation = Quaternion.Euler(0f, Random.Range(-360f, 360f), 0f);
                _deviationTimer = Random.Range(0.1f, 2.0f);
            }
            local.transform.rotation = previousRotation;

            if (_animationBroadcastTimer < 0f && local.playerBodyAnimator != null)
            {
                for (int i = 0; i < state.AnimationLayerHashes.Length; i++)
                    local.UpdatePlayerAnimationServerRpc(state.AnimationLayerHashes[i], state.AnimationSpeed);
                _animationBroadcastTimer = 0.14f;
            }

            if (Vector3.Distance(target.thisPlayerBody.position, state.Position) >= Settings.FollowMaxDistance)
            {
                local.transform.position = state.Position;
            }
        }
    }
}
