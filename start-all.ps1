Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Launching AutoTrade FnO (Backend + Frontend Ecosystem)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Start Backend in new process
Write-Host "[1/2] Starting ASP.NET Core Backend on https://localhost:5011 & http://localhost:5010..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-ExecutionPolicy", "Bypass", "-File", "$PSScriptRoot\run-backend.ps1"

# 2. Wait 2 seconds
Start-Sleep -Seconds 2

# 3. Start Frontend in new process
Write-Host "[2/2] Starting React + Vite Web Cockpit on http://localhost:5174..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-ExecutionPolicy", "Bypass", "-File", "$PSScriptRoot\run-web.ps1"

Write-Host "Ecosystem launched successfully! Browser: http://localhost:5174" -ForegroundColor Yellow
