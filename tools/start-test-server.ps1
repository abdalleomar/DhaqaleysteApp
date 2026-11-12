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
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$apiDir = Join-Path $repo 'DhaqaleysteApi'
$appSettings = Join-Path $repo 'DhaqaleysteApp\Resources\Raw\appsettings.json'
$tunnelId = 'dhaqaleyste-api'
$port = 5092

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

# 1. Database
Step 'Starting the local database (LocalDB)'
try { sqllocaldb start MSSQLLocalDB | Out-Host } catch { Write-Host 'LocalDB could not be started; the API will show a database error.' -ForegroundColor Yellow }

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
$who = (devtunnel user show 2>&1 | Out-String)
if ($who -notmatch 'Logged in') {
    Step 'Sign in to Dev Tunnels in the browser window that opens (use your Microsoft or GitHub account)'
    devtunnel user login
}

# 4. Create the tunnel once (public, so phones can reach it)
Step "Preparing tunnel '$tunnelId'"
$existing = (devtunnel show $tunnelId 2>&1 | Out-String)
if ($existing -match 'not found|does not exist|Error') {
    devtunnel create $tunnelId --allow-anonymous | Out-Host
    devtunnel port create $tunnelId -p $port | Out-Host
}
elseif ($existing -notmatch "\b$port\b") {
    devtunnel port create $tunnelId -p $port | Out-Host
}

# 5. Start the API in its own window (forwarded headers on: it sits behind the tunnel)
Step 'Starting the API in a new window'
$apiCommand = "Set-Location '$apiDir'; `$env:ForwardedHeaders__Enabled='true'; dotnet run --launch-profile http"
Start-Process powershell -ArgumentList '-NoExit', '-Command', $apiCommand | Out-Null

# 6. Host the tunnel, and write its address into the app when it appears
Step 'Opening the tunnel (keep this window open)'
$written = $false
devtunnel host $tunnelId 2>&1 | ForEach-Object {
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
