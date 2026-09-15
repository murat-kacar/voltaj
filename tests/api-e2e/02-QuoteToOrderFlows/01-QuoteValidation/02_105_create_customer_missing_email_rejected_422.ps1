$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.105] Eksik Email ile MÃ¼ÅŸteri OluÅŸturma (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$body = @{ fullName = "Test Customer"; email = ""; phone = "+905001112233" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $body -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] BoÅŸ email ile mÃ¼ÅŸteri oluÅŸturma kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] BoÅŸ email ile mÃ¼ÅŸteri oluÅŸturma reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

