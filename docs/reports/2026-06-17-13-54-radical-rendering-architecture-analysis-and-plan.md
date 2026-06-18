# Radical Rendering-Architecture Analysis & Implementation Plan

| | |
|---|---|
| **Authored** | 2026-06-17 13:54 CEST (2026-06-17T11:54Z) |
| **Author** | Claude Opus 4.8 (1M context), with four parallel research agents (offline codebase map + 3 online prior-art deep-dives) |
| **Repo state** | Originally analyzed at branch `main` @ `8f75594`; implementation/package status audited through Feature 154 squash merge `2c9af24` and the post-merge source package bump to `0.1.17-preview.1` |
| **Scope** | The **radical** framework options only (per request), grounded in offline code reading and online prior art (React Fiber, Jetpack Compose, SwiftUI/AttributeGraph, Flutter, Elm, WebRender, Chromium `cc`, Skia, HarfBuzz) |
| **Status** | Current as of local validation on 2026-06-18: P0-P6 are implemented and landed. P7 is implemented through Feature 149 and extended by Feature 152, Feature 153, Feature 154, and local Feature 155. Feature 152 added explicit three-run live proof-set acceptance, consumer compositor-readiness helpers, Feature152 harness routing, focused test coverage, readiness evidence, and package/surface validation; it was squash-merged to `main` as `8ea61c4`, then packable source projects were bumped to `0.1.15-preview.1` and the template package to `0.1.9-preview.1` in `61d1ce8`. Feature 153 implements the proof-interpreter slice: selected-attempt proof-set identity, pure live-host readiness classification, Feature153 harness routing/readiness renderers, focused tests, FSI transcript coverage, surface-baseline refresh, and durable readiness evidence under `specs/153-compositor-proof-interpreter/readiness/`; it was squash-merged to `main` as `d7c539c`, and the follow-up package bump raises source packages to `0.1.16-preview.1` and the template package to `0.1.10-preview.1`. Feature 154 implements the current proof-acceptance closeout: exact proof-set acceptance, same-profile parity, timing-decision evidence, final readiness/compatibility/package/regression artifacts, FSI transcript coverage, and focused tests across SkiaViewer, Rendering.Harness, Controls, Elmish, Testing, and Package; it was squash-merged to `main` as `2c9af24` and pushed to `origin/main`. The local feature branch was deleted, no stale remote feature branch was present, and the post-merge source package bump raises packages to `0.1.17-preview.1`. Focused Feature154 filters pass, broad `dotnet test FS.GG.Rendering.slnx --no-restore` passes locally, and unsupported-host quickstart exits in approximately 0.6s with zero accepted partial-redraw artifacts. Root `./fake.sh` remains absent, so Fake wrapper package targets are recorded as tooling-limited here. Feature 155 accepts P7 live partial-redraw correctness for current stable host profile `probe-08a47c01` with three fresh sentinel/damage attempts and same-profile parity; unsupported hosts still fail closed, timing remains inconclusive, and no compositor performance claim is accepted. Feature 150 implements the first P8/R3b intrinsic-layout slice and was squash-merged to `main` as `acad00d`. Feature 151 completes the remaining P8 acceptance package and was squash-merged to `main` as `6f9d606`; P8 is accepted for the current public Feature150 protocol. |

---

## 0. How to read this

This is two documents in one: an **analysis** of where FS.GG.Rendering actually is (grounded in the live code, with `file:line` anchors), and a **comprehensive implementation plan** for the radical bets, sequenced into phases with change-sites, parity oracles, risks, and exit criteria. Sources for every external claim are in §16.

### Current status update (2026-06-18 06:13 CEST)

This report began as a plan from `main` @ `8f75594`. The repository has since shipped P0-P3, pushed package version `0.1.3-preview.1`, implemented Feature 142 / P4 on `142-harfbuzz-text-shaping`, implemented the Feature 143 / P5 pure overlay coordinator on `143-interaction-overlay-state`, implemented Feature 144 / P5 host/widget overlay integration on `144-overlay-host-widget-integration`, implemented and merged Feature 145 / P5 overlay visual proof, bumped packages to `0.1.8-preview.1`, implemented Feature 146 / P6 render-anywhere, squash-merged it to `main` as `c0f16ce`, bumped packages to `0.1.9-preview.1` in `d62b026`, implemented Feature 152 / P7 live-proof closeout as squash merge `8ea61c4` followed by package bump `61d1ce8`, implemented Feature 153 / P7 proof-interpreter as squash merge `d7c539c`, and implemented Feature 154 / P7 proof-acceptance closeout as squash merge `2c9af24` followed by source package bump `0.1.17-preview.1`:

| Phase | Status | Evidence |
|---|---|---|
| **P0 - Quick win** | Shipped as Feature 138. Layout attrs and the metrics fix are merged. | `5ae3dad` `Merge 138-layout-attrs-metrics-fix (squash)`; package bump `c1318ee`. |
| **P1 - Duplication reduction** | Shipped as Feature 139. The shared current-node assembly seam is merged. | `f92621d` `Merge 139-shared-assembly-extraction (squash)`; package bump `143342f`. |
| **P2 - IR foundation** | Shipped as Feature 140. Internal Controls composition, modifier classification, local z-order, portals/layers, legacy lowering, and glyph-run proof support are merged. | `ac2b560` `Merge 140-modifier-layer-ir (squash)`; package bump `41fb05c` to `0.1.3-preview.1`. |
| **P3 - Keystone** | Implemented as Feature 141. Retained fragments now store owner-produced assembly results and invalidation evidence; structural scene fingerprinting moved to the assembly-owner side and retained rendering aliases it. | `specs/141-retained-renderer-unification/readiness.md`; focused Feature141, Feature139, Feature140, public-surface, audit, solution build, non-Controls broad deterministic suites, surface-baseline refresh, and offscreen harness passed. |
| **P4 - Text** | Implemented as Feature 142. Scene now carries dependency-light shaped text evidence; SkiaViewer owns HarfBuzz provider install/status/shape/draw; retained text measurement cache keys include the provider version bucket; pure fallback remains available. | `specs/142-harfbuzz-text-shaping/readiness/validation-log.md`; `measure-draw-parity.md`; `fallback-diagnostics.md`; `cache-retained-parity.md`; `surface-baseline.md`; `package-surface.md`; `baseline-disclosure-ledger.md`. |
| **P5 - Interaction** | Implemented and landed through Feature 145. Feature 143 introduced the pure overlay coordinator; Feature 144 adds transient metadata for eight widget categories, pointer/focus/runtime/Elmish routing seams, product-visible open/focus/selection dispatch, AntShowcase product-owned date-picker evidence, deterministic overlay corpus parity, and unsupported-host disclosure; Feature 145 closes the remaining visual-proof caveat with real current-run open/closed artifacts correlated to the date-picker flow. | `specs/143-interaction-overlay-state/readiness/feature143-readiness.md`; `specs/144-overlay-host-widget-integration/readiness/README.md`; `specs/145-overlay-visual-proof/readiness/visual-proof.md`; Feature 145 focused Rendering.Harness and AntShowcase filters passed; three capable-host proof runs passed/closed with stable scenario and evidence labels; `b632d93` `Merge 145-overlay-visual-proof (squash)`; package bump `512c6b0` to `0.1.8-preview.1`. |
| **P6 - Render-anywhere** | Implemented as Feature 146. Scene now exposes a deterministic portable package codec and inspection surface; SkiaViewer exposes a reference rendering oracle; Testing exposes package-inspection assertions; the harness records reference PNG evidence and a browser feasibility fallback decision. | `src/Scene/SceneCodec.fsi`; `src/SkiaViewer/ReferenceRendering.fsi`; `src/Testing/Testing.fsi`; `tests/Rendering.Harness/RenderAnywhere.fsi`; `specs/146-render-anywhere-protocol/readiness/validation-summary.md`; `reference/summary.md`; `browser/browser-feasibility.md`; refreshed `readiness/surface-baselines/*`. |
| **P7 - Compositor** | Implemented through the current evidence/readiness scope, with current-host correctness accepted by Feature 155. Feature 147 is landed and provides proof/scissor/policy contracts plus deterministic readiness artifacts. Feature 148 adds the focused P7 evidence/readiness layer. Feature 149 completes the Spec Kit task checklist and is squash-merged as `a9a1ef1`, with first-class `--feature 149` CLI routing, corpus/readiness inventories, package FSI coverage, consumer compositor-readiness helper tests, focused Controls/SkiaViewer/Elmish/Harness/Package/Testing tests, generated readiness artifacts, and a final P7 validation package. Feature 152 adds the explicit three-run accepted proof-set contract, Feature152 harness routing, compositor readiness helpers, FSI/package coverage, readiness summary, compatibility ledger, and focused regression validation. Feature 153 adds the proof interpreter/readiness slice for exact selected attempts, live-host classification, Feature153 harness routes, FSI transcript coverage, focused validation, and readiness evidence. Feature 154 adds exact proof-acceptance, same-profile parity, timing-decision, final readiness, compatibility, package, regression, and transcript coverage; it is squash-merged as `2c9af24`, pushed to `origin/main`, and source packages are bumped to `0.1.17-preview.1`. Feature 155 produces three accepted fresh sentinel/damage proof attempts on stable host profile `probe-08a47c01`, loads them through `compositor-readiness --feature 155`, records same-profile parity accepted, keeps unsupported hosts fail-closed, and keeps performance `not-accepted`. | `specs/147-compositor-damage-redraw/readiness/validation-summary.md`; `specs/148-compositor-live-integration/tasks.md` (61/76 complete); `specs/149-complete-compositor-p7/tasks.md` (68/68 complete); `specs/149-complete-compositor-p7/readiness/validation-summary.md`; `specs/152-compositor-live-proof/tasks.md` (66/66 complete); `specs/152-compositor-live-proof/readiness/validation-summary.md`; `specs/153-compositor-proof-interpreter/tasks.md` (68/68 complete); `specs/153-compositor-proof-interpreter/readiness/validation-summary.md`; `specs/154-compositor-proof-acceptance/tasks.md` (70/70 complete); `specs/154-compositor-proof-acceptance/readiness/validation-summary.md`; `specs/155-native-proof-capture/readiness/validation-summary.md`; focused Feature155 filters passed for SkiaViewer, Rendering.Harness, and Package; broad solution validation passed on retry; unsupported-host quickstart passed with zero accepted artifacts; root `fake.sh` remains absent, so Fake wrapper package targets are recorded as a tooling limitation. |
| **P8 - Radical layout** | Feature 150 shipped the public constraints-down/sizes-up and intrinsic query/result substrate. Feature 151 completes the final P8 acceptance package on top of that substrate: representative layout and ScrollViewer corpus, measured/intrinsic dependency identity, stale-rejection classification, full/incremental parity, broad regression evidence, compatibility notes, package validation, and one reviewable readiness summary. | `specs/150-intrinsic-layout-protocol/readiness/validation-summary.md`; `specs/151-complete-p8-layout/tasks.md` (62/62 complete); `specs/151-complete-p8-layout/readiness/validation-summary.md`; squash merge `6f9d606`; focused and broad Feature151 tests passed for Layout.Tests, Controls.Tests, Testing.Tests, Package.Tests, Elmish.Tests, Rendering.Harness.Tests, and SkiaViewer.Tests; `dotnet test FS.GG.Rendering.slnx --no-restore` passed; source packages pack at `0.1.14-preview.1`; template package packs at `0.1.8-preview.1`. No new public `.fsi` surface delta is required. |

