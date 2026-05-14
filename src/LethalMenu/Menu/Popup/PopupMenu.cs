using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public abstract class PopupMenu
    {
        private bool _isOpen;

        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen == value) return;

                _isOpen = value;
                if (_isOpen)
                    OnOpen();
                else
                    OnClose();
            }
        }

        public string Title { get; }
        protected Rect WindowRect;
        protected Vector2 ScrollPosition;
        private readonly int _windowId;
        private readonly float _minWidth;
        private readonly float _minHeight;
        private bool _isResizing;
        private const float ResizeHandleSize = 18f;

        protected PopupMenu(string title, int windowId, float width = 400, float height = 350)
        {
            Title = title;
            _windowId = windowId;
            _minWidth = Mathf.Min(width, 320f);
            _minHeight = Mathf.Min(height, 260f);
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

            HandleResize(id);
            GUI.DragWindow(new Rect(0, 0, WindowRect.width, 25));
        }

        protected abstract void DrawBody();

        protected virtual void OnOpen() { }

        protected virtual void OnClose() { }

        private void HandleResize(int id)
        {
            var resizeRect = new Rect(
                WindowRect.width - ResizeHandleSize,
                WindowRect.height - ResizeHandleSize,
                ResizeHandleSize,
                ResizeHandleSize);

            GUI.Box(resizeRect, string.Empty);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && resizeRect.Contains(e.mousePosition))
            {
                _isResizing = true;
                GUIUtility.hotControl = id;
                e.Use();
            }

            if (_isResizing && e.type == EventType.MouseDrag)
            {
                WindowRect.width = Mathf.Max(_minWidth, e.mousePosition.x + ResizeHandleSize * 0.5f);
                WindowRect.height = Mathf.Max(_minHeight, e.mousePosition.y + ResizeHandleSize * 0.5f);
                e.Use();
            }

            if (_isResizing && e.type == EventType.MouseUp)
            {
                _isResizing = false;
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }
    }
}
