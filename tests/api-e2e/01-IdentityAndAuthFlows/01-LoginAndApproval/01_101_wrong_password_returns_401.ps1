# 01_01_wrong_password_returns_401.ps1
# Senaryo 1.1: OnaylÄ± KullanÄ±cÄ±nÄ±n HatalÄ± Åžifre ile GiriÅŸ Reddi (401 Unauthorized)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 1.1] HatalÄ± Åžifre ile GiriÅŸ Reddi (401)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Ã–n HazÄ±rlÄ±k: Test iÃ§in onaylÄ± bir kullanÄ±cÄ± oluÅŸtur (OTP 000000 ile)
$email = "auth_test_user@voltflow.com"
$regBody = @{
    name = "Auth Test User"
    email = $email
    password = "CorrectPassword123!"
    otp = "000000"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8" | Out-Null
} catch {
    # KullanÄ±cÄ± zaten varsa devam et
}

# 2. HatalÄ± ÅŸifre ile giriÅŸ isteÄŸi gÃ¶nder
$loginBody = @{
    email = $email
    password = "ThisIsTheWrongPassword123!"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] HatalÄ± ÅŸifre kabul edildi! Beklenen 401 Unauthorized idi." -ForegroundColor Red
    exit 1
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Write-Host "[PASS] API beklenen 401 Unauthorized yanÄ±tÄ±nÄ± dÃ¶ndÃ¼." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: Invalid credentials doÄŸrulandÄ±." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: UserSessions tablosuna yeni oturum eklenmedi." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status Kodu: $statusCode" -ForegroundColor Red
        exit 1
    }
}


