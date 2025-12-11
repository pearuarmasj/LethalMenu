using System;
using System.Collections.Generic;
using System.Text;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace LethalMenu
{
    /// Server browser for querying and joining Steam lobbies.
    /// NOTE: Can only query from main menu - querying while in a game causes native crashes.
    public static class ServerBrowser
    {
        #region Lobby Info Class

        /// Contains all queryable data about a Steam lobby.
        /// Only primitive types stored to avoid native memory issues.
        public class LobbyInfo
        {
            /// Raw Steam lobby ID
            public ulong LobbyIdRaw { get; set; }
            
            /// Steam ID of the lobby owner/host
            public ulong OwnerIdRaw { get; set; }
            
            /// Display name of the lobby
            public string Name { get; set; } = "";
            
            /// Game version the lobby is running
            public string Version { get; set; } = "";
            
            /// Custom tag set by the host
            public string Tag { get; set; } = "none";
            
            /// Current number of players in the lobby
            public int MemberCount { get; set; }
            
            /// Maximum players allowed (always 4 for Lethal Company)
            public int MaxMembers { get; set; } = 4;
            
            /// Whether this is a challenge moon lobby
            public bool IsChallenge { get; set; }
            
            /// Whether the game has already started
            public bool IsStarted { get; set; }
            
            /// Whether the lobby is accepting new players
            public bool IsJoinable { get; set; } = true;
            
            /// Whether this host previously kicked you
            public bool IsKickedHost { get; set; }
            
            /// Whether the version matches your game version
            public bool IsCompatible { get; set; }
            
            /// UI state - whether details are expanded
            public bool IsExpanded { get; set; }

            /// Get a formatted player count string.
            public string PlayerCountString => $"{MemberCount}/{MaxMembers}";

            /// Get a status string for the lobby.
            public string StatusString
            {
                get
                {
                    try
                    {
                        var parts = new List<string>();
                        if (IsStarted) parts.Add("In Progress");
                        if (!IsJoinable) parts.Add("Closed");
                        if (IsChallenge) parts.Add("Challenge");
                        if (!IsCompatible) parts.Add("Wrong Version");
                        if (IsKickedHost) parts.Add("Kicked You Before");
                        return parts.Count > 0 ? string.Join(", ", parts) : "Open";
                    }
                    catch
                    {
                        return "Unknown";
                    }
                }
            }
        }

        #endregion

        #region State

        /// Lock object for thread-safe list access
        private static readonly object _lobbyLock = new object();

        /// Cached list of lobbies from last query
        private static List<LobbyInfo> _lobbies = new List<LobbyInfo>();
        
        /// Public access to lobby list - returns a COPY for thread safety
        public static List<LobbyInfo> Lobbies
        {
            get
            {
                try
                {
                    lock (_lobbyLock)
                    {
                        return new List<LobbyInfo>(_lobbies);
                    }
                }
                catch
                {
                    return new List<LobbyInfo>();
                }
            }
        }

        /// Whether a query is currently in progress
        public static bool IsQuerying { get; private set; }
        
        /// Status message to display in UI
        public static string StatusMessage { get; private set; } = "Click Refresh to load servers";
        
        /// When the last query was performed
        public static DateTime LastQueryTime { get; private set; } = DateTime.MinValue;

        /// Whether we're currently in a game (can't query).
        /// Defensive - returns true (blocking) on any error.
        public static bool IsInGame
        {
            get
            {
                try
                {
                    var gnm = GameNetworkManager.Instance;
                    if (gnm == null) return false;
                    if (gnm.currentLobby == null) return false;
                    return gnm.currentLobby.HasValue;
                }
                catch
                {
                    // On error, assume we're in game to be safe
                    return true;
                }
            }
        }

        #endregion

        #region Filter Settings

        /// Distance filter: 0=Close, 1=Far, 2=Worldwide
        public static int DistanceFilter { get; set; } = 2;
        
        /// Whether to show full lobbies in results
        public static bool ShowFullLobbies { get; set; } = true;
        
        /// Whether to show version-incompatible lobbies
        public static bool ShowIncompatible { get; set; } = false;
        
        /// Whether to show lobbies where game has started
        public static bool ShowStartedGames { get; set; } = false;
        
        /// Filter by specific tag (empty = show all)
        public static string TagFilter { get; set; } = "";

        /// Minimum seconds between refreshes
        private const float MinRefreshInterval = 3f;
        
        /// Maximum lobbies to request
        private const int MaxResults = 50;

        /// Display names for distance filter options
        public static readonly string[] DistanceNames = { "Close", "Far", "Worldwide" };

        #endregion

        #region Query Methods

        /// Refresh the lobby list from Steam.
        /// Only works from main menu - crashes if called while in a game.
        public static async void RefreshLobbies()
        {
            // CRITICAL: Block queries while in a game to prevent native crashes
            if (IsInGame)
            {
                StatusMessage = "Cannot query while in game.\nReturn to main menu first.";
                Debug.Log("[ServerBrowser] Query blocked - currently in a lobby");
                return;
            }

            if (IsQuerying)
            {
                Debug.Log("[ServerBrowser] Query already in progress");
                return;
            }

            // Rate limiting
            try
            {
                float elapsed = (float)(DateTime.Now - LastQueryTime).TotalSeconds;
                if (elapsed < MinRefreshInterval && LastQueryTime != DateTime.MinValue)
                {
                    StatusMessage = $"Please wait {MinRefreshInterval - elapsed:F0}s...";
                    return;
                }
            }
            catch
            {
                // If DateTime math fails somehow, just proceed
            }

            IsQuerying = true;
            StatusMessage = "Querying Steam servers...";
            
            // Clear list safely
            lock (_lobbyLock)
            {
                _lobbies.Clear();
            }

            try
            {
                // Verify game state
                var gnm = GameNetworkManager.Instance;
                if (gnm == null)
                {
                    StatusMessage = "Error: Not connected to Steam";
                    return;
                }

                string gameVersion;
                try
                {
                    gameVersion = gnm.gameVersionNum.ToString();
                }
                catch
                {
                    StatusMessage = "Error: Could not get game version";
                    return;
                }

                Debug.Log($"[ServerBrowser] Starting query for version {gameVersion}");

                // Build query matching the game's pattern
                LobbyQuery query;
                try
                {
                    query = SteamMatchmaking.LobbyList
                        .WithMaxResults(MaxResults)
                        .WithSlotsAvailable(1)
                        .WithKeyValue("vers", gameVersion);

                    // Apply distance filter
                    switch (DistanceFilter)
                    {
                        case 0:
                            query = query.FilterDistanceClose();
                            break;
                        case 1:
                            query = query.FilterDistanceFar();
                            break;
                        default:
                            query = query.FilterDistanceWorldwide();
                            break;
                    }

                    // Apply tag filter - defensive null/empty check
                    string tag = TagFilter;
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        query = query.WithKeyValue("tag", tag.ToLower().Trim());
                    }
                    else
                    {
                        query = query.WithKeyValue("tag", "none");
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = "Error building query";
                    Debug.LogError($"[ServerBrowser] Query build error: {ex}");
                    return;
                }

                // Execute query
                Debug.Log("[ServerBrowser] Sending query to Steam...");
                Lobby[]? lobbies = null;
                
                try
                {
                    lobbies = await query.RequestAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = "Steam query failed";
                    Debug.LogError($"[ServerBrowser] RequestAsync error: {ex}");
                    return;
                }

                // CRITICAL: Check game state AGAIN after async wait
                if (GameNetworkManager.Instance == null)
                {
                    StatusMessage = "Query aborted - game closing";
                    Debug.Log("[ServerBrowser] GameNetworkManager null after await");
                    return;
                }

                // CRITICAL: Check if we joined a game during the query
                if (IsInGame)
                {
                    StatusMessage = "Query aborted - joined a game";
                    Debug.Log("[ServerBrowser] Joined game during query");
                    return;
                }

                // Process results
                if (lobbies == null || lobbies.Length == 0)
                {
                    StatusMessage = "No servers found matching filters";
                    Debug.Log("[ServerBrowser] No lobbies returned");
                }
                else
                {
                    Debug.Log($"[ServerBrowser] Received {lobbies.Length} lobbies");
                    ProcessLobbyResults(lobbies, gameVersion);
                    
                    int count;
                    lock (_lobbyLock)
                    {
                        count = _lobbies.Count;
                    }
                    StatusMessage = $"Found {count} server(s)";
                }

                LastQueryTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Query failed: {ex.Message}";
                Debug.LogError($"[ServerBrowser] Query exception: {ex}");
            }
            finally
            {
                IsQuerying = false;
            }
        }

        /// Process raw lobby results into LobbyInfo objects.
        private static void ProcessLobbyResults(Lobby[] lobbies, string gameVersion)
        {
            if (lobbies == null) return;

            var tempList = new List<LobbyInfo>();

            foreach (var lobby in lobbies)
            {
                try
                {
                    var info = ExtractLobbyInfo(lobby, gameVersion);
                    if (info == null) continue;

                    // Apply local filters
                    if (!ShowFullLobbies && info.MemberCount >= info.MaxMembers) continue;
                    if (!ShowIncompatible && !info.IsCompatible) continue;
                    if (!ShowStartedGames && info.IsStarted) continue;

                    tempList.Add(info);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ServerBrowser] Error processing lobby: {ex.Message}");
                }
            }

            // Sort: compatible first, then by player count
            try
            {
                tempList.Sort((a, b) =>
                {
                    if (a == null || b == null) return 0;
                    if (a.IsCompatible != b.IsCompatible)
                        return b.IsCompatible.CompareTo(a.IsCompatible);
                    return b.MemberCount.CompareTo(a.MemberCount);
                });
            }
            catch
            {
                // Sorting failed, just use unsorted list
            }

            // Update the main list safely
            lock (_lobbyLock)
            {
                _lobbies = tempList;
            }
        }

        /// Extract all data from a Lobby struct immediately.
        /// Each property access is wrapped in try-catch for safety.
        private static LobbyInfo? ExtractLobbyInfo(Lobby lobby, string gameVersion)
        {
            // Get ID first - if this fails, lobby is completely invalid
            ulong lobbyId;
            try { lobbyId = lobby.Id; }
            catch { return null; }
            
            if (lobbyId == 0) return null;

            // Extract all properties with individual error handling
            ulong ownerId = 0;
            try { ownerId = lobby.Owner.Id; } catch { }

            string name = "";
            try { name = lobby.GetData("name") ?? ""; } catch { }
            if (string.IsNullOrEmpty(name)) return null; // Skip nameless lobbies

            string version = "";
            try { version = lobby.GetData("vers") ?? ""; } catch { }

            string tag = "none";
            try { tag = lobby.GetData("tag") ?? "none"; } catch { }
            if (string.IsNullOrEmpty(tag)) tag = "none";

            int memberCount = 0;
            try { memberCount = lobby.MemberCount; } catch { }

            bool isChallenge = false;
            try { isChallenge = lobby.GetData("chal") == "t"; } catch { }

            bool isStarted = false;
            try { isStarted = lobby.GetData("started") == "1"; } catch { }

            bool isJoinable = true;
            try { isJoinable = lobby.GetData("joinable") != "false"; } catch { }

            // Check kicked hosts safely
            bool isKickedHost = false;
            try
            {
                if (Settings.KickedHostIds != null)
                {
                    isKickedHost = Settings.KickedHostIds.Contains(ownerId);
                }
            }
            catch { }

            return new LobbyInfo
            {
                LobbyIdRaw = lobbyId,
                OwnerIdRaw = ownerId,
                Name = name,
                Version = version ?? "",
                Tag = tag,
                MemberCount = memberCount,
                IsChallenge = isChallenge,
                IsStarted = isStarted,
                IsJoinable = isJoinable,
                IsKickedHost = isKickedHost,
                IsCompatible = !string.IsNullOrEmpty(version) && version == gameVersion
            };
        }

        #endregion

        #region Join Methods

        /// Join a lobby from the browser.
        public static void JoinLobby(LobbyInfo lobbyInfo)
        {
            if (lobbyInfo == null)
            {
                StatusMessage = "Error: No lobby selected";
                return;
            }

            var gnm = GameNetworkManager.Instance;
            if (gnm == null)
            {
                StatusMessage = "Error: Not connected to Steam";
                return;
            }

            try
            {
                // Leave current lobby if in one
                try
                {
                    if (gnm.currentLobby != null && gnm.currentLobby.HasValue)
                    {
                        Debug.Log("[ServerBrowser] Leaving current lobby before joining new one");
                        gnm.LeaveCurrentSteamLobby();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ServerBrowser] Error leaving lobby: {ex.Message}");
                }

                // Reconstruct Lobby struct from ID and join
                var lobby = new Lobby(lobbyInfo.LobbyIdRaw);
                var steamId = new SteamId { Value = lobbyInfo.LobbyIdRaw };

                string safeName = lobbyInfo.Name ?? "Unknown";
                StatusMessage = $"Joining {safeName}...";
                Debug.Log($"[ServerBrowser] Joining lobby {lobbyInfo.LobbyIdRaw} ({safeName})");
                
                LobbySlot.JoinLobbyAfterVerifying(lobby, steamId);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Join failed: {ex.Message}";
                Debug.LogError($"[ServerBrowser] Join error: {ex}");
            }
        }

        #endregion

        #region Utility Methods

        /// Get formatted details string for a lobby.
        public static string GetLobbyDetails(LobbyInfo lobby)
        {
            if (lobby == null) return "No lobby selected";

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Name: {lobby.Name ?? "Unknown"}");
                sb.AppendLine($"Players: {lobby.PlayerCountString}");
                sb.AppendLine($"Version: {lobby.Version ?? "?"} {(lobby.IsCompatible ? "✓" : "✗ INCOMPATIBLE")}");
                sb.AppendLine($"Tag: {lobby.Tag ?? "none"}");
                sb.AppendLine($"Status: {lobby.StatusString}");
                sb.AppendLine();
                sb.AppendLine($"Lobby ID: {lobby.LobbyIdRaw}");
                sb.AppendLine($"Host ID: {lobby.OwnerIdRaw}");
                
                if (lobby.IsKickedHost)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠ WARNING: This host kicked you before!");
                }

                return sb.ToString();
            }
            catch
            {
                return "Error getting lobby details";
            }
        }

        /// 
        /// Clear the cached lobby list.
        /// 
        public static void ClearLobbies()
        {
            try
            {
                lock (_lobbyLock)
                {
                    _lobbies.Clear();
                }
                StatusMessage = "Lobby list cleared";
            }
            catch
            {
                StatusMessage = "Error clearing list";
            }
        }

        /// Get count of lobbies matching current filters.
        public static int GetFilteredCount()
        {
            try
            {
                lock (_lobbyLock)
                {
                    return _lobbies.Count;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// Check if a specific lobby ID is in our cached list.
        public static bool HasLobby(ulong lobbyId)
        {
            try
            {
                lock (_lobbyLock)
                {
                    foreach (var lobby in _lobbies)
                    {
                        if (lobby != null && lobby.LobbyIdRaw == lobbyId) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        /// Get a lobby by its ID from the cached list.
        public static LobbyInfo? GetLobbyById(ulong lobbyId)
        {
            try
            {
                lock (_lobbyLock)
                {
                    foreach (var lobby in _lobbies)
                    {
                        if (lobby != null && lobby.LobbyIdRaw == lobbyId) return lobby;
                    }
                }
            }
            catch { }
            return null;
        }

        #endregion
    }
}
