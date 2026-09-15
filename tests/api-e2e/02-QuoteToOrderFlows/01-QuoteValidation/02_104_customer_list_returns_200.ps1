$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.104] MÃ¼ÅŸteri Listesi (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$customers = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers

if ($customers -is [Array]) {
    Write-Host "[PASS] MÃ¼ÅŸteri listesi baÅŸarÄ±yla dÃ¶ndÃ¼ (Adet: $($customers.Count))." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] MÃ¼ÅŸteri listesi beklendiÄŸi gibi dÃ¶nmedi!" -ForegroundColor Red
    exit 1
}

