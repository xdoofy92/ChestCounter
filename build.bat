@echo off
setlocal EnableExtensions
chcp 65001 >nul
title ChestCounter - Build
cd /d "%~dp0"

rem Colores (Windows 10/11). Si la consola no los soporta, solo se ve el texto.
for /f %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"
set "LOG=%~dp0build.log"
set "SDK_URL=https://dotnet.microsoft.com/download/dotnet/10.0"

rem ===============================================================
rem  Idioma / Language
rem ===============================================================
cls
echo.
echo   1. Español
echo   2. English
echo.
choice /c 12 /n /m "Elige idioma / Choose language [1/2]: "
if errorlevel 2 (call :lang_en) else (call :lang_es)
set "OK=%ESC%[92m%T_OK%%ESC%[0m"
set "FALTA=%ESC%[91m%T_MISSING%%ESC%[0m"
set "ERR=%ESC%[91mERROR%ESC%[0m"
title %T_TITLE%

cls
echo.
echo ==============================================================
echo    %T_TITLE%
echo ==============================================================
echo.
echo %T_INTRO1%
echo %T_INTRO2%
echo %T_INTRO3%
echo.

rem ===============================================================
rem  FASE 1: requisitos / requirements
rem ===============================================================
:checks
echo %T_CHECKING%
echo ----------------------
set "MISSING="

rem [1/3] Windows de 64 bits
<nul set /p "=[1/3] %T_CHK1% "
set "ARCH=%PROCESSOR_ARCHITECTURE%"
if defined PROCESSOR_ARCHITEW6432 set "ARCH=%PROCESSOR_ARCHITEW6432%"
if /i "%ARCH%"=="AMD64" goto arch_ok
if /i "%ARCH%"=="ARM64" goto arch_ok
echo %FALTA%
echo.
echo %T_NO64%
goto fail_end
:arch_ok
echo %OK%

rem [2/3] Programa dotnet
<nul set /p "=[2/3] %T_CHK2% "
set "DOTNET="
where dotnet >nul 2>nul && set "DOTNET=dotnet"
if not defined DOTNET if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
if defined DOTNET (echo %OK%) else (echo %FALTA%& set "MISSING=1")

rem [3/3] SDK de .NET 10 (el runtime solo NO basta para compilar)
<nul set /p "=[3/3] %T_CHK3% "
set "SDKVER="
if defined DOTNET "%DOTNET%" --list-sdks > "%TEMP%\cc_sdks.txt" 2>nul
if defined DOTNET for /f "tokens=1" %%v in ('findstr /b "10." "%TEMP%\cc_sdks.txt"') do set "SDKVER=%%v"
if defined SDKVER (echo %OK%  %T_VERSION% %SDKVER%) else (echo %FALTA%& set "MISSING=1")
echo.

if not defined MISSING goto build

rem ===============================================================
rem  Falta el SDK: explicar y ofrecer instalarlo
rem ===============================================================
if defined TRIED_INSTALL goto manual
echo %T_SDK1%
echo %T_SDK2%
if defined DOTNET echo %T_RUNTIME1%
if defined DOTNET echo %T_RUNTIME2%
echo.
where winget >nul 2>nul || goto manual
echo %T_WINGET0%
echo   - %T_WINGET1%
echo   - %T_WINGET2%
echo   - %T_WINGET3%
echo.
choice /c %T_YN% /n /m "%T_ASK_INSTALL%"
if errorlevel 2 goto manual
set "TRIED_INSTALL=1"
echo.
echo %T_INSTALLING%
echo --------------------------------------------------------------
winget install --id Microsoft.DotNet.SDK.10 -e --source winget --accept-source-agreements --accept-package-agreements
echo --------------------------------------------------------------
echo.
echo %T_INSTALLED%
echo.
goto checks

:manual
if defined TRIED_INSTALL echo %T_AUTO_FAILED%
echo %T_MANUAL0%
echo   1. %T_MANUAL1% %SDK_URL%
echo   2. %T_MANUAL2%
echo      %T_MANUAL3%
echo   3. %T_MANUAL4%
echo.
choice /c %T_YN% /n /m "%T_ASK_OPEN%"
if not errorlevel 2 start "" "%SDK_URL%"
goto fail_end

