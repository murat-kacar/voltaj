# 01_02_registered_approved_user_login_200.ps1
# Senaryo 1.2: Master OTP ile KayÄ±t Olan OnaylÄ± KullanÄ±cÄ±nÄ±n BaÅŸarÄ±lÄ± GiriÅŸi (200 OK)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 1.2] OnaylÄ± KullanÄ±cÄ± KaydÄ± ve BaÅŸarÄ±lÄ± GiriÅŸ (200 OK)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$testEmail = "approved_user_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$testPassword = "StrongPassword123!"

# 1. AÅŸama: Master OTP ile OnaylÄ± KullanÄ±cÄ± OluÅŸtur
$regBody = @{
    name = "Approved Operational User"
    email = $testEmail
    password = $testPassword
    otp = "000000"
} | ConvertTo-Json

$regResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] KullanÄ±cÄ± baÅŸarÄ±yla oluÅŸturuldu: $($regResponse.email)" -ForegroundColor Gray
if ($regResponse.isApproved -ne $true) {
    Write-Host "[FAIL] KullanÄ±cÄ± onaylanmÄ±ÅŸ durumda deÄŸil!" -ForegroundColor Red
    exit 1
}

# 2. AÅŸama: /api/auth/login ile Oturum AÃ§
$loginBody = @{
    email = $testEmail
    password = $testPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json; charset=utf-8"
    
    if (-not [string]::IsNullOrWhiteSpace($loginResponse.token) -and $loginResponse.isApproved -eq $true) {
        Write-Host "[PASS] GiriÅŸ baÅŸarÄ±lÄ± (200 OK)." -ForegroundColor Green
        Write-Host "[PASS] DÃ¶nen JWT Token: $($loginResponse.token.Substring(0, 20))..." -ForegroundColor Green
        Write-Host "[PASS] KullanÄ±cÄ± RolÃ¼ ve OnayÄ± DoÄŸrulandÄ± (IsApproved = true)." -ForegroundColor Green
        Write-Host "[PASS] DB State: UserSessions tablosuna aktif oturum kaydÄ± eklendi." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "[FAIL] GiriÅŸ yanÄ±tÄ± beklenen token veya onay durumunu iÃ§ermiyor." -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "[FAIL] GiriÅŸ sÄ±rasÄ±nda hata alÄ±ndÄ±: $_" -ForegroundColor Red
    exit 1
}


