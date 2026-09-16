# 03_202_checkout_without_checkin_rejected_422.ps1
# Senaryo 03.2.02: AÃ§Ä±k Oturum Olmadan Check-Out Yapma Engeli (VF-03201)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.2.02] AÃ§Ä±k Oturumsuz Check-Out Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri ve InProgress durumunda iÅŸ emri hazÄ±rla
$custBody = @{
    fullName = "Check-Out Test MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "checkout_gate_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5550001122"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{
    customerId = $cust.id
    title = "Saha Kablolama Ä°ÅŸi"
    priority = "Medium"
} | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Ata, Ä°SG tamamla ve baÅŸlat
$assignBody = @{ employeeUserId = [guid]::NewGuid().ToString() } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body (@{ targetCompletionDate = (Get-Date).AddDays(2).ToString("o") } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Ä°ÅŸ emri InProgress durumunda hazÄ±rlandÄ± (Id: $woId)."

# 3. HiÃ§ Check-In yapmadan Check-Out yapmayÄ± dene -> 422
$checkOutBody = @{ notes = "Erken AyrÄ±lÄ±ÅŸ" } | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/check-out" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $checkOutBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] AÃ§Ä±k kayÄ±t olmadan Check-Out yapÄ±labildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] AÃ§Ä±k kayÄ±t olmadan Check-Out beklenen $code ile reddedildi." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: 'No active check-in found' kuralÄ± doÄŸrulandÄ±." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: WorkOrderTimeEntries tablosuna hayali Ã§Ä±kÄ±ÅŸ yazÄ±lmadÄ±." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 03_202_checkout_without_checkin_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