Feature 141 validation is recorded in `specs/141-retained-renderer-unification/readiness.md`: focused Feature 141 tests include 200 deterministic generated direct/cold/warm retained equivalence cases; Feature 139 and Feature 140 compatibility filters passed; retained/cache/fingerprint `Audit` filters passed; the full solution build passed; all non-Controls broad deterministic test projects passed; `scripts/refresh-surface-baselines.fsx` passed; and the offscreen harness wrote `artifacts/feature141-harness/T1/run.json` with `status: passed`. The remaining caveat is local validation scope: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091 --no-build` did not complete in the shell window and was interrupted after more than two minutes, so it is recorded as a limitation rather than a pass.

Feature 142 validation is recorded in `specs/142-harfbuzz-text-shaping/readiness/validation-log.md`: restore and full solution build passed; focused Scene, SkiaViewer, Controls, Rendering.Harness, and Elmish test projects passed; `scripts/refresh-surface-baselines.fsx` passed with additive `FS.GG.UI.Scene` and `FS.GG.UI.SkiaViewer` deltas. The package-readiness caveat is local checkout scope: no root `fake.sh` exists, and `Package.Tests` still depends on historical readiness artifacts such as `readiness/surface-baselines/*`, `scripts/controls-prelude.fsx`, and `specs/035-api-discovery-names/readiness/*`; this is recorded as a pre-existing package-readiness limitation rather than a Feature 142 shaping regression.

Feature 143 validation is recorded in `specs/143-interaction-overlay-state/readiness/validation-log.md`: restore/build passed; focused Feature143 Controls, Elmish, KeyboardInput, and Rendering.Harness tests passed; the AntShowcase Feature143 reference-flow test passed; Feature140/141/142/PublicSurface regression filters passed; `scripts/refresh-surface-baselines.fsx` passed with additive `FS.GG.UI.Controls` deltas.

Feature 144 validation is recorded in `specs/144-overlay-host-widget-integration/readiness/`: restore and solution build passed; focused Feature143/144 Controls, Elmish, KeyboardInput, Rendering.Harness, and AntShowcase date-picker tests passed; `scripts/refresh-surface-baselines.fsx` passed with additive `FS.GG.UI.Controls` deltas. Its remaining caveat was real visual proof scope; Feature 145 supersedes that caveat.

Feature 145 validation is recorded in `specs/145-overlay-visual-proof/readiness/`: the `overlay-visual-proof` harness command ran three equivalent capable-host attempts on X11 display `:1` with AMD Radeon GL renderer. Runs `20260617-203509-749`, `20260617-203538-828`, and `20260617-203538-994` all produced non-blank open/closed PNG artifacts, kept scenario id `feature144-antshowcase-date-picker-reference`, reported `passed`, and closed the Feature 144 visual-proof caveat. The unsupported-host path was separately exercised with display variables unset and reported `environment-limited` with cause `missing-display` and no accepted artifacts.

Feature 144 landing status: feature commit `983c6be` was squash-merged to `main` as `6297bfa` and pushed to `origin/main` on 2026-06-17. Packable projects were bumped from `0.1.6-preview.1` to `0.1.7-preview.1`, and `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local` completed successfully for the source packages.

Feature 145 landing/package status: Feature 145 was squash-merged to `main` as `b632d93`, then packable projects were bumped to `0.1.8-preview.1` in `512c6b0`. Feature 146 was implemented on `146-render-anywhere-protocol`, feature-committed as `99c511e`, squash-merged to `main` as `c0f16ce`, pushed to `origin/main`, then followed by mandatory package bump `d62b026` to `0.1.9-preview.1` and a second push. Feature 146 validation passed for solution build, focused Feature146 tests, package tests, surface tests, readiness FSI snippets, reference/browser harness commands, and `dotnet pack`; the full solution test requires Wayland to be disabled and currently remains blocked by unrelated Controls typed-lowering parity failures around `transientWidgetMetadata`.

Feature 148 landing/package status: Feature 148 was implemented as an evidence/readiness slice for P7 and squash-merged to `main` as `7d708c4`, then pushed to `origin/main`. It adds Feature148 harness constants and commands (`compositor-live-proof`, `compositor-parity --feature 148`, `compositor-reuse`, `compositor-snapshots`, `compositor-timing`, `compositor-readiness --feature 148`), focused Feature148 tests across Harness/Package/Controls/SkiaViewer/Elmish, readiness artifacts under `specs/148-compositor-live-integration/readiness/`, and updated Controls/SkiaViewer docs. Validation passed for all focused Feature148 filters, `dotnet build FS.GG.Rendering.slnx --no-restore`, `dotnet fsi scripts/refresh-surface-baselines.fsx`, source package pack at `0.1.11-preview.1`, and template pack at `0.1.5-preview.1`. The live proof artifact remains `environment-limited`; it does not claim partial-redraw acceptance.

Feature 149 landing/package status: Feature 149 was implemented as the final P7 evidence/readiness Spec Kit package and squash-merged to `main` as `a9a1ef1`, then pushed to `origin/main`. It adds Feature149 harness constants and commands (`compositor-live-proof --feature 149`, `compositor-parity --feature 149`, `compositor-reuse --feature 149`, `compositor-snapshots --feature 149`, `compositor-timing --feature 149`, `compositor-readiness --feature 149`), focused Feature149 tests across Harness/Package/Testing/Controls/SkiaViewer/Elmish, readiness artifacts under `specs/149-complete-compositor-p7/readiness/`, and an updated public compatibility ledger. Validation passed for all focused Feature149 filters and `dotnet build FS.GG.Rendering.slnx --no-restore`. Source packages are bumped to `0.1.12-preview.1`; the template package is bumped to `0.1.6-preview.1`. The live proof artifact remains `environment-limited`; it does not claim partial-redraw acceptance or a performance benefit.

Feature 150 landing/package status: Feature 150 implements the first P8 intrinsic-layout slice and was squash-merged to `main` as `acad00d`, then pushed to `origin/main`. It adds explicit Layout constraints, measurement, child placement, intrinsic query/result, content extent, and cache-entry records; replaces ScrollViewer descendant-bound extent readback with `Layout.contentExtent`; adds Controls.Elmish layout metrics and Testing layout-readiness helpers; refreshes public surface baselines; and records readiness evidence under `specs/150-intrinsic-layout-protocol/readiness/`. Validation passed for focused Feature150 filters across Layout.Tests, Controls.Tests, Elmish.Tests, Testing.Tests, and Package.Tests plus `dotnet build FS.GG.Rendering.slnx --no-restore`. Source packages pack at `0.1.13-preview.1`; the template package packs at `0.1.7-preview.1`. The task ledger is 37/58 complete because the full representative layout/ScrollViewer corpus, evaluator-internal measured/intrinsic cache reuse, retained rendering regression sweep, and full solution test remain open.

Table of contents:

1. Executive summary & the one thesis
2. The chosen radical workstreams (and what was deliberately deferred)
3. Current architecture — grounded map
4. Prior-art lessons that drive the design
5. Workstream R1 — Unify the renderer (one builder)
6. Workstream R2 — Modifier algebra + first-class layers/portals in the Scene IR
7. Workstream R3 — Layout: surface the model + intrinsic-size protocol
8. Workstream R4 — Real interaction/overlay state model
9. Workstream R5 — Scene IR as a versioned cross-backend protocol (render-anywhere)
10. Workstream R6 — Compositor: layer promotion + damage-driven redraw
11. Workstream R7 — Real text shaping (HarfBuzz) behind the measurer seam
12. Sequenced roadmap, dependencies & milestones
13. Cross-cutting: parity, determinism, surface/baseline discipline, perf verification
14. Top risks & mitigations
15. Decision log
16. Sources

---

## 1. Executive summary & the one thesis

FS.GG.Rendering is in an unusually strong position to attempt radical change, because the hard, load-bearing pieces already exist and are correct: a pure immutable Scene IR (`SceneNode` DU), a structural fingerprint (`hashScene`), an `SkPicture` record/replay cache with a *disabled-cache parity oracle*, a per-frame damage set with union-area, a fully-modelled-and-wired Yoga flexbox layer, a measurement seam (`setRealTextMeasurer`), and a deterministic golden/surface-baseline test discipline.

The weakness is **architectural duplication**, not missing capability. The two recurring, expensive bug classes of the last several features (the 136 picture-cache regression; the whole of 137) trace to **one root cause**: scene composition is written **twice** — once in the full `Control.renderTree` path and once in the retained `RetainedRender` build/carry/`assemble` path — and parity between them is maintained "by construction" by remembering to edit *N* sites identically. Feature 137 needed all **six** sites changed in lock-step; the 136 regression was a missed seventh. This is the single most important fact in this document.

**The thesis, confirmed by every mature framework we studied:** *incrementality must be a generic memoization/reconciliation layer wrapped around exactly one pure builder — never a second hand-written builder.* React (one component fn + Fiber bailout), Compose (one `@Composable` + slot-table skipping), SwiftUI (one `body` + AttributeGraph), Flutter (one `build` + the Element tree's single `updateChild`), and Elm (one pure `view` + `lazy`/diff) **all** have exactly one producer of UI structure. None has a "full" path and an "incremental" path. The FS.GG bug is precisely the anti-pattern they all avoid.

Therefore the radical program below is organized around a narrower rule: **remove duplicated assembly before adding new public assembly semantics.** The original "R2 before R1" sequencing is too risky if it forces the modifier/layer rules to be implemented twice first. The revised plan introduces a small **R1a shared-assembly extraction** before public IR churn, then lands the modifier/layer algebra, then completes the retained-renderer unification. The payoff still compounds: every subsequent capability is added once, in one place, with parity guaranteed by construction rather than chased by tests.

---

## 2. The chosen radical workstreams

Selected (the radical options):

| # | Workstream | One-line radical goal | Depends on |
|---|---|---|---|
| **R1** | Unify the renderer | **R1a:** extract one shared assembly function without public IR churn. **R1b:** delete the second builder; one pure `assemble` + a generic memo/reconcile layer; fragments constructor-private so a second producer is a *compile error* | R1a: —; R1b: R1a + R2 |
| **R2** | Modifier algebra + layers/portals | Clip/opacity/offset/z/cache become composable *modifiers*; z-order and out-of-tree portals become first-class Scene-IR concepts | R1a |
| **R3** | Layout model + intrinsic protocol | Surface the already-wired flex model as attributes; add a constraints-down/sizes-up + intrinsic-size protocol | — (R3a), R1b (R3b) |
| **R4** | Interaction/overlay state | Transient surfaces genuinely open/close, trap focus, dismiss-on-outside; anchored portals | R2, R3 |
| **R5** | Scene IR as portable protocol | Versioned TLV serialization + multi-backend (server PNG → CanvasKit/WASM → Canvas2D) | R2, R7 |
| **R6** | Compositor | Layer-promotion heuristic + damage scissoring + content/transform key split + texture tier | R2 |
| **R7** | Real text shaping | HarfBuzz behind a widened measurer seam returning shaped glyph runs; portable `GlyphRun` IR node | (coordinates with R2/R5) |

Deliberately **deferred / rejected for now** (documented in §15): Cassowary/constraint-solver as the *core* layout engine (kept as an optional container only — predictability/O(n) wins); a bespoke binary IR before the algebra stabilizes; WebGPU/Graphite browser backend (Dawn-on-web immature). These are noted so the choices are auditable.

---

## 3. Current architecture — grounded map

All anchors verified against `8f75594` unless marked `~`.

### 3.1 Scene IR — `src/Scene/Scene.fs(i)`
- `SceneNode` is a 24-case recursive immutable DU (`Scene.fsi:321-351`): `Empty | Group | Rectangle | PaintedRectangle | Circle | FilledEllipse | Ellipse | Line | Path | Points | Vertices | Arc | Text | TextRun | Image | ClipNode | RegionNode | ColorSpaceNode | PerspectiveNode | PictureNode | Chart | Translate | SizedText | CachedSubtree`. `Scene = { Nodes: SceneNode list }`.
- `CachedSubtree of CacheBoundary` where `CacheBoundary = { CacheId; Fingerprint; Scene }` (`Scene.fsi:363-370`) — **already a RepaintBoundary analog baked into the IR**; `describe`/`diagnostics`/`measure` see *through* it.
- Constructors in `module Scene` (`Scene.fsi:453-529`): `group`, `clipped`, `translate`, `withColorSpace`, `withPerspective`, etc. — composition is *constructor calls*, not a modifier algebra.
- Two impurities for portability: `Image of (...) * string` holds a **filesystem path** (`diagnostics` even does `IO.File.Exists`), and `setRealTextMeasurer` is a **process-wide mutable** measurer seam.
- Painter: `src/SkiaViewer/SceneRenderer.fs` `paintNode` is an exhaustive match (no wildcard); `ClipNode`/`Translate` → `canvas.Save/Restore`; `CachedSubtree` → `PictureReplayCache.paintBoundary` when a replay cache is active, else recurse (the parity oracle).

### 3.2 Control tree, composition & the duplication — `src/Controls/Control.fs(i)`
- `composeContainerScene (box) (own) (childScenes)` (`Control.fs`, declared `Control.fsi`) — the **single shared clip rule** feature 137 introduced. It is the *only* part of composition currently shared.
- `isOverlayNode c = (c.Kind = "overlay")` — overlay membership is a **hardcoded kind string**; the overlay pass is bolted on, not a first-class layer.
- `renderTree` (full path) and `RetainedRender` (retained path) **both** orchestrate the tree walk independently and *both* call `composeContainerScene` — the duplication R1 removes.
- `scrollViewport` reads content height by **walking descendants** because the child's box is clamped — a smell pointing at the missing intrinsic-size protocol (R3b).

### 3.3 RetainedRender — `src/Controls/RetainedRender.fs(i)`
- `RenderFragment = { OwnScene; SubtreeScene; OverlayScene; Box; Fingerprint }`; `RetainedNode = { Identity; Control; Fragment; Children }`.
- **Six composition sites** all route through `composeContainerScene`: `renderTree` paint; `init` build; step `build`/`buildFresh`/`carry`/Update; and the `assemble` emit walk (the 136 miss). `composeRetainedScenes` shares the in-flow/overlay split across the four retained build sites.
- Caches: memo (113), picture replay (116/120), text measure (117), reconcile/diff (067), `hashScene` fingerprint (120). Each has a **`*Enabled=false` always-miss oracle** proving cache-on ≡ cache-off.
- Parity invariants today: full ≡ retained (Audit/Feature093 parity tests); cache-on ≡ cache-off (`Audit_PictureCache`/`Audit_TextCache`/`Audit_MemoCache`); incremental-layout ≡ full-layout (097 INV-1); identity-at-rest; determinism (no clock in id minting).

### 3.4 Layout — `src/Layout/*` (the surprise: it's already complete)
- `LayoutIntent` (`Types.fsi`) already has the **full flex model**: `Direction, Wrap, AlignItems, AlignSelf, JustifyContent, Padding, Margin, Gap, Size, MinSize, MaxSize, FlexGrow, FlexShrink, FlexBasis`.
- The Yoga binding (`Layout.fs:376-411`) **already wires every one of those** to `YGNodeStyleSet*` (FlexDirection, Wrap, AlignItems/Self, JustifyContent, Padding, Margin, Gap, Width/Height, Min/Max).
- **The main gap:** `Control.toLayout` reads just `width`/`height`/`orientation`, hardcodes `Padding = 8` and `Gap = 8`, and `layoutAffectingAttrNames = {width,height,orientation}`. Some authoring builders already exist (`Attr.padding`, `Attr.margin`), but they are ignored by `toLayout` and the drift tests currently assert they are not layout-driving. So the flex model is mostly one mapping/test update away from being authorable, not a lower-level Yoga problem. (This is why T027 dead-ended last session — `flexShrink 0` on the shell bands would have pinned them; the machinery was there, just unexposed.)

### 3.5 Public surface & test infra
- Visibility lives in `.fsi`; `tests/surface-baselines/*.txt` list public **types** (members don't add lines); the Package.Tests gate fails on drift; `scripts/refresh-surface-baselines.fsx` regenerates.
- Tier-1 changes (renderer output / new public surface) require: spec → `.fsi` → semantic tests → impl → surface baseline → golden/drift re-baseline with a disclosure ledger → docs.
- **Known pre-existing failure** (not caused by 137, confirmed by stash-bisect): `Elmish.Tests/Feature117MetricsTests` "cold text-heavy frame" reports 6 text-cache hits vs 0 expected. Fix opportunistically (see §13).

---

## 4. Prior-art lessons that drive the design

| Lesson | Source frameworks | Consequence for FS.GG |
|---|---|---|
| **One pure builder; incrementality is a generic layer, never a second path** | React Fiber bailout, Compose `remember`/skipping, SwiftUI AttributeGraph, Flutter `Element.updateChild`, Elm `view`+`lazy` | R1: delete the second builder; cache stores the *builder's own prior output* keyed on inputs, so hit ≡ fresh by construction |
| **A persistent identity/reconciliation layer between immutable description and retained state** | Flutter Widget/Element/RenderObject; single `updateChild`+`canUpdate(tag,key)` | R1: introduce a node table reconciled via one `reconcileChild` entry; the renderer becomes a thin *applier* (Compose term) that never *describes* a fragment |
| **Double-buffer + atomic commit** | React `current`/`workInProgress`+`alternate`; Elm old/new vtree | R1: build WIP off to the side, swap one root ref; gives free parity oracle and future interruption |
| **Explicit identity/keying is non-negotiable** | React/Compose/Flutter/Elm/SwiftUI keys; `AnyView` erasing identity is the cautionary tale | R1/R4: keep `Key` on collection nodes; treat any type-erasure as a red flag |
| **Modifiers are immutable *equatable descriptors* folded inside-out; order is semantic** | SwiftUI `.padding().background()` vs reverse; Compose `Modifier` chain | R2: `Modifier` DU folded over nodes; document + property-test order; fuse/normalize |
| **Do NOT model modifiers as per-update closures/factories** | Compose retired `composed{}` (re-allocated every recomposition, never skippable) → `Modifier.Node` cut composition time up to ~80% | R2: immutable value modifiers with structural equality → free diff-and-reuse |
| **z-index is local-to-parent; only portals escape ancestor clips** | SwiftUI presentation, Compose `Popup`, Flutter `Overlay`/`OverlayEntry`, `flutter_portal` | R2/R4: split in-tree `ZIndex` from out-of-tree `Portal` re-parented to named layer hosts with a global z order |
| **Hit-test order must be derived from paint order (one function)** | "hit-testing should match painting" (W3C) | R2: single ordering fn for paint + hit, in-tree and cross-layer |
| **Single measure per pass; intrinsics are the only legal "measure twice"** | Compose `MeasurePolicy`+intrinsics, Flutter constraints-down/sizes-up, SwiftUI `sizeThatFits` at zero/unspecified/infinity | R3b: adopt constraints-down/sizes-up + intrinsic queries + a layout cache |
| **Separate *content* from *properties* (transform/clip/effect as their own nodes)** | Chromium `cc` property trees; WebRender picture tree | R5/R6: a moving/scrolling subtree mutates a property delta, not recorded content → cheap re-composite |
| **A portable IR is self-contained data with no host handles, versioned, with skippable unknowns** | WebRender serialized display list; Skia `SkSerialProcs` for images/typefaces; SkPicture format is *not* a stability contract | R5: TLV + magic/version header + capability negotiation; `Image(path)`→content-addressed `ImageRef`; text pre-shaped to `GlyphRun` |
| **Text is a loose hierarchy: bidi → itemize → fallback → shape (HarfBuzz) → line-break → position → raster**; shape once, use for both measure and draw | HarfBuzz docs, Raph Levien, WebRender `TextRun` | R7: widen the measurer seam to return a shaped run; lower text to a `GlyphRun` node |
| **Promote a subtree to its own layer only when its repaint cadence differs from its parent; over-promotion is pure cost** | Flutter `RepaintBoundary`, Chrome compositing promotion | R6: promotion heuristic on multi-frame fingerprint stability + node count; demote churning boundaries |

---

## 5. Workstream R1 — Unify the renderer (the keystone)

**Goal.** One pure producer of scene structure; the retained renderer becomes a generic memoization/reconciliation layer over it. Full ≡ retained and cache-on ≡ cache-off become **structural properties**, not test-chased invariants. To avoid expanding the duplicated architecture first, split the work into two deliberately separate cuts:

- **R1a — shared assembly extraction:** extract the current full/retained composition rules into one internal assembly function while keeping today's public `SceneNode` shape. This is a behavior-preserving refactor: same `ClipNode`, same `Translate`, same overlay pass, same goldens.
- **R1b — full retained unification:** after R2 proves the cleaner algebra/layer model, collapse retained build/carry/emit into one generic memo/reconcile layer over the single producer.

**Target shape (Elm `lazy` + Flutter Element + Compose applier):**

```
// the ONLY scene producer (pure, total, side-effect free)
assemble : AssembleCtx -> Tree -> Scene

// generic memo layer — contains ZERO scene-specific knowledge
memoize  : key:'k -> inputs:'i -> build:('i -> 'a) -> 'a

// one reconciliation entry (Flutter updateChild analog)
reconcileChild : parent -> old:Node option -> next:Tree option -> Node option
//   canUpdate(old,next) = old.Tag = next.Tag && old.Key = next.Key
```

- **The renderer is an *applier*** (Compose term): it only `insert`/`move`/`remove`/`update`s fragments per the reconciler's decisions. It never constructs a fragment.
- **Fragments are constructor-private** (`.fsi` hides the constructor): the applier *cannot fabricate* a fragment — only receive, cache, move, drop ones `assemble` produced. This makes "no second builder" a **compile-time rule**, the F# analog of SwiftUI forcing structure through `some View`.
- **Double-buffer**: reconcile into a WIP node table + scene, then swap one root ref. In debug, run pure `assemble` and assert deep-equality vs the reconciled scene — now a *redundant check of an invariant*, not the mechanism enforcing it.
- **Memo key = (structural-position + explicit Key, input-hash)**; the cached value is *the fragment `assemble` produced last time for these inputs*. F# structural equality of immutable records/DUs gives reliable input comparison for free.

**Change-sites.**
- **R1a:** new `src/Controls/Assemble.fs(i)` with an internal `assembleCurrent : AssembleCtx -> Control<'msg> -> AssembledScene` that owns the current `paintNode` + `composeContainerScene` + overlay split. `Control.renderTree`, `RetainedRender.init`, retained build/carry, and retained emit all call this one function or a narrow child-composition helper. No public `Scene` changes.
- **R1b:** evolve that module into the single `assemble` + `AssembleCtx` (theme, boundsById). `Control.renderTree` becomes "reconcile with a cold cache" (thin wrapper over the applier).
- `RetainedRender.fs` — `init`/`step` reduce to: diff (`Reconcile.diff`, kept), then `reconcileChild` walk that calls `memoize`/`assemble`; **delete** the parallel `build`/`buildFresh`/`carry`/`assemble`-emit composition logic. `RenderFragment` constructor → private.
- Keep all existing cache plumbing (memo/picture/text) — it becomes the *inputs* to `memoize`, not a separate code path.
- Tests: the parity suites stay but are downgraded to "redundant invariant checks"; add an FsCheck property test fuzzing random trees asserting `assemble(tree) ≡ reconcile(cold) ≡ reconcile(warm)`.

**Invariants/oracles.** All existing parity oracles must stay green *unchanged* through the refactor (they are the safety net). Add the property test as the new structural guarantee.

**Risks.** Highest-risk workstream (touches the parity machinery). Mitigation: land R1a as a *pure refactor* with byte-identical output first (the suite + goldens are the oracle), then do R2 over one internal assembly seam, then simplify in R1b.

**Effort.** L–XL. The keystone; everything else is cheaper once this lands.

---

## 6. Workstream R2 — Modifier algebra + first-class layers/portals

**Goal.** Replace special-cased composition (clip in `composeContainerScene`, overlay deferral in the emit walk, `Translate`/`ClipNode`/`PerspectiveNode` as ad-hoc node kinds, `isOverlayNode` hardcoded) with a **composable modifier algebra** and a **first-class layer/z-order + portal** model in the Scene IR.

**Design — modifier algebra (immutable, equatable, folded inside-out):**

```fsharp
type Modifier =
    // Layout-affecting
    | Padding of Edges | Frame of Constraint | Offset of float * float   // Offset is visual-only
    // Paint / render-layer
    | Opacity of float | Clip of ClipShape | Transform of Affine2D
    | Background of Node | Overlay of Node
    // Structural
    | ZIndex of float | CacheBoundary of CachePolicy | Layer of LayerHint

and Node =
    | Prim of Primitive
    | Container of LayoutSpec * Node list
    | Modified of Modifier * Node
    | Portal of PortalSpec * Node
```

This shape is **conceptual**, not permission to collapse the architecture boundary blindly. `Scene` should remain a display-list/protocol layer unless the project explicitly decides to make layout containers part of the Scene contract. Prefer an internal modifier/layer lowering first, then expose the smallest public surface that proved necessary. In particular, avoid making `Container of LayoutSpec` a public `SceneNode` unless the Control/Layout/Scene responsibility split is deliberately redrawn.

- **Order semantics** documented + property-tested: right-associative fold `mₙ ∘ … ∘ m₁ ∘ node` (innermost applied first), so `padding |> background` colors the padding (SwiftUI/Compose semantics).
- **Three effect classes** for independent invalidation (Compose capability-interface lesson): `LayoutEffect` (Padding/Frame → re-measure+paint), `DrawEffect` (Opacity/Clip/Transform/Offset/Background/Overlay → re-paint only), `OrderEffect` (ZIndex → re-composite only). Non-layout modifiers are layout-neutral (pass constraints through, report child size unchanged).
- **Algebraic normalization pass** (cheap correctness+perf win): `Opacity a ∘ Opacity b ≡ Opacity (a*b)`; `Transform A ∘ Transform B ≡ Transform (A·B)`; `Padding p ∘ Padding q ≡ Padding (p+q)`; drop identities (`Opacity 1`, `Transform I`, `Padding 0`); collapse adjacent paint modifiers into a single `graphicsLayer`-style render-node.
- **`CacheBoundary`/`Layer` as first-class modifiers** (not a hidden heuristic), bundling `{opacity, transform, clip, cache}` like Compose `graphicsLayer` / Flutter layer.

**Design — layers/z-order/portals (split the two mechanisms):**

```fsharp
type Scene = { Root: Node; Layers: (LayerId * Node list) list }   // layers ordered bottom→top
type PortalSpec = { Target: LayerId; Anchor: AnchorSpec; Dismiss: DismissPolicy }
```

- **In-tree z-order**: within a container, paint order = `stableSort(children, key=(zIndex, declIndex))`; `ZIndex` is strictly local to parent. **Hit-test order = reverse paint order**, from the *same* ordering function.
- **Portals (out-of-tree)**: a `Portal` node is lifted at build time into a named layer host (Content < Popup < Tooltip < Modal < DragFeedback < Toast), escaping every ancestor clip/transform/opacity — the thing pure `zIndex` cannot do. Anchored to the origin node's *resolved* frame (declarative, like `flutter_portal`). This *generalizes and replaces* the feature-137 overlay pass.
- `isOverlayNode (kind="overlay")` is deleted; "overlay" becomes "authored a `Portal`."

**Change-sites.**
- `src/Scene/Scene.fs(i)`: add `Modifier`, `Node`-level `Modified`/`Portal`, `Scene.Layers`; constructors `withClip/withOpacity/withOffset/withTransform/withZ/withCache/withLayer`, `portal`; the normalization pass; update `describe`/`diagnostics`/`measure` to recurse modifiers; **deprecate** standalone `ClipNode`/`Translate`/`PerspectiveNode` (one cycle) by lowering them to `Modified`.
- `src/SkiaViewer/SceneRenderer.fs`: a `Modified(mods,scene)` painter case (apply mods L→R via Save/Restore/paint config); a layer-host composite pass (paint `Root`, then `Layers` bottom→top); hit-test across layers top-down via the shared ordering fn.
- `src/Controls/...`: `composeContainerScene` becomes `withClip` over the children node; the overlay split becomes `portal`; `RetainedRender.OverlayScene` is subsumed by layer hosts.
- `hashScene` (`RetainedRender`): hash modifier lists + layers.
- Surface baseline + rebaseline ledger (Tier-1): new public `Modifier`/`Portal`/`LayerId` types if and only if they are truly public authoring/protocol concepts. Because `SceneNode` is public and downstream consumers may exhaustively match it, this is a major compatibility event: publish a migration note, keep old constructors working for at least one cycle, add compatibility tests proving old nodes lower to the new representation, and decide explicitly whether the package version needs a major bump.

**Invariants/oracles.** "Hit matches paint" property test (in-tree + cross-layer). Empty-layer scene ≡ pre-R2 (the 137 empty-overlay invariant generalized). Normalization is output-preserving (property test: `render(normalize s) ≡ render s`).

**Risks.** IR change ripples to every consumer; mitigate with the deprecation cycle (old kinds lower to modifiers) and the exhaustive-match painter (compiler finds every site). Do R2 after R1a so the new rules land behind one current-semantics assembly seam, then complete R1b once the fold is clean.

**Effort.** L.

---

## 7. Workstream R3 — Layout: surface the model + intrinsic protocol

**R3a (quick win, low risk, do first).** Surface the already-wired flex model as attributes. *Zero* Layout/Yoga changes — purely the `Control` boundary.
- `src/Controls/Attributes.fs(i)`: keep existing `padding`/`margin` builders, add the missing `flexGrow, flexShrink, flexBasis, alignSelf, alignItems, justifyContent, gap, minWidth/Height, maxWidth/Height` builders, and decide whether `padding`/`margin` remain uniform-only or gain edge-specific variants.
- `Control.toLayout`: map each attribute into the corresponding `LayoutIntent` field (the fields and Yoga wiring already exist — `Types.fsi` + `Layout.fs:376-411`). Replace the current hardcoded `Padding = 8` / `Gap = 8` behavior with explicit defaults plus authored overrides.
- `layoutAffectingAttrNames`: extend the set so incremental layout invalidates correctly.
- `tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs`: update the candidate corpus and expected discovered set. It currently treats `padding`/`margin` as non-layout-driving; that assertion must flip when `toLayout` starts reading them.
- Immediately fixes the T027 shell-chrome problem (`flexShrink 0` pins the bands) and a whole class of "Yoga shrank my fixed child" surprises.

**R3b (radical, after R1b).** Add the constraints-down/sizes-up + intrinsic-size protocol (Flutter/Compose/SwiftUI convergence) so containers like `ScrollViewer` stop walking descendants to discover content height.
- An `ILayout`-style protocol: `Measure(constraints, children, cache) -> Size`; `Place(bounds, children, cache)`; `Min/MaxIntrinsicWidth/Height`. **Single measure per pass**; intrinsics are the only legal "measure twice." A `LayoutCache` persists across measure/place and invalidations.
- Keep Yoga as the default flex container behind this protocol; add an opt-in relational (`ConstraintLayout`/Cassowary) container *only* as a specialized escape hatch (predictability/O(n) stays the default).

**R3b Feature150 status.** The first slice is implemented: public constraints/intrinsic/content-extent contracts exist, `ScrollViewer` consumes intrinsic extent evidence through Layout, focused full/incremental parity and cache-identity tests pass, and compatibility/readiness records are published. The complete R3b acceptance bar remains open for the full representative corpus, single-measure accounting beyond focused diagnostics, evaluator-internal cache reuse/stale rejection, and broad retained/default layout regression validation.

**R3b Feature151 status.** The final P8 acceptance package is implemented on `151-complete-p8-layout`. It adds the representative layout and ScrollViewer corpus tests, measured and intrinsic reuse identity checks, stale-rejection/fallback diagnostics, full/cold/warm/changed incremental parity, retained/default layout and harness regression classification, package/readiness validation, and a final readiness summary under `specs/151-complete-p8-layout/readiness/`. No new public `.fsi` delta was required beyond Feature150's protocol.

Feature151 validation passed for restore, solution build, focused Feature151 filters, broad regression filters, public surface checks after stable baseline sync, full solution tests, source package pack at `0.1.14-preview.1`, and template package pack at `0.1.8-preview.1`. It was squash-merged to `main` as `6f9d606`.

**Change-sites (R3b).** `src/Layout/*` new protocol + cache; `ScrollViewer` reimplemented against intrinsics (delete the descendant-walk in `scrollViewport`); `Control.evaluateLayout` threads the cache.

**Invariants/oracles.** R3a: bounds for default attrs byte-identical to today. R3b: incremental ≡ full (097 INV-1) preserved; intrinsic results cached deterministically.

**Effort.** R3a: S. R3b: M–L.

---

## 8. Workstream R4 — Real interaction/overlay state model

**Goal.** Make transient surfaces *behave*: combo/menu/date-picker/auto-complete genuinely open/close, trap focus, dismiss on outside-click/Esc, and float via anchored portals (R2). Today they render as static schematics with no open state — the feature-137 overlay pass is the rendering half of a feature whose behavioral half doesn't exist.

**Design.** Elmish-integrated overlay state machine: open/close messages, an overlay manager in the runtime, focus trapping, keyboard nav (arrow/enter/esc), dismiss policies, scrim/modal layers. Renders through R2 `Portal`s anchored to the trigger's resolved frame; hit-tests top-down across layers. Deterministic message log keeps it fully testable headlessly.

**Change-sites.** `src/Controls/Focus.fs`, `Pointer.fs`, `Widgets/Pickers.fs`/`Overlay.fs`, `ControlRuntime`; new overlay-manager module; Elmish wiring in `src/Elmish`. The AntShowcase date-picker (already uses `Overlay` when `IsOpen`) becomes the reference consumer.

**Invariants/oracles.** Open dropdown paints above + wins hit-test (extends 137 tests); closed ≡ pre-open; focus returns to trigger on dismiss; keyboard nav deterministic.

**Effort.** L. Depends on R2 (portals) and R3 (anchoring/measurement).

**Feature 143/144/145 status (2026-06-17).** P5 was implemented across three slices. Feature 143 added the pure overlay coordinator in `src/Controls/OverlayState.fsi` / `.fs`: finite supported surface kinds, dismissal policy, focus scope, modal trap mode, topmost hit decisions, replay logs, diagnostics, and ordered effects. Feature 144 adds product-owned transient widget metadata, metadata collection/validation/translation, pointer/focus overlay routing helpers, runtime dispatch records, Controls.Elmish overlay effect interpretation, eight-category widget lowering, AntShowcase reference date-picker evidence, and deterministic Rendering.Harness overlay corpus evidence. Feature 145 adds the real overlay visual-proof harness/readiness path and closes the Feature 144 caveat with current-run open/closed PNG artifacts correlated to the AntShowcase date-picker flow. Focused Feature143/144/145 tests pass across the relevant harness and AntShowcase filters; unsupported hosts still report an environment-limited limitation without accepted artifacts.

---

## 9. Workstream R5 — Scene IR as a versioned cross-backend protocol

**Goal.** Make the pure Scene IR a **serializable, versioned, cross-backend protocol** and render the *same* scene on multiple backends. The architecture is unusually suited: rendering is already pure `Scene -> pixels` and `SceneEvidence.renderPng` exists.

**Design.**
- **Wire format**: TLV (tagged, length-delimited) with header `magic + version(major.minor) + capabilitySet`. Unknown tags are length-skippable (forward-compat). *Not* F# DU auto-serialization (loses skip-ability, pins CLR layout). Two profiles: ephemeral same-version (fast, like SkPicture's internal format) and durable versioned (cross-backend/version).
- **Capability negotiation**: reuse the existing `describe → SceneElementKind` set as the vocabulary; tag each kind **Core / GPU / HighLevel**. Producer lowers HighLevel (`Chart`) to primitives before the wire; consumer advertises supported tiers.
- **Resources out-of-band**: replace `Image(path)` with content-addressed `ImageRef(hash)` + a resource table (Skia `SkSerialProcs` model); fonts likewise. Text must be pre-shaped to `GlyphRun` (R7) so no backend needs a shaper.
- **Determinism**: pin float formatting, enum ordinals, list order → stable `hashScene`/goldens/cache keys.

**Phased backends.**
1. **Server PNG** (lowest risk, immediate value): swap the `SceneEvidence` placeholder for a real Skia raster/headless-GL surface → the **golden oracle** for all other backends.
2. **CanvasKit / Skia-WASM** (highest fidelity target, but not assumed cheap): run a feasibility spike before committing. The spike must prove the .NET/F# + SkiaSharp/CanvasKit packaging path, font/resource loading, and browser host model. If direct reuse of the current `Scene → SKCanvas` painter is not practical, fall back to a generated/interpreted CanvasKit command stream.
3. **Canvas2D** (optional, size-driven): hand-written interpreter for the Core subset; GPU-tier nodes get software fallback/diagnostic.
4. WebGPU/Graphite — deferred.

**Change-sites.** New `src/Scene/SceneCodec.fs(i)` (`toProto`/`fromProto`/`compare`, version, capability tags); `Image`→`ImageRef` + resource table across Scene + painter; new backend projects (`src/SkiaViewer.Wasm`, server-raster path in `SceneEvidence`). Surface baseline + ledger.

**Invariants/oracles.** Round-trip `fromProto∘toProto = id` (property test); identical scenes serialize byte-identically; each backend diffed pixel-wise against the server-PNG oracle within tolerance.

**Effort.** XL (multi-phase). Depends on R2 (stable IR) + R7 (`GlyphRun`).

**Feature 146 status (2026-06-18 00:04 CEST).** P6 is implemented as
`146-render-anywhere-protocol`. The implementation adds `SceneCodec` to `FS.GG.UI.Scene` for a
deterministic TLV scene package (`FSGGSCENE`), package identity hashing, resource manifests,
capability/resource inspection, forward-compatible optional-tag skipping, required-tag rejection,
semantic scene comparison, and diagnostic formatting. `FS.GG.UI.SkiaViewer` adds
`ReferenceRendering` as an MVU/effect/evidence surface that imports portable packages, renders
through the existing Skia scene path, writes PNG artifacts, validates decodable/non-blank output,
and classifies failures or environment-limited runs honestly. `FS.GG.UI.Testing` adds package
inspection assertions, and `tests/Rendering.Harness/RenderAnywhere` records the representative
corpus, reference evidence, and browser feasibility fallback report.

Validation is recorded in `specs/146-render-anywhere-protocol/readiness/validation-summary.md`.
Focused Feature146 tests passed in Scene, SkiaViewer, Rendering.Harness, and Package.Tests; the full
Package.Tests project and package surface filter passed; `scripts/refresh-surface-baselines.fsx`
passed and refreshed root readiness baselines; the reference harness wrote three PNG artifacts; the
browser feasibility command wrote a documented CanvasKit command-stream fallback report; readiness
FSI snippets passed; solution build passed; and `dotnet pack FS.GG.Rendering.slnx -c Release -o
/home/developer/.local/share/nuget-local --no-restore` passed before merge and again after the
mandatory post-merge bump to `0.1.9-preview.1`. Full solution tests require `WAYLAND_DISPLAY` to be unset on this host; with X11 forced, the
run avoided the `libdecor-gtk.so` Wayland crash but still surfaced unrelated Controls
typed-lowering parity failures where typed transient widgets include `transientWidgetMetadata` and
legacy comparison expectations do not.

---

## 10. Workstream R6 — Compositor: layer promotion + damage-driven redraw

**Goal.** Turn the existing `SkPicture` replay cache + damage set into a real compositor: damage-scissored frame paint, generalized layer promotion, content/transform key separation, and a texture tier — partial redraw instead of full re-walk.

**Design (in dependency order, each preserving the disabled-cache parity oracle).**
1. **Damage-scissored frame paint** (biggest win from data you already have, but correctness depends on presentation semantics): first prove the host preserves previous pixels or has a retained backing surface/layer tree. If each frame starts from a fresh cleared framebuffer, clipping to the union damage rect will leave untouched regions blank/stale. The first deliverable is a present-path proof test; only then set the frame canvas clip to the existing **union damage rect** and replay.
2. **Generalize boundary selection** beyond data-grid rows: a Flutter-style promotion heuristic — promote a subtree to a `CacheBoundary` when its fingerprint was stable over the last *N* frames (you already track prior-frame stability, FR-012) *and* node-count exceeds a threshold; **demote** boundaries whose fingerprint churns every frame (pure cost).
3. **Split the boundary key into content-fingerprint + placement-transform** (the Chromium `cc` property-tree lesson): a subtree that only *moves*/scrolls re-blits at a new offset instead of re-recording. Highest-leverage IR change for compositor efficiency; composes with R2's `Transform` modifier.
4. **Texture-promotion tier** above SkPicture replay: for stable-but-expensive boundaries (blurs/shadows/large paths), snapshot to an `SKImage` and `DrawImage`; same `CacheId`/`Fingerprint` key (the Flutter `EngineLayer` analog). Choose tier by recorded op-count/cost.

