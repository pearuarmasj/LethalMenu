using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class SuitManagerPopup : PopupMenu
    {
        public SuitManagerPopup() : base("Suit Manager", 20004, 350, 400) { }

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
                    if (instance.localPlayerController != null)
                        UnlockableSuit.SwitchSuitForPlayer(instance.localPlayerController, i, true);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
