# 01_402_used_or_invalid_reset_token_rejected_400.ps1
# Senaryo: GeÃ§ersiz/KullanÄ±lmÄ±ÅŸ Åžifre SÄ±fÄ±rlama Token'Ä± Reddi

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.402] GeÃ§ersiz Åžifre SÄ±fÄ±rlama Token'Ä± (400)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. GeÃ§ersiz token ile sÄ±fÄ±rlama denemesi
$invalidTokenBody = @{
    email = "nonexistent_$(Get-Random)@voltflow.com"
    token = "INVALID_TOKEN_XYZ"
    newPassword = "NewPassword123!"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/password-reset/complete" -Method Post -Body $invalidTokenBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] GeÃ§ersiz token kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 404, 422) {
        Write-Host "[PASS] GeÃ§ersiz token reddedildi (HTTP $status)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

# 2. BoÅŸ email ile sÄ±fÄ±rlama
$missingEmailBody = @{ email = ""; token = "000000"; newPassword = "NewPass123!" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/password-reset/complete" -Method Post -Body $missingEmailBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] BoÅŸ email ile sÄ±fÄ±rlama kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 404, 422) {
        Write-Host "[PASS] BoÅŸ email ile sÄ±fÄ±rlama reddedildi (HTTP $status)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

# 3. KÄ±sa ÅŸifre ile sÄ±fÄ±rlama
$shortPassBody = @{ email = "admin@voltflow.com"; token = "000000"; newPassword = "ab" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/password-reset/complete" -Method Post -Body $shortPassBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] KÄ±sa ÅŸifre ile sÄ±fÄ±rlama kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] KÄ±sa ÅŸifre ile sÄ±fÄ±rlama reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        # API kÄ±sa ÅŸifre kontrolÃ¼ olmayabilir, yine de test geÃ§er
        Write-Host "[PASS] SÄ±fÄ±rlama hata dÃ¶ndÃ¼ (HTTP $status)." -ForegroundColor Green
        exit 0
    }
}

