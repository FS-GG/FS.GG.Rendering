# Phase 0 Research: Scene.fs Module Split

All NEEDS CLARIFICATION resolved. Decisions below are grounded in the current tree (re-confirmed
2026-06-22) and the parent report `docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md`.

## Decision 1 — US1 type re-home mechanism: namespace-level file split

- **Decision**: Move the type wall (Scene.fs lines 7–779, `Size`…`RetainedInspectionSummary`) into
  a new `Types.fs` declared as `namespace FS.GG.UI.Scene` with **namespace-level** type definitions
  (no wrapping module). Add a matching `Types.fsi`. Compile `Types.*` before `Scene.*`.
- **Rationale**: The surface baseline (`readiness/surface-baselines/FS.GG.UI.Scene.txt`) is generated
  from CLR `Type.FullName` (`scripts/refresh-surface-baselines.fsx`). Namespace-level types keep
  `FS.GG.UI.Scene.Size` etc. byte-identical → **zero surface drift, zero consumer churn, no version
  bump for the re-home**. F# permits multiple files contributing types to the same namespace. This
  honors Constitution III (idiomatic simplicity) and matches the Phase 1–3 surface-stable discipline.
- **Alternatives considered**:
  - *Sub-module `FS.GG.UI.Scene.Types` (spec body's literal wording)* — rejected: changes every
    type's `FullName`, forces a reviewed surface diff, `open`/reference updates across all 17
    consumers, and a version bump, for **no functional gain**. Maintainer confirmed the file-split
    mechanism 2026-06-22 (the spec Assumptions explicitly defer the mechanism to planning).
  - *Re-export shims from a sub-module back to namespace level* — rejected: more code, dual surface,
    no benefit over the plain namespace-level split.

## Decision 2 — US2 shaping unification + measurer relocation

- **Decision**: Create `TextShaping.fs` exposing `module Text.Shaping` that owns: (a) **one** private
  parameterized shaped-text core that `buildGlyphRun`, `buildFallbackShapedText`, and
  `glyphRunDataFromShapedText` are re-expressed in terms of (collapsing the ~60% duplicated logic,
  incl. the shared `glyphRunFingerprintOf`/`shapedTextFingerprintOf`/`directionOf`/`scriptOf`
  helpers, Scene.fs L1040–1296), and (b) the `realTextMeasurer` mutable cell + its set/measure logic
  (L1306–1323) as **single owner** (FR-003). The public shaping entry points remain reachable from
  `module Scene` as thin delegations so output stays byte-identical (FR-006/SC-004).
- **Rationale**: `module Scene` is a non-namespace module and **cannot span files**, so the impl bulk
  must move to a different module to shrink `Scene.fs`. Keeping public delegations in `module Scene`
  minimizes surface churn while still satisfying FR-002 (3→1 unification) and FR-003 (single owner).
  Glyph fingerprints are hash-of-ordered-fields, so the unified core MUST preserve field/accumulation
  order (Edge Case "float/accumulation order") to keep fingerprints byte-identical.
- **Open surface question (resolved by gate, not by guess)**: whether any public name *necessarily*
  relocates to `Text.Shaping` (and thus appears in the baseline diff) is determined by the regenerated
  baseline, not assumed. Version bump iff that diff is non-empty (plan "version-bump gate").
- **Alternatives considered**:
  - *Move the public functions wholesale to `Text.Shaping`, drop the `Scene` shims* — defensible and
    spec-literal, but increases surface churn (consumers of `Scene.buildGlyphRun` etc. update). Kept
    as the fallback if delegation shims prove awkward; the byte-identical-output requirement is the
    same either way.
  - *Leave the trio in `module Scene`, dedup in place* — rejected: does not shrink `Scene.fs`
    (FR-001/SC-001) and leaves the measurer seam un-quarantined (FR-003).

## Decision 3 — US3 module moves are surface-neutral; module names preserved

- **Decision**: Move `SceneEvidence` + `LayoutEvidence` into `Evidence.fs` and `VisualInspection` +
  `RetainedInspection` into `Inspection.fs`, **keeping the existing namespace-level module names**
  (`module FS.GG.UI.Scene.VisualInspection`, etc.). Each file gets a matching `.fsi`.
- **Rationale**: These four are already namespace-level modules (`.fsi` lines 1184/1193/1202/1248),
  so a file move with unchanged module names is surface-neutral — the same idiom as US1. FR-004's
  "extracted into a `Scene.Inspection` module/file" is read as the *Inspection file* under `src/Scene`
  (file-level grouping), consistent with the maintainer's US1 choice. This isolates the only intended
  behavior change (the dedup) from any surface noise.
- **Alternatives considered**: *Nest under new `Scene.Inspection`/`Scene.Evidence` parent modules* —
  rejected: changes `FullName`s of every inspection/evidence symbol, churns consumers (Testing/Harness
  read these), for no functional gain over file grouping.

## Decision 4 — what "finish the FR-006 dedup" means

- **Decision (hypothesis to confirm against baseline)**: Today `duplicateIds` (Scene.fs L1836, L2043)
  finds finding/node/region IDs that appear more than once and **emits a diagnostic string** ("duplicate
  … id: {id}"); `stableFindingId` (L1813) + `cleanToken` build a stable identity token per finding.
  The dedup is *started* (detection + identity tokens exist) but *unfinished* (duplicate **findings are
  not collapsed** — only reported). "Finishing" = collapse findings sharing a `stableFindingId` to one,
  applied **uniformly** across the visual (`VisualInspection`) and retained (`RetainedInspection`) paths
  (SC-003: zero inspection paths still emit known-duplicate findings).
- **Rationale**: Matches the spec's "collapses duplicate findings by their identity token while
  preserving every unique finding" (Key Entities) and US3 acceptance #2/#3 (collapse duplicates only;
  never silence a unique real finding; never weaken a fail-loud diagnostic).
- **Verification (parent report §7, applied as method not deliverable — FR-012)**: semantic-artifact
  diff (parsed status/counts/headers vs the captured baseline corpus) for evidence/inspection output;
  golden-hash/golden-image review-gate for any rendered output; explicit reviewed-and-approved
  expected-output record for the dedup delta (SC-007). Standing the §7 harness up as permanent CI is
  **out of scope** (deferred follow-up).
- **Alternatives considered**: *Treat detection-only as "done"* — rejected: contradicts FR-005/SC-003.
  *Broad behavioral rewrite beyond duplicate collapse* — out of scope (spec Assumption "dedup delta is
  small and reviewable"); if collapse turns out to need broad changes, defer to a dedicated feature.

## Decision 5 — F# compile-ordering / back-edge avoidance

- **Decision**: `Scene.fsproj` `<Compile>` order becomes: `Types.fsi`, `Types.fs`, then the
  `TextShaping.*` / `Scene.*` pair (their **relative** order fixed empirically by US2's delegation
  direction — if `module Scene` shims call into `Text.Shaping`, `TextShaping.*` compiles **before**
  `Scene.*`, as shown in contract C1; resolve by compiling), then `Inspection.fsi`, `Inspection.fs`,
  `Evidence.fsi`, `Evidence.fs`, then the unchanged `SceneWire.fs` / `SceneCodec.*` / `Animation.*`
  tail. The one fixed invariant: `Types.*` precede all; `Inspection.*`/`Evidence.*` follow `Scene.*`.
- **Rationale**: F# resolves names by file order. Types must precede everything; `Scene` builders
  precede the shaping/inspection/evidence files (the `Scene` shims call into `Text.Shaping`, so
  `TextShaping.*` must come *after* the `Scene` declarations it is called from — confirm no reverse
  dependency; if `module Scene` shims must call `Text.Shaping`, then `TextShaping.*` compiles *before*
  `Scene.fs`. Resolve the exact relative order empirically during US2 by compiling). No file may create
  a circular module dependency (Edge Case). Validate with a full-solution compile per story.
- **Alternatives considered**: none — file order is the only F# mechanism.

## Decision 6 — verification toolchain

- **Build**: `dotnet build FS.GG.Rendering.slnx -c Release`.
- **Test**: `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` (GL suites need X11).
- **Surface baseline**: regenerate with `dotnet fsi scripts/refresh-surface-baselines.fsx`, diff
  `readiness/surface-baselines/FS.GG.UI.Scene.txt`; empty diff ⇒ no version bump.
- **Rationale**: These are the repo-standard commands (README L42–43); `SurfaceAreaTests` is the live
  drift gate that reads the committed baseline.
