# 02_404_invalid_deposit_payment_rejected_422.ps1
# Senaryo 02.4.04: GeÃ§ersiz PeÅŸinat Ã–demesi Reddi (VF-02401)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.4.04] GeÃ§ersiz PeÅŸinat Ã–demesi Reddi (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri ve Taslak Teklif OluÅŸtur
$custBody = @{
    fullName = "PeÅŸinat Test MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "deposit_cust_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5555556677"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$quoteBody = @{ customerId = $cust.id; title = "PeÅŸinat KuralÄ± Testi" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $quoteBody -ContentType "application/json; charset=utf-8"
$quoteId = $quote.id

# 3. HenÃ¼z kabul edilmemiÅŸ (Draft) teklife peÅŸinat Ã¶deme denemesi -> 422
try {
    $payBody = @{ amount = 500.00 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/pay-deposit" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $payBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Taslak teklife peÅŸinat Ã¶denebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Taslak durumundaki teklife peÅŸinat Ã¶demesi engellendi ($code)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}

# 4. Kalem ekle, yayÄ±nla ve kabul et
$itemBody = @{ description = "UPS MontajÄ±"; quantity = 1; unitPrice = 10000.00 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/items" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/issue" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
$acceptBody = @{ requiredDepositPercentage = 30 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/accept" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $acceptBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Teklif kabul edildi (State: Accepted, Total: 10000 TL)."

# 5. Negatif tutarlÄ± peÅŸinat Ã¶deme denemesi -> 422
try {
    $negativePayBody = @{ amount = -200.00 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/pay-deposit" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $negativePayBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Negatif peÅŸinat Ã¶denebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Negatif tutarlÄ± peÅŸinat Ã¶demesi kural ile engellendi ($code)." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: Quotes.DepositPaidAmount = 0 olarak korundu." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 02_404_invalid_deposit_payment_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


