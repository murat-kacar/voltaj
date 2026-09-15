$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 08.403] BoÅŸ Body ile POST Ä°steÄŸi (400)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body "" -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] BoÅŸ body ile login kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 415, 422, 500) {
        Write-Host "[PASS] BoÅŸ body ile POST reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}


