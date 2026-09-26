param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$Configuration = 'Release',

    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..' 'dist')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'src/Localization/Localization.csproj'
$buildDirectory = Join-Path $repoRoot "src/Localization/bin/$Configuration/netstandard2.1"
$builtDll = Join-Path $buildDirectory 'Localization.dll'
$builtCatalog = Join-Path $buildDirectory 'zh-Hant.json'

& (Join-Path $PSScriptRoot 'verify-structure.ps1')

[xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
$projectVersion = [string]$projectXml.Project.PropertyGroup.Version
if ($projectVersion -ne $Version) {
    throw "Requested package version '$Version' does not match project version '$projectVersion'."
}

foreach ($path in @($builtDll, $builtCatalog)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required build output not found: $path"
    }
}

$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($outputRoot) | Out-Null

$dllAsset = Join-Path $outputRoot 'Localization.dll'
$zipAsset = Join-Path $outputRoot "Localization-v$Version.zip"
$checksumAsset = Join-Path $outputRoot 'SHA256SUMS.txt'
$stageRoot = Join-Path $outputRoot ".package-stage-v$Version"
$pluginDirectory = Join-Path $stageRoot 'BepInEx/plugins/Localization'

if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}
foreach ($artifact in @($dllAsset, $zipAsset, $checksumAsset)) {
    if (Test-Path -LiteralPath $artifact) {
        Remove-Item -LiteralPath $artifact -Force
    }
}

[System.IO.Directory]::CreateDirectory($pluginDirectory) | Out-Null
Copy-Item -LiteralPath $builtDll -Destination (Join-Path $pluginDirectory 'Localization.dll')
Copy-Item -LiteralPath $builtCatalog -Destination (Join-Path $pluginDirectory 'zh-Hant.json')
Copy-Item -LiteralPath $builtDll -Destination $dllAsset

Compress-Archive -Path (Join-Path $stageRoot 'BepInEx') -DestinationPath $zipAsset -CompressionLevel Optimal -Force

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipAsset)
try {
    $actualEntries = @(
        $archive.Entries |
            Where-Object { -not [string]::IsNullOrEmpty($_.Name) } |
            ForEach-Object { $_.FullName.Replace('\', '/') } |
            Sort-Object
    )
}
finally {
    $archive.Dispose()
}

$expectedEntries = @(
    'BepInEx/plugins/Localization/Localization.dll',
    'BepInEx/plugins/Localization/zh-Hant.json'
) | Sort-Object

$newLine = [Environment]::NewLine
if (($actualEntries -join $newLine) -ne ($expectedEntries -join $newLine)) {
    throw "Package contents mismatch. Expected: $($expectedEntries -join ', '); actual: $($actualEntries -join ', ')"
}

$hashLines = foreach ($artifact in @($dllAsset, $zipAsset)) {
    $hash = (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([System.IO.Path]::GetFileName($artifact))"
}
[System.IO.File]::WriteAllText(
    $checksumAsset,
    (($hashLines -join $newLine) + $newLine),
    [System.Text.UTF8Encoding]::new($false)
)

Remove-Item -LiteralPath $stageRoot -Recurse -Force

Write-Host 'Package created:'
Write-Host "  $dllAsset"
Write-Host "  $zipAsset"
Write-Host "  $checksumAsset"
