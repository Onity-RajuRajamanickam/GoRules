@echo off
setlocal

set "DEPLOY_ROOT=C:\Projects\PUBLISH\GoRules"

cd /d "%~dp0GoRules.Api"
dotnet restore
if errorlevel 1 exit /b %errorlevel%

if not exist "%DEPLOY_ROOT%\api" mkdir "%DEPLOY_ROOT%\api"
dotnet publish -c Release -o "%DEPLOY_ROOT%\api"
exit /b %errorlevel%