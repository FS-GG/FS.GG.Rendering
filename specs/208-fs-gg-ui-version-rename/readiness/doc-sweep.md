# T029 — Shipped-doc / provenance sweep verification (US3, SC-004)

```
$ grep -rn FsSkiaUiVersion PROVENANCE.md template/base/README.md template/base/docs/UPGRADING.md \
    .template.config/generated/README.md .template.package/README.md src/*/README.md
→ 0 matches  (SC-004 ✓)
```

UPGRADING.md: instructs editing `FsGgUiVersion` and carries the FR-008 pre-rename migration note
("## Migrating a pre-rename project …"), worded WITHOUT the old literal so SC-001 stays at zero.

Immutable boundary preserved (NOT edited — `git status` clean for these; FR-009):
- specs/** — 58 files still reference `FsSkiaUiVersion` (history, untouched)
- docs/product/decisions/0001-package-identity.md (2× fs-skia-ui), docs/audit/mechanism-inventory.md
  (1× fs-skia-ui), docs/bridge/package-identity-migration.md — all unmodified provenance.
