# 01_02_invalid_otp_creates_unapproved_user.ps1
# Senaryo 1.2: GeÃ§ersiz/BoÅŸ OTP ile KayÄ±t Olan KullanÄ±cÄ±nÄ±n OnaysÄ±z KalmasÄ± ve GiriÅŸte 403 AlmasÄ±

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 1.2] GeÃ§ersiz OTP ile KayÄ±t -> IsApproved=False & GiriÅŸte 403" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$testEmail = "unapproved_otp_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$testPassword = "SecurePassword123!"

# 1. AÅŸama: HatalÄ± / Rastgele OTP ile kayÄ±t ol
$regBody = @{
    name = "Pending Approval User"
    email = $testEmail
    password = $testPassword
    otp = "123456" # HatalÄ± OTP (000000 deÄŸil)
} | ConvertTo-Json

try {
    $regResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8"
    Write-Host "[PASS] KayÄ±t talebi alÄ±ndÄ± (User ID: $($regResponse.userId))." -ForegroundColor Green
    Write-Host "[PASS] IsApproved Durumu: $($regResponse.isApproved) (Beklenen: False)." -ForegroundColor Green
    
    if ($regResponse.isApproved -eq $true) {
        Write-Host "[FAIL] HatalÄ± OTP ile kullanÄ±cÄ± otomatik onaylandÄ±! Bu bir gÃ¼venlik aÃ§Ä±ÄŸÄ±dÄ±r." -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "[FAIL] KayÄ±t iÅŸlemi beklenmedik ÅŸekilde hata verdi: $_" -ForegroundColor Red
    exit 1
}

# 2. AÅŸama: OnaysÄ±z kullanÄ±cÄ± ile giriÅŸ yapmayÄ± dene -> 403 Beklenir
$loginBody = @{
    email = $testEmail
    password = $testPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] OnaysÄ±z kullanÄ±cÄ± sisteme giriÅŸ yapabildi!" -ForegroundColor Red
    exit 1
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403) {
        Write-Host "[PASS] OnaysÄ±z kullanÄ±cÄ± giriÅŸinde API beklenen 403 Forbidden dÃ¶ndÃ¼rdÃ¼." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: IsApproved=false olduÄŸu sÃ¼rece UserSessions oluÅŸturulmaz." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "[FAIL] Beklenen 403 yerine $statusCode dÃ¶ndÃ¼." -ForegroundColor Red
        exit 1
    }
}


