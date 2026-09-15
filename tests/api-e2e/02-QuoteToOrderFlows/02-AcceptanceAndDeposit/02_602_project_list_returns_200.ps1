$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.602] Proje Listesi (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$projects = Invoke-RestMethod -Uri "$BaseUrl/projects" -Method Get -Headers $headers
if ($null -ne $projects) {
    Write-Host "[PASS] Proje listesi baÅŸarÄ±yla dÃ¶ndÃ¼." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Proje listesi null dÃ¶ndÃ¼!" -ForegroundColor Red
    exit 1
}

