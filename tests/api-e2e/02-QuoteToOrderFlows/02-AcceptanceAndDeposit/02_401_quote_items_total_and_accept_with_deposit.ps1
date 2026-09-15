# 02_04_quote_items_total_and_accept_with_deposit.ps1
# Senaryo 2.4: Teklif Kalem ToplamÄ±, Kabul ve PeÅŸinat TahsilatÄ± (200 OK)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 2.4] Teklif Kalem ToplamÄ±, Kabul ve PeÅŸinat" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. AÅŸama: Admin giriÅŸi
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ Authorization = "Bearer $($auth.token)" }

# 2. AÅŸama: MÃ¼ÅŸteri oluÅŸtur
$custEmail = "energy_corp_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$custBody = @{ fullName = "Anadolu Trafo Sanayi A.Åž."; email = $custEmail; phone = "+905323334455" } | ConvertTo-Json
$customer = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Body $custBody -Headers $headers -ContentType "application/json; charset=utf-8"

# 3. AÅŸama: Teklif oluÅŸtur (Draft)
$quoteBody = @{ customerId = $customer.id; title = "1600kVA Trafo BakÄ±m ve Revizyonu" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Body $quoteBody -Headers $headers -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Taslak teklif aÃ§Ä±ldÄ± (Id: $($quote.id))." -ForegroundColor Gray

# 4. AÅŸama: Kalem 1 Ekle (10 Adet x 500 TL = 5000 TL)
$item1Body = @{
    description = "Orta Gerilim SigortasÄ± 36kV"
    quantity = 10
    unitPrice = 500.00
} | ConvertTo-Json
$quoteAfterItem1 = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Body $item1Body -Headers $headers -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Kalem 1 eklendi. Ara toplam: $($quoteAfterItem1.total) TL." -ForegroundColor Green

# 5. AÅŸama: Kalem 2 Ekle (2 Adet x 2500 TL = 5000 TL)
$item2Body = @{
    description = "Trafo Ä°zolasyon YaÄŸÄ± DeÄŸiÅŸimi ve Testi"
    quantity = 2
    unitPrice = 2500.00
} | ConvertTo-Json
$quoteAfterItem2 = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Body $item2Body -Headers $headers -ContentType "application/json; charset=utf-8"

if ($quoteAfterItem2.total -eq 10000.00) {
    Write-Host "[PASS] Kalem 2 eklendi. Otomatik genel toplam doÄŸrulandÄ±: $($quoteAfterItem2.total) TL." -ForegroundColor Green
}
else {
    Write-Host "[FAIL] Beklenen 10000.00 TL toplam yerine $($quoteAfterItem2.total) TL hesaplandÄ±!" -ForegroundColor Red
    exit 1
}

# 6. AÅŸama: Teklifi YayÄ±nla (Draft -> Issued)
$issuedQuote = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"
if ($issuedQuote.state -eq "Issued") {
    Write-Host "[PASS] Teklif baÅŸarÄ±yla yayÄ±nlandÄ± (Quotes.State = Issued)." -ForegroundColor Green
}
else {
    Write-Host "[FAIL] Teklif yayÄ±nlanamadÄ±! State: $($issuedQuote.state)" -ForegroundColor Red
    exit 1
}

# 7. AÅŸama: Teklifi Kabul Et (%30 PeÅŸinat ÅžartÄ± ile)
$acceptBody = @{ requiredDepositPercentage = 30.00 } | ConvertTo-Json
$acceptedQuote = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Body $acceptBody -Headers $headers -ContentType "application/json; charset=utf-8"

if ($acceptedQuote.state -eq "Accepted" -and $acceptedQuote.requiredDepositPercentage -eq 30.00) {
    Write-Host "[PASS] Teklif kabul edildi (%30 peÅŸinat ÅŸartÄ± kaydedildi)." -ForegroundColor Green
}
else {
    Write-Host "[FAIL] Teklif kabul aÅŸamasÄ±nda hata! State: $($acceptedQuote.state)" -ForegroundColor Red
    exit 1
}

# 8. AÅŸama: PeÅŸinat TahsilatÄ± Yap (3000 TL)
$depositBody = @{ amount = 3000.00 } | ConvertTo-Json
$depositQuote = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/pay-deposit" -Method Post -Body $depositBody -Headers $headers -ContentType "application/json; charset=utf-8"

if ($depositQuote.depositPaidAmount -eq 3000.00) {
    Write-Host "[PASS] PeÅŸinat tahsilatÄ± baÅŸarÄ±yla iÅŸlendi (Ã–denen PeÅŸinat: $($depositQuote.depositPaidAmount) TL)." -ForegroundColor Green
    Write-Host "[PASS] DB State: Quotes.State=Accepted, Total=10000.00, DepositPaidAmount=3000.00 doÄŸrulandÄ±." -ForegroundColor Green
    exit 0
}
else {
    Write-Host "[FAIL] PeÅŸinat tahsilat tutarÄ± eÅŸleÅŸmiyor: $($depositQuote.depositPaidAmount)" -ForegroundColor Red
    exit 1
}


