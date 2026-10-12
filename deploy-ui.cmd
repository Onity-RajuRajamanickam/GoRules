@echo off
setlocal

set "DEPLOY_ROOT=C:\Projects\PUBLISH\GoRules"

cd /d "%~dp0GoRules.React"
call npm install
if errorlevel 1 exit /b %errorlevel%

call npm run build
if errorlevel 1 exit /b %errorlevel%

if not exist "%DEPLOY_ROOT%\ui" mkdir "%DEPLOY_ROOT%\ui"
xcopy /e /i /y "dist\*" "%DEPLOY_ROOT%\ui\" >nul
exit /b %errorlevel%