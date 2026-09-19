# 04_101_negative_stock_gate_and_atomic_rollback.ps1
# Senaryo 04.1.01: Negatif Stok Bariyeri ve Atomik Geri Alma

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }
$InventoryUrl = "$BaseUrl/inventory"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.1.01] Negatif Stok Bariyeri ve Atomik Geri Alma" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$matCode = "MAT-NEG-$(Get-Random -Minimum 1000 -Maximum 9999)"

# Stok oluÅŸtur (100 adet)
$adjustBody = @{ materialCode = $matCode; delta = 100 } | ConvertTo-Json
$stock = Invoke-RestMethod -Uri "$InventoryUrl/adjust" -Method Post -Headers $headers -Body $adjustBody -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] BaÅŸlangÄ±Ã§ stoÄŸu: $($stock.quantityOnHand)" -ForegroundColor Gray

$qtyBefore = [decimal]$stock.quantityOnHand

# Negatif ayarlama ile stok altÄ±na dÃ¼ÅŸÃ¼rme denemesi (-99999)
$invalidAdjustBody = @{ materialCode = $matCode; delta = -99999 } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$InventoryUrl/adjust" -Method Post -Headers $headers -Body $invalidAdjustBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Negatif stok ayarlamasÄ± kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] Negatif stok bariyeri aktif ($status)." -ForegroundColor Green

        # DB Invariant: Stok deÄŸiÅŸmemiÅŸ olmalÄ±
        $stockAfter = Invoke-RestMethod -Uri "$InventoryUrl/$matCode" -Method Get -Headers $headers
        if ([decimal]$stockAfter.quantityOnHand -eq $qtyBefore) {
            Write-Host "[PASS] DB Invariant: Stok miktarÄ± deÄŸiÅŸmedi ($($stockAfter.quantityOnHand))." -ForegroundColor Green
            exit 0
        } else {
            Write-Host "[FAIL] DB HatasÄ±: Stok deÄŸiÅŸti! $($stockAfter.quantityOnHand) != $qtyBefore" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

