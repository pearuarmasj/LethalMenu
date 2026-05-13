using System;
using UnityEngine;

namespace LethalMenu.Cheats
{
    ///
    /// ESP - draws boxes and labels around players, enemies, and items.
    ///
    public class ESPCheat : CheatBase
    {
        public override string Name => "ESP";
        public override Hack HackType => Hack.EnableESP;

        private GUIStyle? _labelStyle;
        private Texture2D? _boxTexture;

        public override void OnGUI()
        {
            if (!IsEnabled) return;

            var camera = GetActiveCamera();
            if (camera == null) return;

            InitializeStyles();

            if (Hack.PlayerESP.IsEnabled())
            {
                foreach (var player in LethalMenuMod.Players)
                {
                    if (player == null || player == LethalMenuMod.LocalPlayer) continue;
                    if (player.isPlayerDead) continue;

                    DrawPlayerESP(camera, player);
                }
            }

            if (Hack.EnemyESP.IsEnabled())
            {
                foreach (var enemy in LethalMenuMod.Enemies)
                {
                    if (enemy == null || enemy.isEnemyDead) continue;

                    var enemyName = enemy.enemyType?.enemyName ?? "Enemy";
                    DrawESP(camera, enemy.transform.position, enemyName, Settings.EnemyColor, 2f);
                }
            }

            if (Hack.ItemESP.IsEnabled())
            {
                foreach (var item in LethalMenuMod.Items)
                {
                    if (item == null || item.isHeld || item.isInShipRoom) continue;

                    var itemName = item.itemProperties?.itemName ?? "Item";
                    var scrapValue = item.scrapValue;

                    Color itemColor;
                    string label;
                    if (scrapValue > 0)
                    {
                        label = $"{itemName}\n${scrapValue}";
                        if (scrapValue >= 200)
                            itemColor = new Color(1f, 0.5f, 0f);
                        else if (scrapValue >= 100)
                            itemColor = new Color(1f, 1f, 0f);
                        else if (scrapValue >= 50)
                            itemColor = new Color(0.4f, 1f, 0.4f);
                        else
                            itemColor = Settings.ItemColor;
                    }
                    else
                    {
                        label = itemName;
                        itemColor = new Color(0.6f, 0.6f, 0.6f);
                    }
                    DrawESP(camera, item.transform.position, label, itemColor, 1f);
                }
            }

            if (Hack.DoorESP.IsEnabled())
            {
                foreach (var entrance in LethalMenuMod.Entrances)
                {
                    if (entrance == null) continue;

                    var label = entrance.isEntranceToBuilding ? "Entrance" : "Exit";
                    DrawESP(camera, entrance.transform.position, label, Settings.DoorColor, 2f);
                }
            }

            if (Hack.MineESP.IsEnabled())
            {
                foreach (var mine in LethalMenuMod.Landmines)
                {
                    if (mine == null) continue;
                    if (mine.hasExploded) continue;

                    DrawESP(camera, mine.transform.position, "MINE", Settings.MineColor, 0.5f);
                }
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

                    DrawESP(camera, turret.transform.position, mode, Settings.TurretColor, 1.5f);
                }
            }

            if (Hack.FuseboxESP.IsEnabled())
            {
                foreach (var box in LethalMenuMod.BreakerBoxes)
                {
                    if (box == null) continue;

                    string status = box.isPowerOn ? "POWER ON" : $"FUSEBOX\n{box.leversSwitchedOff} OFF";
                    Color boxColor = box.isPowerOn ? new Color(0.4f, 1f, 0.4f) : Settings.FuseboxColor;
                    DrawESP(camera, box.transform.position, status, boxColor, 1.5f);
                }
            }

            if (Hack.SteamValveESP.IsEnabled())
            {
                foreach (var v in LethalMenuMod.SteamValves)
                {
                    if (v == null) continue;
                    DrawESP(camera, v.transform.position, "Steam Valve", Settings.MineColor, 1f);
                }
            }

            if (Hack.BigDoorESP.IsEnabled())
            {
                foreach (var d in LethalMenuMod.BigDoors)
                {
                    if (d == null) continue;
                    DrawESP(camera, d.transform.position, "Big Door", Settings.DoorColor, 2f);
                }
            }

            if (Hack.ShipDoorESP.IsEnabled())
            {
                foreach (var d in LethalMenuMod.HangarShipDoors)
                {
                    if (d == null) continue;
                    DrawESP(camera, d.transform.position, "Ship Door", Color.white, 2f);
                }
            }

