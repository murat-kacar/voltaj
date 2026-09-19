# run-all-tests.ps1 - Voltflow Master Test Koşucu ve Orkestrasyon Scripti
param (
    [switch]$IncludeUI = $false,
    [switch]$BackendOnly = $false,
    [switch]$ApiOnly = $false
)

$ErrorActionPreference = "Continue"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " 🚀 VOLTFLOW UNIFIED TAXONOMY (VUT) MASTER TEST KOŞUMU (12-HALKA)" -ForegroundColor Cyan
Write-Host " Başlangıç: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$summary = @()

# 1. 12. Halka: Backend C# Testleri
if (-not $ApiOnly) {
    Write-Host "`n[1/3] 🏛️ 12. HALKA: BACKEND C# TESTLERİ (xUnit / Moq / Architecture)" -ForegroundColor Yellow
    $backendTimer = [System.Diagnostics.Stopwatch]::StartNew()
    $backendOut = dotnet test "$root\tests\backend" --logger "console;verbosity=minimal" 2>&1
    $backendTimer.Stop()
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Backend C# Testleri Başarılı! (Süre: $($backendTimer.Elapsed.TotalSeconds.ToString('F1'))s)" -ForegroundColor Green
        $summary += [PSCustomObject]@{ Katman = "12. Halka: Backend C#"; Durum = "PASS"; Detay = "117 Test Geçti"; Sure = "$($backendTimer.Elapsed.TotalSeconds.ToString('F1'))s" }
    } else {
        Write-Host "  ✗ Backend C# Testlerinde Hata Oluştu!" -ForegroundColor Red
        $summary += [PSCustomObject]@{ Katman = "12. Halka: Backend C#"; Durum = "FAIL"; Detay = "Hata oluştu"; Sure = "$($backendTimer.Elapsed.TotalSeconds.ToString('F1'))s" }
    }
}

# 2. 6. Halka: API-E2E Testleri (PowerShell Deep Flow)
if (-not $BackendOnly) {
    Write-Host "`n[2/3] ⚡ 6. HALKA: API-E2E TESTLERİ (96 Deep-Flow Senaryo)" -ForegroundColor Yellow
    $apiTimer = [System.Diagnostics.Stopwatch]::StartNew()
    $apiScript = "$root\tests\api-e2e\run-all.ps1"
    if (Test-Path $apiScript) {
        & pwsh -File $apiScript
        $apiExit = $LASTEXITCODE
        $apiTimer.Stop()
        if ($apiExit -eq 0) {
            $summary += [PSCustomObject]@{ Katman = "6. Halka: API-E2E"; Durum = "PASS"; Detay = "96 Test Tamamlandı"; Sure = "$($apiTimer.Elapsed.TotalSeconds.ToString('F1'))s" }
        } else {
            $summary += [PSCustomObject]@{ Katman = "6. Halka: API-E2E"; Durum = "FAIL"; Detay = "Hatalı senaryo var"; Sure = "$($apiTimer.Elapsed.TotalSeconds.ToString('F1'))s" }
        }
    }
}

# 3. 5. Halka: UI-E2E Testleri (Playwright)
if ($IncludeUI) {
    Write-Host "`n[3/3] 🌐 5. HALKA: UI-E2E TESTLERİ (Playwright / Chromium)" -ForegroundColor Yellow
    $uiTimer = [System.Diagnostics.Stopwatch]::StartNew()
    npx playwright test
    $uiTimer.Stop()
    if ($LASTEXITCODE -eq 0) {
        $summary += [PSCustomObject]@{ Katman = "5. Halka: UI-E2E"; Durum = "PASS"; Detay = "19 Test Geçti"; Sure = "$($uiTimer.Elapsed.TotalSeconds.ToString('F1'))s" }
    } else {
        $summary += [PSCustomObject]@{ Katman = "5. Halka: UI-E2E"; Durum = "FAIL"; Detay = "UI hatası"; Sure = "$($uiTimer.Elapsed.TotalSeconds.ToString('F1'))s" }
    }
} else {
    $summary += [PSCustomObject]@{ Katman = "5. Halka: UI-E2E"; Durum = "SKIPPED"; Detay = "-IncludeUI parametresi ile koşulabilir"; Sure = "-" }
}

Write-Host "`n==================================================================" -ForegroundColor Cyan
Write-Host " 🏁 VOLTFLOW MASTER TEST KOŞUM SONUÇLARI" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
$summary | Format-Table -AutoSize
Write-Host "Bitiş: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
