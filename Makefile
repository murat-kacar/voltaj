.PHONY: install dev gates typecheck test arch style contract check-domain-map check-review check-migrations check-telemetry check-secrets audit sbom license-policy check-asvs

install:
	dotnet restore
	npm ci
	npm ci --prefix frontend

dev:
	docker compose up -d postgres redis
	( dotnet run --project src/Voltflow.Api & \
	  dotnet run --project src/Voltflow.Worker & \
	  npm run dev --prefix frontend & \
	  wait )

gates: typecheck arch style contract test check-domain-map check-review check-migrations check-telemetry check-secrets audit license-policy check-asvs
	@echo "All gates passed."

typecheck:
	dotnet build Voltflow.sln --nologo
	npm run typecheck --prefix frontend

test:
	dotnet test Voltflow.sln
	npm test --prefix frontend

arch:
	@echo "make arch: not wired yet - no ArchUnitNET test project, no .dependency-cruiser.cjs config." >&2
	@echo "See docs/architecture/voltflow-agents-compliance-roadmap.md Phase 0 follow-up." >&2
	@exit 1

style:
	@echo "make style: not wired yet - no stylelint config in frontend/." >&2
	@echo "See docs/architecture/voltflow-agents-compliance-roadmap.md Phase 0 follow-up." >&2
	@exit 1

contract:
	npx --yes @stoplight/spectral-cli lint openapi.yaml

check-domain-map:
	@bash scripts/check-domain-map.sh

check-migrations:
	@bash scripts/check-migrations.sh

check-telemetry:
	@bash scripts/check-telemetry.sh

# G7: secret scan over git history AND the working tree (gitleaks, pinned + checksum-verified in .tools/).
check-secrets:
	@bash scripts/check-secrets.sh

# G7: known-vulnerability audit of NuGet (incl. transitive) and npm dependencies; fails closed when the source is unreachable.
audit:
	@bash scripts/check-dependencies.sh

# G8: CycloneDX SBOMs for the backend and the frontend into sbom/ (git-ignored), structurally validated.
sbom:
	@bash scripts/generate-sbom.sh

# G10: SPDX license policy (license-policy.json) evaluated over the SBOMs.
license-policy: sbom
	@node scripts/check-licenses.mjs

# G1: the declared ASVS level (catalog-info.yaml) is audited in full (docs/security/asvs-5.0.0-audit.json).
check-asvs:
	@node scripts/check-asvs.mjs

check-review:
	@echo "make check-review: not wired yet - depends on the A3/A7 decision (is GitHub branch protection actually on?)." >&2
	@echo "See docs/architecture/voltflow-agents-compliance-roadmap.md Phase 8." >&2
	@exit 1
