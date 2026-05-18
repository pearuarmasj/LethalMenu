using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LethalMenu.Cheats
{
    /// Makes the local player's character model visible in first person. Vanilla sets
    /// thisPlayerModel.shadowCastingMode to ShadowsOnly while alive — the body still casts
    /// a shadow but the renderer is suppressed for the first-person camera.
    ///
    /// Three things happen while enabled:
    ///   1. shadowCastingMode flipped back to On so the SkinnedMeshRenderer renders.
    ///   2. Head bones (Rigify "spine.004" + "spine.004_end") scaled to ~0 so the
    ///      helmet/head mesh doesn't surround the camera.
    ///   3. gameplayCamera position snapped to the head bone (with the original camera-
    ///      to-head offset preserved in head-local space). Animator drives the head bone
    ///      to follow camera pitch, so as you look down the head physically tilts forward
    ///      at the neck — and the camera now translates with it instead of pivoting in
    ///      place, revealing the torso/legs underneath. Rotation is untouched so mouse
    ///      look feels identical.
    ///
    /// All bone/camera work runs in LateUpdate, after the Animator has finished updating
    /// the skeleton — otherwise the Animator overwrites our edits before the renderer
    /// reads them.
    public class VisibleBodyCheat : CheatBase
    {
        public override string Name => "Visible Body";
        public override Hack HackType => Hack.VisibleBody;

        private const string HeadBoneName = "spine.004";
        private static readonly string[] HeadBoneNames = { "spine.004", "spine.004_end" };

        private bool _wasEnabled;
        private readonly List<Transform> _headBones = new();
        private readonly List<Vector3> _cachedScales = new();
        private bool _scalesCached;

        private Transform? _headPivot; // spine.004 specifically — used for camera follow
        private Vector3? _cameraLocalOffset; // gameplayCamera position in spine.004 local space, captured once

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (IsEnabled)
            {
                if (player != null && player.thisPlayerModel != null)
                    player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                _wasEnabled = true;
            }
            else if (_wasEnabled)
            {
                Restore(player);
                _wasEnabled = false;
            }
        }

        public override void OnLateUpdate()
        {
            if (!IsEnabled) return;
            var player = LethalMenuMod.LocalPlayer;
            if (player == null || player.thisPlayerModel == null) return;

            EnsureHeadBones(player);
            CacheScalesOnce();

            if (Hack.ThirdPerson.IsEnabled())
            {
                RestoreHeadScales();
                return;
            }

            for (int i = 0; i < _headBones.Count; i++)
            {
                if (_headBones[i] != null)
                    _headBones[i].localScale = Vector3.zero;
            }

            ApplyCameraFollow(player);
        }

        private void ApplyCameraFollow(GameNetcodeStuff.PlayerControllerB player)
        {
            if (_headPivot == null || player.gameplayCamera == null) return;

            if (_cameraLocalOffset == null)
                _cameraLocalOffset = _headPivot.InverseTransformPoint(player.gameplayCamera.transform.position);

            player.gameplayCamera.transform.position = _headPivot.TransformPoint(_cameraLocalOffset.Value);
        }

        public override void OnDisable() => Restore(LethalMenuMod.LocalPlayer);

        private void EnsureHeadBones(GameNetcodeStuff.PlayerControllerB player)
        {
            if (_headBones.Count > 0) return;
            var bones = player.thisPlayerModel.bones;
            if (bones == null) return;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                foreach (var name in HeadBoneNames)
                {
                    if (bones[i].name == name)
                    {
                        _headBones.Add(bones[i]);
                        if (name == HeadBoneName) _headPivot = bones[i];
                        break;
                    }
                }
            }
        }

        private void CacheScalesOnce()
        {
            if (_scalesCached) return;
            _cachedScales.Clear();
            for (int i = 0; i < _headBones.Count; i++)
                _cachedScales.Add(_headBones[i] != null ? _headBones[i].localScale : Vector3.one);
            _scalesCached = _headBones.Count > 0;
        }

        private void RestoreHeadScales()
        {
            if (!_scalesCached) return;
            for (int i = 0; i < _headBones.Count && i < _cachedScales.Count; i++)
            {
                if (_headBones[i] != null)
                    _headBones[i].localScale = _cachedScales[i];
            }
        }

        private void Restore(GameNetcodeStuff.PlayerControllerB? player)
        {
            if (player != null && player.thisPlayerModel != null && !Hack.ThirdPerson.IsEnabled())
                player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            RestoreHeadScales();
            _headBones.Clear();
            _cachedScales.Clear();
            _scalesCached = false;
            _headPivot = null;
            _cameraLocalOffset = null;
            // No camera position restore needed — game re-parents/repositions every frame.
        }
    }
}
