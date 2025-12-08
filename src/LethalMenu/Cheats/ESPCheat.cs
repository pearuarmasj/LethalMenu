using System;
using UnityEngine;

namespace LethalMenu.Cheats
{
    /// <summary>
    /// ESP - draws boxes and labels around players, enemies, and items.
    /// </summary>
    public class ESPCheat : CheatBase
    {
        public override string Name => "ESP";

        // GUI styles
        private GUIStyle? _labelStyle;
        private Texture2D? _boxTexture;

        public override void OnUpdate()
        {
            IsEnabled = Settings.ESP;
        }

        public override void OnGUI()
        {
            if (!IsEnabled) return;

            var camera = GetActiveCamera();
            if (camera == null) return;

            InitializeStyles();

            // Draw player ESP
            if (Settings.PlayerESP)
            {
                foreach (var player in LethalMenuMod.Players)
                {
                    if (player == null || player == LethalMenuMod.LocalPlayer) continue;
                    if (player.isPlayerDead) continue;

                    DrawPlayerESP(camera, player);
                }
            }

            // Draw enemy ESP
            if (Settings.EnemyESP)
            {
                foreach (var enemy in LethalMenuMod.Enemies)
                {
                    if (enemy == null || enemy.isEnemyDead) continue;

                    var enemyName = enemy.enemyType?.enemyName ?? "Enemy";
                    DrawESP(camera, enemy.transform.position, enemyName, Settings.EnemyColor, 2f);
                }
            }

            // Draw item ESP
            if (Settings.ItemESP)
            {
                foreach (var item in LethalMenuMod.Items)
                {
                    if (item == null || item.isHeld || item.isInShipRoom) continue;

                    var itemName = item.itemProperties?.itemName ?? "Item";
                    var scrapValue = item.scrapValue;

                    // Color-code items by value
                    Color itemColor;
                    string label;
                    if (scrapValue > 0)
                    {
                        label = $"{itemName}\n${scrapValue}";
                        // Color gradient: gray ($0-20), white ($20-50), green ($50-100), yellow ($100-200), orange ($200+)
                        if (scrapValue >= 200)
                            itemColor = new Color(1f, 0.5f, 0f); // Orange - high value
                        else if (scrapValue >= 100)
                            itemColor = new Color(1f, 1f, 0f); // Yellow - good value
                        else if (scrapValue >= 50)
                            itemColor = new Color(0.4f, 1f, 0.4f); // Green - decent value
                        else
                            itemColor = Settings.ItemColor; // Default
                    }
                    else
                    {
                        label = itemName;
                        itemColor = new Color(0.6f, 0.6f, 0.6f); // Gray for non-scrap items
                    }
                    DrawESP(camera, item.transform.position, label, itemColor, 1f);
                }
            }

            // Draw door ESP
            if (Settings.DoorESP)
            {
                foreach (var entrance in LethalMenuMod.Entrances)
                {
                    if (entrance == null) continue;

                    var label = entrance.isEntranceToBuilding ? "Entrance" : "Exit";
                    DrawESP(camera, entrance.transform.position, label, Settings.DoorColor, 2f);
                }
            }

            // Draw landmine ESP
            if (Settings.MineESP)
            {
                foreach (var mine in LethalMenuMod.Landmines)
                {
                    if (mine == null) continue;
                    if (mine.hasExploded) continue;

                    DrawESP(camera, mine.transform.position, "MINE", Settings.MineColor, 0.5f);
                }
            }

            // Draw turret ESP
            if (Settings.TurretESP)
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

            // Draw fusebox/breaker box ESP
            if (Settings.FuseboxESP)
            {
                foreach (var box in LethalMenuMod.BreakerBoxes)
                {
                    if (box == null) continue;

                    string status = box.isPowerOn ? "POWER ON" : $"FUSEBOX\n{box.leversSwitchedOff} OFF";
                    Color boxColor = box.isPowerOn ? new Color(0.4f, 1f, 0.4f) : Settings.FuseboxColor;
                    DrawESP(camera, box.transform.position, status, boxColor, 1.5f);
                }
            }
        }

