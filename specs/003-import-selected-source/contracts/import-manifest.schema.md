# Contract: Import manifest

The manifest records every imported group: what was copied, from where, to where, in which
dependency tier, and any adaptation applied. It drives the import and is the basis for the
provenance record.

## Required structure

A table per group (runtime / tests / template / docs):

| Source path | Repo path | Tier | Disposition | Adaptation |
|---|---|---|---|---|

## Field rules

- **Source path**: path under `FS-Skia-UI` at the pinned commit.
- **Repo path**: destination under this repo (`src/`, `tests/`, `template/`, `docs/`).
- **Tier**: dependency tier 1–6 for runtime; `—` for tests/template/docs.
- **Disposition**: `import-as-is` | `adapt` | `exclude`.
- **Adaptation**: required when `adapt`/`exclude` — names the change (governance removal,
  Vulkan-naming cleanup, authors/identity, `.fs` access-modifier strip).

## Acceptance (maps to spec)

- [ ] Every R2 `import-from-source` module appears with a tier. *(FR-001)*
- [ ] Every R3 `import-now` test project appears; excluded projects marked `exclude`. *(FR-004, FR-006)*
- [ ] Template + current docs/ADRs appear; historical readiness logs marked `exclude`. *(FR-003, FR-009)*
- [ ] No `SkillSupport`/governance path has disposition other than `exclude`. *(FR-006)*
