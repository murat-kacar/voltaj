# 01_403_admin_approves_unapproved_user.ps1
# Senaryo 01.1.03: YÃ¶neticinin OnaysÄ±z KullanÄ±cÄ±yÄ± OnaylamasÄ± (VF-01101)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.1.03] YÃ¶netici KullanÄ±cÄ± OnayÄ± (VF-01101)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. OnaysÄ±z aday kullanÄ±cÄ± oluÅŸtur
$candidateEmail = "candidate_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$candidatePass = "PendingPass123!"

$regBody = @{
    name = "Candidate User"
    email = $candidateEmail
    password = $candidatePass
    otp = "999999" # GeÃ§ersiz OTP -> IsApproved = false
} | ConvertTo-Json

$regResp = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8"
$candidateId = $regResp.userId
Write-Host "[INFO] Aday kullanÄ±cÄ± kaydedildi (Id: $candidateId, IsApproved: $($regResp.isApproved))."

# 2. AdayÄ±n giriÅŸ yapmayÄ± denemesi -> 403 almalÄ±
$candidateLoginBody = @{
    email = $candidateEmail
    password = $candidatePass
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $candidateLoginBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] OnaysÄ±z kullanÄ±cÄ± giriÅŸ yapabildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -ne 403) {
        Write-Host "[FAIL] Beklenen 403 yerine $code dÃ¶ndÃ¼." -ForegroundColor Red
        exit 1
    }
    Write-Host "[PASS] Aday kullanÄ±cÄ± onay Ã¶ncesi 403 Forbidden ile engellendi." -ForegroundColor Green
}

# 3. YÃ¶netici (Admin) giriÅŸi yap
$adminLoginBody = @{
    email = "admin@voltflow.com"
    password = "Admin123!"
} | ConvertTo-Json

$adminResp = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLoginBody -ContentType "application/json; charset=utf-8"
$adminToken = $adminResp.token

# 4. YÃ¶netici adayÄ± onaylar
$headers = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}

$approveResp = Invoke-RestMethod -Uri "$BaseUrl/auth/users/$candidateId/approve" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] YÃ¶netici onay API Ã§aÄŸrÄ±sÄ± baÅŸarÄ±lÄ± oldu (IsApproved: $($approveResp.isApproved))." -ForegroundColor Green

# 5. ArtÄ±k onaylanan kullanÄ±cÄ± giriÅŸ yapabilmeli -> 200 OK
$approvedLoginResp = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $candidateLoginBody -ContentType "application/json; charset=utf-8"
if ($approvedLoginResp.token -and $approvedLoginResp.isApproved -eq $true) {
    Write-Host "[PASS] Onaylanan kullanÄ±cÄ± baÅŸarÄ±yla giriÅŸ yaptÄ± ve JWT aldÄ±." -ForegroundColor Green
    Write-Host "[PASS] DB State Invariant: AppUsers.IsApproved=true kalÄ±cÄ± hale geldi." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 01_403_admin_approves_unapproved_user TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Onay sonrasÄ± giriÅŸ yanÄ±tÄ± beklenmeyen formatta." -ForegroundColor Red
    exit 1
}


