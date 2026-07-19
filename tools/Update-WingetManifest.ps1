param(
    [string]$RepositoryOwner = 'Morfyuum',
    [string]$RepositoryName = 'AtlayaView',
    [string]$PackageIdentifier = 'Morfyuum.AtlayaView',
    [string]$Publisher = 'Chris Deliga',
    [string]$PublisherUrl = 'https://atlaya.capecter.com',
    [string]$PublisherSupportUrl = 'https://github.com/Morfyuum/AtlayaView/issues',
    [string]$Author = 'Chris Deliga',
    [string]$PackageName = 'AtlayaView',
    [string]$PackageUrl = 'https://atlaya.capecter.com/atlayaview/',
    [string]$License = 'MIT',
    [string]$LicenseUrl = 'https://raw.githubusercontent.com/Morfyuum/AtlayaView/main/LICENSE',
    [string]$Copyright = 'Copyright (c) 2026 Chris Deliga',
    [string]$ShortDescription = 'Fast local disk space visualization for Windows with cushion treemaps.',
    [string]$Description = 'AtlayaView scans one or more drives and turns used storage into an instantly readable treemap, making large folders and files visible at a glance. It is a local-first Windows desktop application with an optional native Rust renderer and automatic fallback to the managed renderer.',
    [string]$Moniker = 'atlayaview',
    [string[]]$Tags = @('disk-usage', 'disk-space', 'treemap', 'filesystem', 'storage', 'visualizer', 'windows'),
    [string]$ReleaseNotes,
    [string]$PackageLocale = 'en-US',
    [switch]$Validate
)

$ErrorActionPreference = 'Stop'

function ConvertTo-YamlList {
    param(
        [string[]]$Values,
        [string]$Indent = '  '
    )

    return ($Values | ForEach-Object { '{0}- {1}' -f $Indent, $_ }) -join [Environment]::NewLine
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot 'AtlayaView.csproj'

[xml]$projectXml = Get-Content -LiteralPath $projectFile
$version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Version konnte aus AtlayaView.csproj nicht gelesen werden.'
}

if ([string]::IsNullOrWhiteSpace($ReleaseNotes)) {
    $ReleaseNotes = 'Version {0} of AtlayaView.' -f $version
}

$releaseTag = 'v{0}' -f $version
$releaseJson = gh release view $releaseTag --repo "$RepositoryOwner/$RepositoryName" --json assets | ConvertFrom-Json
$assetName = 'AtlayaView-{0}-win-x64-full.zip' -f $version
$asset = $releaseJson.assets | Where-Object { $_.name -eq $assetName } | Select-Object -First 1
if (-not $asset) {
    throw "GitHub-Release-Asset '$assetName' wurde nicht gefunden."
}

if ([string]::IsNullOrWhiteSpace($asset.digest) -or -not $asset.digest.StartsWith('sha256:', [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Das GitHub-Release-Asset enthaelt keinen verwendbaren sha256-Digest.'
}

$sha256 = $asset.digest.Substring(7).ToUpperInvariant()
$manifestRoot = Join-Path $projectRoot (Join-Path 'winget' (Join-Path $PackageIdentifier $version))
New-Item -ItemType Directory -Force -Path $manifestRoot | Out-Null

$versionManifest = @"
# Created by tools/Update-WingetManifest.ps1
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.12.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $version
DefaultLocale: $PackageLocale
ManifestType: version
ManifestVersion: 1.12.0
"@

$installerManifest = @"
# Created by tools/Update-WingetManifest.ps1
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.12.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $version
InstallerType: zip
Installers:
  - Architecture: x64
    NestedInstallerType: portable
    NestedInstallerFiles:
      - RelativeFilePath: AtlayaView.exe
    ArchiveBinariesDependOnPath: true
    InstallerUrl: $($asset.url)
    InstallerSha256: $sha256
    AppsAndFeaturesEntries:
      - DisplayName: AtlayaView
ManifestType: installer
ManifestVersion: 1.12.0
"@

$tagLines = ConvertTo-YamlList -Values $Tags
$localeManifest = @"
# Created by tools/Update-WingetManifest.ps1
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.12.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $version
PackageLocale: $PackageLocale
Publisher: $Publisher
PublisherUrl: $PublisherUrl
PublisherSupportUrl: $PublisherSupportUrl
Author: $Author
PackageName: $PackageName
PackageUrl: $PackageUrl
License: $License
LicenseUrl: $LicenseUrl
Copyright: $Copyright
ShortDescription: $ShortDescription
Description: $Description
Moniker: $Moniker
Tags:
$tagLines
ReleaseNotes: $ReleaseNotes
ReleaseNotesUrl: https://github.com/$RepositoryOwner/$RepositoryName/releases/tag/$releaseTag
ManifestType: defaultLocale
ManifestVersion: 1.12.0
"@

Set-Content -LiteralPath (Join-Path $manifestRoot "$PackageIdentifier.yaml") -Value $versionManifest -Encoding utf8NoBOM
Set-Content -LiteralPath (Join-Path $manifestRoot "$PackageIdentifier.installer.yaml") -Value $installerManifest -Encoding utf8NoBOM
Set-Content -LiteralPath (Join-Path $manifestRoot "$PackageIdentifier.locale.$PackageLocale.yaml") -Value $localeManifest -Encoding utf8NoBOM

if ($Validate) {
    winget validate $manifestRoot
}

Write-Host ('WinGet-Manifeste aktualisiert: {0}' -f $manifestRoot)
Write-Host ('Release-Asset: {0}' -f $asset.url)
Write-Host ('SHA256: {0}' -f $sha256)