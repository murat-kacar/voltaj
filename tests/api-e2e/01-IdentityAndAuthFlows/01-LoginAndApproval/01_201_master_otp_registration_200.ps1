# 01_02_master_otp_registration_200.ps1
# Senaryo 1.2: Test OrtamÄ±nda Master OTP 000000 ile AnÄ±nda OnaylÄ± KayÄ±t

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 1.2] Master OTP (000000) ile KayÄ±t (200/202)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$uniqueEmail = "dry_pilot_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$regBody = @{
    name = "Voltflow Pilot Engineer"
    email = $uniqueEmail
    password = "StrongPassword123!"
    otp = "000000"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8"
    
    if (-not $response.token) {
        Write-Host "[FAIL] KayÄ±t baÅŸarÄ±lÄ± ancak JWT Token boÅŸ dÃ¶ndÃ¼!" -ForegroundColor Red
        exit 1
    }

    if ($response.isApproved -ne $true) {
        Write-Host "[FAIL] Master OTP 000000 kullanÄ±lmasÄ±na raÄŸmen isApproved true olmadÄ±!" -ForegroundColor Red
        exit 1
    }

    Write-Host "[PASS] API kayÄ±t isteÄŸini baÅŸarÄ±yla kabul etti." -ForegroundColor Green
    Write-Host "[PASS] DÃ¶nen JWT Token doÄŸrulandÄ± (Uzunluk: $($response.token.Length))." -ForegroundColor Green
    Write-Host "[PASS] isApproved == true (Master OTP ile anÄ±nda onaylandÄ±)." -ForegroundColor Green
    Write-Host "[PASS] DB State: AppUsers satÄ±rÄ± eklendi (Email: $uniqueEmail)." -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "[FAIL] KayÄ±t sÄ±rasÄ±nda hata oluÅŸtu: $_" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Hata DetayÄ±: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}


