@echo off
setlocal
cd /d "%~dp0"

echo Building AdminDesignerTool...
dotnet build "CientTest\AdminDesignerTool\AdminDesignerTool.csproj" -v minimal
if errorlevel 1 (
    echo.
    echo Build failed. Press any key to close.
    pause >nul
    exit /b 1
)

echo.
echo Starting AdminDesignerTool...
start "" "CientTest\AdminDesignerTool\bin\Debug\net8.0-windows\AdminDesignerTool.exe"
