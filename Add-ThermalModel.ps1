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
$model = $base | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$model.PSObject.Properties.Remove('partGuid')
$model.componentId = $Id
$model.displayName = $DisplayName
Copy-Item -LiteralPath $catalogPath -Destination ($catalogPath + '.backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$catalog.models = @($catalog.models) + @($model)
$catalog | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $catalogPath -Encoding utf8
"JSON-profiel aangemaakt: $DisplayName. Geen extra part nodig. Installeer de catalogus en herstart Sprocket om het in de profile dropdown te zien."
