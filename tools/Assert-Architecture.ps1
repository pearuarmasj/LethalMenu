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

$mod = Get-Content $modPath -Raw
$extensions = Get-Content $extensionsPath -Raw
$itemsTab = Get-Content $itemsTabPath -Raw
$enemiesTab = Get-Content $enemiesTabPath -Raw

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

Write-Host "Architecture assertions passed."
