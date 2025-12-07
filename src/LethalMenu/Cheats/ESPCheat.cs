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

                    var label = scrapValue > 0 ? $"{itemName} (${scrapValue})" : itemName;
                    DrawESP(camera, item.transform.position, label, Settings.ItemColor, 1f);
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

                // Center the label on screen position
                Rect labelRect = new Rect(
                    screenPos.x - size.x / 2,
                    screenPos.y - size.y / 2,
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

                // Draw box outline
                float boxWidth = 30f * scale;
                float boxHeight = height * 40f * scale;
                DrawBox(new Rect(
                    screenPos.x - boxWidth / 2,
                    screenPos.y - boxHeight,
                    boxWidth,
                    boxHeight
                ), color);
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
            string fullLabel = $"{playerName}\n{distance:F0}m";

            // Scale based on distance
            float scale = Mathf.Clamp(200f / distance, 0.3f, 2f);
            int fontSize = Mathf.RoundToInt(12 * scale);

            if (_labelStyle != null)
            {
                _labelStyle.fontSize = fontSize;
                var content = new GUIContent(fullLabel);
                var size = _labelStyle.CalcSize(content);

                Rect labelRect = new Rect(
                    screenPos.x - size.x / 2,
                    screenPos.y - size.y / 2,
                    size.x,
                    size.y
                );

                // Shadow
                var shadowStyle = new GUIStyle(_labelStyle);
                shadowStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width, labelRect.height), fullLabel, shadowStyle);

                // Label
                _labelStyle.normal.textColor = Settings.PlayerColor;
                GUI.Label(labelRect, fullLabel, _labelStyle);

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
