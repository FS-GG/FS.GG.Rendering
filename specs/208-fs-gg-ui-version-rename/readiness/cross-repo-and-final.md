# T030–T034 — Cross-repo contract + final quickstart validation

## T030 Gate (FR-010)
US1 verified (T012 green: 1 FsGgUiVersion, 0 FsSkiaUiVersion, restore+build+invariant green) AND
US2 verified (T016 green: fs-gg-ui/v* at same commits, fs-skia-ui/v* empty). Gate satisfied.

## T031/T032 Registry + ADR (FS-GG/.github) — PR #3 MERGED
- coherence ids `fs-skia-ui-version` → `fs-gg-ui-version`, `fs-skia-ui-bom` → `fs-gg-ui-bom`
- embedded `FsSkiaUiVersion` → `FsGgUiVersion`, tag refs `fs-skia-ui/v*` → `fs-gg-ui/v*`
- per-entry `note:` records the feature-208 clean-break rename
- `docs/registry/compatibility.md` projection updated to match
- ADR-0003 Proposed → **Accepted** (implemented + verified by feature 208)
- PR: https://github.com/FS-GG/.github/pull/3 (merged 2026-06-27); verified on `main`.

## T033 Downstream (FR-011)
- FS.GG.SDD: 0 references (clean).
- FS.GG.Templates: 10 references, all historical/conceptual (named "FsSkiaUiVersion staleness class",
  renovate.json description string, dated 2026-06-27 incident reports, one stale tag link). No live
  pin / no build-time tag lookup → no functional break. Cross-repo request filed:
  https://github.com/FS-GG/FS.GG.Templates/issues/6 (decision deferred to Templates owner).

## T034 — quickstart.md Steps 1–4 / Success Criteria
| SC | statement | result |
|---|---|---|
| SC-001 | generated product: exactly one `FsGgUiVersion`, zero `FsSkiaUiVersion` anywhere | ✓ (smoke-post-rename.md) |
| SC-002 | generated product restores+builds green driven solely by `FsGgUiVersion` | ✓ restore+build EXIT 0; invariant 30/30 |
| SC-003 | `fs-gg-ui/v<V>` resolves to the same commits; `fs-skia-ui/v*` empty | ✓ (tag-swap.md, local+remote) |
| SC-004 | zero `fs-skia-ui`/`FsSkiaUiVersion` in current shipped docs (only specs/** history) | ✓ (doc-sweep.md) |
| SC-005 | all three surfaces use the `fs-gg-ui` root; registry updated; ADR-0003 Accepted | ✓ (PR #3 merged) |

All success criteria hold. Feature 208 complete.
