$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5275/api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [DRY-TEST 01.304] GeÃ§ersiz JWT Token (401)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$headers = @{ "Authorization" = "Bearer invalid.jwt.token.here.xyz" }
try {
    $res = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Get -Headers $headers
    Write-Host "[FAIL] GeÃ§ersiz token ile eriÅŸim kabul edilmemeliydi!" -ForegroundColor Red
    exit 1
}
catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -eq 401) {
        Write-Host "[PASS] GeÃ§ersiz JWT token reddedildi (401 Unauthorized)." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Beklenmeyen hata: $status" -ForegroundColor Red
        exit 1
    }
}
