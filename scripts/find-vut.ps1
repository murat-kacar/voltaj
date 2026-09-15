<#
.SYNOPSIS
    Voltflow 18-Halkalı Evrensel VUT İzlenebilirlik ve Nokta Atışı Sorgulama Aracı (find-vut)
.DESCRIPTION
    Herhangi bir VUT kodu (01_103, 01.1.03, 01103), hata kodu (VF-01101), aksiyon (ACT-01103)
    veya anahtar kelime girildiğinde 18 halkanın tamamındaki tüm dosya, test, kod, tablo, i18n
    ve FSM geçiş kuralı karşılıklarını anında ekrana döker.
.EXAMPLE
    pwsh .\scripts\find-vut.ps1 01_103
    pwsh .\scripts\find-vut.ps1 VF-01101
    pwsh .\scripts\find-vut.ps1 "password"
#>
param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Query
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$jsonPath = Join-Path $repoRoot "docs\architecture\voltflow-traceability.json"

if (-not (Test-Path $jsonPath)) {
    Write-Host "HATA: İzlenebilirlik veri tabanı bulunamadı: $jsonPath" -ForegroundColor Red
    exit 1
}

$rawJson = Get-Content -Path $jsonPath -Raw -Encoding UTF8
$data = ConvertFrom-Json $rawJson

# Normalize search query
$cleanQ = $Query.Trim().ToLower()
$normQ = $cleanQ -replace '[\._\-]', ''

$matches = @()

foreach ($prop in $data.PSObject.Properties) {
    $item = $prop.Value
    $matchScore = 0

    $flat = $item.vut_flat.ToLower()
    $dotted = $item.vut_code.ToLower()
    $fileCode = $item.vut_file.ToLower()
    $err = $item.error_code.ToLower()
    $act = $item.action_id.ToLower()
    $title = $item.title.ToLower()
    $apiPath = $item.api_e2e_path.ToLower()

    if ($flat -eq $normQ -or $dotted -eq $cleanQ -or $fileCode -eq $cleanQ -or $err -eq $cleanQ -or $act -eq $cleanQ) {
        $matchScore = 100
    } elseif ($flat -like "*$normQ*" -or $title -like "*$cleanQ*" -or $err -like "*$cleanQ*" -or $apiPath -like "*$cleanQ*") {
        $matchScore = 50
    }

    if ($matchScore -gt 0) {
        $matches += [PSCustomObject]@{ Score = $matchScore; Item = $item }
    }
}

if ($matches.Count -eq 0) {
    Write-Host "`n[!] '$Query' sorgusuna uyan hiçbir VUT senaryosu bulunamadı." -ForegroundColor Yellow
    Write-Host "İpucu: 01_103, 01.1.03, VF-01101, ACT-01101 veya anahtar kelime deneyin." -ForegroundColor Gray
    exit 0
}

# Sort by relevance
$sorted = $matches | Sort-Object -Property Score -Descending | Select-Object -ExpandProperty Item

Write-Host "`n==================================================================" -ForegroundColor Cyan
Write-Host " 🎯 VOLTFLOW 18-HALKALI KANONİK VUT ARAMA SONUÇLARI ($($sorted.Count) Eşleşme)" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

foreach ($item in $sorted) {
    Write-Host "`n------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host " 📍 KANONİK VUT: [$($item.vut_code)]  (Düz Kod: $($item.vut_flat) | Dosya Kodu: $($item.vut_file))" -ForegroundColor Green
    Write-Host " 🏷️  Tanım: $($item.title)" -ForegroundColor White
    Write-Host " 📦 Modül: [$($item.module_code)] $($item.module_name) > [$($item.feature_code)] $($item.feature_name)" -ForegroundColor Gray
    Write-Host "------------------------------------------------------------------" -ForegroundColor DarkGray

    Write-Host " [1] Dokümantasyon      : $($item.doc_link)" -ForegroundColor Magenta
    Write-Host " [2] Graphify AST Hub   : $($item.graphify_hub)" -ForegroundColor DarkCyan
    Write-Host " [3] OpenAPI Endpoint   : $($item.backend_endpoint_cs)" -ForegroundColor Blue
    Write-Host " [4] Frontend UI (.tsx) : $($item.frontend_ui)" -ForegroundColor Blue
    Write-Host " [5] UI-E2E Testi       : $($item.ui_e2e_path)" -ForegroundColor Cyan
    if ($item.ui_e2e_cmd -ne "N/A") {
        Write-Host "     └─ Komut           : $($item.ui_e2e_cmd)" -ForegroundColor DarkGray
    }
    Write-Host " [6] API-E2E Testi      : $($item.api_e2e_path)" -ForegroundColor Cyan
    Write-Host "     └─ Komut           : $($item.api_e2e_cmd)" -ForegroundColor Yellow
    Write-Host " [7] CI/CD Test Tag     : VUT=$($item.vut_flat)" -ForegroundColor Gray
    Write-Host " [8] RFC 7807 Hata Kodu : $($item.error_code)" -ForegroundColor Red
    Write-Host " [9] Log & Trace        : Screen: $($item.screen_id) | Action: $($item.action_id)" -ForegroundColor DarkYellow
    Write-Host " [10] Idempotency Scope : $($item.idempotency_scope)" -ForegroundColor DarkGray
    Write-Host " [11] Outbox Event Type : $($item.event_type)" -ForegroundColor DarkGray
    Write-Host " [12] Backend C# Testi  : $($item.backend_test_path)" -ForegroundColor Green
    Write-Host "      └─ Test Komutu    : $($item.backend_test_cmd)" -ForegroundColor Yellow
    Write-Host "      └─ Servis Kodu    : $($item.backend_service_cs)" -ForegroundColor DarkGreen
    Write-Host " [13] Veritabanı Tablosu: $($item.db_tables -join ', ')" -ForegroundColor DarkCyan
    Write-Host " [14] REST Client (.http): $($item.http_client)" -ForegroundColor White
    Write-Host " [15] RBAC Yetkilendirme: PERM_$($item.vut_flat)" -ForegroundColor DarkMagenta
    Write-Host " [16] Frontend Rota     : $($item.frontend_route)" -ForegroundColor Gray
    Write-Host " [17] i18n Dil Anahtarı : $($item.i18n_key)" -ForegroundColor Cyan
    Write-Host "      └─ [en-US]        : `"$($item.i18n_en)`"" -ForegroundColor White
    Write-Host "      └─ [tr-TR]        : `"$($item.i18n_tr)`"" -ForegroundColor Gray
    Write-Host " [18] FSM Geçiş Kuralı  : $($item.fsm_transition)" -ForegroundColor Magenta
}

Write-Host "`n==================================================================" -ForegroundColor Cyan