**Change-sites.** `src/SkiaViewer/PictureReplayCache.fs` (scissor, texture tier); `RetainedRender.fs` (promotion heuristic, content/transform key split, damage→scissor plumbing); `WorkReductionRecord` counters for promotion/demotion/texture hits.

**Invariants/oracles.** Disabled-cache path stays pixel-identical to the direct walk for **every** new tier (the existing FR-011 oracle, extended). Perf probes (see §13) prove each tier actually pays off before relying on it.

**Effort.** L. Depends on R2 (`Transform`/`CacheBoundary` modifiers).

**Feature 147 status (2026-06-18).** P7 has a deterministic readiness slice on
`147-compositor-damage-redraw`: `CompositorProof` contracts, host/profile readiness validation,
OpenGL scissor decision helpers, retained damage-union and promotion/snapshot policy helpers,
derived `CompositorFrameDiagnostics`, harness commands for proof/parity/perf/readiness, refreshed
surface baselines, and readiness artifacts under `specs/147-compositor-damage-redraw/readiness/`.
It landed on `main` as squash merge `85b0b7e`, followed by package version `0.1.10-preview.1` at
`2247cb3`.
The slice is intentionally limited: the generated present proof is `environment-limited` because
the live GL sentinel/readback interpreter is not implemented yet, so damage scissoring remains
fallback-gated and no shipped performance win is claimed. Remaining P7 exit criteria are live
sentinel/readback proof, SceneRenderer/SkiaViewer no-clear scissored drawing plus full-redraw
fallback diagnostics, full content/placement tracking, expanded corpus execution, snapshot resource
lifecycle/composition, and real host timing probes.

