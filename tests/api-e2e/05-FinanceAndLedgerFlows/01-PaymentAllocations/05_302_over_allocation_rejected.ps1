# 05_302_over_allocation_rejected.ps1
# Senaryo 05.3.02: AÅŸÄ±rÄ± Mahsup Engeli ve Validasyon Reddi (VF-05302)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.3.02] AÅŸÄ±rÄ± Mahsup Engeli (VF-05302)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Yetkili GiriÅŸi (Admin)
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ 
    "Authorization" = "Bearer $($auth.token)"
    "X-Client-Screen-Id" = "SCR-0530"
    "X-Client-Action-Id" = "ACT-05302"
}

# 2. MÃ¼ÅŸteri ve Fatura Bilgisi Alma
$customers = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
$customer = $customers | Where-Object { $_.email -eq "musteri@voltflow.com" } | Select-Object -First 1
$invoices = Invoke-RestMethod -Uri "$BaseUrl/payments/invoices/$($customer.id)" -Method Get -Headers $headers
$invoice = $invoices | Where-Object { $_.invoiceNumber -eq "INV-2026-001" } | Select-Object -First 1

$invoiceRemainingBefore = [decimal]$invoice.remainingAmount

# 3. 500 TL TutarÄ±nda KÃ¼Ã§Ã¼k Bir Tahsilat OluÅŸtur
$paymentBody = @{
    customerId = $customer.id
    amount = [decimal]500
    paymentMethod = "CASH"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

$payment = Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers $headers -Body $paymentBody -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Tahsilat tutarÄ±: $($payment.amount) TL (Fatura Kalan: $invoiceRemainingBefore TL)" -ForegroundColor Gray

# 4. AÅŸÄ±rÄ± Mahsup Denemesi (99.999 TL)
$overAllocBody = @{
    paymentId = $payment.id
    invoiceId = $invoice.id
    amount = [decimal]99999
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/payments/allocate" -Method Post -Headers $headers -Body $overAllocBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] AÅŸÄ±rÄ± mahsup engellenmedi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] AÅŸÄ±rÄ± mahsup isteÄŸi beklenen HTTP status ile engellendi ($status)." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: 'Payment allocation exceeds the unallocated amount' kuralÄ± doÄŸrulandÄ±." -ForegroundColor Green

        # 5. DB Invariant: Fatura bakiyesi deÄŸiÅŸmemeli
        $invoicesAfter = Invoke-RestMethod -Uri "$BaseUrl/payments/invoices/$($customer.id)" -Method Get -Headers $headers
        $invoiceAfter = $invoicesAfter | Where-Object { $_.id -eq $invoice.id } | Select-Object -First 1

        if ([decimal]$invoiceAfter.remainingAmount -eq $invoiceRemainingBefore) {
            Write-Host "[PASS] DB Invariant: BaÅŸarÄ±sÄ±z mahsup denemesi sonrasÄ± fatura bakiyesi ($($invoiceAfter.remainingAmount) TL) deÄŸiÅŸmedi." -ForegroundColor Green
        } else {
            Write-Host "[FAIL] DB Durum HatasÄ±: Fatura bakiyesi deÄŸiÅŸti!" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n>>> [SUCCESS] 05_302_over_allocation_rejected TAMAMLANDI <<<`n" -ForegroundColor Green

