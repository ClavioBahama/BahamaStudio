@echo off
setlocal
set ROOT=%~dp0

echo Empacotando Bahama Studio...
call "%ROOT%build.cmd"
if errorlevel 1 (
  echo [erro] a compilacao falhou; pacote nao gerado.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%tools\package.ps1"
if errorlevel 1 (
  echo [erro] falha ao gerar o pacote.
  exit /b 1
)

echo.
echo Pacote concluido em dist\.
exit /b 0
