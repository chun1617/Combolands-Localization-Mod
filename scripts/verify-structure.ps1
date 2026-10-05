Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

$requiredFiles = @(
    '.gitignore',
    'NuGet.Config',
    'README.md',
    'reference-stubs/Assembly-CSharp/Assembly-CSharp.csproj',
    'reference-stubs/Assembly-CSharp/GameStubs.cs',
    'reference-stubs/Unity.TextMeshPro/Unity.TextMeshPro.csproj',
    'reference-stubs/Unity.TextMeshPro/TMProStubs.cs',
    'reference-stubs/UnityEngine/UnityEngine.csproj',
    'reference-stubs/UnityEngine/Forwarders.cs',
    'reference-stubs/UnityEngine.CoreModule/UnityEngine.CoreModule.csproj',
    'reference-stubs/UnityEngine.CoreModule/CoreStubs.cs',
    'reference-stubs/UnityEngine.ImageConversionModule/UnityEngine.ImageConversionModule.csproj',
    'reference-stubs/UnityEngine.ImageConversionModule/ImageConversionStubs.cs',
    'reference-stubs/UnityEngine.TextCoreFontEngineModule/UnityEngine.TextCoreFontEngineModule.csproj',
    'reference-stubs/UnityEngine.TextCoreFontEngineModule/FontEngineStubs.cs',
    'reference-stubs/UnityEngine.UI/UnityEngine.UI.csproj',
    'reference-stubs/UnityEngine.UI/UIStubs.cs',
    'reference-stubs/UnityEngine.UIModule/UnityEngine.UIModule.csproj',
    'reference-stubs/UnityEngine.UIModule/UIRuntimeStubs.cs',
    '.github/workflows/ci.yml',
    '.github/workflows/release.yml',
    'scripts/package.ps1',
    'scripts/verify-structure.ps1',
    'src/Localization/Localization.csproj',
    'src/Localization/Plugin.cs',
    'src/Localization/CjkFontFallback.cs',
    'src/Localization/LocalizationRefresh.cs',
    'src/Localization/LocalizationState.cs',
    'src/Localization/MainMenuLogoOverride.cs',
    'src/Localization/MainMenuLogoPatch.cs',
    'src/Localization/ModSettings.cs',
    'src/Localization/ModStrings.cs',
    'src/Localization/SettingsMenuPatch.cs',
    'src/Localization/TextScaleManager.cs',
    'src/Localization/TranslationCatalog.cs',
    'src/Localization/TranslationPatch.cs',
    'localization/zh-Hant.json',
    'assets/MainMenuLogo.zh-Hant.png'
)

$missing = @(
    $requiredFiles | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $repoRoot $_) -PathType Leaf)
    }
)
if ($missing.Count -gt 0) {
    throw "Missing required public-repository files: $($missing -join ', ')"
}

foreach ($forbiddenDirectory in @('Combolands_Data', 'artifacts', 'translations', 'tools')) {
    if (Test-Path -LiteralPath (Join-Path $repoRoot $forbiddenDirectory)) {
        throw "Forbidden private/game directory exists in public staging tree: $forbiddenDirectory"
    }
}

$sourceTreeFiles = @(
    Get-ChildItem -LiteralPath $repoRoot -File -Recurse |
        Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj|dist)[\\/]'
        }
)
$forbiddenBinaryFiles = @(
    $sourceTreeFiles |
        Where-Object {
            $_.Extension.ToLowerInvariant() -in @('.dll', '.exe', '.pdb', '.zip', '.so', '.dylib')
        }
)
if ($forbiddenBinaryFiles.Count -gt 0) {
    throw "Binary/package files must not be committed: $($forbiddenBinaryFiles.FullName -join ', ')"
}

$projectPath = Join-Path $repoRoot 'src/Localization/Localization.csproj'
$projectText = Get-Content -LiteralPath $projectPath -Raw
[xml]$projectXml = $projectText

function Get-ProjectProperty([string]$name) {
    $node = $projectXml.SelectSingleNode("/Project/PropertyGroup/$name")
    if ($null -eq $node) {
        throw "Missing project property: $name"
    }
    return [string]$node.InnerText
}

$targetFramework = Get-ProjectProperty 'TargetFramework'
$assemblyName = Get-ProjectProperty 'AssemblyName'
$version = Get-ProjectProperty 'Version'

