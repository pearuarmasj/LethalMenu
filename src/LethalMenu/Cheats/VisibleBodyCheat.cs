using UnityEngine.Rendering;

namespace LethalMenu.Cheats
{
    /// Makes the local player's character model visible in first person. Vanilla sets
    /// thisPlayerModel.shadowCastingMode to ShadowsOnly while alive — the body still casts
    /// a shadow but the renderer is suppressed for the first-person camera, so looking
    /// down shows the shadow of legs with no actual legs. This cheat flips it back to
    /// ShadowCastingMode.On so the body renders normally.
    ///
    /// Cooperates with ThirdPersonCheat: ThirdPerson also forces ShadowCastingMode.On on
    /// enable, then back to ShadowsOnly on disable. If both are active and ThirdPerson is
    /// toggled off, this cheat's per-frame OnUpdate flips the mode back to On the next
    /// frame, so the body stays visible.
    public class VisibleBodyCheat : CheatBase
    {
        public override string Name => "Visible Body";
        public override Hack HackType => Hack.VisibleBody;

        private bool _wasEnabled;

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;

            if (IsEnabled)
            {
                if (player != null && player.thisPlayerModel != null)
                {
                    player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                }
                _wasEnabled = true;
            }
            else if (_wasEnabled)
            {
                // Don't fight ThirdPerson — it owns shadowCastingMode while active.
                if (player != null && player.thisPlayerModel != null && !Hack.ThirdPerson.IsEnabled())
                {
                    player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                _wasEnabled = false;
            }
        }

        public override void OnDisable()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (player != null && player.thisPlayerModel != null && !Hack.ThirdPerson.IsEnabled())
            {
                player.thisPlayerModel.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            _wasEnabled = false;
        }
    }
}
