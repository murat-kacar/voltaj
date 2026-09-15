# Voltflow Interface Map

Bu belge, .NET tabanlı Voltflow projesinde kullanılacak ana interface'leri, sorumluluklarını ve birbirleriyle olan ilişkilerini açıklar.

## 1. Temel ilke

- Domain katmanı business rule ve entity davranışını içerir.
- Application katmanı use case contract'larını tanımlar.
- Infrastructure katmanı bu contract'ları uygular.
- Web/API katmanı sadece use-case çağrısı yapar.
- Interface'ler, repository, validator, notification, file storage ve job runner için ortak contract sağlar.

## 2. Genel bağımlılık yönü

```text
API
  -> Application
      -> Domain
      -> Interfaces / Contracts
Infrastructure
  -> Application Interfaces
  -> Domain
```

Yani infrastructure, application interface'lerini uygular; API application layer'ı çağırır.

## 3. Ana interface grupları

### 3.1 Real interface set aligned with current codebase

Mevcut kod, aşağıdaki interface setini gerçekten kullanır:

#### IRepository<T>
- GetByIdAsync
- ListAsync
- AddAsync
- UpdateAsync
- DeleteAsync

#### ICustomerRepository
- GetByIdAsync
- GetByEmailAsync
- ListAsync
- AddAsync
- UpdateAsync
- DeleteAsync

#### IQuoteRepository
- GetByIdAsync
- GetByNumberAsync
- GetByCustomerAsync
- ListAsync
- AddAsync
- UpdateAsync
- DeleteAsync

#### ICustomerService
- CreateAsync
- GetByIdAsync
- ListAsync

#### IQuoteService
- CreateAsync
- GetByIdAsync
- ListByCustomerAsync
- IssueAsync
- AcceptAsync
- RejectAsync

### 3.2 Extension interface set for the next modules

Mevcut schema ve mimari hedefi doğrultusunda sonraki module'lar için aşağıdaki interface'ler mantıklı ve uyumlu bir genişleme olarak eklenmelidir:

#### IWorkOrderRepository
- GetByIdAsync
- GetByNumberAsync
- ListAsync
- AddAsync
- UpdateAsync
- DeleteAsync

#### IWorkOrderService
- CreateAsync
- GetByIdAsync
- ListAsync
- AssignAsync
- CompleteAsync

#### IInventoryRepository
- GetByIdAsync
- GetByMaterialCodeAsync
- ListAsync
- AddAsync
- UpdateAsync

#### IInventoryService
- GetStockAsync
- AdjustStockAsync
- ReserveAsync
- ReleaseAsync

#### IProjectRepository
- GetByIdAsync
- GetByCustomerAsync
- ListAsync
- AddAsync
- UpdateAsync

#### IProjectService
- CreateAsync
- AddPhaseAsync
- GetByIdAsync
- ListAsync

#### IBillingRepository
- GetByIdAsync
- ListAsync
- AddAsync
- UpdateAsync

#### IBillingService
- CreateInvoiceAsync
- CreateProgressBillingAsync
- AllocatePaymentAsync

#### IAuthService
- LoginAsync
- RegisterAsync
- RefreshTokenAsync
- LogoutAsync

#### IRoleService
- GetRolesAsync
- AssignRoleAsync
- RemoveRoleAsync

### 3.3 Repository interfaces

Bu interface'ler veritabanı erişimini soyutlar.

#### ICustomerRepository
- GetByIdAsync
- GetCandidateByIdAsync
- GetByEmailAsync
- CreateAsync
- UpdateAsync
- ExistsByTaxNumberAsync
- ListAsync

#### IQuoteRepository
- GetByIdAsync
- GetByNumberAsync
- CreateAsync
- UpdateAsync
- AddItemAsync
- ListByPartyAsync
- GetOpenQuotesAsync

#### IWorkOrderRepository
- GetByIdAsync
- GetByNumberAsync
- CreateAsync
- UpdateAsync
- AddAssignmentAsync
- AddLaborEntryAsync
- AddMaterialUsageAsync
- ListByPartyAsync

#### IProjectRepository
- GetByIdAsync
- GetByProjectPhaseAsync
- CreateAsync
- UpdateAsync
- ListByPartyAsync

#### IStockRepository
- GetBalanceAsync
- GetMovementHistoryAsync
- UpdateBalanceAsync
- AddMovementAsync
- GetMaterialAsync

#### IPaymentRepository
- GetByIdAsync
- CreateAsync
- AddAllocationAsync
- GetAllocationsByPaymentAsync
- GetLedgerAsync

#### ISupplierRepository
- GetByIdAsync
- CreateAsync
- UpdateAsync
- ListAsync

#### IReminderRepository
- GetDueAsync
- CreateAsync
- MarkCompletedAsync
- MarkDismissedAsync

#### IDocumentRepository
- GetByIdAsync
- CreateAsync
- AttachToEntityAsync
- ListByEntityAsync

## 4. Application service interfaces

Bu interface'ler use-case seviyesindeki iş operasyonlarını temsil eder.

#### ICustomerService
- RegisterCandidateAsync
- ConvertCandidateToCustomerAsync
- UpdateCustomerAsync
- GetCustomerAsync
- ListCustomersAsync

