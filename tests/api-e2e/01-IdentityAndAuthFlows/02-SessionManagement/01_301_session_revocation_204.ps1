# 01_301_session_revocation_204.ps1
# Senaryo: Oturum Ä°ptal Etme (204 No Content)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.301] Oturum Ä°ptal Etme (204)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# KullanÄ±cÄ± oluÅŸtur ve token al
$email = "session_revoke_$(Get-Random)@voltflow.com"
$regBody = @{ name = "Session User"; email = $email; password = "Password123!"; otp = "000000" } | ConvertTo-Json
$reg = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8"
$token = $reg.token
Write-Host "[INFO] KullanÄ±cÄ± oluÅŸturuldu ve token alÄ±ndÄ±." -ForegroundColor Gray

# Token'Ä± iptal et
$revokeBody = @{ token = $token } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/auth/session/revoke" -Method Post -Body $revokeBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Oturum baÅŸarÄ±yla iptal edildi." -ForegroundColor Green
exit 0

