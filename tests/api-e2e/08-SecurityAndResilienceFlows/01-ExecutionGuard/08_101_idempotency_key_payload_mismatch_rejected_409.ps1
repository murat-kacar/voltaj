# 08_101_idempotency_key_payload_mismatch_rejected_409.ps1
# Senaryo 08.1.01: AynÄ± Idempotency-Key ile FarklÄ± GÃ¶vde GÃ¶nderiminde 409 Conflict (VF-08101)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 08.1.01] Idempotency Key UyuÅŸmazlÄ±ÄŸÄ± (409)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. Ortak ve tekil bir Idempotency-Key Ã¼ret
$sharedIdempotencyKey = [guid]::NewGuid().ToString()

# 3. Ä°lk istek: Orijinal payload ile mÃ¼ÅŸteri oluÅŸtur
$firstCustomerBody = @{
    fullName = "Orijinal MÃ¼ÅŸteri " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "orig_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5551110001"
} | ConvertTo-Json

$headers = @{
    Authorization = "Bearer $adminToken"
    "Idempotency-Key" = $sharedIdempotencyKey
}

$firstResp = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $firstCustomerBody -ContentType "application/json; charset=utf-8"
Write-Host "[PASS] Ä°lk istek baÅŸarÄ±yla iÅŸlendi (MÃ¼ÅŸteri Id: $($firstResp.id))." -ForegroundColor Green

# 4. Ä°kinci istek: AYNI Idempotency-Key ile FARKLI bir payload gÃ¶nder -> 409 Conflict bekliyoruz
$tamperedCustomerBody = @{
    fullName = "DeÄŸiÅŸtirilmiÅŸ GÃ¶vde MÃ¼ÅŸterisi"
    email = "tampered_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5559990009"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $tamperedCustomerBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] FarklÄ± payload iÃ§eren aynÄ± idempotency key isteÄŸi kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 409) {
        Write-Host "[PASS] FarklÄ± payload beklenen 409 Conflict ile reddedildi." -ForegroundColor Green
        Write-Host "[PASS] ExecutionGuard: 'The idempotency key was already used with different request data' korumasÄ± doÄŸrulandÄ±." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 08_101_idempotency_key_payload_mismatch_rejected_409 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


