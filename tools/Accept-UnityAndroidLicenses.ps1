param(
    [string]$UnityVersion = "6000.3.14f1"
)

$ErrorActionPreference = "Stop"

$sdkManager = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\cmdline-tools\latest\bin\sdkmanager.bat"

if (-not (Test-Path $sdkManager)) {
    Write-Error "Nie znaleziono sdkmanager.bat: $sdkManager"
}

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)

if (-not $isAdmin) {
    Write-Host "Uruchom ten skrypt jako administrator." -ForegroundColor Yellow
    Write-Host "PPM na PowerShell -> Uruchom jako administrator, potem:"
    Write-Host "  powershell -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit 1
}

Write-Host "Akceptowanie licencji Android SDK dla Unity $UnityVersion..." -ForegroundColor Cyan
Write-Host "Wpisuj 'y' przy kolejnych pytaniach sdkmanagera." -ForegroundColor Cyan
& $sdkManager --licenses

Write-Host ""
Write-Host "Gotowe. Teraz mozna ponownie zbudowac APK w Unity/Codex." -ForegroundColor Green
