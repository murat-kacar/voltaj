# 02_601_project_creation_and_phase_management.ps1
# Senaryo 02.6.01: Proje OluÅŸturma, Faz Ekleme ve BÃ¼tÃ§e Takibi (VF-02501)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.6.01] Proje ve Faz YÃ¶netimi (VF-02501)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri oluÅŸtur
$custBody = @{
    fullName = "Proje MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "project_cust_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5557778899"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"
$custId = $cust.id

# 3. Yeni proje oluÅŸtur (Budget: 500,000 TL)
$projectBody = @{
    customerId = $custId
    name = "Organize Sanayi GÃ¼neÅŸ Santrali Faz 1"
    budget = 500000.00
} | ConvertTo-Json

$project = Invoke-RestMethod -Uri "$BaseUrl/projects" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $projectBody -ContentType "application/json; charset=utf-8"
$projId = $project.id
Write-Host "[PASS] Proje baÅŸarÄ±yla oluÅŸturuldu (Id: $projId, No: $($project.number), BÃ¼tÃ§e: $($project.budget) TL)." -ForegroundColor Green

# 4. Projeye Faz Ekle (Faz 1: AltyapÄ± ve Kablolama - 150,000 TL)
$phaseBody = @{
    title = "Faz 1: AltyapÄ± ve Kablolama"
    plannedAmount = 150000.00
} | ConvertTo-Json

$phase = Invoke-RestMethod -Uri "$BaseUrl/projects/$projId/phases" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $phaseBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Proje fazÄ± baÅŸarÄ±yla eklendi (Faz Id: $($phase.id), BaÅŸlÄ±k: $($phase.title))." -ForegroundColor Green

# 5. Projeyi ID ile detaylÄ± sorgula ve fazÄ±n iÃ§inde olduÄŸunu doÄŸrula
$projDetail = Invoke-RestMethod -Uri "$BaseUrl/projects/$projId" -Method Get -Headers @{ Authorization = "Bearer $adminToken" }
$foundPhase = $projDetail.phases | Where-Object { $_.title -eq "Faz 1: AltyapÄ± ve Kablolama" }

if ($foundPhase -and $projDetail.budget -eq 500000.00) {
    Write-Host "[PASS] Proje detayÄ±nda eklenen faz ve bÃ¼tÃ§e bilgisi doÄŸrulandÄ±." -ForegroundColor Green
    Write-Host "[PASS] DB State Invariant: Projects ve ProjectPhases tablolarÄ± iliÅŸkisi baÅŸarÄ±yla kuruldu." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 02_601_project_creation_and_phase_management TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Proje detayÄ±nda faz doÄŸrulanamadÄ±!" -ForegroundColor Red
    exit 1
}


