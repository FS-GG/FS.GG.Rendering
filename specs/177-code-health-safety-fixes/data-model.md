# Phase 1 Data Model: Code Health — Quick Safety Fixes

This is an internal code-health change with no domain entities, schemas, or persisted records. The
"entities" below are the three concrete code artifacts the feature touches, with their before/after
state and the invariants that must hold.

## Entity 1 — `feature159Hash` offset seed

| Field | Before | After |
|-------|--------|-------|
| Location | `src/Controls/RetainedRender.fs:851` | same |
| Literal | `1469598103934665603UL` (19 digits) | `0xcbf29ce484222325UL` (= `14695981039346656037UL`) |
| Binding visibility | private (absent from `RetainedRender.fsi`) | unchanged |
| FNV prime (same fold) | `1099511628211UL` | unchanged |

**Invariants**
- The fold algorithm (XOR byte, multiply prime, `|`-separator) is unchanged; only the seed changes.
- `RetainedRender.fsi` is **not** edited; `feature159ContentIdentity` keeps its `val internal` signature.
- Computed `ContentId` values change, but all Feature 159 tests assert *relations* between freshly
  computed identities, so every relation is preserved.

**State transition**: typo → canonical FNV-1a basis (hex form matching `Composition.fs`/`Control.fs`).

## Entity 2 — Tautological test assertions

| # | Location | Before | After (falsifiable) |
|---|----------|--------|---------------------|
| 2a | `tests/Controls.Tests/Feature093ParityTests.fs:77` | `Expect.isTrue true "frozen-oracle baselines written…"` | Assert the baseline files written by `captureBaselines ()` exist on disk under `specs/093-visual-state-style-layer/readiness/parity/`. |
| 2b | `tests/Controls.Tests/TypedMigrationTests.fs:555` | `Expect.isTrue true "no forked model type"` | Assert each typed facade `init` equals the canonical underlying `init` for the same input (reuse proven behaviorally); drop the now-redundant `ignore` lines. |

**Invariants**
- No `Expect.isTrue true` (or otherwise always-true) assertion remains in either file (SC-003).
- Each affected test retains **at least one** meaningful assertion (FR-004) — no empty test bodies.
- Surrounding tests in both files continue to build and pass.

## Entity 3 — Layout cache revision constant

| Field | Before | After |
|-------|--------|-------|
| Token sites | `Layout.fs:839` (`$"…|rev=150"`), `:964` (`"rev=150"`) | both derive from one constant |
| Int field sites | `Layout.fs:847`, `:974` (`Revision = 150`) | both derive from the same constant |
| Source of truth | duplicated literals (4 sites) | one private `[<Literal>] layoutCacheRevision = 150` (or equivalent) |
| Constant visibility | n/a | private (omitted from `Layout.fsi`) |

**Invariants (FR-006 — byte identity)**
- Composed `IntrinsicQuery.QueryIdentity` strings are byte-identical to pre-change output.
- Composed `cacheEntry` `EntryId` strings are byte-identical to pre-change output.
- The token renders exactly as `rev=150` at both string sites (invariant integer formatting).
- `Revision` record field values are unchanged (`150`).
- `Layout.fsi` is **not** edited.

**State transition**: four hand-duplicated `150`/`rev=150` literals → one named constant feeding all
four sites; a future bump is a single edit.

## Global invariants (apply to the whole change set)

- **Tier 2**: no `.fsi` file and no surface-area baseline is modified.
- **Build + full test suite green** after the change set, with no newly skipped tests (SC-001).
- **No runtime behavior change** except the internal `feature159Hash` value, which is explicitly
  reviewed against persisted goldens/evidence before merge (FR-002, FR-008, SC-005).
