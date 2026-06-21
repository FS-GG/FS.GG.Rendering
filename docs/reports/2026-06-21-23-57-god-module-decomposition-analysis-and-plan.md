# God-Module Decomposition — Analysis & Implementation Plan (constraints relaxed)

**Date:** 2026-06-21 23:57 CEST
**Scope:** Whole repository, focused on the largest remaining god-modules/functions in `src/` and `tools/Rendering.Harness/`.
**Method:** Four parallel deep-read passes (Harness, Controls, Scene+SkiaViewer, Testing) cross-validated against the current tree; quantitative signals via `wc`/`grep`; targeted reads of the largest files. Current line numbers re-confirmed 2026-06-21.
**Status:** Analysis + plan only. No code changed by this report.

> **Constraint context — this is the key difference from prior plans.** The maintainer has agreed to **relax all four freezes** that blocked feature 182's god-module work:
> 1. Public `.fsi` / assembly surface freeze
> 2. Byte-stable render hot-path output (zero-alloc / byte-identical frames)
> 3. Byte-identical evidence/readiness artifacts
> 4. Strict no-behavior-change
>
> This unlocks the three modules feature 182 deliberately **retained** under FR-009 (`Control.fs`/`ControlInternals`, `Scene.fs`, `RetainedRender.step`) and permits the aggressive harness data-table refactor that was never attempted. The price of relaxing #2 specifically is that we **must** stand up replacement gates (golden-image equivalence + a perf budget) — see §7. "Relaxed" does not mean "unverified."

---

## 1. Where the campaign left things

Features 177–184 executed most of the 2026-06-21 05:19 code-health plan. For accuracy, here is the **current** state, not the pre-campaign state:

**Done (do not re-propose):**
- Harness **relocated** `tests/ → tools/Rendering.Harness/` (commit 3240892).
- Shared `ReadinessStatus` vocabulary unified (feature 180); ~66 parallel DU cases collapsed.
- Shared test/util helpers — repo-root finder, FNV-1a basis, `clamp` (feature 178).
- `Testing.fs` (4,629 lines) split into 6 domain files (feature 182 US4).
- `Viewer.Types` extracted from `SkiaViewer.fs` (feature 182 US1).
- `ControlsElmish.runInteractiveAppWithLauncher` → `FrameLoopState` record (feature 182 US6).
- Type-safety hardening + backcompat-shim removal (features 183–184).

**Deferred under FR-009 (the byte-stability freeze) — now unblocked:**
- `Control.fs` / `ControlInternals` — flat 3,010-line module; retained because subset extraction caused back-edges and the chart-preamble hoist was "not provably byte-stable."
- `Scene.fs` — retained because it is the dependency-free root (17 consumers), with a namespace-type resolution hazard and a behavior-affecting inspection dedup (FR-006).
- `RetainedRender.step` — retained because ~18 accumulators entangle with ~15 derived locals across 8 walks on the hot path.

**Never touched by the campaign (no surface constraint, highest untapped volume):**
- `tools/Rendering.Harness/Compositor.fs` — **5,512 lines**, **86** `renderFeature*` functions, **722** `feature1NN` constant references, **110** `*ReadinessDirectory` constants.
- `tools/Rendering.Harness/Cli.fs` — **3,928 lines**, **26** per-feature `runFeature*Cmd` handlers.

The campaign focused on `src/`; the harness is a CLI tool with no `.fsi` surface, so it was out of scope. It is now the single largest concentration of mechanical duplication in the repo.

---

## 2. Current god-module/function inventory

