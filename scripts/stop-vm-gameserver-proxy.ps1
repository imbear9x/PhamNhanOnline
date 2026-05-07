$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$pidFile = Join-Path $repoRoot "artifacts\\vm-udp-proxy.pid"

if (-not (Test-Path $pidFile)) {
    Write-Output "Proxy is not running."
    exit 0
}

$proxyPid = Get-Content $pidFile -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proxyPid) {
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
    Write-Output "Proxy pid file was empty and has been cleaned up."
    exit 0
}

$process = Get-Process -Id $proxyPid -ErrorAction SilentlyContinue
if (-not $process) {
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
    Write-Output "Proxy process was not running. Stale pid file removed."
    exit 0
}

Stop-Process -Id $proxyPid -Force
Start-Sleep -Milliseconds 300
Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
Write-Output "Proxy stopped. PID=$proxyPid"
