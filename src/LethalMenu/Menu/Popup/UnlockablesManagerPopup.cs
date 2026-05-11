using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class UnlockablesManagerPopup : PopupMenu
    {
        public UnlockablesManagerPopup() : base("Unlockables Manager", 20005, 400, 400) { }

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

            var terminal = Object.FindObjectOfType<Terminal>();
            int credits = terminal?.groupCredits ?? 0;

            for (int i = 0; i < unlockables.Count; i++)
            {
                var item = unlockables[i];
                if (item == null || item.suitMaterial != null) continue;
                if (string.IsNullOrEmpty(item.unlockableName)) continue;

                bool owned = item.hasBeenUnlockedByPlayer || item.alreadyUnlocked;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.unlockableName} {(owned ? "[OWNED]" : "")}", GUILayout.Width(250));
                if (!owned && GUILayout.Button("Unlock Free", GUILayout.Width(100)))
                    instance.BuyShipUnlockableServerRpc(i, credits);
                GUILayout.EndHorizontal();
            }
        }
    }
}
