# Voltflow Architecture Summary

## 1. Scope and intent

This project is structured as a single-company modular monolith. It follows the same pattern described in the schema and architecture decisions: one application, one relational database, modular domain boundaries, and no platform-level multi-tenant layer in the current version.

The current state is intentionally conservative and aligned with the smallest viable implementation for a small or mid-sized business workflow system.

## 2.1 Canonical DDL alignment status

The current EF model and checked-in migrations are the sole persistence source for the approved MVP behavior.

| DDL concern | Current EF state | Status |
|---|---|---|
| `party_records` and party-based foreign keys | Aggregate models still use direct `Guid` customer references | Pending |
| `created_by_id` / actor references | `ICurrentUser` and EF creator stamping are wired for new entities | Implemented in current EF model |
| Optimistic concurrency | Every domain entity carries an application-managed `Version` concurrency token, incremented on update | Implemented |
| Lookup/reference tables | Critical payment, work-order, stock-movement, and reminder catalogs are centralized and idempotently seeded; physical DDL table-per-catalog mapping remains pending | Partial, catalog bridge implemented |
| Candidate/customer conversion behavior | Implemented with the current simplified aggregate model | Implemented, physical mapping differs |
| Quote/work-order relationship | Implemented with `SourceQuoteId` and unique index | Implemented, physical link table differs |
| Stock movement ledger | Implemented transactionally in the current model | Implemented, location/material lookup mapping differs |
| Invoice/payment allocation | Implemented transactionally in the current model | Implemented, party and lookup mapping differs |
| Reminder lifecycle | Persisted pending/completed/dismissed state and retry timing exist | Partial, delivery provider mapping pending |

This distinction is intentional: behavioral MVP completion is not presented as full DDL synchronization. Canonical alignment must be delivered as separate migrations so existing data and workflows are not silently reinterpreted.

Persistence policy: EF Core model plus checked-in migrations are canonical for runtime and deployment. The retired SQL design is preserved only through historical notes in `voltflow-ef-ddl-parity.md`.

## 2.2 API execution safety

- Business endpoint groups require an authenticated principal.
- Mutation endpoints use a five-calls-per-minute fixed-window limiter.
- Mutation endpoints persist an execution guard keyed by route and request fingerprint.
- Repeated same-argument calls are rejected before the handler executes.
- An `Idempotency-Key` can explicitly identify a one-time operation; a reused key is rejected with `409 Conflict`.
- Login is rate-limited but is not duplicate-guarded, so repeated failed attempts are handled by the rate limiter rather than permanently poisoning a credential attempt.
- Guard lifecycle uses `PENDING`, `RESOLVED`, and `ORPHANED`: successful operations remain permanently deduplicated; failed or expired leases can be retried.
- These controls provide an exactly-once business effect boundary, not a claim that a distributed handler can literally execute only once. The business mutation and guard resolution must remain transactionally aligned for strict exactly-once effects.
- Role policies are defined for authenticated users, write access (`Admin`/`Manager`), and operations access (`Admin`/`Manager`/`Technician`). Endpoint-specific technician assignment scoping remains a follow-up because it requires employee-to-user mapping in the canonical party model.
- Every approved `AppUser` is an employee by definition. Work orders carry `AssignedUserId`; technician-only reads are filtered to that user, while Admin/Manager can operate across assignments.

## 2.3 Operation tracing and audit

Every API request receives an `OperationId` and returns it in `X-Operation-Id`. A client can provide `X-Client-Screen`, `X-Client-Action`, and `X-Parent-Operation-Id` to preserve the user-visible action and parent-child operation chain.

The combined pattern is **idempotency keys/inbox-style deduplication + rate limiting/throttling + distributed tracing + audit trail**. Logs prove what happened; execution guards and the rate limiter enforce what may happen.

The implementation now also includes:

- idempotent response replay for completed requests
- `PENDING`/`RESOLVED`/`ORPHANED` guard lifecycle
- transactional outbox messages emitted with audit events
- .NET `ActivitySource` tags for W3C-compatible distributed tracing

Mutation rate limiting now uses a Redis-backed atomic fixed-window counter partitioned by endpoint and user/IP. Redis is required for API mutation paths; unavailable Redis returns a controlled `503` instead of silently weakening protection.

The production compose stack includes PostgreSQL, Redis, API, and Worker services with health-gated dependencies.

## 2.4 Exception and error contract

API errors use RFC 9457 Problem Details rather than ad-hoc strings. The standard mapping is:

| Situation | HTTP | Code |
|---|---:|---|
| Invalid argument/request | 400 | `invalid_argument` |
| Authentication required/failed | 401 | `unauthorized` |
| Resource missing | 404 | `not_found` |
| Domain/business rule violation | 409 | `business_rule_violation` |
| Business validation returned by an application service | 422 | `business_validation` |
| Optimistic concurrency conflict | 409 | `concurrency_conflict` |
| Rate limit exceeded | 429 | framework rate-limit response |
| Unexpected failure | 500 | `internal_error` |

