# Cozy Comfort - Stop All Services Script (PowerShell)

Write-Host "Stopping Cozy Comfort Services..." -ForegroundColor Yellow

if (Test-Path ".pids") {
    if (Test-Path ".pids\manufacturer.pid") {
        $jobId = Get-Content ".pids\manufacturer.pid"
        Stop-Job -Id $jobId -ErrorAction SilentlyContinue
        Remove-Job -Id $jobId -ErrorAction SilentlyContinue
        Write-Host "Stopped ManufacturerService" -ForegroundColor Green
    }
    
    if (Test-Path ".pids\distributor.pid") {
        $jobId = Get-Content ".pids\distributor.pid"
        Stop-Job -Id $jobId -ErrorAction SilentlyContinue
        Remove-Job -Id $jobId -ErrorAction SilentlyContinue
        Write-Host "Stopped DistributorService" -ForegroundColor Green
    }
    
    if (Test-Path ".pids\seller.pid") {
        $jobId = Get-Content ".pids\seller.pid"
        Stop-Job -Id $jobId -ErrorAction SilentlyContinue
        Remove-Job -Id $jobId -ErrorAction SilentlyContinue
        Write-Host "Stopped SellerService" -ForegroundColor Green
    }
    
    Remove-Item -Path ".pids" -Recurse -Force -ErrorAction SilentlyContinue
}

# Kill any processes on our ports
Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
Get-NetTCPConnection -LocalPort 5002 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
Get-NetTCPConnection -LocalPort 5003 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }

Write-Host "All services stopped." -ForegroundColor Green
