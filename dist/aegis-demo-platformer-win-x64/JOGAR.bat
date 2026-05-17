@echo off
cd /d %~dp0
echo Iniciando aegis-demo-platformer...
"%~dp0aegis-cli.exe"
if errorlevel 1 (
  echo.
  echo O jogo fechou com erro. Veja crash.log nesta pasta.
  pause
)