if ($targetFramework -ne 'netstandard2.1') {
    throw "Unexpected target framework: $targetFramework"
}
if ($assemblyName -ne 'Localization') {
    throw "Unexpected assembly name: $assemblyName"
}
if ($version -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$') {
    throw "Project version is not semantic-version shaped: $version"
}

$releaseNotesRelativePath = "release-notes/v$version.md"
$releaseNotesPath = Join-Path $repoRoot $releaseNotesRelativePath
if (-not (Test-Path -LiteralPath $releaseNotesPath -PathType Leaf)) {
    throw "Missing release notes for current version: $releaseNotesRelativePath"
}
$releaseNotesText = Get-Content -LiteralPath $releaseNotesPath -Raw
if ([string]::IsNullOrWhiteSpace($releaseNotesText)) {
    throw "Release notes are empty: $releaseNotesRelativePath"
}
if ($releaseNotesText.Length -gt 1200) {
    throw "Release notes must stay concise (maximum 1200 characters): $releaseNotesRelativePath"
}

$referenceNodes = @($projectXml.SelectNodes('/Project/ItemGroup/Reference'))
if ($referenceNodes.Count -eq 0) {
    throw 'Localization.csproj does not declare game/Unity compile references.'
}
foreach ($reference in $referenceNodes) {
    $hintPath = [string]$reference.HintPath
    if ($hintPath -notmatch '^\$\(CombolandsManagedDir\)[\\/]') {
        throw "Compile reference '$($reference.Include)' is not rooted at CombolandsManagedDir: $hintPath"
    }
}

$stubProjectReferences = @(
    $projectXml.SelectNodes('/Project/ItemGroup/ProjectReference') |
        ForEach-Object { [string]$_.Include }
) | Sort-Object
$requiredStubProjects = @(
    '../../reference-stubs/Assembly-CSharp/Assembly-CSharp.csproj',
    '../../reference-stubs/Unity.TextMeshPro/Unity.TextMeshPro.csproj',
    '../../reference-stubs/UnityEngine/UnityEngine.csproj',
    '../../reference-stubs/UnityEngine.CoreModule/UnityEngine.CoreModule.csproj',
    '../../reference-stubs/UnityEngine.ImageConversionModule/UnityEngine.ImageConversionModule.csproj',
    '../../reference-stubs/UnityEngine.TextCoreFontEngineModule/UnityEngine.TextCoreFontEngineModule.csproj',
    '../../reference-stubs/UnityEngine.UI/UnityEngine.UI.csproj',
    '../../reference-stubs/UnityEngine.UIModule/UnityEngine.UIModule.csproj'
) | Sort-Object
if (($stubProjectReferences -join "`n") -ne ($requiredStubProjects -join "`n")) {
    throw 'Localization.csproj source-only reference stub project set is missing or changed.'
}

$logoRelativePath = 'assets/MainMenuLogo.zh-Hant.png'
$logoPath = Join-Path $repoRoot $logoRelativePath
$logoBytes = [System.IO.File]::ReadAllBytes($logoPath)
$pngSignature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
if ($logoBytes.Length -lt 26) {
    throw "Main-menu logo is too short to be a valid PNG: $logoRelativePath"
}
for ($i = 0; $i -lt $pngSignature.Length; $i++) {
    if ($logoBytes[$i] -ne $pngSignature[$i]) {
        throw "Main-menu logo is not a valid PNG: $logoRelativePath"
    }
}
function Read-BigEndianUInt32([byte[]]$bytes, [int]$offset) {
    return [uint32](
        ([uint32]$bytes[$offset] -shl 24) -bor
        ([uint32]$bytes[$offset + 1] -shl 16) -bor
        ([uint32]$bytes[$offset + 2] -shl 8) -bor
        [uint32]$bytes[$offset + 3]
    )
}
$logoWidth = Read-BigEndianUInt32 $logoBytes 16
$logoHeight = Read-BigEndianUInt32 $logoBytes 20
$logoBitDepth = [int]$logoBytes[24]
$logoColorType = [int]$logoBytes[25]
if ($logoWidth -ne 2172 -or $logoHeight -ne 724) {
    throw "Main-menu logo dimensions must be 2172x724; actual $($logoWidth)x$($logoHeight)."
}
if ($logoBitDepth -ne 8 -or $logoColorType -ne 6) {
    throw "Main-menu logo must be 8-bit RGBA PNG; actual bitDepth=$logoBitDepth colorType=$logoColorType."
}

