@echo off
setlocal
set ROOT=%~dp0
set SRC=%ROOT%src
set OUT=%ROOT%bin
set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set WPF=%FW%\WPF
set CSC=%FW%\csc.exe

if not exist "%CSC%" (
  echo [erro] csc.exe do .NET Framework 4.x nao encontrado em %FW%
  echo Instale o .NET Framework 4.8 ou superior.
  exit /b 1
)

if not exist "%OUT%" mkdir "%OUT%"

echo Compilando Bahama Studio...
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /debug- ^
  /out:"%OUT%\BahamaStudio.exe" ^
  /win32icon:"%ROOT%assets\BahamaStudio.ico" ^
  /win32manifest:"%SRC%\app.manifest" ^
  /resource:"%SRC%\Theme.xaml",Theme.xaml ^
  /reference:"%FW%\System.dll" ^
  /reference:"%FW%\System.Core.dll" ^
  /reference:"%FW%\System.Xaml.dll" ^
  /reference:"%WPF%\PresentationFramework.dll" ^
  /reference:"%WPF%\PresentationCore.dll" ^
  /reference:"%WPF%\WindowsBase.dll" ^
  "%SRC%\Native.cs" "%SRC%\ExplorerTransparency.cs" "%SRC%\MachineProfile.cs" "%SRC%\Theme.cs" "%SRC%\Engine.cs" "%SRC%\Ui.cs" "%SRC%\Widgets.cs" ^
  "%SRC%\PageBase.cs" "%SRC%\DashboardPage.cs" "%SRC%\AppearancePage.cs" "%SRC%\InstallPage.cs" ^
  "%SRC%\DiagnosticsPage.cs" "%SRC%\EmergencyPage.cs" "%SRC%\MainWindow.cs" "%SRC%\Program.cs"

if errorlevel 1 goto fail

copy /Y "%SRC%\BahamaStudio.exe.config" "%OUT%\BahamaStudio.exe.config" >nul
if exist "%OUT%\engine" rmdir /S /Q "%OUT%\engine"
xcopy /E /I /Y "%ROOT%engine" "%OUT%\engine" >nul

echo.
echo Pronto: "%OUT%\BahamaStudio.exe"
exit /b 0

:fail
echo.
echo [erro] a compilacao falhou.
exit /b 1
