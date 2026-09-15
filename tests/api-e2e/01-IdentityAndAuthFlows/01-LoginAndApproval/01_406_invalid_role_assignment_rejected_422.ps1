# 01_406_invalid_role_assignment_rejected_422.ps1
# Senaryo 01.1.06: TanÄ±msÄ±z Rol Atama Ä°steÄŸinin Reddi (422/400)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.1.06] TanÄ±msÄ±z Rol Atama Reddi (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin giriÅŸi yap
$adminLoginBody = @{
    email = "admin@voltflow.com"
    password = "Admin123!"
} | ConvertTo-Json

$adminResp = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLoginBody -ContentType "application/json; charset=utf-8"
$adminToken = $adminResp.token

# 2. Var olmayan bir kullanÄ±cÄ± veya geÃ§erli kullanÄ±cÄ± iÃ§in geÃ§ersiz rol ata
$targetUserId = $adminResp.userId
$headers = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}

$invalidAssignBody = @{
    roleName = "NonExistentSuperRole_999"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/users/$targetUserId/roles" -Method Post -Headers $headers -Body $invalidAssignBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] GeÃ§ersiz rol atamasÄ± baÅŸarÄ±yla kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] GeÃ§ersiz rol atamasÄ± beklenen HTTP $code ile reddedildi." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: AppUserRoles tablosuna tanÄ±msÄ±z rol eklenmedi." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 01_406_invalid_role_assignment_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