$embeddedLogo = @(
    $projectXml.SelectNodes('/Project/ItemGroup/EmbeddedResource') |
        Where-Object { [string]$_.Include -eq '../../assets/MainMenuLogo.zh-Hant.png' }
)
if ($embeddedLogo.Count -ne 1) {
    throw 'Localization.csproj must embed assets/MainMenuLogo.zh-Hant.png exactly once.'
}
if ([string]$embeddedLogo[0].LogicalName -ne 'Combolands.Localization.Assets.MainMenuLogo.zh-Hant.png') {
    throw 'Embedded main-menu logo LogicalName is missing or changed.'
}
if ([string]$embeddedLogo[0].WithCulture -ne 'false') {
    throw 'Embedded main-menu logo must set WithCulture=false so zh-Hant does not become a satellite assembly.'
}

$pluginPath = Join-Path $repoRoot 'src/Localization/Plugin.cs'
$pluginText = Get-Content -LiteralPath $pluginPath -Raw
$pluginVersionMatch = [regex]::Match(
    $pluginText,
    'public\s+const\s+string\s+PluginVersion\s*=\s*"([^"]+)"\s*;'
)
if (-not $pluginVersionMatch.Success) {
    throw 'PluginVersion constant was not found in Plugin.cs.'
}
$pluginVersion = $pluginVersionMatch.Groups[1].Value
if ($pluginVersion -ne $version) {
    throw "PluginVersion '$pluginVersion' does not match project Version '$version'."
}

$logoOverridePath = Join-Path $repoRoot 'src/Localization/MainMenuLogoOverride.cs'
$logoOverrideText = Get-Content -LiteralPath $logoOverridePath -Raw
if (-not $logoOverrideText.Contains('if (LocalizationState.IsChinese)')) {
    throw 'Main-menu logo policy must apply the localized logo to both TraditionalChinese and SimplifiedChinese.'
}
if ($logoOverrideText.Contains('LocalizationState.IsTraditionalChinese')) {
    throw 'Main-menu logo policy must not be restricted to TraditionalChinese only.'
}
foreach ($requiredLogoLayoutToken in @(
    'ApplyLocalizedLayout()',
    'RestoreOriginalLayout()',
    'LocalizedLogoSize',
    'LocalizedLogoAnchoredPosition'
)) {
    if (-not $logoOverrideText.Contains($requiredLogoLayoutToken)) {
        throw "Main-menu logo runtime layout contract is missing: $requiredLogoLayoutToken"
    }
}

$settingsPatchPath = Join-Path $repoRoot 'src/Localization/SettingsMenuPatch.cs'
$settingsPatchText = Get-Content -LiteralPath $settingsPatchPath -Raw
foreach ($requiredSettingsLayoutToken in @(
    'LayoutUtility.GetMinHeight(layoutRect)',
    'LayoutUtility.GetMinWidth(layoutRect)',
    'hostLayout.minHeight = viewportHeight'
)) {
    if (-not $settingsPatchText.Contains($requiredSettingsLayoutToken)) {
        throw "Settings scroll layout hardening contract is missing: $requiredSettingsLayoutToken"
    }
}

$catalogPath = Join-Path $repoRoot 'localization/zh-Hant.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
if ([string]$catalog.locale -ne 'zh-Hant') {
    throw "Unexpected catalog locale: $($catalog.locale)"
}
$entries = @($catalog.entries)
if ($entries.Count -eq 0) {
    throw 'Translation catalog contains no entries.'
}

$seenKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($entry in $entries) {
    $key = [string]$entry.key
    if ([string]::IsNullOrEmpty($key)) {
        throw 'Translation catalog contains a blank key.'
    }
    if (-not $seenKeys.Add($key)) {
        throw "Duplicate translation key: $key"
    }
}

$nugetConfigPath = Join-Path $repoRoot 'NuGet.Config'
[xml]$nugetConfig = Get-Content -LiteralPath $nugetConfigPath -Raw
$packageSources = @($nugetConfig.configuration.packageSources.add)
$packageSourceMap = @{}
foreach ($source in $packageSources) {
    $packageSourceMap[[string]$source.key] = [string]$source.value
}
if ($packageSourceMap['nuget.org'] -ne 'https://api.nuget.org/v3/index.json') {
    throw 'NuGet.Config must include the official nuget.org v3 feed.'
}
if ($packageSourceMap['BepInEx'] -ne 'https://nuget.bepinex.dev/v3/index.json') {
    throw 'NuGet.Config must include the official BepInEx NuGet feed.'
}

