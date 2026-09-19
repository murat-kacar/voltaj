$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.407] SÃ¼resi DolmuÅŸ Teklifi Kabul Etme (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "Expire Accept Cust $(Get-Random)"; email = "expacc_$(Get-Random)@test.com"; phone = "+905001112255" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$quoteBody = @{ customerId = $cust.id; title = "Expire Then Accept" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $headers -Body $quoteBody -ContentType "application/json; charset=utf-8"
$itemBody = @{ description = "Sigorta"; quantity = 5; unitPrice = 200 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Headers $headers -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers | Out-Null

# Teklifi expire et
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/expire" -Method Post -Headers $headers | Out-Null
Write-Host "[INFO] Teklif sÃ¼resi doldu (Expired)." -ForegroundColor Gray

# Expire edilmiÅŸ teklifi kabul etme denemesi
try {
    $acceptBody = @{ depositAmount = 50 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Headers $headers -Body $acceptBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Expired teklif kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] Expired teklif kabul denemesi reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

