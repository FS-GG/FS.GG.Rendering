# Radical Rendering-Architecture Analysis & Implementation Plan

| | |
|---|---|
| **Authored** | 2026-06-17 13:54 CEST (2026-06-17T11:54Z) |
| **Author** | Claude Opus 4.8 (1M context), with four parallel research agents (offline codebase map + 3 online prior-art deep-dives) |
| **Repo state** | branch `main` @ `8f75594` (feature 137 merged: container clipping + overlay pass + scroll viewport) |
| **Scope** | The **radical** framework options only (per request), grounded in offline code reading and online prior art (React Fiber, Jetpack Compose, SwiftUI/AttributeGraph, Flutter, Elm, WebRender, Chromium `cc`, Skia, HarfBuzz) |
| **Status** | Analysis + plan. No code changed by this document. |

---

## 0. How to read this

This is two documents in one: an **analysis** of where FS.GG.Rendering actually is (grounded in the live code, with `file:line` anchors), and a **comprehensive implementation plan** for the radical bets, sequenced into phases with change-sites, parity oracles, risks, and exit criteria. Sources for every external claim are in §10.

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

Therefore the radical program below is organized so that the **first deep refactor collapses the two paths into one**, and the others (a modifier algebra, first-class layers/portals, a portable IR, multi-backend rendering, real text, a real compositor, real interaction) are built on top of that single, now-trustworthy producer. The payoff compounds: every subsequent capability is added once, in one place, with parity guaranteed by construction rather than chased by tests.

---

## 2. The chosen radical workstreams

Selected (the radical options):

| # | Workstream | One-line radical goal | Depends on |
|---|---|---|---|
| **R1** | Unify the renderer | Delete the second builder; one pure `assemble` + a generic memo/reconcile layer; fragments constructor-private so a second producer is a *compile error* | — (enabled by R2) |
| **R2** | Modifier algebra + layers/portals | Clip/opacity/offset/z/cache become composable *modifiers*; z-order and out-of-tree portals become first-class Scene-IR concepts | — |
| **R3** | Layout model + intrinsic protocol | Surface the already-wired flex model as attributes; add a constraints-down/sizes-up + intrinsic-size protocol | — (R3a), R1 (R3b) |
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
- **The only gap:** `Control.toLayout` reads just `width`/`height`/`orientation`, and `layoutAffectingAttrNames = {width,height,orientation}`. So **the entire flex model is one mapping function away from being authorable.** (This is why T027 dead-ended last session — `flexShrink 0` on the shell bands would have pinned them; the machinery was there, just unexposed.)

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

**Goal.** One pure producer of scene structure; the retained renderer becomes a generic memoization/reconciliation layer over it. Full ≡ retained and cache-on ≡ cache-off become **structural properties**, not test-chased invariants.

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
- New `src/Controls/Assemble.fs(i)` — the single `assemble` + `AssembleCtx` (theme, boundsById). Move the body of `paintNode`/`composeContainerScene`/overlay split here as the one composition.
- `Control.renderTree` → "reconcile with a cold cache" (thin wrapper over the applier).
- `RetainedRender.fs` — `init`/`step` reduce to: diff (`Reconcile.diff`, kept), then `reconcileChild` walk that calls `memoize`/`assemble`; **delete** the parallel `build`/`buildFresh`/`carry`/`assemble`-emit composition logic. `RenderFragment` constructor → private.
- Keep all existing cache plumbing (memo/picture/text) — it becomes the *inputs* to `memoize`, not a separate code path.
- Tests: the parity suites stay but are downgraded to "redundant invariant checks"; add an FsCheck property test fuzzing random trees asserting `assemble(tree) ≡ reconcile(cold) ≡ reconcile(warm)`.

**Invariants/oracles.** All existing parity oracles must stay green *unchanged* through the refactor (they are the safety net). Add the property test as the new structural guarantee.

**Risks.** Highest-risk workstream (touches the parity machinery). Mitigation: land it as a *pure refactor* with byte-identical output first (the 757-test suite + goldens are the oracle), *then* simplify. Do it after R2 so the single fold is clean.

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
- Surface baseline + rebaseline ledger (Tier-1): new public `Modifier`/`Portal`/`LayerId` types; deprecated node kinds.

**Invariants/oracles.** "Hit matches paint" property test (in-tree + cross-layer). Empty-layer scene ≡ pre-R2 (the 137 empty-overlay invariant generalized). Normalization is output-preserving (property test: `render(normalize s) ≡ render s`).

**Risks.** IR change ripples to every consumer; mitigate with the deprecation cycle (old kinds lower to modifiers) and the exhaustive-match painter (compiler finds every site). **Do R2 before R1** so R1's single fold is over the clean algebra.

**Effort.** L.

---

