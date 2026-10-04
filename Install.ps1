param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Sprocket')
$ErrorActionPreference = 'Stop'
if (Get-Process -Name Sprocket -ErrorAction SilentlyContinue) { throw 'Sluit Sprocket voordat je installeert.' }
$gamePath = (Resolve-Path -LiteralPath $GameDir).Path
if (!(Test-Path -LiteralPath (Join-Path $gamePath 'BepInEx\interop\Sprocket.VehicleController.dll'))) { throw 'Geen geschikte BepInEx IL2CPP-installatie.' }
if ((Get-FileHash -LiteralPath (Join-Path $gamePath 'GameAssembly.dll')).Hash -ne '18A9A15B5E5F11898ED4DC34FC3E2D4C12950C3B37AC1FA499E8B00592DEDD56') { throw 'Gameversie wijkt af van de onderzochte binary.' }
$pluginDir = Join-Path $gamePath 'BepInEx\plugins\SprocketThermalSight'
$dll = Join-Path $PSScriptRoot 'SprocketThermalSight.dll'
if (!(Test-Path -LiteralPath $dll)) { $dll = Join-Path $PSScriptRoot 'bin\Release\net6.0\SprocketThermalSight.dll' }
if (!(Test-Path -LiteralPath $dll)) { throw 'DLL ontbreekt.' }
$catalogSource = Join-Path $PSScriptRoot 'thermal-models.json'
$catalog = Get-Content -LiteralPath $catalogSource -Raw | ConvertFrom-Json
if ($catalog.version -ne 1 -or $catalog.models.Count -eq 0) { throw 'Ongeldige catalogus.' }
$copies = @(@{ Source=$dll; Target=(Join-Path $pluginDir 'SprocketThermalSight.dll') })
foreach ($model in $catalog.models) {
    $id = $model.componentId
    if ($id -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Ongeldige modelnaam.' }
    $part = Join-Path $PSScriptRoot "parts\${id}Part.json"
    $name = Join-Path $PSScriptRoot "parts\${id}.xml"
    if (!(Test-Path -LiteralPath $part) -or !(Test-Path -LiteralPath $name)) { throw "Partbestanden ontbreken: $id" }
    $definition = Get-Content -LiteralPath $part -Raw | ConvertFrom-Json
    if ($definition.guid -ne $model.partGuid -or $definition.components[0].fileID -ne $id) { throw "Part/profiel-identiteit komt niet overeen: $id" }
    $copies += @{ Source=$part; Target=(Join-Path $gamePath "Sprocket_Data\StreamingAssets\Parts\${id}Part.json") }
    $copies += @{ Source=$name; Target=(Join-Path $gamePath "Sprocket_Data\StreamingAssets\Localization\en-UK\Parts\${id}.xml") }
}
$catalogTarget = Join-Path $pluginDir 'thermal-models.json'
$mergedCatalogText = $null
if (!(Test-Path -LiteralPath $catalogTarget)) { $copies += @{ Source=$catalogSource; Target=$catalogTarget } }
else {
    $existingCatalog = Get-Content -LiteralPath $catalogTarget -Raw | ConvertFrom-Json
    if ($existingCatalog.version -ne 1 -or !$existingCatalog.models) { throw 'Bestaande catalogus is ongeldig; herstel die eerst.' }
    $added = $false
    foreach ($model in $catalog.models) {
        $existing = $existingCatalog.models | Where-Object componentId -eq $model.componentId | Select-Object -First 1
        if ($existing -and $existing.partGuid -ne $model.partGuid) { throw 'Part-GUID-conflict met bestaande catalogus.' }
        if ($existing) {
            foreach ($setting in @('palette','customColors','backgroundGamma')) {
                if (!$existing.PSObject.Properties[$setting]) {
                    $existing | Add-Member -MemberType NoteProperty -Name $setting -Value $model.$setting
                    $added = $true
                }
            }
            # Upgrade only the old packaged terrain defaults; retain other custom tonal settings.
            if ($existing.backgroundLevel -eq 0.12 -and $existing.backgroundDetail -eq 0.12) {
                $existing.backgroundLevel = $model.backgroundLevel
                $existing.backgroundDetail = $model.backgroundDetail
                $added = $true
            }
        }
        if (!$existing) { $existingCatalog.models = @($existingCatalog.models) + @($model); $added = $true }
    }
    if ($added) { $mergedCatalogText = $existingCatalog | ConvertTo-Json -Depth 20 }
}
$backup = Join-Path $PSScriptRoot ('backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Force -Path $backup | Out-Null
if ($mergedCatalogText) {
    $mergedSource = Join-Path $backup 'merged-catalog.json'
    $mergedCatalogText | Set-Content -LiteralPath $mergedSource -Encoding utf8
    $copies += @{ Source=$mergedSource; Target=$catalogTarget }
}
$index = 0
foreach ($item in $copies) {
    if (Test-Path -LiteralPath $item.Target) { Copy-Item -LiteralPath $item.Target -Destination (Join-Path $backup ("$index-" + (Split-Path $item.Target -Leaf))) }
    $index++
}
if (Test-Path -LiteralPath $catalogTarget) { Copy-Item -LiteralPath $catalogTarget -Destination (Join-Path $backup 'thermal-models.json') }
$copies | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'manifest.json')
if (Get-Process -Name Sprocket -ErrorAction SilentlyContinue) { throw 'Sprocket is gestart; installatie afgebroken.' }
foreach ($item in $copies) {
    New-Item -ItemType Directory -Force -Path (Split-Path $item.Target -Parent) | Out-Null
    Copy-Item -LiteralPath $item.Source -Destination $item.Target -Force
    if ((Get-FileHash -LiteralPath $item.Source).Hash -ne (Get-FileHash -LiteralPath $item.Target).Hash) { throw 'Bestandsverificatie mislukt.' }
}
"Prototype geïnstalleerd: $pluginDir"
"Bestaande profielen zijn behouden. Back-up: $backup"
"Gebruik Thermal sight model 1, 2 of 3; verwijder of deactiveer Teplovizor apart om twee N-hooks te voorkomen."
