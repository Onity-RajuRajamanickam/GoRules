@echo off
start "GoRules API" /d "%~dp0GoRules.Api" dotnet run --urls http://localhost:5147
start "GoRules UI" /d "%~dp0GoRules.React" cmd /k "npm install && npm run dev"
