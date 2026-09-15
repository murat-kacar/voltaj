# EF Core Persistence Policy

## Canonical source

EF Core model plus checked-in EF migrations are the canonical persistence source for the running Voltflow application.

Deployment database changes must be produced and reviewed through:

```text
dotnet ef migrations add <Name>
dotnet ef database update
```

The former root SQL design artifact has been removed. It was not used by application startup, EF migrations, or runtime database provisioning.

## Current status

### Implemented in EF and migrations

- Identity users, roles, user-role assignments
- Pending account approval state
- Customers and candidate-customer conversion
- Quotes, quote items, lifecycle state, rejection reason
- Accepted quote to work-order conversion
- Work orders and work-order items
- Material stock and stock movement ledger
- Projects, phases, and billing entries
- Customer payments and customer ledger entries
- Sales invoice and payment allocation aggregates
- Progress billing aggregate
- Persisted reminders and retry scheduling
- Reference lookup catalog bridge
- Execution guards and idempotency response state
- Operation traces and audit lineage snapshots
- Transactional outbox
- Optimistic concurrency version

### Historical physical differences from the retired SQL design

The legacy SQL design uses a party-based relational model and many table-per-concept lookup tables. The current EF model intentionally uses a smaller modular-monolith model for the MVP:

- Direct `Guid` customer references are used instead of full `party_records` foreign-key topology.
- Critical lookup values use the centralized `ReferenceValue` bridge rather than one physical table per lookup group.
- Quote-to-work-order linkage uses a unique source-quote field rather than the legacy link table.
- Actor metadata is represented by the current-user and operation context fields rather than every legacy `created_by_id` foreign key.
- The current application treats every approved `AppUser` as an employee; the legacy separate `employees` table and specialty/job metadata are intentionally deferred.
- External delivery/provider tables are not yet implemented as runtime integrations.
- Redis-backed rate-limit counters are deployment infrastructure, not part of the EF persistence model.

These differences are retained only as historical architectural context. New persistence work must be expressed through EF aggregates, mappings, and migrations.
