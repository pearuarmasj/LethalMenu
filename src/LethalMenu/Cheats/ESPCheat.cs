using System.Collections.Generic;
using UnityEngine;

namespace LethalMenu.Cheats
{
    /// ESP - draws boxes and labels around players, enemies, items and map objects.
    /// Boxes are the screen projection of the object's visible renderer bounds, so they follow the mesh
    /// in 3D (a flying Manticoil, a crouching player, a tall Forest Giant) instead of sitting at the root
    /// transform with a guessed height.
    public class ESPCheat : CheatBase
    {
        public override string Name => "ESP";
        public override Hack HackType => Hack.EnableESP;

        private const float MaxDistance = 500f;
        private const float RendererCacheSeconds = 2f;

        private GUIStyle? _labelStyle;
        private GUIStyle? _shadowStyle;
        private GUIStyle? _healthStyle;

        private readonly Dictionary<int, (Renderer[] renderers, float expires)> _rendererCache = new();
        private readonly List<Renderer> _rendererScratch = new();
        private readonly Vector3[] _corners = new Vector3[8];

        public override void OnGUI()
        {
            var camera = Util.ViewCamera.Current;
            if (camera == null) return;

            InitializeStyles();

            if (Hack.PlayerESP.IsEnabled())
            {
                foreach (var player in LethalMenuMod.Players)
                {
                    if (player == null || player == LethalMenuMod.LocalPlayer || player.isPlayerDead) continue;
                    DrawPlayerESP(camera, player);
                }
            }

            if (Hack.EnemyESP.IsEnabled())
            {
                foreach (var enemy in LethalMenuMod.Enemies)
                {
                    if (enemy == null || enemy.isEnemyDead) continue;
                    DrawESP(camera, enemy, enemy.enemyType?.enemyName ?? "Enemy", Settings.EnemyColor);
                }
            }

            if (Hack.ItemESP.IsEnabled())
            {
                foreach (var item in LethalMenuMod.Items)
                {
                    if (item == null || item.isHeld || item.isInShipRoom) continue;

                    var itemName = item.itemProperties?.itemName ?? "Item";
                    int value = item.scrapValue;
                    string label = value > 0 ? $"{itemName}\n${value}" : itemName;
                    Color color = value >= 200 ? new Color(1f, 0.5f, 0f)
                        : value >= 100 ? new Color(1f, 1f, 0f)
                        : value >= 50 ? new Color(0.4f, 1f, 0.4f)
                        : value > 0 ? Settings.ItemColor
                        : new Color(0.6f, 0.6f, 0.6f);
                    DrawESP(camera, item, label, color);
                }
            }

            if (Hack.DoorESP.IsEnabled())
            {
                foreach (var entrance in LethalMenuMod.Entrances)
                    if (entrance != null)
                        DrawESP(camera, entrance, entrance.isEntranceToBuilding ? "Entrance" : "Exit", Settings.DoorColor);
            }

            if (Hack.MineESP.IsEnabled())
            {
                foreach (var mine in LethalMenuMod.Landmines)
                    if (mine != null && !mine.hasExploded)
                        DrawESP(camera, mine, "Mine", Settings.MineColor);
            }

            if (Hack.TurretESP.IsEnabled())
            {
                foreach (var turret in LethalMenuMod.Turrets)
                {
                    if (turret == null) continue;
                    var mode = turret.turretMode switch
                    {
                        TurretMode.Detection => "Scanning",
                        TurretMode.Charging => "CHARGING",
                        TurretMode.Firing => "FIRING",
                        TurretMode.Berserk => "BERSERK",
                        _ => "Turret"
                    };
                    DrawESP(camera, turret, mode, Settings.TurretColor);
                }
            }

            if (Hack.FuseboxESP.IsEnabled())
            {
                foreach (var box in LethalMenuMod.BreakerBoxes)
                {
                    if (box == null) continue;
                    string status = box.isPowerOn ? "POWER ON" : $"FUSEBOX\n{box.leversSwitchedOff} OFF";
                    DrawESP(camera, box, status, box.isPowerOn ? new Color(0.4f, 1f, 0.4f) : Settings.FuseboxColor);
                }
            }

            if (Hack.SteamValveESP.IsEnabled())
                foreach (var v in LethalMenuMod.SteamValves) if (v != null) DrawESP(camera, v, "Steam Valve", Settings.MineColor);
            if (Hack.BigDoorESP.IsEnabled())
                foreach (var d in LethalMenuMod.BigDoors) if (d != null) DrawESP(camera, d, "Big Door", Settings.DoorColor);
            if (Hack.ShipDoorESP.IsEnabled())
                foreach (var d in LethalMenuMod.HangarShipDoors) if (d != null) DrawESP(camera, d, "Ship Door", Color.white);
            if (Hack.EnemyVentESP.IsEnabled())
                foreach (var v in LethalMenuMod.EnemyVents) if (v != null) DrawESP(camera, v, "Vent", Settings.DoorColor);
            if (Hack.ItemDropshipESP.IsEnabled())
                foreach (var d in LethalMenuMod.ItemDropships) if (d != null) DrawESP(camera, d, "Dropship", Color.cyan);
            if (Hack.CruiserESP.IsEnabled())
                foreach (var v in LethalMenuMod.Vehicles) if (v != null) DrawESP(camera, v, "Cruiser", new Color(1f, 0.78f, 0f));
            if (Hack.MoldSporeESP.IsEnabled())
                foreach (var s in LethalMenuMod.MoldSpores) if (s != null) DrawESP(camera, s.transform, "Spore", new Color(0f, 0.78f, 0f));
            if (Hack.MineshaftElevatorESP.IsEnabled())
                foreach (var e in LethalMenuMod.MineshaftElevators) if (e != null) DrawESP(camera, e, "Elevator", new Color(0.7f, 0.7f, 0.7f));
            if (Hack.SpikeRoofTrapESP.IsEnabled())
                foreach (var s in LethalMenuMod.SpikeRoofTraps) if (s != null) DrawESP(camera, s, "Spike Trap", new Color(1f, 0f, 0.25f));
        }

