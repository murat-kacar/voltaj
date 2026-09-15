# 05_301_payment_allocation_to_invoice.ps1
# Senaryo 05.3.01: TahsilatÄ±n Faturaya Mahsubu (VF-05301)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.3.01] Tahsilat Fatura Mahsubu (VF-05301)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Yetkili GiriÅŸi (Admin)
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ 
    "Authorization" = "Bearer $($auth.token)"
    "X-Client-Screen-Id" = "SCR-0530"
    "X-Client-Action-Id" = "ACT-05301"
}

# 2. MÃ¼ÅŸteri ve Fatura Bilgisi Alma
$customers = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
$customer = $customers | Where-Object { $_.email -eq "musteri@voltflow.com" } | Select-Object -First 1
if (-not $customer) {
    Write-Host "[FAIL] Seed mÃ¼ÅŸteri bulunamadÄ±!" -ForegroundColor Red
    exit 1
}

$invoices = Invoke-RestMethod -Uri "$BaseUrl/payments/invoices/$($customer.id)" -Method Get -Headers $headers
$invoice = $invoices | Where-Object { $_.invoiceNumber -eq "INV-2026-001" } | Select-Object -First 1
if (-not $invoice) {
    Write-Host "[FAIL] Seed fatura INV-2026-001 bulunamadÄ±!" -ForegroundColor Red
    exit 1
}

$initialRemaining = [decimal]$invoice.remainingAmount
$initialPaid = [decimal]$invoice.paidAmount
Write-Host "[INFO] MÃ¼ÅŸteri: $($customer.fullName), Fatura No: $($invoice.invoiceNumber) (Kalan: $initialRemaining TL, Ã–denen: $initialPaid TL)" -ForegroundColor Gray

# 3. Yeni Bir Tahsilat OluÅŸtur (100 TL)
$allocateAmount = [decimal]100
$paymentBody = @{
    customerId = $customer.id
    amount = $allocateAmount
    paymentMethod = "BANK_TRANSFER"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

$headers["Idempotency-Key"] = [guid]::NewGuid().ToString()
$payment = Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers $headers -Body $paymentBody -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Tahsilat oluÅŸturuldu: ID=$($payment.id), Tutar=$($payment.amount) TL" -ForegroundColor Gray

# 4. TahsilatÄ± Faturaya Mahsup Et (POST /api/payments/allocate)
$allocBody = @{
    paymentId = $payment.id
    invoiceId = $invoice.id
    amount = $allocateAmount
} | ConvertTo-Json

$headers["Idempotency-Key"] = [guid]::NewGuid().ToString()
$allocResult = Invoke-RestMethod -Uri "$BaseUrl/payments/allocate" -Method Post -Headers $headers -Body $allocBody -ContentType "application/json; charset=utf-8"

if ($allocResult.paymentId -eq $payment.id -and [decimal]$allocResult.amount -eq $allocateAmount) {
    Write-Host "[PASS] Mahsup iÅŸlemi baÅŸarÄ±yla kaydedildi: Tutar=$($allocResult.amount) TL" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Mahsup yanÄ±tÄ± beklenen deÄŸerleri iÃ§ermiyor!" -ForegroundColor Red
    exit 1
}

# 5. DB Invariant: Fatura Kalan TutarÄ±nÄ±n AzaldÄ±ÄŸÄ±nÄ± DoÄŸrula
$invoicesAfter = Invoke-RestMethod -Uri "$BaseUrl/payments/invoices/$($customer.id)" -Method Get -Headers $headers
$invoiceAfter = $invoicesAfter | Where-Object { $_.id -eq $invoice.id } | Select-Object -First 1

$expectedRemaining = $initialRemaining - $allocateAmount
$expectedPaid = $initialPaid + $allocateAmount
if ([decimal]$invoiceAfter.remainingAmount -eq $expectedRemaining -and [decimal]$invoiceAfter.paidAmount -eq $expectedPaid) {
    Write-Host "[PASS] DB Invariant: Fatura Ã¶denen tutarÄ± $($invoiceAfter.paidAmount) TL, kalan tutarÄ± $expectedRemaining TL olarak gÃ¼ncellendi." -ForegroundColor Green
} else {
    Write-Host "[FAIL] DB Durum HatasÄ±: Kalan fatura tutarÄ± hesaplananla uyuÅŸmuyor: $($invoiceAfter.remainingAmount)" -ForegroundColor Red
    exit 1
}

Write-Host "`n>>> [SUCCESS] 05_301_payment_allocation_to_invoice TAMAMLANDI <<<`n" -ForegroundColor Green