| Rank | Location | Current size | Nature | Prior status |
|---|---|---|---|---|
| 1 | `RetainedRender.step` (`src/Controls/RetainedRender.fs`) | **~645-line fn**, 15+ mutable accumulators, 8 walks | Entire frame lifecycle in one fn | Retained (FR-009, hot path) |
| 2 | `Compositor.fs` (`tools/Rendering.Harness/`) | **5,512 lines** | 86 `renderFeature*` + 6 per-feature state machines + 722 const refs | Never touched |
| 3 | `Control.fs` / `ControlInternals` | **3,010-line module** / 3,513-line file | `hashScene` (381 lines), `faithfulContent` (168, 60+ branches), 30 control-helper modules | Retained (FR-009) |
| 4 | `Cli.fs` (`tools/Rendering.Harness/`) | **3,928 lines** | 26 near-duplicate per-feature commands | Never touched |
| 5 | `SkiaViewer.fs` / `Viewer` | **~3,290-line module** | `runPresentedPersistentWindow` (323), `runPersistentWindow` (235, near-dup), `update` (139), `runBounded` (140) | Types-only split done |
| 6 | `ControlsElmish.runScriptCore` | **~365-line fn**, 20+ mutable vars | Frame dispatch + state + metrics | Sibling fn refactored; this one untouched |
| 7 | `SceneCodec.fs` | **1,571 lines** | 54 write/read pairs; each node type appears 3× | Some 183 work |
| 8 | `Scene.fs` | **2,084 lines** | `diagnostics` (54), `describe` (31), glyph-builder trio (~60% shared), 4 inspection modules in-file | Retained (FR-009) |
| 9 | `OpenGl.fs` / `GlHost.run` | **~295-line fn** in 1,200-line module | GL lifecycle + events + effects + screenshots | Never touched |
| 10 | `ValidationLanes.runLane` | **154-line fn** | Process + timeout + output + result in one fn | Never touched |

---

## 3. The five clean architectures (the same medicine recurs)

Almost everything above is curable by five patterns. The same patterns recur because the same two diseases recur: **giant `match` dispatch** and **one function owning a whole lifecycle**.

### Pattern A — Registry / dispatch-table
Replace a giant `match kind with …` / `match tag with …` by a `Map`/array of handlers populated once; for codecs, one record per case holds *both* write and read so symmetry is structurally enforced.
- **Applies to:** `faithfulContent` (60+ kind branches), `hashScene` (25-case node walk), `SceneCodec` (each node type written, read-via-table, and tested — 3×), `Compositor.renderPackageValidation`/`renderRegressionValidation` (feature-number matches), the 30 trivial per-control-type modules in `Control.fs`.
- **Buys:** single source of truth per case; compiler/structural enforcement of encode/decode symmetry (the highest-risk silent-drift point in the repo); functions shrink to a lookup; new control-kind/scene-node becomes a one-site change instead of editing ~10 disjoint switches.

### Pattern B — Pipeline / staged decomposition
Break a lifecycle mega-function into explicit ordered stages, each a named function with an explicit input/output record: `diff → layout/dirty-set → paint/reuse → assemble`.
- **Applies to:** `RetainedRender.step`, `ControlsElmish.runScriptCore`, `Viewer.runPresentedPersistentWindow`, `GlHost.run`.
- **Buys:** each stage independently testable; data flow becomes visible instead of threaded through hidden mutables; bugs localize to one ~80-line stage.
- **Cost (now payable):** on hot paths this can change allocation/output — which is exactly why 182 stopped. Requires the §7 golden-image + perf gates.

### Pattern C — State-record extraction
Collapse scattered `let mutable` into one record (mutable fields), plus a `…Builder` for the repeated metrics tuple.
- **Applies to:** `runScriptCore` (20+ mutables), `step` (15+), `GlHost.run` (12+), and the 4–5× duplicated 30-field `FrameMetrics` tuple in both `RetainedRender` and `ControlsElmish`.
- **Buys:** lowest-risk, repo-proven (182 did this for `FrameLoopState`); makes state explicit; **prerequisite** for Pattern B.

### Pattern D — Workflow-template / parametric command
Replace per-feature copy-forward *code* with per-feature *data* (a descriptor record) + one generic runner.
- **Applies to:** the harness — `Cli`'s 26 `runFeature*Cmd`, `Compositor`'s 86 `renderFeature*` + 722 constants + 110 directory constants.
- **Buys:** collapses thousands of lines; new feature = one data row; kills the `match featureNum with 156 | 157 | …` dispatch duplicated across both files. **No `.fsi` constraint here — safest place to be aggressive, highest line-count payoff.**

### Pattern E — Module-by-responsibility split
Plain extraction into files along concern lines.
- **Applies to:** `Compositor` (→ Types / Config / per-feature State / Render), `SkiaViewer.Viewer` (→ InputQueue / Responsiveness / Evidence / Window), `Scene` (→ extract `Text.Shaping`: the `buildGlyphRun`/`buildFallbackShapedText`/`glyphRunDataFromShapedText` trio shares ~60% code; move the 4 inspection modules to files), `OpenGl.GlHost` (→ Rendering / Input / Damage / Effects), `SceneCodec` (→ Primitives / Paint / Path / Text / Scene / Package).
- **Cost:** F# file/module ordering and `Scene.fs`'s root position create back-edge / namespace-resolution hazards — manageable now that surface can change (we can re-home types).