rem ===============================================================
rem  FASE 2: compilación / build (el detalle va a build.log)
rem ===============================================================
:build
echo %T_READY%
echo.
echo %T_BUILDING%
echo ----------
if exist "dist\ChestCounter.exe" del /q "dist\ChestCounter.exe"
echo Build log - %date% %time% > "%LOG%"

<nul set /p "=[1/2] %T_STEP1% "
"%DOTNET%" restore src\ChestCounter\ChestCounter.csproj -r win-x64 --nologo >> "%LOG%" 2>&1
rem errorlevel puede ser negativo, por eso se compara con 0 y no con "errorlevel 1".
if not "%errorlevel%"=="0" goto restore_failed
echo %OK%

<nul set /p "=[2/2] %T_STEP2% "
"%DOTNET%" publish src\ChestCounter\ChestCounter.csproj -c Release -r win-x64 --self-contained true --no-restore --nologo ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o dist >> "%LOG%" 2>&1
if not "%errorlevel%"=="0" goto publish_failed
if not exist "dist\ChestCounter.exe" goto no_exe
echo %OK%

echo.
echo ==============================================================
echo    %T_DONE%
echo ==============================================================
echo.
echo %T_WHERE% %~dp0dist\ChestCounter.exe
echo %T_OPENING%
explorer dist
echo.
pause
exit /b 0

:restore_failed
echo %ERR%
echo.
echo %T_RESTORE_FAIL1%
echo %T_RESTORE_FAIL2%
goto build_failed

:publish_failed
echo %ERR%
goto build_failed

:no_exe
echo %ERR%
echo.
echo %T_NO_EXE%

:build_failed
echo.
echo %T_ERRORS%
echo --------------------------------------------------------------
findstr /i /r /c:": error " /c:"error [A-Z][A-Z]*[0-9]" "%LOG%"
echo --------------------------------------------------------------
echo %T_LOG% %LOG%

:fail_end
echo.
pause
exit /b 1

rem ===============================================================
rem  Textos / Texts
rem ===============================================================
:lang_es
set "T_TITLE=ChestCounter - Compilar desde el código fuente"
set "T_INTRO1=Este asistente genera ChestCounter.exe a partir del código."
set "T_INTRO2=Primero comprobará que tu PC tiene todo lo necesario."
set "T_INTRO3=No se instalará nada sin preguntarte antes."
set "T_OK=OK"
set "T_MISSING=FALTA"
set "T_VERSION=versión"
set "T_YN=SN"
set "T_CHECKING=Comprobando requisitos"
set "T_CHK1=Windows de 64 bits ....................."
set "T_CHK2=Programa .NET (dotnet) ................."
set "T_CHK3=Kit de desarrollo .NET 10 (SDK) ........"
set "T_NO64=ChestCounter solo funciona en Windows de 64 bits. No se puede continuar."
set "T_SDK1=Falta el kit de desarrollo de .NET 10 (SDK)."
set "T_SDK2=Es la herramienta gratuita de Microsoft que convierte el código en el .exe."
set "T_RUNTIME1=Tienes .NET instalado, pero solo la parte para EJECUTAR programas"
set "T_RUNTIME2=(el "runtime"), no la necesaria para COMPILARLOS (el "SDK")."
set "T_WINGET0=Puedo instalarlo ahora con winget, el instalador oficial de Windows:"
set "T_WINGET1=Se descargan unos 200 MB desde Microsoft."
set "T_WINGET2=Windows te pedirá permiso de administrador."
set "T_WINGET3=Tarda unos minutos, según tu conexión."
set "T_ASK_INSTALL=¿Instalar el SDK de .NET 10 ahora? [S/N] "
set "T_INSTALLING=Instalando el SDK de .NET 10. No cierres esta ventana..."
set "T_INSTALLED=Instalación terminada. Volvemos a comprobar los requisitos."
set "T_AUTO_FAILED=La instalación automática no ha funcionado."
set "T_MANUAL0=Puedes instalarlo a mano en 3 pasos:"
set "T_MANUAL1=Abre"
set "T_MANUAL2=En la columna "SDK", descarga el instalador "Windows x64"."
set "T_MANUAL3=Ojo: el "Runtime" NO sirve, tiene que ser el SDK."
set "T_MANUAL4=Instálalo y vuelve a abrir build.bat."
set "T_ASK_OPEN=¿Abrir la página de descarga ahora? [S/N] "
set "T_READY=Todo listo. Empezamos la compilación."
set "T_BUILDING=Compilando"
set "T_STEP1=Descargando librerías necesarias ......"
set "T_STEP2=Compilando ChestCounter.exe (1-2 min) .."
set "T_DONE=¡Listo! ChestCounter.exe se ha generado correctamente."
set "T_WHERE=Lo tienes en:"
set "T_OPENING=Se abrirá la carpeta para que lo veas."
set "T_RESTORE_FAIL1=No se pudieron descargar las librerías desde nuget.org."
set "T_RESTORE_FAIL2=Comprueba tu conexión a internet, o si un antivirus/proxy bloquea api.nuget.org."
set "T_NO_EXE=La compilación terminó pero no se generó ChestCounter.exe."
set "T_ERRORS=Algo ha fallado. Estos son los errores encontrados:"
set "T_LOG=El registro completo está en:"
exit /b

