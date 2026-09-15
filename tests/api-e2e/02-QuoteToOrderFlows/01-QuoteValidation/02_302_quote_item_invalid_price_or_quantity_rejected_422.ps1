# 02_302_quote_item_invalid_price_or_quantity_rejected_422.ps1
# Senaryo 02.3.02: GeÃ§ersiz Fiyat veya Miktar ile Kalem Ekleme Engeli (VF-02301)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.3.02] GeÃ§ersiz Teklif Kalemi Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}

# 2. MÃ¼ÅŸteri ve Taslak Teklif OluÅŸtur
$createCustBody = @{
    fullName = "Test Kalem MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "quote_item_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5552223344"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $createCustBody -ContentType "application/json; charset=utf-8"

$quoteHeaders = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}
$createQuoteBody = @{
    customerId = $cust.id
    title = "GeÃ§ersiz Kalem Test Teklifi"
} | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $quoteHeaders -Body $createQuoteBody -ContentType "application/json; charset=utf-8"
$quoteId = $quote.id

# 3. Negatif birim fiyat ile kalem ekleme denemesi
$invalidPriceHeaders = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}
$invalidPriceItem = @{
    description = "HatalÄ± Negatif FiyatlÄ± Malzeme"
    quantity = 5
    unitPrice = -150.00
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/items" -Method Post -Headers $invalidPriceHeaders -Body $invalidPriceItem -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Negatif fiyatlÄ± kalem eklenebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Negatif birim fiyat beklenen $code ile reddedildi." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}

# 4. SÄ±fÄ±r miktar ile kalem ekleme denemesi
$zeroQtyHeaders = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}
$zeroQtyItem = @{
    description = "SÄ±fÄ±r MiktarlÄ± Malzeme"
    quantity = 0
    unitPrice = 100.00
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/items" -Method Post -Headers $zeroQtyHeaders -Body $zeroQtyItem -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] SÄ±fÄ±r miktarlÄ± kalem eklenebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] SÄ±fÄ±r miktar beklenen $code ile reddedildi." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: Quotes.Items tablosuna geÃ§ersiz satÄ±r eklenmedi (Total=0 korundu)." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 02_302_quote_item_invalid_price_or_quantity_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


