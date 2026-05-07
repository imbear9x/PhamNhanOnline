param(
    [string]$ListenHost = "127.0.0.1",
    [int]$ListenPort = 7777,
    [string]$TargetHost = "192.168.192.128",
    [int]$TargetPort = 7777
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $repoRoot "artifacts"
$pidFile = Join-Path $artifactsDir "vm-udp-proxy.pid"
$stdoutLogFile = Join-Path $artifactsDir "vm-udp-proxy.out.log"
$stderrLogFile = Join-Path $artifactsDir "vm-udp-proxy.err.log"
$scriptPath = Join-Path $PSScriptRoot "udp_vm_proxy.py"

if (-not (Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir | Out-Null
}

$existing = Get-NetUDPEndpoint -ErrorAction SilentlyContinue | Where-Object {
    $_.LocalAddress -eq $ListenHost -and $_.LocalPort -eq $ListenPort
}

if ($existing) {
    throw "$ListenHost`:$ListenPort is already in use. Stop the local host server or the existing proxy first."
}

if (Test-Path $pidFile) {
    $existingPid = (Get-Content $pidFile -ErrorAction SilentlyContinue | Select-Object -First 1)
    if ($existingPid) {
        $proc = Get-Process -Id $existingPid -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Output "Proxy already running with PID $existingPid"
            exit 0
        }
    }
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
}

foreach ($logFile in @($stdoutLogFile, $stderrLogFile)) {
    if (Test-Path $logFile) {
        Remove-Item $logFile -Force
    }
}

$args = @(
    $scriptPath,
    "--listen-host", $ListenHost,
    "--listen-port", $ListenPort,
    "--target-host", $TargetHost,
    "--target-port", $TargetPort,
    "--pid-file", $pidFile
)

$process = Start-Process -FilePath "python" -ArgumentList $args -RedirectStandardOutput $stdoutLogFile -RedirectStandardError $stderrLogFile -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 1

if ($process.HasExited) {
    $stdoutLog = if (Test-Path $stdoutLogFile) { Get-Content $stdoutLogFile -Raw } else { "" }
    $stderrLog = if (Test-Path $stderrLogFile) { Get-Content $stderrLogFile -Raw } else { "" }
    throw "Proxy exited during startup. STDOUT: $stdoutLog STDERR: $stderrLog"
}

$bound = Get-NetUDPEndpoint -ErrorAction SilentlyContinue | Where-Object {
    $_.LocalAddress -eq $ListenHost -and $_.LocalPort -eq $ListenPort -and $_.OwningProcess -eq $process.Id
}

if (-not $bound) {
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    throw "Proxy process started but did not bind $ListenHost`:$ListenPort."
}

Write-Output "Proxy started. PID=$($process.Id) LISTEN=$ListenHost`:$ListenPort TARGET=$TargetHost`:$TargetPort STDOUT_LOG=$stdoutLogFile STDERR_LOG=$stderrLogFile"
