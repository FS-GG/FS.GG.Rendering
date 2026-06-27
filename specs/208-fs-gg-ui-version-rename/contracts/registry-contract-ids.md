# Contract: Registry Contract Ids + ADR-0003 (cross-repo, `FS-GG/.github`)

Covers FR-010/011 and SC-005. Owned by `FS-GG/.github`; tracked here as the cross-repo dependency
that makes the contract surface coherent. Executed via `gh` + the `cross-repo-coordination` skill —
**not** as files in this repo — and **only after** the in-repo property/tag rename is verified.

## Obligations

| Action | Site (sibling repo) | Detail |
|--------|--------------------|--------|
| Rename version contract id | `registry/dependencies.yml`, `docs/registry/compatibility.md` | `fs-skia-ui-version` → `fs-gg-ui-version` |
| Rename BOM contract id | `registry/dependencies.yml`, `docs/registry/compatibility.md` | `fs-skia-ui-bom` → `fs-gg-ui-bom` |
| Accept ADR | `docs/adr/0003-rename-fs-skia-ui-version-machinery-to-fs-gg-ui.md` | Proposed → **Accepted** on resolution |
| Verify downstream (FR-011) | Templates, SDD | Confirm no `FsSkiaUiVersion` / `fs-skia-ui/*` reference remains; if found, raise a cross-repo request |

## Gating & sequencing

- The registry write and ADR flip happen **after** the property rename (US1) and tag re-point (US2)
  are verified in this repo — the registry must not flip to ids for a rename not yet shipped.
- Carries the `contract-change` + `cross-repo` labels; coordinated through the GitHub-native protocol,
  not vendored locally.

## Acceptance (SC-005)

All three contract surfaces (property, tag namespace, registry ids) use the `fs-gg-ui` root, the
registry projection is updated, and ADR-0003 is Accepted. FR-011: Templates/SDD confirmed clean (or a
cross-repo request filed for any reference found).
