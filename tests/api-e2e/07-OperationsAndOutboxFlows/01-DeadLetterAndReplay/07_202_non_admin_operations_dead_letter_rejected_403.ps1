# 07_202_non_admin_operations_dead_letter_rejected_403.ps1
# Senaryo 07.2.02: Yetkisiz KullanÄ±cÄ±nÄ±n Operasyonel KuyruÄŸa EriÅŸim Engeli (403)

$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5275/api" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 07.2.02] Yetkisiz Operasyon EriÅŸimi Reddi (403)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Normal (Admin olmayan) teknisyen kullanÄ±cÄ±sÄ± ile oturum aÃ§
$techEmail = "tech_ops_" + (Get-Random -Minimum 10000 -Maximum 99999) + "@voltflow.com"
$techPass = "TechPass123!"

$regBody = @{
    name = "Field Technician"
    email = $techEmail
    password = $techPass
    otp = "000000"
} | ConvertTo-Json

Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json; charset=utf-8" | Out-Null

$loginResp = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body (@{ email = $techEmail; password = $techPass } | ConvertTo-Json) -ContentType "application/json; charset=utf-8"
$techToken = $loginResp.token
$headers = @{ Authorization = "Bearer $techToken" }
Write-Host "[INFO] Normal kullanÄ±cÄ± oturumu hazÄ±rlandÄ± ($techEmail)."

# 2. Yetkisiz kullanÄ±cÄ±nÄ±n /api/operations/outbox/dead-letter kuyruÄŸuna eriÅŸim denemesi -> 403
try {
    Invoke-RestMethod -Uri "$BaseUrl/operations/outbox/dead-letter" -Method Get -Headers $headers | Out-Null
    Write-Host "[FAIL] Normal kullanÄ±cÄ± operasyonel kuyruÄŸa eriÅŸebildi!" -ForegroundColor Red
    exit 1
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 403) {
        Write-Host "[PASS] Yetkisiz eriÅŸim beklenen 403 Forbidden ile engellendi." -ForegroundColor Green
        Write-Host "[PASS] RBAC Bariyeri: YalnÄ±zca 'Admin' rolÃ¼ operasyonel dead-letter kuyruÄŸunu gÃ¶rebilir." -ForegroundColor Green
        Write-Host "`n>>> [SUCCESS] 07_202_non_admin_operations_dead_letter_rejected_403 TAMAMLANDI <<<" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen HTTP Status: $code" -ForegroundColor Red
        exit 1
    }
}


