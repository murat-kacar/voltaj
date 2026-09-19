# 01_404_non_admin_approval_rejected_403.ps1
# Senaryo 01.1.04: Yetkisiz KullanÄ±cÄ±nÄ±n Onay Denemesi Reddi (403 Forbidden)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.1.04] Yetkisiz KullanÄ±cÄ± Onay Reddi (403)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Normal (Admin olmayan) onaylÄ± kullanÄ±cÄ± ile oturum aÃ§
$techEmail = "technician_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$techPass = "TechPass123!"

$regBody = @{
    name = "Field Tech"
    email = $techEmail
    password = $techPass
    otp = "000000" # Master OTP ile onaylÄ± kayÄ±t
} | ConvertTo-Json

Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8" | Out-Null

$loginBody = @{
    email = $techEmail
    password = $techPass
} | ConvertTo-Json

$loginResp = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json; charset=utf-8"
$nonAdminToken = $loginResp.token
Write-Host "[INFO] Normal kullanÄ±cÄ± oturumu hazÄ±rlandÄ± ($techEmail)."

# 2. Yetkisiz kullanÄ±cÄ±nÄ±n baÅŸka bir kullanÄ±cÄ±yÄ± onaylama denemesi
$targetUserId = [guid]::NewGuid().ToString()

$headers = @{
    Authorization = "Bearer $nonAdminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}

try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/users/$targetUserId/approve" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Normal kullanÄ±cÄ± onay iÅŸlemi yapabildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 403) {
        Write-Host "[PASS] Yetkisiz onay isteÄŸi beklenen 403 Forbidden ile engellendi." -ForegroundColor Green
        Write-Host "[PASS] RBAC GÃ¼venlik Bariyeri: Sadece Admin rolÃ¼ kullanÄ±cÄ± onaylayabilir." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 01_404_non_admin_approval_rejected_403 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


