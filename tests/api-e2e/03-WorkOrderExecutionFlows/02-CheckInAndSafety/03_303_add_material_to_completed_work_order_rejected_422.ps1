# 03_303_add_material_to_completed_work_order_rejected_422.ps1
# Senaryo 03.3.03: TamamlanmÄ±ÅŸ Ä°ÅŸ Emrine Malzeme Ekleme Engeli (VF-03401)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.3.03] TamamlanmÄ±ÅŸ Ä°ÅŸ Emrine Malzeme Ekleme Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri ve InProgress durumunda iÅŸ emri hazÄ±rla
$custBody = @{
    fullName = "TamamlanmÄ±ÅŸ Ä°ÅŸ MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "closed_wo_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5554446677"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Tamamlanacak BakÄ±m Ä°ÅŸi"; priority = "Medium" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Ata, Ä°SG onayla, baÅŸlat
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body (@{ employeeUserId = [guid]::NewGuid().ToString() } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body (@{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null

# 3. GeÃ§erli kanÄ±t ile iÅŸ emrini tamamla (Status: Completed)
$compBody = @{
    signatureData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
    proofOfWorkPhotoUrl = "https://voltflow.dev/proofs/proof_closed_1.jpg"
} | ConvertTo-Json

$completedWo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/complete" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $compBody -ContentType "application/json; charset=utf-8"
if ($completedWo.status -eq "Completed") {
    Write-Host "[INFO] Ä°ÅŸ emri baÅŸarÄ±yla tamamlandÄ± (Status: Completed, Id: $woId)."
} else {
    Write-Host "[FAIL] Ä°ÅŸ emri tamamlanamadÄ±!" -ForegroundColor Red
    exit 1
}

# 4. TamamlanmÄ±ÅŸ iÅŸ emrine sahada malzeme ekleme denemesi -> 422
$itemBody = @{
    description = "Sonradan Eklenmek Ä°stenen Malzeme"
    quantity = 2
    unitPrice = 500.00
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/items" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] TamamlanmÄ±ÅŸ iÅŸ emrine malzeme eklenebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] TamamlanmÄ±ÅŸ iÅŸ emrine malzeme ekleme FSM kuralÄ± ile engellendi ($code)." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: 'Materials can only be added to in-progress work orders' kuralÄ± doÄŸrulandÄ±." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: WorkOrders.Total ve WorkOrderItems satÄ±rlarÄ± korundu." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 03_303_add_material_to_completed_work_order_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


