$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.104] Projeye GÃ¶re Billing Listesi (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

# MÃ¼ÅŸteri ve proje oluÅŸtur
$custBody = @{ fullName = "Billing List Cust $(Get-Random)"; email = "billlist_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$projectBody = @{ customerId = $cust.id; name = "Billing Test Proje"; budget = 50000 } | ConvertTo-Json
$project = Invoke-RestMethod -Uri "$BaseUrl/projects" -Method Post -Headers $headers -Body $projectBody -ContentType "application/json; charset=utf-8"

$billing = Invoke-RestMethod -Uri "$BaseUrl/billing/$($project.id)" -Method Get -Headers $headers
if ($null -ne $billing) {
    Write-Host "[PASS] Billing listesi baÅŸarÄ±yla dÃ¶ndÃ¼." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Billing listesi null dÃ¶ndÃ¼!" -ForegroundColor Red
    exit 1
}

