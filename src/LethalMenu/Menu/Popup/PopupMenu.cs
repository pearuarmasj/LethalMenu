using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public abstract class PopupMenu
    {
        public bool IsOpen { get; set; }
        public string Title { get; }
        protected Rect WindowRect;
        protected Vector2 ScrollPosition;
        private readonly int _windowId;

        protected PopupMenu(string title, int windowId, float width = 400, float height = 350)
        {
            Title = title;
            _windowId = windowId;
            WindowRect = new Rect(
                Screen.width / 2f - width / 2f,
                Screen.height / 2f - height / 2f,
                width, height);
        }

        public void Draw()
        {
            if (!IsOpen) return;
            GUI.color = new Color(1f, 1f, 1f, Settings.MenuAlpha);
            WindowRect = GUILayout.Window(_windowId, WindowRect, DrawContent, Title);
            GUI.color = Color.white;
        }

        private void DrawContent(int id)
        {
            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);
            DrawBody();
            GUILayout.EndScrollView();

            if (GUILayout.Button("Close", GUILayout.Height(24)))
                IsOpen = false;

            GUI.DragWindow(new Rect(0, 0, WindowRect.width, 25));
        }

        protected abstract void DrawBody();
    }
}
