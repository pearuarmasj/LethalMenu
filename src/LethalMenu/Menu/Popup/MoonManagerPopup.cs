using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class MoonManagerPopup : PopupMenu
    {
        public MoonManagerPopup() : base("Moon Manager", 20007, 450, 400) { }

        protected override void DrawBody()
        {
            var instance = StartOfRound.Instance;
            if (instance == null)
            {
                GUILayout.Label("Not in game");
                return;
            }

            GUILayout.Label($"Current: {instance.currentLevel?.PlanetName ?? "Unknown"}");
            GUILayout.Space(5);

            DrawLandingControls(instance);
            GUILayout.Space(8);

            var levels = instance.levels;
            if (levels == null) return;

            var terminal = Object.FindObjectOfType<Terminal>();
            int credits = terminal?.groupCredits ?? 0;

            for (int i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                if (level == null) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{level.PlanetName} ({level.currentWeather})", GUILayout.Width(250));
                int levelId = level.levelID;
                if (GUILayout.Button("Route", GUILayout.Width(60)))
                    instance.ChangeLevelServerRpc(levelId, credits);
                if (GUILayout.Button("Free", GUILayout.Width(50)))
                    instance.ChangeLevelServerRpc(levelId, credits);
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawLandingControls(StartOfRound instance)
        {
            bool isHost = LethalMenuMod.LocalPlayer?.IsHost == true;
            bool canLand = instance.inShipPhase && !instance.travellingToNewLevel;

            GUILayout.BeginHorizontal();
            GUI.enabled = canLand;
            if (GUILayout.Button(canLand ? "Land" : "Land (wait...)"))
                RequestLand(instance, isHost);
            GUI.enabled = true;

            if (isHost && GUILayout.Button("Force Land"))
                ForceLand(instance);
            GUILayout.EndHorizontal();
        }

        private static void RequestLand(StartOfRound instance, bool isHost)
        {
            var lever = Object.FindObjectOfType<StartMatchLever>();
            if (lever != null)
            {
                lever.singlePlayerEnabled = true;
                lever.PlayLeverPullEffectsServerRpc(true);
            }

            if (isHost)
                instance.StartGame();
            else
                instance.StartGameServerRpc();

            Loader.Log($"[MoonManager] Landing requested for {instance.currentLevel?.PlanetName ?? "Unknown"}");
        }

        private static void ForceLand(StartOfRound instance)
        {
            var lever = Object.FindObjectOfType<StartMatchLever>();
            if (lever != null)
            {
                lever.singlePlayerEnabled = true;
                lever.triggerScript.interactable = true;
            }

            instance.travellingToNewLevel = false;
            instance.shipLeftAutomatically = false;
            instance.inShipPhase = true;

            var localPlayer = GameNetworkManager.Instance?.localPlayerController;
            if (localPlayer != null &&
                instance.fullyLoadedPlayers != null &&
                !instance.fullyLoadedPlayers.Contains(localPlayer.playerClientId))
            {
                instance.fullyLoadedPlayers.Add(localPlayer.playerClientId);
            }

            instance.StartGame();
            Loader.Log($"[MoonManager] Force landed on {instance.currentLevel?.PlanetName ?? "Unknown"}");
        }
    }
}
