using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class EntranceTeleportPopup : PopupMenu
    {
        private int _targetIndex;

        public EntranceTeleportPopup() : base("Entrance Teleporter", 20011, 420, 420) { }

        public void Show(PlayerControllerB? preselectTarget = null)
        {
            _targetIndex = 0;
            IsOpen = true;
            // preselectTarget wiring is added in a later task once the target list exists.
        }

        protected override void DrawBody()
        {
            GUILayout.Label($"(Entrance list rendered here. target index: {_targetIndex})");
        }
    }
}
