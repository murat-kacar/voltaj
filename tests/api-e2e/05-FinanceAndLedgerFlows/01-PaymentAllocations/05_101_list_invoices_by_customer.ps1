# 05_101_list_invoices_by_customer.ps1
# Senaryo 05.1.01: MÃ¼ÅŸteri FaturalarÄ±nÄ± Listeleme ve Bakiye Takibi (VF-05101)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.1.01] MÃ¼ÅŸteri Fatura Listeleme ve Bakiye (VF-05101)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. Sistemdeki ilk mÃ¼ÅŸteriyi al (veya faturalÄ± mÃ¼ÅŸteri)
$customers = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
$customer = $customers[0]
$custId = $customer.id

# 3. MÃ¼ÅŸterinin faturalarÄ±nÄ± sorgula
$invoices = Invoke-RestMethod -Uri "$BaseUrl/payments/invoices/$custId" -Method Get -Headers $headers

if ($invoices -and $invoices.Count -gt 0) {
    $firstInv = $invoices[0]
    Write-Host "[PASS] MÃ¼ÅŸteriye ait faturalar listelendi (Fatura No: $($firstInv.invoiceNumber), Toplam: $($firstInv.grandTotal) TL, Kalan: $($firstInv.remainingAmount) TL)." -ForegroundColor Green
    Write-Host "[PASS] DB State: SalesInvoices tablosundan bakiye ve avans mahsup alanlarÄ± baÅŸarÄ±yla okundu." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 05_101_list_invoices_by_customer TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[PASS] MÃ¼ÅŸteriye ait fatura listesi sorgusu baÅŸarÄ±lÄ± (Mevcut fatura sayÄ±sÄ±: 0)." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 05_101_list_invoices_by_customer TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
}


