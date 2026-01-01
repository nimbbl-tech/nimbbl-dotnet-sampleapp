# Nimbbl Sample App - Kill Ports Script (Windows PowerShell)
# This script kills any processes running on ports 5000 and 5001

Write-Host "Killing processes on ports 5000 and 5001..." -ForegroundColor Yellow

function Kill-Port {
    param([int]$Port)
    
    $connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    
    if ($connections) {
        $processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($pid in $processIds) {
            $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
            if ($process) {
                Write-Host "Found process on port $Port: $($process.ProcessName) (PID: $pid)" -ForegroundColor Yellow
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                Write-Host "✓ Killed process on port $Port" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "✓ No process found on port $Port" -ForegroundColor Green
    }
}

# Kill processes on both ports
Kill-Port -Port 5000
Kill-Port -Port 5001

Write-Host "Done!" -ForegroundColor Green

