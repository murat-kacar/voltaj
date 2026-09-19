# 03_501_review_gate_and_terminal_invoiced_state.ps1
# Senaryo 03.5.01: Onay Bariyeri ve Terminal FaturalanmÄ±ÅŸ Durum

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.5.01] Onay Bariyeri ve Terminal FaturalanmÄ±ÅŸ Durum" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "Review Gate Cust $(Get-Random)"; email = "revgate_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Review Gate Testi" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Full flow to Completed
$assignBody = @{ employeeUserId = $auth.userId } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers $headers | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers $headers | Out-Null
$startBody = @{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers $headers -Body $startBody -ContentType "application/json; charset=utf-8" | Out-Null
$completeBody = @{ signatureData = "sig"; proofOfWorkPhotoUrl = "https://proof.com/test.jpg" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/complete" -Method Post -Headers $headers -Body $completeBody -ContentType "application/json; charset=utf-8" | Out-Null

# Onay verilmeden faturalama denemesi
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/invoice" -Method Post -Headers $headers
    Write-Host "[FAIL] Onay verilmeden faturalama kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] Onay bariyeri aktif - onaysÄ±z faturalama reddedildi ($status)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

# Onay ver
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/approve-billing" -Method Post -Headers $headers | Out-Null
Write-Host "[PASS] Faturalama onayÄ± verildi." -ForegroundColor Green

# Faturala
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/invoice" -Method Post -Headers $headers
if ($wo.status -eq "Invoiced") {
    Write-Host "[PASS] Faturalama baÅŸarÄ±lÄ± (Invoiced)." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Faturalama beklenen sonucu vermedi: $($wo.status)" -ForegroundColor Red
    exit 1
}

# Terminal durumda iptal denemesi
try {
    $cancelBody = @{ reason = "MÃ¼ÅŸteri iptal istedi" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/cancel" -Method Post -Headers $headers -Body $cancelBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] FaturalanmÄ±ÅŸ iÅŸ emri iptal edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] Terminal durum korumasÄ± aktif - iptal reddedildi ($status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

