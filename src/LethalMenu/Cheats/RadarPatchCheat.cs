using UnityEngine;

namespace LethalMenu.Cheats
{
    /// RadarPatch — extend the ship monitor's radar camera so it covers more of the map. Captures the
    /// camera's original clip/size and restores them on disable.
    public class RadarPatchCheat : CheatBase
    {
        public override string Name => "Radar+";
        public override Hack HackType => Hack.RadarPatch;

        private Camera? _captured;
        private float _originalFarClip;
        private float _originalOrthoSize;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            var cam = StartOfRound.Instance?.mapScreen?.cam;
            if (cam == null) return;

            if (_captured != cam)
            {
                _captured = cam;
                _originalFarClip = cam.farClipPlane;
                _originalOrthoSize = cam.orthographicSize;
            }

            cam.farClipPlane = 1000f;
            if (cam.orthographic)
                cam.orthographicSize = 50f;
        }

        public override void OnDisable()
        {
            if (_captured != null)
            {
                _captured.farClipPlane = _originalFarClip;
                _captured.orthographicSize = _originalOrthoSize;
            }
            _captured = null;
        }
    }
}