            if (Hack.EnemyVentESP.IsEnabled())
            {
                foreach (var v in LethalMenuMod.EnemyVents)
                {
                    if (v == null) continue;
                    DrawESP(camera, v.transform.position, "Vent", Settings.DoorColor, 1f);
                }
            }

            if (Hack.ItemDropshipESP.IsEnabled())
            {
                foreach (var d in LethalMenuMod.ItemDropships)
                {
                    if (d == null) continue;
                    DrawESP(camera, d.transform.position, "Dropship", Color.cyan, 2f);
                }
            }

            if (Hack.CruiserESP.IsEnabled())
            {
                foreach (var v in LethalMenuMod.Vehicles)
                {
                    if (v == null) continue;
                    DrawESP(camera, v.transform.position, "Cruiser", new Color(1f, 0.78f, 0f), 1.5f);
                }
            }

            if (Hack.MoldSporeESP.IsEnabled())
            {
                foreach (var s in LethalMenuMod.MoldSpores)
                {
                    if (s == null) continue;
                    DrawESP(camera, s.transform.position, "Spore", new Color(0f, 0.78f, 0f), 0.5f);
                }
            }

            if (Hack.MineshaftElevatorESP.IsEnabled())
            {
                foreach (var e in LethalMenuMod.MineshaftElevators)
                {
                    if (e == null) continue;
                    DrawESP(camera, e.transform.position, "Elevator", new Color(0.7f, 0.7f, 0.7f), 2f);
                }
            }

            if (Hack.SpikeRoofTrapESP.IsEnabled())
            {
                foreach (var s in LethalMenuMod.SpikeRoofTraps)
                {
                    if (s == null) continue;
                    DrawESP(camera, s.transform.position, "Spike Trap", new Color(1f, 0f, 0.25f), 1f);
                }
            }
        }

        private Camera? GetActiveCamera()
        {
            if (LethalMenuMod.LocalPlayer == null)
                return Camera.main;

            if (LethalMenuMod.LocalPlayer.isPlayerDead && LethalMenuMod.GameInstance != null)
                return LethalMenuMod.GameInstance.spectateCamera;

            return LethalMenuMod.LocalPlayer.gameplayCamera ?? Camera.main;
        }

