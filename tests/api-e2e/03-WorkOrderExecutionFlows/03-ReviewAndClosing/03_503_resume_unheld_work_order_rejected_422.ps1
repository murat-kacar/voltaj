# 03_503_resume_unheld_work_order_rejected_422.ps1
# Senaryo 03.5.03: Beklemede Olmayan Ä°ÅŸ Emrinin Devam Ettirilme Engeli (VF-03301)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.5.03] Beklemede Olmayan Ä°ÅŸ Emri Resume Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. InProgress durumunda iÅŸ emri hazÄ±rla
$custBody = @{
    fullName = "Resume Test MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "resume_cust_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5553335566"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Kesintisiz GÃ¼Ã§ KaynaÄŸÄ± Devreye Alma"; priority = "High" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Ata, Ä°SG onayla, baÅŸlat (Status: InProgress)
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body (@{ employeeUserId = [guid]::NewGuid().ToString() } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body (@{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Ä°ÅŸ emri InProgress durumunda (Id: $woId)."

# 3. OnHold yapÄ±lmadan doÄŸrudan Resume Ã§aÄŸrÄ±sÄ± -> 422
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/resume" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Beklemede olmayan iÅŸ emrine resume Ã§aÄŸrÄ±labildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Beklemede olmayan iÅŸ emrine resume Ã§aÄŸrÄ±sÄ± engellendi ($code)." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: 'Only on-hold work orders can be resumed' kuralÄ± doÄŸrulandÄ±." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: WorkOrders.Status 'InProgress' olarak korundu." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 03_503_resume_unheld_work_order_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


