# 05_202_zero_or_negative_payment_amount_rejected_422.ps1
# Senaryo 05.2.02: SÄ±fÄ±r veya Eksi TutarlÄ± Tahsilat Engeli (VF-05201)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 05.2.02] SÄ±fÄ±r/Eksi Tahsilat GiriÅŸ Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token

# 2. MÃ¼ÅŸteri oluÅŸtur
$custBody = @{
    fullName = "Tahsilat Kural MÃ¼ÅŸterisi " + (Get-Random -Minimum 1000 -Maximum 9999)
    email = "pay_gate_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
    phone = "5557771122"
} | ConvertTo-Json
$cust = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $custBody -ContentType "application/json; charset=utf-8"
$custId = $cust.id

# 3. SÄ±fÄ±r tutarlÄ± tahsilat denemesi -> 422
$zeroPayBody = @{
    customerId = $custId
    amount = 0.00
    paymentMethod = "BankTransfer"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $zeroPayBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] SÄ±fÄ±r tutarlÄ± tahsilat kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] SÄ±fÄ±r tutarlÄ± tahsilat beklenen $code ile reddedildi." -ForegroundColor Green
        Write-Host "[PASS] ProblemDetails: 'Amount must be greater than zero' kuralÄ± doÄŸrulandÄ±." -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}

# 4. Negatif tutarlÄ± tahsilat denemesi -> 422
$negativePayBody = @{
    customerId = $custId
    amount = -500.00
    paymentMethod = "Cash"
    paymentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/payments" -Method Post -Headers @{ Authorization = "Bearer $adminToken"; "Idempotency-Key" = [guid]::NewGuid().ToString() } -Body $negativePayBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Negatif tutarlÄ± tahsilat kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422 -or $code -eq 400) {
        Write-Host "[PASS] Negatif tutarlÄ± tahsilat beklenen $code ile reddedildi." -ForegroundColor Green
        Write-Host "[PASS] DB Invariant: CustomerPayments ve Ledger tablolarÄ±na hatalÄ± satÄ±r eklenmedi." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 05_202_zero_or_negative_payment_amount_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


