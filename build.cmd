@echo off
setlocal
set ROOT=%~dp0
set SRC=%ROOT%src
set OUT=%ROOT%bin
set OBJ=%ROOT%obj
set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set WPF=%FW%\WPF
set CSC=%FW%\csc.exe

if not exist "%CSC%" (
  echo [erro] csc.exe do .NET Framework 4.x nao encontrado em %FW%
  echo Instale o .NET Framework 4.8 ou superior.
  exit /b 1
)

rem Fonte unica da versao: version.txt na raiz.
set "VERSION=1.0"
if exist "%ROOT%version.txt" for /f "usebackq delims=" %%v in ("%ROOT%version.txt") do set "VERSION=%%v"

for /f "tokens=1,2,3,4 delims=." %%a in ("%VERSION%") do (
  set "V1=%%a"
  set "V2=%%b"
  set "V3=%%c"
  set "V4=%%d"
)
if "%V2%"=="" set "V2=0"
if "%V3%"=="" set "V3=0"
if "%V4%"=="" set "V4=0"
set "ASMVER=%V1%.%V2%.%V3%.%V4%"

if not exist "%OUT%" mkdir "%OUT%"
if not exist "%OBJ%" mkdir "%OBJ%"

rem AssemblyInfo gerado a partir de version.txt e incluido na compilacao.
> "%OBJ%\AssemblyInfo.g.cs" echo using System.Reflection;
>>"%OBJ%\AssemblyInfo.g.cs" echo [assembly: AssemblyTitle("Bahama Studio")]
>>"%OBJ%\AssemblyInfo.g.cs" echo [assembly: AssemblyProduct("Bahama Studio")]
>>"%OBJ%\AssemblyInfo.g.cs" echo [assembly: AssemblyCompany("Clavio Bahama")]
>>"%OBJ%\AssemblyInfo.g.cs" echo [assembly: AssemblyCopyright("Copyright Clavio Bahama")]
>>"%OBJ%\AssemblyInfo.g.cs" echo [assembly: AssemblyVersion("%ASMVER%")]
>>"%OBJ%\AssemblyInfo.g.cs" echo [assembly: AssemblyFileVersion("%ASMVER%")]

echo Compilando Bahama Studio %VERSION%...
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /debug- ^
  /out:"%OUT%\BahamaStudio.exe" ^
  /win32icon:"%ROOT%assets\BahamaStudio.ico" ^
  /win32manifest:"%SRC%\app.manifest" ^
  /resource:"%SRC%\Theme.xaml",Theme.xaml ^
  /reference:"%FW%\System.dll" ^
  /reference:"%FW%\System.Core.dll" ^
  /reference:"%FW%\System.Drawing.dll" ^
  /reference:"%FW%\System.Windows.Forms.dll" ^
  /reference:"%FW%\System.Xaml.dll" ^
  /reference:"%WPF%\PresentationFramework.dll" ^
  /reference:"%WPF%\PresentationCore.dll" ^
  /reference:"%WPF%\WindowsBase.dll" ^
  "%OBJ%\AssemblyInfo.g.cs" ^
  "%SRC%\Native.cs" "%SRC%\ExplorerTransparency.cs" "%SRC%\MachineProfile.cs" "%SRC%\Theme.cs" "%SRC%\Engine.cs" "%SRC%\Ui.cs" "%SRC%\Widgets.cs" ^
  "%SRC%\PageBase.cs" "%SRC%\DashboardPage.cs" "%SRC%\AppearancePage.cs" "%SRC%\InstallPage.cs" ^
  "%SRC%\DiagnosticsPage.cs" "%SRC%\EmergencyPage.cs" "%SRC%\MainWindow.cs" "%SRC%\Program.cs" ^
  "%SRC%\TrayIcon.cs"

if errorlevel 1 goto fail

copy /Y "%SRC%\BahamaStudio.exe.config" "%OUT%\BahamaStudio.exe.config" >nul
if exist "%OUT%\engine" rmdir /S /Q "%OUT%\engine"
xcopy /E /I /Y "%ROOT%engine" "%OUT%\engine" >nul

echo.
echo Pronto: "%OUT%\BahamaStudio.exe" (versao %ASMVER%)
exit /b 0

:fail
echo.
echo [erro] a compilacao falhou.
exit /b 1
