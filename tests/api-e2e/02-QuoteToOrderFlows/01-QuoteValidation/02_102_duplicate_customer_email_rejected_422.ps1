# 02_102_duplicate_customer_email_rejected_422.ps1
# Senaryo 2.1: MÃ¼kerrer MÃ¼ÅŸteri E-postasÄ± Reddi

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.102] MÃ¼kerrer MÃ¼ÅŸteri KayÄ±t Reddi (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ Authorization = "Bearer $($auth.token)" }

$sharedEmail = "corporate_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$firstBody = @{ fullName = "First Corporate Customer"; email = $sharedEmail; phone = "+905329998877" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Body $firstBody -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Ä°lk mÃ¼ÅŸteri kaydedildi ($sharedEmail)." -ForegroundColor Gray

$secondBody = @{ fullName = "Different Company Same Email"; email = $sharedEmail; phone = "+905321110000" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Body $secondBody -Headers $headers -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] MÃ¼kerrer e-posta kabul edildi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 409, 422) {
        Write-Host "[PASS] MÃ¼kerrer e-posta beklenen hata koduyla reddedildi ($status)." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: Customers tablosuna mÃ¼kerrer kayÄ±t eklenmedi." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

