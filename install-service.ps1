# =============================
# Install SensorService_Labs
# =============================

$ServiceName = "SensorService_Labs"
$DisplayName = "Sensor Monitor Service - Labs"

# The service EXE now lives under a 'Service' subfolder in the WinUI output
$ExePath = Join-Path $PSScriptRoot "Service\SensorsWorkerService.exe"

# Setup logging
$LogDir  = "$env:ProgramData\A1_SensorService"
$LogFile = Join-Path $LogDir "install_log.txt"
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

Log "Installing $ServiceName from $ExePath..."

# Verify the EXE exists
if (-not (Test-Path $ExePath)) {
    Log "❌ ERROR: service executable not found at:"
    Log "   $ExePath"
    pause
    exit 1
}

# Clean up old logs (.txt and .log)
Get-ChildItem -Path $LogDir -Include *.txt,*.log -File -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

# If the service already exists, stop & delete it
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Log "ℹ️ Existing service detected. Stopping and removing..."
    sc.exe stop $ServiceName | Out-Null
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Create (install) the new service
try {
    New-Service -Name $ServiceName `
                -BinaryPathName "`"$ExePath`"" `
                -DisplayName $DisplayName `
                -StartupType Automatic
    Log "✅ Service '$ServiceName' created."
} catch {
    Log "❌ Failed to create service: $_"
    pause
    exit 1
}

# Start and verify the service
try {
    Start-Service -Name $ServiceName -ErrorAction Stop
    Start-Sleep -Seconds 2
    $status = (Get-Service -Name $ServiceName).Status
    Log "✅ Service started, current status: $status"
} catch {
    Log "⚠️ Service created but failed to start: $_"
}

#pause
