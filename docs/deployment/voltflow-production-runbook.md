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

## Health checks

```text
GET http://localhost:8080/health
GET http://localhost:8080/ready
```

`/health` confirms process liveness. `/ready` confirms that the API can connect to PostgreSQL.

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
