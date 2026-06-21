# Phase 0 Research: God-Module Splits (Feature 182)

All "NEEDS CLARIFICATION" items from the Technical Context are resolved below. Findings are grounded
in the code at HEAD (line numbers from `grep`/`wc -l` on the six targets) and in the proven patterns
of features 179/180/181 (the prior code-health phases).

## D-001 — Byte oracle: what "behavior unchanged" means and how it is checked

- **Decision**: The acceptance oracle is a three-part byte-diff against a baseline captured *before any
  edit*: (1) **surface** — `dotnet fsi scripts/refresh-surface-baselines.fsx` regenerates all 12
  `readiness/surface-baselines/*.txt`; `git diff --exit-code readiness/surface-baselines/` MUST be
  empty. (2) **artifacts** — regenerate every readiness/evidence Markdown+JSON for the touched
  subsystems + capture viewer observations / scene hashes / fingerprints / damage regions; diff
  byte-for-byte. (3) **red/green** — `dotnet fsi scripts/baseline-tests.fsx` over every
  `*.Tests.fsproj`; the pass/fail set MUST equal baseline (known pre-existing reds unchanged, no new
  reds, no flipped greens).
- **Rationale**: This is exactly the gate features 180 and 181 used and it is the spec's stated
  acceptance gate (Assumptions, FR-002/003/008, SC-001/002/003). The surface generator is the *single
  authoritative* baseline location — the live `SurfaceAreaTests` gate and `build/Governance/
  PackageSurface.fs` both READ `readiness/surface-baselines/`, so writer and readers agree by
  construction. A green surface diff is therefore a sufficient proof of "no public surface change."
- **Alternatives considered**: Per-symbol manual `.fsi` review (rejected — the surface generator is
  automated and authoritative, manual review is error-prone for 40 KB `.fsi` files); trusting
  `dotnet build` alone (rejected — a split can compile while silently relocating a public symbol's
  module path, which only the surface diff catches).

## D-002 — F# file-order constraint and the "no back-edge" rule

