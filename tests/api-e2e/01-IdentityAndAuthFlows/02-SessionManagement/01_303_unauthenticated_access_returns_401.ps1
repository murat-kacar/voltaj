$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.303] Token Olmadan KorumalÄ± Endpoint (401)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

try {
    $res = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get
    Write-Host "[FAIL] Token olmadan eriÅŸim kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -eq 401) {
        Write-Host "[PASS] Token olmadan eriÅŸim reddedildi (401 Unauthorized)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}
