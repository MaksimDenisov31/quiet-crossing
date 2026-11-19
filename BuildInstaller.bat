@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
call Build.bat
if errorlevel 1 exit /b 1
set "GAME_ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not "%~1"=="" set "GAME_ISCC=%~1"
if not exist "%GAME_ISCC%" (
 echo Install Inno Setup 6 or pass the ISCC.exe path as the first argument.
 exit /b 1
)
"%GAME_ISCC%" installer\QuietCrossing.iss
