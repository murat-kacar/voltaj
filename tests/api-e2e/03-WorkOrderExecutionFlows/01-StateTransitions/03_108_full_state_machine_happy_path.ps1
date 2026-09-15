$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.108] Ä°ÅŸ Emri Tam Durum Makinesi (Happy Path)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

# MÃ¼ÅŸteri oluÅŸtur
$custBody = @{ fullName = "Full SM Cust $(Get-Random)"; email = "fsm_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

# Ä°ÅŸ emri oluÅŸtur -> Created
$woBody = @{ customerId = $cust.id; title = "Tam Durum Makinesi Testi" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
$woId = $wo.id
Write-Host "[INFO] Created: $woId" -ForegroundColor Gray

# Assign -> Assigned
$assignBody = @{ employeeUserId = $auth.userId } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8"
if ($wo.status -ne "Assigned") { Write-Host "[FAIL] Assigned beklendi, $($wo.status) geldi!" -ForegroundColor Red; exit 1 }
Write-Host "[PASS] Assigned durumuna geÃ§ildi." -ForegroundColor Green

# EnRoute
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/en-route" -Method Post -Headers $headers
if ($wo.status -ne "EnRoute") { Write-Host "[FAIL] EnRoute beklendi, $($wo.status) geldi!" -ForegroundColor Red; exit 1 }
Write-Host "[PASS] EnRoute durumuna geÃ§ildi." -ForegroundColor Green

# Safety Checklist
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/safety-checklist" -Method Post -Headers $headers | Out-Null
Write-Host "[PASS] Ä°SG kontrolÃ¼ tamamlandÄ±." -ForegroundColor Green

# Start -> InProgress
$startBody = @{ targetCompletionDate = (Get-Date).AddDays(5).ToString("o") } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/start" -Method Post -Headers $headers -Body $startBody -ContentType "application/json; charset=utf-8"
if ($wo.status -ne "InProgress") { Write-Host "[FAIL] InProgress beklendi, $($wo.status) geldi!" -ForegroundColor Red; exit 1 }
Write-Host "[PASS] InProgress durumuna geÃ§ildi." -ForegroundColor Green

# Check-In
$ciBody = @{ notes = "Sahada Ã§alÄ±ÅŸma baÅŸladÄ±" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/check-in" -Method Post -Headers $headers -Body $ciBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[PASS] Check-In yapÄ±ldÄ±." -ForegroundColor Green

# Check-Out
$coBody = @{ notes = "GÃ¼nlÃ¼k Ã§alÄ±ÅŸma tamamlandÄ±" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/check-out" -Method Post -Headers $headers -Body $coBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[PASS] Check-Out yapÄ±ldÄ±." -ForegroundColor Green

# Complete
$completeBody = @{ signatureData = "base64sigdata"; proofOfWorkPhotoUrl = "https://storage.voltflow.com/proof/test.jpg" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$woId/complete" -Method Post -Headers $headers -Body $completeBody -ContentType "application/json; charset=utf-8"
if ($wo.status -ne "Completed") { Write-Host "[FAIL] Completed beklendi, $($wo.status) geldi!" -ForegroundColor Red; exit 1 }
Write-Host "[PASS] Completed durumuna geÃ§ildi." -ForegroundColor Green

Write-Host "`n>>> [SUCCESS] Tam durum makinesi happy path tamamlandÄ± <<<`n" -ForegroundColor Green
exit 0

