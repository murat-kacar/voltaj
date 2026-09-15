# 02_103_customer_get_by_id_and_404.ps1
# Senaryo 02.1.03: MÃ¼ÅŸteri Listeleme, Detay Sorgulama ve 404 DoÄŸrulamasÄ± (VF-02101)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 02.1.03] MÃ¼ÅŸteri Detay ve 404 KorumasÄ± (VF-02101)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. Yeni mÃ¼ÅŸteri oluÅŸtur
$custName = "Sorgu MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
$custEmail = "query_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$postHeaders = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = [guid]::NewGuid().ToString()
}
$createCustBody = @{
    fullName = $custName
    email = $custEmail
    phone = "5551112233"
    taxNumber = (Get-Random -Minimum 1000000000 -Maximum 9999999999).ToString()
} | ConvertTo-Json

$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $postHeaders -Body $createCustBody -ContentType "application/json; charset=utf-8"
$custId = $cust.id
Write-Host "[PASS] MÃ¼ÅŸteri baÅŸarÄ±yla oluÅŸturuldu (Id: $custId, AdÄ±: $custName)." -ForegroundColor Green

# 3. MÃ¼ÅŸteri listesini Ã§ek ve iÃ§inde olduÄŸunu doÄŸrula
$custList = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
$found = $custList | Where-Object { $_.id -eq $custId }
if ($found) {
    Write-Host "[PASS] GET /api/customers listesinde yeni mÃ¼ÅŸteri bulundu." -ForegroundColor Green
} else {
    Write-Host "[FAIL] MÃ¼ÅŸteri listede bulunamadÄ±!" -ForegroundColor Red
    exit 1
}

# 4. MÃ¼ÅŸteriyi ID ile doÄŸrudan getir
$custDetail = Invoke-RestMethod -Uri "$BaseUrl/customers/$custId" -Method Get -Headers $headers
if ($custDetail.id -eq $custId -and $custDetail.email -eq $custEmail) {
    Write-Host "[PASS] GET /api/customers/$custId detay sorgulamasÄ± doÄŸrulandÄ±." -ForegroundColor Green
} else {
    Write-Host "[FAIL] MÃ¼ÅŸteri detay bilgileri uyuÅŸmuyor!" -ForegroundColor Red
    exit 1
}

# 5. Var olmayan rastgele GUID ile 404 testi
$randomGuid = [guid]::NewGuid().ToString()
try {
    Invoke-RestMethod -Uri "$BaseUrl/customers/$randomGuid" -Method Get -Headers $headers | Out-Null
    Write-Host "[FAIL] Var olmayan mÃ¼ÅŸteri iÃ§in 404 dÃ¶nmedi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 404) {
        Write-Host "[PASS] Var olmayan ID iÃ§in beklenen 404 NotFound yanÄ±tÄ± alÄ±ndÄ±." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 02_103_customer_get_by_id_and_404 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


