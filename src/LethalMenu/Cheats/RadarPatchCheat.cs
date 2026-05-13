using UnityEngine;

namespace LethalMenu.Cheats
{
    /// RadarPatch — extend the in-ship radar camera's render volume so it covers more of the map.
    public class RadarPatchCheat : CheatBase
    {
        public override string Name => "Radar+";
        public override Hack HackType => Hack.RadarPatch;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            var renderer = Object.FindObjectOfType<ManualCameraRenderer>();
            if (renderer == null || renderer.cam == null) return;

            renderer.cam.farClipPlane = 1000f;
            if (renderer.cam.orthographic)
                renderer.cam.orthographicSize = 50f;
        }
    }
}