Every Problem Details response carries a trace identifier and operation identifier. Internal logs contain the exception and structured context; clients receive no stack trace, secret, token, password, or provider-sensitive detail. Authentication failures intentionally return generic text to prevent credential enumeration.

Updates use optimistic concurrency. EF includes the original `Version` in the update predicate; when another writer has already advanced it, the update affects zero rows and returns `concurrency_conflict` (`409`) instead of silently overwriting data.

## 2.5 Account approval and retention decisions

- New registrations remain pending and receive no access token.
- An Admin-only approval operation activates the account and assigns the initial `Viewer` role.
- Admin can assign additional system roles through the Admin-only role assignment endpoint; all approved users remain employees by definition.
- Approved users have revocable session and password-reset token persistence; raw token values are never stored.
- Quote rejection reasons are persisted on the Quote aggregate and included in the business audit trail.
- Audit snapshots redact email, phone, tax, address, password, token, and secret fields.
- Test fixtures use unique data per test; reference seed data is shared, while assertions never depend on global row counts.
- Outbox delivery uses five attempts with exponential backoff and then moves to `DeadLetter` for operational review/replay.
- EF Core migrations and model are the canonical persistence source. The legacy SQL schema remains an audit/reference artifact until party and lookup parity is explicitly verified.

The persisted operation trace records:

- user, endpoint, screen, action, parent operation
- query string and a safe request fingerprint
- completed/failed/cancelled/blocked outcome
- HTTP status, duration, error type and message

Each entity audit event additionally records:

- parent operation and source endpoint
- source screen and client action
- serialized before snapshot
- serialized after snapshot
- serialized changed-field list
- actor and entity identity

These records form the data-lineage trail without persisting raw request bodies or secrets. Audit events are immutable application history; the transactional outbox carries their safe serialized event payload to asynchronous publishers.

EF persistence automatically records redacted `CREATED`, `UPDATED`, and `DELETED` audit events with entity name, entity id, changed fields, actor, endpoint, and operation id. Passwords, tokens, secrets, raw request bodies, and sensitive headers are never persisted by this layer.

Transaction failures remain distinguishable from successful operations. Where a database transaction rolls back, its entity audit events roll back with it; the outer request trace attempts to persist the terminal failure separately without masking the original exception.

## 2. Current real implementation

The current codebase already contains the real foundation for the chosen architecture:

- Domain layer with entity and invariant logic
- Application layer with DTOs and use-case contracts
- Infrastructure layer with DbContext and repository implementations
- API layer with minimal endpoint wiring
- Singleton/DI-style registration across application startup

This is the foundation that the next modules should extend without breaking the current design.

## 3. Module map

### 3.1 Customer and quote flow

This is the currently implemented path.

- Domain: Customer, Quote, QuoteItem, QuoteState
- Application: ICustomerService, IQuoteService
- Infrastructure: CustomerRepository, QuoteRepository
- API: /api/customers, /api/quotes

### 3.2 Work order module

The initial work-order module and accepted-quote conversion flow are implemented.

Required components:

- Domain: WorkOrder, WorkOrderStatus, WorkOrderItem
- Application: IWorkOrderService, WorkOrderService
- Infrastructure: IWorkOrderRepository, WorkOrderRepository
- API: /api/workorders

### 3.3 Inventory and stock module

The initial inventory module is implemented. Manual adjustments write the balance update and append-only stock movement in one transaction.

Required components:

- Domain: Material, MaterialStock, StockMovement
- Application: IInventoryService, IStockService
- Infrastructure: IInventoryRepository, StockMovementRepository
- API: /api/inventory, /api/stock

### 3.4 Project and billing flow

Project and basic billing entries are implemented. Customer payment creation, customer ledger posting, invoice allocation, and progress billing aggregates are implemented transactionally. Full canonical party/account lookup mapping remains pending.

Required components:

- Domain: Project, ProjectPhase, BillingEntry, PaymentAllocation
- Application: IProjectService, IBillingService, IPaymentService
- Infrastructure: IProjectRepository, IBillingRepository, IPaymentRepository
- API: /api/projects, /api/billing, /api/payments

### 3.5 Auth and role model

This is implemented as a company-local identity layer with password hashing, JWT role claims, role seed, and opt-in admin seed.

Required components:

- Domain: User, Role, Session
- Application: IAuthService, IUserRoleService
- Infrastructure: IUserRepository, IRoleRepository, ISessionRepository
- API: /api/auth, /api/users, /api/roles

### 3.6 Reminder worker

The Worker project contains a cancellation-aware hosted service backed by persisted reminder records. Pending reminders are polled, attempts are recorded, and completion/dismissal lifecycle is represented. External delivery provider integration remains pending.

