@echo off
setlocal
set "APP=Bahama Studio"
set "VERSION=1.0"
if exist "%~dp0version.txt" for /f "usebackq delims=" %%v in ("%~dp0version.txt") do set "VERSION=%%v"
set "SRC=%~dp0"
set "DEST=%LOCALAPPDATA%\Programs\Bahama Studio"
set "STARTMENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "UNKEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Bahama Studio"

echo ==========================================================
echo  Bahama Studio %VERSION% - Instalador (sem administrador)
echo ==========================================================
echo.

if not exist "%SRC%BahamaStudio.exe" (
  echo [ERRO] BahamaStudio.exe nao encontrado em:
  echo        %SRC%
  echo Extraia todos os ficheiros do ZIP antes de instalar.
  exit /b 1
)

if not exist "%SRC%engine\ExplorerBlurMica.dll" (
  echo [ERRO] A engine nao foi encontrada na pasta engine.
  echo Extraia todos os ficheiros do ZIP antes de instalar.
  exit /b 1
)

tasklist /FI "IMAGENAME eq BahamaStudio.exe" | find /I "BahamaStudio.exe" >nul
if not errorlevel 1 (
  echo [ERRO] O Bahama Studio esta aberto. Feche-o e tente novamente.
  exit /b 1
)

echo Pasta de destino:
echo   %DEST%
echo.

if not exist "%DEST%" mkdir "%DEST%"
if not exist "%DEST%" (
  echo [ERRO] Nao foi possivel criar a pasta de destino.
  exit /b 1
)

echo Copiando o aplicativo...
copy /Y "%SRC%BahamaStudio.exe" "%DEST%\BahamaStudio.exe" >nul
copy /Y "%SRC%BahamaStudio.exe.config" "%DEST%\BahamaStudio.exe.config" >nul
copy /Y "%SRC%uninstall.cmd" "%DEST%\uninstall.cmd" >nul
if exist "%SRC%README.md" copy /Y "%SRC%README.md" "%DEST%\README.md" >nul
if errorlevel 1 (
  echo [ERRO] Falha ao copiar o aplicativo.
  exit /b 1
)

echo Copiando a engine...
xcopy /E /I /Y "%SRC%engine" "%DEST%\engine" >nul
if errorlevel 1 (
  echo [ERRO] Falha ao copiar a engine.
  exit /b 1
)

echo Criando o atalho no Menu Iniciar...
powershell -NoProfile -Command "$sh = New-Object -ComObject WScript.Shell; $lnk = $sh.CreateShortcut('%STARTMENU%\Bahama Studio.lnk'); $lnk.TargetPath = '%DEST%\BahamaStudio.exe'; $lnk.WorkingDirectory = '%DEST%'; $lnk.IconLocation = '%DEST%\BahamaStudio.exe,0'; $lnk.Description = 'Bahama Studio - Gerenciador do ExplorerBlurMica'; $lnk.Save()"
if errorlevel 1 echo [AVISO] Nao foi possivel criar o atalho no Menu Iniciar.

echo Criando o atalho de arranque com o Windows...
powershell -NoProfile -Command "$sh = New-Object -ComObject WScript.Shell; $lnk = $sh.CreateShortcut('%STARTUP%\Bahama Studio.lnk'); $lnk.TargetPath = '%DEST%\BahamaStudio.exe'; $lnk.Arguments = '--startup'; $lnk.WorkingDirectory = '%DEST%'; $lnk.IconLocation = '%DEST%\BahamaStudio.exe,0'; $lnk.Description = 'Bahama Studio - reaplica os efeitos ao arrancar'; $lnk.Save()"
if errorlevel 1 echo [AVISO] Nao foi possivel criar o atalho de arranque.

echo Registrando a desinstalacao...
reg add "%UNKEY%" /v DisplayName /t REG_SZ /d "Bahama Studio %VERSION%" /f >nul
reg add "%UNKEY%" /v DisplayVersion /t REG_SZ /d "%VERSION%" /f >nul
reg add "%UNKEY%" /v Publisher /t REG_SZ /d "Clavio Bahama" /f >nul
reg add "%UNKEY%" /v DisplayIcon /t REG_SZ /d "%DEST%\BahamaStudio.exe,0" /f >nul
reg add "%UNKEY%" /v InstallLocation /t REG_SZ /d "%DEST%" /f >nul
reg add "%UNKEY%" /v UninstallString /t REG_SZ /d "\"%DEST%\uninstall.cmd\"" /f >nul
reg add "%UNKEY%" /v NoModify /t REG_DWORD /d 1 /f >nul
reg add "%UNKEY%" /v NoRepair /t REG_DWORD /d 1 /f >nul

echo.
echo Instalacao concluida.
echo.
echo Proximo passo:
echo   1. Abra o Bahama Studio pelo Menu Iniciar.
echo   2. Na pagina Diagnostico, use "Detetar e adaptar ao PC".
echo   3. Na pagina Instalar, registre a engine (pedira UAC so nesse momento).
echo.
echo Nada e enviado para a internet. Os dados ficam em:
echo   %LOCALAPPDATA%\BahamaStudio
echo.
pause
