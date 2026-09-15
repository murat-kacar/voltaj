# run-all.ps1 - Tüm Dry Testleri Çalıştıran Dikey (Deep) Hiyerarşik Ana Otomasyon Scripti
$ErrorActionPreference = "Continue"

Write-Host "==================================================" -ForegroundColor Yellow
Write-Host " 🚀 VOLTFLOW FLOW-BASED DRY-TEST KOŞUMU" -ForegroundColor Yellow
Write-Host " Tarih: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor Yellow

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Recursive search
$testsList = Get-ChildItem -Path $scriptDir -Recurse -Filter "*.ps1" | Where-Object { $_.Name -ne "run-all.ps1" } | Sort-Object FullName

$passed = 0
$failed = 0

foreach ($testFile in $testsList) {
    $path = $testFile.FullName
    $relativePath = $path.Substring($scriptDir.Length + 1)
    Write-Host "`n>>> ÇALIŞTIRILIYOR: $relativePath <<<" -ForegroundColor White
    try {
        & $path
        if ($LASTEXITCODE -eq 0 -or $? -eq $true) {
            $passed++
        } else {
            $failed++
        }
    } catch {
        Write-Host "Hata oluştu: $_" -ForegroundColor Red
        $failed++
    }
}

Write-Host "`n==================================================" -ForegroundColor Yellow
Write-Host " 🏁 AKIŞ TABANLI (FLOW) TEST SONUÇ RAPORU" -ForegroundColor Yellow
Write-Host " Başarılı: $passed / $($testsList.Count)" -ForegroundColor Green
if ($failed -gt 0) {
    Write-Host " Başarısız: $failed" -ForegroundColor Red
}
Write-Host "==================================================" -ForegroundColor Yellow

