# 04_102_get_stock_by_material_code_and_404.ps1
# Senaryo 04.1.02: Malzeme Kodu ile Stok Sorgulama ve 404 KorumasÄ± (VF-04101)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.1.02] Stok Sorgulama ve 404 DoÄŸrulamasÄ± (VF-04101)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. Sistemde kayÄ±tlÄ± olan malzemeyi sorgula (NYY-4X16)
$materialCode = "NYY-4X16"
$stockResp = Invoke-RestMethod -Uri "$BaseUrl/inventory/$materialCode" -Method Get -Headers $headers

if ($stockResp.materialCode -eq $materialCode -and $stockResp.quantityOnHand -ge 0) {
    Write-Host "[PASS] KayÄ±tlÄ± malzeme baÅŸarÄ±yla sorgulandÄ±: $materialCode (Eldeki: $($stockResp.quantityOnHand), Rezerve: $($stockResp.reservedQuantity), KullanÄ±labilir: $($stockResp.availableQuantity))." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Malzeme stok yanÄ±tÄ± beklenen alanlarÄ± iÃ§ermiyor!" -ForegroundColor Red
    exit 1
}

# 3. Sistemde var olmayan hayali bir malzeme kodunu sorgula -> 404 bekliyoruz
$nonExistentCode = "HAYALI_MALZEME_KODU_9999"
try {
    Invoke-RestMethod -Uri "$BaseUrl/inventory/$nonExistentCode" -Method Get -Headers $headers | Out-Null
    Write-Host "[FAIL] Var olmayan malzeme iÃ§in 404 dÃ¶nmedi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 404) {
        Write-Host "[PASS] Var olmayan malzeme sorgusu beklenen 404 NotFound dÃ¶ndÃ¼." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 04_102_get_stock_by_material_code_and_404 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


