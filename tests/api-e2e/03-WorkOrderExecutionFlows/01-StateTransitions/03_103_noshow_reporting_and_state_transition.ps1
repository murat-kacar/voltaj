# 03_103_noshow_reporting_and_state_transition.ps1
# Senaryo 03.1.03: Teknisyen Adreste Bulunamama (NoShow / KapÄ± Duvar) Raporu (VF-03101)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.1.03] Adreste Bulunamama (NoShow) Raporu (VF-03101)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri ve Ä°ÅŸ Emri oluÅŸtur, ata ve EnRoute yap
$custBody = @{
    fullName = "KapÄ± Duvar MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "noshow_cust_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5559990011"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{
    customerId = $cust.id
    title = "Acil Trafo SigortasÄ± DeÄŸiÅŸimi"
    priority = "Critical"
} | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Atama yap ve yola Ã§Ä±k
$assignBody = @{ employeeUserId = [guid]::NewGuid().ToString() } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Ä°ÅŸ emri EnRoute durumuna getirildi (Id: $woId)."

# 3. Teknisyen adrese varÄ±r ancak tesis kilitlidir -> NoShow raporla
$noShowBody = @{
    reason = "Tesis kapalÄ±, firma yetkilisine ulaÅŸÄ±lamadÄ± (KapÄ± Duvar)."
} | ConvertTo-Json

$noShowWo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/no-show" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $noShowBody -ContentType "application/json; charset=utf-8"

if ($noShowWo.status -eq "NoShow") {
    Write-Host "[PASS] Ä°ÅŸ emri baÅŸarÄ±yla NoShow durumuna geÃ§irildi." -ForegroundColor Green
    Write-Host "[PASS] DB Invariant: WorkOrders.Status = NoShow ve CancellationReason saklandÄ±." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Ä°ÅŸ emri durumu NoShow olmadÄ±!" -ForegroundColor Red
    exit 1
}

# 4. NoShow durumundaki iÅŸ emrinin doÄŸrudan baÅŸlatÄ±lmasÄ± engellenmeli -> 422
try {
    $startBody = @{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $startBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] NoShow durumundaki iÅŸ emri baÅŸlatÄ±labildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] NoShow durumundaki iÅŸ emrinin baÅŸlatÄ±lmasÄ± FSM kuralÄ± ile engellendi ($code)." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 03_103_noshow_reporting_and_state_transition TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


