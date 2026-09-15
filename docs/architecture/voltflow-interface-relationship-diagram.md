# Voltflow Interface Relationship Diagram

## 1. Ana akış diagramı

```text
+------------------+
|     API Layer    |
|   Controllers    |
+--------+---------+
         |
         v
+------------------+
| Application Layer|
|  Services        |
|  Commands/Querys |
+--------+---------+
         |
         +---------------------+---------------------+
         |                     |                     |
         v                     v                     v
+------------------+   +------------------+   +------------------+
| Customer Service |   | Quote Service    |   | WorkOrder Service|
+--------+---------+   +--------+---------+   +--------+---------+
         |                     |                     |
         +---------------------+---------------------+
                               |
                               v
                    +--------------------------+
                    |  Domain / Aggregates     |
                    |  Customer, Quote,        |
                    |  WorkOrder, Stock,...    |
                    +--------------------------+
                               |
         +---------------------+---------------------+
         |                     |                     |
         v                     v                     v
+------------------+   +------------------+   +------------------+
| Repository Impl  |   | Stock Service    |   | Payment Service  |
| EF Core          |   | Ledger logic     |   | Allocation logic |
+--------+---------+   +------------------+   +------------------+
         |
         v
+------------------+
| PostgreSQL       |
+------------------+
```

## 2. Dış sistem bağımlılıkları

```text
+-------------------+
|  Notification     |
|  Email / SMS /    |
|  WhatsApp         |
+---------+---------+
          |
          v
+-------------------+
| INotificationSender |
+-------------------+

+-------------------+
| Blob Storage      |
| MinIO / S3        |
+---------+---------+
          |
          v
+-------------------+
| IBlobStorageService |
+-------------------+

+-------------------+
| Job Scheduler     |
| Hangfire          |
+---------+---------+
          |
          v
+-------------------+
| IJobScheduler     |
+-------------------+
```

## 3. Kritik interface bağımlılık örneği

```text
IQuoteService
  depends on:
    - IQuoteRepository
    - ICustomerRepository
    - IAuditLogger
    - IBlobStorageService
    - INotificationSender
    - IJobScheduler
    - IValidator<CreateQuoteCommand>
```

## 4. Uygulama akışı örneği

```text
CreateQuoteCommand
        |
        v
IQuoteService.CreateDraftAsync
        |
        v
IQuoteRepository.AddAsync
        |
        v
IAuditLogger.LogAsync
        |
        v
IDomainEventPublisher.PublishAsync
        |
        v
IJobScheduler.EnqueueAsync("generate-quote-pdf")
```

## 5. Özet

Ana tasarım şu şekildedir:

- API → Application service
- Application service → Repository + validator + policy + logger
- Infrastructure → interface implementation
- Background jobs and external services stay behind interfaces

Böylece sistem hem clean architecture benzeri hem de test edilebilir olur.
