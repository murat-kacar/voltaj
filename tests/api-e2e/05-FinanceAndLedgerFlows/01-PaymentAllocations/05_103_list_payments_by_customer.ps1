$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.103] MÃ¼ÅŸteriye GÃ¶re Ã–deme Listesi (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "Payment List Cust $(Get-Random)"; email = "paylist_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$payments = Invoke-RestMethod -Uri "$BaseUrl/payments/$($cust.id)" -Method Get -Headers $headers
if ($null -ne $payments) {
    Write-Host "[PASS] Ã–deme listesi baÅŸarÄ±yla dÃ¶ndÃ¼ (Yeni mÃ¼ÅŸteri, adet: $(if ($payments -is [Array]) {$payments.Count} else {0}))." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Ã–deme listesi null dÃ¶ndÃ¼!" -ForegroundColor Red
    exit 1
}