        private void DrawESP(Camera camera, Component target, string label, Color color)
        {
            if (!TryGetScreenBox(camera, target, out Rect box, out float distance)) return;

            DrawBox(box, color);
            DrawLabelAbove(box, $"{label}\n{distance:F0}m", color, distance);
        }

        private void DrawPlayerESP(Camera camera, GameNetcodeStuff.PlayerControllerB player)
        {
            if (!TryGetScreenBox(camera, player, out Rect box, out float distance)) return;

            DrawBox(box, Settings.PlayerColor);

            Color distColor = distance < 10f ? new Color(1f, 0.3f, 0.3f)
                : distance < 30f ? new Color(1f, 0.7f, 0.3f)
                : distance < 60f ? new Color(1f, 1f, 0.3f)
                : new Color(0.5f, 1f, 0.5f);

            float top = box.y;
            if (Hack.PlayerHealthBars.IsEnabled())
                top = DrawHealthBar(box, player.health, distance);

            _labelStyle!.fontSize = Mathf.RoundToInt(12 * FontScale(distance));
            var distContent = new GUIContent($"[{distance:F0}m]");
            var nameContent = new GUIContent(player.playerUsername ?? "Player");
            var distSize = _labelStyle.CalcSize(distContent);
            var nameSize = _labelStyle.CalcSize(nameContent);

            float centerX = box.center.x;
            var distRect = new Rect(centerX - distSize.x / 2f, top - distSize.y - 2f, distSize.x, distSize.y);
            var nameRect = new Rect(centerX - nameSize.x / 2f, distRect.y - nameSize.y, nameSize.x, nameSize.y);
            ShadowLabel(nameRect, nameContent, Settings.PlayerColor);
            ShadowLabel(distRect, distContent, distColor);
        }

