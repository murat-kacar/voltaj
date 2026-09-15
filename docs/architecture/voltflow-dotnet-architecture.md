# Voltflow .NET Mimari Rehberi

## 1. Yüksek seviyeli karar

Voltflow için doğru mimari yaklaşımı: Modüler Monolith + Domain-Driven Design + Workflow-driven state model.

Neden?

- Proje operasyonel olarak güçlü ama yönetilebilir bir iş akışı sistemidir.
- Küçük ve orta ölçekli işletmeler için erken evrede ayrı VPS ve ayrı veritabanı modeline gitmek gereksiz maliyet ve operasyon yükü getirir.
- Hızlı geliştirme, kolay bakım ve kontrollü ölçekleme gerektirir.
- Mikroservisler erken evrede gereksiz koordinasyon ve operasyonel karmaşıklık yaratır.

Bu yüzden ana yapı:

- tek uygulama
- modüler domain katmanları
- tek üretim kurulumunda tek veritabanı
- güçlü transaction sınırları
- background jobs
- tek firma odaklı operasyon ve yetki modeli

> Uyarı: platform yapısı ve çoklu-tenant izolasyonu bu aşamada askıya alınmıştır. Uygulama tek bir firmanın operasyonel ihtiyacına göre tasarlanır; sonraki büyüme evresinde gerekirse ayrı deployment veya izolasyon planı eklenebilir.

## 2. Temel hedefler

Voltflow, aşağıdaki iş değerlerini üretmeyi hedefler:

- Müşteri adayından nihai müşteri ve iş emrine kadar görünürlük
- Teklif, iş emri, stok, faturalama ve tahsilat akışının tek çatı altında yönetimi
- Teknik ekip, ofis ve yöneticiler için roller bazlı erişim
- Saha verilerinin kanonik ve güvenilir kayda dönüşümü
- Finansal ve stok işlemlerinin tam izlenebilirliği
- Tek firma için güvenli, net ve operasyonel çalışma ortamı

## 3. Mimari prensipleri

### 3.1 Aday ve nihai veri yaklaşımı

Veri modeli şu ilkeye dayanır:

- Aday kayıtlar geçici ve eksik olabilir.
- Nihai kayıtlar eksiksiz ve doğrulanmış olmalıdır.
- Aynı gerçeklik birden fazla yerde tutulmaz; yalnızca kanonik kaydın güvenilirliği önemlidir.
- Durumlar alanla değil tablo ve transition flow ile izlenmelidir.

Bu yaklaşım, veri bütünlüğü ve operasyonel güveni sağlar.

### 3.2 State machine yaklaşımı

Durum tek alan yerine state/transition modeli ile yönetilir, ancak gereksiz state tablolarına kaçılmamalıdır.

Örnekler:

- Quote: Draft -> Issued -> Accepted / Rejected / Expired
- WorkOrder: Open -> Assigned -> InProgress -> Completed -> Invoiced
- Reminder: Pending -> Sent -> Completed / Dismissed
- Export: Pending -> Processing -> Completed / Failed

Temel kural:

- ana varlıkta state alanı vardır
- geçiş kayıtları (transition/event) ayrı tutulur
- ekstra metadata gerekiyorsa sadece o state için özel tablo/ek alan kullanılır
- her state için ayrı tablo açmak overengineering'e girer

Her geçişte:

- actor bilgisi
- timestamp
- neden/mesaj
- geçerli kurallar
- audit kaydı

bulunmalıdır.

### 3.3 Transaction-first model

Aşağıdaki işler tek transaction içinde yapılmalıdır:

- Stock movement + ledger update + approval check
- Work order completion + material consumption + margin update
- Quote conversion to work order
- Payment allocation to invoice / progress bill
- Progress billing approval

İdempotency key zorunlu olmalıdır.

## 4. Bounded contexts

### 4.1 Customers

Sorumluluk:

- candidate customer
- customer conversion
- customer notes
- customer profiles
- party model
- customer ledger

Ana aggregate'ler:

- CustomerAggregate
- PartyAggregate

### 4.2 Quotes

Sorumluluk:

- quote draft creation
- quote conversion
- issue / accept / reject / expire
- item pricing
- quote document generation

Ana aggregate:

- QuoteAggregate

### 4.3 Work Orders

Sorumluluk:

- work order creation
- assignment
- labor and material consumption
- completion
- invoice linkage

Ana aggregate:

- WorkOrderAggregate

### 4.4 Inventory

Sorumluluk:

- material master
- stock balances
- stock movements
- inventory valuation
- purchase integration

Ana aggregate:

- StockAggregate

### 4.5 Purchasing

Sorumluluk:

- supplier management
- purchase invoice
- supplier ledger
- payment obligations