- **Decision**: Each god-file is split by **carving extracted concern modules into new files inserted
  *before* the residual file** in the `.fsproj` `<Compile Include>` order, so the residual file
  references the extracted modules forward (the only direction F# allows). Any seam whose extraction
  would force a *back-edge* (extracted module needing something defined later) or would reorder a
  public symbol's definition site is **out of scope for that family** and retained as-is with the
  reason recorded (FR-009, Edge Cases).
- **Rationale**: F# compiles strictly in file order; there are no forward references across files. The
  six targets are internally layered (types → helpers → assembly → public module), which is the
  natural carve direction. `Testing.fs` (~30 already-grouped top-level modules) and `Scene.fs`
  (inspection sub-modules + a leading type block) are the cleanest; `Control.fs` `ControlInternals`
  (one 3,010-line internal module) is the hardest because intra-module references must be re-expressed
  as cross-file references in dependency order.
- **Alternatives considered**: `rec module` / `and` to allow mutual references (rejected — adds
  cleverness against Constitution III and risks surface/codegen changes); a single file with multiple
  `module` blocks (rejected — does not reduce per-file size, the actual goal).

## D-003 — Preserving the public-surface *union* across split files (Constitution II)

- **Decision**: The package `.fsi` stays **byte-identical**. Extracted files declare visibility with
  `module internal X` (or a new *internal* `.fsi` for that file) so nothing leaks to the package
  surface; the residual public module (`Viewer`, `Control`, `Scene`, `Testing`, …) keeps its exact
  name and continues to expose the same members — either by keeping the public members in the residual
  file or by re-exposing extracted internals through the unchanged `.fsi`. No `private`/`internal`/
  `public` modifier is ever added to a top-level `.fs` binding.
- **Rationale**: Constitution II: visibility lives in the `.fsi`, never in `.fs`. The package's
  external surface is the *union* of what the `.fsi` admits; as long as that file is unchanged and
  every public symbol still resolves at its original module path, the surface is unchanged regardless
  of which `.fs` file the implementation now lives in. FS0078 (internal-module name clashes) is handled
  with `module internal` on the new files.
- **Alternatives considered**: Promoting helpers to public to ease the split (rejected — that *is* the
  forbidden surface change, FR-002, Edge "public-surface leakage"); per-file `[<AutoOpen>]` shuffles
  (rejected — risks resolution/order changes).

## D-004 — US1 viewer run-loop unification (FR-004)

- **Decision**: Attempt to unify `runPresentedPersistentWindow` (`SkiaViewer.fs:2114`) and
  `runPersistentWindow` (`:2437`) behind one private lifecycle scaffold; if and only if the unified
  path produces byte-identical window observations, diagnostics, and evidence does it land. Otherwise
  retain both explicitly with the divergence recorded (FR-009). Both are currently `private` and have
  three internal call sites (`:3382`, `:3498`, `:3670`), all within `module Viewer`, so unification
  changes no surface.
- **Rationale**: They are confirmed near-duplicates (the spec's primary dedup target). Because both are
  private, unification is purely internal — the only risk is a behavioral/ordering difference, caught
  by the viewer evidence/screenshot byte-diff (D-001 part 2).
- **Alternatives considered**: Forcing a merge that subtly changes tick/render ordering (rejected by
  FR-004 — byte-stable observation wins; retain explicit if they diverge).

## D-005 — US2 chart preamble combinator (FR-005)

- **Decision**: Hoist the **exactly 17** `match pts with [] -> emptyState` chart preambles
  (confirmed count in `Control.fs`) into a `withPoints` combinator plus a shared bar-layout helper,
  landing only call sites that stay byte-identical in produced scene + scene-hash + fingerprint; any
  call site that genuinely diverges is left explicit (FR-009). The 170 `*Geom` bindings move into
  `ChartGeometry`/`WidgetGeometry` files.
- **Rationale**: The preamble is concrete, mechanical, 17× duplication with an obvious combinator seam
  — the highest-confidence dedup in the feature. `withPoints` is a plain higher-order function (no
  SRTP/CE), satisfying Constitution III.
- **Alternatives considered**: A typeclass/SRTP-style abstraction over chart kinds (rejected —
  cleverness, Constitution III); collapsing geometry families that diverge in detail (rejected per the
  180/181 measured-collapse lesson — size/legibility is the goal, not line count).

## D-006 — US3 Scene dedup + mutable isolation (FR-006)

- **Decision**: Complete the started-but-unfinished `cleanToken`/`duplicateIds`/`finding` dedup so
  Scene's `VisualInspection` and `RetainedInspection` share one implementation, with inspection
  records (tokens, findings, serialized form) byte-identical to baseline. Isolate the module-level
  `realTextMeasurer` mutable (and `measurementVersionBucket`) into a contained module without changing
  initialization timing or first-use semantics (Edge "module-level mutable side-channels").
- **Rationale**: The dedup is a named loose end in the spec; the inspection sub-modules are already
  conceptually distinct, making this lower-risk than viewer/controls. The mutable side-channel must
  keep identical observable behavior — moving it must not change when it initializes or how first use
  behaves; the inspection-record byte-diff is the guard.
- **Alternatives considered**: Deferring the dedup (rejected — FR-006 mandates completing it); making
  `realTextMeasurer` immutable (rejected — out of scope DU/ownership change, and mutation is allowed
  per Constitution III; only *isolation* is in scope).

## D-007 — US5 StepMetrics + US6 FrameLoopState (FR-007, Constitution III)

- **Decision**: Replace `RetainedRender.step`'s ~30 ad-hoc `let mutable` accumulators with a
  `StepMetrics` record threaded through named passes, and unify the build/paint scaffolding it
  duplicates with `init` (`RetainedRender.fs:1254`/`:1424`). Replace
  `runInteractiveAppWithLauncher`'s ~20 `ref` cells with a `FrameLoopState` record + module functions
  (`ControlsElmish.fs:1186`). **Mutation MAY be retained where it is the simpler/faster code on these
  hot paths**, disclosed with a one-line `// mutable: hot path` comment (Constitution III, FR-007) —
  the goal is *named passes over a typed accumulator*, not dogmatic immutability.
- **Rationale**: These are the two largest functions in the repo and frequent edit sites. Both are
  heavily test-covered (retained-render + damage-locality suites; frame-loop/render-lag traces), so the
  rendered-output/metrics/damage and trace byte-diffs are strong oracles. Constitution III explicitly
  endorses retained mutation on measured hot paths.
- **Alternatives considered**: Full immutable threading via `let rec` accumulator passing (rejected —
  Constitution III: `let rec` to hide state is the wrong tool; `mutable` is clearer and faster here);
  leaving the functions as-is (rejected — they are the named Phase-5 targets).

## D-008 — Scope per-story, single shared baseline (sequencing)

- **Decision**: All six stories share **one** baseline captured up front; each story is validated
  independently against it and is independently shippable (SC-004). US2 and US5 both touch
  `src/Controls/` (different files) and are serialized for clean per-story `Controls.txt` diffs.
- **Rationale**: Mirrors features 179/180/181. One baseline avoids re-capture churn; per-story
  validation preserves the "each split is its own PR" shape the plan describes (Assumptions).
- **Alternatives considered**: Six separate baselines (rejected — unnecessary; the surface + artifact
  set is stable across stories that don't touch the same package).

## Open items / explicitly deferred

- **Phase 6 (type-safety hardening)** — Control `Kind` registry, `SceneCodec` symmetry, boolean-trap
  cleanup — is **out of scope** (surface-affecting; spec Out of Scope). Some Phase-6 work touches the
  same files; this feature must not pre-empt it.
- Cross-project DU migrations (`RetainedInspectionStatus`/`VisualInspectionStatus` ownership) are
  **out of scope** (spec Out of Scope) — US3 isolates the mutable but does not move the DU.
- Any seam that proves un-splittable without a back-edge or surface change is recorded as an FR-009
  retention in that story's contract during implementation — not pre-decided here.