---

## 4. Decomposition designs — the hard three

### 4.1 `RetainedRender.step` (Patterns C → B)

**Today:** one ~645-line function holds: diff, layout-dirty-set, per-frame text-cache install, incremental layout, theme-change invalidation, id minting, reconciliation walk (`recurse` with Keep/Replace/Update + ChildKeep/Move/Insert/Remove), fragment-reuse decisions, animation sampling, picture-cache update, and 15+ metrics accumulators — plus `init` (~97 lines) duplicates its build/paint scaffolding.

**Target:**
```
type FrameState =                      // Pattern C: replaces 15+ let mutable
  { mutable NextId: uint64; mutable Recomputed: int; mutable Shifted: int
    mutable Memo: MemoCache; RepaintedBoxes: ResizeArray<Rect>; … }

type FrameMetricsBuilder = { … }       // kills the 4–5× duplicated 30-field tuple
  member Build : unit -> FrameMetrics

module DiffStage     // diff prev/next
module LayoutStage   // dirty-set + incremental layout (independently testable!)
module PaintStage    // reuse decisions + picture/text/memo cache updates
module AssemblyStage // build render result + metrics
let step = DiffStage >> LayoutStage >> PaintStage >> AssemblyStage  // threading FrameState
```
`init` becomes `LayoutStage(full) >> PaintStage(seed) >> AssemblyStage`, sharing the same stage bodies instead of a parallel copy.

**Why it needs relaxed #2:** stage boundaries materialize intermediate records; that can change allocation counts and (via float accumulation order) frame bytes. Gate with golden-image equivalence + a per-frame alloc/time budget (§7).

### 4.2 `Control.fs` / `ControlInternals` (Patterns A + E)

**Today:** flat 3,010-line module. Worst functions: `hashScene` (381 lines, 42 inline mixer closures + 25-case `goNode`), `faithfulContent` (168 lines, 60+ kind branches each calling a `*Geom`), plus 30 near-identical trivial modules (`TextBlock`/`Label`/`Button`/…) at the file tail and ~40 `*Geom` chart/widget functions sharing a `match pts with [] -> emptyState` preamble (×17).

**Target:**
```
ControlInternals.SceneHash      // hashScene → a SceneHasher with Mix* methods over SceneNode (Pattern A visitor)
ControlInternals.ContentRender  // faithfulContent → registry: Map<Kind, Theme->Rect->Control->Scene list>
ControlInternals.ChartGeometry  // line/bar/pie/scatter/graph *Geom (+ a `withPoints` combinator for the ×17 preamble)
ControlInternals.WidgetGeometry // button/toggle/switch/checkbox/slider/tabs/… *Geom
ControlInternals.LayoutEval     // toLayout / evaluateLayout
ControlInternals.NodeAssembly   // paintNode / paintLeaf / renderScene
Control.Helpers                 // data-driven replacement for the 30 trivial create/text modules
```
The kind registry also feeds the **6 parallel `match …Kind` sites** confirmed across `Control.fs` (×2), `ControlRuntime.fs`, `Catalog.fs`, `Inspection.fs`, `RetainedRender.fs` — one table restores exhaustiveness.

**Why it needs relaxed #1/#2:** moving `ControlInternals` members across modules changes internal module layout (and, where any leak to the public surface, `.fsi`); the hash visitor reorders nothing but must be proven byte-equal on golden hashes.

### 4.3 `Scene.fs` (Pattern E + finishing FR-006)

**Today:** 2,084 lines — a ~767-line type wall, then `Colors`/`Paint`/`Path`/`Scene` builders, the glyph-builder trio (`buildGlyphRun` 73, `buildFallbackShapedText` 40, `glyphRunDataFromShapedText` 34; ~60% shared), `describe`/`diagnostics` tree-walks, and four inspection/evidence modules (`SceneEvidence`, `LayoutEvidence`, `VisualInspection`, `RetainedInspection`) in-file. The module-level `mutable realTextMeasurer` side-channel lives here too.

