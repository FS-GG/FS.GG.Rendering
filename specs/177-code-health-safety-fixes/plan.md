# Implementation Plan: Code Health — Quick Safety Fixes (Refactoring Phase 0)

**Branch**: `177-code-health-safety-fixes` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/177-code-health-safety-fixes/spec.md`

## Summary

Phase 0 of the code-health refactoring plan: resolve one suspected latent bug and bank two
near-zero-risk cleanups so later, heavier phases start from a trusted base. Three independent items:

1. **Fix the `feature159Hash` offset typo** (`src/Controls/RetainedRender.fs:851`). The seed
   `1469598103934665603UL` is the standard 64-bit FNV-1a offset basis `14695981039346656037UL`
   **with the trailing `7` dropped** (19 digits vs 20). The FNV prime on the same fold
   (`1099511628211UL`) is correct, and the three other accumulators in the repo all use the hex
   form `0xcbf29ce484222325UL` (`Composition.fs:157`, `Control.fs:2454`, `Control.fs:2830`). This
   is an unambiguous typo, not an intentional value. **Decision: change it to `0xcbf29ce484222325UL`**
   (hex form, matching the other three sites) and verify no persisted/golden hash artifact is
   silently invalidated.
2. **Replace two tautological test placeholders** (`Feature093ParityTests.fs:77`,
   `TypedMigrationTests.fs:555`), each `Expect.isTrue true …`, with real falsifiable assertions.
3. **Centralize the `"rev=150"` layout-cache version** (`Layout.fs:839`, `:964`) behind one named
   constant, byte-identical output preserved.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This is the rare case where the standing "live smoke run" requirement maps to the build+test
> suite rather than a UI observation: the change set has **no app-visible behavior**. The only
> runtime change is an internal, private hash *value* (`feature159Hash`) that feeds layer
> reuse/promotion identity comparisons; it produces no different pixels, layout, or user-facing
> output. The honest "smoke" for this Tier-2 change is a clean `dotnet build` + full `dotnet test`
> green run with the Feature 159 identity/reuse/promotion/readiness suites intact, plus an explicit
> golden/artifact diff review (see FR-002). `/speckit-tasks` MUST still schedule that full
> build+test run early (Foundational phase) to confirm the typo hypothesis before banking the
> cleanups, and again at the end.

## Technical Context

**Language/Version**: F# on .NET `net10.0`

**Primary Dependencies**: SkiaSharp over OpenGL (rendering); Expecto (test framework — `test` /
`testList` / `Expect.*`). No new dependencies introduced by this feature.

**Storage**: N/A (no persistence change). Persisted readiness/evidence artifacts under `specs/*/readiness/`
are *reviewed* (FR-002) but not written by this feature.

**Testing**: Expecto across `tests/Controls.Tests`, `tests/Rendering.Harness.Tests` (Feature 159
identity/reuse/promotion/readiness suites), plus the API surface-drift check.

**Target Platform**: Linux/desktop (SkiaSharp + GL viewer host).

**Project Type**: Multi-project F# UI framework / desktop-app (`src/Controls`, `src/Layout`, viewer,
Elmish, etc.). This feature touches `src/Controls/RetainedRender.fs`, `src/Layout/Layout.fs`, and two
files under `tests/Controls.Tests/`.

**Performance Goals**: N/A — no hot-path change. The hash fold and cache-key composition keep the
same shape; only one literal seed and one duplicated token change.

**Constraints**: Zero public `.fsi` surface change (Tier 2). Byte-identical layout cache-key output.
No silent invalidation of persisted goldens/evidence. No `private`/`internal`/`public` keyword on any
new top-level binding (visibility via `.fsi` presence/absence per Principle II).

**Scale/Scope**: 4 files, ~3 logical edits. ~1 changed literal + 1 new private constant + 2 rewritten
test assertions.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change Classification: Tier 2 (internal change).** No public API surface is added, removed, or
modified. `feature159Hash` is private (absent from `RetainedRender.fsi`); `feature159ContentIdentity`
is `val internal` and its *signature* is unchanged. The new layout-revision constant stays out of
`Layout.fsi` (private). `.fsi` files and surface-area baselines remain untouched.

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ Pass (N/A FSI) | No new public surface to sketch in FSI. Spec authored; tests-first applies to the two rewritten test assertions and a new byte-identity check for the layout token. |
| II. Visibility in `.fsi`, not `.fs` | ✅ Pass | New layout-revision constant is made private by omission from `Layout.fsi` — **no `private` keyword**. (Pre-existing `let private feature159Hash` keyword usage is out of scope; not introduced here.) |
| III. Idiomatic Simplicity | ✅ Pass | A named `[<Literal>]`/`let` constant replacing a duplicated string is strictly simpler. No clever features introduced. |
| IV. Elmish/MVU boundary | ✅ Pass (N/A) | No new stateful/I/O workflow. Pure value + pure string composition. |
| V. Test Evidence | ✅ Pass | Hash fix: **no fail-before/pass-after test is possible without asserting an absolute hash, which FR-002 explicitly forbids** (the seed shift moves all computed identities together, preserving every relation). Evidence is therefore the existing Feature 159 relational identity/reuse suites staying green + an explicit reviewed golden/evidence diff (T011) — a deliberate, disclosed deviation from the literal "fails before, passes after" wording for this value-only change. Placeholders: each replaced with a falsifiable assertion (the evidence itself). Layout: a byte-identity assertion proves output stability. No assertion weakened; no test left empty. |
| VI. Observability & Safe Failure | ✅ Pass (N/A) | No critical-path failure handling changed. |

**Gate result: PASS — no violations, no Complexity Tracking entries required.**

Re-check after Phase 1 design: **still PASS** — design artifacts confirm zero `.fsi`/baseline edits
and zero new dependencies.

## Project Structure

### Documentation (this feature)

```text
specs/177-code-health-safety-fixes/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — three decisions recorded
├── data-model.md        # Phase 1 output — the three "entities" (constants/assertions)
├── quickstart.md        # Phase 1 output — build/test validation guide
├── contracts/
│   └── README.md        # Phase 1 output — N/A rationale (no external contract change)
├── spec.md              # Feature specification
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Controls/
│   └── RetainedRender.fs   # :851 feature159Hash seed (typo fix)   [RetainedRender.fsi UNCHANGED]
└── Layout/
    └── Layout.fs           # :839,:964 "rev=150" token (centralize) [Layout.fsi UNCHANGED]

tests/
├── Controls.Tests/
│   ├── Feature093ParityTests.fs   # :77 Expect.isTrue true → real assertion
│   └── TypedMigrationTests.fs     # :555 Expect.isTrue true → real assertion
├── Controls.Tests/                # Feature159IdentitySplitTests, Feature159ReuseCounterTests (regression guard)
└── Rendering.Harness.Tests/       # Feature159PromotionEvidenceTests, Feature159ReadinessPackageTests (regression guard)
```

**Structure Decision**: Existing multi-project layout; this feature edits four files in place. No new
projects, modules, or files in `src/` or `tests/` beyond the spec folder's design docs.

## Complexity Tracking

> No Constitution Check violations. This section intentionally left empty.

## Implementation Progress (2026-06-21)

**Status: COMPLETE.** All 23 tasks in [tasks.md](./tasks.md) are marked `[X]`. All three Phase 0
items implemented on branch `177-code-health-safety-fixes`.

### What changed

| Item | File | Change |
|------|------|--------|
| US1 (P1) | `src/Controls/RetainedRender.fs:851` | FNV-1a offset seed `1469598103934665603UL` → `0xcbf29ce484222325UL` (canonical basis, hex, matching the three sibling accumulators). Old literal absent repo-wide. |
| US2 (P2) | `tests/Controls.Tests/Feature093ParityTests.fs:75` | `captureBaselines` now returns the written paths; the T020 test asserts each baseline file exists and is non-empty (falsifiable). |
| US2 (P2) | `tests/Controls.Tests/TypedMigrationTests.fs:548` | SC-003 placeholder replaced with runtime equality: typed `TextArea.init`/`ListView.init` equal the canonical `TextInput`/`Collections` init (model + effects). `ignore` lines removed. |
| US3 (P3) | `src/Layout/Layout.fs` | New private `[<Literal>] layoutCacheRevision = 150` feeds all four sites (two `rev=…` tokens + two `Revision` fields). Byte-identical output. |
| US3 (P3) | `tests/Layout.Tests/Feature151IntrinsicReuseTests.fs` | Added an explicit FR-006 byte-identity test pinning the literal `|rev=150` token in `QueryIdentity` and `EntryId`. |

### Evidence

- **Baseline (pre-edit):** 18 test projects, 16 green / 2 red. Pre-existing reds:
  `tests/Package.Tests` (8 fail, release-only package-feed) and
  `samples/ControlsGallery/ControlsGallery.Tests` (2 fail, package-feed consumer). Recorded in
  `readiness/baseline.md`.
- **Post-edit targeted:** `Controls.Tests` 932 passed / 1 skipped (byte-identical to baseline),
  `Layout.Tests` 79 passed (+1 new byte-identity test), `Rendering.Harness.Tests` Feature159 6 passed.
- **Post-edit full sweep:** recorded in `readiness/post-change.md` — same 2 pre-existing reds.
  `SecondAntShowcase.Tests` showed a transient exit-1 with a `Passed! Failed: 0` summary and a
  truncated total (159 vs 172) during the sweep — a flaky test-host crash, not a test failure.
  Re-run in isolation it is **171 passed / 1 skipped, exit 0** (green). It is a package-feed consumer
  that builds against the published package, not these source edits, so a real regression is
  impossible here. Net result: **no new regressions** (SC-001).
- **Tier-2 gate:** `git diff -- '*.fsi'` empty — zero public-surface change (FR-009, SC-005).
- `readiness/*.md` artifacts are local evidence (gitignored per Feature 168), not committed.

### Success criteria

- **SC-001** ✅ build+test green; the only reds are the 2 documented pre-existing package-feed failures, unchanged.
- **SC-002** ✅ `feature159Hash` seed resolved to the canonical hex basis; old literal gone.
- **SC-003** ✅ zero `Expect.isTrue true` in the two files; each test keeps a meaningful, falsifiable assertion.
- **SC-004** ✅ `rev=150` sourced from one constant; all four sites derive from it; cache strings byte-identical.
- **SC-005** ✅ no unintended `.fsi`/golden change; internal hash value is the sole reviewed runtime change.
