param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$serverProject = Join-Path $repoRoot 'GameServer/GameServer.csproj'
$clientProject = Join-Path $repoRoot 'ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj'

Write-Host "Building server..."
dotnet build $serverProject -c $Configuration
if ($LASTEXITCODE -ne 0)
{
    throw "Server build failed."
}

Write-Host "Building Unity client assembly..."
dotnet build $clientProject -c $Configuration
if ($LASTEXITCODE -ne 0)
{
    throw "Client build failed."
}

Write-Host "Verification build complete."
