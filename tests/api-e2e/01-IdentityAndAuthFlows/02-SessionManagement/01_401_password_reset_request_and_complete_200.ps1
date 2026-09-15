# 01_401_password_reset_request_and_complete_200.ps1
# Senaryo: Åžifre SÄ±fÄ±rlama Talebi, Tamamlama ve Yeni Åžifre ile GiriÅŸ

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.401] Åžifre SÄ±fÄ±rlama ve Yeni Åžifre ile GiriÅŸ (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$email = "reset_user_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$oldPassword = "OldPassword123!"
$newPassword = "NewSecretPassword2026!"

# 1. KullanÄ±cÄ± oluÅŸtur
$regBody = @{ name = "Reset User"; email = $email; password = $oldPassword; otp = "000000" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] KullanÄ±cÄ± oluÅŸturuldu ($email)." -ForegroundColor Gray

# 2. Åžifre sÄ±fÄ±rlama talebi
$reqBody = @{ email = $email } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/auth/password-reset/request" -Method Post -Body $reqBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Åžifre sÄ±fÄ±rlama talebi kabul edildi." -ForegroundColor Green

# 3. Åžifre sÄ±fÄ±rlamayÄ± tamamla
$completeBody = @{ email = $email; token = "000000"; newPassword = $newPassword } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/auth/password-reset/complete" -Method Post -Body $completeBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Åžifre sÄ±fÄ±rlama baÅŸarÄ±yla tamamlandÄ±." -ForegroundColor Green

# 4. Eski ÅŸifre ile giriÅŸ reddedilmeli
$oldLoginBody = @{ email = $email; password = $oldPassword } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $oldLoginBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Eski ÅŸifre hÃ¢lÃ¢ geÃ§erli!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -eq 401) {
        Write-Host "[PASS] Eski ÅŸifre ile giriÅŸ reddedildi (401 Unauthorized)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

# 5. Yeni ÅŸifre ile baÅŸarÄ±lÄ± giriÅŸ
$newLoginBody = @{ email = $email; password = $newPassword } | ConvertTo-Json
$loginRes = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $newLoginBody -ContentType "application/json; charset=utf-8"
if ($loginRes.token) {
    Write-Host "[PASS] Yeni ÅŸifre ile baÅŸarÄ±lÄ± oturum aÃ§Ä±ldÄ±." -ForegroundColor Green
    Write-Host "[PASS] DB State: PasswordHash yeni deÄŸeri ile gÃ¼ncellendi." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Yeni ÅŸifre ile giriÅŸ yapÄ±lamadÄ±!" -ForegroundColor Red
    exit 1
}

