using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System.Collections;
using LethalMenu.Mixins;
using LethalMenu.Patches;
using LethalMenu.Menu.Popup;

namespace LethalMenu.Menu
{
    ///
    /// Unity IMGUI-based menu system with styling.
    ///
    public partial class HackMenu : ITeleporter, IHazardController, IShipController, IItemManipulator, IEnemyPrompter, IJetpack
    {
        private Rect _windowRect;
        private bool _windowRectInitialized = false;
        private int _selectedTab = 0;
        private readonly string[] _tabs = { "Self", "Enemies", "Items", "Visuals", "World", "Network", "Terminal", "Browser", "Settings", "Experimentation" };
        private Vector2 _scrollPosition;
        private bool _stylesInitialized = false;
        
        // Resize state
        private bool _isResizing = false;
        private const float ResizeHandleSize = 20f;
        private const float MinWindowWidth = 400f;
        private const float MinWindowHeight = 300f;

        // Credit editor state
        private string _creditInput = "10000";
        private Terminal? _cachedTerminal;
        
        // Quota state
        private string _quotaInput = "130";
        private string _quotaFulfilledInput = "0";

        // Custom styles
        private GUIStyle? _windowStyle;
        private GUIStyle? _tabStyle;
        private GUIStyle? _selectedTabStyle;
        private GUIStyle? _toggleStyle;
        private GUIStyle? _labelStyle;
        private GUIStyle? _headerStyle;
        private GUIStyle? _buttonStyle;
        private GUIStyle? _boxStyle;
        private GUIStyle? _textFieldStyle;

