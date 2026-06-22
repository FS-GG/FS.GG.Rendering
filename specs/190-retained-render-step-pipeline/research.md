# Phase 0 Research: `RetainedRender.step` Pipeline Decomposition

All spec **Assumptions** are accepted as written (campaign defaults); the open questions they leave are
the stage carving, the file/module placement that meets SC-001 without a back-edge, and the regression
posture. Each is resolved below as Decision / Rationale / Alternatives, grounded in the actual code.

---

## R1 — Stage boundaries: where does `step` cut into four stages?

**Decision.** Four named internal stages, cut on the existing `retained-step-*` trace seams:

| Stage | Source span (`RetainedRender.fs`) | Owns | Trace spans inside |
|---|---|---|---|
| **diffStage** | 1507–1522 | `Reconcile.diff` → `result`; `layoutDirtySet` → `dirty`; `invalidated` | `retained-step-diff`, `retained-step-layout-dirty-set` |
| **layoutStage** | 1524–1593 | seed `FrameState` from `prev`; install text-measure hook (`measureCached`); `evaluateLayoutIncremental` → `root/boundsById/layoutResult`; `remeasured`; `themeChanged` | `retained-step-layout-incremental` |
| **paintStage** | 1607–1809 | `mint`/`metadataFor`/`paintOwn`/`paintFresh`/`buildFresh`/`carry`/`build`; `newRoot = build …`; clear text-measure hook | `retained-build-paint-own`, `retained-step-build` |
| **assemblyStage** | 1811–2115 | `countVirtual`; damage-reduce; `walkPictures` + replay/avoided-work; `collectOffscreen`; `indexPriorOwn`+`collect` (clocks); scene `assemble`; render result; `WorkReductionRecord` + `RetainedRenderStep` | `retained-step-count-virtual`, `retained-step-damage-reduce`, `retained-step-picture-walk`, `retained-step-offscreen-diagnostics`, `retained-step-index-prior-own`, `retained-step-state-collect`, `retained-step-scene-assembly`, `retained-step-render-result`, `retained-step-work-node-count` |

`step` becomes: `diffStage` → `layoutStage` → `paintStage` → `assemblyStage`, threading the mutable
`FrameState` and an explicit `FrameContext` (see data-model.md), with the text-measure-hook lifetime
managed by the orchestrator across layout+paint (see R4).

**Rationale.** These nine+ spans are already the author-chosen seams; the spec's "stage boundaries
follow the existing trace seams" assumption maps them onto exactly the four target concerns. Each cut
is at a point where the data crossing the seam is already a named local (`result`, `dirty`, `root`/
`boundsById`/`layoutResult`/`themeChanged`, `newRoot`), so no *new* intermediate needs to be
materialized except the `FrameContext` carrier — keeping accumulation order byte-identical (FR-005).

**Alternatives considered.** (a) Finer-grained one-stage-per-trace-span: rejected — over-fragments,
multiplies FrameContext threading, and the spec fixes the count at four. (b) Merge diff into layout:
rejected — diff is genuinely independent (its `result.Patch`/`result.Diagnostics` are pure inputs to
layout and assembly) and is the cleanest unit to test in isolation.

---

## R2 — Closures → explicit parameters: how do the stage bodies stop capturing per-frame state?

**Decision.** The nested closures in today's `step` (`mint`, `paintOwn`, `paintFresh`, `buildFresh`,
`carry`, `build`, `measureCached`, `countVirtual`, `walkPictures`, `collectOffscreen`, `indexPriorOwn`,
`collect`, `assemble`) currently capture `st: FrameState`, `theme`, `boundsById`, `prev`,
`themeChanged`, `size`. Each stage function takes those as **explicit parameters** (bundled as the
`FrameContext` input record + the threaded `FrameState`), and the recursion helpers become local
`let rec` inside their owning stage (paintStage owns `build`/`carry`/`buildFresh`; assemblyStage owns
the read-only walks). No stage reads hidden mutable state beyond the threaded `FrameState` (FR-002).

