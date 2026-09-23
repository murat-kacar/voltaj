# docs/reviews/README.md

Self-review checklists for operations completed without a forge (forge-less workflow, A7).

When A3 (branch protection / human review via forge) is not in place, every operation must be self-reviewed before being marked done. The checklist for each operation is written here.

## Format

Each file is named `<operation-id>.md` (e.g. `create-customer-v1.md`).

### Checklist template

```markdown
# Self-Review: <OperationId>

**Date**: YYYY-MM-DD  
**Rules in scope**: (from task-index row in AGENTS.md)  
**Outcome**: PASSED / FAILED  

| Rule | Check | Result |
|:---|:---|:---:|
| M9 | Cross-cutting concerns via pipeline, no duplication | ✅ |
| M10 | Only command schema + business logic per operation | ✅ |
| ... | ... | ... |

**Diff read**: Yes / No  
**Gates run**: `make gates` exit 0 — Yes / No  
**Done claimed**: Yes / No  
```

## Index

| Operation | File | Date | Outcome |
|:---|:---|:---|:---:|
| Quote Refund (H10) | `quote-refund-h10.md` | 2026-09-23 | PASSED |
| WorkOrder Cancel (H11) | `workorder-cancel-h11.md` | 2026-09-23 | PASSED |