        // Colors
        private readonly Color _accentColor = new Color(0.65f, 0.22f, 0.99f, 1f);       // Purple
        private readonly Color _bgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);        // Dark gray
        private readonly Color _headerColor = new Color(0.08f, 0.08f, 0.1f, 1f);        // Darker
        private readonly Color _textColor = new Color(0.9f, 0.9f, 0.9f, 1f);            // Light gray
        private readonly Color _enabledColor = new Color(0.4f, 0.9f, 0.4f, 1f);         // Green
        private readonly Color _disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);        // Gray

        private Texture2D? _bgTexture;
        private Texture2D? _headerTexture;
        private Texture2D? _buttonTexture;
        private Texture2D? _buttonHoverTexture;
        private Texture2D? _toggleOnTexture;

        // Collapsible section style
        private GUIStyle? _collapseButtonStyle;

        // Popup managers
        private readonly ItemManagerPopup _itemManager = new();
        private readonly EnemyManagerPopup _enemyManager = new();
        private readonly WeatherManagerPopup _weatherManager = new();
        private readonly SuitManagerPopup _suitManager = new();
        private readonly UnlockablesManagerPopup _unlockablesManager = new();
        private readonly LootManagerPopup _lootManager = new();
        private readonly MoonManagerPopup _moonManager = new();

        private void Stylize()
        {
            if (Theme.ThemeLoader.Skin == null)
                Theme.ThemeLoader.SetTheme(Settings.ThemeName);

            if (Theme.ThemeLoader.Skin != null)
            {
                GUI.skin = Theme.ThemeLoader.Skin;
                GUI.skin.label.fontSize = Settings.MenuFontSize;
                GUI.skin.button.fontSize = Settings.MenuFontSize;
                GUI.skin.toggle.fontSize = Settings.MenuFontSize;
                GUI.skin.box.fontSize = Settings.MenuFontSize;
                GUI.skin.textField.fontSize = Settings.MenuFontSize;
            }
        }

        public void Draw()
        {
            Stylize();
            InitStyles();
            
            // Initialize window rect from saved settings (once)
            if (!_windowRectInitialized)
            {
                _windowRect = new Rect(Settings.WindowX, Settings.WindowY, Settings.WindowWidth, Settings.WindowHeight);
                _windowRectInitialized = true;
            }

            // Apply custom skin
            GUI.skin.window = _windowStyle;

            // Handle resize before drawing window
            HandleResize();

            GUI.color = new Color(1f, 1f, 1f, Settings.MenuAlpha);
            _windowRect = GUI.Window(12345, _windowRect, DrawWindow, "");
            GUI.color = Color.white;

            // Draw popup windows
            _itemManager.Draw();
            _enemyManager.Draw();
            _weatherManager.Draw();
            _suitManager.Draw();
            _unlockablesManager.Draw();
            _lootManager.Draw();
            _moonManager.Draw();

            // Keep window on screen
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
            
            // Update settings with current window state
            Settings.WindowX = _windowRect.x;
            Settings.WindowY = _windowRect.y;
            Settings.WindowWidth = _windowRect.width;
            Settings.WindowHeight = _windowRect.height;
        }
        
        private void HandleResize()
        {
            // Resize handle rect (bottom-right corner)
            Rect resizeHandle = new Rect(
                _windowRect.x + _windowRect.width - ResizeHandleSize,
                _windowRect.y + _windowRect.height - ResizeHandleSize,
                ResizeHandleSize,
                ResizeHandleSize
            );

            Event e = Event.current;
            
            // Start resize on mouse down in handle area
            if (e.type == EventType.MouseDown && e.button == 0 && resizeHandle.Contains(e.mousePosition))
            {
                _isResizing = true;
                e.Use();
            }
            
            // Continue resize while dragging
            if (_isResizing)
            {
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
                {
                    float newWidth = e.mousePosition.x - _windowRect.x;
                    float newHeight = e.mousePosition.y - _windowRect.y;
                    
                    _windowRect.width = Mathf.Max(MinWindowWidth, newWidth);
                    _windowRect.height = Mathf.Max(MinWindowHeight, newHeight);
                    
                    e.Use();
                }
                
                // Stop resize on mouse up
                if (e.type == EventType.MouseUp)
                {
                    _isResizing = false;
                    e.Use();
                }
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            // Create textures
            _bgTexture = MakeTexture(2, 2, _bgColor);
            _headerTexture = MakeTexture(2, 2, _headerColor);
            _buttonTexture = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.22f, 1f));
            _buttonHoverTexture = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.35f, 1f));
            _toggleOnTexture = MakeTexture(2, 2, _accentColor);

            // Window style
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _bgTexture, textColor = _textColor },
                onNormal = { background = _bgTexture, textColor = _textColor },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(10, 10, 35, 10)
            };

            // Tab style
            _tabStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _buttonTexture, textColor = _textColor },
                hover = { background = _buttonHoverTexture, textColor = _textColor },
                active = { background = _buttonHoverTexture, textColor = _textColor },
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(2, 2, 0, 0)
            };

            // Selected tab style
            _selectedTabStyle = new GUIStyle(_tabStyle)
            {
                normal = { background = _toggleOnTexture, textColor = Color.white },
                hover = { background = _toggleOnTexture, textColor = Color.white },
                fontStyle = FontStyle.Bold
            };

            // Toggle style
            _toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                normal = { textColor = _disabledColor },
                onNormal = { textColor = _enabledColor },
                hover = { textColor = _textColor },
                onHover = { textColor = _enabledColor },
                fontSize = 13,
                padding = new RectOffset(20, 0, 2, 2),
                margin = new RectOffset(4, 4, 4, 4)
            };

            // Label style
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = _textColor },
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };

            // Header style
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = _accentColor },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 5, 5)
            };

            // Button style
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _buttonTexture, textColor = _textColor },
                hover = { background = _buttonHoverTexture, textColor = Color.white },
                active = { background = _toggleOnTexture, textColor = Color.white },
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6)
            };

            // Box style for sections
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _headerTexture, textColor = _textColor },
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 5, 5)
            };

            // Text field style
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                normal = { background = _buttonTexture, textColor = _textColor },
                focused = { background = _buttonHoverTexture, textColor = Color.white },
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4)
            };

            // Collapse button style (invisible button for header)
            _collapseButtonStyle = new GUIStyle()
            {
                normal = { textColor = _accentColor },
                hover = { textColor = Color.white },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 5, 5)
            };
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void DrawWindow(int windowId)
        {
            // Draw title bar
            GUI.DrawTexture(new Rect(0, 0, _windowRect.width, 30), _headerTexture);
            GUI.Label(new Rect(10, 5, 200, 20), "<b>LETHAL MENU</b>", new GUIStyle(_labelStyle) { richText = true, fontSize = 14 });
            GUI.Label(new Rect(_windowRect.width - 100, 5, 90, 20), "v1.0", new GUIStyle(_labelStyle) { alignment = TextAnchor.MiddleRight, fontSize = 11 });

            GUILayout.Space(5);

            // Draw tabs
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabs.Length; i++)
            {
                var style = (i == _selectedTab) ? _selectedTabStyle : _tabStyle;
                if (GUILayout.Button(_tabs[i], style, GUILayout.Height(28)))
                {
                    _selectedTab = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Draw content in scrollview
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);

            switch (_selectedTab)
            {
                case 0: DrawSelfTab(); break;
                case 1: DrawEnemiesTab(); break;
                case 2: DrawItemsTab(); break;
                case 3: DrawVisualsTab(); break;
                case 4: DrawWorldTab(); break;
                case 5: DrawNetworkTab(); break;
                case 6: DrawTerminalTab(); break;
                case 7: DrawBrowserTab(); break;
                case 8: DrawSettingsTab(); break;
                case 9: DrawExperimentationTab(); break;
            }

            GUILayout.EndScrollView();
            
            // Draw resize handle indicator (bottom-right corner)
            Rect handleRect = new Rect(_windowRect.width - ResizeHandleSize, _windowRect.height - ResizeHandleSize, ResizeHandleSize, ResizeHandleSize);
            GUI.DrawTexture(handleRect, _buttonHoverTexture);
            // Draw diagonal lines to indicate resize
            var handleStyle = new GUIStyle(_labelStyle) { fontSize = 10, alignment = TextAnchor.LowerRight };
            GUI.Label(handleRect, "◢", handleStyle);

            // Make window draggable from title bar
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 30));
        }


        // Player selector state (shared by multiple tabs)
        private int _selectedPlayerIndex = 0;

        // Helper: Draw a collapsible section with header (state persisted in Settings)
        private void DrawSection(string title, System.Action content)
        {
            bool isCollapsed = Settings.CollapsedSections.Contains(title);
            string arrow = isCollapsed ? "▶" : "▼";
            string headerText = $"{arrow} {title}";

            // Clickable header
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(headerText, _collapseButtonStyle, GUILayout.Height(24)))
            {
                if (isCollapsed)
                    Settings.CollapsedSections.Remove(title);
                else
                    Settings.CollapsedSections.Add(title);
            }
            GUILayout.EndHorizontal();

            // Only draw content if not collapsed
            if (!isCollapsed)
            {
                GUILayout.BeginVertical(_boxStyle);
                content?.Invoke();
                GUILayout.EndVertical();
            }
            GUILayout.Space(5);
        }

        // Helper: Draw a toggle with optional description
        private bool DrawToggle(string label, bool value, string? tooltip = null)
        {
            GUILayout.BeginHorizontal();
            bool newValue = GUILayout.Toggle(value, label, _toggleStyle);
            if (!string.IsNullOrEmpty(tooltip))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(tooltip, new GUIStyle(_labelStyle) { fontSize = 11, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } });
            }
            GUILayout.EndHorizontal();
            return newValue;
        }

        private void DrawHackToggle(Hack hack, string? tooltip = null)
        {
            string label = hack.GetDisplayName();
            GUILayout.BeginHorizontal();
            bool newValue = GUILayout.Toggle(hack.IsEnabled(), label, _toggleStyle);
            if (newValue != hack.IsEnabled())
                hack.SetEnabled(newValue);
            if (!string.IsNullOrEmpty(tooltip))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(tooltip, new GUIStyle(_labelStyle) { fontSize = 11, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } });
            }
            GUILayout.EndHorizontal();
        }

        private void DrawHackToggle(Hack hack, string label, string? tooltip)
        {
            GUILayout.BeginHorizontal();
            bool newValue = GUILayout.Toggle(hack.IsEnabled(), label, _toggleStyle);
            if (newValue != hack.IsEnabled())
                hack.SetEnabled(newValue);
            if (!string.IsNullOrEmpty(tooltip))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(tooltip, new GUIStyle(_labelStyle) { fontSize = 11, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } });
            }
            GUILayout.EndHorizontal();
        }

        private void DrawHackButton(Hack hack, string? label = null)
        {
            if (GUILayout.Button(label ?? hack.GetDisplayName(), _buttonStyle))
                hack.Execute();
        }

    }
}
