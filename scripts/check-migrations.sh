#!/usr/bin/env bash
# V4 gate: Expand/Contract in three steps - a migration new on this branch may not single-step
# drop or rename a column/table. Historical migrations already applied before this gate existed are
# not retroactively checked (see docs/architecture/voltflow-agents-compliance-roadmap.md Phase 4) -
# only migrations added since this branch diverged from main are in scope.
set -u

base=$(git merge-base main HEAD 2>/dev/null || echo "")
if [ -z "$base" ]; then
  echo "check-migrations: could not determine merge-base with main; skipping (not on a feature branch?)"
  exit 0
fi

# Committed-since-base migrations, plus anything staged or still untracked in the working tree -
# a local `make gates` run should catch a new migration before it's even committed.
committed_new=$(git diff --name-only --diff-filter=A "$base" -- 'src/Voltflow.Infrastructure/Migrations/*.cs' 2>/dev/null)
untracked_new=$(git ls-files --others --exclude-standard -- 'src/Voltflow.Infrastructure/Migrations/*.cs' 2>/dev/null)
new_migrations=$(printf '%s\n%s\n' "$committed_new" "$untracked_new" | grep -v '^$' | sort -u \
  | grep -v '\.Designer\.cs$' | grep -v 'ModelSnapshot\.cs$')

if [ -z "$new_migrations" ]; then
  echo "check-migrations: no new migrations since $base - nothing to check."
  exit 0
fi

violations=0
for file in $new_migrations; do
  # Down() legitimately drops/renames to reverse its own Up() - only Up() is the forward migration
  # that actually ships, so only Up()'s body is checked against Expand/Contract.
  up_body=$(awk '/protected override void Up\(/{flag=1} /protected override void Down\(/{flag=0} flag' "$file")
  if echo "$up_body" | grep -qE '\.(DropColumn|RenameColumn|DropTable|RenameTable)\('; then
    echo "VIOLATION: $file's Up() uses a single-step Drop/Rename operation." >&2
    echo "  V4 requires Expand/Contract in three separate migrations: add the new shape, backfill" >&2
    echo "  and dual-write, then drop/rename only once nothing reads the old shape." >&2
    violations=1
  fi
done

if [ "$violations" -eq 1 ]; then
  exit 1
fi

echo "check-migrations: all new migrations follow Expand/Contract (no single-step drop/rename)."
