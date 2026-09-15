# 03_104_enroute_without_assigned_rejected_422.ps1
# Senaryo 03.1.04: AtanmamÄ±ÅŸ Ä°ÅŸ Emrinin Yola Ã‡Ä±karÄ±lma Engeli (VF-03102)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.1.04] AtanmamÄ±ÅŸ Ä°ÅŸ Emrini Yola Ã‡Ä±karma Engeli" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. MÃ¼ÅŸteri, Teklif ve Ä°ÅŸ Emri oluÅŸtur (StatÃ¼: Open)
$customerBody = @{ fullName = "Yola Ã‡Ä±kÄ±ÅŸ Test MÃ¼ÅŸterisi"; email = "wo_enroute_" + (Get-Random) + "@voltflow.com"; phone = "5551234567" } | ConvertTo-Json
$customer = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $customerBody -ContentType "application/json; charset=utf-8"

$quoteBody = @{ customerId = $customer.id; title = "JeneratÃ¶r BakÄ±m" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $headers -Body $quoteBody -ContentType "application/json; charset=utf-8"

$itemBody = @{ description = "JeneratÃ¶r YaÄŸ DeÄŸiÅŸimi"; quantity = 1; unitPrice = 1200 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Headers $headers -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Headers $headers -Body (@{ requiredDepositPercentage = 0 } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null

$wo = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/work-order" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Ä°ÅŸ emri oluÅŸturuldu: $($wo.id) (Mevcut StatÃ¼: $($wo.status))" -ForegroundColor Gray

# 3. HenÃ¼z hiÃ§bir teknisyene atanmadan doÄŸrudan en-route yapmayÄ± dene -> 422 Bekleniyor
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/en-route" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] AtanmamÄ±ÅŸ iÅŸ emri yola Ã§Ä±karÄ±ldÄ±!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    $errorMsg = $_.ErrorDetails.Message

    if ($code -eq 422) {
        Write-Host "[PASS] AtanmamÄ±ÅŸ iÅŸ emrini yola Ã§Ä±karma isteÄŸi beklenen 422 ile engellendi." -ForegroundColor Green
        if ($errorMsg -match "Only assigned") {
            Write-Host "[PASS] ProblemDetails: 'Only assigned work orders can be marked as en-route.' kuralÄ± doÄŸrulandÄ±." -ForegroundColor Green
        }
        
        # 4. DB Invariant: StatÃ¼ Open olarak korundu
        $currentWo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)" -Method Get -Headers $headers
        if ($currentWo.status -eq "Open") {
            Write-Host "[PASS] DB Invariant: WorkOrders.Status deÄŸeri 'Open' olarak korundu." -ForegroundColor Green
        } else {
            Write-Host "[FAIL] DB Invariant bozuldu! Status: $($currentWo.status)" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "`n>>> [SUCCESS] 03_104_enroute_without_assigned_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP status: $code ($errorMsg)" -ForegroundColor Red
        exit 1
    }
}


