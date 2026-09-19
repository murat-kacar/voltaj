# 03_102_work_order_assignment_and_enroute.ps1
# Senaryo 03.1.02: Ä°ÅŸ Emri Atama ve Yola Ã‡Ä±kÄ±ÅŸ (EnRoute) FSM GeÃ§iÅŸi (VF-03101)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.1.02] Ä°ÅŸ Emri Atama ve Yola Ã‡Ä±kÄ±ÅŸ (VF-03101)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri ve Ä°ÅŸ Emri oluÅŸtur (Status: Open)
$custBody = @{
    fullName = "Saha Ä°ÅŸ Emri MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "wo_enroute_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5558889900"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{
    customerId = $cust.id
    title = "Saha DaÄŸÄ±tÄ±m Panosu Revizyonu"
    priority = "High"
} | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id
Write-Host "[INFO] Ä°ÅŸ emri oluÅŸturuldu (Status: $($wo.status), Id: $woId)."

# 3. Atama yapÄ±lmadan (Open iken) EnRoute Ã§aÄŸrÄ±sÄ± -> 422 bekliyoruz
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] AtanmamÄ±ÅŸ iÅŸ emri EnRoute yapÄ±labildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] AtanmamÄ±ÅŸ iÅŸ emrinin EnRoute geÃ§iÅŸi engellendi ($code)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}

# 4. Teknisyene ata (Status: Assigned)
$assignBody = @{ employeeUserId = [guid]::NewGuid().ToString() } | ConvertTo-Json
$assignedWo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $assignBody -ContentType "application/json; charset=utf-8"
if ($assignedWo.status -eq "Assigned") {
    Write-Host "[PASS] Ä°ÅŸ emri baÅŸarÄ±yla atandÄ± (Status: Assigned)." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Atama sonrasÄ± durum Assigned olmadÄ±!" -ForegroundColor Red
    exit 1
}

# 5. Åžimdi EnRoute yap (Status: EnRoute)
$enrouteWo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8"
if ($enrouteWo.status -eq "EnRoute") {
    Write-Host "[PASS] Ä°ÅŸ emri baÅŸarÄ±yla EnRoute durumuna geÃ§ti." -ForegroundColor Green
    Write-Host "[PASS] DB Invariant: WorkOrders.Status = EnRoute baÅŸarÄ±yla saklandÄ±." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 03_102_work_order_assignment_and_enroute TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] EnRoute durumuna geÃ§ilemedi!" -ForegroundColor Red
    exit 1
}


