# 01_302_revoked_token_access_returns_401.ps1
# Senaryo: Ä°ptal EdilmiÅŸ Token ile EriÅŸim (401)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.302] Ä°ptal EdilmiÅŸ Token ile EriÅŸim (401)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# KullanÄ±cÄ± oluÅŸtur ve giriÅŸ yap
$email = "revoke_test_$(Get-Random)@voltflow.com"
$regBody = @{ name = "Revoke User"; email = $email; password = "Password123!"; otp = "000000" } | ConvertTo-Json
$reg = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8"
$token = $reg.token
Write-Host "[INFO] KullanÄ±cÄ± oluÅŸturuldu ve token alÄ±ndÄ±." -ForegroundColor Gray

# Token'Ä± iptal et
$revokeBody = @{ token = $token } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/auth/session/revoke" -Method Post -Body $revokeBody -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Token iptal edildi." -ForegroundColor Gray

# Ä°ptal edilmiÅŸ token ile eriÅŸim denemesi
$headers = @{ "Authorization" = "Bearer $token" }
try {
    Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
    Write-Host "[FAIL] Ä°ptal edilmiÅŸ token ile eriÅŸim kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -eq 401) {
        Write-Host "[PASS] Ä°ptal edilmiÅŸ token ile eriÅŸim reddedildi (401)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

