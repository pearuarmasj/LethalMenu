using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalMenu.Cheats
{
    /// ClearVision — locally suppress HDRP fog volumes so vision stays clear regardless of server weather.
    public class ClearVisionCheat : CheatBase
    {
        public override string Name => "Clear Vision";
        public override Hack HackType => Hack.ClearVisionMod;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            foreach (var v in Object.FindObjectsOfType<Volume>())
            {
                if (v == null || v.profile == null) continue;
                if (v.profile.TryGet<Fog>(out var fog)) fog.active = false;
            }
        }
    }
}
