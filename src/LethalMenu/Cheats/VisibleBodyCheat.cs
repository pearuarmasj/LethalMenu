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
    ///   3. gameplayCamera pushed forward along its look direction by
    ///      Settings.VisibleBodyCameraOffset metres. Vanilla rotates the camera in place
    ///      at the neck pivot, so looking down doesn't reveal the torso — the eye stays
    ///      at the neck. Shifting forward along look direction makes pitch arc the eye
    ///      down through the torso plane, which is the natural human geometry.
    ///
    /// All bone/camera work runs in LateUpdate, after the Animator has finished updating
    /// the skeleton — otherwise the Animator overwrites our edits before the renderer
    /// reads them.
    public class VisibleBodyCheat : CheatBase
    {
        public override string Name => "Visible Body";
        public override Hack HackType => Hack.VisibleBody;

        private static readonly string[] HeadBoneNames = { "spine.004", "spine.004_end" };

        private bool _wasEnabled;
        private readonly List<Transform> _headBones = new();
        private readonly List<Vector3> _cachedScales = new();
        private bool _scalesCached;

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

            if (Hack.FreeCam.IsEnabled() || Hack.SpectatePlayer.IsEnabled()) return;
            if (player.gameplayCamera == null) return;

            // Push the camera forward along its look direction so pitching down arcs the
            // eye down through the torso plane instead of pivoting in place at the neck.
            float forward = Settings.VisibleBodyCameraOffset;
            if (forward != 0f)
                player.gameplayCamera.transform.position += player.gameplayCamera.transform.forward * forward;
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
        }
    }
}