**Target:**
```
Scene.Types            // the ~767-line primitive/SceneNode block (own file)
Scene  (builders)      // empty/group/rectangle/… stay as the root
Text.Shaping           // glyph trio unified behind one parameterized builder + fingerprint; owns realTextMeasurer seam
Scene.Inspection       // VisualInspection + RetainedInspection (finish the started cleanToken/duplicateIds/finding dedup — FR-006)
Scene.Evidence         // SceneEvidence + LayoutEvidence
```

**Why it needs relaxed #1/#3/#4:** `Scene` is the dependency-free root with 17 consumers; splitting it re-homes public types (surface change), and finishing the FR-006 inspection dedup is **behavior-affecting** (it changes which duplicate findings are emitted) — only permissible now that #4 is relaxed. Validate evidence artifacts for semantic, not byte, equivalence.

---

## 5. Decomposition design — the harness (Patterns D + E, highest volume, lowest risk)

This is the biggest line-count win and has **no surface constraint**.

**`FeatureDescriptor` (the data):**
```
type FeatureDescriptor =
  { Id: int; Slug: string
    Directories: FeatureDirectories          // readiness/parity/timing/scenarios — replaces 110 constants
    RequiredHeaders: string list
    Thresholds: FeatureThresholds
    Renderers: FeatureRenderHooks }          // override points for the ~20% that genuinely differ
let descriptors : FeatureDescriptor list = [ … 156..161 … ]
```

**`Compositor.fs` → split + parametrize:**
- `Compositor.Types` — the ~60 type defs.
- `Compositor.Config` — the descriptor list (absorbs the 722 `feature1NN` refs + 110 directory constants).
- `Compositor.FeatureState` — one parametric `init`/`update`/`status` over a descriptor (replaces the 6 per-feature state machines).
- `Compositor.Render` — one generic renderer + the per-feature `Renderers` hooks (replaces 86 `renderFeature*`; the `renderPackageValidation`/`renderRegressionValidation` feature-number matches become descriptor lookups — Pattern A).

**`Cli.fs` → command table:**
- One `runReadiness descriptor` workflow (`probe → mkdirs → build reports → write N files → render`) replaces the 26 `runFeature*Cmd`.
- `runLane` (in `ValidationLanes.fs`, 154 lines) splits into `ProcessRunner` + `TimeoutManager` + `OutputBuffer` + a thin orchestrator.

Expected reduction: the largest in the repo. New compositor feature = add a `FeatureDescriptor` row.

---

## 6. Implementation plan

Each phase is an independent PR, builds + tests green, and (where it touches rendering/evidence) passes the §7 gates. Ordered **safest-and-highest-volume first**, structural-hot-path last.

### Phase 1 — Harness data-table refactor (Patterns D+E) — *highest payoff, lowest risk*
No `.fsi` surface; relaxed #3 covers any incidental artifact wording change.
1. Introduce `FeatureDescriptor` + `descriptors` list.
2. Split `Compositor.fs` → Types / Config / FeatureState / Render; parametrize the 86 renderers and 6 state machines; convert the two feature-number-dispatch renderers to descriptor lookups.
3. Collapse `Cli.fs`'s 26 commands → one `runReadiness` workflow + command table.
4. Split `ValidationLanes.runLane` into ProcessRunner / TimeoutManager / OutputBuffer / orchestrator.
*Exit:* harness < ~1,500 lines/file; new feature = one data row; readiness artifacts diffed for **semantic** equivalence.

