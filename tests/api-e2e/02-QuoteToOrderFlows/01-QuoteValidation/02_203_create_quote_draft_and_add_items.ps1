$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.203] Teklif TaslaÄŸÄ± OluÅŸtur ve Kalem Ekle (200)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ "Authorization" = "Bearer $($auth.token)" }

# MÃ¼ÅŸteri oluÅŸtur
$custBody = @{ fullName = "Quote Draft Cust $(Get-Random)"; email = "qdraft_$(Get-Random)@test.com"; phone = "+905001234567" } | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $custBody -ContentType "application/json; charset=utf-8"

# Teklif taslaÄŸÄ± oluÅŸtur
$quoteBody = @{ customerId = $cust.id; title = "Test Teklif TaslaÄŸÄ±" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $headers -Body $quoteBody -ContentType "application/json; charset=utf-8"
if ($null -eq $quote.id) {
    Write-Host "[FAIL] Teklif oluÅŸturulamadÄ±!" -ForegroundColor Red
    exit 1
}
Write-Host "[PASS] Teklif taslaÄŸÄ± oluÅŸturuldu (Id: $($quote.id))." -ForegroundColor Green

# Kalem ekle
$itemBody = @{ description = "Kablo DÃ¶ÅŸeme"; quantity = 100; unitPrice = 25.50 } | ConvertTo-Json
$updated = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Headers $headers -Body $itemBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Teklif kalemine kalem eklendi." -ForegroundColor Green

# Ä°kinci kalem ekle
$itemBody2 = @{ description = "Sigorta MontajÄ±"; quantity = 10; unitPrice = 150.00 } | ConvertTo-Json
$updated2 = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Headers $headers -Body $itemBody2 -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Ä°kinci kalem de eklendi." -ForegroundColor Green

# Teklifi ID ile Ã§ek ve doÄŸrula
$fetched = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)" -Method Get -Headers $headers
if ($fetched.id -eq $quote.id) {
    Write-Host "[PASS] Teklif GetById ile doÄŸrulandÄ±." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Teklif GetById doÄŸrulamasÄ± baÅŸarÄ±sÄ±z!" -ForegroundColor Red
    exit 1
}

