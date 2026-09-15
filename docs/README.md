# Deployment

- [Production runbook](deployment/voltflow-production-runbook.md)

# Voltflow Documentation

Bu klasör, Voltflow projesinin mimari ve tasarım kararlarını içerir.

## İçerik

- [Voltflow .NET Mimari Rehberi](architecture/voltflow-dotnet-architecture.md)
- [Voltflow Architecture Summary](architecture/voltflow-architecture-summary.md)
- [Voltflow Domain Model Listesi](architecture/voltflow-domain-models.md)
- [Voltflow İlk Sprint Planı](architecture/voltflow-first-sprint-plan.md)
- [Voltflow Interface Map](architecture/voltflow-interface-map.md)
- [Voltflow Interface Relationship Diagram](architecture/voltflow-interface-relationship-diagram.md)
- [Voltflow İş Akışları ve Arıza/İptal Matrisi](architecture/voltflow-workflows-and-failure-modes.md)
- [Voltflow Unified Taxonomy (VUT) Numaralandırma Mantığı ve Tam Kod Kataloğu](architecture/voltflow-unified-taxonomy-guide.md)
- [Voltflow 16-Halkalı Evrensel İzlenebilirlik Matrisi](architecture/voltflow-16-ring-traceability-matrix.md)
- [Voltflow Kapsamlı Dry-Test Rehberi](architecture/voltflow-dry-tests-guide.md)
- [Voltflow API-E2E Test Kütüphanesi](../tests/api-e2e/README.md)
- [Voltflow UI-E2E Test Paketi](../tests/ui-e2e/README.md)
- [Voltflow Backend C# Test Katmanı](../tests/backend/README.md)
- [Voltflow REST Client Koleksiyonu](../tests/requests.http)

## Amaç

Bu dökümanlar, veritabanı şemasından sonra gerekli olan domain, application, infrastructure ve deployment kararlarını netleştirir.

- Modüler monolith yaklaşımı
- Domain-first tasarım
- Transaction-safe operasyonlar
- Tek firma odaklı kurulum
- State machine ve workflow odaklı model
- .NET 10 + ASP.NET Core + PostgreSQL stack

## Kullanim

- [Voltflow API Kullanim Kilavuzu](usage/voltflow-api-kullanim-kilavuzu.md)
- [Voltflow UI/UX Blueprint](uiux/voltflow-uiux-blueprint.md)
