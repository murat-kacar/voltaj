$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.107] Ä°ÅŸ Emri OluÅŸturma (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "WO Create Cust $(Get-Random)"; email = "wo_create_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$woBody = @{ customerId = $cust.id; title = "Yeni Ä°ÅŸ Emri Testi" } | ConvertTo-Json
$wo = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Post -Headers $headers -Body $woBody -ContentType "application/json; charset=utf-8"
if ($null -ne $wo.id -and $wo.status -eq "Open") {
    Write-Host "[PASS] Ä°ÅŸ emri baÅŸarÄ±yla oluÅŸturuldu (Id: $($wo.id), Status: $($wo.status))." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Ä°ÅŸ emri oluÅŸturma beklenen sonucu vermedi!" -ForegroundColor Red
    exit 1
}


