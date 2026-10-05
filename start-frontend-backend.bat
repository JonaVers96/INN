@echo off
setlocal
cd /d "%~dp0"

set BACKEND_PORT=5209

start "Backend" cmd /k dotnet run --project backend/LYL.Api/LYL.Api.csproj --launch-profile http

echo Waiting for backend on port %BACKEND_PORT%...
:wait
powershell -NoProfile -Command "try { (New-Object Net.Sockets.TcpClient).Connect('localhost', %BACKEND_PORT%); exit 0 } catch { exit 1 }"
if errorlevel 1 (
    timeout /t 2 /nobreak >nul
    goto wait
)

echo Backend is up. Starting frontend...
start "Frontend" cmd /k "cd frontend && npm run dev"