param(
    [Parameter(Mandatory = $true)]
    [string]$SourceRoot,
    [Parameter(Mandatory = $true)]
    [string]$DeployRoot,
    [string]$ApiProxyServiceName = "Roblox.ApiProxy",
    [string]$WebsiteServiceName = "Roblox.Website",
    [string]$DataStoreServiceName = "Roblox.Services.DataStore"
)

$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Message)
    Write-Host ""
    Write-Host "== $Message =="
}

function Stop-ServiceIfExists {
    param([string]$Name)

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $service) {
        Write-Warning "Service '$Name' was not found. Skipping stop."
        return
    }

    if ($service.Status -ne "Stopped") {
        Write-Host "Stopping service '$Name'..."
        Stop-Service -Name $Name -Force
        $service.WaitForStatus("Stopped", [TimeSpan]::FromSeconds(45))
    }
}

function Start-ServiceIfExists {
    param([string]$Name)

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $service) {
        Write-Warning "Service '$Name' was not found. Skipping start."
        return
    }

    Write-Host "Starting service '$Name'..."
    Start-Service -Name $Name
    $service.WaitForStatus("Running", [TimeSpan]::FromSeconds(45))
}

function Sync-Directory {
    param(
        [string]$From,
        [string]$To
    )

    if (-not (Test-Path -LiteralPath $From)) {
        throw "Source directory '$From' does not exist."
    }

    New-Item -ItemType Directory -Force -Path $To | Out-Null

    $logPath = Join-Path ([System.IO.Path]::GetTempPath()) ("robocopy-" + [System.Guid]::NewGuid().ToString("N") + ".log")
    $robocopyArgs = @(
        $From
        $To
        "/MIR"
        "/R:2"
        "/W:2"
        "/NFL"
        "/NDL"
        "/NP"
        "/XF"
        "appsettings*.json"
    )

    & robocopy @robocopyArgs | Tee-Object -FilePath $logPath | Out-Null
    $exitCode = $LASTEXITCODE
    if ($exitCode -gt 7) {
        $log = Get-Content -LiteralPath $logPath -Raw
        throw "robocopy failed from '$From' to '$To' with exit code $exitCode.`n$log"
    }

    Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
}

Write-Section "Validating paths"
$SourceRoot = [System.IO.Path]::GetFullPath($SourceRoot)
$DeployRoot = [System.IO.Path]::GetFullPath($DeployRoot)
Write-Host "SourceRoot: $SourceRoot"
Write-Host "DeployRoot: $DeployRoot"

$services = @(
    @{ Name = $ApiProxyServiceName; Folder = "Roblox.ApiProxy" }
    @{ Name = $WebsiteServiceName; Folder = "Roblox.Website" }
    @{ Name = $DataStoreServiceName; Folder = "Roblox.Services.DataStore" }
)

Write-Section "Stopping services"
foreach ($entry in $services) {
    Stop-ServiceIfExists -Name $entry.Name
}

Write-Section "Syncing published output"
foreach ($entry in $services) {
    $from = Join-Path $SourceRoot $entry.Folder
    $to = Join-Path $DeployRoot $entry.Folder
    Write-Host "Syncing '$from' -> '$to'"
    Sync-Directory -From $from -To $to
}

Write-Section "Starting services"
foreach ($entry in $services) {
    Start-ServiceIfExists -Name $entry.Name
}

Write-Section "Cleanup"
if (Test-Path -LiteralPath $SourceRoot) {
    Remove-Item -LiteralPath $SourceRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Deployment complete."
