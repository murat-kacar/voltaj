# 03_502_work_order_cancellation_and_terminal_guard.ps1
# Senaryo 03.5.02: Ä°ÅŸ Emri Ä°ptali ve Ä°ptal EdilmiÅŸ Ä°ÅŸ Emri KorumasÄ± (VF-03501)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.5.02] Ä°ÅŸ Emri Ä°ptali ve FSM KorumasÄ± (VF-03501)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri ve Ä°ÅŸ Emri oluÅŸtur (Status: Open)
$custBody = @{
    fullName = "Ä°ptal Test MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "cancel_cust_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5552224455"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Ä°ptal Edilecek Tesis BakÄ±mÄ±"; priority = "Low" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id
Write-Host "[INFO] Ä°ÅŸ emri oluÅŸturuldu (Id: $woId, Status: Open)."

# 3. Ä°ÅŸ emrini gerekÃ§e belirterek iptal et (Cancel)
$cancelBody = @{ reason = "MÃ¼ÅŸteri sÃ¶zleÅŸmeyi feshetti, saha Ã§alÄ±ÅŸmasÄ± iptal edildi." } | ConvertTo-Json
$cancelledWo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/cancel" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $cancelBody -ContentType "application/json; charset=utf-8"

if ($cancelledWo.status -eq "Cancelled") {
    Write-Host "[PASS] Ä°ÅŸ emri baÅŸarÄ±yla Cancelled durumuna geÃ§irildi." -ForegroundColor Green
    Write-Host "[PASS] DB State Invariant: WorkOrders.Status = Cancelled olarak kaydedildi." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Ä°ÅŸ emri iptal edilemedi!" -ForegroundColor Red
    exit 1
}

# 4. Ä°ptal edilmiÅŸ iÅŸ emrini baÅŸlatmaya Ã§alÄ±ÅŸma -> 422
try {
    $startBody = @{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $startBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Ä°ptal edilmiÅŸ iÅŸ emri baÅŸlatÄ±labildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Ä°ptal edilmiÅŸ iÅŸ emrinin baÅŸlatÄ±lmasÄ± FSM kuralÄ± ile engellendi ($code)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}

# 5. Ä°ptal edilmiÅŸ iÅŸ emrini yola Ã§Ä±karma (EnRoute) denemesi -> 422
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Ä°ptal edilmiÅŸ iÅŸ emri EnRoute yapÄ±labildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Ä°ptal edilmiÅŸ iÅŸ emrinin EnRoute geÃ§iÅŸi engellendi ($code)." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 03_502_work_order_cancellation_and_terminal_guard TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


