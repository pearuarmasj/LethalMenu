using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalMenu.Cheats
{
    /// ClearVision — locally suppress HDRP fog volumes so vision stays clear regardless of server weather.
    /// Tracks every Fog component we deactivate so toggle-off restores them. Re-scans once a second so fogs
    /// spawned mid-session (weather changes, new moon) also get suppressed.
    public class ClearVisionCheat : CheatBase
    {
        public override string Name => "Clear Vision";
        public override Hack HackType => Hack.ClearVisionMod;

        private const float ScanInterval = 1f;
        private readonly HashSet<Fog> _disabled = new();
        private float _nextScan;

        public override void OnUpdate()
        {
            if (!IsEnabled || Time.time < _nextScan) return;
            _nextScan = Time.time + ScanInterval;

            foreach (var v in Object.FindObjectsOfType<Volume>())
            {
                if (v == null || v.profile == null) continue;
                if (!v.profile.TryGet<Fog>(out var fog) || fog == null) continue;
                if (!fog.active) continue;
                fog.active = false;
                _disabled.Add(fog);
            }
        }

        public override void OnEnable() => _nextScan = 0f;

        public override void OnDisable() => RestoreAll();

        private void RestoreAll()
        {
            foreach (var fog in _disabled)
                if (fog != null) fog.active = true;
            _disabled.Clear();
        }
    }
}
