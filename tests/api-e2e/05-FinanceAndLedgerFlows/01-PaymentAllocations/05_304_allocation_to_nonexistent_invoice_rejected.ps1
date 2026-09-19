$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.304] Var Olmayan Faturaya Mahsup (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "NonExInv Cust $(Get-Random)"; email = "nonexinv_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$paymentBody = @{
    customerId = $cust.id
    amount = 1000
    paymentMethod = "BANK_TRANSFER"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json
$payment = Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers $headers -Body $paymentBody -ContentType "application/json; charset=utf-8"

$allocBody = @{
    paymentId = $payment.id
    invoiceId = [Guid]::NewGuid()
    amount = 500
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/payments/allocate" -Method Post -Headers $headers -Body $allocBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Var olmayan faturaya mahsup kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 404, 422) {
        Write-Host "[PASS] Var olmayan faturaya mahsup reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

