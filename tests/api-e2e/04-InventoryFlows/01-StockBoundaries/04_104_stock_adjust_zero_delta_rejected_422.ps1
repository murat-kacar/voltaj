$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }
$InventoryUrl = "$BaseUrl/inventory"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.104] SÄ±fÄ±r Delta ile Stok Ayarlama (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$adjustBody = @{ materialCode = "MAT-ZERO-$(Get-Random)"; delta = 0 } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$InventoryUrl/adjust" -Method Post -Headers $headers -Body $adjustBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] SÄ±fÄ±r delta ile stok ayarlama kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] SÄ±fÄ±r delta ile stok ayarlama reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

