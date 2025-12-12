using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
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
