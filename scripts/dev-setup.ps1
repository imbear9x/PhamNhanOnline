$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot

try {
    Write-Host "[1/5] dotnet --version"
    dotnet --version

    Write-Host "[2/5] dotnet restore GameServer"
    dotnet restore .\GameServer\GameServer.csproj

    Write-Host "[3/5] dotnet restore TestClient"
    dotnet restore .\CientTest\TestClient\TestClient.csproj

    Write-Host "[4/5] dotnet build GameServer -c Debug"
    dotnet build .\GameServer\GameServer.csproj -c Debug

    Write-Host "[5/5] dotnet build TestClient -c Debug"
    dotnet build .\CientTest\TestClient\TestClient.csproj -c Debug
}
finally {
    Pop-Location
}