:lang_en
set "T_TITLE=ChestCounter - Build from source"
set "T_INTRO1=This assistant builds ChestCounter.exe from the source code."
set "T_INTRO2=First it will check that your PC has everything it needs."
set "T_INTRO3=Nothing will be installed without asking you first."
set "T_OK=OK"
set "T_MISSING=MISSING"
set "T_VERSION=version"
set "T_YN=YN"
set "T_CHECKING=Checking requirements"
set "T_CHK1=64-bit Windows ........................."
set "T_CHK2=.NET program (dotnet) .................."
set "T_CHK3=.NET 10 development kit (SDK) .........."
set "T_NO64=ChestCounter only works on 64-bit Windows. Cannot continue."
set "T_SDK1=The .NET 10 development kit (SDK) is missing."
set "T_SDK2=It is Microsoft's free tool that turns the source code into the .exe."
set "T_RUNTIME1=You have .NET installed, but only the part that RUNS programs"
set "T_RUNTIME2=(the "runtime"), not the part needed to BUILD them (the "SDK")."
set "T_WINGET0=I can install it now with winget, the official Windows installer:"
set "T_WINGET1=About 200 MB is downloaded from Microsoft."
set "T_WINGET2=Windows will ask you for administrator permission."
set "T_WINGET3=It takes a few minutes, depending on your connection."
set "T_ASK_INSTALL=Install the .NET 10 SDK now? [Y/N] "
set "T_INSTALLING=Installing the .NET 10 SDK. Do not close this window..."
set "T_INSTALLED=Installation finished. Checking the requirements again."
set "T_AUTO_FAILED=The automatic installation did not work."
set "T_MANUAL0=You can install it manually in 3 steps:"
set "T_MANUAL1=Open"
set "T_MANUAL2=In the "SDK" column, download the "Windows x64" installer."
set "T_MANUAL3=Note: the "Runtime" is NOT enough, it must be the SDK."
set "T_MANUAL4=Install it and run build.bat again."
set "T_ASK_OPEN=Open the download page now? [Y/N] "
set "T_READY=All set. Starting the build."
set "T_BUILDING=Building"
set "T_STEP1=Downloading required libraries ........."
set "T_STEP2=Building ChestCounter.exe (1-2 min) ...."
set "T_DONE=Done! ChestCounter.exe was built successfully."
set "T_WHERE=You can find it at:"
set "T_OPENING=The folder will open so you can see it."
set "T_RESTORE_FAIL1=The libraries could not be downloaded from nuget.org."
set "T_RESTORE_FAIL2=Check your internet connection, or whether an antivirus/proxy blocks api.nuget.org."
set "T_NO_EXE=The build finished but ChestCounter.exe was not created."
set "T_ERRORS=Something went wrong. These are the errors found:"
set "T_LOG=The full log is at:"
exit /b