$ciText = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/ci.yml') -Raw
if ($ciText -notmatch '(?m)^\s*pull_request\s*:') {
    throw 'ci.yml must validate public pull requests.'
}
if ($ciText -notmatch 'dotnet\s+restore\s+src/Localization/Localization\.csproj\s+--nologo\s+--configfile\s+NuGet\.Config') {
    throw 'ci.yml must restore packages with the repository NuGet.Config.'
}
if ($ciText -match 'self-hosted|COMBOLANDS_MANAGED_DIR|combolands-localization-builders') {
    throw 'ci.yml must be fully GitHub-hosted and independent of private game paths/runners.'
}
$hostedRunnerMatches = [regex]::Matches($ciText, '(?m)^\s*runs-on\s*:\s*ubuntu-latest\s*$')
if ($hostedRunnerMatches.Count -lt 2) {
    throw 'ci.yml validate and build jobs must both run on ubuntu-latest.'
}
if ($ciText -match 'UseGameAssemblies\s*=\s*true|UseGameAssemblies=true') {
    throw 'ci.yml must build against source-only reference stubs, not game assemblies.'
}

$releaseText = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/release.yml') -Raw
if ($releaseText -notmatch '(?m)^\s*workflow_run\s*:') {
    throw 'release.yml must be triggered from completion of CI.'
}
if ($releaseText -notmatch '(?m)^\s*-\s*CI\s*$' -or
    $releaseText -notmatch '(?m)^\s*-\s*completed\s*$') {
    throw 'release.yml workflow_run trigger must target completed CI runs.'
}
if ($releaseText -notmatch "github\.event\.workflow_run\.conclusion\s*==\s*'success'" -or
    $releaseText -notmatch "github\.event\.workflow_run\.event\s*==\s*'push'" -or
    $releaseText -notmatch "github\.event\.workflow_run\.head_branch\s*==\s*'main'") {
    throw 'release.yml must publish only successful push CI runs from main.'
}
if ($releaseText -notmatch '(?m)^\s*contents\s*:\s*write\s*$') {
    throw 'release.yml must grant contents: write for release tag and GitHub Release publishing.'
}
if ($releaseText -notmatch '(?m)^\s*actions\s*:\s*read\s*$') {
    throw 'release.yml must grant actions: read to download the completed CI artifact.'
}
if ($releaseText -notmatch '(?m)^\s*runs-on\s*:\s*ubuntu-latest\s*$') {
    throw 'release.yml must publish from a GitHub-hosted runner.'
}
if ($releaseText -match 'self-hosted|COMBOLANDS_MANAGED_DIR') {
    throw 'release.yml must not access private game references or self-hosted runners.'
}
if ($releaseText -notmatch "ref:\s*\$\{\{\s*github\.event\.workflow_run\.head_sha\s*\}\}") {
    throw 'release.yml must checkout the exact successful CI commit.'
}
if ($releaseText -notmatch "gh\s+run\s+download\s+'?\$\{\{\s*github\.event\.workflow_run\.id\s*\}\}'?") {
    throw 'release.yml must download artifacts from the exact triggering CI run.'
}
if ($releaseText -notmatch 'refs/tags/\$RELEASE_TAG' -or
    $releaseText -notmatch 'git/ref/tags/\$RELEASE_TAG') {
    throw 'release.yml must create and verify the semantic version release tag without moving an existing tag.'
}
if ($releaseText -notmatch "(?s)assets=\(\s*'dist/Localization-v\$\{\{\s*steps\.metadata\.outputs\.version\s*\}\}\.zip'\s*\)") {
    throw 'GitHub Release must expose exactly one player-facing asset: Localization-vX.Y.Z.zip.'
}
if ($releaseText -match '--generate-notes') {
    throw 'release.yml must use curated version release notes instead of GitHub generated notes.'
}
if ($releaseText -notmatch 'RELEASE_NOTES_FILE:\s*\$\{\{\s*steps\.metadata\.outputs\.notes_file\s*\}\}' -or
    $releaseText -notmatch '--notes-file\s+"\$RELEASE_NOTES_FILE"') {
    throw 'release.yml must publish the curated release-notes/vX.Y.Z.md file.'
}

Write-Host 'Public staging structure verification passed.'
Write-Host "  Version: $version"
Write-Host "  Runtime sources: $((Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src/Localization') -Filter '*.cs' -File).Count)"
Write-Host "  Translation entries: $($entries.Count)"
