param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$src = Join-Path $Root "src/LethalMenu"
$modPath = Join-Path $src "LethalMenuMod.cs"
$extensionsPath = Join-Path $src "HackExtensions.cs"
$itemsTabPath = Join-Path $src "Menu/HackMenu.ItemsTab.cs"
$enemiesTabPath = Join-Path $src "Menu/HackMenu.EnemiesTab.cs"
$networkTabPath = Join-Path $src "Menu/HackMenu.NetworkTab.cs"
$selfTabPath = Join-Path $src "Menu/HackMenu.SelfTab.cs"
$terminalTabPath = Join-Path $src "Menu/HackMenu.TerminalTab.cs"

$mod = Get-Content $modPath -Raw
$extensions = Get-Content $extensionsPath -Raw
$itemsTab = Get-Content $itemsTabPath -Raw
$enemiesTab = Get-Content $enemiesTabPath -Raw
$networkTab = Get-Content $networkTabPath -Raw
$selfTab = Get-Content $selfTabPath -Raw
$terminalTab = Get-Content $terminalTabPath -Raw

Assert-True ($mod -notmatch "_cheats\.Add\(new\s+\w+Cheat\(") `
    "LethalMenuMod still hardcodes CheatBase construction instead of reflection discovery."

Assert-True ($mod -notmatch "\b(UpdateKillClick|UpdateStunClick|UpdateFogState|UpdateBreadcrumbs|DrawBreadcrumbs)\b") `
    "LethalMenuMod still owns inline runtime hacks that should be CheatBase subclasses."

Assert-True ($extensions -match "IsAction") `
    "HackExtensions must distinguish one-shot action hacks from toggle hacks."

Assert-True ($extensions -match "if\s*\(\s*!hack\.IsAction\(\)\s*\)\s*[\r\n\s]*ToggleFlags\[hack\]\s*=") `
    "HackExtensions.InitializeDefaults must guard ToggleFlags registration with !hack.IsAction()."

$executorUsages = Get-ChildItem $src -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -ne $extensionsPath } |
    Select-String -Pattern "\.RegisterExecutor\("

Assert-True (@($executorUsages).Count -gt 0) `
    "No action executors are registered outside HackExtensions."

Assert-True ($itemsTab -notmatch 'DrawSection\("Item Spawner') `
    "Item spawner UI still lives in the Items tab instead of ItemManagerPopup."

Assert-True ($enemiesTab -notmatch 'DrawSection\("Enemy (Actions|List)') `
    "Enemy action/list UI still lives in the Enemies tab instead of EnemyManagerPopup."

$duplicatedHelpers = @(
    "private static void TeleportAllItemsToShip",
    "private static void TeleportNearbyItemsToPlayer",
    "private static void TeleportAllEnemiesAway"
)

foreach ($helper in $duplicatedHelpers) {
    Assert-True ($mod -notmatch [regex]::Escape($helper)) `
        "LethalMenuMod still owns duplicated helper '$helper' instead of using mixins/action executors."
}

$itemTabDuplicatedHelpers = @(
    "private void TeleportItemsToShip",
    "private void TeleportNearbyItemsToPlayer",
    "GetItemTeleportShipPosition",
    "GetItemTeleportStackOffset"
)

foreach ($helper in $itemTabDuplicatedHelpers) {
    Assert-True ($itemsTab -notmatch [regex]::Escape($helper)) `
        "Items tab still owns duplicated item teleport helper '$helper' instead of IItemManipulator."
}

$directActionCalls = @(
    "Cheats.NetworkCheats.SelfRevive(",
    "Cheats.NetworkCheats.FakeDeath(",
    "Cheats.NetworkCheats.CancelFakeDeath(",
    "Cheats.NetworkCheats.SellQuota(",
    "Cheats.NetworkCheats.ForceShipLeave(",
    "Cheats.NetworkCheats.EjectAllPlayers(",
    "Cheats.NetworkCheats.ForceStartGame(",
    "Cheats.NetworkCheats.ForceEndGame(",
    "Cheats.NetworkCheats.ReviveAllPlayers(",
    "Cheats.NetworkCheats.TeleportAllToMe(",
    "Cheats.NetworkCheats.UnlockAllDoors(",
    "Cheats.NetworkCheats.KillAllEnemies(",
    "Cheats.NetworkCheats.FlickerShipLights(",
    "Cheats.NetworkCheats.MaxChaos(",
    "Cheats.NetworkCheats.BlowUpAllLandmines(",
    "Cheats.NetworkCheats.BerserkAllTurrets("
)

$actionUi = "$selfTab`n$networkTab"
foreach ($call in $directActionCalls) {
    Assert-True ($actionUi -notmatch [regex]::Escape($call)) `
        "Menu UI directly calls '$call' instead of Hack.Execute()/DrawHackButton."
}

$networkTabExtractedSections = @(
    'DrawSection("Enemy Spawning',
    'DrawSection("Enemy Actions',
    'DrawSection("Ship Unlockables',
    'DrawSection("Ship Inventory',
    'DrawSection("Sell Items'
)

foreach ($section in $networkTabExtractedSections) {
    Assert-True ($networkTab -notmatch [regex]::Escape($section)) `
        "Network tab still owns extracted section '$section' instead of the popup manager."
}

$networkTabExtractedState = @(
    "_selectedEnemyIndex",
    "_cachedEnemyNames",
    "_selectedUnlockableIndex"
)

foreach ($field in $networkTabExtractedState) {
    Assert-True ($networkTab -notmatch [regex]::Escape($field)) `
        "Network tab still owns extracted popup state '$field'."
}

Assert-True ($terminalTab -notmatch "private\s+\(int scrapCount, int totalItems, int rawValue, int adjustedValue\)\s+CalculateShipInventory") `
    "Terminal tab still owns duplicated ship inventory calculation instead of IItemManipulator."

Assert-True ($terminalTab -notmatch "private\s+void\s+SellAllItemsNaturally") `
    "Terminal tab still owns duplicated sell-all logic instead of IItemManipulator/LootManagerPopup."

Write-Host "Architecture assertions passed."
