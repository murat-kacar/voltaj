$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.105] Ä°ÅŸ Emri Listesi (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$workOrders = Invoke-RestMethod -Uri "$BaseUrl/workorders" -Method Get -Headers $headers
if ($null -ne $workOrders) {
    Write-Host "[PASS] Ä°ÅŸ emri listesi baÅŸarÄ±yla dÃ¶ndÃ¼." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Ä°ÅŸ emri listesi null dÃ¶ndÃ¼!" -ForegroundColor Red
    exit 1
}

