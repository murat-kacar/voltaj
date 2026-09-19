$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.104] Åžifresiz KayÄ±t Denemesi (400)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$body = @{
    name = "Test User"
    email = "no_pass_$(Get-Random)@voltflow.com"
    password = ""
    otp = "000000"
} | ConvertTo-Json

try {
    $res = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $body -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] BoÅŸ ÅŸifre ile kayÄ±t kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] BoÅŸ ÅŸifre ile kayÄ±t reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status - $_" -ForegroundColor Red
        exit 1
    }
}

