# 02_405_accept_already_accepted_quote_rejected_422.ps1
$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.405] Kabul Edilmiş Teklifi Tekrar Kabul Etme (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

# Müşteri oluştur
$custBody = @{ fullName = "Double Accept Cust $(Get-Random)"; email = "dblaccept_$(Get-Random)@test.com"; phone = "+905009876543" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

# Teklif ve kalem
$quoteBody = @{ customerId = $cust.id; title = "Double Accept Test" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $headers -Body $quoteBody -ContentType "application/json; charset=utf-8"
$itemBody = @{ description = "Kablo Kanalı"; quantity = 50; unitPrice = 10.00 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Headers $headers -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null

# Issue
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers | Out-Null

# İlk kabul (Idempotency Key ile)
$acceptBody = @{ requiredDepositPercentage = 10 } | ConvertTo-Json
$headers1 = @{ "Authorization" = "Bearer $($auth.token)"; "Idempotency-Key" = [guid]::NewGuid().ToString() }
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Headers $headers1 -Body $acceptBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Teklif ilk kez kabul edildi." -ForegroundColor Gray

# İkinci kabul denemesi (Farklı Idempotency Key ile)
$headers2 = @{ "Authorization" = "Bearer $($auth.token)"; "Idempotency-Key" = [guid]::NewGuid().ToString() }
try {
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Headers $headers2 -Body $acceptBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Zaten kabul edilmiş teklif tekrar kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 409, 422) {
        Write-Host "[PASS] Zaten kabul edilmiş teklif tekrar kabul reddedildi (HTTP $status)." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 02_405_accept_already_accepted_quote_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}
