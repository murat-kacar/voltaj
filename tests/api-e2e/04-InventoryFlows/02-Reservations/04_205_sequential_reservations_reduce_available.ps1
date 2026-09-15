$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"
$InventoryUrl = "$BaseUrl/inventory"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.205] ArdÄ±ÅŸÄ±k Rezervasyonlar MÃ¼sait StoÄŸu AzaltÄ±r" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$matCode = "MAT-SEQ-$(Get-Random -Minimum 1000 -Maximum 9999)"

# Stok oluÅŸtur (1000 adet)
$adjustBody = @{ materialCode = $matCode; delta = 1000 } | ConvertTo-Json
Invoke-RestMethod -Uri "$InventoryUrl/adjust" -Method Post -Headers $headers -Body $adjustBody -ContentType "application/json; charset=utf-8" | Out-Null

# 1. Rezervasyon (300)
$res1Body = @{ materialCode = $matCode; quantity = 300 } | ConvertTo-Json
Invoke-RestMethod -Uri "$InventoryUrl/reserve" -Method Post -Headers $headers -Body $res1Body -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[PASS] 1. rezervasyon yapÄ±ldÄ± (300 adet)." -ForegroundColor Green

# 2. Rezervasyon (400)
$res2Body = @{ materialCode = $matCode; quantity = 400 } | ConvertTo-Json
Invoke-RestMethod -Uri "$InventoryUrl/reserve" -Method Post -Headers $headers -Body $res2Body -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[PASS] 2. rezervasyon yapÄ±ldÄ± (400 adet)." -ForegroundColor Green

# Stok kontrolÃ¼
$stock = Invoke-RestMethod -Uri "$InventoryUrl/$matCode" -Method Get -Headers $headers
if ($stock.reservedQuantity -ge 700) {
    Write-Host "[PASS] Toplam rezerve miktar doÄŸru (Reserved: $($stock.reservedQuantity))." -ForegroundColor Green
    Write-Host "[PASS] MÃ¼sait miktar: $($stock.availableQuantity)." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Rezervasyon toplamÄ± yanlÄ±ÅŸ! Reserved: $($stock.reservedQuantity)" -ForegroundColor Red
    exit 1
}

