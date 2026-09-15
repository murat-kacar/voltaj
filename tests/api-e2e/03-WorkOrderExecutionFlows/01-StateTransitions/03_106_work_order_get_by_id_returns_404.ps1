$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.106] Var Olmayan Ä°ÅŸ Emri ID (404)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$fakeId = [Guid]::NewGuid()
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$fakeId" -Method Get -Headers $headers
    Write-Host "[FAIL] Var olmayan iÅŸ emri bulunamazdÄ±!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -eq 404) {
        Write-Host "[PASS] Var olmayan iÅŸ emri iÃ§in 404 dÃ¶ndÃ¼." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

