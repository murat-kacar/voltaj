$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"
$WorkOrdersUrl = "$BaseUrl/workorders"
$CustomersUrl = "$BaseUrl/customers"
$RegisterUrl = "$BaseUrl/auth/register"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 3.3] Beklemeye Alma ve Otomatik Check-Out Telafisi" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Yetkili GiriÅŸi (Admin)
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$token = $auth.token
$headers = @{
    "Authorization" = "Bearer $token"
}

# 2. MÃ¼ÅŸteri OluÅŸtur
$custBody = @{
    fullName = "Bursa Demir Ã‡elik A.Åž."
    email = "wo_hold_cust_$(Get-Random)@bursacelik.com"
    phone = "+905321112233"
} | ConvertTo-Json
$custResp = Invoke-RestMethod -Uri $CustomersUrl -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"
$customerId = $custResp.id

# 3. Ä°ÅŸ Emri OluÅŸtur
$woBody = @{
    customerId = $customerId
    title = "Ana DaÄŸÄ±tÄ±m Panosu Revizyonu"
} | ConvertTo-Json
$woResp = Invoke-RestMethod -Uri $WorkOrdersUrl -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
$workOrderId = $woResp.id

# 4. Atama Yap
$assignBody = @{
    employeeUserId = $auth.userId
} | ConvertTo-Json
Invoke-RestMethod -Uri "$WorkOrdersUrl/$workOrderId/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null

# 5. Ä°SG KontrolÃ¼nÃ¼ Tamamla
Invoke-RestMethod -Uri "$WorkOrdersUrl/$workOrderId/safety-checklist" -Method Post -Headers $headers | Out-Null

# 6. Ä°ÅŸi BaÅŸlat (InProgress)
$startBody = @{
    targetCompletionDate = (Get-Date).AddDays(1).ToString("o")
} | ConvertTo-Json
$startResp = Invoke-RestMethod -Uri "$WorkOrdersUrl/$workOrderId/start" -Method Post -Headers $headers -Body $startBody -ContentType "application/json; charset=utf-8"
if ($startResp.status -ne "InProgress") {
    Write-Host "[FAIL] Ä°ÅŸ emri InProgress durumuna geÃ§emedi!" -ForegroundColor Red
    exit 1
}
Write-Host "[INFO] InProgress durumunda iÅŸ emri hazÄ±rlandÄ± (Id: $workOrderId)." -ForegroundColor Gray

# 7. Check-In Yap (AÃ§Ä±k zaman kaydÄ± oluÅŸtur)
$checkInBody = @{
    notes = "Sahada tespit ve demontaj yapÄ±lÄ±yor"
} | ConvertTo-Json
$ciResp = Invoke-RestMethod -Uri "$WorkOrdersUrl/$workOrderId/check-in" -Method Post -Headers $headers -Body $checkInBody -ContentType "application/json; charset=utf-8"
$activeEntry = $ciResp.timeEntries | Where-Object { -not $_.checkOutTime }
if (-not $activeEntry) {
    Write-Host "[FAIL] Check-In sonrasÄ± aÃ§Ä±k zaman kaydÄ± bulunamadÄ±!" -ForegroundColor Red
    exit 1
}
Write-Host "[PASS] Check-In yapÄ±ldÄ±, aÃ§Ä±k zaman kaydÄ± mevcut (CheckOutTime = null)." -ForegroundColor Green

# 8. Check-Out YAPMADAN iÅŸ emrini On-Hold'a al
$holdReason = "Eksik 630A kompakt ÅŸalter tedariÄŸi bekleniyor"
$holdBody = @{
    reason = $holdReason
} | ConvertTo-Json

$holdResp = Invoke-RestMethod -Uri "$WorkOrdersUrl/$workOrderId/hold" -Method Post -Headers $headers -Body $holdBody -ContentType "application/json; charset=utf-8"
if ($holdResp.status -ne "OnHold") {
    Write-Host "[FAIL] Ä°ÅŸ emri OnHold durumuna geÃ§emedi! GÃ¼ncel durum: $($holdResp.status)" -ForegroundColor Red
    exit 1
}
Write-Host "[PASS] Ä°ÅŸ emri baÅŸarÄ±yla OnHold durumuna alÄ±ndÄ±." -ForegroundColor Green

# 9. DB State & Telafi Ä°ncelemesi: AÃ§Ä±k zaman kaydÄ± otomatik kapatÄ±ldÄ± mÄ±?
$latestEntry = $holdResp.timeEntries[-1]
if (-not $latestEntry.checkOutTime) {
    Write-Host "[FAIL] Telafi mekanizmasÄ± Ã§alÄ±ÅŸmadÄ±: AÃ§Ä±k kalan TimeEntry kapatÄ±lmadÄ±!" -ForegroundColor Red
    exit 1
}
Write-Host "[PASS] DB State Telafi: AÃ§Ä±k zaman kaydÄ± otomatik Check-Out ile kapatÄ±ldÄ± (CheckOutTime: $($latestEntry.checkOutTime))." -ForegroundColor Green

if ($latestEntry.notes -notlike "*Auto check-out due to placing on hold*") {
    Write-Host "[FAIL] Otomatik Check-Out notu beklenen metni iÃ§ermiyor: $($latestEntry.notes)" -ForegroundColor Red
    exit 1
}
Write-Host "[PASS] DB State Invariant: Zaman kaydÄ± telafi notu doÄŸrulandÄ± ('$($latestEntry.notes)')." -ForegroundColor Green

# 10. Ä°ÅŸi Tekrar Devam Ettir (Resume)
$resumeResp = Invoke-RestMethod -Uri "$WorkOrdersUrl/$workOrderId/resume" -Method Post -Headers $headers
if ($resumeResp.status -ne "InProgress") {
    Write-Host "[FAIL] Ä°ÅŸ emri tekrar InProgress durumuna geÃ§emedi! GÃ¼ncel durum: $($resumeResp.status)" -ForegroundColor Red
    exit 1
}
Write-Host "[PASS] Ä°ÅŸ emri baÅŸarÄ±yla Resume edildi ve InProgress durumuna dÃ¶ndÃ¼." -ForegroundColor Green
Write-Host "`n>>> [SUCCESS] 03_03_hold_state_auto_checkout_compensation TAMAMLANDI <<<`n" -ForegroundColor Green


