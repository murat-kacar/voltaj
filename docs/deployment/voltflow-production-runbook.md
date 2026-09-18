# Voltflow Production Runbook

## Services

The compose stack contains:

- `postgres`: persistent application database
- `redis`: distributed-infrastructure boundary and future rate-limit counter store
- `api`: ASP.NET Core API on port `8080`
- `worker`: reminder and outbox polling worker

External email, SMS, document, and notification providers are intentionally not included in this stack.

## Required secret

Set the JWT signing key outside source control before starting the stack:

```powershell
$env:VOLT_JWT_KEY = "a-secret-at-least-32-characters-long"
docker compose up -d
```

The compose file fails fast when `VOLT_JWT_KEY` is absent.

## First administrator (optional secrets)

There is no default administrator outside Development (ADR 0009). A fresh environment gets its first
administrator from two optional deployment secrets, `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD` - GitHub secrets
for `deploy.yml`, or environment variables when `docker-compose.deploy.yml` is run by hand. Both must be set; the
account is created on the first start and never modified afterwards. Without them the API still starts, logs
"No administrator was seeded", and nobody can sign in until an administrator exists. Avoid `$` in the password:
docker compose interpolates it when it reads `.env`.

## Health checks

```text
GET http://localhost:8080/health
GET http://localhost:8080/ready
GET http://localhost:8080/startup
```

Three separate probes, three separate questions:

- `/health` (liveness) confirms the process is up. It checks no dependency, so a database outage never gets the process restarted.
- `/ready` (readiness) confirms that the API can connect to PostgreSQL right now.
- `/startup` confirms that one-time initialization has finished and that the database schema has no pending migrations; it answers `503` with `starting` or `migrations_pending` until then.

## Tracing

The API and the Worker are instrumented with OpenTelemetry. Spans are always recorded in-process; they are exported only when a collector endpoint is configured through the standard environment variable:

```powershell
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
```

An incoming W3C `traceparent` header is honoured, and the trace continues across the transactional outbox into the Worker. The probe endpoints and `/metrics` are excluded from traces.

## Database

EF Core migrations are canonical:

```powershell
dotnet ef database update --project src/Voltflow.Infrastructure/Voltflow.Infrastructure.csproj --startup-project src/Voltflow.Api/Voltflow.Api.csproj
```

Do not apply retired standalone SQL designs to the running application database. Use EF Core migrations exclusively.

## Operational flows

- Pending users are approved by an Admin, then receive the initial Viewer role.
- Admins can assign additional system roles.
- Technicians see only assigned WorkOrders.
- Outbox messages retry up to five times with exponential backoff, then move to DeadLetter.
- Admin replay endpoint: `POST /api/operations/outbox/{id}/replay`.
- Every request returns `X-Operation-Id` and supports screen/action/parent operation headers.

## Rate limiting

Mutation quotas use Redis atomic counters partitioned by endpoint and authenticated user/IP. Redis must be healthy before API mutation traffic is accepted. If Redis is unavailable, mutation requests return a controlled `503` rather than bypassing protection.
