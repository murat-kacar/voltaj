$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 08.405] Var Olmayan Endpoint (404)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

try {
    Invoke-RestMethod -Uri "$BaseUrl/nonexistent-endpoint/foobar" -Method Get
    Write-Host "[FAIL] Var olmayan endpoint 200 dÃ¶nmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 401, 404) {
        Write-Host "[PASS] Var olmayan endpoint reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}