## 4. Real interface list (current + expected extension)

### 4.1 Existing concrete interfaces

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
}

public interface IQuoteRepository : IRepository<Quote>
{
    Task<Quote?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<Quote>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
}
```

```csharp
public interface ICustomerService
{
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CustomerDto>>> ListAsync(CancellationToken ct = default);
}

public interface IQuoteService
{
    Task<Result<QuoteDto>> CreateAsync(CreateQuoteRequest request, CancellationToken ct = default);
    Task<Result<QuoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<QuoteDto>>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<QuoteDto>> IssueAsync(Guid id, CancellationToken ct = default);
    Task<Result<QuoteDto>> AcceptAsync(Guid id, CancellationToken ct = default);
    Task<Result<QuoteDto>> RejectAsync(Guid id, CancellationToken ct = default);
}
```

### 4.2 Extension interfaces for next phase

```csharp
public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    Task<WorkOrder?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<WorkOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
}

public interface IWorkOrderService
{
    Task<Result<WorkOrderDto>> CreateAsync(CreateWorkOrderRequest request, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WorkOrderDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<WorkOrderDto>> AssignAsync(Guid id, Guid technicianId, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> CompleteAsync(Guid id, CancellationToken ct = default);
}
```

```csharp
public interface IInventoryRepository : IRepository<MaterialStock>
{
    Task<MaterialStock?> GetByMaterialCodeAsync(string materialCode, CancellationToken ct = default);
    Task<IReadOnlyList<MaterialStock>> GetLowStockAsync(CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<Result<StockDto>> GetStockAsync(string materialCode, CancellationToken ct = default);
    Task<Result<StockDto>> AdjustStockAsync(AdjustStockRequest request, CancellationToken ct = default);
    Task<Result<StockDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct = default);
}
```

```csharp
public interface IProjectRepository : IRepository<Project>
{
    Task<IReadOnlyList<Project>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Project?> GetByNumberAsync(string number, CancellationToken ct = default);
}

public interface IProjectService
{
    Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<Result<ProjectDto>> AddPhaseAsync(Guid projectId, CreateProjectPhaseRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProjectDto>>> ListAsync(CancellationToken ct = default);
}
```

## 5. DbContext and DI example

Current pattern is already in place and should be extended module-by-module.

```csharp
builder.Services.AddDbContext<VoltflowDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
```

For the next modules, the same pattern is used:

```csharp
builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();

builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();
```

## 6. EF Core mapping pattern

The existing DbContext pattern is the model to use for the next modules.

```csharp
modelBuilder.Entity<Quote>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.Property(x => x.Number).IsRequired();
    entity.Property(x => x.Title).IsRequired();
    entity.Property(x => x.Total).HasColumnType("decimal(18,2)");

    entity.OwnsMany(x => x.Items, items =>
    {
        items.WithOwner().HasForeignKey("QuoteId");
        items.Property(x => x.Description).IsRequired();
        items.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
        items.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
    });
});
```

For WorkOrder and Project, the same approach applies: configure keys, owned collections, numeric precision, and explicit names that reflect the database schema.

## 7. Initial API endpoint skeletons

Current endpoint design uses minimal API groups and a thin layer over application services.

```csharp
var group = routes.MapGroup("/api/workorders");

group.MapGet("", async (IWorkOrderService service) =>
{
    var result = await service.ListAsync();
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

group.MapGet("{id:guid}", async (Guid id, IWorkOrderService service) =>
{
    var result = await service.GetByIdAsync(id);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
});

group.MapPost("", async (CreateWorkOrderRequest request, IWorkOrderService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});
```

```csharp
var group = routes.MapGroup("/api/projects");

group.MapGet("", async (IProjectService service) =>
{
    var result = await service.ListAsync();
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

group.MapPost("", async (CreateProjectRequest request, IProjectService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});
```

## 8. Domain and service template pattern

The project uses a simple and consistent pattern:

- Domain aggregate owns invariants and state transitions
- Service owns use-case orchestration and input validation
- Repository owns persistence access
- API maps DTOs to HTTP and delegates to service layer

This makes the extension to later modules straightforward and predictable.

## 9. Graphify interpretation

The code graph confirms the same structure: domain, application, infrastructure, and API remain distinct and connected through strongly typed interfaces and repository/service registrations. This matches the modular-monolith design and the single-company model encoded in the schema and docs.

The current graph does not reveal a platform-tenant layer or a cross-company boundary in code. That matches the actual project intent and the current architecture documents.

## 10. Final recommendation

Continue from the current foundation in this order:

1. WorkOrder module
2. Inventory and stock module
3. Project and billing module
4. Auth and role layer
5. EF migrations and seed structure
6. Graphify architecture summary refresh after each layer

This order is aligned with the current code and schema, avoids premature abstraction, and keeps the project within the intended small-to-mid business scope.
