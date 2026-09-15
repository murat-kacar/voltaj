# 04_204_stock_reservation_negative_quantity_rejected_422.ps1
# Senaryo 04.2.04: Negatif MiktarlÄ± Stok Rezervasyonu Engeli (VF-04202)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 04.2.04] Negatif Stok Rezervasyonu Engeli (400/422)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Admin ile oturum aÃ§
$adminLogin = @{ email = "admin@voltflow.com"; password = "Admin123!" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLogin -ContentType "application/json; charset=utf-8").token
$headers = @{ Authorization = "Bearer $adminToken" }

# 2. Mevcut NYY-4X16 stoÄŸunu sorgula
$matCode = "NYY-4X16"
$initialStock = Invoke-RestMethod -Uri "$BaseUrl/inventory/$matCode" -Method Get -Headers $headers
Write-Host "[INFO] Malzeme: $matCode (Mevcut: $($initialStock.quantityOnHand), Rezerve: $($initialStock.reservedQuantity))" -ForegroundColor Gray

# 3. Negatif miktarla (-15 adet) rezervasyon isteÄŸi gÃ¶nder -> 400/422 Bekleniyor
$negativeReserveBody = @{
    materialCode = $matCode
    quantity = -15
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$BaseUrl/inventory/reserve" -Method Post -Headers $headers -Body $negativeReserveBody -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Host "[FAIL] Negatif stok rezervasyonu kabul edildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    $errorMsg = $_.ErrorDetails.Message

    if ($code -in 400, 422) {
        Write-Host "[PASS] Negatif stok rezervasyonu beklenen HTTP $code ile engellendi." -ForegroundColor Green
        if ($errorMsg -match "greater than zero|positive|cannot be negative") {
            Write-Host "[PASS] ProblemDetails: Negatif miktar kural ihlali doÄŸrulandÄ± ('$errorMsg')." -ForegroundColor Green
        }
        
        # 4. DB Invariant: ReservedQuantity deÄŸiÅŸmedi
        $afterStock = Invoke-RestMethod -Uri "$BaseUrl/inventory/$matCode" -Method Get -Headers $headers
        if ($afterStock.reservedQuantity -eq $initialStock.reservedQuantity) {
            Write-Host "[PASS] DB Invariant: MaterialStock.ReservedQuantity deÄŸiÅŸmedi ($($afterStock.reservedQuantity))." -ForegroundColor Green
        } else {
            Write-Host "[FAIL] DB Invariant bozuldu! Rezerve: $($afterStock.reservedQuantity)" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "`n>>> [SUCCESS] 04_204_stock_reservation_negative_quantity_rejected_422 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP status: $code ($errorMsg)" -ForegroundColor Red
        exit 1
    }
}


