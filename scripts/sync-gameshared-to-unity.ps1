param(
    [string]$Configuration = "Debug",
    [string]$UnityProjectPath = "ClientUnity/PhamNhanOnline"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$gameSharedProject = Join-Path $repoRoot "GameShared/GameShared.csproj"
$gameSharedPluginsPath = Join-Path $repoRoot "$UnityProjectPath/Assets/Plugins/GameShared"
$liteNetLibPluginsPath = Join-Path $repoRoot "$UnityProjectPath/Assets/Plugins/LiteNetLib"

Write-Host "Building GameShared for Unity (netstandard2.1)..."
dotnet build $gameSharedProject -c $Configuration -f netstandard2.1 -m:1 -v:minimal
if ($LASTEXITCODE -ne 0)
{
    throw "GameShared build failed."
}

$outputPath = Join-Path $repoRoot "GameShared/bin/$Configuration/netstandard2.1"
if (-not (Test-Path $outputPath))
{
    throw "Expected output path not found: $outputPath"
}

New-Item -ItemType Directory -Force -Path $gameSharedPluginsPath | Out-Null
New-Item -ItemType Directory -Force -Path $liteNetLibPluginsPath | Out-Null

$filesToCopy = @(
    "GameShared.dll",
    "GameShared.pdb",
    "GameShared.xml"
)

foreach ($fileName in $filesToCopy)
{
    $source = Join-Path $outputPath $fileName
    if (-not (Test-Path $source))
    {
        continue
    }

    $destination = Join-Path $gameSharedPluginsPath $fileName
    Copy-Item $source $destination -Force
    Write-Host "Synced $fileName -> $destination"
}

$liteNetLibSourceRoot = Join-Path $env:USERPROFILE ".nuget\packages\litenetlib\2.0.2\lib\netstandard2.1"
$liteNetLibFiles = @(
    "LiteNetLib.dll",
    "LiteNetLib.xml"
)

foreach ($fileName in $liteNetLibFiles)
{
    $source = Join-Path $liteNetLibSourceRoot $fileName
    if (-not (Test-Path $source))
    {
        continue
    }

    $destination = Join-Path $liteNetLibPluginsPath $fileName
    Copy-Item $source $destination -Force
    Write-Host "Synced $fileName -> $destination"
}

Write-Host "Unity shared dependency sync complete."