#### IQuoteService
- CreateDraftAsync
- IssueAsync
- AcceptAsync
- RejectAsync
- ExpireAsync
- AddItemAsync
- GetQuoteAsync

#### IWorkOrderService
- CreateAsync
- AssignTechnicianAsync
- AddLaborEntryAsync
- AddMaterialEntryAsync
- CompleteAsync
- LinkInvoiceAsync

#### IProjectService
- CreateAsync
- AddPhaseAsync
- CreateBillingAsync
- ApproveBillingAsync

#### IStockService
- AdjustStockAsync
- ReserveStockAsync
- ReleaseReservedStockAsync
- GetBalanceAsync
- GetMovementHistoryAsync

#### IPaymentService
- CreatePaymentAsync
- AllocateToInvoiceAsync
- AllocateToProgressBillingAsync
- GetLedgerAsync

#### IReminderService
- CreateReminderAsync
- ProcessDueRemindersAsync
- CompleteReminderAsync
- DismissReminderAsync

#### IDocumentService
- CreateDocumentAsync
- SendDocumentAsync
- StoreAttachmentAsync
- GetDocumentAsync

## 5. Cross-cutting interfaces

### 5.1 Validation

#### IValidator<T>
- Validate(T request)

Kullanım: command validation, DTO validation.

### 5.2 Event / messaging

#### IDomainEventPublisher
- PublishAsync(IDomainEvent domainEvent)

#### INotificationSender
- SendAsync(NotificationMessage message)

### 5.3 File / storage

#### IBlobStorageService
- UploadAsync
- DownloadAsync
- DeleteAsync
- GetUrlAsync

#### IFileStorageService
- SaveAttachmentAsync
- GetAttachmentAsync
- RemoveAttachmentAsync

### 5.4 Background jobs

#### IJobScheduler
- EnqueueAsync(string jobName, object payload)
- ScheduleAsync(string jobName, DateTimeOffset runAt, object payload)

#### IReminderJobRunner
- RunDueRemindersAsync
- RunExpiredQuotesAsync
- RunLowStockAlertsAsync

### 5.5 Audit

#### IAuditLogger
- LogAsync(AuditEntry entry)

#### IExecutionGuardService
- AcquireAsync(string scope, string idempotencyKey, string requestHash)
- ResolveAsync(string scope, string idempotencyKey)

## 6. Interface ilişkisi haritası

```text
ICustomerService
  -> ICustomerRepository
  -> IAuditLogger
  -> IDomainEventPublisher
  -> IValidator<CreateCustomerCommand>

IQuoteService
  -> IQuoteRepository
  -> ICustomerRepository
  -> IDocumentService
  -> IAuditLogger
  -> IDomainEventPublisher
  -> IValidator<CreateQuoteCommand>

IWorkOrderService
  -> IWorkOrderRepository
  -> IStockService
  -> ICustomerRepository
  -> IAuditLogger
  -> INotificationSender

IProjectService
  -> IProjectRepository
  -> IWorkOrderRepository
  -> IPaymentRepository
  -> IAuditLogger

IStockService
  -> IStockRepository
  -> IAuditLogger
  -> IDomainEventPublisher
  -> IJobScheduler

IPaymentService
  -> IPaymentRepository
  -> ICustomerRepository
  -> IAuditLogger
  -> IValidator<PaymentAllocationCommand>

IReminderService
  -> IReminderRepository
  -> INotificationSender
  -> IJobScheduler

IDocumentService
  -> IDocumentRepository
  -> IBlobStorageService
  -> IAuditLogger
  -> INotificationSender
```

## 7. Temel contract örnekleri

### Repository contract örneği

```csharp
public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Quote?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task AddAsync(Quote quote, CancellationToken ct = default);
    Task UpdateAsync(Quote quote, CancellationToken ct = default);
    Task AddItemAsync(QuoteItem item, CancellationToken ct = default);
    Task<IReadOnlyList<Quote>> ListByPartyAsync(Guid partyId, CancellationToken ct = default);
}
```

### Service contract örneği

```csharp
public interface IQuoteService
{
    Task<Result<QuoteDto>> CreateDraftAsync(CreateQuoteCommand command, CancellationToken ct = default);
    Task<Result<QuoteDto>> IssueAsync(IssueQuoteCommand command, CancellationToken ct = default);
    Task<Result<QuoteDto>> AcceptAsync(AcceptQuoteCommand command, CancellationToken ct = default);
    Task<Result<QuoteDto>> RejectAsync(RejectQuoteCommand command, CancellationToken ct = default);
}
```

### Storage contract örneği

```csharp
public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
```

## 8. Uygulama önceliği

İlk sürümde aşağıdaki interface'ler hayati önceliğe sahiptir:

1. ICustomerRepository
2. IQuoteRepository
3. IWorkOrderRepository
4. IStockRepository
5. IPaymentRepository
6. IReminderRepository
7. IBlobStorageService
8. IAuditLogger
9. IJobScheduler
10. IExecutionGuardService

## 9. Kısa özet

Voltflow için interface yapısı şudur:

- Repository interfaces: veri erişimi
- Service interfaces: iş akışı
- Storage / Notification / Job interfaces: dış sistem entegrasyonu
- Audit / guard interfaces: güvenilirlik ve kontrol

Bu tasarım, uygulamayı clean architecture benzeri bir yapıya taşır ve her katmanın sorumluluğunu netleştirir.