**Feature 148 status (2026-06-18).** Feature 148 implements the next evidence/readiness layer for
P7 without claiming the native live-renderer work as complete, and was squash-merged to `main` as
`7d708c4`.
Completed pieces include exact Feature148 readiness folders and corpus inventory; harness contracts,
formatters, and CLI routes for live proof, damage parity, reuse, snapshot lifecycle, timing, and
readiness assembly; FSI transcript coverage; focused tests in `Rendering.Harness.Tests`,
`Package.Tests`, `Controls.Tests`, `SkiaViewer.Tests`, and `Elmish.Tests`; generated readiness
artifacts; and Controls/SkiaViewer documentation updates. Current status is 61/76 tasks complete.
Validation passed for all focused Feature148 test filters and `dotnet build FS.GG.Rendering.slnx
--no-restore`, `dotnet fsi scripts/refresh-surface-baselines.fsx`, source package pack at
`0.1.11-preview.1`, and template pack at `0.1.5-preview.1`. The open tasks are deliberately not
marked complete: live `CompositorProof`/OpenGL
sentinel-damage readback and artifact capture, public damage/reuse/snapshot/timing surface
expansions in their final package modules, no-clear damage-scoped renderer integration, full
snapshot resource lifecycle/composition, real timing probes in `Perf.fs`, Evidence-module
formatters, and remaining public surface expansion. The generated live proof is `environment-limited`,
so damage scissoring remains fallback-gated.

