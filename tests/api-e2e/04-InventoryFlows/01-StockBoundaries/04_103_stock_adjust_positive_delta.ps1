$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }
$InventoryUrl = "$BaseUrl/inventory"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.103] Pozitif Stok Ayarlama (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$matCode = "MAT-ADJ-$(Get-Random -Minimum 1000 -Maximum 9999)"
$adjustBody = @{ materialCode = $matCode; delta = 500 } | ConvertTo-Json
$res = Invoke-RestMethod -Uri "$InventoryUrl/adjust" -Method Post -Headers $headers -Body $adjustBody -ContentType "application/json; charset=utf-8"

if ($res.quantityOnHand -ge 500) {
    Write-Host "[PASS] Stok ayarlama baÅŸarÄ±lÄ± (QuantityOnHand: $($res.quantityOnHand))." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Stok ayarlama beklenen sonucu vermedi!" -ForegroundColor Red
    exit 1
}

