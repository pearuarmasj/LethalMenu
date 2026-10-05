using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace LethalMenu.Cheats
{
    public class AlwaysShowClockCheat : CheatBase
    {
        public override string Name => "Always Show Clock";
        public override Hack HackType => Hack.AlwaysShowClock;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;

            var clockObj = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/Clock");
            if (clockObj != null)
                clockObj.SetActive(true);
        }
    }

    /// Overrides the gameplay camera FOV. Written in LateUpdate so it lands after
    /// PlayerControllerB.Update's own lerp towards targetFOV; while disabled (and in the terminal)
    /// it writes nothing and the game's sprint/inspect/terminal FOV logic is untouched.
    public class CustomFOVCheat : CheatBase
    {
        public override string Name => "Custom FOV";
        public override Hack HackType => Hack.CustomFOV;

        public override void OnLateUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (!IsEnabled || player?.gameplayCamera == null || player.inTerminalMenu) return;

            player.gameplayCamera.fieldOfView = Settings.FOVValue;
        }
    }

    /// Disables fog: every LocalVolumetricFog component and the Fog override of every HDRP volume profile.
    /// Re-scans once a second so new fog (moon travel, weather) gets caught; tracks what it touched so
    /// toggle-off restores it.
    public class NoFogCheat : CheatBase
    {
        public override string Name => "No Fog";
        public override Hack HackType => Hack.NoFog;

        private const float ScanInterval = 1f;
        private readonly System.Collections.Generic.HashSet<LocalVolumetricFog> _disabledVolumes = new();
        private readonly System.Collections.Generic.HashSet<Fog> _disabledOverrides = new();
        private float _nextScan;

        public override void OnUpdate()
        {
            if (!IsEnabled || Time.time < _nextScan) return;
            _nextScan = Time.time + ScanInterval;

            foreach (var fog in Object.FindObjectsOfType<LocalVolumetricFog>())
            {
                if (fog == null || !fog.enabled) continue;
                fog.enabled = false;
                _disabledVolumes.Add(fog);
            }

            foreach (var volume in Object.FindObjectsOfType<UnityEngine.Rendering.Volume>())
            {
                if (volume == null || volume.profile == null) continue;
                if (!volume.profile.TryGet<Fog>(out var fog) || fog == null || !fog.active) continue;
                fog.active = false;
                _disabledOverrides.Add(fog);
            }
        }

        public override void OnEnable() => _nextScan = 0f;

        public override void OnDisable()
        {
            foreach (var fog in _disabledVolumes)
                if (fog != null) fog.enabled = true;
            foreach (var fog in _disabledOverrides)
                if (fog != null) fog.active = true;
            _disabledVolumes.Clear();
            _disabledOverrides.Clear();
        }
    }

    public class BreadcrumbsCheat : CheatBase
    {
        public override string Name => "Breadcrumbs";
        public override Hack HackType => Hack.Breadcrumbs;

        private readonly System.Collections.Generic.List<Vector3> _breadcrumbs = new();
        private float _lastBreadcrumbTime;
        private static GUIStyle? _breadcrumbStyle;

        public override void OnUpdate()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (!IsEnabled || player == null || player.isPlayerDead)
            {
                if (_breadcrumbs.Count > 0)
                    _breadcrumbs.Clear();
                return;
            }

            if (Time.time - _lastBreadcrumbTime < Settings.BreadcrumbInterval)
                return;

            _lastBreadcrumbTime = Time.time;
            var pos = player.transform.position;
            pos.y -= 0.5f;
            _breadcrumbs.Add(pos);

            if (_breadcrumbs.Count > 1000)
                _breadcrumbs.RemoveAt(0);
        }

        public override void OnGUI()
        {
            if (!IsEnabled) return;

            if (_breadcrumbStyle == null)
            {
                _breadcrumbStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _breadcrumbStyle.normal.textColor = Color.yellow;
            }

            var camera = Util.ViewCamera.Current;
            if (camera == null) return;

            for (int i = 0; i < _breadcrumbs.Count; i++)
            {
                var viewport = camera.WorldToViewportPoint(_breadcrumbs[i]);
                if (viewport.z <= 0 || viewport.z > 100f) continue;

                float screenX = viewport.x * Screen.width;
                float screenY = (1f - viewport.y) * Screen.height;
                if (screenX < 0 || screenX > Screen.width || screenY < 0 || screenY > Screen.height)
                    continue;

                var rect = new Rect(screenX - 8, screenY - 8, 16, 16);
                GUI.color = new Color(1f, 0.9f, 0f, 0.8f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);

                GUI.color = Color.black;
                GUI.Label(new Rect(screenX - 15, screenY - 10, 30, 20), i.ToString(), _breadcrumbStyle);
            }

            GUI.color = Color.white;
        }
    }

    /// LMB kills the enemy under the crosshair (for everyone).
    public class KillClickCheat : CheatBase
    {
        public override string Name => "Kill Click";
        public override Hack HackType => Hack.KillClick;

        public override void OnUpdate()
        {
            if (!IsEnabled || Settings.ShowMenu) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            var enemy = CrosshairTarget.Enemy();
            if (enemy == null) return;

            NetworkCheats.KillEnemy(enemy);
            HUDManager.Instance?.DisplayTip("Kill", $"Killed {enemy.enemyType?.enemyName ?? "enemy"}");
        }
    }

    /// MMB stuns the enemy under the crosshair, or temporarily disables a turret/landmine.
    public class StunClickCheat : CheatBase
    {
        public override string Name => "Stun Click";
        public override Hack HackType => Hack.StunClick;

        public override void OnUpdate()
        {
            if (!IsEnabled || Settings.ShowMenu) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.middleButton.wasPressedThisFrame) return;

            NetworkCheats.StunAtCrosshair();
        }

        /// Trigger Gun uses the same button for the opposite effect.
        public override void OnEnable() => Hack.TriggerGun.SetEnabled(false);
    }

    /// Raycast helper shared by the crosshair-click cheats.
    internal static class CrosshairTarget
    {
        private const float Range = 100f;

        public static EnemyAI? Enemy()
        {
            var camera = LethalMenuMod.LocalPlayer?.gameplayCamera;
            if (camera == null) return null;

            var hits = Physics.RaycastAll(new Ray(camera.transform.position, camera.transform.forward), Range);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var detect = hit.collider.GetComponent<EnemyAICollisionDetect>();
                if (detect != null && detect.mainScript != null && !detect.mainScript.isEnemyDead)
                    return detect.mainScript;
            }
            return null;
        }
    }
}
