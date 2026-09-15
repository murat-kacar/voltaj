# 03_402_complete_without_proof_rejected_422.ps1
# Senaryo 03.4.02: KanÄ±tsÄ±z (Ä°mza ve FotoÄŸrafsÄ±z) Ä°ÅŸ Emri Tamamlama Reddi (VF-03401)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 03.4.02] KanÄ±tsÄ±z Tamamlama Engeli (422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. MÃ¼ÅŸteri, Teklif ve Ä°ÅŸ Emri oluÅŸtur
$customerBody = @{ fullName = "KanÄ±t DoÄŸrulama MÃ¼ÅŸterisi"; email = "wo_proof_" + (Get-Random) + "@voltflow.com"; phone = "5551234567" } | ConvertTo-Json
$customer = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Headers $headers -Body $customerBody -ContentType "application/json; charset=utf-8"

$quoteBody = @{ customerId = $customer.id; title = "Trafo BakÄ±m Hizmeti" } | ConvertTo-Json
$quote = Invoke-RestMethod -Uri "$BaseUrl/quotes" -Method Post -Headers $headers -Body $quoteBody -ContentType "application/json; charset=utf-8"

$itemBody = @{ description = "Trafo Testi"; quantity = 1; unitPrice = 2500 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/items" -Method Post -Headers $headers -Body $itemBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/issue" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/accept" -Method Post -Headers $headers -Body (@{ requiredDepositPercentage = 0 } | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null

$wo = Invoke-RestMethod -Uri "$BaseUrl/quotes/$($quote.id)/work-order" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8"
Write-Host "[INFO] Ä°ÅŸ emri oluÅŸturuldu: $($wo.id)" -ForegroundColor Gray

# 3. Teknisyene ata, Ä°SG checklist tamamla ve baÅŸlat
$assignBody = @{ employeeUserId = [guid]::NewGuid() } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/assign" -Method Post -Headers $headers -Body $assignBody -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/safety-checklist" -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" | Out-Null
Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/start" -Method Post -Headers $headers -Body (@{} | ConvertTo-Json) -ContentType "application/json; charset=utf-8" | Out-Null

# 4. KanÄ±tsÄ±z (Ä°mza yok, FotoÄŸraf yok) tamamlama isteÄŸi gÃ¶nder -> 422 Bekleniyor
$emptyProofBody = @{
    signature = ""
    photoUrl = ""
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)/complete" -Method Post -Headers $headers -Body $emptyProofBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] KanÄ±tsÄ±z tamamlama isteÄŸi kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    $errorMsg = $_.ErrorDetails.Message

    if ($code -eq 422) {
        Write-Host "[PASS] KanÄ±tsÄ±z tamamlama isteÄŸi beklenen 422 Unprocessable Entity ile reddedildi." -ForegroundColor Green
        if ($errorMsg -match "Proof of work") {
            Write-Host "[PASS] ProblemDetails: 'Proof of work (signature or photo) is required' kuralÄ± doÄŸrulandÄ±." -ForegroundColor Green
        }
        
        # 5. DB State Invariant DoÄŸrulamasÄ±: StatÃ¼ InProgress kalmalÄ±, Completed olmamalÄ±
        $currentWo = Invoke-RestMethod -Uri "$BaseUrl/workorders/$($wo.id)" -Method Get -Headers $headers
        if ($currentWo.status -eq "InProgress") {
            Write-Host "[PASS] DB Invariant: WorkOrders.Status deÄŸeri 'InProgress' olarak korundu." -ForegroundColor Green
        } else {
            Write-Host "[FAIL] DB Invariant bozuldu! Status: $($currentWo.status)" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "`n>>> [SUCCESS] 03_402_complete_without_proof_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP status: $code ($errorMsg)" -ForegroundColor Red
        exit 1
    }
}


