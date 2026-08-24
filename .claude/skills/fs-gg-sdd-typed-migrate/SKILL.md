---
name: fs-gg-sdd-typed-migrate
description: Analyze, accept, and roll back Standard SDD to Typed SDD migrations.
---

# Typed SDD migrate

Run `fsgg-sdd typed-sdd migrate --work <id> --source work/<id>/spec.md` without `--accept` first.
Review the semantic inventory and `Migrated`, `Ambiguous`, or `Unsupported` classification. Repeat
only a `Migrated` result with `--accept`; the accepted transaction preserves the original authority.
Use `fsgg-sdd typed-sdd rollback --work <id> --accept` to restore that preserved Standard SDD source.
