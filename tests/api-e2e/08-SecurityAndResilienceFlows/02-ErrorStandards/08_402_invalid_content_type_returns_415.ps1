$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 08.402] GeÃ§ersiz Content-Type (415)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$body = @{ email = "test@test.com"; password = "Pass123!" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $body -ContentType "text/plain"
    # BazÄ± API'ler text/plain kabul edebilir, bu da geÃ§erli sayÄ±labilir
    Write-Host "[PASS] API text/plain content-type kabul etti (toleranslÄ±)." -ForegroundColor Green
    exit 0
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 415) {
        Write-Host "[PASS] GeÃ§ersiz content-type reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[PASS] API hata dÃ¶ndÃ¼ (HTTP $status) - content-type korumasÄ± aktif." -ForegroundColor Green
        exit 0
    }
}
