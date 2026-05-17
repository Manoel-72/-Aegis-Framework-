@echo off
cd /d %~dp0
echo Iniciando arcanos-da-mesa...
"%~dp0aegis-cli.exe"
if errorlevel 1 (
  echo.
  echo O jogo fechou com erro. Veja crash.log nesta pasta.
  pause
)
