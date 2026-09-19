$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.202] Var Olmayan Teklif ID (404)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$fakeId = [Guid]::NewGuid()
try {
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$fakeId" -Method Get -Headers $headers
    Write-Host "[FAIL] Var olmayan teklif bulunamazdÄ±!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -eq 404) {
        Write-Host "[PASS] Var olmayan teklif ID iÃ§in 404 dÃ¶ndÃ¼." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

