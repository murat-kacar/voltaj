# 03_101_safety_checklist_gate_enforcement.ps1
# Senaryo 03.1.01: Ä°SG Kontrol Listesi Bariyeri

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.1.01] Ä°SG Kontrol Listesi Bariyeri" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

# MÃ¼ÅŸteri ve Ä°ÅŸ Emri OluÅŸtur
$custBody = @{ fullName = "ISG Gate Cust $(Get-Random)"; email = "isg_gate_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Ä°SG Bariyeri Testi" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Atama
$assignBody = @{ employeeUserId = $auth.userId } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers $headers | Out-Null

# Ä°SG kontrolÃ¼ YAPMADAN baÅŸlatma denemesi
$startBody = @{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers $headers -Body $startBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Ä°SG kontrolÃ¼ yapÄ±lmadan baÅŸlatma kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] Ä°SG bariyeri aktif - baÅŸlatma reddedildi ($status)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

# Ä°SG kontrolÃ¼nÃ¼ tamamla
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers $headers | Out-Null
Write-Host "[PASS] Ä°SG kontrolÃ¼ tamamlandÄ±." -ForegroundColor Green

# Åžimdi baÅŸlatma baÅŸarÄ±lÄ± olmalÄ±
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers $headers -Body $startBody -ContentType "application/json; charset=utf-8"
if ($wo.status -eq "InProgress") {
    Write-Host "[PASS] Ä°SG sonrasÄ± baÅŸlatma baÅŸarÄ±lÄ± (InProgress)." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Ä°SG sonrasÄ± baÅŸlatma beklenen sonucu vermedi: $($wo.status)" -ForegroundColor Red
    exit 1
}

