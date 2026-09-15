# 04_202_insufficient_stock_reservation_rejected.ps1
# Senaryo 04.2.02: Yetersiz Stok Rezervasyonu Reddi

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"
$InventoryUrl = "$BaseUrl/inventory"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.2.02] Yetersiz Stok Rezervasyonu Reddi" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

# Yeni malzeme kodu ile kÃ¼Ã§Ã¼k stok oluÅŸtur
$matCode = "MAT-INSUF-$(Get-Random -Minimum 1000 -Maximum 9999)"
$adjustBody = @{ materialCode = $matCode; delta = 10 } | ConvertTo-Json
Invoke-RestMethod -Uri "$InventoryUrl/adjust" -Method Post -Headers $headers -Body $adjustBody -ContentType "application/json; charset=utf-8" | Out-Null

# Stoktan fazla rezervasyon dene
$failBody = @{ materialCode = $matCode; quantity = 99999 } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$InventoryUrl/reserve" -Method Post -Headers $headers -Body $failBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Yetersiz stok rezervasyonu kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] Yetersiz stok rezervasyonu reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