**Rationale.** The closures already *are* the stage bodies; lifting their captured environment into
parameters is a mechanical, behavior-preserving transform. Because the same `FrameState` instance is
**threaded** (not copied) and the operations run in the same order, the integer/float accumulation and
the allocation profile are byte-identical by construction (the central byte-identity claim, FR-005).

**Alternatives considered.** Passing `FrameState` immutably (returning a new record per mutation):
rejected — it would change the allocation profile (the very risk §7 warns about) and fights the
hot-path `mutable` the constitution explicitly blesses; the record stays `mutable`-field and is
threaded by reference.

---

## R3 — File/module placement to meet SC-001 (≤≈1,500 lines) WITHOUT a back-edge (FR-009)

**Decision (recommended, pending the R6 compile probe).** Keep the four stages **inside**
`module internal RetainedRender` in `RetainedRender.fs` (so every in-module helper stays in scope and
no producer→consumer back-edge is created). Meet the size target by relocating the **step-independent**
policy cluster to a new `src/Controls/Internal/CompositorPolicy.fs` compiled *before*
`RetainedRender.fs`:

- feature-159 family: `feature159ReasonToken`/`PromotionStatusToken`/`ReuseStatusToken`,
  `feature159ContentIdentity`/`PlacementIdentity`/`ClassifyReuse`/`EvaluatePromotion`/`CountersFromWork`
  and their private helpers (`feature159Hash`, `rectToken`, `zeroFeature159Counters`,
  `feature159ReuseDecision`);
- feature-147 damage/compositor: `unionArea`, `rectOfDamage`, `damageOfRect`, `damageRegionSet`,
  `placementDamage`, `classifyDamageFallback`, `DamageSetInputs`, `PromotionInputs`,
  `promotionDecision`, `snapshotVerdict`.

These (~lines 600–1290, already pure and already `val internal`) reference **none** of the step
pipeline, so moving them earlier in compile order is safe. Their associated `type internal` DUs/records
(`CompositorDamageRegion`…`Feature159*`, `SnapshotResourceVerdict`, etc.) are namespace-level types in
`namespace FS.GG.UI.Controls`, so re-homing them to the same namespace keeps every **unqualified**
consumer reference resolving unchanged.

**Cost & mitigation.** Internal *test* call sites that qualify these as `RetainedRender.promotionDecision`
change to `CompositorPolicy.promotionDecision` (a rename, surface unchanged, no bump). If the spec's
"names preserved on move" intent is read strictly enough to avoid even that internal churn, the fallback
(R6) keeps the stages in-file and accepts `RetainedRender.fs` staying >1,500 by re-homing only the
*types* — but the recommended path is the clean Pattern-E relocation, consistent with 187/188/189.

**Rationale.** This is the lowest-risk way to satisfy SC-001: it moves code that is *already*
independent and *already* tested, touching nothing on the hot path. The high-risk stage extraction
stays in one file where the helper graph is intact.

**Alternatives considered.** (a) Move the stage **bodies** to `Internal/FramePipeline.fs`: rejected as
primary — requires re-homing the entire retained type family *and* all step-only helpers to break the
back-edge, a far larger, riskier ripple for no extra benefit over relocating the independent cluster.
Recorded as the R6 fallback only if the compile probe shows the in-file stages cannot meet the
≤250-line per-stage sub-goal. (b) Do nothing about size, accept >1,500: rejected — violates SC-001.

---

## R4 — Text-measure hook lifetime across the layout/paint seam

**Decision.** The `ControlInternals.setMeasureTextHook (Some measureCached)` install (today at line
1579, before incremental layout) and the `setMeasureTextHook None` clear (line 1809, after `build`)
straddle **both** layoutStage and paintStage. The **orchestrator** (`step`) owns the hook lifetime: it
builds `measureCached` over the threaded `FrameState`/`frameStartTextKeys`, installs it before calling
layoutStage, and clears it after paintStage returns — in a shape that still always clears on the total
(never-throwing) path. The stages themselves do not touch the global hook (no hidden cross-stage
coupling); they simply measure through `ControlInternals`, which reads the installed hook.

