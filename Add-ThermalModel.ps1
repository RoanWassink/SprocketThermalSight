param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$Id,
    [Parameter(Mandatory=$true)][string]$DisplayName,
    [string]$CopyFrom = 'thermalSightModel2'
)
$ErrorActionPreference = 'Stop'
$catalogPath = Join-Path $PSScriptRoot 'thermal-models.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
if ($catalog.models | Where-Object componentId -eq $Id) { throw "Model bestaat al: $Id" }
$base = $catalog.models | Where-Object componentId -eq $CopyFrom | Select-Object -First 1
if (!$base) { throw "Basismodel ontbreekt: $CopyFrom" }
$partPath = Join-Path $PSScriptRoot "parts\${Id}Part.json"
$namePath = Join-Path $PSScriptRoot "parts\${Id}.xml"
if ((Test-Path -LiteralPath $partPath) -or (Test-Path -LiteralPath $namePath)) { throw 'Doelbestanden bestaan al.' }
$model = $base | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$model.partGuid = [Guid]::NewGuid().ToString()
$model.componentId = $Id
$model.displayName = $DisplayName
$part = Get-Content -LiteralPath (Join-Path $PSScriptRoot "parts\${CopyFrom}Part.json") -Raw | ConvertFrom-Json
$part.guid = $model.partGuid; $part.name = $Id; $part.components[0].fileID = $Id
$escaped = [System.Security.SecurityElement]::Escape($DisplayName)
$xml = '<?xml version="1.0" encoding="utf-16"?>' + "`n<Part><name>$escaped</name><description>Thermal gunner sight. Press N while scoped. Sensor quality is configured per model in thermal-models.json.</description></Part>"
Copy-Item -LiteralPath $catalogPath -Destination ($catalogPath + '.backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$part | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $partPath -Encoding utf8
$xml | Set-Content -LiteralPath $namePath -Encoding unicode
$catalog.models = @($catalog.models) + @($model)
$catalog | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $catalogPath -Encoding utf8
"Model aangemaakt: $DisplayName. Pas het profiel aan en voer Install.ps1 uit met Sprocket gesloten."