**Feature 149 status (2026-06-18).** Feature 149, the final P7 Spec Kit evidence/readiness
package, was squash-merged to `main` as `a9a1ef1`. All 68 tasks in
`specs/149-complete-compositor-p7/tasks.md` are checked complete. The merge adds first-class
Feature149 harness constants, `--feature 149` command routing for live proof, damage parity, reuse,
snapshot, timing, and readiness assembly, focused Feature149 tests across `Rendering.Harness.Tests`,
`Package.Tests`, `Testing.Tests`, `Controls.Tests`, `Elmish.Tests`, and `SkiaViewer.Tests`,
consumer-facing readiness helper coverage, FSI transcript coverage, and generated readiness
artifacts under `specs/149-complete-compositor-p7/readiness/`. Source packages are bumped to
`0.1.12-preview.1`; the template package is bumped to `0.1.6-preview.1`.
Validation passed for all focused Feature149 test filters, all Feature149 harness artifact commands,
and `dotnet build FS.GG.Rendering.slnx --no-restore`. The Feature149 validation summary is
`environment-limited`: the deterministic harness records host facts and safe fallback evidence, but
the live sentinel/damage readback proof still does not produce three accepted capable-host artifacts.
Partial redraw therefore remains disabled unless future capable-host proof is fresh, matching, and
accepted; no P7 performance claim is made from synthetic or environment-limited evidence.