**Rationale.** The hook is a `ThreadStatic` global read by `evaluateLayoutIncremental` and `paintNode`;
making either stage responsible for install/clear would be the exact "hidden mutable state outside the
threaded record" FR-002 forbids. Centralizing it in the orchestrator keeps the seam honest and the
clear unconditional (preserving the totality guarantee).

**Alternatives considered.** layoutStage installs / paintStage clears: rejected — couples two stages
through a global and breaks paintStage's independent testability (FR-003).

---

## R5 — Regression posture / gate (the §7 question)

**Decision.** Adopt the spec's **campaign-default** posture: target byte-identical scene/hash output by
construction; treat the primary gate as **(1)** byte-identity of emitted scenes + `hashScene`
fingerprints over the existing scene corpus (golden-hash zero-delta, with a reviewed-and-recorded delta
the only accepted alternative), **(2)** the existing per-frame perf/responsiveness lanes
(features 160/161/167/173) within the agreed margin, and **(3)** existing visual-inspection evidence.
A standalone perceptual golden-**image** harness is a *fallback*, stood up only if a stage boundary
genuinely breaks byte-identity. The gate must **demonstrably catch an injected regression** (FR-015/
SC-008): a deliberately perturbed `step` (e.g. a reordered accumulation or a dropped damage box) makes
the gate go red; the real decomposition makes it green.

**Rationale.** Phases 185–189 passed on exactly this posture (byte-identity + golden-hash + existing
lanes) without a separate image corpus; the `FrameState` prerequisite (186) makes byte-identity the
expected outcome here too. Standing up a perceptual harness unconditionally is scope the spec flags as
an *optional* expansion — not adopted absent a real byte break.

**Alternatives considered.** Build the perceptual golden-image harness up front (spec's noted
expansion): deferred — recorded as the contingency if R5(1) fails, to avoid pre-building unused
infrastructure (FR-016 "no added indirection that doesn't net a reduction" applied to tooling).

---

## R6 — Compile probe (the design-time confirmation the spec mandates)

**Decision.** Before any US1 stage edit, run a **full-solution compile probe** that (a) adds the
`val internal` stage signatures + `type internal FrameState`/`FrameContext` to `RetainedRender.fsi` and
stub bodies, and (b) creates `Internal/CompositorPolicy.fs` with the relocated cluster, then
`dotnet build`s the whole solution to prove **no back-edge** and that the retained type family resolves
in the new compile order. The probe's result *fixes* the stage grouping and the relocation set; if it
reveals a back-edge or a >250-line residual stage, fall back to the R3(a) type-re-home variant for the
offending stage only. This probe is the first Foundational task in `tasks.md`.

**Rationale.** The spec states the precise grouping is "fixed at design time and confirmed by a
full-solution compile probe before US1 edits." F# file/module ordering errors are compile-time, so a
cheap stub-compile de-risks the whole phase before the expensive byte-identity work begins.

---

## Resolved unknowns

| Spec marker | Resolution |
|---|---|
| §7 regression-posture question | R5 — campaign-default (byte-identity + golden-hash + existing lanes); image harness is fallback. |
| "precise stage grouping fixed at design time" | R1 + R6 — four stages on the trace seams, confirmed by the compile probe. |
| US2 init convergence go/no-go | Decided empirically in implementation: converge `init` onto the shared paint/assembly stages **iff** it nets a real line/duplication reduction; else drop per FR-007/FR-016 (carry-forward lesson 180 SC-005 / 189 US4). |
| File-size strategy for SC-001 | R3 — relocate the step-independent feature-159/147 policy cluster to `Internal/CompositorPolicy.fs`. |

No NEEDS CLARIFICATION markers remain.
