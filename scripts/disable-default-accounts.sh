#!/usr/bin/env bash
# Disables the seeded default administrator and the approved E2E test accounts of a Voltflow database and
# revokes their sessions (ADR 0009). Run it ON the server as root; it is idempotent (O6: setup is a script):
#   scp scripts/disable-default-accounts.sh root@<host>:/root/ && ssh root@<host> "bash /root/disable-default-accounts.sh"
# Nothing is deleted. A backup of the identity tables is written first (mode 600). To undo, set "IsApproved" back
# to true for the accounts printed below; the backup file holds the previous rows.
# Use it on every environment that was seeded before ADR 0009 (production, staging).
set -euo pipefail

container="${POSTGRES_CONTAINER:-voltflow-postgres}"
database="${POSTGRES_DB:-voltflow}"
psql_i() { docker exec -i "$container" psql -U postgres -d "$database" -X -v ON_ERROR_STOP=1 "$@"; }

umask 077
mkdir -p /root/backups
backup="/root/backups/identity-$(date +%Y%m%d-%H%M%S).sql"
docker exec "$container" pg_dump -U postgres -d "$database" -t '"AppUsers"' -t '"AppUserRoles"' -t '"UserSessions"' > "$backup"
[ -s "$backup" ] || { echo "disable-default-accounts: empty backup, aborting" >&2; exit 1; }
echo "backup written: $backup"

psql_i <<'SQL'
BEGIN;
CREATE TEMP TABLE targets ON COMMIT DROP AS
  SELECT "Id", "Email" FROM "AppUsers"
   WHERE "IsApproved" AND ("Email" = 'admin@voltflow.com'
      OR "Email" ~ '^(auth_test_user|dry_pilot_[0-9]+|e2e_pilot_[0-9]+|e2e_[0-9]+_[0-9]+)@voltflow\.com$');
DO $$
BEGIN
  IF (SELECT count(*) FROM targets) > 25 THEN RAISE EXCEPTION 'unexpectedly many accounts match - aborting'; END IF;
END $$;
SELECT 'disabling: ' || "Email" AS account FROM targets ORDER BY "Email";
UPDATE "AppUsers"
   SET "IsApproved" = false, "Version" = "Version" + 1, "UpdatedAt" = now(),
       "UpdatedByEndpoint" = 'ops: default and E2E accounts disabled'
 WHERE "Id" IN (SELECT "Id" FROM targets);
UPDATE "UserSessions"
   SET "RevokedAt" = now(), "Version" = "Version" + 1, "UpdatedAt" = now()
 WHERE "RevokedAt" IS NULL AND "UserId" IN (SELECT "Id" FROM targets);
COMMIT;
SQL

echo
echo "== accounts now (email | approved | roles) =="
docker exec "$container" psql -U postgres -d "$database" -X -A -t -F ' | ' -c \
  'select u."Email", u."IsApproved", coalesce(string_agg(r."Name", chr(44)), chr(45)) from "AppUsers" u left join "AppUserRoles" ur on ur."UserId" = u."Id" left join "AppRoles" r on r."Id" = ur."RoleId" group by u."Id" order by u."CreatedAt"'
echo "still-valid sessions of disabled accounts: $(docker exec "$container" psql -U postgres -d "$database" -X -A -t -c 'select count(*) from "UserSessions" s join "AppUsers" u on u."Id" = s."UserId" where not u."IsApproved" and s."RevokedAt" is null and s."ExpiresAt" > now()')"
