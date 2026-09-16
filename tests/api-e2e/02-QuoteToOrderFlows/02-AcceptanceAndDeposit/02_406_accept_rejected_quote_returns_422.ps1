$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.406] Red EdilmiÅŸ Teklifi Kabul Etme (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

$custBody = @{ fullName = "Reject Accept Cust $(Get-Random)"; email = "rjacc_$(Get-Random)@test.com"; phone = "+905001112244" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

$quoteBody = @{ customerId = $cust.id; title = "Reject Then Accept" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $headers -Body $quoteBody -ContentType "application/json; charset=utf-8"
$itemBody = @{ description = "Pano"; quantity = 1; unitPrice = 5000.00 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Headers $headers -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers | Out-Null

# Teklifi reddet
$rejectBody = @{ reason = "BÃ¼tÃ§e aÅŸÄ±mÄ±" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/reject" -Method Post -Headers $headers -Body $rejectBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Teklif reddedildi." -ForegroundColor Gray

# Red edilmiÅŸ teklifi kabul etme denemesi
try {
    $acceptBody = @{ depositAmount = 100 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Headers $headers -Body $acceptBody -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Red edilmiÅŸ teklif kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] Red edilmiÅŸ teklif kabul denemesi reddedildi (HTTP $status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

