Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Starting AutoFno Web Frontend (Vite)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Set-Location -Path "$PSScriptRoot\web\auto-fno-web"
npm run dev
