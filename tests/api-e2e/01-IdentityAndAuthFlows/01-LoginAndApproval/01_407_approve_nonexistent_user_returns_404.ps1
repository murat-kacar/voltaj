$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.407] Var Olmayan KullanÄ±cÄ±yÄ± Onaylama (404)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$fakeUserId = [Guid]::NewGuid()
try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/users/$fakeUserId/approve" -Method Post -Headers $headers
    Write-Host "[FAIL] Var olmayan kullanÄ±cÄ± onayÄ± kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 404, 422) {
        Write-Host "[PASS] Var olmayan kullanÄ±cÄ± onayÄ± reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status - $_" -ForegroundColor Red
        exit 1
    }
}

