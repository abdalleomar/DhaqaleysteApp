<#
  Starts everything needed to test the Dhaqaleyste app on real phones:
    1. the local database (LocalDB)
    2. the API (in its own window)
    3. a public https Dev Tunnel to the API
  and writes the tunnel address into the app (Resources/Raw/appsettings.json).

  Run from the repository folder in PowerShell:
      powershell -ExecutionPolicy Bypass -File .\tools\start-test-server.ps1

  Keep this window open while people test. Press Ctrl+C here to stop the tunnel.
#>
# Native tools (devtunnel, sqllocaldb) write normal messages to stderr; do not treat those as script errors.
$ErrorActionPreference = 'Continue'
$repo = Split-Path -Parent $PSScriptRoot
$apiDir = Join-Path $repo 'DhaqaleysteApi'
$appSettings = Join-Path $repo 'DhaqaleysteApp\Resources\Raw\appsettings.json'
$tunnelId = 'dhaqaleyste-api'
$port = 5092

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

# 1. Database
Step 'Starting the local database (LocalDB)'
sqllocaldb start MSSQLLocalDB
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'LocalDB did not start, so the API cannot reach the database.' -ForegroundColor Yellow
    Write-Host 'Send Claude the last lines of this file:' -ForegroundColor Yellow
    Write-Host "  $env:LOCALAPPDATA\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\error.log" -ForegroundColor Yellow
    Write-Host 'Continuing with the tunnel anyway.' -ForegroundColor Yellow
}

# 2. Dev Tunnels command-line tool
Step 'Checking the Dev Tunnels tool'
if (-not (Get-Command devtunnel -ErrorAction SilentlyContinue)) {
    Write-Host 'Installing the Dev Tunnels tool (one time)...'
    winget install --id Microsoft.devtunnel -e --accept-source-agreements --accept-package-agreements
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
    if (-not (Get-Command devtunnel -ErrorAction SilentlyContinue)) {
        throw 'The Dev Tunnels tool was installed, but this window cannot see it yet. Close PowerShell, open it again and run this script again.'
    }
}

# 3. Sign in (one time; opens the browser)
$who = (devtunnel user show | Out-String)
if ($LASTEXITCODE -ne 0 -or $who -match 'Not logged in|Login required' -or $who -notmatch 'Logged in as') {
    Step 'Sign in to Dev Tunnels in the browser window that opens (use your Microsoft or GitHub account)'
    devtunnel user login
    if ($LASTEXITCODE -ne 0) { throw 'Dev Tunnels sign-in did not complete. Run the script again.' }
}

# 4. Create the tunnel once (public, so phones can reach it)
Step "Preparing tunnel '$tunnelId'"
devtunnel show $tunnelId *> $null
if ($LASTEXITCODE -ne 0) {
    devtunnel create $tunnelId --allow-anonymous
    if ($LASTEXITCODE -ne 0) { throw "Could not create the tunnel '$tunnelId'." }
}
devtunnel port show $tunnelId -p $port *> $null
if ($LASTEXITCODE -ne 0) {
    devtunnel port create $tunnelId -p $port
    if ($LASTEXITCODE -ne 0) { throw "Could not add port $port to the tunnel." }
}

# 5. Start the API in its own window (forwarded headers on: it sits behind the tunnel)
Step 'Starting the API in a new window'
# An API that is still running (from Visual Studio or an earlier run) blocks the new build.
Get-Process -Name DhaqaleysteApi -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Stopping an API that was already running (process $($_.Id))"
    Stop-Process -Id $_.Id -Force
}
Start-Sleep -Seconds 1
$apiCommand = "Set-Location '$apiDir'; `$env:ForwardedHeaders__Enabled='true'; dotnet run --launch-profile http"
Start-Process powershell -ArgumentList '-NoExit', '-Command', $apiCommand | Out-Null

# 6. Host the tunnel, and write its address into the app when it appears
Step 'Opening the tunnel (keep this window open)'
$written = $false
devtunnel host $tunnelId | ForEach-Object {
    $line = "$_"
    Write-Host $line
    if (-not $written -and $line -match '(https://[A-Za-z0-9.-]+devtunnels\.ms)' -and $line -notmatch 'inspect') {
        $url = $Matches[1].TrimEnd('/') + '/'
        $json = @{ ApiBaseUrl = $url } | ConvertTo-Json
        [IO.File]::WriteAllText($appSettings, $json, (New-Object System.Text.UTF8Encoding $false))
        $written = $true
        Write-Host ''
        Write-Host '========================================================================' -ForegroundColor Green
        Write-Host " API address for phones: $url" -ForegroundColor Green
        Write-Host " Saved into DhaqaleysteApp\Resources\Raw\appsettings.json" -ForegroundColor Green
        Write-Host " Check it in a browser:  ${url}health   (click Continue if asked)" -ForegroundColor Green
        Write-Host ' Next: build the APK with  .\tools\build-apk.ps1  in another PowerShell window.' -ForegroundColor Green
        Write-Host '========================================================================' -ForegroundColor Green
    }
}
