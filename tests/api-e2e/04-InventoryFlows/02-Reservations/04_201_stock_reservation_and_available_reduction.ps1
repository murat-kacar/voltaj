# 04_201_stock_reservation_and_available_reduction.ps1
# Senaryo 04.2.01: Stok Rezervasyonu ve KullanÄ±labilir Miktar DÃ¼ÅŸÃ¼ÅŸÃ¼ (VF-04201)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }
$InventoryUrl = "$BaseUrl/inventory"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.2.01] Stok Rezervasyonu ve KullanÄ±labilir Azalma (VF-04201)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Yetkili GiriÅŸi (Admin)
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ 
    "Authorization" = "Bearer $($auth.token)"
    "X-Client-Screen-Id" = "SCR-0420"
    "X-Client-Action-Id" = "ACT-04201"
}

$materialCode = "NYY-4X16"

# 2. Rezervasyon Ã–ncesi Stok KontrolÃ¼
$before = Invoke-RestMethod -Uri "$InventoryUrl/$materialCode" -Method Get -Headers $headers
$initReserved = [decimal]$before.reservedQuantity
$initAvailable = [decimal]$before.availableQuantity
$reserveAmount = [decimal]5

Write-Host "[INFO] BaÅŸlangÄ±Ã§: Eldeki=$($before.quantityOnHand), Rezerve=$initReserved, KullanÄ±labilir=$initAvailable" -ForegroundColor Gray

# 3. GeÃ§erli Rezervasyon Talebi (POST /api/inventory/reserve)
$reserveHeaders = $headers.Clone()
$reserveHeaders["Idempotency-Key"] = [Guid]::NewGuid().ToString()

$reserveBody = @{
    materialCode = $materialCode
    quantity = $reserveAmount
} | ConvertTo-Json

$resp = Invoke-RestMethod -Uri "$InventoryUrl/reserve" -Method Post -Headers $reserveHeaders -Body $reserveBody -ContentType "application/json; charset=utf-8"

# 4. YanÄ±t ve Matematiksel DoÄŸrulama
$expectedReserved = $initReserved + $reserveAmount
$expectedAvailable = $initAvailable - $reserveAmount

if ([decimal]$resp.reservedQuantity -eq $expectedReserved -and [decimal]$resp.availableQuantity -eq $expectedAvailable) {
    Write-Host "[PASS] Rezervasyon baÅŸarÄ±lÄ±: Rezerve=$($resp.reservedQuantity), KullanÄ±labilir=$($resp.availableQuantity)" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Rezervasyon hesaplama hatasÄ±: Beklenen Rezerve=$expectedReserved, AlÄ±nan=$($resp.reservedQuantity)" -ForegroundColor Red
    exit 1
}

# 5. DB Invariant: GET ile kalÄ±cÄ±lÄ±k doÄŸrulamasÄ±
$after = Invoke-RestMethod -Uri "$InventoryUrl/$materialCode" -Method Get -Headers $headers
if ([decimal]$after.reservedQuantity -eq $expectedReserved -and [decimal]$after.availableQuantity -eq $expectedAvailable) {
    Write-Host "[PASS] DB Invariant: MaterialStock tablosunda rezerve ve kullanÄ±labilir miktar doÄŸrulandÄ±." -ForegroundColor Green
} else {
    Write-Host "[FAIL] DB Durum TutarsÄ±zlÄ±ÄŸÄ±: GET deÄŸerleri uyuÅŸmuyor." -ForegroundColor Red
    exit 1
}

Write-Host "`n>>> [SUCCESS] 04_201_stock_reservation_and_available_reduction TAMAMLANDI <<<`n" -ForegroundColor Green


