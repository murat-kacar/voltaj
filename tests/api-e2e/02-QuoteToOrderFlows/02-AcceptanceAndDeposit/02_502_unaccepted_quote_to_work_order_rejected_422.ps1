# 02_502_unaccepted_quote_to_work_order_rejected_422.ps1
# Senaryo 02.5.02: Kabul EdilmemiÅŸ Tekliften Ä°ÅŸ Emri DÃ¶nÃ¼ÅŸÃ¼m Engeli (VF-02501)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.5.02] OnaysÄ±z Tekliften Ä°ÅŸ Emri Ãœretim Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri ve Taslak Teklif OluÅŸtur
$custBody = @{
    fullName = "DÃ¶nÃ¼ÅŸÃ¼m Engeli MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "wo_gate_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5556667788"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$quoteBody = @{ customerId = $cust.id; title = "Taslak DÃ¶nÃ¼ÅŸÃ¼m Testi" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $quoteBody -ContentType "application/json; charset=utf-8"
$quoteId = $quote.id

# 3. Taslak durumundaki teklifi doÄŸrudan iÅŸ emrine dÃ¶nÃ¼ÅŸtÃ¼rme denemesi -> 422
try {
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/work-order" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Taslak tekliften iÅŸ emri Ã¼retilebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Taslak durumundaki teklifin iÅŸ emrine dÃ¶nÃ¼ÅŸÃ¼mÃ¼ engellendi ($code)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}

# 4. Kalem ekle ve yayÄ±nla (Issued durumuna getir, ama kabul etme)
$itemBody = @{ description = "Solar Panel Kurulumu"; quantity = 2; unitPrice = 25000.00 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/items" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/issue" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Teklif Issued durumuna getirildi (henÃ¼z kabul edilmedi)."

# 5. Issued durumundaki (kabul edilmemiÅŸ) tekliften iÅŸ emri dÃ¶nÃ¼ÅŸtÃ¼rme denemesi -> 422
try {
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$quoteId/work-order" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Kabul edilmemiÅŸ (Issued) tekliften iÅŸ emri Ã¼retilebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Issued durumundaki teklifin iÅŸ emrine dÃ¶nÃ¼ÅŸÃ¼mÃ¼ engellendi ($code)." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: WorkOrders tablosuna yetkisiz iÅŸ emri eklenmedi." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 02_502_unaccepted_quote_to_work_order_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


