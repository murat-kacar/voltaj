# Voltflow Domain Model Listesi

Bu belge, Voltflow sisteminin temel domain modellerini ve aggregate root yapılarını açıklar.

## 1. Temel domain kavramları

### 1.1 Party
Party, müşteri, aday müşteri, tedarikçi ve benzeri iş taraflarını temsil eden ortak soyut varlıktır.

Özellikler:
- Id
- CreatedBy
- CreatedAt
- Type

Alt tipler:
- CandidateCustomer
- WalkInCustomer
- Customer
- Supplier
- ProjectParty

### 1.2 User
Kullanıcı, uygulama içindeki çalışan/operasyon/yonetim kullanıcısıdır.

Özellikler:
- Id
- Name
- Email
- EmailVerified
- Image
- CreatedAt
- UpdatedAt

### 1.3 Role ve Permission
Kullanıcılar rol ve yetki kombinasyonu ile kontrol edilir. Bu uygulama tek firma odaklı çalıştığı için hiçbir kullanıcı ve veri kaydı çoklu-tenant scope altında tutulmaz.

Örnek roller:
- Admin
- Office
- Technician
- Finance
- Manager
- Viewer

Örnek izinler:
- customer.read
- customer.write
- quote.issue
- quote.accept
- finance.approve
- stock.adjust
- export.download

## 2. Domain model listesi

### 2.1 Customer Domain

#### Customer
- Id
- PartyId
- LegalName
- Phone
- Address
- TaxOffice
- TaxNumber
- CustomerType
- CreatedBy
- CreatedAt
- UpdatedAt

#### CandidateCustomer
- PartyId
- DisplayName
- Source
- Notes
- CreatedBy
- CreatedAt

#### WalkInCustomer
- PartyId
- DisplayName
- CreatedBy
- CreatedAt

#### CustomerConversion
- Id
- CandidatePartyId
- CustomerPartyId
- ConvertedBy
- ConvertedAt

#### CustomerLedgerEntry
- Id
- PartyId
- EntryType
- Amount
- BalanceAfter
- Description
- CreatedBy
- CreatedAt

#### CustomerPayment
- Id
- PartyId
- PaymentMethod
- Amount
- PaymentDate
- CreatedBy
- CreatedAt

#### CustomerPaymentAllocation
- PaymentId
- SalesInvoiceId
- AllocatedAmount

#### CustomerPaymentProgressAllocation
- PaymentId
- ProgressBillingId
- AllocatedAmount

### 2.2 Quote Domain

#### Quote
- Id
- PartyId
- QuoteNumber
- Title
- State
- IssueDate
- ValidUntil
- Subtotal
- DiscountTotal
- VatTotal
- GrandTotal
- Notes
- IssuedBy
- IssuedAt

#### QuoteItem
- MaterialItem
- LaborItem
- ServiceItem

#### QuoteMaterialItem
- Id
- QuoteId
- MaterialId
- MaterialCodeSnapshot
- MaterialNameSnapshot
- UnitNameSnapshot
- Quantity
- UnitPrice
- VatRate
- VatAmount
- LineTotal

#### QuoteLaborItem
- Id
- QuoteId
- Description
- UnitName
- Quantity
- UnitPrice
- VatRate
- VatAmount
- LineTotal

#### QuoteServiceItem
- Id
- QuoteId
- Description
- UnitName
- Quantity
- UnitPrice
- VatRate
- VatAmount
- LineTotal

#### QuoteLifecycleState
- Draft
- Issued
- Accepted
- Rejected
- Expired

### 2.3 Work Order Domain

#### WorkOrder
- Id
- OrderNumber
- PartyId
- Title
- Status
- Description
- Address
- Priority
- CreatedBy
- CreatedAt
- UpdatedAt

#### ServiceWorkOrder
- WorkOrderId
- ServiceTypeId
- VisitDate