### 4.6 Finance

Sorumluluk:

- customer payments
- direct cash/bank transactions
- payment allocation
- ledger tracking
- financial accounts

Ana aggregate:

- PaymentAggregate
- LedgerAggregate

### 4.7 Projects

Sorumluluk:

- projects
- project phases
- work order to project mapping
- progress billings
- approvals

### 4.8 Documents and Delivery

Sorumluluk:

- generated PDFs
- document delivery
- sent / failed deliveries
- attachment metadata

### 4.9 Notifications and Reminders

Sorumluluk:

- reminders
- SLA warnings
- due payment alerts
- low stock warnings

### 4.10 Identity and Access

Sorumluluk:

- app users
- roles
- permissions
- sessions
- audit trail

### 4.11 Work Orders

Mevcut proje içindeki gerçek iş akışı, "Customer" ve "Quote" ile başlatılmış bir domain yaklaşımına dayanır. Work Order modülünün eklenmesi gerektiğinde temel olarak şunlar gerçek müşteri/quote/servis akışından türetilir:

- work order creation from accepted quote or direct service request
- technician assignment and status transitions
- labor and material consumption tracking
- completion and invoicing linkage

Bu modül, mevcut uygulama katmanlarıyla uyumlu biçimde şu pattern'i izler:

- Domain: WorkOrder aggregate, WorkOrderItem, WorkOrderStatus
- Application: IWorkOrderService, WorkOrderService
- Infrastructure: IWorkOrderRepository ve EF Core mapping
- API: /api/workorders endpoints

### 4.12 Inventory and Stock

Inventory/Stock modülünün temel sorumluluğu, mevcut schema içindeki stock ve material modeline dayanır:

- material master
- stock balances and stock movement records
- stock movement direction and ledger-like traceability
- stock reservation / release flows

Mevcut schema'da bu konseptler "material_categories", "material_units", "stock_location_types", "stock_movement_types" ve stock tables ile görünür. Bu nedenle uygulama katmanları bu model üzerinden genişletilir:

- Domain: Material, StockEntry, StockMovement
- Application: IInventoryService, IStockService
- Infrastructure: IInventoryRepository, IStockMovementRepository
- API: /api/inventory, /api/stock

### 4.13 Project and Billing

Project ve billing akışı, schema içindeki project tables ve financial tables ile açıkça uyumludur:

- projects and project phases
- project-work order relationship
- progress billing and invoice linkage
- customer payment and allocation flows

Bu modül için uygulanacak temel yapı:

- Domain: Project, ProjectPhase, BillingEntry, PaymentAllocation
- Application: IProjectService, IBillingService, IPaymentService
- Infrastructure: IProjectRepository, IBillingRepository
- API: /api/projects, /api/billing, /api/payments

### 4.14 Auth and Role Model

Mevcut schema, auth ve role sistemini zaten içerir:

- app_users
- application_roles
- app_user_roles
- user_sessions
- user_accounts
- verification_tokens

Bu nedenle auth/role layer'i, mevcut uygulama için ayrı bir platform katmanı değil, tek firmanın operasyonel identity katmanı olarak düşünülür.

- Domain: User, Role, Session
- Application: IAuthService, IUserRoleService
- Infrastructure: IUserRepository, IRoleRepository, ISessionRepository
- API: /api/auth, /api/users, /api/roles

## 5. Current implementation pattern in codebase

Mevcut kod, aşağıdaki pattern'i açıkça göstermektedir:

- Domain katmanı: aggregate root, invariant checks, base entity, guard logic
- Application katmanı: DTO ve use-case interface'leri
- Infrastructure katmanı: DbContext ve repository implementation'ları
- API katmanı: minimal API endpoints ve DI registrasyonları

Bu yapı, "modüler monolith" hedefi ile uyumludur. Platform-level multi-tenant soyutlaması şu an için projede yer almaz; schema ve docs da bu kararı açıkça destekler.

### 5.1 DbContext and DI pattern

Mevcut uygulama örneği:

```csharp
builder.Services.AddDbContext<VoltflowDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
```

Bu pattern, sonraki module'lar için aynı şekilde genişletilir: WorkOrder, Inventory, Project, Billing, Auth.

## 6. Aggregate root ve invariant örnekleri

### QuoteAggregate

Zorunlu invariants:

- Draft quote only editable in Draft state.
- Issued quote cannot be changed arbitrarily.
- Accepted, rejected and expired quotes become terminal states.
- Quote total must equal subtotal - discount + VAT.
- Quote must belong to a valid party.

### WorkOrderAggregate

Zorunlu invariants:

