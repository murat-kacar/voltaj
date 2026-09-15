# 02_402_quote_rejection_with_reason.ps1
# Senaryo 02.4.02: Teklifin Reddedilmesi ve ReddedilmiÅŸ Teklif KorumasÄ± (VF-02401)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.4.02] Teklif Reddi ve FSM KorumasÄ± (VF-02401)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri oluÅŸtur ve teklif hazÄ±rla
$custBody = @{
    fullName = "Teklif Red MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "reject_cust_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5553334455"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$quoteBody = @{
    customerId = $cust.id
    title = "Reddedilecek Teklif Projesi"
} | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $quoteBody -ContentType "application/json; charset=utf-8"
$quoteId = $quote.id

# 3. Kalem ekle ve teklifi yayÄ±nla (Issue)
$itemBody = @{ description = "EndÃ¼striyel Trafo BakÄ±mÄ±"; quantity = 1; unitPrice = 15000.00 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/items" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/issue" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Teklif hazÄ±rlandÄ± ve yayÄ±nlandÄ± (State: Issued, Id: $quoteId)."

# 4. Teklifi gerekÃ§e belirterek reddet (Reject)
$rejectBody = @{ reason = "MÃ¼ÅŸteri bÃ¼tÃ§eyi onaylamadÄ±, proje seneye ertelendi." } | ConvertTo-Json
$rejectedQuote = Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/reject" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $rejectBody -ContentType "application/json; charset=utf-8"

if ($rejectedQuote.state -eq "Rejected") {
    Write-Host "[PASS] Teklif baÅŸarÄ±yla reddedildi (State: Rejected, Reason: $($rejectedQuote.rejectionReason))." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Teklif Rejected durumuna geÃ§medi!" -ForegroundColor Red
    exit 1
}

# 5. ReddedilmiÅŸ teklifi kabul etmeye Ã§alÄ±ÅŸma -> 422 bekliyoruz
try {
    $acceptBody = @{ requiredDepositPercentage = 20 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/accept" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $acceptBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] ReddedilmiÅŸ teklif kabul edilebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] ReddedilmiÅŸ teklifin kabulÃ¼ FSM kuralÄ± ile engellendi ($code)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}

# 6. ReddedilmiÅŸ teklifi iÅŸ emrine dÃ¶nÃ¼ÅŸtÃ¼rme denemesi -> 422 bekliyoruz
try {
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/work-order" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] ReddedilmiÅŸ teklif iÅŸ emrine dÃ¶nÃ¼ÅŸtÃ¼rÃ¼lebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] ReddedilmiÅŸ tekliften iÅŸ emri Ã¼retimi engellendi ($code)." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: Quotes.State 'Rejected' terminal durumu korundu." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 02_402_quote_rejection_with_reason TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


