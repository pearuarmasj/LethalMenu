using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
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

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Moon Manager", _buttonStyle, GUILayout.Height(28)))
                _moonManager.IsOpen = !_moonManager.IsOpen;
            if (GUILayout.Button("Suit Manager", _buttonStyle, GUILayout.Height(28)))
                _suitManager.IsOpen = !_suitManager.IsOpen;
            if (GUILayout.Button("Unlockables", _buttonStyle, GUILayout.Height(28)))
                _unlockablesManager.IsOpen = !_unlockablesManager.IsOpen;
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

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

            // Dropship status (items + vehicles)
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
            else
            {
                // Build combined status for items and vehicles
                int itemCount = terminal.numberOfItemsInDropship;
                bool hasVehicle = terminal.vehicleInDropship;

                // Get vehicle name if one is queued
                string? vehicleName = null;
                if (hasVehicle && terminal.buyableVehicles != null
                    && terminal.orderedVehicleFromTerminal >= 0
                    && terminal.orderedVehicleFromTerminal < terminal.buyableVehicles.Length)
                {
                    vehicleName = terminal.buyableVehicles[terminal.orderedVehicleFromTerminal]?.vehicleDisplayName ?? "Cruiser";
                }
                else if (hasVehicle)
                {
                    vehicleName = "Cruiser";
                }

                if (itemCount > 0 && vehicleName != null)
                {
                    dropshipStatus = $"{itemCount} items + {vehicleName} queued";
                }
                else if (itemCount > 0)
                {
                    dropshipStatus = $"{itemCount} items queued";
                }
                else if (vehicleName != null)
                {
                    dropshipStatus = $"{vehicleName} queued";
                }
                else
                {
                    dropshipStatus = "Ready";
                }
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
                    GUILayout.EndScrollView();

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
                    // Clear items
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.numberOfItemsInDropship = 0;
                    // Clear vehicle
                    terminal.vehicleInDropship = false;
                    terminal.orderedVehicleFromTerminal = -1;
                    Loader.Log("[Terminal] Cleared dropship queue (items + vehicle)");
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

            // Use proper BuyVehicleServerRpc - this sets vehicleInDropship, orderedVehicleFromTerminal, and syncs credits
            terminal.BuyVehicleServerRpc(vehicleIndex, newCredits, false);

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
            if (terminal == null)
            {
                Loader.Log("[Terminal] Terminal not found");
                return;
            }

            // Check for items OR vehicle queued
            int itemCount = terminal.orderedItemsFromTerminal?.Count ?? 0;
            bool hasVehicle = terminal.vehicleInDropship;

            if (itemCount == 0 && !hasVehicle)
            {
                Loader.Log("[Terminal] Nothing in queue to deliver (no items or vehicle)");
                return;
            }

            if (dropship.deliveringOrder)
            {
                Loader.Log("[Terminal] Dropship is already delivering");
                return;
            }

            // Build delivery message
            string deliveryMsg;
            if (itemCount > 0 && hasVehicle)
                deliveryMsg = $"{itemCount} items + vehicle";
            else if (itemCount > 0)
                deliveryMsg = $"{itemCount} items";
            else
                deliveryMsg = "vehicle";

            // Method 1: Use reflection to call private LandShipOnServer
            var method = typeof(ItemDropship).GetMethod("LandShipOnServer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(dropship, null);
                Loader.Log($"[Terminal] Forced delivery of {deliveryMsg}");
            }
            else
            {
                // Method 2: Set timer to trigger delivery on next Update tick
                var timerField = typeof(ItemDropship).GetField("shipTimer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (timerField != null)
                {
                    timerField.SetValue(dropship, 50f); // Above the 40f threshold
                    Loader.Log($"[Terminal] Triggered dropship timer for {deliveryMsg}");
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
    }
}