## 7. Workstream R3 — Layout: surface the model + intrinsic protocol

**R3a (quick win, low risk, do first).** Surface the already-wired flex model as attributes. *Zero* Layout/Yoga changes — purely the `Control` boundary.
- `src/Controls/Attributes.fs(i)`: add `flexGrow, flexShrink, flexBasis, alignSelf, alignItems, justifyContent, gap, padding, margin, minWidth/Height, maxWidth/Height` builders.
- `Control.toLayout`: map each attribute into the corresponding `LayoutIntent` field (the fields and Yoga wiring already exist — `Types.fsi` + `Layout.fs:376-411`).
- `layoutAffectingAttrNames`: extend the set so incremental layout invalidates correctly.
- Immediately fixes the T027 shell-chrome problem (`flexShrink 0` pins the bands) and a whole class of "Yoga shrank my fixed child" surprises.

**R3b (radical, after R1).** Add the constraints-down/sizes-up + intrinsic-size protocol (Flutter/Compose/SwiftUI convergence) so containers like `ScrollViewer` stop walking descendants to discover content height.
- An `ILayout`-style protocol: `Measure(constraints, children, cache) -> Size`; `Place(bounds, children, cache)`; `Min/MaxIntrinsicWidth/Height`. **Single measure per pass**; intrinsics are the only legal "measure twice." A `LayoutCache` persists across measure/place and invalidations.
- Keep Yoga as the default flex container behind this protocol; add an opt-in relational (`ConstraintLayout`/Cassowary) container *only* as a specialized escape hatch (predictability/O(n) stays the default).

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
2. **CanvasKit / Skia-WASM** (highest fidelity, least new code): recompile the `Scene → SkCanvas` painter to WASM; identical effects; ~1.5 MB.
3. **Canvas2D** (optional, size-driven): hand-written interpreter for the Core subset; GPU-tier nodes get software fallback/diagnostic.
4. WebGPU/Graphite — deferred.

**Change-sites.** New `src/Scene/SceneCodec.fs(i)` (`toProto`/`fromProto`/`compare`, version, capability tags); `Image`→`ImageRef` + resource table across Scene + painter; new backend projects (`src/SkiaViewer.Wasm`, server-raster path in `SceneEvidence`). Surface baseline + ledger.

**Invariants/oracles.** Round-trip `fromProto∘toProto = id` (property test); identical scenes serialize byte-identically; each backend diffed pixel-wise against the server-PNG oracle within tolerance.

**Effort.** XL (multi-phase). Depends on R2 (stable IR) + R7 (`GlyphRun`).

---

## 10. Workstream R6 — Compositor: layer promotion + damage-driven redraw

**Goal.** Turn the existing `SkPicture` replay cache + damage set into a real compositor: damage-scissored frame paint, generalized layer promotion, content/transform key separation, and a texture tier — partial redraw instead of full re-walk.

**Design (in dependency order, each preserving the disabled-cache parity oracle).**
1. **Damage-scissored frame paint** (biggest win from data you already have): set the frame canvas clip to the existing **union damage rect**, then replay. Untouched cached pictures outside the clip cost nothing.
2. **Generalize boundary selection** beyond data-grid rows: a Flutter-style promotion heuristic — promote a subtree to a `CacheBoundary` when its fingerprint was stable over the last *N* frames (you already track prior-frame stability, FR-012) *and* node-count exceeds a threshold; **demote** boundaries whose fingerprint churns every frame (pure cost).
3. **Split the boundary key into content-fingerprint + placement-transform** (the Chromium `cc` property-tree lesson): a subtree that only *moves*/scrolls re-blits at a new offset instead of re-recording. Highest-leverage IR change for compositor efficiency; composes with R2's `Transform` modifier.
4. **Texture-promotion tier** above SkPicture replay: for stable-but-expensive boundaries (blurs/shadows/large paths), snapshot to an `SKImage` and `DrawImage`; same `CacheId`/`Fingerprint` key (the Flutter `EngineLayer` analog). Choose tier by recorded op-count/cost.

**Change-sites.** `src/SkiaViewer/PictureReplayCache.fs` (scissor, texture tier); `RetainedRender.fs` (promotion heuristic, content/transform key split, damage→scissor plumbing); `WorkReductionRecord` counters for promotion/demotion/texture hits.

**Invariants/oracles.** Disabled-cache path stays pixel-identical to the direct walk for **every** new tier (the existing FR-011 oracle, extended). Perf probes (see §13) prove each tier actually pays off before relying on it.

**Effort.** L. Depends on R2 (`Transform`/`CacheBoundary` modifiers).

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

---

## 12. Sequenced roadmap, dependencies & milestones

Dependency graph (→ = "depends on"): R1→R2; R3b→R1; R4→R2,R3; R5→R2,R7; R6→R2. R3a, R7 are near-independent.

