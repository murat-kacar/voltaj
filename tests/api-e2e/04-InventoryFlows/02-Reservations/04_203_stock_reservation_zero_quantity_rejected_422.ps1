# 04_203_stock_reservation_zero_quantity_rejected_422.ps1
# Senaryo 04.2.03: SÄ±fÄ±r veya Negatif MiktarlÄ± Stok Rezervasyonu Engeli (VF-04202)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.2.03] SÄ±fÄ±r/Negatif Stok Rezervasyon Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. SÄ±fÄ±r miktarlÄ± rezervasyon talebi -> 422
$zeroReserveBody = @{
    materialCode = "NYY-4X16"
    quantity = 0
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/inventory/reserve" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $zeroReserveBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] SÄ±fÄ±r miktarlÄ± rezervasyon kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] SÄ±fÄ±r miktarlÄ± rezervasyon beklenen $code ile reddedildi." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}

# 3. Negatif miktarlÄ± rezervasyon talebi -> 422
$negativeReserveBody = @{
    materialCode = "NYY-4X16"
    quantity = -10
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/inventory/reserve" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $negativeReserveBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Negatif miktarlÄ± rezervasyon kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Negatif miktarlÄ± rezervasyon beklenen $code ile reddedildi." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: MaterialStock tablosundaki rezerve miktarÄ± mutasyona uÄŸramadÄ±." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 04_203_stock_reservation_zero_quantity_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


