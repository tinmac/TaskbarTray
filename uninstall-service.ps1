# =============================
# Uninstall PowerSwitchService
# =============================

$ServiceName = "PowerSwitchService"

# Setup logging
$LogDir  = "$env:ProgramData\PowerSwitch"
$LogFile = Join-Path $LogDir "service_setup_log.txt"
if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

function Log {
    param ([string]$msg)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$timestamp] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

Log "Uninstalling $ServiceName..."

# Check if the service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    # Stop the service if running
    if ($service.Status -eq 'Running') {
        Log "Stopping $ServiceName..."
        try {
            Stop-Service -Name $ServiceName -Force -ErrorAction Stop
            Log "Service stopped."
        } catch {
            Log "?? Failed to stop service: $_"
        }
    }
    # Delete the service
    Log "Deleting $ServiceName..."
    try {
        sc.exe delete $ServiceName | Out-Null
        Log "? Service deleted."
    } catch {
        Log "? Failed to delete service: $_"
        pause
        exit 1
    }
} else {
    Log "?? Service $ServiceName does not exist. Nothing to uninstall."
}

# Clean up old logs (.txt and .log) if desired
# Get-ChildItem -Path $LogDir -Include *.txt,*.log -File -ErrorAction SilentlyContinue |
#     Remove-Item -Force -ErrorAction SilentlyContinue

#pause
