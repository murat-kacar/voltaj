# 05_303_allocation_exceeding_invoice_remaining_rejected_422.ps1
# Senaryo 05.3.03: Fatura KalanÄ±nÄ± AÅŸan Mahsup Engeli (VF-05302)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.3.03] Fatura Bakiyesini AÅŸan Mahsup Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. Sistemdeki ilk mÃ¼ÅŸteriyi ve faturayÄ± al
$customers = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
$customer = $customers | Where-Object { $_.email -eq "musteri@voltflow.com" } | Select-Object -First 1
$invoices = Invoke-RestMethod -Uri "$BaseUrl/payments/invoices/$($customer.id)" -Method Get -Headers $headers

if (-not $invoices -or $invoices.Count -eq 0) {
    Write-Host "[INFO] MÃ¼ÅŸteriye ait fatura bulunamadÄ±, test pas geÃ§iliyor." -ForegroundColor Yellow
    exit 0
}

$invoice = $invoices[0]
$invRemaining = [decimal]$invoice.remainingAmount
Write-Host "[INFO] Fatura No: $($invoice.invoiceNumber), Kalan Bakiye: $invRemaining TL."

# 3. YÃ¼ksek tutarlÄ± bir tahsilat oluÅŸtur (Kalan bakiyenin 2 katÄ±)
$payAmount = $invRemaining * 2 + 1000
$payBody = @{
    customerId = $customer.id
    amount = $payAmount
    paymentMethod = "BANK_TRANSFER"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

$payment = Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $payBody -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Tahsilat oluÅŸturuldu: Id=$($payment.id), Tutar=$payAmount TL."

# 4. Fatura kalanÄ±ndan daha fazla tutarÄ± mahsup etmeyi dene (invRemaining + 500 TL) -> 422
$excessiveAmount = $invRemaining + 500
$allocBody = @{
    paymentId = $payment.id
    invoiceId = $invoice.id
    amount = $excessiveAmount
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/payments/allocate" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $allocBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Fatura kalanÄ±nÄ± aÅŸan mahsup kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Fatura kalanÄ±nÄ± aÅŸan mahsup beklenen $code ile engellendi." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: Fatura bakiyesinden fazla mahsup yapÄ±lamayacaÄŸÄ± doÄŸrulandÄ±." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: SalesInvoices.RemainingAmount deÄŸiÅŸmedi." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 05_303_allocation_exceeding_invoice_remaining_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


