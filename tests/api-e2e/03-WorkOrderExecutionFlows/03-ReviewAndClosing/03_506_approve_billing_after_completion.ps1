$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.506] Tamamlanan Ä°ÅŸ Emri Faturalama OnayÄ±" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "Billing Approve Cust $(Get-Random)"; email = "billapprove_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Billing Onay Testi" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id

# Full flow: assign -> safety -> start -> complete
$assignBody = @{ employeeUserId = $auth.userId } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers $headers | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers $headers | Out-Null
$startBody = @{ targetCompletionDate = (Get-Date).AddDays(1).ToString("o") } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers $headers -Body $startBody -ContentType "application/json; charset=utf-8" | Out-Null
$completeBody = @{ signatureData = "sig"; proofOfWorkPhotoUrl = "https://proof.com/test.jpg" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/complete" -Method Post -Headers $headers -Body $completeBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Ä°ÅŸ emri Completed durumunda." -ForegroundColor Gray

# Approve billing
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/approve-billing" -Method Post -Headers $headers
if ($wo.status -eq "ReadyForBilling") {
    Write-Host "[PASS] Faturalama onayÄ± baÅŸarÄ±lÄ± (Status: $($wo.status))." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Beklenen durum ReadyForBilling, gelen: $($wo.status)" -ForegroundColor Red
    exit 1
}


