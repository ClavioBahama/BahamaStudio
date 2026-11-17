@echo off
setlocal
set "DEST=%LOCALAPPDATA%\Programs\Bahama Studio"
set "STARTMENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "UNKEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Bahama Studio"

echo ==========================================================
echo  Bahama Studio - Desinstalador
echo ==========================================================
echo.

tasklist /FI "IMAGENAME eq BahamaStudio.exe" | find /I "BahamaStudio.exe" >nul
if not errorlevel 1 (
  echo [ERRO] O Bahama Studio esta aberto. Feche-o e tente novamente.
  exit /b 1
)

echo Removendo o atalho do Menu Iniciar...
if exist "%STARTMENU%\Bahama Studio.lnk" del /F /Q "%STARTMENU%\Bahama Studio.lnk"

echo Removendo o atalho de arranque com o Windows...
if exist "%STARTUP%\Bahama Studio.lnk" del /F /Q "%STARTUP%\Bahama Studio.lnk"

echo Removendo o registro de desinstalacao...
reg delete "%UNKEY%" /f >nul 2>&1

echo Removendo a pasta do aplicativo...
rem O uninstall.cmd pode estar dentro dessa pasta, entao a remocao
rem e feita por um helper que so atua depois deste script terminar.
set "CLEANUP=%TEMP%\BahamaStudio-cleanup.cmd"
(
  echo @echo off
  echo ping -n 3 127.0.0.1 ^>nul 2^>^&1
  echo rmdir /S /Q "%DEST%" ^>nul 2^>^&1
  echo del /F /Q "%%~f0" ^>nul 2^>^&1
) > "%CLEANUP%"
start "" /min "%CLEANUP%"

echo.
echo Desinstalacao concluida.
echo.
echo Foram preservados os seus dados em:
echo   %LOCALAPPDATA%\BahamaStudio
echo   (configuracoes, logs, backups e efeitos aplicados)
echo.
echo A engine em Program Files nao foi removida. Para desativa-la,
echo abra o Bahama Studio antes de desinstalar e use a pagina Emergencia.
echo.
pause