**Feature 152 status (2026-06-18).** Feature 152 implements the explicit live acceptance closeout
layer for P7 without overclaiming live readiness. It adds `CompositorProof` proof-set contracts for
three fresh matching capable-host attempts, `Testing.CompositorReadiness` helper contracts,
Feature152 harness constants and `--feature 152` routing for live proof, parity, timing, and
readiness assembly, focused tests across `SkiaViewer.Tests`, `Rendering.Harness.Tests`,
`Testing.Tests`, `Controls.Tests`, `Elmish.Tests`, and `Package.Tests`, package FSI transcript
coverage, refreshed surface baselines, and readiness artifacts under
`specs/152-compositor-live-proof/readiness/`. Validation passed for all focused Feature152 filters,
Feature152 harness artifact commands, `dotnet test FS.GG.Rendering.slnx --no-restore`, package
surface tests, and `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local
--no-restore` at `0.1.14-preview.1` before merge. It was feature-committed as `4151f24`,
squash-merged to `main` as `8ea61c4`, pushed to `origin/main`, then followed by post-merge package
bump `61d1ce8`; source packages pack at `0.1.15-preview.1`, and the template package packs at
`0.1.9-preview.1`. The root `fake.sh` wrapper is absent in this checkout, so
`PackageSurfaceCheck` and `PackLocal` Fake targets are recorded as tooling-limited and substituted by
the package surface test suite plus `dotnet pack`. The Feature152 readiness summary is
`environment-limited`: current host facts are recorded, unsupported-host proof records zero accepted
partial-redraw artifacts, partial redraw remains fallback-gated, and no compositor performance claim
is accepted.

**Feature 153 status (2026-06-18).** Feature 153 implements the next proof-interpreter slice on
`153-compositor-proof-interpreter` and was squash-merged to `main` as `d7c539c`. It extends
`CompositorProof.AcceptedProofSet` with explicit
selected attempt ids and freshness window, adds pure `GlHost.LiveProofHostFacts` /
`LiveProofHostReadiness` classification, exposes `Viewer.liveProofInterpreterSupported`, adds
Feature153 harness constants/renderers/CLI routing, records FSI transcript coverage, refreshes
SkiaViewer and Testing surface baselines, and writes the readiness package under
`specs/153-compositor-proof-interpreter/readiness/`. Focused filters pass:
`SkiaViewer.Tests --filter Feature153` (11), `Rendering.Harness.Tests --filter Feature153` (5),
`Testing.Tests --filter Feature153` (2), and `Package.Tests --filter Feature153` (3).
Unsupported-host quickstart exits in 1s with `environment-limited` and zero accepted
partial-redraw artifacts. `dotnet test FS.GG.Rendering.slnx --no-restore` was restored and started;
Testing, Color, KeyboardInput, Rendering.Harness, Scene, SkiaViewer, Smoke, Lib, Layout, Input, and
Elmish pass summaries were observed, but the run was interrupted after several minutes without final
Controls/Package completion output. `./fake.sh` remains absent, so `PackageSurfaceCheck` and
`PackLocal` Fake targets remain tooling-limited. The post-merge package bump raises source packages
to `0.1.16-preview.1` and the template package to `0.1.10-preview.1`.

**Feature 154 status (2026-06-18, landed).** Feature 154 implements the P7 proof-acceptance
closeout package. It was squash-merged to `main` as `2c9af24`, pushed to `origin/main`,
and the local `154-compositor-proof-acceptance` branch was deleted; no stale remote branch was
present. The post-merge package step raises packable source projects to `0.1.17-preview.1`.
It adds Feature154 harness constants,
renderers, command routing for `compositor-live-proof`, `compositor-parity`, `compositor-timing`,
and `compositor-readiness`, focused tests across SkiaViewer, Rendering.Harness, Controls, Elmish,
Testing, and Package suites, FSI transcript coverage, and readiness evidence under
`specs/154-compositor-proof-acceptance/readiness/`. All 70 tasks are checked. Focused Feature154
filters pass: `SkiaViewer.Tests` (10), `Rendering.Harness.Tests` (9), `Controls.Tests` (2),
`Elmish.Tests` (2), `Testing.Tests` (3), and `Package.Tests` (3). Broad
`dotnet test FS.GG.Rendering.slnx --no-restore` passes locally; the final long-running Controls
suite reports 876 passed / 1 skipped. Unsupported-host quickstart completes in approximately 0.6s
with `environment-limited` and zero accepted partial-redraw artifacts. Root `./fake.sh` remains
absent, so the FAKE `PackageSurfaceCheck` and `PackLocal` targets are tooling-limited here; direct
`dotnet pack` is handled during the merge/package step.

