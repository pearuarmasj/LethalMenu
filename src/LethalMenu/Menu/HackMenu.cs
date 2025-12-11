using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System.Collections;
using LethalMenu.Patches;

namespace LethalMenu.Menu
{
    /// 
    /// Unity IMGUI-based menu system with styling.
    /// 
    public class HackMenu
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

        // Item spawner state
        private int _selectedItemIndex = 0;
        private string _spawnValue = "100";
        private string _expLevelInput = "1";
        private string _expUnlockableInput = "0";
        private int _expTurretMode = 3;
        private string _expSpawnEnemyName = "Bracken";
        
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

        public void Draw()
        {
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

            _windowRect = GUI.Window(12345, _windowRect, DrawWindow, "");

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

        private void DrawSelfTab()
        {
            DrawSection("Player Cheats", () =>
            {
                Settings.GodMode = DrawToggle("God Mode", Settings.GodMode, "Prevents all damage");
                // Demi-God is now per-player in the Players tab
                Settings.InfiniteStamina = DrawToggle("Infinite Stamina", Settings.InfiniteStamina, "Never run out of sprint");
                Settings.NoFallDamage = DrawToggle("No Fall Damage", Settings.NoFallDamage, "Take no damage from falls");
                Settings.NoWeight = DrawToggle("No Weight", Settings.NoWeight, "Carry unlimited items without slowdown");
                
                // Extra item slots
                Settings.ExtraItemSlots = DrawToggle("Extra Item Slots", Settings.ExtraItemSlots, "Expand inventory (requires restart)");
                if (Settings.ExtraItemSlots)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Slots: {Settings.ItemSlotCount}", _labelStyle, GUILayout.Width(80));
                    Settings.ItemSlotCount = (int)GUILayout.HorizontalSlider(Settings.ItemSlotCount, 4, 20, GUILayout.Width(120));
                    GUILayout.EndHorizontal();
                    GUILayout.Label("  Changes apply on game restart", _labelStyle);
                }
                
                Settings.UnlimitedOxygen = DrawToggle("Unlimited Oxygen", Settings.UnlimitedOxygen, "No drowning");
                Settings.AntiFlash = DrawToggle("Anti-Flash", Settings.AntiFlash, "Block stun grenade effects");
                Settings.NoQuicksand = DrawToggle("No Quicksand", Settings.NoQuicksand, "No sinking/slowing");
                
                // Self-Revive button (only show when dead)
                if (LethalMenuMod.LocalPlayer?.isPlayerDead == true)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button("Self Revive", _buttonStyle, GUILayout.Height(28)))
                    {
                        Cheats.NetworkCheats.SelfRevive();
                    }
                    GUILayout.Label("  Respawn at ship (client-side)", _labelStyle);
                }
                
                // Fake Death button (only show when alive)
                if (LethalMenuMod.LocalPlayer?.isPlayerDead == false)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Settings.FakeDeath ? "Cancel Fake Death" : "Fake Death", _buttonStyle, GUILayout.Height(28)))
                    {
                        if (Settings.FakeDeath)
                            Cheats.NetworkCheats.CancelFakeDeath();
                        else
                            Cheats.NetworkCheats.FakeDeath();
                    }
                    GUILayout.EndHorizontal();
                    if (Settings.FakeDeath)
                    {
                        GUILayout.Label("  Others see you dead. Will die when ship leaves.", _labelStyle);
                    }
                    else
                    {
                        GUILayout.Label("  Appear dead to others, stay alive", _labelStyle);
                    }
                }
            });

            DrawSection("Movement", () =>
            {
                Settings.NoClip = DrawToggle("No Clip", Settings.NoClip, "Fly through walls (WASD + Space/Ctrl)");
                Settings.SpeedHack = DrawToggle("Speed Hack", Settings.SpeedHack, "Move faster");

                if (Settings.SpeedHack)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Speed: {Settings.SpeedMultiplier:F1}x", _labelStyle, GUILayout.Width(80));
                    Settings.SpeedMultiplier = GUILayout.HorizontalSlider(Settings.SpeedMultiplier, 1f, 10f, GUILayout.Width(200));
                    GUILayout.EndHorizontal();
                }

                Settings.JumpHack = DrawToggle("Jump Hack", Settings.JumpHack, "Jump higher");

                if (Settings.JumpHack)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Jump: {Settings.JumpMultiplier:F1}x", _labelStyle, GUILayout.Width(80));
                    Settings.JumpMultiplier = GUILayout.HorizontalSlider(Settings.JumpMultiplier, 1f, 10f, GUILayout.Width(200));
                    GUILayout.EndHorizontal();
                }

                Settings.SuperSpeed = DrawToggle("Super Speed", Settings.SuperSpeed, "Move much faster");
                Settings.SuperJump = DrawToggle("Super Jump", Settings.SuperJump, "Jump much higher");
                Settings.UnlimitedJump = DrawToggle("Unlimited Jump", Settings.UnlimitedJump, "Jump in mid-air");
                Settings.FastClimb = DrawToggle("Fast Climb", Settings.FastClimb, "Climb ladders faster");
                Settings.TauntSlide = DrawToggle("Taunt Slide", Settings.TauntSlide, "Emote while moving");
            });

            DrawSection("Vision", () =>
            {
                Settings.NightVision = DrawToggle("Night Vision", Settings.NightVision, "See in the dark");
            });

            DrawSection("Teleport", () =>
            {
                Settings.TeleportWithItems = DrawToggle("Teleport With Items", Settings.TeleportWithItems, "Keep items when teleporting");
                
                if (GUILayout.Button("Teleport to Ship", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportToShip();
                }

                if (GUILayout.Button("Teleport to Main Entrance", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportToEntrance(true);
                }

                if (GUILayout.Button("Teleport to Fire Exit", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportToEntrance(false);
                }
            });
        }

        private void DrawEnemiesTab()
        {
            DrawSection("Enemy Protection", () =>
            {
                Settings.Untargetable = DrawToggle("Untargetable", Settings.Untargetable, "Enemies ignore you");
                Settings.GhostMode = DrawToggle("Ghost Mode", Settings.GhostMode, "Enemies can't target you (EnemyAI)");
                Settings.AntiGhostGirl = DrawToggle("Anti-Ghost Girl", Settings.AntiGhostGirl, "Ghost Girl won't haunt you");
            });

            DrawSection("Enemy Control", () =>
            {
                // Show possession status
                bool isPossessing = Cheats.EnemyControlCheat.IsControlling;
                var controlledEnemy = Cheats.EnemyControlCheat.GetControlledEnemy();
                
                if (isPossessing && controlledEnemy != null)
                {
                    string enemyName = controlledEnemy.enemyType?.enemyName ?? "Unknown";
                    string aiStatus = Cheats.EnemyControlCheat.IsAIControlled ? "AI" : "Manual";
                    string noClipStatus = Cheats.EnemyControlCheat.NoClipEnabled ? " | NoClip" : "";
                    
                    GUILayout.BeginVertical(_boxStyle);
                    GUILayout.Label($"Possessing: {enemyName}", _labelStyle);
                    GUILayout.Label($"Mode: {aiStatus}{noClipStatus}", _labelStyle);
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Release (Z)", _buttonStyle, GUILayout.Width(90)))
                    {
                        Cheats.EnemyControlCheat.StopControl();
                    }
                    if (GUILayout.Button("Kill (Del)", _buttonStyle, GUILayout.Width(90)))
                    {
                        controlledEnemy.KillEnemyOnOwnerClient(false);
                        Cheats.EnemyControlCheat.StopControl();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
                else
                {
                    Settings.EnemyControl = DrawToggle("Enemy Control", Settings.EnemyControl, "RMB to possess enemy you're looking at");
                    if (Settings.EnemyControl)
                    {
                        GUILayout.Label("  Close menu, look at enemy, RMB to possess", _labelStyle);
                    }
                }
                
                GUILayout.Space(5);
                Settings.KillClick = DrawToggle("Kill Click", Settings.KillClick, "LMB kills enemies (close menu first)");
                Settings.StunClick = DrawToggle("Stun Click", Settings.StunClick, "MMB stuns enemies/turrets/mines");
                
                // Show controls when enabled
                if (Settings.EnemyControl && !isPossessing)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Controls while possessing:", _labelStyle);
                    GUILayout.Label("  WASD=Move | Shift=Sprint | Space=Jump", _labelStyle);
                    GUILayout.Label("  LMB=Primary | RMB=Secondary | E=Door", _labelStyle);
                    GUILayout.Label("  N=NoClip | F9=AI Toggle | Z=Release | Del=Kill", _labelStyle);
                }
            });

            DrawSection("Enemy Actions", () =>
            {
                int aliveCount = 0;
                foreach (var e in LethalMenuMod.Enemies)
                {
                    if (e != null && !e.isEnemyDead) aliveCount++;
                }
                GUILayout.Label($"Alive Enemies: {aliveCount} / {LethalMenuMod.Enemies.Count}", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Kill All", _buttonStyle, GUILayout.Height(28)))
                {
                    Cheats.NetworkCheats.KillAllEnemies();
                }
                if (GUILayout.Button("Stun All", _buttonStyle, GUILayout.Height(28)))
                {
                    Cheats.NetworkCheats.StunAllEnemies();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Teleport All Away", _buttonStyle, GUILayout.Height(28)))
                {
                    var farPos = new Vector3(0, -500, 0);
                    foreach (var enemy in LethalMenuMod.Enemies)
                    {
                        if (enemy != null && !enemy.isEnemyDead)
                        {
                            enemy.transform.position = farPos;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Enemy List", () =>
            {
                // List individual enemies
                foreach (var enemy in LethalMenuMod.Enemies)
                {
                    if (enemy == null || enemy.isEnemyDead) continue;

                    string enemyName = enemy.enemyType?.enemyName ?? "Unknown";
                    float dist = 0f;
                    if (LethalMenuMod.LocalPlayer != null)
                    {
                        dist = Vector3.Distance(LethalMenuMod.LocalPlayer.transform.position, enemy.transform.position);
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{enemyName} ({dist:F0}m)", _labelStyle, GUILayout.Width(200));
                    if (GUILayout.Button("Kill", _buttonStyle, GUILayout.Height(22), GUILayout.Width(50)))
                    {
                        enemy.KillEnemyOnOwnerClient(true);
                    }
                    if (GUILayout.Button("TP Away", _buttonStyle, GUILayout.Height(22), GUILayout.Width(60)))
                    {
                        enemy.transform.position = new Vector3(0, -500, 0);
                    }
                    GUILayout.EndHorizontal();
                }
            });
        }

        private void DrawItemsTab()
        {
            DrawSection("Item Cheats", () =>
            {
                Settings.InfiniteBattery = DrawToggle("Infinite Battery", Settings.InfiniteBattery, "Items never lose charge");
                Settings.OneHanded = DrawToggle("One-Handed", Settings.OneHanded, "Two-handed items become one-handed");
                Settings.StrongHands = DrawToggle("Strong Hands", Settings.StrongHands, "Two-handed items held one-handed");
                Settings.Reach = DrawToggle("Extended Reach", Settings.Reach, "Grab items from far away");
                Settings.LootThroughWalls = DrawToggle("Loot Through Walls", Settings.LootThroughWalls, "Grab items through walls");
                Settings.InteractThroughWalls = DrawToggle("Interact Through Walls", Settings.InteractThroughWalls, "Interact through walls");
                Settings.LootBeforeGameStarts = DrawToggle("Loot Before Start", Settings.LootBeforeGameStarts, "Grab items before game starts");
                Settings.GrabNutcrackerShotgun = DrawToggle("Grab Nutcracker Gun", Settings.GrabNutcrackerShotgun, "Steal shotgun from Nutcracker");
            });

            DrawSection("Weapon Cheats", () =>
            {
                Settings.SuperShovel = DrawToggle("Super Shovel", Settings.SuperShovel, "One-hit kill with shovel");
                Settings.SuperKnife = DrawToggle("Super Knife", Settings.SuperKnife, "Knife does massive damage");
                Settings.UnlimitedAmmo = DrawToggle("Unlimited Ammo", Settings.UnlimitedAmmo, "Shotgun never runs out");
                Settings.MinigunShotgun = DrawToggle("Minigun Shotgun", Settings.MinigunShotgun, "Hold LMB to rapid fire shotgun");
                Settings.UnlimitedZapGun = DrawToggle("Unlimited Zap Gun", Settings.UnlimitedZapGun, "Zap gun never overheats");
            });

            DrawSection("Special Items", () =>
            {
                Settings.UnlimitedTZP = DrawToggle("Unlimited TZP", Settings.UnlimitedTZP, "TZP never runs out");
                Settings.NoTZPEffects = DrawToggle("No TZP Effects", Settings.NoTZPEffects, "TZP doesn't affect vision");
                Settings.EggsAlwaysExplode = DrawToggle("Eggs Always Explode", Settings.EggsAlwaysExplode, "Easter eggs always explode");
                Settings.EggsNeverExplode = DrawToggle("Eggs Never Explode", Settings.EggsNeverExplode, "Easter eggs never explode");
            });

            DrawSection("Item Teleport", () =>
            {
                // Count ALL grabbable items (not just scrap) for teleport purposes
                var gameInstance = StartOfRound.Instance;
                var allItems = Object.FindObjectsOfType<GrabbableObject>();
                
                int totalItems = 0;
                int inShipCount = 0;
                int outsideCount = 0;

                if (gameInstance?.shipInnerRoomBounds != null)
                {
                    var shipBounds = gameInstance.shipInnerRoomBounds;
                    foreach (var item in allItems)
                    {
                        if (item == null) continue;
                        if (item.isHeld || item.isHeldByEnemy) continue;
                        if (item.scrapValue <= 0 && !item.itemProperties.isScrap) continue;

                        totalItems++;
                        if (shipBounds.bounds.Contains(item.transform.position))
                            inShipCount++;
                        else
                            outsideCount++;
                    }
                }

                GUILayout.Label($"Loot: {inShipCount} in ship, {outsideCount} outside ({totalItems} total)", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("TP All to Ship", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportItemsToShip();
                }
                if (GUILayout.Button("TP Nearby to Me", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportNearbyItemsToPlayer(15f);
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Item Spawner (Host Only)", () =>
            {
                var allItems = StartOfRound.Instance?.allItemsList?.itemsList;
                if (allItems == null || allItems.Count == 0)
                {
                    GUILayout.Label("No items available", _labelStyle);
                    return;
                }

                bool isHost = NetworkManager.Singleton?.IsHost ?? false;
                if (!isHost)
                {
                    GUILayout.Label("Only the host can spawn items", _labelStyle);
                    return;
                }

                // Item selector - show items with valid prefabs
                var spawnableItems = allItems.Where(i => i != null && i.spawnPrefab != null).ToList();
                if (spawnableItems.Count == 0)
                {
                    GUILayout.Label("No spawnable items found", _labelStyle);
                    return;
                }

                _selectedItemIndex = Mathf.Clamp(_selectedItemIndex, 0, spawnableItems.Count - 1);
                var selectedItem = spawnableItems[_selectedItemIndex];

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                {
                    _selectedItemIndex = (_selectedItemIndex - 1 + spawnableItems.Count) % spawnableItems.Count;
                }
                GUILayout.Label(selectedItem.itemName, _labelStyle, GUILayout.Width(150));
                if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                {
                    _selectedItemIndex = (_selectedItemIndex + 1) % spawnableItems.Count;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Value:", _labelStyle, GUILayout.Width(50));
                _spawnValue = GUILayout.TextField(_spawnValue, GUILayout.Width(60));
                if (GUILayout.Button("Spawn", _buttonStyle, GUILayout.Height(25)))
                {
                    SpawnItem(selectedItem);
                }
                GUILayout.EndHorizontal();
            });
        }

        private void DrawVisualsTab()
        {
            DrawSection("ESP", () =>
            {
                Settings.ESP = DrawToggle("Enable ESP", Settings.ESP, "Show objects through walls");

                if (Settings.ESP)
                {
                    GUILayout.Space(5);
                    Settings.PlayerESP = DrawToggle("  Player ESP", Settings.PlayerESP);
                    if (Settings.PlayerESP)
                    {
                        Settings.PlayerHealthBars = DrawToggle("    Health Bars", Settings.PlayerHealthBars, "Show HP bars above players");
                    }
                    Settings.EnemyESP = DrawToggle("  Enemy ESP", Settings.EnemyESP);
                    Settings.ItemESP = DrawToggle("  Item ESP", Settings.ItemESP);
                    Settings.DoorESP = DrawToggle("  Door ESP", Settings.DoorESP);
                    Settings.MineESP = DrawToggle("  Mine ESP", Settings.MineESP);
                    Settings.TurretESP = DrawToggle("  Turret ESP", Settings.TurretESP);
                    Settings.FuseboxESP = DrawToggle("  Fusebox ESP", Settings.FuseboxESP);
                }
            });

            DrawSection("Camera", () =>
            {
                Settings.FreeCam = DrawToggle("FreeCam", Settings.FreeCam, "WASD+Mouse to fly around");
                if (Settings.FreeCam)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Speed: {Settings.FreeCamSpeed:F0}", _labelStyle, GUILayout.Width(80));
                    Settings.FreeCamSpeed = GUILayout.HorizontalSlider(Settings.FreeCamSpeed, 5f, 50f);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Label("  Arrow L/R: Snap to players | Down: Back to you", _labelStyle);
                    if (GUILayout.Button("Teleport Me To Camera", _buttonStyle, GUILayout.Height(25)))
                    {
                        LethalMenu.Cheats.FreeCamCheat.TeleportToCameraPosition();
                    }
                    GUILayout.Label("  Or hold Shift when disabling FreeCam", _labelStyle);
                }

                // Third-person camera
                bool prevThirdPerson = Settings.ThirdPerson;
                Settings.ThirdPerson = DrawToggle("Third Person", Settings.ThirdPerson, "Press V to toggle (view from behind)");
                if (prevThirdPerson != Settings.ThirdPerson)
                {
                    LethalMenu.Cheats.ThirdPersonCheat.Toggle();
                }
                if (Settings.ThirdPerson)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Distance: {Settings.ThirdPersonDistance:F1}", _labelStyle, GUILayout.Width(100));
                    Settings.ThirdPersonDistance = GUILayout.HorizontalSlider(Settings.ThirdPersonDistance, 1f, 10f);
                    GUILayout.EndHorizontal();
                }

                // Spectate player
                Settings.SpectatePlayer = DrawToggle("Spectate Player", Settings.SpectatePlayer, "Watch another player");
                if (Settings.SpectatePlayer)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                    {
                        CyclePreviousSpectatePlayer();
                    }

                    string playerName = "None";
                    if (Settings.SpectatePlayerIndex >= 0 && Settings.SpectatePlayerIndex < LethalMenuMod.Players.Count)
                    {
                        var player = LethalMenuMod.Players[Settings.SpectatePlayerIndex];
                        if (player != null)
                        {
                            playerName = player.playerUsername ?? "Unknown";
                        }
                    }
                    GUILayout.Label(playerName, _labelStyle, GUILayout.Width(120));

                    if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                    {
                        CycleNextSpectatePlayer();
                    }
                    GUILayout.EndHorizontal();
                }
            });

            DrawSection("HUD/Visual", () =>
            {
                Settings.AlwaysShowClock = DrawToggle("Always Show Clock", Settings.AlwaysShowClock, "Clock always visible");
                Settings.Crosshair = DrawToggle("Crosshair", Settings.Crosshair, "Show crosshair on screen");
                Settings.HPDisplay = DrawToggle("HP Display", Settings.HPDisplay, "Show health on screen");
                
                // Info Display - comprehensive game info HUD
                Settings.InfoDisplay = DrawToggle("Info Display", Settings.InfoDisplay, "Show game info panel (top-right)");
                if (Settings.InfoDisplay)
                {
                    GUILayout.Label("  Display Options:", _labelStyle);
                    GUILayout.BeginHorizontal();
                    Settings.InfoDisplayCredits = GUILayout.Toggle(Settings.InfoDisplayCredits, "Credits", _buttonStyle, GUILayout.Width(70));
                    Settings.InfoDisplayQuota = GUILayout.Toggle(Settings.InfoDisplayQuota, "Quota", _buttonStyle, GUILayout.Width(60));
                    Settings.InfoDisplayDeadline = GUILayout.Toggle(Settings.InfoDisplayDeadline, "Deadline", _buttonStyle, GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    Settings.InfoDisplayEnemies = GUILayout.Toggle(Settings.InfoDisplayEnemies, "Enemies", _buttonStyle, GUILayout.Width(70));
                    Settings.InfoDisplayBodies = GUILayout.Toggle(Settings.InfoDisplayBodies, "Bodies", _buttonStyle, GUILayout.Width(60));
                    Settings.InfoDisplayMapLoot = GUILayout.Toggle(Settings.InfoDisplayMapLoot, "Map Loot", _buttonStyle, GUILayout.Width(75));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    Settings.InfoDisplayShipLoot = GUILayout.Toggle(Settings.InfoDisplayShipLoot, "Ship Loot", _buttonStyle, GUILayout.Width(75));
                    Settings.InfoDisplayMoon = GUILayout.Toggle(Settings.InfoDisplayMoon, "Moon", _buttonStyle, GUILayout.Width(55));
                    Settings.InfoDisplayTime = GUILayout.Toggle(Settings.InfoDisplayTime, "Time", _buttonStyle, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }
                
                Settings.NoVisor = DrawToggle("No Visor", Settings.NoVisor, "Hide helmet visor");
                Settings.NoCameraShake = DrawToggle("No Camera Shake", Settings.NoCameraShake, "Disable screen shake");
                Settings.NoFieldOfDepth = DrawToggle("No Depth of Field", Settings.NoFieldOfDepth, "Disable blur effects");
                
                // Full Render Resolution toggle - applies via Harmony patch
                bool prevFullRes = Settings.FullRenderResolution;
                Settings.FullRenderResolution = DrawToggle("Full Render Resolution", Settings.FullRenderResolution, "Render at native screen resolution");
                if (prevFullRes != Settings.FullRenderResolution)
                {
                    // Apply immediately when toggled
                    Patches.FullRenderResolutionPatch.ApplyResolution(LethalMenuMod.LocalPlayer);
                }
                
                Settings.FOV = DrawToggle("Custom FOV", Settings.FOV, "Change field of view");
                if (Settings.FOV)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  FOV: {Settings.FOVValue:F0}", _labelStyle, GUILayout.Width(80));
                    Settings.FOVValue = GUILayout.HorizontalSlider(Settings.FOVValue, 60f, 120f);
                    GUILayout.EndHorizontal();
                }
                Settings.Breadcrumbs = DrawToggle("Breadcrumbs", Settings.Breadcrumbs, "Mark your path");
                if (Settings.Breadcrumbs)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Interval: {Settings.BreadcrumbInterval:F1}s", _labelStyle, GUILayout.Width(100));
                    Settings.BreadcrumbInterval = GUILayout.HorizontalSlider(Settings.BreadcrumbInterval, 1f, 10f);
                    GUILayout.EndHorizontal();
                }
            });

            DrawSection("Environment", () =>
            {
                Settings.NoFog = DrawToggle("No Fog", Settings.NoFog, "Remove all fog effects");
            });
        }

        private void CycleNextSpectatePlayer()
        {
            if (LethalMenuMod.Players.Count <= 1) return;

            int startIndex = Settings.SpectatePlayerIndex;
            int nextIndex = (startIndex + 1) % LethalMenuMod.Players.Count;

            for (int i = 0; i < LethalMenuMod.Players.Count; i++)
            {
                var player = LethalMenuMod.Players[nextIndex];
                if (player != null && player != LethalMenuMod.LocalPlayer && !player.isPlayerDead)
                {
                    Settings.SpectatePlayerIndex = nextIndex;
                    return;
                }
                nextIndex = (nextIndex + 1) % LethalMenuMod.Players.Count;
            }
        }

        private void CyclePreviousSpectatePlayer()
        {
            if (LethalMenuMod.Players.Count <= 1) return;

            int startIndex = Settings.SpectatePlayerIndex;
            if (startIndex < 0) startIndex = 0;
            int prevIndex = (startIndex - 1 + LethalMenuMod.Players.Count) % LethalMenuMod.Players.Count;

            for (int i = 0; i < LethalMenuMod.Players.Count; i++)
            {
                var player = LethalMenuMod.Players[prevIndex];
                if (player != null && player != LethalMenuMod.LocalPlayer && !player.isPlayerDead)
                {
                    Settings.SpectatePlayerIndex = prevIndex;
                    return;
                }
                prevIndex = (prevIndex - 1 + LethalMenuMod.Players.Count) % LethalMenuMod.Players.Count;
            }
        }

        private void DrawPlayersTab()
        {
            DrawSection("Players", () =>
            {
                if (LethalMenuMod.Players.Count == 0)
                {
                    GUILayout.Label("No players in game", _labelStyle);
                    return;
                }

                foreach (var player in LethalMenuMod.Players)
                {
                    if (player == null) continue;

                    bool isLocal = player == LethalMenuMod.LocalPlayer;
                    bool isDead = player.isPlayerDead;
                    float dist = 0f;

                    if (!isLocal && LethalMenuMod.LocalPlayer != null)
                    {
                        dist = Vector3.Distance(LethalMenuMod.LocalPlayer.transform.position, player.transform.position);
                    }

                    string status = isDead ? " [DEAD]" : "";
                    string localTag = isLocal ? " (You)" : "";
                    string distText = isLocal ? "" : $" - {dist:F0}m";

                    GUILayout.BeginVertical(_boxStyle);
                    
                    // Player info row
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{player.playerUsername ?? "Unknown"}{localTag}{status}{distText}", _labelStyle, GUILayout.Width(220));

                    if (!isLocal && !isDead)
                    {
                        if (GUILayout.Button("TP To", _buttonStyle, GUILayout.Width(55)))
                        {
                            TeleportTo(player.transform.position);
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    // Per-player cheats row (only for alive players)
                    if (!isDead)
                    {
                        GUILayout.BeginHorizontal();
                        
                        // Demi-God toggle for this player
                        bool hasDemiGod = Settings.IsDemiGod(player);
                        bool newDemiGod = GUILayout.Toggle(hasDemiGod, "Demi-God", _buttonStyle, GUILayout.Width(80));
                        if (newDemiGod != hasDemiGod)
                        {
                            Settings.SetDemiGod(player, newDemiGod);
                        }
                        
                        // Heal button
                        if (GUILayout.Button("Heal", _buttonStyle, GUILayout.Width(50)))
                        {
                            HealPlayer(player);
                        }
                        
                        // Kill button (only for other players)
                        if (!isLocal)
                        {
                            if (GUILayout.Button("Kill", _buttonStyle, GUILayout.Width(50)))
                            {
                                KillPlayer(player);
                            }
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                    
                    GUILayout.EndVertical();
                }
            });
        }
        
        /// 
        /// Heal a player using negative damage exploit
        /// 
        private void HealPlayer(GameNetcodeStuff.PlayerControllerB player)
        {
            if (player == null || player.isPlayerDead) return;
            
            int healthNeeded = 100 - player.health;
            if (healthNeeded <= 0) return;
            
            player.DamagePlayerFromOtherClientServerRpc(
                -healthNeeded, 
                Vector3.zero, 
                (int)player.playerClientId
            );
        }
        
        /// 
        /// Kill a player using damage exploit
        /// 
        private void KillPlayer(GameNetcodeStuff.PlayerControllerB player)
        {
            if (player == null || player.isPlayerDead) return;
            
            // Deal massive damage
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;
            
            player.DamagePlayerFromOtherClientServerRpc(
                999, 
                Vector3.zero, 
                (int)localPlayer.playerClientId
            );
        }

        private void DrawWorldTab()
        {
            DrawSection("Credits", () =>
            {
                // Get or refresh terminal reference
                if (_cachedTerminal == null)
                {
                    _cachedTerminal = Object.FindObjectOfType<Terminal>();
                }

                int currentCredits = _cachedTerminal?.groupCredits ?? 0;
                GUILayout.Label($"Current Credits: ${currentCredits}", _labelStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Amount:", _labelStyle, GUILayout.Width(60));
                _creditInput = GUILayout.TextField(_creditInput, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Set Credits", _buttonStyle, GUILayout.Height(28)))
                {
                    SetCredits();
                }
                if (GUILayout.Button("+1000", _buttonStyle, GUILayout.Height(28), GUILayout.Width(60)))
                {
                    AddCredits(1000);
                }
                if (GUILayout.Button("+10000", _buttonStyle, GUILayout.Height(28), GUILayout.Width(70)))
                {
                    AddCredits(10000);
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Doors", () =>
            {
                if (GUILayout.Button("Unlock All Doors", _buttonStyle, GUILayout.Height(28)))
                {
                    foreach (var door in LethalMenuMod.DoorLocks)
                    {
                        if (door != null && door.isLocked)
                        {
                            door.UnlockDoorSyncWithServer();
                        }
                    }
                }
            });

            DrawSection("Ship", () =>
            {
                Settings.OpenShipDoorSpace = DrawToggle("Ship Door In Space", Settings.OpenShipDoorSpace, "Open ship door in space");
            });

            DrawSection("Fusebox Control", () =>
            {
                var breakerBoxes = LethalMenuMod.BreakerBoxes;
                if (breakerBoxes == null || breakerBoxes.Count == 0)
                {
                    GUILayout.Label("No fuseboxes found", _labelStyle);
                    return;
                }

                for (int boxIndex = 0; boxIndex < breakerBoxes.Count; boxIndex++)
                {
                    var box = breakerBoxes[boxIndex];
                    if (box == null) continue;

                    // Header row
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(box.isPowerOn ? "POWER ON" : "POWER OFF", _labelStyle);
                    if (GUILayout.Button("All ON", _buttonStyle, GUILayout.Width(60)))
                        SetAllSwitches(box, true);
                    if (GUILayout.Button("All OFF", _buttonStyle, GUILayout.Width(60)))
                        SetAllSwitches(box, false);
                    GUILayout.EndHorizontal();

                    // Switches
                    if (box.breakerSwitches != null)
                    {
                        for (int i = 0; i < box.breakerSwitches.Length; i++)
                        {
                            var anim = box.breakerSwitches[i];
                            if (anim == null) continue;
                            var trig = anim.gameObject.GetComponent<AnimatedObjectTrigger>();
                            if (trig == null) continue;

                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"  {i + 1}. {(trig.boolValue ? "ON" : "OFF")}", _labelStyle);
                            if (GUILayout.Button("Toggle", _buttonStyle, GUILayout.Width(60)))
                                ToggleSwitch(box, i);
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            });

            DrawSection("Environment", () =>
            {
                Settings.BridgeNeverFalls = DrawToggle("Bridge Never Falls", Settings.BridgeNeverFalls, "Bridges don't collapse");
                Settings.OpenDropShipLand = DrawToggle("Auto-Open Dropship", Settings.OpenDropShipLand, "Dropship opens on landing");
                Settings.Shoplifter = DrawToggle("Shoplifter", Settings.Shoplifter, "Terminal items cost $0");
                Settings.GrabInLobby = DrawToggle("Grab In Lobby", Settings.GrabInLobby, "Grab items before round");
                Settings.JebAttackPrevention = DrawToggle("Anti-Jeb", Settings.JebAttackPrevention, "Company desk won't attack");
                Settings.BuildAnywhere = DrawToggle("Build Anywhere", Settings.BuildAnywhere, "Place furniture outside ship");
                Settings.InstantInteract = DrawToggle("Instant Interact", Settings.InstantInteract, "No hold-to-interact delay");
            });
            // Hazard controls moved to Network tab -> Hazard Control section
        }

        // Network tab state
        private string _creditSetInput = "10000";
        private string _chatMessageInput = "";
        private string _signalMessageInput = "";
        
        // Host powers state
        private int _selectedEnemyIndex = 0;
        private string[]? _cachedEnemyNames = null;
        private int _selectedMimicPlayerIndex = 0;
        private int _selectedPlayerIndex = 0;
        private int _selectedUnlockableIndex = 0;

        private bool RequireHost()
        {
            if (LethalMenuMod.LocalPlayer?.IsHost == true)
                return true;

            HUDManager.Instance?.DisplayTip("Host Only", "This action requires host.", false, false, "LCM-Host");
            return false;
        }

        private void DrawNetworkTab()
        {
            DrawSection("Network Status", () =>
            {
                var gameInstance = StartOfRound.Instance;
                if (gameInstance != null)
                {
                    GUILayout.Label($"Current Planet: {gameInstance.currentLevel?.PlanetName ?? "Unknown"}", _labelStyle);
                    GUILayout.Label($"Is Host: {Cheats.NetworkCheats.IsHost()}", _labelStyle);

                    var terminal = Object.FindObjectOfType<Terminal>();
                    if (terminal != null)
                    {
                        GUILayout.Label($"Credits: ${terminal.groupCredits}", _labelStyle);
                    }

                    var timeOfDay = TimeOfDay.Instance;
                    if (timeOfDay != null)
                    {
                        GUILayout.Label($"Quota: ${timeOfDay.quotaFulfilled} / ${timeOfDay.profitQuota}", _labelStyle);
                    }
                }
                else
                {
                    GUILayout.Label("Not in game", _labelStyle);
                }
            });

            DrawSection("Networking Options", () =>
            {
                Settings.AntiKick = DrawToggle("Anti-Kick", Settings.AntiKick, "Rejoin lobbies after being kicked");
                Settings.ShowKickedLobbies = DrawToggle("Show Kicked Hosts", Settings.ShowKickedLobbies, "Mark lobbies from hosts who kicked you");
                
                if (Settings.KickedHostIds.Count > 0)
                {
                    GUILayout.Label($"  Kicked by {Settings.KickedHostIds.Count} host(s)", _labelStyle);
                    if (GUILayout.Button("Clear Kicked Hosts", _buttonStyle, GUILayout.Width(150)))
                    {
                        Settings.KickedHostIds.Clear();
                        Settings.SaveConfig();
                    }
                }
                
                Settings.HearEveryone = DrawToggle("Hear Everyone", Settings.HearEveryone, "Hear all voice chat");
                Settings.Invisibility = DrawToggle("Invisibility", Settings.Invisibility, "Other players can't see you");
                Settings.DeathNotifications = DrawToggle("Death Notifications", Settings.DeathNotifications, "See when players die");
                Settings.HearDeadPeople = DrawToggle("Hear Dead People", Settings.HearDeadPeople, "Hear dead players' voice chat");
            });

            DrawSection("Credits Exploit", () =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Amount:", _labelStyle, GUILayout.Width(60));
                _creditSetInput = GUILayout.TextField(_creditSetInput, GUILayout.Width(100));
                if (GUILayout.Button("Set Credits", _buttonStyle, GUILayout.Width(100)))
                {
                    if (int.TryParse(_creditSetInput, out int amount))
                    {
                        Cheats.NetworkCheats.SetCredits(amount);
                    }
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+1000", _buttonStyle)) Cheats.NetworkCheats.SetCredits(GetCurrentCredits() + 1000);
                if (GUILayout.Button("+10000", _buttonStyle)) Cheats.NetworkCheats.SetCredits(GetCurrentCredits() + 10000);
                if (GUILayout.Button("MAX", _buttonStyle)) Cheats.NetworkCheats.SetCredits(999999);
                GUILayout.EndHorizontal();
            });

            // Quota (Host)
            DrawSection("Quota (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                var quotaInfo = Cheats.NetworkCheats.GetQuotaInfo();
                _quotaInput = quotaInfo.quota.ToString();
                _quotaFulfilledInput = quotaInfo.fulfilled.ToString();

                int daysLeft = TimeOfDay.Instance != null ? Mathf.Max(0, TimeOfDay.Instance.daysUntilDeadline) : 0;

                string needText = quotaInfo.remaining >= 0
                    ? $"Need: ${quotaInfo.remaining}"
                    : $"Over by ${-quotaInfo.remaining}";
                GUILayout.Label($"Quota: ${quotaInfo.fulfilled} / ${quotaInfo.quota} ({needText})", _labelStyle);
                GUILayout.Label($"Days Left: {daysLeft}", _labelStyle);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Set 0", _buttonStyle, GUILayout.Width(60))) Cheats.NetworkCheats.SetQuota(0);
                if (GUILayout.Button("Set 100", _buttonStyle, GUILayout.Width(60))) Cheats.NetworkCheats.SetQuota(100);
                if (GUILayout.Button("Complete", _buttonStyle, GUILayout.Width(70)))
                {
                    var quota = TimeOfDay.Instance?.profitQuota ?? 100;
                    Cheats.NetworkCheats.SetQuota(quota, quota);
                }
                if (GUILayout.Button("Sell Quota", _buttonStyle, GUILayout.Width(80)))
                {
                    Cheats.NetworkCheats.SellQuota();
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Quota:", _labelStyle, GUILayout.Width(50));
                _quotaInput = GUILayout.TextField(_quotaInput, GUILayout.Width(60));
                GUILayout.Label("Fulfilled:", _labelStyle, GUILayout.Width(55));
                _quotaFulfilledInput = GUILayout.TextField(_quotaFulfilledInput, GUILayout.Width(60));
                if (GUILayout.Button("Set", _buttonStyle, GUILayout.Width(40)))
                {
                    if (int.TryParse(_quotaInput, out int q) && int.TryParse(_quotaFulfilledInput, out int f))
                    {
                        Cheats.NetworkCheats.SetQuota(q, f);
                        quotaInfo = Cheats.NetworkCheats.GetQuotaInfo();
                        _quotaInput = quotaInfo.quota.ToString();
                        _quotaFulfilledInput = quotaInfo.fulfilled.ToString();
                    }
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Chat Exploits", () =>
            {
                GUILayout.BeginHorizontal();
                _chatMessageInput = GUILayout.TextField(_chatMessageInput, GUILayout.Width(200));
                if (GUILayout.Button("Send", _buttonStyle, GUILayout.Width(50)))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SendChatMessage(_chatMessageInput);
                        // Note: Same message can be sent again by clicking again
                    }
                }
                if (GUILayout.Button("Clear", _buttonStyle, GUILayout.Width(50)))
                {
                    _chatMessageInput = "";
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("System Msg", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SendSystemMessage(_chatMessageInput);
                    }
                }
                if (GUILayout.Button("Chat Spam x10", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SpamChat(_chatMessageInput, 10);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Sys Spam x10", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SpamSystemMessage(_chatMessageInput, 10);
                    }
                }
                if (GUILayout.Button("MAX SPAM x50", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SpamChat(_chatMessageInput, 50);
                        Cheats.NetworkCheats.SpamSystemMessage(_chatMessageInput, 50);
                    }
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Ship Control (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Ship Leave", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceShipLeave();
                }
                if (GUILayout.Button("Eject All Players", _buttonStyle))
                {
                    Cheats.NetworkCheats.EjectAllPlayers();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Start", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceStartGame();
                }
                if (GUILayout.Button("Force End", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceEndGame();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Lights ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleShipLights(true);
                }
                if (GUILayout.Button("Lights OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleShipLights(false);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Magnet ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleMagnet(true);
                }
                if (GUILayout.Button("Magnet OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleMagnet(false);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Doors Open", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetShipDoors(false);
                }
                if (GUILayout.Button("Doors Close", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetShipDoors(true);
                }
                if (GUILayout.Button("OVERHEAT", _buttonStyle))
                {
                    Cheats.NetworkCheats.OverheatShipDoors();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Vehicle Control (Cruiser) (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                var vehicles = Object.FindObjectsOfType<VehicleController>();
                int vehicleCount = vehicles?.Length ?? 0;
                GUILayout.Label($"Vehicles on map: {vehicleCount}", _labelStyle);
                
                if (vehicleCount > 0)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Hijack Vehicle", _buttonStyle))
                    {
                        Cheats.NetworkCheats.HijackVehicle();
                    }
                    if (GUILayout.Button("Eject Driver", _buttonStyle))
                    {
                        Cheats.NetworkCheats.EjectVehicleDriver();
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("+5 Turbo", _buttonStyle))
                    {
                        Cheats.NetworkCheats.AddVehicleTurbo(5);
                    }
                    if (GUILayout.Button("Use Turbo", _buttonStyle))
                    {
                        Cheats.NetworkCheats.UseVehicleTurbo();
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Kill Engine", _buttonStyle))
                    {
                        Cheats.NetworkCheats.KillVehicleEngine();
                    }
                    if (GUILayout.Button("Repair Engine", _buttonStyle))
                    {
                        Cheats.NetworkCheats.RepairVehicleEngine();
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Label("Gear Control:", _labelStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Park", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ShiftVehicleGear(0);
                    }
                    if (GUILayout.Button("Drive", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ShiftVehicleGear(1);
                    }
                    if (GUILayout.Button("Reverse", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ShiftVehicleGear(2);
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Horn ON", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ToggleCarHorns(true);
                    }
                    if (GUILayout.Button("Horn OFF", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ToggleCarHorns(false);
                    }
                    if (GUILayout.Button("Explode All", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ExplodeAllVehicles();
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("  No vehicles found. Buy a Cruiser!", _labelStyle);
                }
            });

            DrawSection("Player Level (Cosmetic)", () =>
            {
                GUILayout.Label("Changes your badge - purely visual flex", _labelStyle);
                
                var levelNames = Cheats.NetworkCheats.GetLevelNames();
                int currentLevel = Cheats.NetworkCheats.GetCurrentLevelIndex();
                int currentXP = Cheats.NetworkCheats.GetCurrentXP();
                
                GUILayout.Label($"Current: {(levelNames.Length > currentLevel ? levelNames[currentLevel] : "?")} ({currentXP} XP)", _labelStyle);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("MAX LEVEL", _buttonStyle, GUILayout.Height(28)))
                {
                    Cheats.NetworkCheats.MaxOutXP();
                }
                if (GUILayout.Button("Reset (0 XP)", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(0);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+100 XP", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(currentXP + 100);
                }
                if (GUILayout.Button("+1000 XP", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(currentXP + 1000);
                }
                if (GUILayout.Button("+10000 XP", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(currentXP + 10000);
                }
                GUILayout.EndHorizontal();
            });

            // Player Management (Host)
            DrawSection("Player Management (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Revive All Players", _buttonStyle))
                {
                    Cheats.NetworkCheats.ReviveAllPlayers();
                }
                if (GUILayout.Button("Teleport All To Me", _buttonStyle))
                {
                    Cheats.NetworkCheats.TeleportAllToMe();
                }
                GUILayout.EndHorizontal();
            });

            // Facility Control (Host)
            DrawSection("Facility Control (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                GUILayout.Label("Facility Doors:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlock All", _buttonStyle))
                {
                    Cheats.NetworkCheats.UnlockAllDoors();
                }
                if (GUILayout.Button("Lock All", _buttonStyle))
                {
                    Cheats.NetworkCheats.LockAllDoors();
                }
                if (GUILayout.Button("Toggle Big Doors", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleBigDoors();
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("Noise Maker:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Noise At Me", _buttonStyle))
                {
                    Cheats.NetworkCheats.MakeNoiseAtMe();
                }
                if (GUILayout.Button("Noise At Camera", _buttonStyle))
                {
                    Cheats.NetworkCheats.MakeNoiseAtCamera();
                }
                GUILayout.EndHorizontal();
            });

            // Timescale (Host)
            DrawSection("Timescale (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                GUILayout.Label($"Game Speed: {Time.timeScale:F1}x", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("0.5x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(0.5f);
                if (GUILayout.Button("1x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(1f);
                if (GUILayout.Button("2x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(2f);
                if (GUILayout.Button("5x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(5f);
                GUILayout.EndHorizontal();
            });

            // Player Trolling (Host)
            DrawSection("Player Trolling (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                var mobPlayers = Cheats.NetworkCheats.GetAllPlayers();
                if (mobPlayers.Length > 0)
                {
                    var mobPlayerNames = mobPlayers.Select(p => p.playerUsername ?? "Unknown").ToArray();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                        _selectedMimicPlayerIndex = (_selectedMimicPlayerIndex - 1 + mobPlayerNames.Length) % mobPlayerNames.Length;
                    _selectedMimicPlayerIndex = Mathf.Clamp(_selectedMimicPlayerIndex, 0, mobPlayerNames.Length - 1);
                    GUILayout.Label(mobPlayerNames[_selectedMimicPlayerIndex], _labelStyle, GUILayout.Width(100));
                    if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                        _selectedMimicPlayerIndex = (_selectedMimicPlayerIndex + 1) % mobPlayerNames.Length;
                    if (GUILayout.Button("MOB!", _buttonStyle, GUILayout.Width(60)))
                    {
                        Cheats.NetworkCheats.MobPlayer(mobPlayers[_selectedMimicPlayerIndex], false);
                    }
                    if (GUILayout.Button("MOB+TP!", _buttonStyle, GUILayout.Width(70)))
                    {
                        Cheats.NetworkCheats.MobPlayer(mobPlayers[_selectedMimicPlayerIndex], true);
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("VOID", _buttonStyle, GUILayout.Width(60)))
                    {
                        Cheats.NetworkCheats.SendToVoid(mobPlayers[_selectedMimicPlayerIndex]);
                    }
                    if (GUILayout.Button("BOMB", _buttonStyle, GUILayout.Width(60)))
                    {
                        Cheats.NetworkCheats.BombPlayer(mobPlayers[_selectedMimicPlayerIndex]);
                    }
                    if (GUILayout.Button("LAG", _buttonStyle, GUILayout.Width(50)))
                    {
                        Cheats.NetworkCheats.LagPlayer(mobPlayers[_selectedMimicPlayerIndex]);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Label("  VOID=death | BOMB=jetpack | LAG=bracken", _labelStyle);
                    
                    GUILayout.Space(5);
                    GUILayout.Label("Spin Player:", _labelStyle);
                    GUILayout.BeginHorizontal();
                    Settings.SpinCamera = GUILayout.Toggle(Settings.SpinCamera, "Camera", _toggleStyle, GUILayout.Width(70));
                    Settings.SpinModel = GUILayout.Toggle(Settings.SpinModel, "Model", _toggleStyle, GUILayout.Width(60));
                    GUILayout.Label($"Time: {Settings.SpinDuration:F0}s", _labelStyle, GUILayout.Width(60));
                    if (GUILayout.Button("-", _buttonStyle, GUILayout.Width(25)))
                        Settings.SpinDuration = Mathf.Max(1f, Settings.SpinDuration - 5f);
                    if (GUILayout.Button("+", _buttonStyle, GUILayout.Width(25)))
                        Settings.SpinDuration = Mathf.Min(60f, Settings.SpinDuration + 5f);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("SPIN!", _buttonStyle, GUILayout.Width(60)))
                    {
                        Cheats.NetworkCheats.SpinPlayer(
                            mobPlayers[_selectedMimicPlayerIndex],
                            Settings.SpinDuration,
                            Settings.SpinCamera,
                            Settings.SpinModel);
                    }
                    if (GUILayout.Button("STOP", _buttonStyle, GUILayout.Width(50)))
                    {
                        Cheats.NetworkCheats.StopSpinPlayer(mobPlayers[_selectedMimicPlayerIndex]);
                    }
                    bool isSpinning = Cheats.NetworkCheats.IsSpinning(mobPlayers[_selectedMimicPlayerIndex]);
                    GUILayout.Label(isSpinning ? " [SPINNING]" : "", _labelStyle);
                    GUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("Spin Ship Objects", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpinShipObjects(5f);
                }
            });

            // Enemy Spawning (Host Only)
            DrawSection("Enemy Spawning (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                GUILayout.Label("Spawn Enemy:", _labelStyle);
                
                if (_cachedEnemyNames == null || _cachedEnemyNames.Length == 0)
                {
                    _cachedEnemyNames = Cheats.NetworkCheats.GetAvailableEnemyNames();
                }
                
                if (_cachedEnemyNames.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                        _selectedEnemyIndex = (_selectedEnemyIndex - 1 + _cachedEnemyNames.Length) % _cachedEnemyNames.Length;
                    _selectedEnemyIndex = Mathf.Clamp(_selectedEnemyIndex, 0, _cachedEnemyNames.Length - 1);
                    GUILayout.Label(_cachedEnemyNames[_selectedEnemyIndex], _labelStyle, GUILayout.Width(150));
                    if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                        _selectedEnemyIndex = (_selectedEnemyIndex + 1) % _cachedEnemyNames.Length;
                    if (GUILayout.Button("Refresh", _buttonStyle, GUILayout.Width(60)))
                        _cachedEnemyNames = Cheats.NetworkCheats.GetAvailableEnemyNames();
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Spawn At Me", _buttonStyle))
                    {
                        var pos = LethalMenuMod.LocalPlayer?.transform.position ?? Vector3.zero;
                        bool isOutside = _cachedEnemyNames[_selectedEnemyIndex].StartsWith("[O]");
                        Cheats.NetworkCheats.SpawnEnemy(_cachedEnemyNames[_selectedEnemyIndex], pos, isOutside);
                    }
                    if (GUILayout.Button("Spawn At Camera", _buttonStyle))
                    {
                        var pos = Camera.main?.transform.position ?? Vector3.zero;
                        bool isOutside = _cachedEnemyNames[_selectedEnemyIndex].StartsWith("[O]");
                        Cheats.NetworkCheats.SpawnEnemy(_cachedEnemyNames[_selectedEnemyIndex], pos, isOutside);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("  No enemies available (land on a moon)", _labelStyle);
                }
                
                var hostPlayers = Cheats.NetworkCheats.GetAllPlayers();
                if (hostPlayers.Length > 0 && GUILayout.Button("Spawn Mimic of Selected", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpawnMimic(hostPlayers[Mathf.Clamp(_selectedMimicPlayerIndex, 0, hostPlayers.Length - 1)]);
                }
            });

            DrawSection("Player Actions", () =>
            {
                var players = Cheats.NetworkCheats.GetAllPlayers();
                var playerNames = players.Select(p => p.playerUsername ?? "Unknown").ToArray();
                
                if (playerNames.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Target:", _labelStyle, GUILayout.Width(50));
                    _selectedPlayerIndex = Mathf.Clamp(_selectedPlayerIndex, 0, playerNames.Length - 1);
                    
                    if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                        _selectedPlayerIndex = (_selectedPlayerIndex - 1 + playerNames.Length) % playerNames.Length;
                    GUILayout.Label(playerNames[_selectedPlayerIndex], _labelStyle, GUILayout.Width(120));
                    if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                        _selectedPlayerIndex = (_selectedPlayerIndex + 1) % playerNames.Length;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Damage (10)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.DamagePlayer(players[_selectedPlayerIndex], 10);
                    }
                    if (GUILayout.Button("Damage (50)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.DamagePlayer(players[_selectedPlayerIndex], 50);
                    }
                    if (GUILayout.Button("Kill (100)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.DamagePlayer(players[_selectedPlayerIndex], 100);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Heal Player", _buttonStyle))
                    {
                        Cheats.NetworkCheats.HealPlayer(players[_selectedPlayerIndex]);
                    }
                    if (GUILayout.Button("Force Drop Items", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ForceDropItems(players[_selectedPlayerIndex]);
                    }
                    GUILayout.EndHorizontal();

                    // Teleport options
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("TP Me To Player", _buttonStyle))
                    {
                        if (LethalMenuMod.LocalPlayer != null)
                            Cheats.NetworkCheats.TeleportPlayerToPlayer(LethalMenuMod.LocalPlayer, players[_selectedPlayerIndex]);
                    }
                    if (GUILayout.Button("TP Player To Me", _buttonStyle))
                    {
                        if (LethalMenuMod.LocalPlayer != null)
                            Cheats.NetworkCheats.TeleportPlayerToPosition(players[_selectedPlayerIndex], LethalMenuMod.LocalPlayer.transform.position);
                    }
                    GUILayout.EndHorizontal();

                    // More teleport options (host only for remote players)
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("TP Random Inside", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerRandom(players[_selectedPlayerIndex], true);
                    }
                    if (GUILayout.Button("TP Random Outside", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerRandom(players[_selectedPlayerIndex], false);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("TP To Void (Death)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerToVoid(players[_selectedPlayerIndex]);
                    }
                    if (players.Length >= 2)
                    {
                        int otherIdx = (_selectedPlayerIndex + 1) % players.Length;
                        if (GUILayout.Button($"Swap w/ {players[otherIdx].playerUsername}", _buttonStyle))
                        {
                            Cheats.NetworkCheats.SwapPlayerPositions(players[_selectedPlayerIndex], players[otherIdx]);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Teleport to ship via teleporter (works on any player)
                    if (GUILayout.Button("TELEPORT TO SHIP (via TP)", _buttonStyle, GUILayout.Height(28)))
                    {
                        Cheats.NetworkCheats.TeleportPlayerViaShipTeleporter(players[_selectedPlayerIndex]);
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("TP to Main Entrance", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerToEntrance(players[_selectedPlayerIndex], true);
                    }
                    if (GUILayout.Button("TP to Fire Exit", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerToEntrance(players[_selectedPlayerIndex], false);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No other players found.", _labelStyle);
                }
            });

            DrawSection("Enemy Actions", () =>
            {
                var enemies = Cheats.NetworkCheats.GetAllEnemies();
                GUILayout.Label($"Alive enemies: {enemies.Length}", _labelStyle);

                if (GUILayout.Button("KILL ALL ENEMIES", _buttonStyle, GUILayout.Height(30)))
                {
                    Cheats.NetworkCheats.KillAllEnemies();
                }
            });

            DrawSection("Ship Unlockables", () =>
            {
                var unlockables = Cheats.NetworkCheats.GetShipUnlockables();
                var locked = unlockables.Where(u => !u.unlocked).ToArray();

                if (locked.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Unlock:", _labelStyle, GUILayout.Width(50));
                    _selectedUnlockableIndex = Mathf.Clamp(_selectedUnlockableIndex, 0, locked.Length - 1);

                    if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                        _selectedUnlockableIndex = (_selectedUnlockableIndex - 1 + locked.Length) % locked.Length;
                    GUILayout.Label(locked[_selectedUnlockableIndex].name, _labelStyle, GUILayout.Width(150));
                    if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                        _selectedUnlockableIndex = (_selectedUnlockableIndex + 1) % locked.Length;
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Unlock Selected (Free)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.UnlockShipUpgrade(locked[_selectedUnlockableIndex].id);
                    }
                }
                else
                {
                    GUILayout.Label("All unlockables obtained.", _labelStyle);
                }
            });

            DrawSection("Ship Inventory", () =>
            {
                var (scrapCount, totalItems, rawValue, adjustedValue) = CalculateShipInventory();
                var gameInstance = StartOfRound.Instance;
                float buyRate = gameInstance?.companyBuyingRate ?? 0f;

                GUILayout.Label($"Items in Ship: {totalItems} ({scrapCount} with value)", _labelStyle);
                GUILayout.Label($"Raw Scrap Value: ${rawValue}", _labelStyle);
                GUILayout.Label($"Company Buy Rate: {buyRate:P0}", _labelStyle);
                GUILayout.Label($"Sell Value: ${adjustedValue}", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, normal = { textColor = _enabledColor } });
            });

            DrawSection("Sell Items (Company Planet Only)", () =>
            {
                if (GUILayout.Button("SELL ALL ITEMS", _buttonStyle, GUILayout.Height(35)))
                {
                    SellAllItemsNaturally();
                }
                GUILayout.Label("Places items on counter, triggers sell.", _labelStyle);
            });

            // Malicious section - only show if enabled
            DrawSection("Trolling / Malicious (USE RESPONSIBLY)", () =>
            {
                GUILayout.Label("These can ruin other players' experience.", new GUIStyle(_labelStyle) { normal = { textColor = Color.red } });

                // SPAM TOGGLES - Continuous spam while enabled
                GUILayout.Label("--- Continuous Spam Toggles ---", _labelStyle);
                
                GUILayout.BeginHorizontal();
                Settings.HornSpam = DrawToggle("Horn Spam", Settings.HornSpam);
                Settings.DoorSpam = DrawToggle("Door Spam", Settings.DoorSpam);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Settings.SignalSpam = DrawToggle("Signal Spam", Settings.SignalSpam);
                Settings.RPCLagSpam = DrawToggle("RPC Lag", Settings.RPCLagSpam);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Settings.TerminalSoundSpam = DrawToggle("Terminal Spam", Settings.TerminalSoundSpam);
                Settings.TerminalEarrapeSpam = DrawToggle("EARRAPE", Settings.TerminalEarrapeSpam);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Settings.ChatSpamLoop = DrawToggle("Chat Spam", Settings.ChatSpamLoop);
                Settings.CarHornSpam = DrawToggle("Car Horns", Settings.CarHornSpam);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Settings.DeskDoorSpam = DrawToggle("Desk Door", Settings.DeskDoorSpam);
                if (GUILayout.Button("Flicker Lights", _buttonStyle))
                {
                    Cheats.NetworkCheats.FlickerShipLights();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label("--- One-Shot Actions ---", _labelStyle);

                // One-shot chaos buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("MAX CHAOS", _buttonStyle))
                {
                    Cheats.NetworkCheats.MaxChaos();
                }
                if (GUILayout.Button("Terminal Crash", _buttonStyle))
                {
                    Cheats.NetworkCheats.AttemptTerminalCrash();
                }
                GUILayout.EndHorizontal();

                // Bracken Lag Attack (the real deal)
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("BRACKEN LAG ALL", _buttonStyle))
                {
                    Cheats.NetworkCheats.BrackenLagAllPlayers();
                }
                if (GUILayout.Button("Kill All Players", _buttonStyle))
                {
                    Cheats.NetworkCheats.MassKillPlayers();
                }
                GUILayout.EndHorizontal();

                // Chat spam one-shot
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Chat Spam x50", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamChatMax(Settings.SpamMessage);
                }
                if (GUILayout.Button("Earrape x30", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamTerminalEarrape(30);
                }
                GUILayout.EndHorizontal();

                // Impersonation
                var players = Cheats.NetworkCheats.GetAllPlayers();
                if (players.Length > 0 && !string.IsNullOrEmpty(_chatMessageInput))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Impersonate:", _labelStyle, GUILayout.Width(80));
                    for (int i = 0; i < Mathf.Min(players.Length, 4); i++)
                    {
                        int idx = i;
                        if (GUILayout.Button(players[idx].playerUsername ?? $"P{idx}", _buttonStyle, GUILayout.Width(70)))
                        {
                            Cheats.NetworkCheats.ImpersonateInChat(_chatMessageInput, (int)players[idx].playerClientId);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            });

            // Signal Translator direct message
            DrawSection("Signal Translator", () =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Message (max 10):", _labelStyle, GUILayout.Width(110));
                _signalMessageInput = GUILayout.TextField(_signalMessageInput ?? "", 10, GUILayout.Width(100));
                if (GUILayout.Button("Send", _buttonStyle, GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(_signalMessageInput))
                    {
                        Cheats.NetworkCheats.SendSignalTranslatorMessage(_signalMessageInput);
                    }
                }
                GUILayout.EndHorizontal();
            });

            // Free vehicles section
            DrawSection("Free Vehicles", () =>
            {
                var vehicles = Cheats.NetworkCheats.GetAvailableVehicles();
                if (vehicles.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < vehicles.Length; i++)
                    {
                        var (id, name) = vehicles[i];
                        if (GUILayout.Button($"Buy {name}", _buttonStyle))
                        {
                            Cheats.NetworkCheats.BuyFreeVehicle(id);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No vehicles available.", _labelStyle);
                }
            });

            // More trolling features
            DrawSection("Hazard Control", () =>
            {
                // Count active hazards
                int mineCount = 0;
                int turretCount = 0;
                foreach (var mine in LethalMenuMod.Landmines)
                {
                    if (mine != null && !mine.hasExploded) mineCount++;
                }
                foreach (var turret in LethalMenuMod.Turrets)
                {
                    if (turret != null) turretCount++;
                }
                GUILayout.Label($"Landmines: {mineCount}  |  Turrets: {turretCount}", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Blow Up All Mines", _buttonStyle))
                {
                    Cheats.NetworkCheats.BlowUpAllLandmines();
                }
                if (GUILayout.Button("Mines OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllLandmines(false);
                }
                if (GUILayout.Button("Mines ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllLandmines(true);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Turrets OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllTurrets(false);
                }
                if (GUILayout.Button("Turrets ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllTurrets(true);
                }
                if (GUILayout.Button("Berserk Turrets", _buttonStyle))
                {
                    Cheats.NetworkCheats.BerserkAllTurrets();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Structure Control", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Collapse Bridge", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceBridgeFall();
                }
                if (GUILayout.Button("Collapse Small Bridge", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceSmallBridgeFall();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle Factory Lights", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleFactoryLights();
                }
                if (GUILayout.Button("Tentacle Attack", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceTentacleAttack();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Vehicle Trolling", () =>
            {
                GUILayout.BeginHorizontal();
                Settings.CarHornSpam = DrawToggle("Car Horn Spam", Settings.CarHornSpam);
                if (GUILayout.Button("Explode Vehicles", _buttonStyle))
                {
                    Cheats.NetworkCheats.ExplodeAllVehicles();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Weapon/Item Chaos", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Fire All Shotguns", _buttonStyle))
                {
                    Cheats.NetworkCheats.ShootAllShotguns();
                }
                if (GUILayout.Button("Spam Shotguns", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamShootAllShotguns(10);
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Explode Jetpacks", _buttonStyle))
                {
                    Cheats.NetworkCheats.ExplodeAllJetpacks();
                }
            });
        }

        // Terminal tab state
        private int _selectedMoonIndex = 0;
        private int _selectedBuyItemIndex = 0;
        private int _selectedUpgradeIndex = 0;
        private int _selectedDecorIndex = 0;
        private string _buyQuantity = "1";
        private Vector2 _moonScrollPos;
        private Vector2 _shopScrollPos;
        private Vector2 _upgradeScrollPos;
        private Vector2 _suitScrollPos;
        private Vector2 _decorScrollPos;
        private int _selectedSuitIndex;
        
        // All moons (bypass rotation) state
        private bool _showAllMoons = false;
        private int _selectedAllMoonIndex = 0;
        private Vector2 _allMoonsScrollPos;

        /// Checks if a scene exists in the game's build.
        private bool SceneExists(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (name == sceneName) return true;
            }
            return false;
        }

        /// Injects a moon into the terminal's moonsCatalogueList so it becomes a valid routing destination.
        /// This allows landing on moons that aren't in the current rotation.
        private void InjectMoonIntoCatalogue(Terminal terminal, SelectableLevel level)
        {
            if (terminal == null || level == null) return;
            
            // Check if already in catalogue
            if (terminal.moonsCatalogueList != null)
            {
                foreach (var moon in terminal.moonsCatalogueList)
                {
                    if (moon != null && moon.levelID == level.levelID)
                    {
                        Loader.Log($"[Terminal] Moon {level.PlanetName} already in catalogue");
                        return;
                    }
                }
            }
            
            // Create new array with the moon added
            var oldList = terminal.moonsCatalogueList ?? new SelectableLevel[0];
            var newList = new SelectableLevel[oldList.Length + 1];
            for (int i = 0; i < oldList.Length; i++)
            {
                newList[i] = oldList[i];
            }
            newList[oldList.Length] = level;
            terminal.moonsCatalogueList = newList;
            
            Loader.Log($"[Terminal] Injected {level.PlanetName} (scene={level.sceneName}) into moonsCatalogueList (now {newList.Length} moons)");
        }

        private void DrawTerminalTab()
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            var startOfRound = StartOfRound.Instance;
            var timeOfDay = TimeOfDay.Instance;

            if (terminal == null || startOfRound == null)
            {
                GUILayout.Label("Terminal not available", _labelStyle);
                return;
            }

            // Credits and dropship status
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Credits: ${terminal.groupCredits}", _headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+1k", _buttonStyle, GUILayout.Width(40)))
            {
                terminal.groupCredits += 1000;
                terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
            }
            if (GUILayout.Button("+10k", _buttonStyle, GUILayout.Width(45)))
            {
                terminal.groupCredits += 10000;
                terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
            }
            GUILayout.EndHorizontal();

            // Dropship status
            var dropship = Object.FindObjectOfType<ItemDropship>();
            string dropshipStatus;
            if (dropship == null)
            {
                // Dropship only exists when landed on a moon
                dropshipStatus = "N/A (land first)";
            }
            else if (dropship.deliveringOrder)
            {
                dropshipStatus = "Delivering...";
            }
            else if (terminal.numberOfItemsInDropship > 0)
            {
                dropshipStatus = $"{terminal.numberOfItemsInDropship} items queued";
            }
            else
            {
                dropshipStatus = "Ready";
            }
            GUILayout.Label($"Dropship: {dropshipStatus}", _labelStyle);

            GUILayout.Space(5);

            // Route to Moon section
            DrawSection("Moons", () =>
            {
                if (startOfRound.levels == null || startOfRound.levels.Length == 0)
                {
                    GUILayout.Label("No moons available", _labelStyle);
                    return;
                }

                // Current location
                string currentWeather = startOfRound.currentLevel?.currentWeather.ToString() ?? "None";
                GUILayout.Label($"Current: {startOfRound.currentLevel?.PlanetName ?? "Unknown"} ({currentWeather})", _labelStyle);

                // Moon list with weather - use moonsCatalogueList (actual routable moons)
                var moonCatalogue = terminal.moonsCatalogueList;
                _moonScrollPos = GUILayout.BeginScrollView(_moonScrollPos, GUILayout.Height(130));
                if (moonCatalogue != null && moonCatalogue.Length > 0)
                {
                    for (int i = 0; i < moonCatalogue.Length; i++)
                    {
                        var level = moonCatalogue[i];
                        if (level == null) continue;

                        string weather = level.currentWeather.ToString();
                        string weatherTag = weather != "None" ? $" [{weather}]" : "";
                        string riskTag = !string.IsNullOrEmpty(level.riskLevel) ? $" ({level.riskLevel})" : "";

                        GUILayout.BeginHorizontal();
                        bool isSelected = (i == _selectedMoonIndex);
                        if (GUILayout.Toggle(isSelected, "", GUILayout.Width(20)))
                            _selectedMoonIndex = i;
                        GUILayout.Label($"{level.PlanetName}{riskTag}{weatherTag}", _labelStyle);
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.Label("No moons available", _labelStyle);
                }
                GUILayout.EndScrollView();
                
                // Get the actual level ID for the selected moon
                int selectedLevelID = -1;
                SelectableLevel? selectedLevel = null;
                if (moonCatalogue != null && _selectedMoonIndex >= 0 && _selectedMoonIndex < moonCatalogue.Length)
                {
                    selectedLevel = moonCatalogue[_selectedMoonIndex];
                    selectedLevelID = selectedLevel?.levelID ?? -1;
                }

                // State info display
                bool isTravelling = startOfRound.travellingToNewLevel;
                bool inShipPhase = startOfRound.inShipPhase;
                bool shipHasLanded = startOfRound.shipHasLanded;
                bool canLand = inShipPhase && !isTravelling;
                bool isHost = startOfRound.IsServer || startOfRound.IsHost;
                int loadedPlayers = startOfRound.fullyLoadedPlayers?.Count ?? 0;
                int neededPlayers = startOfRound.connectedPlayersAmount + 1;
                
                // Detailed status line
                string stateInfo = $"Travel:{isTravelling} Ship:{inShipPhase} Landed:{shipHasLanded} Players:{loadedPlayers}/{neededPlayers}";
                string levelInfo = startOfRound.currentLevel != null ? $"Current:{startOfRound.currentLevel.PlanetName} Selected:{selectedLevel?.PlanetName}(id:{selectedLevelID})" : "Level:NULL";
                GUILayout.Label($"Host: {(isHost ? "Y" : "N")} | {stateInfo}", _labelStyle);
                GUILayout.Label(levelInfo, _labelStyle);

                // Route buttons
                GUILayout.BeginHorizontal();
                GUI.enabled = selectedLevelID >= 0;
                if (GUILayout.Button("Route", _buttonStyle))
                {
                    startOfRound.ChangeLevelServerRpc(selectedLevelID, terminal.groupCredits);
                    Loader.Log($"[Terminal] Routed to {selectedLevel?.PlanetName} (levelID={selectedLevelID})");
                }
                if (GUILayout.Button("Route FREE", _buttonStyle))
                {
                    startOfRound.ChangeLevelServerRpc(selectedLevelID, 999999);
                    Loader.Log($"[Terminal] Routed FREE to {selectedLevel?.PlanetName} (levelID={selectedLevelID})");
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                // Company button - find Gordion (levelID 3) in levels array
                GUILayout.BeginHorizontal();
                int companyLevelID = -1;
                for (int i = 0; i < startOfRound.levels.Length; i++)
                {
                    var lvl = startOfRound.levels[i];
                    if (lvl != null && lvl.PlanetName != null && lvl.PlanetName.ToLower().Contains("gordion"))
                    {
                        companyLevelID = i;
                        break;
                    }
                }
                if (companyLevelID >= 0)
                {
                    if (GUILayout.Button("Company", _buttonStyle))
                    {
                        startOfRound.ChangeLevelServerRpc(companyLevelID, terminal.groupCredits);
                        Loader.Log($"[Terminal] Routed to Company (levelID={companyLevelID})");
                    }
                    if (GUILayout.Button("Company FREE", _buttonStyle))
                    {
                        startOfRound.ChangeLevelServerRpc(companyLevelID, 999999);
                        Loader.Log($"[Terminal] Routed FREE to Company (levelID={companyLevelID})");
                    }
                }
                GUILayout.EndHorizontal();

                // Collapsible All Moons section (bypass rotation)
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                _showAllMoons = GUILayout.Toggle(_showAllMoons, "", GUILayout.Width(20));
                GUILayout.Label("Hidden Moons (Not in Rotation)", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, normal = { textColor = Color.yellow } });
                GUILayout.EndHorizontal();
                
                if (_showAllMoons)
                {
                    // Show ONLY levels NOT in current catalogue (hidden moons)
                    var allLevels = startOfRound.levels;
                    var hiddenMoons = new System.Collections.Generic.List<(int index, SelectableLevel level, bool sceneExists)>();
                    
                    if (allLevels != null)
                    {
                        for (int i = 0; i < allLevels.Length; i++)
                        {
                            var level = allLevels[i];
                            if (level == null) continue;
                            
                            bool inRotation = moonCatalogue?.Any(m => m != null && m.levelID == level.levelID) ?? false;
                            if (!inRotation)
                            {
                                bool sceneExists = SceneExists(level.sceneName);
                                hiddenMoons.Add((i, level, sceneExists));
                            }
                        }
                    }
                    
                    _allMoonsScrollPos = GUILayout.BeginScrollView(_allMoonsScrollPos, GUILayout.Height(150));
                    if (hiddenMoons.Count > 0)
                    {
                        for (int i = 0; i < hiddenMoons.Count; i++)
                        {
                            var (levelIndex, level, sceneExists) = hiddenMoons[i];

                            string weather = level.currentWeather.ToString();
                            string weatherTag = weather != "None" ? $" [{weather}]" : "";
                            string riskTag = !string.IsNullOrEmpty(level.riskLevel) ? $" ({level.riskLevel})" : "";
                            string sceneTag = sceneExists ? "" : " [NO SCENE]";

                            GUILayout.BeginHorizontal();
                            bool isSelected = (levelIndex == _selectedAllMoonIndex);
                            if (GUILayout.Toggle(isSelected, "", GUILayout.Width(20)))
                                _selectedAllMoonIndex = levelIndex;
                            
                            var labelStyle = new GUIStyle(_labelStyle);
                            labelStyle.normal.textColor = sceneExists ? Color.green : Color.red;
                            GUILayout.Label($"{level.PlanetName}{riskTag}{weatherTag}{sceneTag}", labelStyle);
                            GUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        GUILayout.Label("All moons are in current rotation", _labelStyle);
                    }
                    GUILayout.EndScrollView();;
                    
                    // Get selected level from all levels
                    SelectableLevel? selectedAllLevel = null;
                    int selectedAllLevelID = -1;
                    if (startOfRound.levels != null && _selectedAllMoonIndex >= 0 && _selectedAllMoonIndex < startOfRound.levels.Length)
                    {
                        selectedAllLevel = startOfRound.levels[_selectedAllMoonIndex];
                        selectedAllLevelID = _selectedAllMoonIndex; // levels array index = levelID for routing
                    }
                    
                    // Check if selected moon is in catalogue (it shouldn't be if shown here, but double check)
                    bool selectedInCatalogue = moonCatalogue?.Any(m => m != null && m.levelID == selectedAllLevelID) ?? false;
                    
                    GUILayout.BeginHorizontal();
                    GUI.enabled = selectedAllLevelID >= 0 && selectedAllLevel != null;
                    if (GUILayout.Button($"Route to {selectedAllLevel?.PlanetName ?? "?"}", _buttonStyle))
                    {
                        // Inject moon into catalogue if not present (makes scene load work)
                        if (!selectedInCatalogue && selectedAllLevel != null)
                        {
                            InjectMoonIntoCatalogue(terminal, selectedAllLevel);
                        }
                        startOfRound.ChangeLevelServerRpc(selectedAllLevelID, terminal.groupCredits);
                        Loader.Log($"[Terminal] Routed (ALL) to {selectedAllLevel?.PlanetName} (levelID={selectedAllLevelID}) [injected={!selectedInCatalogue}]");
                    }
                    if (GUILayout.Button("Route FREE", _buttonStyle))
                    {
                        // Inject moon into catalogue if not present
                        if (!selectedInCatalogue && selectedAllLevel != null)
                        {
                            InjectMoonIntoCatalogue(terminal, selectedAllLevel);
                        }
                        startOfRound.ChangeLevelServerRpc(selectedAllLevelID, 999999);
                        Loader.Log($"[Terminal] Routed FREE (ALL) to {selectedAllLevel?.PlanetName} (levelID={selectedAllLevelID}) [injected={!selectedInCatalogue}]");
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                    
                    // Scene status info
                    if (hiddenMoons.Count > 0 && selectedAllLevel != null)
                    {
                        bool selectedSceneExists = SceneExists(selectedAllLevel.sceneName);
                        if (selectedSceneExists)
                        {
                            GUILayout.Label($"* Green = Scene exists, will inject into rotation", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Italic, normal = { textColor = Color.green } });
                        }
                        else
                        {
                            GUILayout.Label($"* Red = NO SCENE - Will get stuck (DLC/unreleased)", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Italic, normal = { textColor = Color.red } });
                        }
                    }
                }
                GUILayout.Space(5);

                // Land buttons
                GUILayout.BeginHorizontal();
                
                // Normal Land - respects game state
                GUI.enabled = canLand;
                if (GUILayout.Button(canLand ? "Land" : "Land (wait...)", _buttonStyle))
                {
                    // Debug log everything
                    Loader.Log($"[Terminal] Land clicked - canLand={canLand}, inShipPhase={inShipPhase}, currentLevel={startOfRound.currentLevel?.PlanetName}, sceneName={startOfRound.currentLevel?.sceneName}");
                    
                    var lever = Object.FindObjectOfType<StartMatchLever>();
                    if (lever != null)
                    {
                        lever.singlePlayerEnabled = true;
                        lever.PlayLeverPullEffectsServerRpc(true);
                    }
                    
                    if (isHost)
                    {
                        startOfRound.StartGame();
                        Loader.Log($"[Terminal] After StartGame() - inShipPhase={startOfRound.inShipPhase}");
                    }
                    else
                    {
                        startOfRound.StartGameServerRpc();
                        Loader.Log("[Terminal] Landing requested from server");
                    }
                }
                GUI.enabled = true;
                
                // Force Land - host only, bypasses all checks
                if (isHost)
                {
                    if (GUILayout.Button("Force Land", _buttonStyle))
                    {
                        Loader.Log($"[Terminal] Force Land: Before - inShipPhase={startOfRound.inShipPhase}, travellingToNewLevel={startOfRound.travellingToNewLevel}");
                        
                        var lever = Object.FindObjectOfType<StartMatchLever>();
                        if (lever != null)
                        {
                            lever.singlePlayerEnabled = true;
                            lever.triggerScript.interactable = true;
                        }
                        
                        // Force all states to allow landing
                        startOfRound.travellingToNewLevel = false;
                        startOfRound.shipLeftAutomatically = false;
                        startOfRound.inShipPhase = true; // CRITICAL: must be true for StartGame() to work
                        
                        // Force add self to fully loaded players if missing
                        ulong myClientId = GameNetworkManager.Instance.localPlayerController.playerClientId;
                        if (startOfRound.fullyLoadedPlayers != null && !startOfRound.fullyLoadedPlayers.Contains(myClientId))
                        {
                            startOfRound.fullyLoadedPlayers.Add(myClientId);
                        }
                        
                        Loader.Log($"[Terminal] Force Land: After setup - inShipPhase={startOfRound.inShipPhase}, fullyLoadedPlayers={startOfRound.fullyLoadedPlayers?.Count}");
                        
                        // Now actually start the game
                        startOfRound.StartGame();
                        Loader.Log($"[Terminal] Force Land: After StartGame() - inShipPhase={startOfRound.inShipPhase}");
                    }
                    
                    // Skip Landing Animation button - forces shipHasLanded immediately
                    if (GUILayout.Button("Skip Anim", _buttonStyle))
                    {
                        startOfRound.shipHasLanded = true;
                        startOfRound.shipDoorsEnabled = true;
                        startOfRound.inShipPhase = false;
                        
                        var lever = Object.FindObjectOfType<StartMatchLever>();
                        if (lever != null)
                        {
                            lever.triggerScript.interactable = true;
                            lever.triggerScript.animationString = "SA_PushLeverBack";
                        }
                        
                        Loader.Log("[Terminal] Skipped landing animation - ship now landed");
                    }
                    
                    // Force all players marked as loaded - use when stuck on "Waiting for crew..."
                    if (GUILayout.Button("Force Loaded", _buttonStyle))
                    {
                        // Add all connected players to fullyLoadedPlayers
                        if (startOfRound.fullyLoadedPlayers != null)
                        {
                            startOfRound.fullyLoadedPlayers.Clear();
                            for (int i = 0; i <= startOfRound.connectedPlayersAmount; i++)
                            {
                                var player = startOfRound.allPlayerScripts[i];
                                if (player != null)
                                {
                                    startOfRound.fullyLoadedPlayers.Add(player.playerClientId);
                                }
                            }
                        }
                        Loader.Log($"[Terminal] Force marked all {startOfRound.fullyLoadedPlayers?.Count} players as loaded");
                    }
                }
                GUILayout.EndHorizontal();
                
                // Reset to orbit button - use when stuck in loading
                if (isHost)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reset to Orbit (Stuck Fix)", _buttonStyle))
                    {
                        Loader.Log("[Terminal] Resetting to orbit...");
                        
                        // Stop all coroutines that might be waiting
                        startOfRound.StopAllCoroutines();
                        
                        // Reset all ship state to orbit
                        startOfRound.inShipPhase = true;
                        startOfRound.travellingToNewLevel = false;
                        startOfRound.shipHasLanded = false;
                        startOfRound.shipIsLeaving = false;
                        startOfRound.shipLeftAutomatically = false;
                        startOfRound.shipDoorsEnabled = true;
                        
                        // Clear player loading state
                        if (startOfRound.fullyLoadedPlayers != null)
                            startOfRound.fullyLoadedPlayers.Clear();
                        
                        // Re-enable lever
                        var lever = Object.FindObjectOfType<StartMatchLever>();
                        if (lever != null)
                        {
                            lever.leverHasBeenPulled = false;
                            lever.triggerScript.interactable = true;
                            lever.singlePlayerEnabled = true;
                        }
                        
                        // Reset animations
                        if (startOfRound.shipAnimator != null)
                        {
                            startOfRound.shipAnimator.ResetTrigger("ShipLeave");
                            startOfRound.shipAnimator.ResetTrigger("OpenShip");
                        }
                        
                        // Hide loading screen
                        HUDManager.Instance.loadingText.enabled = false;
                        HUDManager.Instance.loadingDarkenScreen.enabled = false;
                        
                        Loader.Log("[Terminal] Reset complete - should be back in orbit");
                    }
                    GUILayout.EndHorizontal();
                }
            });

            // Big Store section with all purchasable items
            DrawSection("Store", () =>
            {
                // ====== CONSUMABLE ITEMS ======
                GUILayout.Label("--- Consumable Items ---", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } });
                
                if (terminal.buyableItemsList == null || terminal.buyableItemsList.Length == 0)
                {
                    GUILayout.Label("No items available", _labelStyle);
                }
                else
                {
                    _shopScrollPos = GUILayout.BeginScrollView(_shopScrollPos, GUILayout.Height(120));
                    for (int i = 0; i < terminal.buyableItemsList.Length; i++)
                    {
                        var item = terminal.buyableItemsList[i];
                        if (item == null) continue;

                        int basePrice = item.creditsWorth;
                        int price = basePrice;
                        string saleTag = "";
                        
                        if (terminal.itemSalesPercentages != null && i < terminal.itemSalesPercentages.Length)
                        {
                            int salePercent = terminal.itemSalesPercentages[i];
                            if (salePercent < 100)
                            {
                                price = (int)(basePrice * (salePercent / 100f));
                                saleTag = $" SALE {100 - salePercent}% OFF";
                            }
                        }

                        GUILayout.BeginHorizontal();
                        bool isSelected = (i == _selectedBuyItemIndex);
                        if (GUILayout.Toggle(isSelected, "", GUILayout.Width(20)))
                            _selectedBuyItemIndex = i;
                        GUILayout.Label($"{item.itemName} ${price}{saleTag}", _labelStyle);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();

                    // Buy controls for consumables
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Qty:", _labelStyle, GUILayout.Width(30));
                    _buyQuantity = GUILayout.TextField(_buyQuantity, GUILayout.Width(35));
                    
                    if (GUILayout.Button("Buy", _buttonStyle))
                    {
                        BuyItems(terminal, _selectedBuyItemIndex, false);
                    }
                    if (GUILayout.Button("Buy FREE", _buttonStyle))
                    {
                        BuyItems(terminal, _selectedBuyItemIndex, true);
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(8);

                // ====== SHIP UPGRADES (Loud horn, Signal Translator, Teleporter, Inverse Teleporter) ======
                GUILayout.Label("--- Ship Upgrades ---", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } });
                
                var unlockables = startOfRound.unlockablesList?.unlockables;
                var upgrades = new System.Collections.Generic.List<(int index, UnlockableItem item)>();
                
                // Ship upgrades are: AlwaysInStock=True AND Type=1 (not suits)
                if (unlockables != null)
                {
                    for (int i = 0; i < unlockables.Count; i++)
                    {
                        var unlock = unlockables[i];
                        if (unlock.alwaysInStock && unlock.unlockableType == 1)
                        {
                            upgrades.Add((i, unlock));
                        }
                    }
                }

                if (upgrades.Count == 0)
                {
                    GUILayout.Label("No upgrades available", _labelStyle);
                }
                else
                {
                    _upgradeScrollPos = GUILayout.BeginScrollView(_upgradeScrollPos, GUILayout.Height(80));
                    for (int i = 0; i < upgrades.Count; i++)
                    {
                        var (unlockId, item) = upgrades[i];
                        int price = item.shopSelectionNode?.itemCost ?? GetUpgradePrice(item.unlockableName);
                        string status = item.hasBeenUnlockedByPlayer ? " [OWNED]" : "";
                        string label = $"{item.unlockableName} - ${price}{status}";

                        bool isSelected = (i == _selectedUpgradeIndex);
                        if (GUILayout.Toggle(isSelected, label, _buttonStyle) && !isSelected)
                        {
                            _selectedUpgradeIndex = i;
                        }
                    }
                    GUILayout.EndScrollView();

                    if (_selectedUpgradeIndex >= 0 && _selectedUpgradeIndex < upgrades.Count)
                    {
                        var (unlockId, item) = upgrades[_selectedUpgradeIndex];
                        int price = item.shopSelectionNode?.itemCost ?? GetUpgradePrice(item.unlockableName);
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Buy", _buttonStyle))
                        {
                            BuyUnlockable(startOfRound, terminal, unlockId, price, false);
                        }
                        if (GUILayout.Button("Buy FREE", _buttonStyle))
                        {
                            BuyUnlockable(startOfRound, terminal, unlockId, price, true);
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(8);

                // ====== VEHICLES (Cruiser etc.) ======
                GUILayout.Label("--- Vehicles ---", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } });
                
                if (terminal.buyableVehicles == null || terminal.buyableVehicles.Length == 0)
                {
                    GUILayout.Label("No vehicles available", _labelStyle);
                }
                else
                {
                    for (int i = 0; i < terminal.buyableVehicles.Length; i++)
                    {
                        var vehicle = terminal.buyableVehicles[i];
                        if (vehicle == null) continue;

                        int price = vehicle.creditsWorth;
                        // Check for sale on vehicles (index is after buyableItemsList)
                        int saleIndex = (terminal.buyableItemsList?.Length ?? 0) + i;
                        string saleTag = "";
                        if (terminal.itemSalesPercentages != null && saleIndex < terminal.itemSalesPercentages.Length)
                        {
                            int salePercent = terminal.itemSalesPercentages[saleIndex];
                            if (salePercent < 100)
                            {
                                price = (int)(vehicle.creditsWorth * (salePercent / 100f));
                                saleTag = $" SALE";
                            }
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{vehicle.vehicleDisplayName} - ${price}{saleTag}", _labelStyle);
                        if (GUILayout.Button("Buy", _buttonStyle, GUILayout.Width(50)))
                        {
                            BuyVehicle(terminal, i, price, false);
                        }
                        if (GUILayout.Button("FREE", _buttonStyle, GUILayout.Width(50)))
                        {
                            BuyVehicle(terminal, i, price, true);
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(8);

                // ====== SUITS ======
                GUILayout.Label("--- Suits ---", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } });
                
                var suits = new System.Collections.Generic.List<(int index, UnlockableItem item)>();
                if (unlockables != null)
                {
                    for (int i = 0; i < unlockables.Count; i++)
                    {
                        var unlock = unlockables[i];
                        // Suits are Type=0 and have shopSelectionNode
                        if (unlock.unlockableType == 0 && unlock.shopSelectionNode != null)
                        {
                            suits.Add((i, unlock));
                        }
                    }
                }

                if (suits.Count == 0)
                {
                    GUILayout.Label("No suits available", _labelStyle);
                }
                else
                {
                    _suitScrollPos = GUILayout.BeginScrollView(_suitScrollPos, GUILayout.Height(80));
                    for (int i = 0; i < suits.Count; i++)
                    {
                        var (unlockId, item) = suits[i];
                        int price = item.shopSelectionNode?.itemCost ?? 0;
                        string status = item.hasBeenUnlockedByPlayer ? " [OWNED]" : "";
                        string label = $"{item.unlockableName} - ${price}{status}";

                        bool isSelected = (i == _selectedSuitIndex);
                        if (GUILayout.Toggle(isSelected, label, _buttonStyle) && !isSelected)
                        {
                            _selectedSuitIndex = i;
                        }
                    }
                    GUILayout.EndScrollView();

                    if (_selectedSuitIndex >= 0 && _selectedSuitIndex < suits.Count)
                    {
                        var (unlockId, item) = suits[_selectedSuitIndex];
                        int price = item.shopSelectionNode?.itemCost ?? 0;
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Buy", _buttonStyle))
                        {
                            BuyUnlockable(startOfRound, terminal, unlockId, price, false);
                        }
                        if (GUILayout.Button("Buy FREE", _buttonStyle))
                        {
                            BuyUnlockable(startOfRound, terminal, unlockId, price, true);
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(8);

                // ====== SHIP DECOR (Weekly rotating) ======
                GUILayout.Label("--- Ship Decor (Weekly) ---", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } });
                
                var decorSelection = terminal.ShipDecorSelection;
                if (decorSelection == null || decorSelection.Count == 0)
                {
                    GUILayout.Label("No decor this week", _labelStyle);
                }
                else
                {
                    _decorScrollPos = GUILayout.BeginScrollView(_decorScrollPos, GUILayout.Height(80));
                    for (int i = 0; i < decorSelection.Count; i++)
                    {
                        var node = decorSelection[i];
                        if (node == null) continue;

                        int unlockId = node.shipUnlockableID;
                        bool owned = false;
                        string itemName = node.creatureName ?? "Unknown";
                        
                        if (unlockId >= 0 && startOfRound.unlockablesList?.unlockables != null && unlockId < startOfRound.unlockablesList.unlockables.Count)
                        {
                            owned = startOfRound.unlockablesList.unlockables[unlockId].hasBeenUnlockedByPlayer;
                        }

                        string status = owned ? " [OWNED]" : "";
                        string label = $"{itemName} - ${node.itemCost}{status}";

                        bool isSelected = (i == _selectedDecorIndex);
                        if (GUILayout.Toggle(isSelected, label, _buttonStyle) && !isSelected)
                        {
                            _selectedDecorIndex = i;
                        }
                    }
                    GUILayout.EndScrollView();

                    if (_selectedDecorIndex >= 0 && _selectedDecorIndex < decorSelection.Count)
                    {
                        var node = decorSelection[_selectedDecorIndex];
                        if (node != null)
                        {
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("Buy", _buttonStyle))
                            {
                                BuyUnlockable(startOfRound, terminal, node.shipUnlockableID, node.itemCost, false);
                            }
                            if (GUILayout.Button("Buy FREE", _buttonStyle))
                            {
                                BuyUnlockable(startOfRound, terminal, node.shipUnlockableID, node.itemCost, true);
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }

                GUILayout.Space(8);

                // ====== DELIVERY OPTIONS ======
                GUILayout.Label("--- Delivery ---", new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } });
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Instant Spawn (Host)", _buttonStyle))
                {
                    InstantSpawnOrderedItems();
                }
                if (GUILayout.Button("Clear Queue", _buttonStyle))
                {
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.numberOfItemsInDropship = 0;
                    Loader.Log("[Terminal] Cleared dropship queue");
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Force Dropship (Landed)", _buttonStyle))
                {
                    ForceDropshipDeliver();
                }
            });

            // Ship controls
            DrawSection("Ship", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Leave Early", _buttonStyle))
                {
                    timeOfDay?.SetShipLeaveEarlyServerRpc();
                }
                if (GUILayout.Button("End Round", _buttonStyle))
                {
                    startOfRound.EndGameServerRpc(0);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Doors", _buttonStyle))
                    Cheats.NetworkCheats.SetShipDoors(false);
                if (GUILayout.Button("Close Doors", _buttonStyle))
                    Cheats.NetworkCheats.SetShipDoors(true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Lights ON", _buttonStyle))
                    Cheats.NetworkCheats.ToggleShipLights(true);
                if (GUILayout.Button("Lights OFF", _buttonStyle))
                    Cheats.NetworkCheats.ToggleShipLights(false);
                GUILayout.EndHorizontal();
            });
        }

        private void BuyItems(Terminal terminal, int itemIndex, bool free)
        {
            if (!int.TryParse(_buyQuantity, out int qty) || qty <= 0) qty = 1;
            if (qty > 12) qty = 12; // Server rejects > 12 items at once

            int[] items = new int[qty];
            for (int i = 0; i < qty; i++)
                items[i] = itemIndex;

            // Calculate cost
            int itemPrice = terminal.buyableItemsList[itemIndex]?.creditsWorth ?? 0;
            if (terminal.itemSalesPercentages != null && itemIndex < terminal.itemSalesPercentages.Length)
            {
                itemPrice = (int)(itemPrice * (terminal.itemSalesPercentages[itemIndex] / 100f));
            }
            int totalCost = itemPrice * qty;

            int newCredits;
            if (free)
            {
                // FREE: pass current credits (no deduction) - server accepts if newCredits <= groupCredits
                newCredits = terminal.groupCredits;
            }
            else
            {
                // Normal: deduct cost
                newCredits = terminal.groupCredits - totalCost;
                if (newCredits < 0)
                {
                    Loader.Log("[Terminal] Not enough credits");
                    return;
                }
            }

            // Call BuyItemsServerRpc - this adds items to orderedItemsFromTerminal and syncs credits
            terminal.BuyItemsServerRpc(items, newCredits, terminal.numberOfItemsInDropship);

            var itemName = terminal.buyableItemsList[itemIndex]?.itemName ?? "item";
            Loader.Log($"[Terminal] Ordered {qty}x {itemName}{(free ? " (FREE)" : $" (${totalCost})")}");
        }

        private void BuyUnlockable(StartOfRound startOfRound, Terminal terminal, int unlockableId, int itemCost, bool free)
        {
            if (unlockableId < 0 || unlockableId >= startOfRound.unlockablesList.unlockables.Count)
            {
                Loader.Log("[Terminal] Invalid unlockable ID");
                return;
            }

            var unlock = startOfRound.unlockablesList.unlockables[unlockableId];
            if (unlock.hasBeenUnlockedByPlayer)
            {
                Loader.Log($"[Terminal] {unlock.unlockableName} already owned");
                return;
            }

            int newCredits;
            if (free)
            {
                // FREE: pass current credits (no deduction)
                newCredits = terminal.groupCredits;
            }
            else
            {
                newCredits = terminal.groupCredits - itemCost;
                if (newCredits < 0)
                {
                    Loader.Log("[Terminal] Not enough credits");
                    return;
                }
            }

            // BuyShipUnlockableServerRpc validates: newGroupCredits > terminal.groupCredits => reject
            // So passing same or less credits works
            startOfRound.BuyShipUnlockableServerRpc(unlockableId, newCredits);
            Loader.Log($"[Terminal] Purchased {unlock.unlockableName}{(free ? " (FREE)" : $" (${itemCost})")}");
        }

        private void BuyVehicle(Terminal terminal, int vehicleIndex, int price, bool free)
        {
            if (terminal.buyableVehicles == null || vehicleIndex < 0 || vehicleIndex >= terminal.buyableVehicles.Length)
            {
                Loader.Log("[Terminal] Invalid vehicle index");
                return;
            }

            var vehicle = terminal.buyableVehicles[vehicleIndex];
            if (vehicle == null)
            {
                Loader.Log("[Terminal] Vehicle not found");
                return;
            }

            // Check if dropship is already delivering a vehicle
            if (terminal.vehicleInDropship)
            {
                Loader.Log("[Terminal] Dropship already has a vehicle queued");
                return;
            }

            int newCredits;
            if (free)
            {
                newCredits = terminal.groupCredits;
            }
            else
            {
                newCredits = terminal.groupCredits - price;
                if (newCredits < 0)
                {
                    Loader.Log("[Terminal] Not enough credits");
                    return;
                }
            }

            // Set vehicle order and sync
            terminal.orderedVehicleFromTerminal = vehicleIndex;
            terminal.vehicleInDropship = true;
            terminal.groupCredits = newCredits;
            
            // Sync with server
            terminal.SyncGroupCreditsServerRpc(newCredits, terminal.numberOfItemsInDropship);
            
            Loader.Log($"[Terminal] Ordered {vehicle.vehicleDisplayName}{(free ? " (FREE)" : $" (${price})")}");
        }

        private void ForceDropshipDeliver()
        {
            var dropship = Object.FindObjectOfType<ItemDropship>();
            if (dropship == null)
            {
                Loader.Log("[Terminal] Dropship not available - must be landed on a moon");
                return;
            }

            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null || terminal.orderedItemsFromTerminal == null || terminal.orderedItemsFromTerminal.Count == 0)
            {
                Loader.Log("[Terminal] No items in queue to deliver");
                return;
            }

            if (dropship.deliveringOrder)
            {
                Loader.Log("[Terminal] Dropship is already delivering");
                return;
            }

            // Method 1: Use reflection to call private LandShipOnServer
            var method = typeof(ItemDropship).GetMethod("LandShipOnServer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(dropship, null);
                Loader.Log($"[Terminal] Forced delivery of {terminal.orderedItemsFromTerminal.Count} items");
            }
            else
            {
                // Method 2: Set timer to trigger delivery on next Update tick
                var timerField = typeof(ItemDropship).GetField("shipTimer", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (timerField != null)
                {
                    timerField.SetValue(dropship, 50f); // Above the 40f threshold
                    Loader.Log("[Terminal] Triggered dropship timer for delivery");
                }
                else
                {
                    Loader.Log("[Terminal] Failed to force delivery - reflection failed");
                }
            }
        }

        private void InstantSpawnOrderedItems()
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            var startOfRound = StartOfRound.Instance;
            
            if (terminal == null || startOfRound == null)
            {
                Loader.Log("[Terminal] Cannot spawn items - no terminal or game instance");
                return;
            }

            // Check if we're the host - only host can spawn network objects
            if (!startOfRound.IsServer && !startOfRound.IsHost)
            {
                Loader.Log("[Terminal] Instant spawn requires being host");
                return;
            }

            if (terminal.orderedItemsFromTerminal == null || terminal.orderedItemsFromTerminal.Count == 0)
            {
                Loader.Log("[Terminal] No items in queue to spawn");
                return;
            }

            // Get spawn position - center of ship
            var spawnPos = startOfRound.middleOfShipNode?.position ?? startOfRound.playerSpawnPositions[0].position;
            
            int spawned = 0;
            foreach (int itemIndex in terminal.orderedItemsFromTerminal)
            {
                if (itemIndex < 0 || itemIndex >= terminal.buyableItemsList.Length) continue;
                
                var item = terminal.buyableItemsList[itemIndex];
                if (item?.spawnPrefab == null) continue;

                try
                {
                    // Randomize position slightly so items don't stack perfectly
                    var offset = new UnityEngine.Vector3(
                        UnityEngine.Random.Range(-1f, 1f),
                        0.5f,
                        UnityEngine.Random.Range(-1f, 1f)
                    );
                    
                    var obj = Object.Instantiate(item.spawnPrefab, spawnPos + offset, UnityEngine.Quaternion.identity, startOfRound.propsContainer);
                    
                    var grabbable = obj.GetComponent<GrabbableObject>();
                    if (grabbable != null)
                    {
                        grabbable.fallTime = 0f;
                    }
                    
                    var netObj = obj.GetComponent<Unity.Netcode.NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn(false);
                    }
                    
                    spawned++;
                }
                catch (System.Exception ex)
                {
                    Loader.Log($"[Terminal] Failed to spawn item {itemIndex}: {ex.Message}");
                }
            }

            // Clear the queue
            terminal.orderedItemsFromTerminal.Clear();
            terminal.numberOfItemsInDropship = 0;
            
            // Sync the cleared state
            terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, 0);
            
            Loader.Log($"[Terminal] Instantly spawned {spawned} items in ship");
        }

        private int GetCurrentCredits()
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            return terminal?.groupCredits ?? 0;
        }

        private int GetUpgradePrice(string? unlockableName)
        {
            if (string.IsNullOrEmpty(unlockableName)) return 0;
            string name = unlockableName.ToLower();
            if (name.Contains("teleporter") && name.Contains("inverse")) return 425;
            if (name.Contains("teleporter")) return 375;
            if (name.Contains("signal")) return 255;
            if (name.Contains("horn")) return 100;
            return 0;
        }

        private (int scrapCount, int totalItems, int rawValue, int adjustedValue) CalculateShipInventory()
        {
            var gameInstance = StartOfRound.Instance;
            if (gameInstance == null || gameInstance.shipBounds == null)
                return (0, 0, 0, 0);

            var shipBounds = gameInstance.shipBounds;
            var allItems = Object.FindObjectsOfType<GrabbableObject>();

            int scrapCount = 0;
            int totalItems = 0;
            int rawValue = 0;

            foreach (var item in allItems)
            {
                if (item == null) continue;
                if (item.itemProperties == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (!shipBounds.bounds.Contains(item.transform.position)) continue;

                // Only include items with scrap value > 0 (same as sell function)
                if (item.scrapValue <= 0) continue;

                totalItems++;
                scrapCount++;
                rawValue += item.scrapValue;
            }

            int adjustedValue = (int)(rawValue * gameInstance.companyBuyingRate);
            return (scrapCount, totalItems, rawValue, adjustedValue);
        }

        private void SellAllItemsNaturally()
        {
            var gameInstance = StartOfRound.Instance;
            if (gameInstance == null || gameInstance.currentLevel == null)
            {
                Debug.Log("[LethalMenu] Not in game.");
                return;
            }

            // Check if on Company planet
            if (!gameInstance.currentLevel.PlanetName.Contains("Gordion"))
            {
                Debug.Log("[LethalMenu] Not on Company planet. Go to 71-Gordion first.");
                return;
            }

            // Get all sellable items in ship
            var shipBounds = gameInstance.shipBounds;
            if (shipBounds == null)
            {
                Debug.Log("[LethalMenu] Ship bounds not found.");
                return;
            }

            var allItems = Object.FindObjectsOfType<GrabbableObject>();
            var itemsToSell = new System.Collections.Generic.List<GrabbableObject>();
            int rawValue = 0;

            foreach (var item in allItems)
            {
                if (item == null) continue;
                if (item.itemProperties == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (!shipBounds.bounds.Contains(item.transform.position)) continue;
                
                // Only include items with actual scrap value
                if (item.scrapValue <= 0) continue;

                itemsToSell.Add(item);
                rawValue += item.scrapValue;
            }

            if (itemsToSell.Count == 0)
            {
                Loader.Log("[LethalMenu] No sellable items in ship.");
                return;
            }

            // Calculate adjusted value
            int adjustedValue = (int)(rawValue * gameInstance.companyBuyingRate);

            // Build item summary first
            var itemNames = new System.Collections.Generic.Dictionary<string, (int count, int value)>();
            foreach (var item in itemsToSell)
            {
                string name = item.itemProperties?.itemName ?? "Unknown";
                if (!itemNames.ContainsKey(name))
                    itemNames[name] = (0, 0);
                var (count, value) = itemNames[name];
                itemNames[name] = (count + 1, value + item.scrapValue);
            }

            Loader.Log("=== SELL SUMMARY ===");
            foreach (var kvp in itemNames)
            {
                Loader.Log($"  {kvp.Key} x{kvp.Value.count} = ${kvp.Value.value}");
            }
            Loader.Log($"  RAW TOTAL: ${rawValue}");
            Loader.Log($"  ADJUSTED ({gameInstance.companyBuyingRate:P0}): ${adjustedValue}");

            // Apply credits and quota directly
            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Loader.Log("[LethalMenu] Terminal not found.");
                return;
            }

            int oldCredits = terminal.groupCredits;
            
            // Update credits
            terminal.groupCredits += adjustedValue;
            
            // Update quota
            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay != null)
            {
                timeOfDay.quotaFulfilled += adjustedValue;
                timeOfDay.UpdateProfitQuotaCurrentTime();
            }

            // Update game stats
            gameInstance.gameStats.scrapValueCollected += adjustedValue;

            Loader.Log($"  Credits: ${oldCredits} -> ${terminal.groupCredits}");
            Loader.Log("====================");

            // Build the item list text ourselves
            string itemListText = "";
            foreach (var kvp in itemNames)
            {
                itemListText += $"{kvp.Key} (x{kvp.Value.count}) : {kvp.Value.value} \n";
            }

            // Force the HUD to display our text directly
            var hud = HUDManager.Instance;
            if (hud != null)
            {
                hud.moneyRewardsListText.text = itemListText;
                hud.moneyRewardsTotalText.text = $"TOTAL: ${adjustedValue}";
                hud.moneyRewardsAnimator.SetTrigger("showRewards");
                hud.rewardsScrollbar.value = 1f;
            }

            // Despawn all sold items
            foreach (var item in itemsToSell)
            {
                if (item != null && item.NetworkObject != null && item.NetworkObject.IsSpawned)
                {
                    // Only server/host can despawn
                    if (NetworkManager.Singleton?.IsHost == true || NetworkManager.Singleton?.IsServer == true)
                    {
                        item.NetworkObject.Despawn(true);
                    }
                    else
                    {
                        // Non-host: just deactivate locally (items will remain for others)
                        item.gameObject.SetActive(false);
                    }
                }
            }

            Loader.Log($"[LethalMenu] SUCCESS: Sold {itemsToSell.Count} items for ${adjustedValue}");
        }

        #region Server Browser Tab
        
        private Vector2 _browserScrollPosition = Vector2.zero;
        private string _browserTagFilter = "";
        
        private void DrawBrowserTab()
        {
            // Hot-swap status (if active)
            if (ServerHotSwap.IsHotSwapping || !string.IsNullOrEmpty(ServerHotSwap.Status))
            {
                DrawSection("Hot Swap Status", () =>
                {
                    GUILayout.Label($"Status: {ServerHotSwap.Status}", _labelStyle);
                    if (ServerHotSwap.IsHotSwapping)
                    {
                        GUILayout.Label("⚠ EXPERIMENTAL - May cause issues!", _labelStyle);
                        if (GUILayout.Button("Cancel", _buttonStyle, GUILayout.Height(25)))
                        {
                            ServerHotSwap.Cancel();
                        }
                    }
                });
            }
            
            DrawSection("Server Browser", () =>
            {
                GUILayout.Label($"Status: {ServerBrowser.StatusMessage}", _labelStyle);
                
                if (ServerBrowser.LastQueryTime != System.DateTime.MinValue)
                {
                    var elapsed = System.DateTime.Now - ServerBrowser.LastQueryTime;
                    GUILayout.Label($"Last refresh: {elapsed.TotalSeconds:F0}s ago", _labelStyle);
                }
                
                GUILayout.Space(5);
                
                // Refresh button
                GUILayout.BeginHorizontal();
                GUI.enabled = !ServerBrowser.IsQuerying;
                if (GUILayout.Button(ServerBrowser.IsQuerying ? "Querying..." : "Refresh Servers", _buttonStyle, GUILayout.Height(30)))
                {
                    ServerBrowser.RefreshLobbies();
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            });

            DrawSection("Filters", () =>
            {
                // Distance filter
                GUILayout.BeginHorizontal();
                GUILayout.Label("Distance:", _labelStyle, GUILayout.Width(70));
                for (int i = 0; i < ServerBrowser.DistanceNames.Length; i++)
                {
                    bool isSelected = ServerBrowser.DistanceFilter == i;
                    if (GUILayout.Toggle(isSelected, ServerBrowser.DistanceNames[i], _buttonStyle, GUILayout.Width(75)))
                    {
                        ServerBrowser.DistanceFilter = i;
                    }
                }
                GUILayout.EndHorizontal();

                // Tag filter
                GUILayout.BeginHorizontal();
                GUILayout.Label("Tag:", _labelStyle, GUILayout.Width(70));
                _browserTagFilter = GUILayout.TextField(_browserTagFilter ?? "", _textFieldStyle, GUILayout.Width(150));
                ServerBrowser.TagFilter = _browserTagFilter;
                GUILayout.EndHorizontal();

                // Show filters
                GUILayout.BeginHorizontal();
                ServerBrowser.ShowFullLobbies = GUILayout.Toggle(ServerBrowser.ShowFullLobbies, "Full", _buttonStyle, GUILayout.Width(55));
                ServerBrowser.ShowStartedGames = GUILayout.Toggle(ServerBrowser.ShowStartedGames, "Started", _buttonStyle, GUILayout.Width(65));
                ServerBrowser.ShowIncompatible = GUILayout.Toggle(ServerBrowser.ShowIncompatible, "Incompatible", _buttonStyle, GUILayout.Width(95));
                GUILayout.EndHorizontal();
            });

            DrawSection($"Servers ({ServerBrowser.Lobbies.Count})", () =>
            {
                if (ServerBrowser.Lobbies.Count == 0)
                {
                    GUILayout.Label("No servers to display. Click 'Refresh Servers' above.", _labelStyle);
                    return;
                }

                _browserScrollPosition = GUILayout.BeginScrollView(_browserScrollPosition, GUILayout.Height(350));
                
                foreach (var lobby in ServerBrowser.Lobbies)
                {
                    DrawLobbyEntry(lobby);
                }
                
                GUILayout.EndScrollView();
            });
        }

        private void DrawLobbyEntry(ServerBrowser.LobbyInfo lobby)
        {
            // Determine background color
            Color bgColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            if (lobby.IsKickedHost)
                bgColor = new Color(0.4f, 0.1f, 0.1f, 0.9f); // Red for kicked hosts
            else if (!lobby.IsCompatible)
                bgColor = new Color(0.3f, 0.2f, 0.1f, 0.9f); // Orange for incompatible
            else if (lobby.IsStarted)
                bgColor = new Color(0.2f, 0.2f, 0.3f, 0.9f); // Blue-ish for started

            // Create a colored box background
            GUIStyle entryStyle = new GUIStyle(_boxStyle);
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, bgColor);
            bgTex.Apply();
            entryStyle.normal.background = bgTex;

            GUILayout.BeginVertical(entryStyle);
            
            // Header row with name, player count, and expand button
            GUILayout.BeginHorizontal();
            
            // Lobby name (truncated)
            string displayName = lobby.Name.Length > 25 ? lobby.Name.Substring(0, 22) + "..." : lobby.Name;
            if (lobby.IsKickedHost)
                displayName = "⚠ " + displayName;
            
            GUILayout.Label(displayName, _labelStyle, GUILayout.Width(180));
            
            // Player count
            string playerText = $"{lobby.MemberCount}/4";
            Color playerColor = lobby.MemberCount >= 4 ? Color.red : (lobby.MemberCount >= 3 ? Color.yellow : Color.green);
            GUIStyle playerStyle = new GUIStyle(_labelStyle) { normal = { textColor = playerColor } };
            GUILayout.Label(playerText, playerStyle, GUILayout.Width(35));
            
            // Tag (if not "none")
            if (!string.IsNullOrEmpty(lobby.Tag) && lobby.Tag != "none")
            {
                GUILayout.Label($"[{lobby.Tag}]", _labelStyle, GUILayout.Width(60));
            }
            
            // Expand/collapse button
            string expandText = lobby.IsExpanded ? "▼" : "►";
            if (GUILayout.Button(expandText, _buttonStyle, GUILayout.Width(25)))
            {
                lobby.IsExpanded = !lobby.IsExpanded;
            }
            
            GUILayout.EndHorizontal();

            // Expanded details
            if (lobby.IsExpanded)
            {
                GUILayout.Space(5);
                
                // Status indicators
                GUILayout.BeginHorizontal();
                if (lobby.IsChallenge)
                    GUILayout.Label("🏆 Challenge", _labelStyle, GUILayout.Width(80));
                if (lobby.IsStarted)
                    GUILayout.Label("🎮 In Progress", _labelStyle, GUILayout.Width(85));
                if (!lobby.IsJoinable)
                    GUILayout.Label("🔒 Locked", _labelStyle, GUILayout.Width(70));
                GUILayout.EndHorizontal();

                // Version info
                string versionText = $"Version: {lobby.Version}";
                if (!lobby.IsCompatible)
                    versionText += " (INCOMPATIBLE)";
                GUILayout.Label(versionText, _labelStyle);
                
                // IDs (smaller text)
                GUIStyle smallStyle = new GUIStyle(_labelStyle) { fontSize = 10 };
                GUILayout.Label($"Lobby: {lobby.LobbyIdRaw} | Host: {lobby.OwnerIdRaw}", smallStyle);

                // Warning for kicked hosts
                if (lobby.IsKickedHost)
                {
                    GUIStyle warnStyle = new GUIStyle(_labelStyle) { normal = { textColor = Color.red } };
                    GUILayout.Label("⚠ WARNING: This host kicked you before!", warnStyle);
                }

                GUILayout.Space(5);
                
                // Action buttons
                GUILayout.BeginHorizontal();
                
                // Join button (normal - goes through main menu)
                bool canJoin = lobby.IsCompatible && lobby.IsJoinable && lobby.MemberCount < 4;
                GUI.enabled = canJoin;
                if (GUILayout.Button("Join", _buttonStyle, GUILayout.Height(25), GUILayout.Width(50)))
                {
                    ServerBrowser.JoinLobby(lobby);
                }
                
                // Hot Swap button (experimental - direct switch)
                bool canHotSwap = canJoin && StartOfRound.Instance != null && !ServerHotSwap.IsHotSwapping;
                GUI.enabled = canHotSwap;
                if (GUILayout.Button("Swap", _buttonStyle, GUILayout.Height(25), GUILayout.Width(45)))
                {
                    ServerHotSwap.HotSwapTo(lobby.LobbyIdRaw, lobby.OwnerIdRaw);
                }
                GUI.enabled = true;
                
                // Copy ID button
                if (GUILayout.Button("ID", _buttonStyle, GUILayout.Height(25), GUILayout.Width(30)))
                {
                    GUIUtility.systemCopyBuffer = lobby.LobbyIdRaw.ToString();
                    HUDManager.Instance?.DisplayTip("Copied", $"Lobby ID copied to clipboard");
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndVertical();
            GUILayout.Space(3);
        }
        
        #endregion

        private void DrawSettingsTab()
        {
            DrawSection("Menu Settings", () =>
            {
                GUILayout.Label("Press INSERT to toggle menu", _labelStyle);
                GUILayout.Space(10);
                var timeOfDay = TimeOfDay.Instance;
                GUILayout.Label($"Game Time: {(timeOfDay?.normalizedTimeOfDay ?? 0):P0}", _labelStyle);
                GUILayout.Label($"Players: {LethalMenuMod.Players.Count}", _labelStyle);
                GUILayout.Label($"Local Player: {LethalMenuMod.LocalPlayer?.playerUsername ?? "None"}", _labelStyle);
            });

            DrawSection("Config", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Config", _buttonStyle, GUILayout.Height(28)))
                {
                    Settings.SaveConfig();
                }
                if (GUILayout.Button("Load Config", _buttonStyle, GUILayout.Height(28)))
                {
                    Settings.LoadConfig();
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Reset to Defaults", _buttonStyle, GUILayout.Height(28)))
                {
                    Settings.ResetConfig();
                }

                GUILayout.Label("Config saves to: %APPDATA%\\LethalMenu\\config.json", 
                    new GUIStyle(_labelStyle) { fontSize = 10, normal = { textColor = Color.gray } });
            });

            DrawSection("Debug", () =>
            {
                if (GUILayout.Button("Log Game State", _buttonStyle, GUILayout.Height(28)))
                {
                    Loader.Log($"GameInstance: {LethalMenuMod.GameInstance != null}");
                    Loader.Log($"LocalPlayer: {LethalMenuMod.LocalPlayer?.playerUsername}");
                    Loader.Log($"Players: {LethalMenuMod.Players.Count}");
                    Loader.Log($"Enemies: {LethalMenuMod.Enemies.Count}");
                    Loader.Log($"Items: {LethalMenuMod.Items.Count}");
                }
            });
        }

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

        // Actions
        private void TeleportToShip()
        {
            if (LethalMenuMod.LocalPlayer == null || LethalMenuMod.GameInstance == null) return;

            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal != null)
            {
                LethalMenuMod.LocalPlayer.TeleportPlayer(terminal.transform.position);
            }
        }

        private void TeleportTo(Vector3 position)
        {
            if (LethalMenuMod.LocalPlayer == null) return;
            LethalMenuMod.LocalPlayer.TeleportPlayer(position);
        }

        private void TeleportItemsToShip()
        {
            if (LethalMenuMod.GameInstance == null || LethalMenuMod.LocalPlayer == null) return;

            // Get player position as reference (they should be in ship when using this)
            Vector3 playerPos = LethalMenuMod.LocalPlayer.transform.position;
            float spawnHeight = playerPos.y + 1.5f; // Spawn above ground to let them fall

            // Get ship bounds for containment check
            var shipBounds = LethalMenuMod.GameInstance.shipInnerRoomBounds;
            if (shipBounds == null)
            {
                Loader.LogError("Ship bounds not found");
                return;
            }

            // Get the ship's elevator transform for proper parenting
            var elevatorTransform = LethalMenuMod.GameInstance.elevatorTransform;
            var localPlayer = LethalMenuMod.LocalPlayer;

            // Re-collect items to ensure fresh list
            var allItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            int teleported = 0;
            int totalValue = 0;

            foreach (var item in allItems)
            {
                if (item == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (item.isPocketed) continue;

                // Check if item is ACTUALLY in ship bounds
                bool actuallyInShip = shipBounds.bounds.Contains(item.transform.position);
                if (actuallyInShip) continue;

                // Set position above player to allow proper falling
                var targetPos = new Vector3(playerPos.x, spawnHeight, playerPos.z);

                // Parent to elevator so it moves with ship
                if (elevatorTransform != null)
                {
                    item.transform.SetParent(elevatorTransform, true);
                }

                // Set position and trigger proper ground detection
                item.transform.position = targetPos;
                item.startFallingPosition = item.transform.localPosition;
                item.hasHitGround = false;
                item.reachedFloorTarget = false;
                item.fallTime = 0f;

                // IMPORTANT: Use SetItemInElevator to properly track stats
                // This updates scrapCollectedInLevel and player's profitable stat
                if (!item.isInShipRoom)
                {
                    // Call SetItemInElevator which handles all the stat tracking
                    localPlayer.SetItemInElevator(true, true, item);
                    totalValue += item.scrapValue;
                }
                else
                {
                    // Already marked as in ship, just set flags
                    item.isInShipRoom = true;
                    item.isInElevator = true;
                }

                // Call FallToGround to properly land the item
                item.FallToGround(false);

                teleported++;
            }

            Loader.Log($"Teleported {teleported} items to ship (value: ${totalValue})");
        }

        private void TeleportNearbyItemsToPlayer(float radius)
        {
            if (LethalMenuMod.LocalPlayer == null || LethalMenuMod.GameInstance == null) return;

            var playerPos = LethalMenuMod.LocalPlayer.transform.position;
            float spawnHeight = playerPos.y + 1.5f;

            // Get ship bounds to check if player is in ship
            var shipBounds = LethalMenuMod.GameInstance.shipInnerRoomBounds;
            bool playerInShip = shipBounds != null && shipBounds.bounds.Contains(playerPos);

            // Get elevator transform for parenting if in ship
            var elevatorTransform = LethalMenuMod.GameInstance.elevatorTransform;

            // Re-collect items for fresh list
            var allItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            int teleported = 0;

            foreach (var item in allItems)
            {
                if (item == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (item.isPocketed) continue;

                float dist = Vector3.Distance(playerPos, item.transform.position);
                if (dist <= radius)
                {
                    // Parent to elevator if player is in ship
                    if (playerInShip && elevatorTransform != null)
                    {
                        item.transform.SetParent(elevatorTransform, true);
                        item.isInShipRoom = true;
                        item.isInElevator = true;
                    }

                    // Set position and trigger fall
                    var targetPos = new Vector3(playerPos.x, spawnHeight, playerPos.z);
                    item.transform.position = targetPos;
                    item.startFallingPosition = item.transform.localPosition;
                    item.hasHitGround = false;
                    item.reachedFloorTarget = false;
                    item.fallTime = 0f;
                    item.FallToGround(false);

                    teleported++;
                }
            }

            Loader.Log($"Teleported {teleported} nearby items to player");
        }

        private void TeleportToEntrance(bool mainEntrance)
        {
            if (LethalMenuMod.LocalPlayer == null) return;

            var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
            if (entrances == null || entrances.Length == 0) return;

            EntranceTeleport? target = null;
            foreach (var entrance in entrances)
            {
                if (entrance == null) continue;

                // entranceId 0 = main entrance, 1+ = fire exits
                if (mainEntrance && entrance.entranceId == 0)
                {
                    target = entrance;
                    break;
                }
                else if (!mainEntrance && entrance.entranceId != 0)
                {
                    target = entrance;
                    break;
                }
            }

            if (target != null)
            {
                LethalMenuMod.LocalPlayer.transform.position = target.entrancePoint?.position ?? target.transform.position;
            }
        }

        private void SetCredits()
        {
            if (_cachedTerminal == null)
            {
                _cachedTerminal = Object.FindObjectOfType<Terminal>();
            }

            if (_cachedTerminal == null) return;

            if (int.TryParse(_creditInput, out int credits))
            {
                credits = Mathf.Clamp(credits, 0, 10000000);
                _cachedTerminal.groupCredits = credits;
                
                // Try to sync with server if we're the host
                try
                {
                    _cachedTerminal.SyncGroupCreditsServerRpc(credits, _cachedTerminal.numberOfItemsInDropship);
                }
                catch
                {
                    // Not host or sync failed, local change only
                }
            }
        }

        #region Fusebox Control Helpers

        private string GetSwitchName(int index)
        {
            // Lethal Company fuseboxes typically have 5 switches controlling different areas
            string[] switchNames = new[]
            {
                "Switch 1 (Main Hall)",
                "Switch 2 (Back Rooms)",
                "Switch 3 (Storage)",
                "Switch 4 (Offices)",
                "Switch 5 (Basement)"
            };
            
            if (index >= 0 && index < switchNames.Length)
                return switchNames[index];
            return $"Switch {index + 1}";
        }

        private void ToggleSwitch(BreakerBox box, int switchIndex)
        {
            if (box == null || box.breakerSwitches == null) return;
            if (switchIndex < 0 || switchIndex >= box.breakerSwitches.Length) return;

            var switchAnimator = box.breakerSwitches[switchIndex];
            if (switchAnimator == null) return;

            var trigger = switchAnimator.gameObject.GetComponent<AnimatedObjectTrigger>();
            if (trigger == null) return;

            bool wasOn = trigger.boolValue;
            
            // Toggle the switch - TriggerAnimationNonPlayer handles animation, boolValue, audio,
            // AND fires onTriggerBool event which calls BreakerBox.SwitchBreaker automatically
            trigger.TriggerAnimationNonPlayer(false, false, false);
            
            Loader.Log($"[LethalMenu] Toggled switch {switchIndex}: {(wasOn ? "ON->OFF" : "OFF->ON")}");
        }

        private void SetAllSwitches(BreakerBox box, bool targetState)
        {
            if (box == null || box.breakerSwitches == null) return;

            for (int i = 0; i < box.breakerSwitches.Length; i++)
            {
                var switchAnimator = box.breakerSwitches[i];
                if (switchAnimator == null) continue;

                var trigger = switchAnimator.gameObject.GetComponent<AnimatedObjectTrigger>();
                if (trigger == null) continue;

                bool currentState = trigger.boolValue;
                
                // Only toggle if current state differs from target
                if (currentState != targetState)
                {
                    // TriggerAnimationNonPlayer handles everything including the BreakerBox counter update
                    trigger.TriggerAnimationNonPlayer(false, false, false);
                }
            }

            Loader.Log($"[LethalMenu] Set all switches to {(targetState ? "ON" : "OFF")}");
        }

        #endregion

        private void AddCredits(int amount)
        {
            if (_cachedTerminal == null)
            {
                _cachedTerminal = Object.FindObjectOfType<Terminal>();
            }

            if (_cachedTerminal == null) return;

            int newCredits = Mathf.Clamp(_cachedTerminal.groupCredits + amount, 0, 10000000);
            _cachedTerminal.groupCredits = newCredits;
            _creditInput = newCredits.ToString();

            // Try to sync with server if we're the host
            try
            {
                _cachedTerminal.SyncGroupCreditsServerRpc(newCredits, _cachedTerminal.numberOfItemsInDropship);
            }
            catch
            {
                // Not host or sync failed, local change only
            }
        }

        private void SpawnItem(Item item)
        {
            if (LethalMenuMod.LocalPlayer == null || item?.spawnPrefab == null) return;
            if (StartOfRound.Instance?.propsContainer == null) return;

            int value = 100;
            int.TryParse(_spawnValue, out value);

            try
            {
                var spawnPos = LethalMenuMod.LocalPlayer.gameplayCamera.transform.position + 
                               LethalMenuMod.LocalPlayer.gameplayCamera.transform.forward * 2f;

                var obj = Object.Instantiate(item.spawnPrefab, spawnPos, Quaternion.identity, StartOfRound.Instance.propsContainer);
                var grabbable = obj.GetComponent<GrabbableObject>();
                if (grabbable != null)
                {
                    grabbable.SetScrapValue(value);
                    grabbable.fallTime = 0f;
                }
                obj.GetComponent<NetworkObject>()?.Spawn();
                Loader.Log($"[LethalMenu] Spawned {item.itemName} with value {value}");
            }
            catch (System.Exception ex)
            {
                Loader.LogError($"[LethalMenu] Failed to spawn item: {ex.Message}");
            }
        }

        private string _expChatMsg = "";
        private string _expSysMsgInput = "";

        private void DrawExperimentationTab()
        {
            GUILayout.Label("WARNING: Private/protected calls. Host recommended. May desync.", _labelStyle);
            bool isHost = Cheats.NetworkCheats.IsHost();
            GUILayout.Label($"Status: {(isHost ? "HOST" : "CLIENT")}", _labelStyle);
            GUILayout.Space(4);

            // Player selector
            var players = Cheats.NetworkCheats.GetAllPlayers();
            var playerNames = players.Select(p => p.playerUsername ?? "Unknown").ToArray();
            if (playerNames.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target:", _labelStyle, GUILayout.Width(50));
                _selectedPlayerIndex = Mathf.Clamp(_selectedPlayerIndex, 0, playerNames.Length - 1);
                if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                    _selectedPlayerIndex = (_selectedPlayerIndex - 1 + playerNames.Length) % playerNames.Length;
                GUILayout.Label(playerNames[_selectedPlayerIndex], _labelStyle, GUILayout.Width(120));
                if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                    _selectedPlayerIndex = (_selectedPlayerIndex + 1) % playerNames.Length;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);

            // ========== CATEGORY A: LOCAL-ONLY PRIVATE ==========
            GUILayout.Label("A: Local-Only Private (No RPC, may desync):", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Detonate Landmines (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalDetonateLandmines();
            }
            if (GUILayout.Button("Turrets Berserk (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalTurretsBerserk();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Turret Mode: {_expTurretMode}", _labelStyle, GUILayout.Width(130));
            _expTurretMode = Mathf.RoundToInt(GUILayout.HorizontalSlider(_expTurretMode, 0, 3, GUILayout.Width(120)));
            if (GUILayout.Button("Apply Mode", _buttonStyle, GUILayout.Width(140)))
            {
                Cheats.NetworkCheats.ExperimentalSetTurretMode(_expTurretMode);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Turrets OFF (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalToggleTurretsLocal(false);
            }
            if (GUILayout.Button("Turrets ON (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalToggleTurretsLocal(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mines OFF (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalToggleLandminesLocal(false);
            }
            if (GUILayout.Button("Mines ON (private)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalToggleLandminesLocal(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mine Animation (local)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalPlayMineAnimation();
            }
            if (playerNames.Length > 0 && GUILayout.Button("Clear Target Hand (local)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalClearHeldItemLocal(players[_selectedPlayerIndex]);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ========== CATEGORY B: PRIVATE SERVERRPC ==========
            GUILayout.Label("B: Private ServerRpc (May require ownership):", _labelStyle);
            if (playerNames.Length > 0)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Private Kill RPC", _buttonStyle, GUILayout.Width(130)))
                {
                    Cheats.NetworkCheats.ExperimentalKillPlayerPrivate(players[_selectedPlayerIndex]);
                }
                if (GUILayout.Button("TP Target -> Void", _buttonStyle, GUILayout.Width(130)))
                {
                    Cheats.NetworkCheats.TeleportPlayerToVoid(players[_selectedPlayerIndex]);
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Force Drop Items (private RPC)", _buttonStyle))
                {
                    Cheats.NetworkCheats.ExperimentalDespawnHeldItem(players[_selectedPlayerIndex]);
                }
            }

            // Chat impersonation
            GUILayout.BeginHorizontal();
            _expChatMsg = GUILayout.TextField(_expChatMsg, GUILayout.Width(180));
            if (GUILayout.Button("Chat as Target (private)", _buttonStyle, GUILayout.Width(160)) && playerNames.Length > 0)
            {
                Cheats.NetworkCheats.ExperimentalChatAsPlayer(_expChatMsg, _selectedPlayerIndex);
            }
            GUILayout.EndHorizontal();

            // System message
            GUILayout.BeginHorizontal();
            _expSysMsgInput = GUILayout.TextField(_expSysMsgInput, GUILayout.Width(180));
            if (GUILayout.Button("System Msg (private)", _buttonStyle, GUILayout.Width(160)))
            {
                Cheats.NetworkCheats.ExperimentalSystemMessage(_expSysMsgInput);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ========== CATEGORY C: HOST-ONLY ==========
            GUILayout.Label("C: Game Flow (public RPCs already in main menu):", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Game (public RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalStartGame();
            }
            if (GUILayout.Button("End Game (public RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalEndGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Level ID:", _labelStyle, GUILayout.Width(70));
            _expLevelInput = GUILayout.TextField(_expLevelInput, GUILayout.Width(60));
            if (GUILayout.Button("Change Level (host)", _buttonStyle))
            {
                if (int.TryParse(_expLevelInput, out int levelId))
                {
                    Cheats.NetworkCheats.ExperimentalChangeLevel(levelId);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Unlock ID:", _labelStyle, GUILayout.Width(70));
            _expUnlockableInput = GUILayout.TextField(_expUnlockableInput, GUILayout.Width(60));
            if (GUILayout.Button("Spawn Unlockable (host)", _buttonStyle))
            {
                if (int.TryParse(_expUnlockableInput, out int unlockId))
                {
                    Cheats.NetworkCheats.ExperimentalSpawnUnlockable(unlockId);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Enemy:", _labelStyle, GUILayout.Width(50));
            _expSpawnEnemyName = GUILayout.TextField(_expSpawnEnemyName, GUILayout.Width(140));
            if (GUILayout.Button("Spawn Enemy (host)", _buttonStyle))
            {
                var local = LethalMenuMod.LocalPlayer;
                if (local != null)
                {
                    Cheats.NetworkCheats.SpawnEnemy(_expSpawnEnemyName, local.transform.position);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ========== CATEGORY D: PUBLIC RPC RequireOwnership=false ==========
            GUILayout.Label("D: Public RPC (RequireOwnership=false):", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Explode Mines (RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExplodeMinesViaRpc();
            }
            if (GUILayout.Button("Turrets Berserk (RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.TurretsBerserkViaRpc();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Turrets OFF (RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ToggleTurretsViaRpc(false);
            }
            if (GUILayout.Button("Turrets ON (RPC)", _buttonStyle))
            {
                Cheats.NetworkCheats.ToggleTurretsViaRpc(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ========== CATEGORY E: RPC EXEC STAGE SPOOF ==========
            GUILayout.Label("E: RPC Exec Stage (Advanced):", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set exec_stage=Execute (self)", _buttonStyle))
            {
                if (LethalMenuMod.LocalPlayer != null)
                {
                    Cheats.NetworkCheats.ExperimentalForceRpcExecStageExecute(LethalMenuMod.LocalPlayer);
                }
            }
            if (playerNames.Length > 0 && GUILayout.Button("Set exec_stage=Execute (target)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalForceRpcExecStageExecute(players[_selectedPlayerIndex]);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset exec_stage (self)", _buttonStyle))
            {
                if (LethalMenuMod.LocalPlayer != null)
                {
                    Cheats.NetworkCheats.ExperimentalResetRpcExecStage(LethalMenuMod.LocalPlayer);
                }
            }
            if (playerNames.Length > 0 && GUILayout.Button("Reset exec_stage (target)", _buttonStyle))
            {
                Cheats.NetworkCheats.ExperimentalResetRpcExecStage(players[_selectedPlayerIndex]);
            }
            GUILayout.EndHorizontal();
        }
    }
}
