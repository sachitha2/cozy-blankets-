# Cozy Comfort - Start All Services Script (PowerShell)
# Run this script: .\start-services.ps1

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Starting Cozy Comfort Services" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Create logs directory if it doesn't exist
if (-not (Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" | Out-Null
}

# Create pids directory if it doesn't exist
if (-not (Test-Path ".pids")) {
    New-Item -ItemType Directory -Path ".pids" | Out-Null
}

# Function to check if port is available
function Test-Port {
    param([int]$Port)
    $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($connection) {
        Write-Host "Warning: Port $Port is already in use" -ForegroundColor Yellow
        return $false
    }
    return $true
}

# Check ports
Write-Host "Checking ports..."
if (-not (Test-Port 5001)) { exit }
if (-not (Test-Port 5002)) { exit }
if (-not (Test-Port 5003)) { exit }
Write-Host "All ports are available" -ForegroundColor Green
Write-Host ""

# Start ManufacturerService
Write-Host "Starting ManufacturerService on port 5001..." -ForegroundColor Yellow
$manufacturerJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    Set-Location ManufacturerService
    dotnet run *> ..\logs\manufacturer-service.log
}
Write-Host "ManufacturerService started (Job ID: $($manufacturerJob.Id))" -ForegroundColor Green
Start-Sleep -Seconds 3

# Start DistributorService
Write-Host "Starting DistributorService on port 5002..." -ForegroundColor Yellow
$distributorJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    Set-Location DistributorService
    dotnet run *> ..\logs\distributor-service.log
}
Write-Host "DistributorService started (Job ID: $($distributorJob.Id))" -ForegroundColor Green
Start-Sleep -Seconds 3

# Start SellerService
Write-Host "Starting SellerService on port 5003..." -ForegroundColor Yellow
$sellerJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    Set-Location SellerService
    dotnet run *> ..\logs\seller-service.log
}
Write-Host "SellerService started (Job ID: $($sellerJob.Id))" -ForegroundColor Green

# Save job IDs
$manufacturerJob.Id | Out-File -FilePath ".pids\manufacturer.pid"
$distributorJob.Id | Out-File -FilePath ".pids\distributor.pid"
$sellerJob.Id | Out-File -FilePath ".pids\seller.pid"

Write-Host ""
Write-Host "Waiting for services to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "All services started successfully!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services are running:"
Write-Host "  - ManufacturerService: http://localhost:5001"
Write-Host "  - DistributorService:  http://localhost:5002"
Write-Host "  - SellerService:       http://localhost:5003"
Write-Host ""
Write-Host "Logs are being written to:"
Write-Host "  - logs/manufacturer-service.log"
Write-Host "  - logs/distributor-service.log"
Write-Host "  - logs/seller-service.log"
Write-Host ""
Write-Host "To stop all services, run: .\stop-services.ps1"
Write-Host "Or press Ctrl+C and run: .\stop-services.ps1"
Write-Host ""

# Keep script running and show status
try {
    while ($true) {
        Start-Sleep -Seconds 10
        Write-Host "Services are running... (Press Ctrl+C to stop)" -ForegroundColor Gray
    }
}
finally {
    Write-Host "`nStopping services..." -ForegroundColor Yellow
    Get-Job | Stop-Job
    Get-Job | Remove-Job
}
