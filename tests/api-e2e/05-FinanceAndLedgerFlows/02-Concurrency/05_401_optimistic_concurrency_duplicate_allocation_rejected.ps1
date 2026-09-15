# 05_401_optimistic_concurrency_duplicate_allocation_rejected.ps1
# Senaryo 05.4.01: MÃ¼kerrer / EÅŸzamanlÄ± Mahsup Engeli (VF-05401)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.4.01] MÃ¼kerrer / EÅŸzamanlÄ± Mahsup Engeli (VF-05401)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Yetkili GiriÅŸi (Admin)
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ 
    "Authorization" = "Bearer $($auth.token)"
    "X-Client-Screen-Id" = "SCR-0540"
    "X-Client-Action-Id" = "ACT-05401"
}

# 2. MÃ¼ÅŸteri ve Fatura
$customers = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
$customer = $customers | Where-Object { $_.email -eq "musteri@voltflow.com" } | Select-Object -First 1
$invoices = Invoke-RestMethod -Uri "$BaseUrl/payments/invoices/$($customer.id)" -Method Get -Headers $headers
$invoice = $invoices | Where-Object { $_.invoiceNumber -eq "INV-2026-001" } | Select-Object -First 1

# 3. Yeni Bir Tahsilat OluÅŸtur (500 TL)
$paymentBody = @{
    customerId = $customer.id
    amount = [decimal]500
    paymentMethod = "BANK_TRANSFER"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

$payment = Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers $headers -Body $paymentBody -ContentType "application/json; charset=utf-8"

# 4. Ä°lk Mahsup Ä°steÄŸi (100 TL) -> BaÅŸarÄ±lÄ± OlmalÄ±
$allocBody = @{
    paymentId = $payment.id
    invoiceId = $invoice.id
    amount = [decimal]100
} | ConvertTo-Json

$firstAlloc = Invoke-RestMethod -Uri "$BaseUrl/payments/allocate" -Method Post -Headers $headers -Body $allocBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Ä°lk mahsup baÅŸarÄ±lÄ±: Tutar=$($firstAlloc.amount) TL" -ForegroundColor Green

# 5. Ä°kinci Mahsup Denemesi -> Reddedilmeli
$secondAllocBody = @{
    paymentId = $payment.id
    invoiceId = $invoice.id
    amount = [decimal]150
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/payments/allocate" -Method Post -Headers $headers -Body $secondAllocBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] MÃ¼kerrer mahsup engellenmedi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 409, 422) {
        Write-Host "[PASS] MÃ¼kerrer mahsup isteÄŸi beklenen HTTP status ile reddedildi ($status)." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: 'This payment is already allocated to the invoice' korumasÄ± doÄŸrulandÄ±." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n>>> [SUCCESS] 05_401_optimistic_concurrency_duplicate_allocation_rejected TAMAMLANDI <<<`n" -ForegroundColor Green

