using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalMenu.Cheats
{
    /// ClearVision — locally suppress HDRP fog volumes so vision stays clear regardless of server weather.
    /// Tracks every Fog component we deactivate so toggle-off restores them. Re-scans each frame so fogs
    /// spawned mid-session (weather changes, new moon) also get suppressed.
    public class ClearVisionCheat : CheatBase
    {
        public override string Name => "Clear Vision";
        public override Hack HackType => Hack.ClearVisionMod;

        private readonly HashSet<Fog> _disabled = new();
        private bool _wasEnabled;

        public override void OnUpdate()
        {
            if (IsEnabled)
            {
                foreach (var v in Object.FindObjectsOfType<Volume>())
                {
                    if (v == null || v.profile == null) continue;
                    if (!v.profile.TryGet<Fog>(out var fog) || fog == null) continue;
                    if (!fog.active) continue;
                    fog.active = false;
                    _disabled.Add(fog);
                }
                _wasEnabled = true;
            }
            else if (_wasEnabled)
            {
                RestoreAll();
                _wasEnabled = false;
            }
        }

        public override void OnDisable() => RestoreAll();

        private void RestoreAll()
        {
            foreach (var fog in _disabled)
                if (fog != null) fog.active = true;
            _disabled.Clear();
        }
    }
}
