using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LethalMenu.Cheats
{
    /// Makes the local player's character model visible in first person. Vanilla sets
    /// thisPlayerModel.shadowCastingMode to ShadowsOnly while alive — the body still casts
    /// a shadow but the renderer is suppressed for the first-person camera, so looking
    /// down shows the shadow of legs with no actual legs.
    ///
    /// Two fixes:
    ///   1. Flip shadowCastingMode back to On so the SkinnedMeshRenderer participates in
    ///      the color pass.
    ///   2. Collapse the head bone (spine.004 in the Rigify rig) plus its end-bone to
    ///      ~0 scale so the head/helmet mesh doesn't surround the camera — the camera
    ///      sits inside the head and would otherwise see the inside of the helmet.
    ///
    /// Critically, bone scaling MUST run in LateUpdate. The Animator updates bones each
    /// frame after Update, so a scale set in Update gets overwritten before rendering.
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
