# Phase 0 Research — Cross-Cutting Dedup + State Records

All "NEEDS CLARIFICATION" for this phase are resolved below. The spec is highly detailed; the only
genuine unknowns were (a) the real current-tree counts vs the spec's estimates and (b) how to share
an **internal** helper across two signature-file-having Testing modules without touching the public
surface. Both are settled.

## Decision 1 — Counts re-confirmed against the 2026-06-22 tree (supersede spec estimates)

- **Decision**: Use the confirmed counts in plan.md "Scale/Scope". The headline corrections: the
  metrics record has **32 fields** (not ~36); it is constructed by **2** full spell-out sites (not
  ~5), plus 4 `{ zero with … }` partial-update sites that do *not* re-spell every field.
- **Rationale**: Direct read of `ControlsElmish.fs:63–97` (type), `1423–1460` and `1957–1990`
  (full constructions). Spec Assumptions explicitly flag the counts as estimates to re-confirm and
  define the requirement as "built once," not a number.
- **Alternatives considered**: Treating the 4 `{ zero with … }` sites as additional "construction
  sites." Rejected — they already delegate to a `zero`/default value and re-spell only the deltas;
  forcing them through the builder would be a behavior-neutral nicety but is **not required** by
  FR-001 (which targets the full hand-spelled records). They MAY be left as-is or routed through a
  `with`-style overload of the builder if it stays byte-identical.

## Decision 2 — `runScriptCore` state record holds the 7 metric carriers (FR-004 scope)

- **Decision**: `FrameScriptState` collects the **7 metric-carrier** mutables
  (`lastMemo`, `lastVirtual`, `lastDamage`, `lastPicture`, `lastReplay`, `lastTextCache`,
  `lastInvalidated`; `ControlsElmish.fs:1849–1865`). The 3 genuine workflow-state mutables
  (`model`, `retained`, `lastRender`; `1840–1845`) MAY also move into the record for cohesion but
  are not the FR-004 target and can stay loose if that keeps the diff smaller and byte-identical.
- **Rationale**: FR-004 names "frame-metric carrier mutables." Spec SC-002 measures the
  metric-carrier count → 0. The 3 workflow vars are not metrics.
- **Alternatives considered**: Moving all 10 into one record (cleaner) vs only the 7 (minimal). Pick
  at implementation time on whichever is plainer while staying byte-identical; both satisfy FR-004.

## Decision 3 — Shared Testing helpers are `internal` in `TestingVisual.fsi`, NOT public (FR-009)

- **Context / hazard**: The parent report (§8) flags the F# back-edge / module-ordering hazard. The
  Testing project compiles in this order: `TestingTypes` → **`TestingVisual`** →
  **`TestingRetainedInspection`** → `TestingEvidence` → `TestingCompositor` → `Testing`
  (`src/Testing/Testing.fsproj:15–26`). Both US3 (validation) and US4 (managed-section) duplicates
  live in `TestingVisual.fs` (the earlier module) **and** `TestingRetainedInspection.fs` (the later
  module). Because every Testing module has an `.fsi`, anything **absent** from the `.fsi` is private
  to its own `.fs` and **invisible** to the other file — so a shared helper buried privately in
  `TestingVisual.fs` cannot be called from `TestingRetainedInspection.fs`.
- **Decision**: Home the shared validation routine and the shared managed-section helper in a
  `module internal …` declared in **`TestingVisual.fsi`** (which compiles before
  `TestingRetainedInspection`). Expose them as **`internal`** (assembly-internal), NOT public. The
  existing `module internal ReadinessFormatting` (`TestingVisual.fsi:53`, "consumed across Testing
  domain files") is the proven precedent and a natural host (or a sibling `module internal`).
- **Why this satisfies FR-009 / SC-006**: The public package surface is the set of **public**
  symbols. Empirically confirmed: `module internal ReadinessFormatting` does **not** appear in
  `readiness/surface-baselines/FS.GG.UI.Testing.txt` (grep count = 0) — the surface baseline tracks
  public-only, so adding `internal` declarations leaves the baseline diff empty and forces no version
  bump. This mirrors `RetainedRender.fsi`, whose entire surface is `type internal …` / assembly-
  internal and test-visible via `InternalsVisibleTo`.
- **Alternatives considered**:
  - *New file without an `.fsi`, compiled first* — would also work but adds a new file/module; the
    existing `module internal` host is simpler and keeps "extraction within existing modules"
    (FR-010 spirit).
  - *Expose the helper publicly in the `.fsi`* — **rejected**: changes the public surface (FR-009
    violation), would regenerate the baseline and arguably bump the version.
  - *Duplicate the shared helper into both files* — rejected: that is the very duplication this
    feature removes.

## Decision 4 — Public wrappers stay; new internals are private-by-`.fsi`-absence

- **Decision**: For every public site touched (`FrameMetrics` type, `VisualInspectionValidation.validateCheck`,
  `RetainedInspectionValidation.validateCheck`, the three `updateManagedSection` functions), the
  **public signature is unchanged** — each becomes a thin delegator to a new internal
  builder/routine. New records (`FrameState`, `FrameScriptState`) and the `FrameMetricsBuilder` are
  defined only in the `.fs` body and **omitted from the `.fsi`** → private by compiler enforcement
  (constitution II). `step`/`init`'s `FrameState` lives inside `RetainedRender.fs`; its `.fsi`
  (already fully `internal`) is unchanged.
- **Rationale**: Tier-2 internal change; FR-007/FR-009 demand byte-identical surface and behavior.
- **Alternatives considered**: Changing `validateCheck` to take the shared routine's parameter shape
  directly — rejected, that would alter the public signature.

## Decision 5 — Verification = baseline-first byte/semantic diff (no §7 gates)

- **Decision**: Mirror feature 185's baseline-first discipline. Capture, before any production edit:
  (1) the red/green test set across the 4 affected projects, (2) rendered frames + per-frame metrics
  for the relevant corpus, (3) emitted readiness/inspection/evidence artifacts. Diff each story
  against it — **byte-identical** for frames/metrics and for artifacts whose deduplicated logic was
  already identical; **semantically equivalent** only where prior wording legitimately differed.
- **Rationale**: Spec Assumptions + parent report §7: the golden-image/perceptual/perf gates are
  scoped to render-altering Phases 5–6 and are **out of scope** here. Byte-identity is the cheap,
  sufficient gate for a Pattern-C refactor.
- **Alternatives considered**: Standing up the §7 gates now — explicitly deferred by the spec.

## Decision 6 — Float accumulation order is the silent-failure risk; byte-identity is the catch

- **Decision**: When collapsing `step`'s 19 accumulators into `FrameState`, **preserve the exact
  update/evaluation order** of every metric sum and cache mutation; the record fields are mutated in
  the same sequence as the former loose mutables. Use `mutable` record fields (not immutable
  copy-on-write) so the hot-path semantics and allocation profile are unchanged, with a
  `// mutable: hot path` disclosure per the constitution.
- **Rationale**: Reordering float accumulation can change rendered bytes (spec Edge Cases). The
  byte-identical frame/metrics diff is the gate that catches any slip.
- **Alternatives considered**: Immutable record threaded functionally — rejected: changes
  allocation, risks reorder, and the constitution prefers the plain mutable accumulator on a hot path.
