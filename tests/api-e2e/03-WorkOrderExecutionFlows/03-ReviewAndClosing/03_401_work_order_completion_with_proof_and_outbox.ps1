# 03_401_work_order_completion_with_proof_and_outbox.ps1
# Senaryo 03.4.01: KanÄ±tsÄ±z Tamamlama Engeli ve KanÄ±tlÄ± Tamamlama

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.4.01] KanÄ±tsÄ±z Tamamlama Engeli ve KanÄ±tlÄ± Tamamlama" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "Proof Cust $(Get-Random)"; email = "proof_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "KanÄ±t Testi" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Full flow to InProgress
$assignBody = @{ employeeUserId = $auth.userId } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers $headers | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers $headers | Out-Null
$startBody = @{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers $headers -Body $startBody -ContentType "application/json; charset=utf-8" | Out-Null

# KanÄ±tsÄ±z tamamlama denemesi
$emptyProofBody = @{ signatureData = $null; proofOfWorkPhotoUrl = $null } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/complete" -Method Post -Headers $headers -Body $emptyProofBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] KanÄ±tsÄ±z tamamlama kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] KanÄ±tsÄ±z tamamlama reddedildi (HTTP $status)." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

# KanÄ±tlÄ± tamamlama
$proofBody = @{ signatureData = "base64sig"; proofOfWorkPhotoUrl = "https://storage.voltflow.com/proof.jpg" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/complete" -Method Post -Headers $headers -Body $proofBody -ContentType "application/json; charset=utf-8"
if ($wo.status -eq "Completed") {
    Write-Host "[PASS] KanÄ±tlÄ± tamamlama baÅŸarÄ±lÄ± (Completed)." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Tamamlama beklenen sonucu vermedi: $($wo.status)" -ForegroundColor Red
    exit 1
}

