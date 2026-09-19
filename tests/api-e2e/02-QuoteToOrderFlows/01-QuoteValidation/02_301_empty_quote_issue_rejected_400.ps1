# 02_301_empty_quote_issue_rejected_400.ps1
# Senaryo: Kalemsiz Teklif YayÄ±nlama Reddi (400)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.301] Kalemsiz Teklif YayÄ±nlama Reddi (400)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ Authorization = "Bearer $($auth.token)" }

# MÃ¼ÅŸteri oluÅŸtur
$custBody = @{ fullName = "Empty Quote Cust $(Get-Random)"; email = "emptyq_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

# Kalem eklemeden teklif oluÅŸtur
$quoteBody = @{ customerId = $cust.id; title = "BoÅŸ Teklif Testi" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $headers -Body $quoteBody -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Kalemsiz teklif oluÅŸturuldu (Id: $($quote.id))." -ForegroundColor Gray

# Kalem eklemeden Issue denemesi
try {
    Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] Kalemsiz teklif yayÄ±nlanmamalÄ±ydÄ±!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -in 400, 422) {
        Write-Host "[PASS] Kalemsiz teklif yayÄ±nlama reddedildi ($status)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}

