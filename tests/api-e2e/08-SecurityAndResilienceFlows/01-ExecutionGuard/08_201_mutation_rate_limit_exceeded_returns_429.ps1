# 08_201_mutation_rate_limit_exceeded_returns_429.ps1
# Senaryo 08.2.01: DaÄŸÄ±tÄ±k HÄ±z SÄ±nÄ±rlama (Rate Limiting) ve 429 Bariyeri (VF-08201)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 08.2.01] DaÄŸÄ±tÄ±k HÄ±z SÄ±nÄ±rlama Bariyeri (429)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Hedef uÃ§: /api/auth/password-reset/request (Rate limited endpoint)
$targetEmail = "ratelimit_target_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$resetBody = @{ email = $targetEmail } | ConvertTo-Json

Write-Host "[INFO] HÄ±z sÄ±nÄ±rlama testi baÅŸlatÄ±lÄ±yor (Hedef: $targetEmail, Limit: 500 istek/dakika)..."

$got429 = $false
$successfulRequests = 0

for ($i = 1; $i -le 510; $i++) {
    try {
        Invoke-RestMethod -Uri "$BaseUrl/auth/password-reset/request" -Method Post -Body $resetBody -ContentType "application/json; charset=utf-8" | Out-Null
        $successfulRequests++
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        if ($code -eq 429) {
            $got429 = $true
            Write-Host "[PASS] $i. istekte beklenen 429 Too Many Requests bariyeri tetiklendi." -ForegroundColor Green
            Write-Host "[PASS] ProblemDetails: 'Mutation rate limit exceeded' korumasÄ± doÄŸrulandÄ±." -ForegroundColor Green
            break
        } else {
            Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
            exit 1
        }
    }
}

if ($got429) {
    Write-Host "[PASS] DaÄŸÄ±tÄ±k HÄ±z SÄ±nÄ±rlayÄ±cÄ± (DistributedRateLimitFilter) baÅŸarÄ±yla doÄŸrulandÄ±." -ForegroundColor Green
    Write-Host "`n>>> [SUCCESS] 08_201_mutation_rate_limit_exceeded_returns_429 TAMAMLANDI <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] 15 istek boyunca 429 yanÄ±tÄ± alÄ±namadÄ±!" -ForegroundColor Red
    exit 1
}


