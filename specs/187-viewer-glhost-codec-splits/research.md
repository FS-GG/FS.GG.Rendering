# Phase 0 Research: Viewer + GlHost + SceneCodec Module Splits

**Feature**: 187-viewer-glhost-codec-splits | **Date**: 2026-06-22

All line references re-confirmed against the working tree on 2026-06-22 and MUST be re-confirmed at
implementation time (parent report standing note).

---

## R1 — How do you split a public `.fs`/`.fsi` pair without changing the surface?

**Decision**: Move **bodies** into new **internal-only `.fs` files (no `.fsi`)**; leave thin
**public delegators** in the original `.fs`. The original `.fsi` and the surface baseline stay
byte-identical.

**Rationale**: F# binds exactly one `.fsi` to one `.fs`. A public function's fully-qualified path is
`Namespace.Module.fn`; relocating it to a different module changes that path and therefore the
`.fsi` and the surface baseline. But a file **without** a companion `.fsi` is fully internal by
compiler enforcement (Constitution II) — its bindings are invisible outside the assembly. The repo
already does this: `src/SkiaViewer/SceneRenderer.fs` and `src/Shared/Numeric.fs` are compiled with no
`.fsi`. Feature 186 used the identical move (internal `SharedTesting` helper + public delegators) to
dedup the Testing layer with an empty public diff.

**Alternatives considered**:
- *Take the relaxed surface freeze and relocate public functions into sub-modules* — permitted by the
  campaign, rejected here: it forces baseline regeneration + a version-bump decision + consumer
  churn for **zero** behavioral gain, contradicting the spec's surface-stable default and the 185/186
  precedent. Deferred as a possible later cosmetic pass, out of scope now.
- *F# `[<AutoOpen>]` partial modules across files* — F# has no cross-file partial module; rejected.

**Implication for `.fsproj` ordering** (no back-edge): new internal files compile *before* the
public `.fs` that calls them.
- `SceneWire.fs` → between `Scene.fs` (L16) and `SceneCodec.fsi` (L17).
- `GlHostRun.fs` → after `Host/OpenGl.fs` is not possible (OpenGl is where `run` lives); instead the
  internal units live **inside** `OpenGl.fs` as private modules *or* in a new file inserted before
  `Host/OpenGl.fsi` (L33). Confirmed in R3 — `GlHost.run`'s helpers only depend on already-compiled
  modules (`PresentMode`, `CompositorProof`, `Host/Diagnostics`, `Fonts`, `SceneRenderer`,
  `ReferenceRendering`, `Numeric`), so an internal `Host/GlHostRun.fs` placed before
  `Host/OpenGl.fsi` compiles cleanly and `OpenGl.fs` consumes it. The simpler option — private
  sub-modules within `OpenGl.fs` itself — is also viable since OpenGl is already 1,454 lines and the
  goal is ≤~1,500; **decision: prefer a new internal file** to actually reduce `OpenGl.fs` below the
  target.
- New SkiaViewer body files → before `SkiaViewer.fsi` (L39, currently last).

---

## R2 — Are the two persistent-window runners actually duplicates? (US1 / FR-002)

**Decision**: They are **not** near-identical. Extract the genuinely **shared scaffold** and keep the
two specializations as selectable behavior; do **not** force a single body. Treat the scaffold
extraction as the FR-002 deliverable and the per-runner specialization as preserved divergence.

**Findings** (read at L1421 `runPresentedPersistentWindow`, L1744 `runPersistentWindow`):

| Aspect | `runPresentedPersistentWindow` (~L1421) | `runPersistentWindow` (~L1744) |
|---|---|---|
| Window creation | `Host.Viewer.defaultConfiguration` → `ViewerProgram`/GlHost **present** path | Legacy raw `Window.Create WindowOptions.Default` |
| Input model | full input queue (`emptyInputQueue`, drain batches), pointer + resize + scripted inputs | bounded **warmup** FIFO (cap 64), **key-only**, no pointer/resize |
| Frame source | `getScene () |> nodeToScene` | `renderScene` passed in |
| Present mode | honors `options.PresentMode`, `FrameRateCap` | fixed 60 FPS/UPS |
| Shared | `windowOpened`/`framePresented`/`closeReason` refs; `withNativeWindowEnvironment` wrapper; diagnostic dispatch/capture; handler teardown; close-reason classification | same shared primitives |

**Rationale**: The parent report (§2 rank 5) called them "near-dup" by line-count similarity; the
bodies actually diverge on lifecycle (present-program vs raw-window), input model, and event
surface. Collapsing them into one body would risk behavior drift — exactly what this phase forbids.
The safe, valuable move is a **shared lifecycle scaffold** (window-environment setup, the three
lifecycle refs, diagnostic dispatch + capture, handler removal, close handling) that both runners
call, with the divergent steps (window construction, input pump, event handlers) passed in.

**Risk + de-risk**: If the shared surface turns out smaller than expected, US1 still delivers full
value via the **module-group split** (input-queue / responsiveness / evidence bodies moved out); the
scaffold unification is the smaller, optional sub-part of US1 and can be trimmed to "shared helpers"
without losing the story. SC-002 (2→1 run loops) is then re-read as "2→1 shared *scaffold*," not
"2→1 identical body" — recorded in quickstart's verification notes.

---

## R3 — `GlHost.run` decomposition seams (US2 / FR-003)

**Decision**: Carve `run` (~L1153, ~295 lines) into internal units — **Rendering** (offscreen
render→GPU→CPU readback, ~L788+), **Effects** (`interpretEffect`, ~L1227, incl. screenshot capture),
**Input** (key/pointer dispatch into the program), **Damage/Present** (the `runEventLoop`
present/scissor/no-clear decisions) — leaving `run` a thin orchestrator that wires them.

