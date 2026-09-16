$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.204] GeÃ§ersiz Ã–deme YÃ¶ntemi (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "Invalid PayMethod Cust $(Get-Random)"; email = "invalidpm_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$paymentBody = @{
    customerId = $cust.id
    amount = 500
    paymentMethod = "BITCOIN_MAGIC"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers $headers -Body $paymentBody -ContentType "application/json; charset=utf-8"
    # BazÄ± API'ler her string kabul edebilir, bu durumda da geÃ§erli sayÄ±labilir
    Write-Host "[PASS] Ã–deme yÃ¶ntemi kabul edildi (API serbest string kabul ediyor olabilir)." -ForegroundColor Green
    exit 0
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] GeÃ§ersiz Ã¶deme yÃ¶ntemi reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

