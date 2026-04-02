@echo off
setlocal
cd /d "%~dp0"

echo Killing GameServer / dotnet processes related to PhamNhanOnline...

powershell -NoProfile -ExecutionPolicy Bypass ^
  "$targets = Get-CimInstance Win32_Process | Where-Object { " ^
  "    $_.Name -eq 'GameServer.exe' -or " ^
  "    ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -like '*PhamNhanOnline*')" ^
  "};" ^
  "if (-not $targets) {" ^
  "    Write-Host 'No matching processes found.';" ^
  "    exit 0" ^
  "};" ^
  "Write-Host 'Matched processes:';" ^
  "$targets | Select-Object Name, ProcessId, CommandLine | Format-List;" ^
  "foreach ($target in $targets) {" ^
  "    try {" ^
  "        Stop-Process -Id $target.ProcessId -Force -ErrorAction Stop;" ^
  "        Write-Host ('Stopped PID ' + $target.ProcessId + ' (' + $target.Name + ')');" ^
  "    }" ^
  "    catch {" ^
  "        Write-Host ('Failed to stop PID ' + $target.ProcessId + ': ' + $_.Exception.Message);" ^
  "    }" ^
  "}"

echo.
echo Done. Press any key to close.
pause >nul