### Phase 2 — Cross-cutting dedup + state records (Pattern C) — *low risk, enables Phase 4*
1. `FrameMetricsBuilder` — eliminate the 4–5× duplicated 30-field tuple in `RetainedRender` + `ControlsElmish`.
2. `FrameState` / `FrameScriptState` records — collapse the 15+/20+ mutables in `step` and `runScriptCore` (no behavior change yet; pure state-shape refactor, byte-identical by construction like 182's `FrameLoopState`).
3. Generic validation orchestrator unifying the ~95 duplicated lines between `VisualInspectionValidation` and `RetainedInspectionValidation`; `ManagedSection<'T>` for the duplicated markdown section-update logic.
*Exit:* mutable state is explicit and named; metrics tuple defined once.

### Phase 3 — SkiaViewer + OpenGl + SceneCodec module splits (Pattern E + A) — *medium risk*
1. `SkiaViewer.Viewer` → InputQueue / Responsiveness / Evidence / Window; unify `runPresentedPersistentWindow`/`runPersistentWindow` behind one lifecycle scaffold.
2. `GlHost` → Rendering / Input / Damage / Effects (extract `run`'s `interpretEffect`/screenshot closures).
3. `SceneCodec.fs` → Primitives / Paint / Path / Text / Scene / Package; convert `writeSceneNode`/`readSceneNode` to a per-case `NodeCodec` table (Pattern A) so additions are compiler-enforced.
*Exit:* no harness/viewer/codec file > ~1,500 lines; codec symmetry structurally enforced.

### Phase 4 — `Scene.fs` split (Pattern E, finish FR-006) — *medium risk, surface-changing*
1. Extract `Scene.Types`, `Text.Shaping` (unify glyph trio + isolate `realTextMeasurer`), `Scene.Inspection` (finish FR-006 dedup), `Scene.Evidence`.
2. Re-home public types as needed; update the 17 consumers + `.fsi` baselines + templates.
*Exit:* `Scene.fs` is builders-only; inspection dedup complete; evidence artifacts semantically validated.

### Phase 5 — `Control.fs` split (Patterns A+E) — *medium-high risk*
1. Extract `SceneHash`, `ContentRender` (kind registry), `ChartGeometry`+`withPoints`, `WidgetGeometry`, `LayoutEval`, `NodeAssembly`, `Control.Helpers`.
2. Route the 6 parallel `match …Kind` sites through the registry.
*Exit:* `ControlInternals` decomposed; adding a control kind is one-site, compiler-checked; golden hashes/images proven equal.

### Phase 6 — `RetainedRender.step` pipeline (Pattern B) — *highest risk, do last*
1. With `FrameState` (Phase 2) in place, extract `DiffStage`/`LayoutStage`/`PaintStage`/`AssemblyStage`; converge `init` onto the shared stages.
*Exit:* `step` is a stage composition; each stage unit-tested; per-frame alloc/time within budget; golden-image equivalent.

---

## 7. Replacement gates (mandatory once #2 is relaxed)

Relaxing byte-stability removes the cheap regression check 182 relied on. Before Phases 5–6 (and ideally before any rendering-path change) stand up:

1. **Golden-image equivalence harness** — render the showcase/gallery scene corpus and compare against committed PNGs with a perceptual tolerance (e.g. per-pixel ΔE threshold + max-diff-pixel count), not byte-equality. Failing diffs surface as artifacts.
2. **Golden-hash review gate** — `hashScene`/fingerprint outputs may legitimately change when computation reorders; require an explicit "hashes changed, reviewed" step rather than a hard equality assert.
3. **Per-frame perf budget** — extend the existing host-performance / responsiveness lanes (features 160/161/167/173) with an allocation-count and frame-time ceiling per scenario, failing CI on regression beyond a margin.
4. **Semantic artifact diff** — for readiness/evidence markdown+JSON, compare parsed structure (status, counts, headers) rather than bytes, so harmless wording/ordering changes don't block.

Without these, "constraints relaxed" becomes "regressions undetected." Phase 1 (harness) and Phase 2 (state records) don't need them; everything touching the render path does.

---

## 8. Risk & sequencing rationale

- **Phase 1** is the highest line-count win and carries the least risk (tool, no surface, no hot path) — do it first regardless of appetite for the rest.
- **Phase 2** is byte-identical-by-construction and is the **prerequisite** for the Phase 6 pipeline split — cheap insurance.
- **Phases 3–4** are conventional module splits; the F# ordering/root hazards are real but tractable now that types can be re-homed.
- **Phases 5–6** are the genuine hot-path structural work and must sit behind the §7 gates. Phase 6 (`step`) is last because it is the single riskiest change in the codebase.
- The §7 gates should be built **early** (alongside Phase 1) so they're ready when the render-path phases land.

**F#-specific hazards to watch:** module/file declaration order (a split that creates a back-edge won't compile); `Scene` as the dependency-free root (re-homing its types ripples to 17 consumers + templates + baselines); generic-over-`'msg` registries fighting type inference (mitigate with explicit annotations or tag-indexed arrays on hot paths); and `.fsi` baseline + template + surface-baseline regeneration on every surface-changing phase.

---

*Generated 2026-06-21 from a four-pass parallel deep-read. File/line references reflect the tree at that time and should be re-confirmed before editing. Supersedes the structural-split portions of the 2026-06-21 05:19 code-health plan, whose duplication-removal and shared-vocabulary phases are now shipped (features 177–184).*
