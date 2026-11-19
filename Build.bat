@echo off
setlocal
cd /d "%~dp0"
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /unsafe+ /platform:anycpu /win32manifest:app.manifest /win32icon:assets\app.ico /out:QuietCrossing.exe /resource:assets\app.ico,QuietCrossing.Icon /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll src\*.cs
if errorlevel 1 exit /b 1
echo Built QuietCrossing.exe
