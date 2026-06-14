# Contract: Provenance record (`PROVENANCE.md`)

Makes the import auditable: every imported file traces to a source commit and original path.

## Required structure

```markdown
# Provenance

**Source repository**: EHotwagner/FS-Skia-UI
**Source commit**: f759f399... (2026-06-14)
**Imported at**: <stage R4 / feature 003>

## Path map
| Source path | Repo path |
|---|---|
| src/Scene/ | src/Scene/ |
| tests/Color.Tests/ | tests/Color.Tests/ |
| ... | ... |

## Adaptations
- Removed: src/SkillSupport, tests/Governance.Tests, tests/SkillSupport.Tests, readiness/, docs/testSpecs.
- Vulkan cleanup: removed vestigial ViewerBackendPreference.Vulkan case + stale comments.
- Identity: Authors → FS.GG; package identity preserved as FS.Skia.UI.* (rebrand deferred to R8).
- Visibility: stripped any .fs top-level access modifiers (moved to .fsi).
```

## Field rules

- **Source commit**: the exact commit hash imported from. Required.
- **Path map**: covers every imported top-level group (each `src/**`, `tests/**`, template,
  docs). Required.
- **Adaptations**: lists every non-verbatim change category. Required.

## Acceptance (maps to spec)

- [ ] Source repo + commit recorded. *(FR-008, SC-006)*
- [ ] Every imported group has a path-map row. *(SC-006)*
- [ ] Governance removal, Vulkan cleanup, and identity handling are listed. *(FR-005/006/010)*