        /// <summary>
        /// Get the active camera - handles spectator mode and fallback to Camera.main.
        /// </summary>
        private Camera? GetActiveCamera()
        {
            if (LethalMenuMod.LocalPlayer == null)
                return Camera.main;

            if (LethalMenuMod.LocalPlayer.isPlayerDead && LethalMenuMod.GameInstance != null)
                return LethalMenuMod.GameInstance.spectateCamera;

            return LethalMenuMod.LocalPlayer.gameplayCamera ?? Camera.main;
        }

        /// <summary>
        /// Convert world position to screen coordinates.
        /// Returns false if position is behind camera.
        /// </summary>
        private bool WorldToScreen(Camera camera, Vector3 worldPos, out Vector2 screenPos)
        {
            // Use viewport point to get normalized coordinates
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPos);

            // Check if behind camera
            if (viewportPoint.z <= 0)
            {
                screenPos = Vector2.zero;
                return false;
            }

            // Convert to screen coordinates
            screenPos = new Vector2(
                viewportPoint.x * Screen.width,
                (1f - viewportPoint.y) * Screen.height  // Flip Y axis for GUI coordinates
            );

            return true;
        }

        private void DrawESP(Camera camera, Vector3 worldPos, string label, Color color, float height)
        {
            // Convert world to screen
            if (!WorldToScreen(camera, worldPos, out Vector2 screenPos))
                return;

            // Calculate distance
            float distance = Vector3.Distance(camera.transform.position, worldPos);
            if (distance > 500f) return; // Max ESP distance

            // Draw distance label
            string fullLabel = $"{label}\n{distance:F0}m";

            // Scale based on distance
            float scale = Mathf.Clamp(200f / distance, 0.3f, 2f);
            int fontSize = Mathf.RoundToInt(12 * scale);

            // Calculate label size
            if (_labelStyle != null)
            {
                _labelStyle.fontSize = fontSize;
                var content = new GUIContent(fullLabel);
                var size = _labelStyle.CalcSize(content);

                // Draw box first (at the object's position)
                float boxWidth = 30f * scale;
                float boxHeight = height * 40f * scale;
                Rect boxRect = new Rect(
                    screenPos.x - boxWidth / 2,
                    screenPos.y - boxHeight / 2,  // Center the box on the object
                    boxWidth,
                    boxHeight
                );
                DrawBox(boxRect, color);

                // Position label ABOVE the box
                Rect labelRect = new Rect(
                    screenPos.x - size.x / 2,
                    boxRect.y - size.y - 4f,  // Above the box with small gap
                    size.x,
                    size.y
                );

                // Draw shadow/outline for visibility
                var shadowStyle = new GUIStyle(_labelStyle);
                shadowStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width, labelRect.height), fullLabel, shadowStyle);