| Phase | Workstream(s) | Why here | Exit criteria |
|---|---|---|---|
| **P0 — Quick win** | R3a (layout attrs) + fix the pre-existing Elmish metrics test | Zero-risk, unblocks T027, immediate authoring value | Flex attrs authorable; T027 shell chrome clean; Elmish suite green |
| **P1 — IR foundation** | R2 (modifier algebra + layers/portals) + R7 `GlyphRun` node only | Everything downstream composes over the clean algebra/IR; do before R1 | Modifiers/portals shipped; old nodes lower to modifiers; 137 overlay pass reimplemented as portals; all goldens re-based + disclosed; suite green |
| **P2 — Keystone** | R1 (unify renderer) | The clean algebra makes the single fold tractable; kills the drift bug class | One `assemble`; `RenderFragment` constructor-private; second builder deleted; fuzz property test green; byte-identical output through the refactor |
| **P3 — Text** | R7 (HarfBuzz shaping) | Independent; unblocks portable text for R5 | Measured==drawn; complex scripts render; pure fallback goldens intact |
| **P4 — Interaction** | R4 (overlay state) | Needs R2 portals + R3 anchoring | Dropdowns open/close/dismiss/focus-trap; deterministic; reference: AntShowcase date-picker |
| **P5 — Render-anywhere** | R5 (protocol + server PNG + CanvasKit) | Needs stable IR (R2) + portable text (R7) | Round-trip codec; server-PNG oracle; CanvasKit renders the showcase pixel-matching the oracle |
| **P6 — Compositor** | R6 (promotion, scissor, key split, texture) | Needs R2 modifiers; pure perf, gated by probes | Damage-scissored frames; promotion heuristic; scroll re-blits; parity oracle holds per tier; probes show net win |
| **P7 — Radical layout** | R3b (intrinsic protocol) | Needs R1; removes the scrollViewport descendant-walk smell | Constraints/intrinsics protocol; ScrollViewer reimplemented; incremental≡full preserved |

Each phase is independently shippable and Tier-1-disclosed. P0–P2 are the high-leverage core; P3–P7 are capability expansion in any order their deps allow.

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
| R1 destabilizes the parity machinery | High | Do R2 first (clean fold); land R1 as byte-identical pure refactor gated by the full suite+goldens, then simplify; fuzz property test |
| IR change (R2/R5/R7) ripples to all consumers | High | Deprecation cycle (old nodes lower to modifiers); exhaustive-match painter forces the compiler to find every site; phase-by-phase Tier-1 re-baseline |
| Modifier order/normalization subtly changes pixels | Med | Property test `render(normalize s) ≡ render s`; document inside-out fold; goldens |
| Canvas2D backend drifts from Skia | Med | Prefer CanvasKit (same Skia) first; Canvas2D only for Core subset, diffed vs server-PNG oracle |
| Text shaping changes every text golden | Med (expected) | Keep pure heuristic as the no-shaper default; shaper installed only at the rendering edge; disclose re-baselines |
| Over-promotion in R6 costs more than it saves | Med | Conservative heuristic (multi-frame stability + node count), demotion, probe-gated |
| Scope/timeline (7 workstreams) | Med | P0–P2 deliver most structural value; P3–P7 are independent capability adds shippable in any dep-valid order |

---

## 15. Decision log

- **Chosen (radical):** R1 unify; R2 modifier algebra + first-class layers/portals; R3 surface layout + intrinsic protocol; R4 interaction/overlay state; R5 portable IR + multi-backend; R6 compositor; R7 HarfBuzz shaping. Rationale: each is the radical variant from the brainstorm, and together they form a compounding program rooted in the one-builder thesis.
- **Sequencing decision:** R2 *before* R1 (the algebra makes the single fold clean), against the naive "unify first" instinct.
- **Deferred:** Cassowary/constraint solver as the *core* layout engine — kept only as an opt-in relational container (Flexbox/Yoga predictability + O(n) wins; constraint solvers are globally coupled and hard to debug). WebGPU/Graphite browser backend (Dawn-on-web immature). A bespoke binary IR before the modifier algebra stabilizes (would churn).
- **Reframed by evidence:** the layout work is *much* cheaper than assumed — `LayoutIntent` + the Yoga binding already implement the full flex model (`Types.fsi`; `Layout.fs:376-411`); only the `Control.toLayout` attr-mapping is missing. R3a is therefore P0.

---

## 16. Sources

