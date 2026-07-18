param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$rustRoot = Join-Path $projectRoot 'native\atlaya_renderer'
$projectFile = Join-Path $projectRoot 'AtlayaView.csproj'
$distRoot = Join-Path $projectRoot 'dist'
$publishRoot = Join-Path $distRoot 'publish'
$intermediateRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'AtlayaView\publish-obj\'
$intermediateRootFx = Join-Path ([System.IO.Path]::GetTempPath()) 'AtlayaView\publish-obj-fx\'

[xml]$projectXml = Get-Content -LiteralPath $projectFile
$version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Version konnte aus AtlayaView.csproj nicht gelesen werden.'
}

# Self-contained (buendelt die .NET-Runtime, "mit .NET" auf der Webseite)
$publishDir = Join-Path $publishRoot ("AtlayaView-{0}-win-x64" -f $version)
$zipPath = Join-Path $distRoot ("AtlayaView-{0}-win-x64.zip" -f $version)

# Framework-dependent (setzt installierte .NET-9-Runtime voraus, "ohne .NET" auf der Webseite)
$publishDirFx = Join-Path $publishRoot ("AtlayaView-{0}-win-x64-fx" -f $version)
$zipPathFx = Join-Path $distRoot ("AtlayaView-{0}-win-x64-fx.zip" -f $version)

New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

foreach ($path in @($publishDir, $publishDirFx, $zipPath, $zipPathFx)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

Push-Location $rustRoot
try {
    cargo build --release
}
finally {
    Pop-Location
}

Push-Location $projectRoot
try {
    dotnet publish $projectFile -c $Configuration `
        -o $publishDir `
        -p:BaseIntermediateOutputPath=$intermediateRoot

    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force

    dotnet publish $projectFile -c $Configuration `
        -o $publishDirFx `
        -p:BaseIntermediateOutputPath=$intermediateRootFx `
        -p:SelfContained=false -p:PublishSingleFile=false --self-contained false

    Compress-Archive -Path (Join-Path $publishDirFx '*') -DestinationPath $zipPathFx -Force
}
finally {
    Pop-Location
}