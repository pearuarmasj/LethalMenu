using UnityEngine;
using UnityEngine.Rendering;

namespace LethalMenu.Cheats
{
    /// Makes the local player's character model visible in first person. Vanilla sets
    /// thisPlayerModel.shadowCastingMode to ShadowsOnly while alive — the body still casts
    /// a shadow but the renderer is suppressed for the first-person camera, so looking
    /// down shows the shadow of legs with no actual legs.
    ///
    /// Two fixes needed:
    ///   1. Flip shadowCastingMode back to On so the SkinnedMeshRenderer participates in
    ///      the color pass.
    ///   2. Collapse playerGlobalHead.localScale to ~0 so the head/helmet mesh doesn't
    ///      surround the camera (which sits at the head position and would otherwise see
    ///      the inside of the helmet).
    ///
    /// Cooperates with ThirdPersonCheat: that cheat also forces ShadowCastingMode.On on
    /// enable then back to ShadowsOnly on disable. If both are active and ThirdPerson is
    /// toggled off, this cheat's per-frame OnUpdate flips the mode back to On the next
    /// frame. We don't shrink the head while ThirdPerson is active — full body is fine
    /// from behind.
    public class VisibleBodyCheat : CheatBase
    {
        public override string Name => "Visible Body";
        public override Hack HackType => Hack.VisibleBody;

        private bool _wasEnabled;
        private Vector3? _cachedHeadScale;

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;

            if (IsEnabled)
            {
                if (player != null && player.thisPlayerModel != null)
                {
                    player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;

                    if (!Hack.ThirdPerson.IsEnabled() && player.playerGlobalHead != null)
                    {
                        if (_cachedHeadScale == null)
                            _cachedHeadScale = player.playerGlobalHead.localScale;
                        player.playerGlobalHead.localScale = Vector3.zero;
                    }
                    else if (_cachedHeadScale != null && player.playerGlobalHead != null)
                    {
                        // ThirdPerson is active — restore head so the body looks normal from behind.
                        player.playerGlobalHead.localScale = _cachedHeadScale.Value;
                        _cachedHeadScale = null;
                    }
                }
                _wasEnabled = true;
            }
            else if (_wasEnabled)
            {
                Restore(player);
                _wasEnabled = false;
            }
        }

        public override void OnDisable() => Restore(LethalMenuMod.LocalPlayer);

        private void Restore(GameNetcodeStuff.PlayerControllerB? player)
        {
            if (player == null) return;

            if (player.thisPlayerModel != null && !Hack.ThirdPerson.IsEnabled())
                player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            if (_cachedHeadScale != null && player.playerGlobalHead != null)
            {
                player.playerGlobalHead.localScale = _cachedHeadScale.Value;
                _cachedHeadScale = null;
            }
        }
    }
}
