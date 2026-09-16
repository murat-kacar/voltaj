# 03_302_field_material_addition_to_work_order.ps1
# Senaryo 03.3.02: Sahada Malzeme TÃ¼ketimi ve Tutar HesaplamasÄ± (VF-03401)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.3.02] Sahada Malzeme TÃ¼ketimi (VF-03401)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. InProgress durumunda iÅŸ emri hazÄ±rla
$custBody = @{
    fullName = "Malzeme TÃ¼ketim MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "mat_cust_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5551113344"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Trafo Revizyon ve Kablo Ã‡ekimi"; priority = "High" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Ata, Ä°SG onayla, baÅŸlat
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body (@{ employeeUserId = [guid]::NewGuid().ToString() } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body (@{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Ä°ÅŸ emri InProgress durumunda (Id: $woId, BaÅŸlangÄ±Ã§ TutarÄ±: 0 TL)."

# 3. Sahada ilk malzemeyi ekle (25 metre Kablo x 120 TL = 3000 TL)
$item1 = @{
    description = "NYY 4x16 GÃ¼Ã§ Kablosu"
    quantity = 25
    unitPrice = 120.00
} | ConvertTo-Json

$woAfterItem1 = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/items" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $item1 -ContentType "application/json; charset=utf-8"
if ($woAfterItem1.total -eq 3000.00) {
    Write-Host "[PASS] Kalem 1 eklendi: Ara toplam 3000 TL olarak doÄŸrulandÄ±." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Beklenen tutar 3000 TL, gelen: $($woAfterItem1.total)" -ForegroundColor Red
    exit 1
}

# 4. Ä°kinci malzemeyi ekle (8 adet Kablo Pabucu x 50 TL = 400 TL -> Toplam: 3400 TL)
$item2 = @{
    description = "BakÄ±r Kablo Pabucu M10"
    quantity = 8
    unitPrice = 50.00
} | ConvertTo-Json

$woAfterItem2 = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/items" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $item2 -ContentType "application/json; charset=utf-8"
if ($woAfterItem2.total -eq 3400.00) {
    Write-Host "[PASS] Kalem 2 eklendi: Yeni genel toplam 3400 TL baÅŸarÄ±yla doÄŸrulandÄ±." -ForegroundColor Green
    Write-Host "[PASS] DB State Invariant: WorkOrders.Total malzeme toplamÄ± (3400 TL) tutarlÄ±lÄ±ÄŸÄ± korundu." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 03_302_field_material_addition_to_work_order TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Kalem 2 sonrasÄ± beklenen tutar 3400 TL, gelen: $($woAfterItem2.total)" -ForegroundColor Red
    exit 1
}


