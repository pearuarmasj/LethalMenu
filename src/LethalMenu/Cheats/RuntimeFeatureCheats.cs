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

    public class CustomFOVCheat : CheatBase
    {
        public override string Name => "Custom FOV";
        public override Hack HackType => Hack.CustomFOV;

        public override void OnUpdate()
        {
            var camera = LethalMenuMod.LocalPlayer?.gameplayCamera;
            if (camera == null) return;

            camera.fieldOfView = LethalMenuMod.LocalPlayer!.inTerminalMenu || !IsEnabled
                ? 66f
                : Settings.FOVValue;
        }
    }

    /// Disables every LocalVolumetricFog component. Re-scans each frame so new fogs (moon travel,
    /// weather) get caught; tracks what we touched so toggle-off restores them.
    public class NoFogCheat : CheatBase
    {
        public override string Name => "No Fog";
        public override Hack HackType => Hack.NoFog;

        private readonly System.Collections.Generic.HashSet<LocalVolumetricFog> _disabled = new();
        private bool _wasEnabled;

        public override void OnUpdate()
        {
            if (IsEnabled)
            {
                foreach (var fog in Object.FindObjectsOfType<LocalVolumetricFog>())
                {
                    if (fog == null || !fog.enabled) continue;
                    fog.enabled = false;
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
                if (fog != null) fog.enabled = true;
            _disabled.Clear();
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

            var camera = LethalMenuMod.LocalPlayer?.gameplayCamera;
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

    public class KillClickCheat : CheatBase
    {
        public override string Name => "Kill Click";
        public override Hack HackType => Hack.KillClick;

        public override void OnUpdate()
        {
            if (!IsEnabled || Settings.ShowMenu || LethalMenuMod.LocalPlayer == null) return;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            var camera = LethalMenuMod.LocalPlayer.gameplayCamera;
            if (camera == null) return;

            var ray = new Ray(camera.transform.position, camera.transform.forward);
            foreach (var hit in Physics.RaycastAll(ray, 100f))
            {
                var enemyCollider = hit.collider.GetComponent<EnemyAICollisionDetect>();
                if (enemyCollider?.mainScript == null) continue;

                var enemy = enemyCollider.mainScript;
                enemy.ChangeEnemyOwnerServerRpc(LethalMenuMod.LocalPlayer.actualClientId);

                if (enemy is NutcrackerEnemyAI nutcracker)
                    nutcracker.KillEnemy();
                else
                    enemy.KillEnemyServerRpc(true);

                Loader.Log($"Killed {enemy.enemyType?.enemyName ?? "enemy"}");
                break;
            }
        }
    }

    public class StunClickCheat : CheatBase
    {
        public override string Name => "Stun Click";
        public override Hack HackType => Hack.StunClick;

        public override void OnUpdate()
        {
            if (!IsEnabled || Settings.ShowMenu || LethalMenuMod.LocalPlayer == null) return;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.middleButton.wasPressedThisFrame) return;

            var camera = LethalMenuMod.LocalPlayer.gameplayCamera;
            if (camera == null) return;

            var ray = new Ray(camera.transform.position, camera.transform.forward);
            foreach (var hit in Physics.RaycastAll(ray, 100f))
            {
                var enemyCollider = hit.collider.GetComponent<EnemyAICollisionDetect>();
                if (enemyCollider?.mainScript != null)
                {
                    enemyCollider.mainScript.SetEnemyStunned(true, 5f);
                    HUDManager.Instance?.DisplayTip("Stun", $"Stunned {enemyCollider.mainScript.enemyType?.enemyName}");
                    return;
                }

                var turret = hit.collider.GetComponent<Turret>();
                if (turret != null)
                {
                    turret.GetComponent<TerminalAccessibleObject>()?.CallFunctionFromTerminal();
                    HUDManager.Instance?.DisplayTip("Stun", "Disabled turret");
                    return;
                }

                var landmine = hit.collider.GetComponent<Landmine>();
                if (landmine != null)
                {
                    landmine.GetComponent<TerminalAccessibleObject>()?.CallFunctionFromTerminal();
                    HUDManager.Instance?.DisplayTip("Stun", "Disabled landmine");
                    return;
                }
            }
        }
    }
}
