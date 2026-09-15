$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 08.404] Bozuk JSON ile POST Ä°steÄŸi (400)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$malformedJson = '{"email": "test@test.com", "password": }'
try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $malformedJson -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Bozuk JSON kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422, 500) {
        Write-Host "[PASS] Bozuk JSON reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}


