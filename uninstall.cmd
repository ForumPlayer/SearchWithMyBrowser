@echo off
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (goto UACPrompt) else (goto gotAdmin)
:UACPrompt
echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
"%temp%\getadmin.vbs"
goto End
:GotAdmin
if exist "%temp%\getadmin.vbs" (del "%temp%\getadmin.vbs")
pushd "%CD%"
CD /D "%~dp0"
:FileExists
cls
set RequiredFiles=1
IF NOT EXIST ".\SearchWithMyBrowser.exe" (set RequiredFiles=0)
IF /I %RequiredFiles% EQU 0 (
    echo One or more required files does not exists. Are you sure you successfully compiled? After making sure, please continue.
    pause
    goto FileExists
)

.\SearchWithMyBrowser.exe /unregister