#### ProjectWorkOrder
- WorkOrderId
- ProjectId
- ProjectPhaseId

#### WorkOrderAssignment
- Id
- WorkOrderId
- EmployeeId
- AssignedAt

#### WorkOrderLaborEntry
- Id
- WorkOrderId
- EmployeeId
- WorkDate
- HoursWorked
- CostAmount
- BillableAmount
- CreatedBy
- CreatedAt

#### WorkOrderMaterial
- Id
- WorkOrderId
- MaterialId
- QuantityUsed
- UnitCost
- UnitPrice
- TotalCost
- TotalPrice
- CreatedAt

#### WorkOrderCompletion
- WorkOrderId
- CompletedBy
- CompletedAt
- CustomerSignatureName
- TechnicianNotes

### 2.4 Project Domain

#### Project
- Id
- PartyId
- Name
- SiteAddress
- ContractAmount
- StartDate
- TargetEndDate
- CreatedBy
- CreatedAt
- UpdatedAt

#### ProjectPhase
- Id
- ProjectId
- Name
- OrderIndex
- CreatedAt

#### ProgressBilling
- Id
- ProjectId
- BillingNumber
- PeriodStart
- PeriodEnd
- RequestedAmount
- ApprovedAmount
- DeductionAmount
- NetPayableAmount
- CreatedBy
- CreatedAt

#### ProgressBillingItem
- Id
- ProgressBillingId
- ProjectPhaseId
- Description
- GrossAmount
- DeductionAmount
- NetAmount

#### ProgressBillingApproval
- ProgressBillingId
- ApprovedBy
- ApprovedAt
- Decision
- Note

### 2.5 Inventory Domain

#### Material
- Id
- Code
- Name
- BrandId
- CategoryId
- UnitId
- Barcode
- VatRate
- MinimumQuantity
- CreatedBy
- CreatedAt
- UpdatedAt

#### MaterialPrice
- Id
- MaterialId
- PriceTypeId
- Amount
- ValidFrom
- CreatedBy
- CreatedAt

#### StockLocation
- Id
- Name
- LocationType
- CreatedAt

#### StockBalance
- StockLocationId
- MaterialId
- Quantity
- UpdatedAt

#### StockMovement
- Id
- StockLocationId
- MaterialId
- MovementType
- Quantity
- UnitCost
- PreviousQuantity
- NewQuantity
- Reason
- CreatedBy
- CreatedAt

### 2.6 Maintenance and Service Contracts Domain (Modül 06)

#### MaintenanceContract
- Id
- CustomerId
- SiteId
- ContractNumber
- Title
- StartDate
- EndDate
- RecurrenceIntervalDays
- NextMaintenanceDate
- IsActive
- CreatedBy
- CreatedAt
- UpdatedAt

### 2.7 Purchasing and Supplier Domain

#### Supplier
- Id
- CompanyName
- ContactName
- Phone
- Address
- TaxOffice
- TaxNumber
- CreatedBy
- CreatedAt
- UpdatedAt

#### PurchaseInvoice
- Id
- InvoiceNumber
- SupplierId
- InvoiceDate
- DueDate
- Subtotal
- VatTotal
- GrandTotal
- CreatedBy
- CreatedAt

#### PurchaseInvoiceItem
- Id
- PurchaseInvoiceId
- MaterialId
- Quantity
- UnitCost
- VatRate
- VatAmount
- LineTotal

#### SupplierLedgerEntry
- Id
- SupplierId
- EntryType
- Amount
- BalanceAfter
- Description
- CreatedBy
- CreatedAt

### 2.8 Sales and Finance Domain

#### SalesInvoice
- Id
- InvoiceNumber
- PartyId
- InvoiceDate
- Subtotal
- DiscountTotal
- VatTotal
- GrandTotal
- CreatedBy
- CreatedAt

