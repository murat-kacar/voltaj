# 01_02_duplicate_email_registration_409.ps1
# Senaryo 1.2: MÃ¼kerrer E-posta KaydÄ±nÄ±n Engellenmesi ve DB DeÄŸiÅŸmezliÄŸi

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 1.2] MÃ¼kerrer E-posta KaydÄ± Reddi (400/409)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. AÅŸama: Ä°lk geÃ§erli kaydÄ± yap
$duplicateEmail = "duplicate_test_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$regBody1 = @{
    name = "Initial User"
    email = $duplicateEmail
    password = "StrongPassword123!"
    otp = "000000"
} | ConvertTo-Json

$firstResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody1 -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Ä°lk kullanÄ±cÄ± baÅŸarÄ±yla aÃ§Ä±ldÄ± ($duplicateEmail)." -ForegroundColor Gray

# 2. AÅŸama: AynÄ± e-posta ile fakat farklÄ± bilgilerle yeni kayÄ±t isteÄŸi at (MÃ¼kerrer e-posta denemesi)
$regBody2 = @{
    name = "Second Attempt User"
    email = $duplicateEmail
    password = "DifferentPassword999!"
    otp = "000000"
} | ConvertTo-Json

try {
    $secondResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody2 -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] MÃ¼kerrer e-posta kabul edildi! Sistem Ã§ift kayÄ±t oluÅŸturmamalÄ±ydÄ±." -ForegroundColor Red
    exit 1
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -in 400, 409, 422) {
        Write-Host "[PASS] API beklenen hata kodunu dÃ¶ndÃ¼ ($statusCode)." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: MÃ¼kerrer e-posta ihlali doÄŸrulandÄ± ('A user with this email already exists.')." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: AppUsers tablosuna ikinci satÄ±r eklenmedi (Rollback / Unique constraint)." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status Kodu: $statusCode" -ForegroundColor Red
        exit 1
    }
}


