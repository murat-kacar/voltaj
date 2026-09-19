$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.103] Eksik Email ile Login (400)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$body = @{ email = ""; password = "Password123!" } | ConvertTo-Json
try {
    $res = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $body -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] BoÅŸ email ile login baÅŸarÄ±lÄ± olmamalÄ±ydÄ±!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 401, 422) {
        Write-Host "[PASS] BoÅŸ email ile login reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status - $_" -ForegroundColor Red
        exit 1
    }
}