**Feature 155 status (2026-06-18, local closeout).** Feature 155 removes the current-host P7
environment limit for correctness on this capable host. The host has X11 display `:1`, direct
OpenGL rendering, and AMD Radeon Graphics via Mesa/radeonsi; the stable harness host profile is
`probe-08a47c01`. `compositor-live-proof --feature 155 --attempt-count 3` produced three accepted
current-run attempts under `specs/155-native-proof-capture/readiness/live-proof/attempts/`, each
with decodable non-blank sentinel/damage PNG artifacts, preserved undamaged pixels, updated damaged
pixels, and non-synthetic proof metadata. `compositor-readiness --feature 155` now loads those real
attempt artifacts rather than synthesizing rows, and `validation-summary.md` records proof-set
`accepted`, parity `accepted`, selected attempts `3/3`, fallback status
`partial-redraw-accepted`, and performance claim `not-accepted`. The unsupported-host run with
display variables unset still records `environment-limited` and zero accepted partial-redraw
artifacts. Focused Feature155 filters pass for SkiaViewer, Rendering.Harness, and Package; the FSI
transcript passes; broad `dotnet test FS.GG.Rendering.slnx --no-restore` passed on retry after an
initial transient Layout.Tests testhost access violation, and the specific failing Layout filter
passed on rerun.

**Remaining P7 caveats after Feature 155.** Correctness acceptance is scoped to the current stable
host profile and the Feature155 sentinel/damage proof path; it is not a universal host guarantee.
Unsupported hosts continue to fail closed. The performance claim remains deliberately unaccepted:
the timing package is `inconclusive`, so there is no claimed compositor speedup yet. A future
performance closeout still needs a predeclared threshold/noise policy, representative same-profile
live timing, and reviewer-visible acceptance or rejection. Restoring a root `fake.sh` wrapper would
remove the separate package-target tooling limitation, but it is not a compositor proof prerequisite.

---

## 11. Workstream R7 — Real text shaping (HarfBuzz)

**Goal.** Replace per-character drawing + the `0.58·size·length` heuristic with a real shaping pipeline, so measured advances == drawn advances *by construction*, and complex scripts/ligatures/kerning/emoji work.

