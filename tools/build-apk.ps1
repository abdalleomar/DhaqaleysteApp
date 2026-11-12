<#
  Builds a signed Android APK of the Dhaqaleyste app and puts it on your Desktop.

  Run from the repository folder in PowerShell (after start-test-server.ps1 wrote the API address):
      powershell -ExecutionPolicy Bypass -File .\tools\build-apk.ps1

  The first time, it creates your signing key in  %USERPROFILE%\DhaqaleysteKeys\dhaqaleyste.keystore
  and asks you to choose a password. KEEP THAT FILE AND PASSWORD SAFE: Google Play needs the same key
  for every future update. Never put it in Git.
#>
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'DhaqaleysteApp\DhaqaleysteApp.csproj'
$appSettings = Join-Path $repo 'DhaqaleysteApp\Resources\Raw\appsettings.json'
$keyDir = Join-Path $env:USERPROFILE 'DhaqaleysteKeys'
$keystore = Join-Path $keyDir 'dhaqaleyste.keystore'
$alias = 'dhaqaleyste'

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

# 1. The app must know where the API is
$settings = Get-Content $appSettings -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($settings.ApiBaseUrl) -or -not $settings.ApiBaseUrl.StartsWith('https://')) {
    throw "No https API address in $appSettings. Run .\tools\start-test-server.ps1 first (or put your server address there)."
}
Write-Host "API address in the app: $($settings.ApiBaseUrl)"

# 2. Password for the signing key (not stored anywhere by this script)
$secure = Read-Host 'Signing key password (at least 6 characters; choose one the first time)' -AsSecureString
$password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
if ($password.Length -lt 6) { throw 'The password must be at least 6 characters.' }
$env:DHAQ_KEY_PASS = $password

# 3. Create the signing key the first time
if (-not (Test-Path $keystore)) {
    Step "Creating your signing key: $keystore"
    $keytool = Get-Command keytool -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    if (-not $keytool) {
        $keytool = Get-ChildItem -Path 'C:\Program Files\Microsoft', 'C:\Program Files (x86)\Android', 'C:\Program Files\Android' `
            -Filter keytool.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $keytool) { throw 'keytool.exe (part of the Java JDK that Visual Studio installs for Android) was not found.' }
    New-Item -ItemType Directory -Force -Path $keyDir | Out-Null
    & $keytool -genkeypair -v -keystore $keystore -alias $alias -keyalg RSA -keysize 2048 -validity 10000 `
        -storepass $password -keypass $password -dname 'CN=Dhaqaleyste, O=Dhaqaleyste, C=SO'
    if ($LASTEXITCODE -ne 0) { throw 'Creating the signing key failed.' }
    Write-Host 'Signing key created. Back up this folder somewhere safe (not in Git):' -ForegroundColor Yellow
    Write-Host "  $keyDir" -ForegroundColor Yellow
}

# 4. Build and sign
Step 'Building the APK (the first time can take 10-20 minutes: it downloads Android build tools)'
# TargetFrameworks limited to Android so restore does not download iOS/Mac/Windows packages.
dotnet publish $project -f net9.0-android -c Release `
    -p:TargetFrameworks=net9.0-android `
    -p:AndroidPackageFormat=apk `
    -p:AndroidKeyStore=true `
    -p:AndroidSigningKeyStore="$keystore" `
    -p:AndroidSigningKeyAlias=$alias `
    -p:AndroidSigningKeyPass=env:DHAQ_KEY_PASS `
    -p:AndroidSigningStorePass=env:DHAQ_KEY_PASS
if ($LASTEXITCODE -ne 0) { throw 'The build failed. Scroll up for the first error and send it to Claude.' }
Remove-Item Env:\DHAQ_KEY_PASS

# 5. Copy to the Desktop
$apk = Get-ChildItem (Join-Path $repo 'DhaqaleysteApp\bin\Release\net9.0-android\publish') -Filter '*-Signed.apk' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $apk) { throw 'The signed APK was not found in the publish folder.' }
$target = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Dhaqaleyste.apk'
Copy-Item $apk.FullName $target -Force

Write-Host ''
Write-Host '========================================================================' -ForegroundColor Green
Write-Host " Done: $target" -ForegroundColor Green
Write-Host ' Send it to phones by WhatsApp or USB and open it to install.' -ForegroundColor Green
Write-Host ' (Android asks to allow installing unknown apps: allow it.)' -ForegroundColor Green
Write-Host '========================================================================' -ForegroundColor Green