**Rationale**: `run` already delegates to many **public pure** decision functions (`shouldPresent`,
`planPresent`, `decideScissorRedraw`, `validateDamage`, `decideDamageScopedRender`, `shouldAdvanceFrame`)
that stay in `GlHost`'s public surface. The non-pure glue (GL resource lifecycle, the effect
interpreter, the readback/screenshot path, the Silk event-loop wiring) is what bloats `run`; it is
all internal and can move to `Host/GlHostRun.fs` (no `.fsi`). The public `GlHost.run` signature is
preserved (`ViewerProgram<'model,'msg> -> Result<unit, RenderDiagnostic>`).

**Constraint**: float accumulation / present ordering on the live path MUST be preserved (Edge Cases)
— the extraction reorders nothing, only relocates closures into named functions threading the same
state in the same order.

**Alternatives considered**: private sub-modules inside `OpenGl.fs` (keeps one file) — rejected in
favor of a new file so `OpenGl.fs` drops below ~1,500 (SC-001).

---

## R4 — SceneCodec node-codec table (US3 / FR-004, FR-005, Pattern A)

**Decision**: Introduce an internal per-node-kind codec representation in a new internal file
`src/Scene/SceneWire.fs` (no `.fsi`), grouped by node family (primitives / paint / path / text /
scene). Each node kind gets one entry pairing its write closure with its read closure and tag, so
adding a node kind is a one-site change and write-without-read drift is structurally impossible. The
public `SceneCodec` module (export/import/inspect/compare + package types) stays in `SceneCodec.fs`
and calls the table.

**Findings**:
- `writeSceneNode` (~L772) is an exhaustive `match node` with **25** arms (the comment at L756 notes
  it is deliberately exhaustive so `FS0025` flags a missing case).
- `readSceneNode` (~L1046) is a tag-dispatch that must mirror those 25 cases by hand — the
  hand-alignment is the silent-drift risk Pattern A removes.
- `Feature183CodecSymmetryTests.fs` already asserts symmetry; `Feature146PortableSceneRoundTripTests.fs`
  asserts byte-exact round-trip. These are the regression oracles for US3.

**Table shape decision** (detail in data-model.md): each entry is a record
`{ Tag: byte; Write: BinaryWriter -> SceneNode -> unit; Read: BinaryReader -> SceneNode }` indexed by
node kind for write and by tag byte for read. Because `SceneNode` is a single DU, the write side
still pattern-matches to pick the entry (preserving `FS0025` exhaustiveness — a *new* node kind with
no entry is a compile error); the read side is a tag→entry lookup that fails loud on an unknown tag
(FR-009). **Byte format is unchanged**: each entry writes exactly the bytes the current arm writes,
in the same order/width/endianness (Edge Cases) — verified by the round-trip corpus, not by visual
inspection.

**Rationale**: This is the highest-payoff, lowest-risk target — self-contained in `src/Scene`, with
existing byte-exact and symmetry oracles, and it converts the repo's worst silent-drift point into a
compiler-checked one.

**Alternatives considered**:
- *Reflection / source-gen over the DU* — rejected (Constitution III; reflection needs justification
  and adds risk for no benefit at 25 cases).
- *Keep two functions but co-locate each case's write+read lexically* — weaker: still two `match`es,
  no structural enforcement; rejected.

---

## R5 — Affected test suites & the equivalence oracle

**Decision**: The pre-refactor baseline + the existing suites are the full regression oracle; no new
behavioral tests are authored (Tier 2). New tests, if any, are **internal-helper unit tests** that
pin the extracted seams (allowed, additive) — optional, not required.

**Suites** (run under `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release`):
- `tests/SkiaViewer.Tests` — host/present/input-queue/responsiveness/OpenGl-host/live-proof/damage
  (US1 + US2). Many are GL/timing-bound and **legitimately skip without a GL surface** — the baseline
  records which, so a skip is not read as a regression.
- `tests/Scene.Tests` — Feature146 round-trip/resource/capability/compatibility + Feature183 codec
  symmetry (US3). Pure, deterministic — the byte-exact oracle.
- `tests/Smoke.Tests` — GL smoke (US2).
- `tests/Elmish.Tests` — Feature167/174 responsiveness regressions (US1).
- `tests/Rendering.Harness.Tests` — evidence artifacts that read viewer screenshots / scene packages
  (semantic-equivalence diff where wording/ordering varies).
- `tests/Package.Tests/SurfaceAreaTests.fs` — proves `FS.GG.UI.SkiaViewer.txt` and
  `FS.GG.UI.Scene.txt` baselines are unchanged (SC-006).

**Known pre-existing reds** (from feature 186's baseline, unrelated to this work): `Package.Tests` ×8
and `ControlsGallery.Tests` ×2 (package-feed / sample pins). The baseline MUST record these so they
are not attributed to this refactor.

**Surface baselines**: `readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt` (covers `SkiaViewer.fs`
**and** `Host/OpenGl.fs` — same assembly) and `readiness/surface-baselines/FS.GG.UI.Scene.txt`.
Regenerate-and-diff via `scripts/refresh-surface-baselines.fsx`; the diff MUST be empty.

---

## Open questions resolved

| Was it unknown? | Resolution |
|---|---|
| Can we split without surface change? | Yes — internal files (no `.fsi`) + public delegators (R1). |
| Are the window runners true duplicates? | No — shared scaffold + preserved specializations (R2). |
| Where do new files sit in compile order? | Before the public `.fs`; specific slots in R1. |
| Does the codec table change wire bytes? | No — same order/width/endianness; round-trip corpus is the oracle (R4). |
| Do we need the §7 gates? | No — behavior-preserving phase; existing suites + baseline are the oracle (spec Assumptions, R5). |

No `NEEDS CLARIFICATION` remain.
