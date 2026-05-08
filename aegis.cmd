@echo off
REM Sempre executa a partir da pasta deste ficheiro (raiz do repo) — build + cwd corretos.
setlocal
cd /d "%~dp0"
dotnet run --project "src\Aegis.CLI\Aegis.CLI.csproj" -- %*
exit /b %ERRORLEVEL%
