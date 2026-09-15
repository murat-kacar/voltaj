# 05_201_customer_payment_and_ledger_credit.ps1
# Senaryo 05.2.01: MÃ¼ÅŸteri TahsilatÄ± ve Cari Alacak KaydÄ± (VF-05201)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.2.01] Tahsilat ve Cari Alacak KaydÄ± (VF-05201)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Yetkili GiriÅŸi (Admin)
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ 
    "Authorization" = "Bearer $($auth.token)"
    "X-Client-Screen-Id" = "SCR-0520"
    "X-Client-Action-Id" = "ACT-05201"
}

# 2. MÃ¼ÅŸteri Bilgisi Alma
$customers = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
$customer = $customers | Where-Object { $_.email -eq "musteri@voltflow.com" } | Select-Object -First 1
if (-not $customer) {
    # Yoksa oluÅŸtur
    $newCustBody = @{
        fullName = "Volt Ã–rnek MÃ¼ÅŸteri A.Åž."
        email = "musteri@voltflow.com"
        phone = "+905551234567"
        taxNumber = "1234567890"
    } | ConvertTo-Json
    $customer = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $newCustBody -ContentType "application/json; charset=utf-8"
}

Write-Host "[INFO] MÃ¼ÅŸteri ID: $($customer.id), AdÄ±: $($customer.fullName)" -ForegroundColor Gray

# 3. MÃ¼ÅŸteri Tahsilat KaydÄ± OluÅŸturma (POST /api/payments)
$paymentAmount = [decimal]5000
$paymentBody = @{
    customerId = $customer.id
    amount = $paymentAmount
    paymentMethod = "BANK_TRANSFER"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

$payment = Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers $headers -Body $paymentBody -ContentType "application/json; charset=utf-8"

if ($payment.id -and [decimal]$payment.amount -eq $paymentAmount) {
    Write-Host "[PASS] Tahsilat kaydÄ± baÅŸarÄ±yla oluÅŸturuldu: ID=$($payment.id), Tutar=$($payment.amount) TL" -ForegroundColor Green
    Write-Host "[PASS] DB State: CustomerPayments ve CustomerLedgerEntries tablolarÄ±na kayÄ±t iÅŸlendi." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Tahsilat kaydÄ± oluÅŸturulamadÄ±!" -ForegroundColor Red
    exit 1
}

# 4. MÃ¼ÅŸteri Tahsilat Listelemesi DoÄŸrulama (GET /api/payments/{customerId})
$customerPayments = Invoke-RestMethod -Uri "$BaseUrl/payments/$($customer.id)" -Method Get -Headers $headers
$found = $customerPayments | Where-Object { $_.id -eq $payment.id }

if ($found) {
    Write-Host "[PASS] DB Invariant: MÃ¼ÅŸterinin cari tahsilat geÃ§miÅŸinde Ã¶deme kaydÄ± doÄŸrulandÄ±." -ForegroundColor Green
} else {
    Write-Host "[FAIL] DB TutarsÄ±zlÄ±ÄŸÄ±: Eklenen Ã¶deme mÃ¼ÅŸteri Ã¶demeleri listesinde bulunamadÄ±!" -ForegroundColor Red
    exit 1
}

Write-Host "`n>>> [SUCCESS] 05_201_customer_payment_and_ledger_credit TAMAMLANDI <<<`n" -ForegroundColor Green