**Design.**
- **Shape once, use twice**: widen the `realTextMeasurer` seam to return a **shaped glyph run** (glyph IDs + advances + offsets + cluster map), not a scalar width. Measurement reads `Σ advances`; drawing emits those exact glyphs. The "box sized differently than drawn" bug class disappears.
- **`GlyphRun` IR node**: the lowered, portable form of text (WebRender `TextRun` model). Producer shapes `TextRun`/`SizedText`/`Text` into `GlyphRun`s before serialization → text portable across backends without shipping a shaper (ties into R5).
- **Stack**: `HarfBuzzSharp` (ships with SkiaSharp) per run; ICU/ICU4N for bidi (UAX#9) + line/grapheme breaks (UAX#14/#29); a font-fallback resolver over the bundled families; `SKTextBlob`/`DrawText` for raster. **Ship single-run HarfBuzz first** (kills measure≠draw); add bidi/fallback/line-break incrementally.
- Keep `realTextMeasurer = None` (the heuristic) as the deterministic pure/golden fallback. Cache shaped runs (keyed by text/font/script/dir); they compose with the replay cache since `GlyphRun` fingerprints stably.

**Change-sites.** `src/Scene/Scene.fs(i)` (`GlyphRun` node, widened measurer payload `TextMetrics → ShapedRun`); `src/SkiaViewer/Fonts.fs`/`Text` (install a HarfBuzz measurer; draw glyph runs); text lowering in `Control`/`SceneRenderer`. Surface baseline + ledger.

**Invariants/oracles.** Measured advance == drawn advance (property test over the installed shaper); pure-fallback goldens byte-identical with no shaper installed; round-trip `GlyphRun` through R5.

**Effort.** M–L. Largely independent; coordinate the `GlyphRun` node with R2/R5.

**Feature 142 status (2026-06-17).** P4's first production text-shaping slice is implemented on
`142-harfbuzz-text-shaping`. `src/Scene` exposes dependency-light shaped text records, provider evidence,
fallback modes, fingerprints, and glyph-run projection without taking a Skia/HarfBuzz dependency.
`src/SkiaViewer` owns `SkiaSharp.HarfBuzz`, the HarfBuzzSharp native asset packages, provider lifecycle/status,
shaped result construction, and glyph-id/position drawing through `SKTextBlobBuilder`. Controls/retained text
measurement cache keys include the active text provider version bucket, so pure fallback, bundled fallback, and
HarfBuzz shaping cannot reuse stale text metrics across provider changes. Still deferred: full bidi, line breaking,
portable serialization, browser rendering, caret/selection/editing, and compositor work.

---

## 12. Sequenced roadmap, dependencies & milestones

Dependency graph: R1a has no dependency; R2 depends on R1a if it changes assembly semantics; R1b depends on R1a + R2; R3b depends on R1b; R4 depends on R2 + R3 anchoring; R5 depends on R2 + R7; R6 depends on R2 plus a present-path proof. R3a and the initial `GlyphRun` type spike are near-independent.

| Phase | Workstream(s) | Why here | Exit criteria |
|---|---|---|---|
| **P0 — Quick win** | R3a (layout attrs) + fix the pre-existing Elmish metrics test | Low-risk, unblocks T027, immediate authoring value | Implemented as Feature 138: flex attrs authorable; existing `padding`/`margin` honored; drift guard updated; T027 shell chrome clean; Elmish suite green |
| **P1 — Duplication reduction** | R1a shared assembly extraction | Removes the most dangerous duplication before adding new semantics | Implemented as Feature 139: one internal current-semantics assembly seam; `renderTree`, retained init/build/carry/emit call it; byte-identical output; suite green |
| **P2 — IR foundation** | R2 internal modifier/layer model + R7 `GlyphRun` type spike | Everything downstream composes over cleaner assembly/IR; public surface only after proof | Implemented as Feature 140: modifiers/portals proven internally; old nodes lower to new representation; 137 overlay pass reimplemented as portals; public compatibility plan written; goldens re-based + disclosed if pixels change |
| **P3 — Keystone** | R1b retained renderer unification | The clean algebra makes the single fold tractable; kills the drift bug class | Implemented as Feature 141: one `assemble`; `RenderFragment` constructor-private; second builder deleted; fuzz property test green; byte-identical output through the refactor |
| **P4 — Text** | R7 (HarfBuzz shaping) | Independent; unblocks portable text for R5 | Implemented as Feature 142: measured/drawn shaped glyph evidence path; complex-script fixture coverage; pure fallback intact |
| **P5 — Interaction** | R4 (overlay state) | Needs R2 portals + R3 anchoring | Implemented and landed through Feature 145: pure overlay coordinator, transient widget metadata, host/runtime dispatch seams, AntShowcase reference date-picker flow, deterministic corpus parity, unsupported-host disclosure, and real current-run overlay visual proof |
| **P6 — Render-anywhere** | R5 (protocol + server PNG + CanvasKit feasibility) | Needs stable IR (R2) + portable text (R7) | Implemented and landed as Feature 146: deterministic package codec and inspection surface; Skia reference oracle with PNG evidence; package inspection helpers; browser feasibility fallback report; focused/package/build/pack validation passed; squash merge `c0f16ce`; package bump `d62b026` to `0.1.9-preview.1`; full solution remains blocked by unrelated Controls transient-metadata parity failures |
| **P7 — Compositor** | R6 (present-path proof, promotion, scissor, key split, texture) | Needs R2 modifiers; pure perf, gated by probes | Implemented through the Feature149 evidence/readiness package plus Feature152, Feature153, Feature154, and Feature155 live-proof closeout layers: Feature 147 landed as squash merge `85b0b7e`, package bump `2247cb3` to `0.1.10-preview.1`; Feature 148 landed as squash merge `7d708c4`; Feature 149 landed as squash merge `a9a1ef1`; Feature 152 landed as squash merge `8ea61c4`, followed by package bump `61d1ce8` to source packages `0.1.15-preview.1` and template package `0.1.9-preview.1`; Feature 153 landed as squash merge `d7c539c`, followed by source package bump to `0.1.16-preview.1` and template package bump to `0.1.10-preview.1`; Feature 154 landed as squash merge `2c9af24`, with exact proof acceptance, same-profile parity, timing decision, final readiness, compatibility, package, regression, and transcript coverage, followed by source package bump to `0.1.17-preview.1`; Feature 155 accepts current-host P7 correctness with three fresh sentinel/damage proof attempts and same-profile parity on `probe-08a47c01`. Unsupported hosts still fail closed and performance remains `not-accepted`. |
| **P8 — Radical layout** | R3b (intrinsic protocol) | Needs R1b; removes the scrollViewport descendant-walk smell | Accepted through Feature151 on top of Feature150's public protocol: representative corpus breadth, ScrollViewer extent corpus, measured/intrinsic dependency identity, stale-rejection classification, full/incremental parity, broad regression evidence, package validation, and final readiness summary are recorded. Feature155 P7 current-host correctness is accepted separately and is not a P8 claim. |

Each phase is independently shippable and Tier-1-disclosed. P0–P3 are the high-leverage core; P4–P8 are capability expansion in any order their deps allow.

**Feature 140 status (2026-06-17).** P2's internal foundation has landed and is pushed on `main` at
`ac2b560`, followed by package version `0.1.3-preview.1` at `41fb05c`. It adds an assembly-internal Controls
composition model for modifiers, invalidation classification, normalization, local z-order, layer hosts,
portals, legacy lowering, and compatibility evidence. The only public IR delta is the glyph-run proof surface
in `FS.GG.UI.Scene` plus a SkiaViewer proof helper/drawing path. Still deferred: R1b retained unification,
full HarfBuzz shaping/bidi/line breaking, overlay interaction state, portable serialization, compositor work,
and intrinsic layout.

---

## 13. Cross-cutting: parity, determinism, surface/baseline discipline, perf

- **Parity oracles are the safety net, not the mechanism.** Keep full≡retained, cache-on≡cache-off, incremental≡full, and disabled-cache pixel-parity green *unchanged* through every refactor; they catch mistakes while R1 makes them structurally redundant. Add FsCheck **fuzz** property tests (random trees/modifier chains) as the new structural guarantees.
- **Determinism is paramount.** No wall-clock in id minting or caches (preserve). Pin serialization float/enum/order (R5). Same-seed evidence stays byte-identical (the 135/136/137 SC-006 gate).
- **Surface/baseline discipline (Tier-1).** Each IR/public change: `.fsi` first → semantic tests → impl → `scripts/refresh-surface-baselines.fsx` (confirm the diff is *exactly* the intended types) → golden/drift re-baseline with a **disclosure ledger** row per changed baseline → docs + migration note. (This is the workflow feature 137 followed.)
- **Perf must be verified, not assumed.** Per the standing memory note, the cache/compositor mechanisms are *unverified*. Before relying on R6 tiers (or claiming R1 didn't regress), build standalone probes measuring frame cost cache-on vs cache-off and with/without each tier; record results in a report. Promotion/texture tiers ship only with a probe showing net win.
- **Opportunistic fix.** The pre-existing `Feature117MetricsTests` "cold text-heavy frame" failure (6 hits vs 0) should be root-caused and fixed in P0 (likely a measurement-window double-count) so the solution suite is fully green before the big refactors begin.

---

## 14. Top risks & mitigations

| Risk | Severity | Mitigation |
|---|---|---|
| R1 destabilizes the parity machinery | High | Split R1: land R1a as byte-identical shared assembly first, do R2 over that seam, then complete R1b; fuzz property test |
| Public `SceneNode` compatibility breaks downstream consumers | High | Treat R2 as a major compatibility event; keep old constructors/lowering for at least one cycle; publish migration notes and versioning decision; test old-node lowering |
| IR change (R2/R5/R7) ripples to all consumers | High | Internal-first rollout where possible; deprecation cycle (old nodes lower to modifiers); exhaustive-match painter forces the compiler to find every internal site; phase-by-phase Tier-1 re-baseline |
| Modifier order/normalization subtly changes pixels | Med | Property test `render(normalize s) ≡ render s`; document inside-out fold; goldens |
| Canvas2D backend drifts from Skia | Med | Prefer CanvasKit (same Skia) first; Canvas2D only for Core subset, diffed vs server-PNG oracle |
| CanvasKit/WASM path is harder than assumed | Med | Feasibility spike before project commitment; prove packaging, font/resource loading, and host model; fall back to a CanvasKit command stream if direct painter reuse fails |
| Text shaping changes every text golden | Med (expected) | Keep pure heuristic as the no-shaper default; shaper installed only at the rendering edge; disclose re-baselines |
| Damage scissoring corrupts fresh-frame presentation | Med | Add a present-path proof before clipping; require retained backing surface/layer preservation or redraw untouched regions |
| Over-promotion in R6 costs more than it saves | Med | Conservative heuristic (multi-frame stability + node count), demotion, probe-gated |
| Scope/timeline (7 workstreams) | Med | P0–P3 deliver most structural value; P4–P8 are independent capability adds shippable in any dep-valid order |

---

## 15. Decision log

- **Chosen (radical):** R1 unify; R2 modifier algebra + first-class layers/portals; R3 surface layout + intrinsic protocol; R4 interaction/overlay state; R5 portable IR + multi-backend; R6 compositor; R7 HarfBuzz shaping. Rationale: each is the radical variant from the brainstorm, and together they form a compounding program rooted in the one-builder thesis.
- **Sequencing decision (revised):** R1a *before* R2, then R1b after R2. The earlier R2-before-R1 plan made the algebra clean but risked implementing the new rules in both existing assembly paths. The revised sequence removes the dangerous duplication seam first without changing public IR.
- **Deferred:** Cassowary/constraint solver as the *core* layout engine — kept only as an opt-in relational container (Flexbox/Yoga predictability + O(n) wins; constraint solvers are globally coupled and hard to debug). WebGPU/Graphite browser backend (Dawn-on-web immature). A bespoke binary IR before the modifier algebra stabilizes (would churn).
- **Reframed by evidence:** the layout work is *much* cheaper than assumed — `LayoutIntent` + the Yoga binding already implement the full flex model (`Types.fsi`; `Layout.fs:376-411`). `Attr.padding`/`Attr.margin` already exist; `Control.toLayout` ignores them and lacks the rest of the flex mapping. R3a is therefore P0.

---

## 16. Sources

**Offline (this repo @ 8f75594):** `src/Scene/Scene.fs(i)`, `src/SkiaViewer/SceneRenderer.fs`, `src/SkiaViewer/PictureReplayCache.fs`, `src/Controls/Control.fs(i)`, `src/Controls/RetainedRender.fs(i)`, `src/Layout/Types.fsi` + `Layout.fs:376-411`, `tests/surface-baselines/`, `scripts/refresh-surface-baselines.fsx`, `tests/Controls.Tests/Audit_PictureCache.fs`, `tests/Rendering.Harness/TestAssertions.fs`.

**Reconciliation / one-builder:** React Fiber — [acdlite/react-fiber-architecture](https://github.com/acdlite/react-fiber-architecture), [useMemo (react.dev)](https://react.dev/reference/react/useMemo). Compose — [SlotTable explained](https://medium.com/@nikhil.cse16/compose-slot-table-explained-i-read-the-runtime-source-so-you-dont-have-to-98e07c9a8bff), [Recomposition under the hood](https://medium.com/@farimarwat/jetpack-compose-recomposition-and-performance-under-the-hood-fc3a8e254edc). SwiftUI — [Untangling the AttributeGraph](https://rensbr.eu/blog/swiftui-attribute-graph/), [Structural identity (Majid)](https://swiftwithmajid.com/2021/12/09/structural-identity-in-swiftui/). Flutter — [Three trees](https://medium.com/@harshhub.414/understanding-flutters-three-trees-widget-element-and-render-object-5e3f8d840eab). Elm — [elm/virtual-dom](https://github.com/elm/virtual-dom/blob/master/src/VirtualDom.elm), [Caching behind Html.Lazy](https://jfmengels.net/caching-behind-elm-lazy/), [Keyed (Elm guide)](https://guide.elm-lang.org/optimization/keyed.html).

**Modifiers / layout / z-order:** SwiftUI — [modifier order (hackingwithswift)](https://www.hackingwithswift.com/books/ios-swiftui/why-modifier-order-matters), [Layout protocol (SwiftUI Lab)](https://swiftui-lab.com/layout-protocol-part-1/), [zIndex (Sarunw)](https://sarunw.com/posts/swiftui-zindex/). Compose — [custom modifiers / Modifier.Node (Android)](https://developer.android.com/develop/ui/compose/custom-modifiers), [Aug '23 release (~80% win)](https://android-developers.googleblog.com/2023/08/whats-new-in-jetpack-compose-august-23-release.html), [intrinsic measurements](https://developer.android.com/develop/ui/compose/layouts/intrinsic-measurements), [How Compose Measuring Works (Square)](https://developer.squareup.com/blog/how-jetpack-compose-measuring-works/). Flutter — [Understanding constraints](https://docs.flutter.dev/ui/layout/constraints), [Overlay guide](https://blog.logrocket.com/complete-guide-implementing-overlays-flutter/), [flutter_portal](https://github.com/fzyzcjy/flutter_portal). Constraints — [Cassowary (Wikipedia)](https://en.wikipedia.org/wiki/Cassowary_(software)), [Cassowary TOCHI paper](https://constraints.cs.washington.edu/solvers/cassowary-tochi.pdf). [Hit-testing should match painting (W3C)](https://lists.w3.org/Archives/Public/public-css-archive/2019Sep/0332.html).

**Scene IR / compositor / text:** Flutter — [scene_builder.cc](https://github.com/flutter/engine/blob/master/lib/ui/compositing/scene_builder.cc), [layer.dart](https://github.com/flutter/flutter/blob/master/packages/flutter/lib/src/rendering/layer.dart), [Repaint boundaries](https://lazebny.io/repaint-boundary/), [web renderers](https://docs.flutter.dev/platform-integration/web/renderers). Skia — [SkPicture](https://api.skia.org/classSkPicture.html), [SkSerialProcs](https://api.skia.org/SkSerialProcs_8h.html), [CanvasKit](https://skia.org/docs/user/modules/canvaskit/). Chromium cc — [core/paint README](https://chromium.googlesource.com/chromium/src/+/master/third_party/blink/renderer/core/paint/README.md), [How cc Works](https://chromium.googlesource.com/chromium/src/+/master/docs/how_cc_works.md). WebRender — [Rendering Overview](https://firefox-source-docs.mozilla.org/gfx/RenderingOverview.html), [Intro to WebRender pt.1](https://mozillagfx.wordpress.com/2017/09/21/introduction-to-webrender-part-1-browsers-today/). Text — [What HarfBuzz doesn't do](https://harfbuzz.github.io/what-harfbuzz-doesnt-do.html), [Text layout is a loose hierarchy (Raph Levien)](https://raphlinus.github.io/text/2020/10/26/text-layout.html).

---

*End of report. This document chooses the radical options and tracks their delivery status. P0-P6 are implemented and landed through Feature 146 plus package version `0.1.9-preview.1`; P7 has Feature 147's deterministic readiness slice landed on `main` plus package version `0.1.10-preview.1`, Feature 148's focused evidence/readiness layer squash-merged as `7d708c4`, Feature 149 squash-merged as `a9a1ef1` with 68/68 tasks checked, Feature 152 squash-merged as `8ea61c4` with 66/66 tasks checked and packages bumped to `0.1.15-preview.1` / template `0.1.9-preview.1`, Feature 153 squash-merged as `d7c539c` with 68/68 tasks checked, focused Feature153 filters passing, unsupported-host evidence passing in 1s, broad solution validation recorded as interrupted after partial pass output, and packages bumped to `0.1.16-preview.1` / template `0.1.10-preview.1`, Feature 154 squash-merged as `2c9af24` with 70/70 tasks checked, focused Feature154 filters passing, broad solution validation passing locally, unsupported-host evidence passing in approximately 0.6s, and source packages bumped to `0.1.17-preview.1`, and local Feature155 closes current-host P7 correctness with three fresh capable-host sentinel/damage proofs, same-profile parity, focused/package/FSI validation, and broad solution validation passing on retry. Unsupported hosts still fail closed and no P7 performance claim is accepted. P8 is accepted through Feature150 plus Feature151: Feature150 shipped the public intrinsic-layout protocol as squash merge `acad00d`, and Feature151 completed the representative corpus, reuse/parity, regression, compatibility, package, and readiness evidence package as squash merge `6f9d606`, with full solution and package validation passing locally.*
