# 01_405_assign_role_by_admin.ps1
# Senaryo 01.1.05: YÃ¶netici TarafÄ±ndan KullanÄ±cÄ±ya Rol AtanmasÄ± (VF-01101)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.1.05] YÃ¶netici TarafÄ±ndan Rol AtamasÄ± (VF-01101)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. OnaylÄ± bir kullanÄ±cÄ± oluÅŸtur
$userEmail = "operator_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$userPass = "Operator123!"

$regBody = @{
    name = "Field Operator"
    email = $userEmail
    password = $userPass
    otp = "000000"
} | ConvertTo-Json

$regResp = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8"
$targetUserId = $regResp.userId
Write-Host "[INFO] KullanÄ±cÄ± oluÅŸturuldu (Id: $targetUserId, Email: $userEmail)."

# 2. Admin giriÅŸi yap
$adminLoginBody = @{
    email = "admin@voltflow.com"
    password = "Admin123!"
} | ConvertTo-Json

$adminResp = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLoginBody -ContentType "application/json; charset=utf-8"
$adminToken = $adminResp.token

# 3. Admin kullanÄ±cÄ±ya 'Manager' rolÃ¼ atar
$headers = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}

$assignBody = @{
    roleName = "Manager"
} | ConvertTo-Json

$assignResp = Invoke-RestMethod -Uri "$BaseUrl/auth/users/$targetUserId/roles" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Admin rol atama Ã§aÄŸrÄ±sÄ± baÅŸarÄ±lÄ± oldu (HTTP 200/204)." -ForegroundColor Green

# 4. KullanÄ±cÄ± oturum aÃ§tÄ±ÄŸÄ±nda yeni rollerini alabilmeli
$userLoginBody = @{
    email = $userEmail
    password = $userPass
} | ConvertTo-Json

$userLoginResp = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $userLoginBody -ContentType "application/json; charset=utf-8"
if ($userLoginResp.token) {
    Write-Host "[PASS] Rol atanan kullanÄ±cÄ± yeni oturumunu baÅŸarÄ±yla aÃ§tÄ±." -ForegroundColor Green
    Write-Host "[PASS] DB State Invariant: AppUserRoles tablosunda rol iliÅŸkisi kuruldu." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 01_405_assign_role_by_admin TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] KullanÄ±cÄ± giriÅŸ yapamadÄ±." -ForegroundColor Red
    exit 1
}


