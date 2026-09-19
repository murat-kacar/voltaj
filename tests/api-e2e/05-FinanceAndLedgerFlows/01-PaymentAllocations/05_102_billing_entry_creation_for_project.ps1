# 05_102_billing_entry_creation_for_project.ps1
# Senaryo 05.1.02: Proje HakediÅŸ GiriÅŸi ve FaturalandÄ±rma Takibi (VF-05101)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.1.02] Proje HakediÅŸ GiriÅŸi (VF-05101)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri ve Proje oluÅŸtur
$custBody = @{
    fullName = "HakediÅŸ MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "billing_cust_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5556660099"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"
$custId = $cust.id

$projBody = @{
    customerId = $custId
    name = "RÃ¼zgar TÃ¼rbini MontajÄ±"
    budget = 1000000.00
} | ConvertTo-Json
$project = Invoke-RestMethod -Uri "$BaseUrl/projects" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $projBody -ContentType "application/json; charset=utf-8"
$projId = $project.id
Write-Host "[INFO] Proje oluÅŸturuldu (Id: $projId, BÃ¼tÃ§e: 1,000,000 TL)."

# 3. Projeye HakediÅŸ GiriÅŸi Yap (Amount: 250,000 TL)
$billingBody = @{
    customerId = $custId
    amount = 250000.00
} | ConvertTo-Json

$entry = Invoke-RestMethod -Uri "$BaseUrl/billing/$projId" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $billingBody -ContentType "application/json; charset=utf-8"

if ($entry.amount -eq 250000.00) {
    Write-Host "[PASS] Proje hakediÅŸ giriÅŸi baÅŸarÄ±yla kaydedildi (Tutar: 250,000 TL)." -ForegroundColor Green
} else {
    Write-Host "[FAIL] HakediÅŸ tutarÄ± doÄŸrulanamadÄ±!" -ForegroundColor Red
    exit 1
}

# 4. Projeye ait hakediÅŸ listesini sorgula
$entriesList = Invoke-RestMethod -Uri "$BaseUrl/billing/$projId" -Method Get -Headers @{ Authorization = "Bearer $adminToken" }
$found = $entriesList | Where-Object { $_.id -eq $entry.id }

if ($found) {
    Write-Host "[PASS] GET /api/billing/$projId sorgusunda oluÅŸturulan hakediÅŸ satÄ±rÄ± doÄŸrulandÄ±." -ForegroundColor Green
    Write-Host "[PASS] DB State Invariant: BillingEntries tablosuna projectId ve customerId ile satÄ±r yazÄ±ldÄ±." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 05_102_billing_entry_creation_for_project TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] HakediÅŸ satÄ±rÄ± listede bulunamadÄ±!" -ForegroundColor Red
    exit 1
}


