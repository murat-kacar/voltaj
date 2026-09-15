$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.505] Ä°ptal EdilmiÅŸ Ä°ÅŸ Emrini BaÅŸlatma (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "Cancel Start Cust $(Get-Random)"; email = "cancelstart_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Ä°ptal SonrasÄ± BaÅŸlatma Testi" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Ä°ptal et
$cancelBody = @{ reason = "MÃ¼ÅŸteri iptal istedi" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/cancel" -Method Post -Headers $headers -Body $cancelBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Ä°ÅŸ emri iptal edildi." -ForegroundColor Gray

# Assign denemesi
try {
    $assignBody = @{ employeeUserId = $auth.userId } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Ä°ptal edilen iÅŸ emrine atama yapÄ±lmamalÄ±ydÄ±!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 409, 422) {
        Write-Host "[PASS] Ä°ptal edilen iÅŸ emrine atama reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}


