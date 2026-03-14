param(
    [ValidateSet("restore", "clean", "build")]
    [string]$Command = "build",
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solutionPath = Join-Path $repoRoot "PhamNhanOnline.sln"

Push-Location $repoRoot

try {
    $dotnetArgs = @(
        $Command
        $solutionPath
        "/p:MSBuildEnableWorkloadResolver=false"
        "-m:1"
        "-v:minimal"
    )

    if ($Command -in @("build", "clean")) {
        $dotnetArgs += "-c"
        $dotnetArgs += $Configuration
    }

    Write-Host "dotnet $($dotnetArgs -join ' ')"
    & dotnet @dotnetArgs

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
