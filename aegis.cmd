@echo off
REM Runs from the repository root while preserving the caller folder.
setlocal
set "CALL_CWD=%CD%"
cd /d "%~dp0"

REM Convenience: from inside a game folder, "..\aegis.cmd run" runs that folder.
if /I "%~1"=="run" if "%~2"=="" (
  dotnet run --no-restore --project "src\Aegis.CLI\Aegis.CLI.csproj" -- run "%CALL_CWD%"
  exit /b %ERRORLEVEL%
)

dotnet run --project "src\Aegis.CLI\Aegis.CLI.csproj" -- %*
exit /b %ERRORLEVEL%
