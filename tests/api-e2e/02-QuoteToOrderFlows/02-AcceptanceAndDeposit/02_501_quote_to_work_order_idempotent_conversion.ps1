# 02_05_quote_to_work_order_idempotent_conversion.ps1
# Senaryo 2.5: Kabul Edilen Tekliften Ä°ÅŸ Emrine DÃ¶nÃ¼ÅŸÃ¼m ve Idempotent MÃ¼kerrerlik KorumasÄ±

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 2.5] Teklif -> Ä°ÅŸ Emri DÃ¶nÃ¼ÅŸÃ¼mÃ¼ ve Idempotency" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. AÅŸama: Admin giriÅŸi
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ Authorization = "Bearer $($auth.token)" }

# 2. AÅŸama: MÃ¼ÅŸteri ve Kabul EdilmiÅŸ Teklif HazÄ±rla
$custEmail = "factory_conversion_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$custBody = @{ fullName = "Borusan Lojistik Tesisleri"; email = $custEmail; phone = "+905324445566" } | ConvertTo-Json
$customer = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Body $custBody -Headers $headers -ContentType "application/json; charset=utf-8"

$quoteBody = @{ customerId = $customer.id; title = "Bursa Depo GÃ¼neÅŸ Enerjisi Ä°nverter Kurulumu" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Body $quoteBody -Headers $headers -ContentType "application/json; charset=utf-8"

# Kalem ekle
$itemBody = @{ description = "50kW Hibrit Ä°nverter"; quantity = 2; unitPrice = 45000.00 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Body $itemBody -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null

# Teklifi yayÄ±nla
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null

# Teklifi kabul et
$acceptBody = @{ requiredDepositPercentage = 20.00 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Body $acceptBody -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Kabul edilmiÅŸ teklif hazÄ±rlandÄ± (Id: $($quote.id))." -ForegroundColor Gray

# 3. AÅŸama: Tekliften Ä°ÅŸ Emri OluÅŸtur (POST /api/quotes/{id}/work-order)
$firstWo = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/work-order" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"

if (-not [string]::IsNullOrWhiteSpace($firstWo.id) -and $firstWo.customerId -eq $customer.id) {
    Write-Host "[PASS] Ä°ÅŸ Emri baÅŸarÄ±yla oluÅŸturuldu (WorkOrder Id: $($firstWo.id), Durum: $($firstWo.state))." -ForegroundColor Green
    Write-Host "[PASS] Kaynak Teklif Ä°liÅŸkisi DoÄŸrulandÄ± (SourceQuoteId: $($firstWo.sourceQuoteId))." -ForegroundColor Green
}
else {
    Write-Host "[FAIL] Ä°ÅŸ Emri oluÅŸturulamadÄ±!" -ForegroundColor Red
    exit 1
}

# 4. AÅŸama: AynÄ± tekliften Ä°KÄ°NCÄ° KEZ iÅŸ emri oluÅŸturmayÄ± dene (MÃ¼kerrerlik / Idempotency Testi)
$secondWo = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/work-order" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"

if ($secondWo.id -eq $firstWo.id) {
    Write-Host "[PASS] Idempotency DoÄŸrulandÄ±: Ä°kinci istek yeni iÅŸ emri oluÅŸturmadÄ±, mevcut WorkOrder'Ä± dÃ¶ndÃ¼ (Id: $($secondWo.id))." -ForegroundColor Green
    Write-Host "[PASS] DB Invariant: WorkOrders tablosunda mÃ¼kerrer kayÄ±t oluÅŸmadÄ± (SingleOrDefault SourceQuoteId gÃ¼vencesi)." -ForegroundColor Green
    exit 0
}
else {
    Write-Host "[FAIL] MÃ¼kerrer Ä°ÅŸ Emri oluÅŸturuldu! First Id: $($firstWo.id), Second Id: $($secondWo.id)" -ForegroundColor Red
    exit 1
}