        private bool WorldToScreen(Camera camera, Vector3 worldPos, out Vector2 screenPos)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPos);

            if (viewportPoint.z <= 0)
            {
                screenPos = Vector2.zero;
                return false;
            }

            screenPos = new Vector2(
                viewportPoint.x * Screen.width,
                (1f - viewportPoint.y) * Screen.height
            );

            return true;
        }

        private void DrawESP(Camera camera, Vector3 worldPos, string label, Color color, float height)
        {
            if (!WorldToScreen(camera, worldPos, out Vector2 screenPos))
                return;

            float distance = Vector3.Distance(camera.transform.position, worldPos);
            if (distance > 500f) return;

            string fullLabel = $"{label}\n{distance:F0}m";

            float scale = Mathf.Clamp(200f / distance, 0.3f, 2f);
            int fontSize = Mathf.RoundToInt(12 * scale);

            if (_labelStyle != null)
            {
                _labelStyle.fontSize = fontSize;
                var content = new GUIContent(fullLabel);
                var size = _labelStyle.CalcSize(content);

                float boxWidth = 30f * scale;
                float boxHeight = height * 40f * scale;
                Rect boxRect = new Rect(
                    screenPos.x - boxWidth / 2,
                    screenPos.y - boxHeight / 2,
                    boxWidth,
                    boxHeight
                );
                DrawBox(boxRect, color);

                Rect labelRect = new Rect(
                    screenPos.x - size.x / 2,
                    boxRect.y - size.y - 4f,
                    size.x,
                    size.y
                );

                var shadowStyle = new GUIStyle(_labelStyle);
                shadowStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width, labelRect.height), fullLabel, shadowStyle);

                _labelStyle.normal.textColor = color;
                GUI.Label(labelRect, fullLabel, _labelStyle);
            }
        }

        private void DrawPlayerESP(Camera camera, GameNetcodeStuff.PlayerControllerB player)
        {
            var worldPos = player.transform.position;

            if (!WorldToScreen(camera, worldPos, out Vector2 screenPos))
                return;

            float distance = Vector3.Distance(camera.transform.position, worldPos);
            if (distance > 500f) return;

            string playerName = player.playerUsername ?? "Player";
            int health = player.health;

            Color distColor;
            if (distance < 10f)
                distColor = new Color(1f, 0.3f, 0.3f);
            else if (distance < 30f)
                distColor = new Color(1f, 0.7f, 0.3f);
            else if (distance < 60f)
                distColor = new Color(1f, 1f, 0.3f);
            else
                distColor = new Color(0.5f, 1f, 0.5f);

            float scale = Mathf.Clamp(200f / distance, 0.3f, 2f);
            int fontSize = Mathf.RoundToInt(12 * scale);

            if (_labelStyle != null)
            {
                _labelStyle.fontSize = fontSize;

                string nameLine = playerName;
                string distLine = $"[{distance:F0}m]";

                var nameContent = new GUIContent(nameLine);
                var distContent = new GUIContent(distLine);
                var nameSize = _labelStyle.CalcSize(nameContent);
                var distSize = _labelStyle.CalcSize(distContent);

                float totalWidth = Mathf.Max(nameSize.x, distSize.x);
                float totalHeight = nameSize.y + distSize.y;

                float baseX = screenPos.x - totalWidth / 2;
                float baseY = screenPos.y - totalHeight / 2;

                var shadowStyle = new GUIStyle(_labelStyle);
                shadowStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(baseX + 1 + (totalWidth - nameSize.x) / 2, baseY + 1, nameSize.x, nameSize.y), nameLine, shadowStyle);
                _labelStyle.normal.textColor = Settings.PlayerColor;
                GUI.Label(new Rect(baseX + (totalWidth - nameSize.x) / 2, baseY, nameSize.x, nameSize.y), nameLine, _labelStyle);

                GUI.Label(new Rect(baseX + 1 + (totalWidth - distSize.x) / 2, baseY + nameSize.y + 1, distSize.x, distSize.y), distLine, shadowStyle);
                _labelStyle.normal.textColor = distColor;
                GUI.Label(new Rect(baseX + (totalWidth - distSize.x) / 2, baseY + nameSize.y, distSize.x, distSize.y), distLine, _labelStyle);

                float boxWidth = 30f * scale;
                float boxHeight = 80f * scale;
                Rect boxRect = new Rect(
                    screenPos.x - boxWidth / 2,
                    screenPos.y - boxHeight,
                    boxWidth,
                    boxHeight
                );
                DrawBox(boxRect, Settings.PlayerColor);

                if (Hack.PlayerHealthBars.IsEnabled() && _boxTexture != null)
                {
                    var healthStyle = new GUIStyle(_labelStyle)
                    {
                        fontSize = Mathf.Clamp(Mathf.RoundToInt(9 * scale), 8, 13),
                        normal = { textColor = Color.white }
                    };

                    string healthText = $"{health}%";
                    var healthContent = new GUIContent(healthText);
                    var healthSize = healthStyle.CalcSize(healthContent);

                    float baseBarWidth = boxWidth;
                    float healthBarWidth = Mathf.Max(baseBarWidth, healthSize.x + 8f);
                    float healthBarHeight = Mathf.Clamp(6f * scale, 6f, 12f);
                    float healthPercent = Mathf.Clamp01(health / 100f);

                    float hbX = screenPos.x - healthBarWidth / 2f;
                    float hbY = screenPos.y - boxHeight - healthBarHeight - 4f;

                    var oldColor = GUI.color;

                    GUI.color = new Color(0f, 0f, 0f, 0.7f);
                    GUI.DrawTexture(new Rect(hbX, hbY, healthBarWidth, healthBarHeight), _boxTexture);

                    Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
                    GUI.color = healthColor;
                    GUI.DrawTexture(new Rect(hbX + 1, hbY + 1, (healthBarWidth - 2) * healthPercent, healthBarHeight - 2), _boxTexture);

                    GUI.color = Color.white;
                    DrawBox(new Rect(hbX, hbY, healthBarWidth, healthBarHeight), Color.white);

                    float textX = hbX + (healthBarWidth - healthSize.x) / 2f;
                    float textY = hbY + (healthBarHeight - healthSize.y) / 2f - 0.5f;
                    GUI.Label(new Rect(textX, textY, healthSize.x, healthSize.y), healthText, healthStyle);

                    GUI.color = oldColor;
                }
            }
        }

        private void DrawBox(Rect rect, Color color)
        {
            if (_boxTexture == null) return;

            var oldColor = GUI.color;
            GUI.color = color;

            float thickness = 2f;

            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _boxTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), _boxTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _boxTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), _boxTexture);

            GUI.color = oldColor;
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

            _boxTexture = new Texture2D(1, 1);
            _boxTexture.SetPixel(0, 0, Color.white);
            _boxTexture.Apply();
        }
    }
}
