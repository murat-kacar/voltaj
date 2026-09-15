$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.305] SÄ±fÄ±r TutarlÄ± Mahsup (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$allocBody = @{
    paymentId = [Guid]::NewGuid()
    invoiceId = [Guid]::NewGuid()
    amount = 0
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/payments/allocate" -Method Post -Headers $headers -Body $allocBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] SÄ±fÄ±r tutarlÄ± mahsup kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] SÄ±fÄ±r tutarlÄ± mahsup reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

