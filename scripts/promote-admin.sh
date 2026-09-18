#!/usr/bin/env bash
# Makes an already REGISTERED account an approved, verified administrator (ADR 0009: there is no default
# administrator outside Development, and the application has no other way to create the first one).
# Run it ON the server as root (register the account in the application first, with your own password):
#   scp scripts/promote-admin.sh root@<host>:/root/ && ssh root@<host> "bash /root/promote-admin.sh you@example.com"
# Nothing is deleted; a backup of the identity tables is written first (mode 600). Idempotent.
set -euo pipefail

email="${1:-}"
[[ "$email" =~ ^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$ ]] || { echo "usage: promote-admin.sh <registered e-mail>" >&2; exit 2; }
email="${email,,}"
# the characters allowed above cannot break out of an SQL string literal; the accounts below are never promotable
if [[ "$email" == "admin@voltflow.com" || "$email" =~ ^(auth_test_user|dry_pilot_|e2e_|pending_|unapproved) ]]; then
  echo "promote-admin: refusing to promote the default or a test account" >&2; exit 2
fi

container="${POSTGRES_CONTAINER:-voltflow-postgres}"
database="${POSTGRES_DB:-voltflow}"
psql_i() { docker exec -i "$container" psql -U postgres -d "$database" -X -v ON_ERROR_STOP=1 "$@"; }

umask 077
mkdir -p /root/backups
backup="/root/backups/identity-$(date +%Y%m%d-%H%M%S).sql"
docker exec "$container" pg_dump -U postgres -d "$database" -t '"AppUsers"' -t '"AppUserRoles"' -t '"UserSessions"' > "$backup"
[ -s "$backup" ] || { echo "promote-admin: empty backup, aborting" >&2; exit 1; }
echo "backup written: $backup"

sql=$(cat <<'SQL'
BEGIN;
DO $$
BEGIN
  IF (SELECT count(*) FROM "AppUsers" WHERE "Email" = '__EMAIL__') <> 1 THEN
    RAISE EXCEPTION 'no account with the e-mail __EMAIL__ - register it in the application first';
  END IF;
END $$;
UPDATE "AppUsers"
   SET "IsApproved" = true, "EmailVerified" = true, "Version" = "Version" + 1, "UpdatedAt" = now(),
       "UpdatedByEndpoint" = 'ops: promoted to administrator'
 WHERE "Email" = '__EMAIL__';
INSERT INTO "AppUserRoles" ("Id", "UserId", "RoleId", "Version", "CreatedAt", "CreatedByEndpoint")
SELECT gen_random_uuid(), u."Id", r."Id", 1, now(), 'ops: promoted to administrator'
  FROM "AppUsers" u CROSS JOIN "AppRoles" r
 WHERE u."Email" = '__EMAIL__' AND r."Name" = 'Admin'
   AND NOT EXISTS (SELECT 1 FROM "AppUserRoles" x WHERE x."UserId" = u."Id" AND x."RoleId" = r."Id");
COMMIT;
SQL
)
printf '%s\n' "${sql//__EMAIL__/$email}" | psql_i

echo
echo "== $email now (email | approved | verified | roles) =="
docker exec "$container" psql -U postgres -d "$database" -X -A -t -F ' | ' -c \
  "select u.\"Email\", u.\"IsApproved\", u.\"EmailVerified\", coalesce(string_agg(r.\"Name\", chr(44)), chr(45)) from \"AppUsers\" u left join \"AppUserRoles\" ur on ur.\"UserId\" = u.\"Id\" left join \"AppRoles\" r on r.\"Id\" = ur.\"RoleId\" where u.\"Email\" = '$email' group by u.\"Id\""
