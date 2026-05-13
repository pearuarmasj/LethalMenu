using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class SuitManagerPopup : PopupMenu
    {
        public SuitManagerPopup() : base("Suit Manager", 20004, 350, 400) { }

        private static void WearSuit(StartOfRound instance, int suitId)
        {
            var player = instance.localPlayerController ?? GameNetworkManager.Instance?.localPlayerController;
            if (player == null)
                return;

            var suitRacks = Object.FindObjectsOfType<UnlockableSuit>();
            foreach (var suitRack in suitRacks)
            {
                if (suitRack == null) continue;
                if (suitRack.suitID != suitId) continue;

                suitRack.SwitchSuitToThis(player);
                return;
            }

            UnlockableSuit.SwitchSuitForPlayer(player, suitId, true);
        }

        protected override void DrawBody()
        {
            var instance = StartOfRound.Instance;
            if (instance == null)
            {
                GUILayout.Label("Not in game");
                return;
            }

            var unlockables = instance.unlockablesList?.unlockables;
            if (unlockables == null) return;

            for (int i = 0; i < unlockables.Count; i++)
            {
                var item = unlockables[i];
                if (item == null || item.suitMaterial == null) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(item.unlockableName ?? $"Suit {i}", GUILayout.Width(200));
                if (GUILayout.Button("Wear", GUILayout.Width(60)))
                {
                    WearSuit(instance, i);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