                // Draw colored label
                _labelStyle.normal.textColor = color;
                GUI.Label(labelRect, fullLabel, _labelStyle);
            }
        }

        private void DrawPlayerESP(Camera camera, GameNetcodeStuff.PlayerControllerB player)
        {
            var worldPos = player.transform.position;
            
            // Convert world to screen
            if (!WorldToScreen(camera, worldPos, out Vector2 screenPos))
                return;

            // Calculate distance
            float distance = Vector3.Distance(camera.transform.position, worldPos);
            if (distance > 500f) return;

            string playerName = player.playerUsername ?? "Player";
            int health = player.health;
            
            // Distance color: green (far) to red (close)
            Color distColor;
            if (distance < 10f)
                distColor = new Color(1f, 0.3f, 0.3f); // Red - very close
            else if (distance < 30f)
                distColor = new Color(1f, 0.7f, 0.3f); // Orange - close
            else if (distance < 60f)
                distColor = new Color(1f, 1f, 0.3f); // Yellow - medium
            else
                distColor = new Color(0.5f, 1f, 0.5f); // Green - far

            // Scale based on distance
            float scale = Mathf.Clamp(200f / distance, 0.3f, 2f);
            int fontSize = Mathf.RoundToInt(12 * scale);

            if (_labelStyle != null)
            {
                _labelStyle.fontSize = fontSize;
                
                // Draw name and distance as separate lines with different colors
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

                // Shadow for name
                var shadowStyle = new GUIStyle(_labelStyle);
                shadowStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(baseX + 1 + (totalWidth - nameSize.x) / 2, baseY + 1, nameSize.x, nameSize.y), nameLine, shadowStyle);
                // Name label
                _labelStyle.normal.textColor = Settings.PlayerColor;
                GUI.Label(new Rect(baseX + (totalWidth - nameSize.x) / 2, baseY, nameSize.x, nameSize.y), nameLine, _labelStyle);
                
                // Shadow for distance
                GUI.Label(new Rect(baseX + 1 + (totalWidth - distSize.x) / 2, baseY + nameSize.y + 1, distSize.x, distSize.y), distLine, shadowStyle);
                // Distance label with color-coded distance
                _labelStyle.normal.textColor = distColor;
                GUI.Label(new Rect(baseX + (totalWidth - distSize.x) / 2, baseY + nameSize.y, distSize.x, distSize.y), distLine, _labelStyle);

                // Box
                float boxWidth = 30f * scale;
                float boxHeight = 80f * scale;
                Rect boxRect = new Rect(
                    screenPos.x - boxWidth / 2,
                    screenPos.y - boxHeight,
                    boxWidth,
                    boxHeight
                );
                DrawBox(boxRect, Settings.PlayerColor);

                // Health bar (if enabled)
                if (Settings.PlayerHealthBars && _boxTexture != null)
                {
                    float healthBarWidth = boxWidth + 10f;
                    float healthBarHeight = 6f * scale;
                    float healthPercent = Mathf.Clamp01(health / 100f);

                    // Position health bar above the box
                    float hbX = screenPos.x - healthBarWidth / 2;
                    float hbY = screenPos.y - boxHeight - healthBarHeight - 4f;

                    // Background (dark)
                    var oldColor = GUI.color;
                    GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                    GUI.DrawTexture(new Rect(hbX, hbY, healthBarWidth, healthBarHeight), _boxTexture);

                    // Health fill (green to red based on health)
                    Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
                    GUI.color = healthColor;
                    GUI.DrawTexture(new Rect(hbX + 1, hbY + 1, (healthBarWidth - 2) * healthPercent, healthBarHeight - 2), _boxTexture);

                    // Border
                    GUI.color = Color.white;
                    DrawBox(new Rect(hbX, hbY, healthBarWidth, healthBarHeight), Color.white);

                    GUI.color = oldColor;

                    // Health text
                    var healthStyle = new GUIStyle(_labelStyle);
                    healthStyle.fontSize = Mathf.RoundToInt(10 * scale);
                    healthStyle.normal.textColor = Color.white;
                    string healthText = $"{health}%";
                    var healthContent = new GUIContent(healthText);
                    var healthSize = healthStyle.CalcSize(healthContent);
                    GUI.Label(new Rect(hbX + healthBarWidth / 2 - healthSize.x / 2, hbY - 2, healthSize.x, healthSize.y), healthText, healthStyle);
                }
            }
        }

        private void DrawBox(Rect rect, Color color)
        {
            if (_boxTexture == null) return;

            // Save current color
            var oldColor = GUI.color;
            GUI.color = color;

            // Draw 4 sides of the box
            float thickness = 2f;

            // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _boxTexture);
            // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), _boxTexture);
            // Left
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _boxTexture);
            // Right
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), _boxTexture);

            // Restore color
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

            // Create white texture for box drawing
            _boxTexture = new Texture2D(1, 1);
            _boxTexture.SetPixel(0, 0, Color.white);
            _boxTexture.Apply();
        }
    }
}