        /// Projects the target's visible renderer bounds to a GUI-space rect. Falls back to a 1 m box above
        /// the root transform when the object has no visible renderers.
        private bool TryGetScreenBox(Camera camera, Component target, out Rect box, out float distance)
        {
            box = default;
            Vector3 root = target.transform.position;
            distance = Vector3.Distance(camera.transform.position, root);
            if (distance > MaxDistance) return false;

            if (!TryGetBounds(target, out Bounds bounds))
                bounds = new Bounds(root + Vector3.up * 0.5f, Vector3.one);

            var min = bounds.min;
            var max = bounds.max;
            _corners[0] = new Vector3(min.x, min.y, min.z);
            _corners[1] = new Vector3(max.x, min.y, min.z);
            _corners[2] = new Vector3(min.x, max.y, min.z);
            _corners[3] = new Vector3(max.x, max.y, min.z);
            _corners[4] = new Vector3(min.x, min.y, max.z);
            _corners[5] = new Vector3(max.x, min.y, max.z);
            _corners[6] = new Vector3(min.x, max.y, max.z);
            _corners[7] = new Vector3(max.x, max.y, max.z);

            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (var corner in _corners)
            {
                var vp = camera.WorldToViewportPoint(corner);
                // A corner behind the camera makes the projected rect meaningless; skip the object.
                if (vp.z <= 0f) return false;
                float sx = vp.x * Screen.width;
                float sy = (1f - vp.y) * Screen.height;
                xMin = Mathf.Min(xMin, sx); xMax = Mathf.Max(xMax, sx);
                yMin = Mathf.Min(yMin, sy); yMax = Mathf.Max(yMax, sy);
            }

            if (xMax < 0f || yMax < 0f || xMin > Screen.width || yMin > Screen.height) return false;

            // Keep tiny far-away objects visible.
            const float minSize = 6f;
            if (xMax - xMin < minSize) { float c = (xMin + xMax) / 2f; xMin = c - minSize / 2f; xMax = c + minSize / 2f; }
            if (yMax - yMin < minSize) { float c = (yMin + yMax) / 2f; yMin = c - minSize / 2f; yMax = c + minSize / 2f; }

            box = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return true;
        }

        private bool TryGetBounds(Component target, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var renderer in GetRenderers(target))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!any) { bounds = renderer.bounds; any = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return any;
        }

        private Renderer[] GetRenderers(Component target)
        {
            int id = target.GetInstanceID();
            if (_rendererCache.TryGetValue(id, out var cached) && Time.time < cached.expires)
                return cached.renderers;

            _rendererScratch.Clear();
            var root = target.transform;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
                if (Util.RendererFilter.IsVisual(renderer, root))
                    _rendererScratch.Add(renderer);

            var renderers = _rendererScratch.ToArray();
            _rendererCache[id] = (renderers, Time.time + RendererCacheSeconds);
            return renderers;
        }

        public override void OnDisable() => _rendererCache.Clear();

        private static float FontScale(float distance) => Mathf.Clamp(200f / Mathf.Max(distance, 1f), 0.6f, 1.4f);

        private void DrawLabelAbove(Rect box, string text, Color color, float distance)
        {
            _labelStyle!.fontSize = Mathf.RoundToInt(12 * FontScale(distance));
            var content = new GUIContent(text);
            var size = _labelStyle.CalcSize(content);
            ShadowLabel(new Rect(box.center.x - size.x / 2f, box.y - size.y - 2f, size.x, size.y), content, color);
        }

        private void ShadowLabel(Rect rect, GUIContent content, Color color)
        {
            _shadowStyle!.fontSize = _labelStyle!.fontSize;
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), content, _shadowStyle);
            _labelStyle.normal.textColor = color;
            GUI.Label(rect, content, _labelStyle);
        }

        /// Draws the health bar just above the box and returns its top, where the labels go.
        private float DrawHealthBar(Rect box, int health, float distance)
        {
            float percent = Mathf.Clamp01(health / 100f);
            _healthStyle!.fontSize = Mathf.Clamp(Mathf.RoundToInt(9 * FontScale(distance)), 8, 13);
            var text = new GUIContent($"{health}%");
            var textSize = _healthStyle.CalcSize(text);

            float width = Mathf.Max(box.width, textSize.x + 8f);
            float height = Mathf.Max(textSize.y * 0.8f, 6f);
            var bar = new Rect(box.center.x - width / 2f, box.y - height - 3f, width, height);

            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, percent);
            GUI.DrawTexture(new Rect(bar.x + 1, bar.y + 1, (bar.width - 2) * percent, bar.height - 2), Texture2D.whiteTexture);
            GUI.color = old;
            DrawBox(bar, Color.white);
            GUI.Label(new Rect(bar.center.x - textSize.x / 2f, bar.center.y - textSize.y / 2f, textSize.x, textSize.y), text, _healthStyle);
            return bar.y;
        }

        private static void DrawBox(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            const float t = 2f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - t, rect.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, t, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - t, rect.y, t, rect.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void InitializeStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _shadowStyle = new GUIStyle(_labelStyle) { normal = { textColor = Color.black } };
            _healthStyle = new GUIStyle(_labelStyle) { normal = { textColor = Color.white } };
        }
    }
}
