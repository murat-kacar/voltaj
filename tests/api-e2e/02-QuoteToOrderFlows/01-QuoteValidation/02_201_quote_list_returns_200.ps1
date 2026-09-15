$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.201] Teklif Listesi (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$quotes = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Get -Headers $headers
if ($null -ne $quotes) {
    Write-Host "[PASS] Teklif listesi baÅŸarÄ±yla dÃ¶ndÃ¼." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Teklif listesi null dÃ¶ndÃ¼!" -ForegroundColor Red
    exit 1
}