#### SalesMaterialItem
- Id
- SalesInvoiceId
- MaterialId
- MaterialCodeSnapshot
- MaterialNameSnapshot
- UnitNameSnapshot
- Quantity
- UnitPrice
- VatRate
- VatAmount
- LineTotal

#### SalesLaborItem
- Id
- SalesInvoiceId
- Description
- UnitName
- Quantity
- UnitPrice
- VatRate
- VatAmount
- LineTotal

#### SalesServiceItem
- Id
- SalesInvoiceId
- Description
- UnitName
- Quantity
- UnitPrice
- VatRate
- VatAmount
- LineTotal

#### FinancialAccount
- Id
- Name
- AccountType
- CreatedAt

#### FinancialTransaction
- Id
- FinancialAccountId
- Direction
- Amount
- TransactionDate
- Description
- CreatedBy
- CreatedAt

#### CustomerLedgerEntry
- Id
- PartyId
- EntryType
- Amount
- BalanceAfter
- Description
- CreatedBy
- CreatedAt

#### FinancialExpense
- Id
- FinancialTransactionId
- Category
- Description

## 3. Aggregate root listesi

### 3.1 CustomerAggregate
- Customer
- CandidateCustomer
- CustomerConversion
- CustomerLedgerEntry
- CustomerPayment

### 3.2 QuoteAggregate
- Quote
- QuoteMaterialItem
- QuoteLaborItem
- QuoteServiceItem
- QuoteLifecycleState

### 3.3 WorkOrderAggregate
- WorkOrder
- WorkOrderAssignment
- WorkOrderLaborEntry
- WorkOrderMaterial
- WorkOrderCompletion

### 3.4 ProjectAggregate
- Project
- ProjectPhase
- ProgressBilling
- ProgressBillingItem
- ProgressBillingApproval

### 3.5 StockAggregate
- Material
- StockBalance
- StockMovement
- MaterialPrice

### 3.6 PaymentAggregate
- CustomerPayment
- CustomerPaymentAllocation
- FinancialTransaction
- FinancialAccount

### 3.7 SupplierAggregate
- Supplier
- PurchaseInvoice
- PurchaseInvoiceItem
- SupplierLedgerEntry

## 4. Domain events

Önemli event örnekleri:

- CustomerCreated
- CandidateCustomerConverted
- QuoteIssued
- QuoteAccepted
- QuoteRejected
- QuoteExpired
- WorkOrderCreated
- WorkOrderCompleted
- PaymentAllocated
- StockAdjusted
- ProgressBillingApproved
- ReminderTriggered
- DocumentDelivered
- DataExportCompleted

## 5. Invariant ve access policy örnekleri

### Quote policy
- sadece Draft quote değiştirilebilir
- issue sonrası fiyat değiştirilemez
- accepted/rejected/expired olan terminal statüye geçer

### WorkOrder policy
- teknisyen ataması zorunludur
- tamamlanma için müşteri imzası / not gerekli olabilir
- iş emri faturalanmadan önce tamamlanma durumunu taşımalıdır

### Stock policy
- quantity < 0 olamaz
- movement türü geçerli olmalı
- stock balance update ve ledger update tek transaction içinde yapılmalı

### Finance policy
- allocations total invoice totalini aşamaz
- ödeme ve fatura eşleşmesi kayıt altında olmalıdır
- approval öncesi mutasyonlar engellenmelidir

## 6. Uygulama planı

İlk sürüm için en kritik aggregate'ler:

1. CustomerAggregate
2. QuoteAggregate
3. WorkOrderAggregate
4. StockAggregate
5. PaymentAggregate
6. ProjectAggregate

Bu altı aggregate, işin ana değer zincirini oluşturur.

## 7. Son söz

Bu model, veritabanı tasarımının ötesinde domain davranışını kapsar. Asıl amaç, sadece tablo üretmek değil; iş kurallarını doğru yerde ve temiz şekilde kodlamak ve transaction-safe bir sistem kurmaktır.
