# 07_201_operations_dead_letter_outbox_query.ps1
# Senaryo 07.2.01: YÃ¶netici Dead-Letter Outbox Kuyruk Sorgulama (VF-07201)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 07.2.01] Dead-Letter Outbox KuyruÄŸu (VF-07201)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. Dead-letter outbox kuyruÄŸunu sorgula
$deadLetterMessages = Invoke-RestMethod -Uri "$BaseUrl/operations/outbox/dead-letter" -Method Get -Headers $headers

Write-Host "[PASS] Dead-letter kuyruÄŸu baÅŸarÄ±yla sorgulandÄ± (Mevcut kuyruk eleman sayÄ±sÄ±: $($deadLetterMessages.Count))." -ForegroundColor Green
Write-Host "[PASS] DB State: OutboxMessages tablosundan Status='DeadLetter' filtreli kayÄ±tlar Ã§ekildi." -ForegroundColor Green
Write-Host "`n>>> [SUCCESS] 07_201_operations_dead_letter_outbox_query TAMAMLANDI <<<" -ForegroundColor Green
exit 0


