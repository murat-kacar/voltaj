# 08_401_rfc7807_problem_details_structure_compliance.ps1
# Senaryo 08.4.01: RFC 7807 Problem Details UyumluluÄŸu

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 08.4.01] RFC 7807 Problem Details UyumluluÄŸu" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Bilerek hatalÄ± bir istek yap - yanlÄ±ÅŸ ÅŸifre ile login
$body = @{ email = "admin@voltflow.com"; password = "WrongPassword!" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $body -ContentType "application/json; charset=utf-8"
    Write-Host "[FAIL] YanlÄ±ÅŸ ÅŸifre ile login baÅŸarÄ±lÄ± olmamalÄ±ydÄ±!" -ForegroundColor Red
    exit 1
}
catch {
    # PowerShell 7: ErrorDetails.Message doğrudan response body içerir
    $errorBody = $_.ErrorDetails.Message
    if (-not $errorBody) {
        # Fallback: exception mesajından dene
        $errorBody = $_.Exception.Message
    }
    if (-not $errorBody) {
        Write-Host "[FAIL] Hata yanıtında body yok!" -ForegroundColor Red
        exit 1
    }

    $problem = $errorBody | ConvertFrom-Json

    # RFC 7807 zorunlu alanlarÄ± kontrol et
    $hasType = $null -ne $problem.type
    $hasTitle = $null -ne $problem.title
    $hasStatus = $null -ne $problem.status

    if ($hasType -and $hasTitle -and $hasStatus) {
        Write-Host "[PASS] RFC 7807 uyumlu: type=$($problem.type)" -ForegroundColor Green
        Write-Host "[PASS] RFC 7807 uyumlu: title=$($problem.title)" -ForegroundColor Green
        Write-Host "[PASS] RFC 7807 uyumlu: status=$($problem.status)" -ForegroundColor Green

        if ($problem.detail) {
            Write-Host "[PASS] Opsiyonel alan: detail=$($problem.detail)" -ForegroundColor Green
        }
        exit 0
    } else {
        Write-Host "[FAIL] RFC 7807 zorunlu alanlarÄ± eksik! type=$hasType, title=$hasTitle, status=$hasStatus" -ForegroundColor Red
        Write-Host "[INFO] Hata yanÄ±tÄ±: $errorBody" -ForegroundColor Gray
        exit 1
    }
}


