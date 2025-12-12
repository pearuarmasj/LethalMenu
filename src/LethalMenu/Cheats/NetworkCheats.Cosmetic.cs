using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Cosmetic - XP/Level Manipulation

        /// Sets local player XP to a specific value and syncs to others.
        /// Purely cosmetic - only changes the badge and displayed level.
        public static void SetPlayerXP(int xp)
        {
            var hud = HUDManager.Instance;
            if (hud == null || LethalMenuMod.LocalPlayer == null)
            {
                Debug.LogWarning("[NetworkCheats] Not in game.");
                return;
            }

            xp = Mathf.Max(0, xp);

            // Find the level index for this XP
            int levelIndex = 0;
            for (int i = 0; i < hud.playerLevels.Length; i++)
            {
                if (xp >= hud.playerLevels[i].XPMin && xp < hud.playerLevels[i].XPMax)
                {
                    levelIndex = i;
                    break;
                }
                if (i == hud.playerLevels.Length - 1)
                {
                    levelIndex = i; // Max level
                }
            }

            // Set locally
            hud.localPlayerXP = xp;
            hud.localPlayerLevel = levelIndex;

            // Update UI
            hud.playerLevelText.text = hud.playerLevels[levelIndex].levelName;
            hud.playerLevelXPCounter.text = $"{xp} EXP";
            if (hud.playerLevels[levelIndex].XPMax > 0)
            {
                hud.playerLevelMeter.fillAmount = (float)(xp - hud.playerLevels[levelIndex].XPMin) /
                    (hud.playerLevels[levelIndex].XPMax - hud.playerLevels[levelIndex].XPMin);
            }

            // Sync to other players
            bool hasBeta = ES3.Load<bool>("playedDuringBeta", "LCGeneralSaveData", true);
            hud.SyncPlayerLevelServerRpc((int)LethalMenuMod.LocalPlayer.playerClientId, levelIndex, hasBeta);

            Debug.Log($"[NetworkCheats] Set XP to {xp} (Level: {hud.playerLevels[levelIndex].levelName})");
            HUDManager.Instance?.DisplayTip("XP Set", $"{xp} XP - {hud.playerLevels[levelIndex].levelName}");
        }

        /// Sets local player to a specific level index.
        public static void SetPlayerLevel(int levelIndex)
        {
            var hud = HUDManager.Instance;
            if (hud == null || LethalMenuMod.LocalPlayer == null)
            {
                Debug.LogWarning("[NetworkCheats] Not in game.");
                return;
            }

            levelIndex = Mathf.Clamp(levelIndex, 0, hud.playerLevels.Length - 1);
            int xp = hud.playerLevels[levelIndex].XPMin;
            SetPlayerXP(xp);
        }

        /// Gets all available level names for UI.
        public static string[] GetLevelNames()
        {
            var hud = HUDManager.Instance;
            if (hud?.playerLevels == null) return new[] { "Unknown" };

            string[] names = new string[hud.playerLevels.Length];
            for (int i = 0; i < hud.playerLevels.Length; i++)
            {
                names[i] = hud.playerLevels[i].levelName ?? $"Level {i}";
            }
            return names;
        }

        /// Gets current player level index.
        public static int GetCurrentLevelIndex()
        {
            return HUDManager.Instance?.localPlayerLevel ?? 0;
        }

        /// Gets current player XP.
        public static int GetCurrentXP()
        {
            return HUDManager.Instance?.localPlayerXP ?? 0;
        }

        /// Sets XP to maximum for flex purposes.
        public static void MaxOutXP()
        {
            var hud = HUDManager.Instance;
            if (hud?.playerLevels == null) return;

            int maxLevel = hud.playerLevels.Length - 1;
            int maxXP = hud.playerLevels[maxLevel].XPMax - 1;
            SetPlayerXP(maxXP);
        }

        #endregion
    }
}
