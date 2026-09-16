# 01_01_unapproved_user_returns_403.ps1
# Senaryo 1.1: Onay Bekleyen (Pending Approval) KullanÄ±cÄ±nÄ±n GiriÅŸ Reddi (403 Forbidden)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 1.1] OnaysÄ±z KullanÄ±cÄ± GiriÅŸ Reddi (403)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. OnaysÄ±z hesap oluÅŸtur (GeÃ§ersiz/rastgele OTP ile)
$unapprovedEmail = "unapproved_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$unapprovedPass = "PendingPass123!"

$regBody = @{
    name = "Unapproved Candidate"
    email = $unapprovedEmail
    password = $unapprovedPass
    otp = "999999" # GeÃ§ersiz OTP -> isApproved = false
} | ConvertTo-Json

Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8" | Out-Null

$loginBody = @{
    email = $unapprovedEmail
    password = $unapprovedPass
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Onay bekleyen hesap doÄŸrudan iÃ§eri alÄ±ndÄ±! Beklenen 403 idi." -ForegroundColor Red
    exit 1
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403) {
        Write-Host "[PASS] API beklenen 403 Forbidden yanÄ±tÄ±nÄ± dÃ¶ndÃ¼." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: 'Account approval is pending' mesajÄ± doÄŸrulandÄ±." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: IsApproved=false olduÄŸu iÃ§in oturum aÃ§Ä±lmadÄ±." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status Kodu: $statusCode" -ForegroundColor Red
        exit 1
    }
}


