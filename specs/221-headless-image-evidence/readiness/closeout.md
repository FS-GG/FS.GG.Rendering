# Closeout — Feature 221 (Headless Image Evidence Path) — T027

**Date**: 2026-06-30 · **Branch**: `221-headless-image-evidence` · **Classification**: Tier 1

## Success Criteria

| SC | Statement | Verdict | Evidence |
|---|---|---|---|
| **SC-001** | Headless render yields a PNG decoding to requested dims with non-blank, non-stub content, 100% of runs | ✅ real | `evidence/representative-game-scene.png` (800×600 RGBA, `file`-confirmed PNG); test "renderPng yields a decodable, correctly-sized, non-blank, deterministic PNG". |
| **SC-002** | Same scene twice → identical bytes, ≥2 distinct runs/instances | ✅ real | `sha256` run1==run2 (`fixture.md`); tests T008 (same-process) + T009 (cross-instance). |
| **SC-003** | Live-window pixel proof obtainable by documentation alone, zero decompiling/guesswork | ✅ docs / ⚠️ live env-limited | `docs/usage.md` US2 section; live capture `environment-limited` (no GL) with disclosed substitute (`evidence/us2-live-frame.md`). |
| **SC-004** | Representative scene rendered under 5 s on a standard runner | ✅ real | median **11.9 ms** (`evidence/timing.md`). |
| **SC-005** | No artifact smaller than a valid image ever emitted as success; prior stub eliminated + regression-covered | ✅ real | `renderPng` returns typed failure when unproducible; test T018 ("typed UnsupportedEnvironment failure, never a stub"); the `Encoding.UTF8.GetBytes hash` stub is removed. |

## Edge Cases

| Edge case | Handling | Evidence |
|---|---|---|
| Zero/negative size | `ProductDefect`, preserved | test T019 |
| Very large size | succeeds within bounds (2000×1500 → valid PNG) or typed diagnostic, never a stub | test T019 |
| GPU-only effects | shared exhaustive painter — no node silently dropped; CPU-vs-GL is sub-pixel (spec accepts) | `degradation.md` |
| Fonts/text headless | bundled faces; uncovered glyph fallback **disclosed** via `Text.fallbackReport()` | test T022; `degradation.md` |
| Concurrent renders | injected rasterizer serialized + per-call local surface → isolated & deterministic | test T010 |

## Functional Requirements

FR-001/002/003/004/008 → US1 CPU raster path (real PNG, deterministic, no GPU/GL/display, fast).
FR-005 → typed honest failure (US3). FR-006 → US2 docs. FR-007 → `fr007-diff.md`.
FR-009 → `template/base/docs/evidence-formats.md`, `docs/usage.md`, `docs/harness/capability-baseline.md`.

## Test summary (workaround restore, see `baseline.md`)

- `Scene.Tests` 78/0 · `SkiaViewer.Tests` 213/0 (+6 new) · `Controls.Tests` 949/0 (1 pre-existing skip) ·
  `Package.Tests --filter Surface` 34/1 (1 pre-existing Build-engine RED).

## Tier-1 obligations

- Spec ✓ · Plan ✓ · `.fsi` updated ✓ (`surface-baseline.md`) · surface baseline = no new type ✓ ·
  tests (fail-before intent; US1/US3 semantic) ✓ · docs (FR-009) ✓.

## Readiness evidence allowlist (repo evidence rule)

`specs/*/readiness/` is gitignored by default. This feature is allowlisted in `.gitignore` lines 157-158:
`!specs/221-headless-image-evidence/readiness/` + `/**`. `git check-ignore` proof:

```
$ git check-ignore -v specs/221-headless-image-evidence/readiness/closeout.md
.gitignore:158:!specs/221-headless-image-evidence/readiness/**	specs/221-headless-image-evidence/readiness/closeout.md
$ git check-ignore specs/221-headless-image-evidence/readiness/closeout.md ; echo $?
1        # (no output, exit 1 = NOT ignored; negation rule wins) → readiness is committable
```

`evidence/` is not gitignored (no allowlist needed). 

## Disclosed caveats (not summarized as fully green)

- US2 live GL `OffscreenReadback` capture: **environment-limited** on this no-GL runner — documented +
  substitute recorded, **not** claimed as a live green capture.
- Full-graph `baseline-tests.fsx`: blocked by the pre-existing NU1403 lockfile issue; baseline recorded
  for affected projects via the documented workaround.
- `Package.Tests` Build-engine baseline: pre-existing RED, unrelated to this feature.

## Merge evidence

- Squash-merged `221-headless-image-evidence` → `main`; pushed; feature branch deleted.
- **No `FS.GG.UI.*` package version bump** — consistent with the recent feature merges (218/219/220),
  which do not bump per-feature; the publish/version cadence is a separate release process (features
  214/216 release-dispatch). This change touches **no sample package pins** and claims **no local-feed
  readiness**, so the local-feed pack / sample realignment step is not applicable here.
- Readiness allowlist verified committed (`.gitignore` 157-158; `git check-ignore` proof above).