**Offline (this repo @ 8f75594):** `src/Scene/Scene.fs(i)`, `src/SkiaViewer/SceneRenderer.fs`, `src/SkiaViewer/PictureReplayCache.fs`, `src/Controls/Control.fs(i)`, `src/Controls/RetainedRender.fs(i)`, `src/Layout/Types.fsi` + `Layout.fs:376-411`, `tests/surface-baselines/`, `scripts/refresh-surface-baselines.fsx`, `tests/Controls.Tests/Audit_PictureCache.fs`, `tests/Rendering.Harness/TestAssertions.fs`.

**Reconciliation / one-builder:** React Fiber — [acdlite/react-fiber-architecture](https://github.com/acdlite/react-fiber-architecture), [useMemo (react.dev)](https://react.dev/reference/react/useMemo). Compose — [SlotTable explained](https://medium.com/@nikhil.cse16/compose-slot-table-explained-i-read-the-runtime-source-so-you-dont-have-to-98e07c9a8bff), [Recomposition under the hood](https://medium.com/@farimarwat/jetpack-compose-recomposition-and-performance-under-the-hood-fc3a8e254edc). SwiftUI — [Untangling the AttributeGraph](https://rensbr.eu/blog/swiftui-attribute-graph/), [Structural identity (Majid)](https://swiftwithmajid.com/2021/12/09/structural-identity-in-swiftui/). Flutter — [Three trees](https://medium.com/@harshhub.414/understanding-flutters-three-trees-widget-element-and-render-object-5e3f8d840eab). Elm — [elm/virtual-dom](https://github.com/elm/virtual-dom/blob/master/src/VirtualDom.elm), [Caching behind Html.Lazy](https://jfmengels.net/caching-behind-elm-lazy/), [Keyed (Elm guide)](https://guide.elm-lang.org/optimization/keyed.html).

**Modifiers / layout / z-order:** SwiftUI — [modifier order (hackingwithswift)](https://www.hackingwithswift.com/books/ios-swiftui/why-modifier-order-matters), [Layout protocol (SwiftUI Lab)](https://swiftui-lab.com/layout-protocol-part-1/), [zIndex (Sarunw)](https://sarunw.com/posts/swiftui-zindex/). Compose — [custom modifiers / Modifier.Node (Android)](https://developer.android.com/develop/ui/compose/custom-modifiers), [Aug '23 release (~80% win)](https://android-developers.googleblog.com/2023/08/whats-new-in-jetpack-compose-august-23-release.html), [intrinsic measurements](https://developer.android.com/develop/ui/compose/layouts/intrinsic-measurements), [How Compose Measuring Works (Square)](https://developer.squareup.com/blog/how-jetpack-compose-measuring-works/). Flutter — [Understanding constraints](https://docs.flutter.dev/ui/layout/constraints), [Overlay guide](https://blog.logrocket.com/complete-guide-implementing-overlays-flutter/), [flutter_portal](https://github.com/fzyzcjy/flutter_portal). Constraints — [Cassowary (Wikipedia)](https://en.wikipedia.org/wiki/Cassowary_(software)), [Cassowary TOCHI paper](https://constraints.cs.washington.edu/solvers/cassowary-tochi.pdf). [Hit-testing should match painting (W3C)](https://lists.w3.org/Archives/Public/public-css-archive/2019Sep/0332.html).

**Scene IR / compositor / text:** Flutter — [scene_builder.cc](https://github.com/flutter/engine/blob/master/lib/ui/compositing/scene_builder.cc), [layer.dart](https://github.com/flutter/flutter/blob/master/packages/flutter/lib/src/rendering/layer.dart), [Repaint boundaries](https://lazebny.io/repaint-boundary/), [web renderers](https://docs.flutter.dev/platform-integration/web/renderers). Skia — [SkPicture](https://api.skia.org/classSkPicture.html), [SkSerialProcs](https://api.skia.org/SkSerialProcs_8h.html), [CanvasKit](https://skia.org/docs/user/modules/canvaskit/). Chromium cc — [core/paint README](https://chromium.googlesource.com/chromium/src/+/master/third_party/blink/renderer/core/paint/README.md), [How cc Works](https://chromium.googlesource.com/chromium/src/+/master/docs/how_cc_works.md). WebRender — [Rendering Overview](https://firefox-source-docs.mozilla.org/gfx/RenderingOverview.html), [Intro to WebRender pt.1](https://mozillagfx.wordpress.com/2017/09/21/introduction-to-webrender-part-1-browsers-today/). Text — [What HarfBuzz doesn't do](https://harfbuzz.github.io/what-harfbuzz-doesnt-do.html), [Text layout is a loose hierarchy (Raph Levien)](https://raphlinus.github.io/text/2020/10/26/text-layout.html).

---

*End of report. This document chooses the radical options and plans them; it changes no code. Recommended first action: P0 (surface the flex layout attributes + fix the pre-existing Elmish metrics test), then P1 (the modifier-algebra IR refactor) as the foundation for the keystone renderer unification (P2).*
