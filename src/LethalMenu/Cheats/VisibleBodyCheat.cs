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
    ///   3. gameplayCamera is anchored to the neck bone (spine.003) — the eye sits at a
    ///      fixed offset in the *camera's* local frame, so pitch rotates the eye around
    ///      the neck pivot the way real human anatomy does. Looking down arcs the eye
    ///      down-and-forward over the torso; looking up arcs it back-and-up.
    ///
    /// All bone/camera work runs in LateUpdate, after the Animator has finished updating
    /// the skeleton — otherwise the Animator overwrites our edits before the renderer
    /// reads them.
    public class VisibleBodyCheat : CheatBase
    {
        public override string Name => "Visible Body";
        public override Hack HackType => Hack.VisibleBody;

        private const string NeckBoneName = "spine.003";
        private static readonly string[] HeadBoneNames = { "spine.004", "spine.004_end" };

        // Eye position relative to the neck bone, expressed in the camera's local frame
        // when looking forward. Forward = how far in front of the neck pivot the eye sits;
        // Up = how far above. These approximate human anatomy in metres.
        private const float EyeForward = 0.05f;
        private const float EyeUp = 0.10f;

        private bool _wasEnabled;
        private readonly List<Transform> _headBones = new();
        private readonly List<Vector3> _cachedScales = new();
        private bool _scalesCached;
        private Transform? _neckBone;

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

            EnsureBones(player);
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

            if (Hack.FreeCam.IsEnabled() || Hack.SpectatePlayer.IsEnabled()) return;
            if (_neckBone == null || player.gameplayCamera == null) return;

            var camTf = player.gameplayCamera.transform;
            camTf.position = _neckBone.position + camTf.up * EyeUp + camTf.forward * EyeForward;
        }

        public override void OnDisable() => Restore(LethalMenuMod.LocalPlayer);

        private void EnsureBones(GameNetcodeStuff.PlayerControllerB player)
        {
            if (_headBones.Count > 0 && _neckBone != null) return;
            var bones = player.thisPlayerModel.bones;
            if (bones == null) return;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                if (_neckBone == null && bones[i].name == NeckBoneName)
                    _neckBone = bones[i];
                foreach (var name in HeadBoneNames)
                {
                    if (bones[i].name == name)
                    {
                        if (!_headBones.Contains(bones[i])) _headBones.Add(bones[i]);
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
            _neckBone = null;
        }
    }
}