- Completion before invoice is optional; invoice linkage must be valid.
- Labor and material quantities must have valid value constraints.
- Technician assignment cannot exceed valid operational scope.
- Completion requires responsible actor and evidence.

### StockAggregate

Zorunlu invariants:

- Quantity cannot go below zero.
- Movement must carry a valid movement type.
- Ledger update and stock update must be transactional.
- Generated stock summary values must be recomputed from canonical data.

### PaymentAggregate

Zorunlu invariants:

- Allocation total cannot exceed invoice amount.
- Payment amount must be positive.
- Allocation towards invoice and progress bill requires business rules.
- State transition must be auditable.

## 6. Application layer tasarımı

### Layer yapısı

src/
  Voltflow.Api/
  Voltflow.Application/
  Voltflow.Domain/
  Voltflow.Infrastructure/
  Voltflow.Shared/
  Voltflow.Worker/
  Voltflow.Tests/

### Application katmanının sorumluluğu

- Command ve Query tasarımı
- Handler yapısı
- DTO ve mapper
- Validation
- Authorization policy
- Result pattern
- Orchestration between domain and infrastructure

### Örnek command'lar

- CreateCustomerCommand
- ConvertCandidateCustomerCommand
- IssueQuoteCommand
- AcceptQuoteCommand
- CreateWorkOrderCommand
- CompleteWorkOrderCommand
- AllocatePaymentCommand
- ExportDataCommand

### Örnek query'ler

- GetCustomerByIdQuery
- GetOpenWorkOrdersQuery
- GetStockBalanceByMaterialQuery
- GetOverdueReceivablesQuery
- GetProjectBillingSummaryQuery

## 7. Infrastructure katmanı

### 7.1 Veritabanı

- PostgreSQL
- Npgsql
- EF Core 10
- explicit migrations
- idempotent transaction support

### 7.2 Auth

- ASP.NET Core Identity veya OpenIddict
- resource-based authorization
- tenant-aware claims

### 7.3 Jobs

- Hangfire + PostgreSQL storage
- reminder jobs
- document generation jobs
- scheduled exports
- late payment notification

### 7.4 Storage

- MinIO or S3-compatible object storage
- file metadata table
- attachment linkage
- validation and lifecycle management

### 7.5 Documents

- PDF generation
- Excel export
- invoice / quote / report rendering

### 7.6 Observation

- Serilog
- OpenTelemetry
- Sentry
- health checks

## 8. Security ve Yetkilendirme

Rol modeli tek başına yeterli olmaz; izin modeline geçmek gerekir.

Örnek izin grupları:

- Admin
- Office
- Technician
- Finance
- Customer support
- Read-only manager

Permission örnekleri:

- customer.read
- customer.write
- quote.issue
- quote.accept
- finance.approve
- stock.adjust
- export.download

Ayrıca her işlem için:

- actor id
- action
- resource id
- timestamp
- audit log

olmalıdır.

## 9. Deployment modeli

### Single-company deployment model

Varsayılan üretim yaklaşımı:

- tek uygulama instance'ı
- tek PostgreSQL instance'ı
- tek firma için tek veri seti
- tek operasyonel çalışma alanı
- merkezi yapılandırma ve güvenlik yönetimi

Bu model, küçük-orta ölçekli firma için en düşük operasyonel yükü ve en hızlı değer üretimini sağlar. Çoklu-tenant ve platform katmanı, şu an için gereksiz ve maliyetli bir soyutlamadır.

## 10. Geliştirme önceliği

### Phase 1 - Core business

- Customer and party lifecycle
- Quote lifecycle
- Work order lifecycle
- Material and stock
- Finance and payment allocation
- Audit trail

### Phase 2 - Operations automation

- Reminder system
- document generation
- delivery tracking
- background jobs
- PDF and export

### Phase 3 - Governance

- advanced permissions
- reporting
- dashboard analytics
- tenant provisioning and deployment automation

## 11. Son karar

Voltflow için doğru mimari yaklaşımı:

- ASP.NET Core Web API
- PostgreSQL
- EF Core
- .NET 10
- Modüler monolith
- DDD + aggregate roots
- State-based workflow engine
- Hangfire background jobs
- MinIO file storage
- tenant-isolated deployment

Bu model, hem hızlı geliştirme hem de kurumsal operasyonel güven sağlar.

## 12. Kısa özet

Şema veri modeli doğru temeli sağladı. Eksik olan şey, bu temel üzerinde domain behavior ve execution boundaries kurmaktır.

Gerçek çözüm:

- domain rules
- aggregate invariants
- transition engine
- idempotent transactions
- authorization layer
- worker jobs
- storage and delivery layer
- isolated deployment model

Bu tamamlandığında sistem hem teknik olarak sağlam hem de iş açısından işletilebilir hale gelir.
