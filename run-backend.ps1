Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Starting AutoFno .NET 10 Web API Backend" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Set-Location -Path "$PSScriptRoot\backend\AutoFno.Api"
dotnet run
