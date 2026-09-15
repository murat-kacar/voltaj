# 03_201_time_tracking_and_double_checkin_prevention.ps1
# Senaryo 03.2.01: Zaman Takibi ve Ã‡ift Check-In Engeli

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.2.01] Zaman Takibi ve Ã‡ift Check-In Engeli" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

# Ä°ÅŸ emri hazÄ±rla
$custBody = @{ fullName = "DblCI Cust $(Get-Random)"; email = "dblci_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Ã‡ift Check-In Testi" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

$assignBody = @{ employeeUserId = $auth.userId } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers $headers | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers $headers | Out-Null
$startBody = @{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers $headers -Body $startBody -ContentType "application/json; charset=utf-8" | Out-Null

# Ä°lk Check-In
$ciBody = @{ notes = "Ä°lk giriÅŸ" } | ConvertTo-Json
$ci = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/check-in" -Method Post -Headers $headers -Body $ciBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Ä°lk Check-In baÅŸarÄ±lÄ±." -ForegroundColor Green

# Ã‡ift Check-In denemesi
$ciBody2 = @{ notes = "Ä°kinci giriÅŸ denemesi" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/check-in" -Method Post -Headers $headers -Body $ciBody2 -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Ã‡ift check-in kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 409, 422) {
        Write-Host "[PASS] Ã‡ift check-in reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

