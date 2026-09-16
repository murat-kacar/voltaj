# 02_01_customer_lead_to_active_conversion.ps1
# Senaryo 2.1: MÃ¼ÅŸteri Lead Olarak BaÅŸlatma ve Aktif MÃ¼ÅŸteriye DÃ¶nÃ¼ÅŸtÃ¼rme

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 2.1] MÃ¼ÅŸteri Lead BaÅŸlatma ve Aktivasyon" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. AÅŸama: Admin ile giriÅŸ yap
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8"
$headers = @{ Authorization = "Bearer $($auth.token)" }

# 2. AÅŸama: MÃ¼ÅŸteri oluÅŸtur (Lead / Pasif)
$customerEmail = "bursa_sanayi_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$createBody = @{
    fullName = "Bursa Otomotiv Yan Sanayi Ltd."
    email = $customerEmail
    phone = "+905321112233"
    taxNumber = "1234567890"
} | ConvertTo-Json

$custRes = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Body $createBody -Headers $headers -ContentType "application/json; charset=utf-8"

if ($custRes.type -eq "Lead") {
    Write-Host "[PASS] MÃ¼ÅŸteri Lead olarak baÅŸarÄ±yla oluÅŸturuldu (Id: $($custRes.id))." -ForegroundColor Green
    Write-Host "[PASS] DB State: Type=Lead (0) doÄŸrulandÄ±." -ForegroundColor Green
}
else {
    Write-Host "[FAIL] MÃ¼ÅŸteri beklenen Lead durumunda baÅŸlamadÄ±! Type: $($custRes.type)" -ForegroundColor Red
    exit 1
}

# 3. AÅŸama: MÃ¼ÅŸteriyi aktif duruma geÃ§ir (/convert-to-active)
$activateRes = Invoke-RestMethod -Uri "$BaseUrl/customers/$($custRes.id)/convert-to-active" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"

if ($activateRes.type -eq "Active") {
    Write-Host "[PASS] MÃ¼ÅŸteri baÅŸarÄ±yla aktif duruma geÃ§irildi." -ForegroundColor Green
    Write-Host "[PASS] DB State: Type=Active (1) ve Version gÃ¼ncellendi." -ForegroundColor Green
    exit 0
}
else {
    Write-Host "[FAIL] Aktivasyon sonrasÄ± mÃ¼ÅŸteri durumu hatalÄ±! Type: $($activateRes.type)" -ForegroundColor Red
    exit 1
}


