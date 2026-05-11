using System;
using System.Linq;
using UnityEngine;

namespace LethalMenu.Cheats
{
    /// /// Info Display HUD - shows game information in the corner of the screen.
    /// Displays: ship value, quota progress, deadline, credits, enemy count, body count.
    public class InfoDisplayCheat : CheatBase
    {
        public override string Name => "Info Display";
        public override Hack HackType => Hack.InfoDisplay;

        // GUI styling
        private static GUIStyle? _infoStyle;
        private static GUIStyle? _headerStyle;
        private static GUIStyle? _valueStyle;
        private static Texture2D? _backgroundTexture;

        public override void OnUpdate() { }

        public override void OnGUI()
        {
            if (!IsEnabled) return;
            if (LethalMenuMod.LocalPlayer == null) return;
            if (StartOfRound.Instance == null) return;

            InitializeStyles();
            DrawInfoPanel();
        }

        private static void InitializeStyles()
        {
            if (_infoStyle != null) return;

            // Create background texture
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
            _backgroundTexture.Apply();

            _infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 2, 2)
            };
            _infoStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _headerStyle = new GUIStyle(_infoStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _headerStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);

            _valueStyle = new GUIStyle(_infoStyle)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            _valueStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);
        }

        private void DrawInfoPanel()
        {
            // Position in top-right corner
            float panelWidth = 200f;
            float panelHeight = CalculatePanelHeight();
            float padding = 10f;
            float x = Screen.width - panelWidth - padding;
            float y = padding;

            // Draw background
            GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), _backgroundTexture);

            // Draw border
            GUI.color = new Color(0.4f, 0.8f, 1f, 0.5f);
            GUI.Box(new Rect(x, y, panelWidth, panelHeight), "");
            GUI.color = Color.white;

            float currentY = y + 5f;
            float labelWidth = 110f;
            float valueWidth = 75f;

            // Header
            GUI.Label(new Rect(x, currentY, panelWidth, 20), "GAME INFO", _headerStyle);
            currentY += 22f;

            // Draw separator line
            DrawLine(x + 10, currentY, panelWidth - 20);
            currentY += 5f;

            // Credits
            if (Hack.InfoDisplayCredits.IsEnabled())
            {
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Credits:", $"${GetCredits()}");
                currentY += 18f;
            }

            // Quota progress
            if (Hack.InfoDisplayQuota.IsEnabled())
            {
                int shipValue = GetShipValue();
                int quota = GetQuota();
                string quotaColor = shipValue >= quota ? "<color=#00FF00>" : "<color=#FFFF00>";
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Ship Value:", $"${shipValue}");
                currentY += 18f;
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Quota:", $"${quota}");
                currentY += 18f;
            }

            // Deadline
            if (Hack.InfoDisplayDeadline.IsEnabled())
            {
                int deadline = GetDeadline();
                float buyingRate = GetBuyingRate();
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Deadline:", $"{deadline} days");
                currentY += 18f;
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Sell Rate:", $"{buyingRate:F0}%");
                currentY += 18f;
            }

            // Enemy count
            if (Hack.InfoDisplayEnemies.IsEnabled())
            {
                int enemies = GetEnemyCount();
                _valueStyle!.normal.textColor = enemies > 0 ? new Color(1f, 0.4f, 0.4f) : new Color(0.2f, 1f, 0.4f);
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Enemies:", enemies.ToString());
                _valueStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);
                currentY += 18f;
            }

            // Body count (dead players)
            if (Hack.InfoDisplayBodies.IsEnabled())
            {
                int bodies = GetBodyCount();
                _valueStyle!.normal.textColor = bodies > 0 ? new Color(1f, 0.4f, 0.4f) : new Color(0.2f, 1f, 0.4f);
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Dead Players:", bodies.ToString());
                _valueStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);
                currentY += 18f;
            }

            // Scrap on map (not in ship)
            if (Hack.InfoDisplayMapLoot.IsEnabled())
            {
                int mapCount = GetMapLootCount();
                int mapValue = GetMapLootValue();
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Map Loot:", $"{mapCount} (${mapValue})");
                currentY += 18f;
            }

            // Ship loot count
            if (Hack.InfoDisplayShipLoot.IsEnabled())
            {
                int shipCount = GetShipLootCount();
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Ship Items:", shipCount.ToString());
                currentY += 18f;
            }

            // Current moon
            if (Hack.InfoDisplayMoon.IsEnabled())
            {
                string moon = GetCurrentMoon();
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Moon:", moon);
                currentY += 18f;
            }

            // Time
            if (Hack.InfoDisplayTime.IsEnabled())
            {
                string time = GetCurrentTime();
                DrawInfoRow(x, currentY, labelWidth, valueWidth, "Time:", time);
            }
        }

        private void DrawInfoRow(float x, float y, float labelWidth, float valueWidth, string label, string value)
        {
            GUI.Label(new Rect(x + 10, y, labelWidth, 18), label, _infoStyle);
            GUI.Label(new Rect(x + labelWidth, y, valueWidth, 18), value, _valueStyle);
        }

        private static void DrawLine(float x, float y, float width)
        {
            GUI.color = new Color(0.4f, 0.8f, 1f, 0.3f);
            GUI.DrawTexture(new Rect(x, y, width, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private float CalculatePanelHeight()
        {
            float height = 35f; // Header + separator
            
            if (Hack.InfoDisplayCredits.IsEnabled()) height += 18f;
            if (Hack.InfoDisplayQuota.IsEnabled()) height += 36f; // Two lines
            if (Hack.InfoDisplayDeadline.IsEnabled()) height += 36f; // Two lines
            if (Hack.InfoDisplayEnemies.IsEnabled()) height += 18f;
            if (Hack.InfoDisplayBodies.IsEnabled()) height += 18f;
            if (Hack.InfoDisplayMapLoot.IsEnabled()) height += 18f;
            if (Hack.InfoDisplayShipLoot.IsEnabled()) height += 18f;
            if (Hack.InfoDisplayMoon.IsEnabled()) height += 18f;
            if (Hack.InfoDisplayTime.IsEnabled()) height += 18f;

            return height + 10f; // Padding
        }

        #region Data Getters

        private int GetCredits()
        {
            var terminal = LethalMenuMod.GameTerminal;
            return terminal != null ? terminal.groupCredits : 0;
        }

        private int GetQuota()
        {
            return TimeOfDay.Instance?.profitQuota ?? 0;
        }

        private int GetDeadline()
        {
            return TimeOfDay.Instance?.daysUntilDeadline ?? 0;
        }

        private float GetBuyingRate()
        {
            var startOfRound = StartOfRound.Instance;
            return startOfRound != null ? (float)Math.Round(startOfRound.companyBuyingRate * 100, 0) : 0f;
        }

        private int GetEnemyCount()
        {
            return LethalMenuMod.Enemies.Count(e => e != null && !e.isEnemyDead);
        }

        private int GetBodyCount()
        {
            return LethalMenuMod.Players.Count(p => p != null && p.isPlayerDead);
        }

        private int GetShipValue()
        {
            return LethalMenuMod.Items
                .Where(i => i != null && i.isInShipRoom && !i.isHeld && !i.isPocketed && !IsDefaultShipItem(i))
                .Sum(i => i.scrapValue);
        }

        private int GetShipLootCount()
        {
            return LethalMenuMod.Items
                .Count(i => i != null && i.isInShipRoom && !i.isHeld && !i.isPocketed && !IsDefaultShipItem(i));
        }

        private int GetMapLootValue()
        {
            return LethalMenuMod.Items
                .Where(i => i != null && !i.isInShipRoom && !i.isHeld && !i.isPocketed && !IsDefaultShipItem(i))
                .Sum(i => i.scrapValue);
        }

        private int GetMapLootCount()
        {
            return LethalMenuMod.Items
                .Count(i => i != null && !i.isInShipRoom && !i.isHeld && !i.isPocketed && !IsDefaultShipItem(i));
        }

        private string GetCurrentMoon()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound?.currentLevel == null) return "Unknown";
            return startOfRound.currentLevel.PlanetName ?? "Unknown";
        }

        private string GetCurrentTime()
        {
            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay == null) return "--:--";

            // Calculate hour from normalized time (0-1 = 6am to midnight)
            float normalizedTime = timeOfDay.currentDayTime / timeOfDay.totalTime;
            float hour = 6f + (normalizedTime * 18f); // 6am to midnight (18 hours)
            
            if (hour >= 24f) hour -= 24f;
            
            int hourInt = (int)hour;
            int minutes = (int)((hour - hourInt) * 60);
            
            string ampm = hourInt >= 12 ? "PM" : "AM";
            if (hourInt > 12) hourInt -= 12;
            if (hourInt == 0) hourInt = 12;
            
            return $"{hourInt}:{minutes:D2} {ampm}";
        }

        /// Check if an item is a default ship item (flashlight, walkie, etc.)
        private bool IsDefaultShipItem(GrabbableObject item)
        {
            if (item?.itemProperties == null) return false;
            
            // Items that start in the ship and shouldn't count as loot
            string itemName = item.itemProperties.itemName?.ToLower() ?? "";
            return itemName.Contains("flashlight") || 
                   itemName.Contains("walkie") || 
                   itemName.Contains("shovel") ||
                   itemName.Contains("key") ||
                   itemName.Contains("clipboard") ||
                   itemName.Contains("sticky note") ||
                   item.scrapValue <= 0; // Non-scrap items
        }

        #endregion
    }
}

