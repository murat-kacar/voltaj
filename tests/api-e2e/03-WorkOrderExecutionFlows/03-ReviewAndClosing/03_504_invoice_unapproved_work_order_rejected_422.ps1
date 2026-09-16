# 03_504_invoice_unapproved_work_order_rejected_422.ps1
# Senaryo 03.5.04: OnaysÄ±z Ä°ÅŸ Emrini Faturalama Engeli (VF-03501)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.5.04] OnaysÄ±z Faturalama Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. MÃ¼ÅŸteri, Teklif ve Ä°ÅŸ Emri oluÅŸtur
$customerBody = @{ fullName = "Fatura Onay MÃ¼ÅŸterisi"; email = "wo_inv_" + (Get-Random) + "@voltflow.com"; phone = "5551234567" } | ConvertTo-Json
$customer = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $customerBody -ContentType "application/json; charset=utf-8"

$quoteBody = @{ customerId = $customer.id; title = "Pano Yenileme" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $headers -Body $quoteBody -ContentType "application/json; charset=utf-8"

$itemBody = @{ description = "Pano Montaj"; quantity = 1; unitPrice = 5000 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Headers $headers -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Headers $headers -Body (@{ requiredDepositPercentage = 0 } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null

$wo = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/work-order" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"

# 3. Ä°ÅŸi baÅŸlat ve geÃ§erli kanÄ±t ile tamamla (StatÃ¼: Completed)
$assignBody = @{ employeeUserId = [guid]::NewGuid() } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/safety-checklist" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/start" -Method Post -Headers $headers -Body (@{} | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null

$completeBody = @{ signatureData = "data:image/png;base64,mockSignature" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/complete" -Method Post -Headers $headers -Body $completeBody -ContentType "application/json; charset=utf-8" | Out-Null
Write-Host "[INFO] Ä°ÅŸ emri tamamlandÄ± (StatÃ¼: Completed)." -ForegroundColor Gray

# 4. Ofis onayÄ± (approve-billing) ALMADAN doÄŸrudan faturalama isteÄŸi gÃ¶nder -> 422 Bekleniyor
try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/invoice" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] OnaysÄ±z iÅŸ emri faturalandÄ±!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    $errorMsg = $_.ErrorDetails.Message

    if ($code -eq 422) {
        Write-Host "[PASS] OnaysÄ±z faturalama beklenen 422 Unprocessable Entity ile engellendi." -ForegroundColor Green
        if ($errorMsg -match "Only approved") {
            Write-Host "[PASS] ProblemDetails: 'Only approved (ready for billing) work orders can be invoiced.' kuralÄ± doÄŸrulandÄ±." -ForegroundColor Green
        }
        
        # 5. DB Invariant: StatÃ¼ Invoiced olmadÄ±, Completed olarak korundu
        $currentWo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)" -Method Get -Headers $headers
        if ($currentWo.status -eq "Completed") {
            Write-Host "[PASS] DB Invariant: WorkOrders.Status deÄŸeri 'Completed' olarak korundu." -ForegroundColor Green
        } else {
            Write-Host "[FAIL] DB Invariant bozuldu! Status: $($currentWo.status)" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "`n>>> [SUCCESS] 03_504_invoice_unapproved_work_order_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP status: $code ($errorMsg)" -ForegroundColor Red
        exit 1
    }
}


