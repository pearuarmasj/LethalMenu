using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Server Browser Tab

        private Vector2 _browserScrollPosition = Vector2.zero;
        private string _browserTagFilter = "";
        private Texture2D? _browserDefaultBg;
        private Texture2D? _browserKickedBg;
        private Texture2D? _browserIncompatibleBg;
        private Texture2D? _browserStartedBg;

        private void EnsureBrowserTextures()
        {
            _browserDefaultBg ??= MakeTexture(1, 1, new Color(0.15f, 0.15f, 0.15f, 0.9f));
            _browserKickedBg ??= MakeTexture(1, 1, new Color(0.4f, 0.1f, 0.1f, 0.9f));
            _browserIncompatibleBg ??= MakeTexture(1, 1, new Color(0.3f, 0.2f, 0.1f, 0.9f));
            _browserStartedBg ??= MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.3f, 0.9f));
        }

        private void DrawBrowserTab()
        {
            EnsureBrowserTextures();
            var lobbies = ServerBrowser.Lobbies;

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
                bool canRefresh = !ServerBrowser.IsQuerying && ServerBrowser.CanRefreshLobbies;
                GUI.enabled = canRefresh;
                string refreshText = ServerBrowser.IsQuerying ? "Querying..." : "Refresh Servers";
                if (GUILayout.Button(refreshText, _buttonStyle, GUILayout.Height(30)))
                {
                    ServerBrowser.RefreshLobbies();
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                if (!ServerBrowser.CanRefreshLobbies)
                    GUILayout.Label(ServerBrowser.QueryBlockReason, _tooltipStyle);
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

            DrawSection($"Servers ({lobbies.Count})", () =>
            {
                if (lobbies.Count == 0)
                {
                    GUILayout.Label("No servers to display. Click 'Refresh Servers' above.", _labelStyle);
                    return;
                }

                _browserScrollPosition = GUILayout.BeginScrollView(_browserScrollPosition, GUILayout.Height(350));

                foreach (var lobby in lobbies)
                {
                    DrawLobbyEntry(lobby);
                }

                GUILayout.EndScrollView();
            });
        }

        private void DrawLobbyEntry(ServerBrowser.LobbyInfo lobby)
        {
            // Determine background color
            Texture2D? bgTex = _browserDefaultBg;
            if (lobby.IsKickedHost)
                bgTex = _browserKickedBg; // Red for kicked hosts
            else if (!lobby.IsCompatible)
                bgTex = _browserIncompatibleBg; // Orange for incompatible
            else if (lobby.IsStarted)
                bgTex = _browserStartedBg; // Blue-ish for started

            GUIStyle entryStyle = new GUIStyle(_boxStyle);
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
                bool canJoin = lobby.IsCompatible && lobby.IsJoinable && lobby.MemberCount < 4 && !ServerBrowser.IsInActiveGame;
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
                    HUDManager.Instance?.DisplayTip("Copied", "Lobby ID copied to clipboard");
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        #endregion
    }
}
