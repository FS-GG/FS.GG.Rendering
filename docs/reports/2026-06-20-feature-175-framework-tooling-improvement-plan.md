# Framework, Tooling & Skills Improvement Plan — Lessons from Feature 175

- **Report date (UTC):** 2026-06-20T14:11:00Z
- **Author:** Claude Opus 4.8 (1M context) — implementing agent for `175-fix-showcase-controls`
- **Source merge:** `c99a37c` — "175: Fix non-functional controls in the Second Ant Showcase (squash)"
- **Status:** Analysis + proposed implementation plan (no code changes in this report)

---

## Executive summary

Feature 175 made the `SecondAntShowcase` controls actually work under real input (scroll, hover,
focus, toggles). The framework-level math and stamping were verified green by deterministic tests
*before* the live behaviour was correct — i.e. **unit tests proved the logic while the running app
stayed broken**. Every remaining defect lived in places unit tests don't reach: repaint scheduling,
control identity, the click→value boundary, and the package-consuming sample boundary. They were only
found by *running the app* and instrumenting the live loop.

The throughline and the three highest-leverage fixes:

1. **The framework has no general "runtime-state changed → repaint" signal.** One missing
   invalidation channel caused three separate reported bugs (focus-one-click-behind, dead hover, dead
   scroll).
2. **Layout has two divergent consumers** (`boundsById` for paint vs `retained.Layout` for live
   hit-testing); shifting one but not the other produced a bug invisible to tests.
3. **There is no closed-loop "drive an interaction → capture the resulting frame" harness**, so the
   agent had to outsource visual verification to the operator.

This report catalogues each problem with evidence from the session, root cause, impact, and a
concrete implementation plan, then prioritises them into a phased roadmap.

---

## Background

The plan (`specs/175-fix-showcase-controls/plan.md`) hypothesised the defects were in visual-state
*stamping* and *painting*. They were not. After landing the deterministic core (scroll math,
offset-aware bounds, focus ring, combined hover/focus), running the showcase exposed a different set
of real bugs, each diagnosed live:

| Reported live symptom | Actual root cause | Fix location |
|---|---|---|
| "focus on hover" absent; focus invisible on buttons | filled `buttonGeom` ignored the focus stroke | `Control.fs` |
| focus marks ALL nav pages | nav buttons unkeyed → stamp keys by `Key ?? Kind` → all collapse to `"button"` | `Shell.fs` (sample) |
| focus is "one click behind" | viewer only repaints on a model change; runtime state changes don't request a frame | `SkiaViewer.fs` |
| scrollbar does nothing | content height-capped to fit the viewport (sample) + offset only shifted `boundsById`, not the live hit-test layout | `Shell.fs` + `Control.fs`/`ControlsElmish.fs` |
| input registers at pre-scroll positions | live route hit-tests `retained.Layout` (raw), which wasn't offset-shifted | `ControlsElmish.fs` |
| scrolling crawls | raw wheel delta (~3 units/notch) applied 1:1 | `ControlsElmish.fs` |
| switch/checkbox turn off but never on | click dispatch sets `Payload = None`; `onChangedBool` defaults to `false` | `ControlsElmish.fs` |

The remediations shipped, but most point at structural gaps worth fixing at the source.

---

## Part 1 — Framework / library

### F1. No general runtime-state invalidation / repaint trigger  · Severity: **Critical**

- **Evidence (this session):** instrumenting the live loop showed exactly **15 presses → 15 renders**
  — the viewer (`runPresentedPersistentWindow`) only re-derived the scene inside `dispatchHostMsg`
  (i.e. on a product-message-driven model change). `MapPointer`/`MapKey` mutate host-internal refs
  (`focused`, `pointerState.Hover`, `scrollOffsets`) but produce no message, so the scene was stale
  until the next model change. The focus value resolved *correctly every frame* (proven by trace) yet
  appeared one click behind, because the correct frame simply wasn't presented.
- **Root cause:** runtime/interaction state is a first-class input to `host.View` (it reads those
  refs) but has **no path to request a frame**. Repaint is implicitly coupled to product-model change.
- **Impact:** one cause, three reported bugs (focus lag, dead hover, dead scroll); any *future*
  runtime state will reintroduce the class.
- **Shipped mitigation:** after any input that yields no message, re-derive `currentScene <- host.View`
  in `handlePointer`/`handleKey` (`SkiaViewer.fs`). Correct, but a patch local to two handlers in one
  loop.
- **Recommendation / implementation plan:**
  1. Introduce an explicit invalidation channel. Option A (minimal): the host's `MapPointer`/`MapKey`
     return a `RepaintRequested` flag (or the viewer always refreshes after input — current patch
     generalised). Option B (principled): a `dirty` ref the runtime-state mutators set, checked by the
     viewer's present loop; integrate with the existing `RenderLagTrace`/damage machinery so the
     repaint stays damage-local and attributed.
  2. Apply it consistently across **all** viewer loops (there are ≥2: `~3412` key-only and `~3517`
     full-interactive) and the scripted/responsiveness paths, not just the live one.
  3. Add a deterministic regression at the host seam: "a focus/hover/scroll change with no product
     message yields a new presented scene." (`Feature175NavFocusTests` proves the value path; this
     would prove the *presentation* path.)
- **Effort:** M. **Risk:** medium (touches the present loop + render-lag budgets — verify p95 with the
  responsiveness runner).

### F2. Two divergent layout consumers: paint vs live hit-test  · Severity: **High**

- **Evidence:** the scroll-offset shift was applied to `boundsById` (paint + `Control.hitTest`) but
  **not** to the `LayoutResult` the live retained route reads (`retained.Layout` →
  `Layout.hitTestComputed`, `ControlsElmish.fs:667`). Result: content painted scrolled, but clicks/
  hover resolved at pre-scroll positions. Invisible to tests, which used `Control.hitTest`.
- **Root cause:** `evaluateLayout` returns `(root, boundsById, result)` where `boundsById` is derived
  *and transformed* but `result` is raw; paint and `Control.hitTest` consume the former, the live
  route consumes the latter. Two sources of truth for "where is each control."
- **Secondary footgun:** `result` is *also* the incremental-layout cache (`prev.Layout`,
  `RetainedRender.fs:1485`), so it cannot be pre-shifted without **double-shifting** reused bounds
  each frame.
- **Shipped mitigation:** keep `boundsById` shifted; thread/store raw `result`; the host re-applies
  `ControlInternals.applyScrollOffsets` to `retained.Layout` at hit-test time (idempotent).
- **Recommendation / implementation plan:**
  1. Make offset-awareness a property of a **single** layout abstraction that both paint and hit-test
     consume; keep "the incremental cache" as a distinct value from "the queryable layout."
  2. Failing that (smaller): centralise hit-testing so every consumer (live route, `Control.hitTest`,
     `resolveFocus`) goes through one offset-aware function — no caller can forget the shift.
  3. Add a regression that drives the **live** route (`routeRetainedPointer`) over scrolled content
     and asserts the resolved control, not just `Control.hitTest`.
- **Effort:** M. **Risk:** medium (parity-gated render pipeline).

### F3. Click→value computation special-cased per control kind, with gaps  · Severity: **High**

- **Evidence:** `bindingMessagesFor` dispatches a click with `Payload = None`
  (`ControlsElmish.fs:479`). The slider has bespoke `sliderChangedMessages` to compute its value;
  boolean toggles had nothing, so `onChangedBool` (`Payload |> Option.exists ((=) "true")`) always
  produced `false` — switch/checkbox could be turned off but never on.
- **Root cause:** no uniform "the control reports its activated value on click" protocol. Each kind
  that needs a value is hand-coded; toggles were simply missing.
- **Shipped mitigation:** `toggleChangedMessages` reads the control's current `selected` and dispatches
  `not current` (mirrors the slider). Covers `switch`/`check-box`.
- **Recommendation / implementation plan:**
  1. Define an **activation-value contract**: a control kind optionally declares how a click computes
     its `changed` payload from (current attributes, click position). Slider, toggle, segmented,
     rate, etc. register; `bindingMessagesFor` consults the registry instead of `if kind = …`.
  2. Audit every value-bearing control for the same gap (segmented, rate, color-picker were not
     live-tested this session).
  3. Unify the toggle authoring story (see F6) so widgets don't each invent a value path.
- **Effort:** M. **Risk:** low–medium (dispatch parity tests will guard — one needed updating this
  session, `Feature090`).

### F4. Visual-state stamping identity ≠ routing identity  · Severity: **High**

- **Evidence:** focus/hover stamp by `Key ?? Kind` (`ControlRuntime.deriveVisualState` /
  `applyRuntimeVisualState`). The unkeyed nav buttons all collapse to id `"button"`, so focusing one
  marked **all** of them. Routing, by contrast, uses stable `RetainedId` (Feature 110) and
  distinguishes them correctly.
- **Root cause:** two identity schemes for the same tree — the stamp uses authored/kind identity, the
  router uses retained positional identity.
- **Shipped mitigation:** keyed the nav buttons in the sample (`nav-<pageId>`). Works, but every
  unkeyed same-kind interactive sibling in any product hits the same trap.
- **Recommendation / implementation plan:**
  1. Stamp visual state by the **same RetainedId/positional identity** the router resolves, so
     unkeyed siblings are distinguished without authoring keys.
  2. Or, lower-effort: a diagnostic (build-time or render-time) that warns when interactive
     same-kind siblings are unkeyed, since the failure is silent today.
- **Effort:** L (diagnostic) / L–XL (unify identity). **Risk:** XL option is high (touches the
  byte-identity-gated stamp); the diagnostic is cheap and high-value.

### F5. No scroll *behaviour*, only scroll *painting*  · Severity: **Medium-High**

- **Evidence:** the framework painted a scroll affordance (Features 137/150) but owned **no** offset
  state, content translation, thumb tracking, clamp, dead-zone, or offset-aware hit-testing. US1
  implemented all of it from scratch across `Types.fs`, `ControlRuntime.fs`, `Control.fs`,
  `Pointer.fs`, and the host.
- **Impact:** large surface area for one capability every product needs; easy to wire inconsistently.
- **Recommendation / implementation plan:** promote a first-class `ScrollViewer` behaviour that owns
  offset + clamp + thumb + clip + offset-aware hit-test as one unit, so products inherit correct
  scrolling and the offset/hit-test consistency (F2) is structural, not per-feature. The `ScrollState`
  type + `applyScrollDelta` shipped here are the seed.
- **Effort:** M–L. **Risk:** medium.

### F6. Inconsistent toggle authoring + un-normalised wheel deltas  · Severity: **Low-Medium**

- **`ToggleButton`** bakes `Button.onClick (map (not IsOn))` at *view* time (works under full render,
  fragile under retained reuse), while **`Switch`/`CheckBox`** use a payload-driven `onChanged`. Two
  patterns for the same concept. Plan: one "toggle reports new value" contract (ties into F3).
- **Wheel deltas** arrive ~3 units/notch and were applied 1:1 (crawl). Shipped a `wheelScrollStep`
  multiplier in the host; better: the **viewer** should normalise wheel deltas to pixels (or expose a
  line-height step) so each consumer doesn't reinvent it.
- **Effort:** S each. **Risk:** low.

---

## Part 2 — Tooling & developer loop

### T1. The package-consuming-sample repack dance  · Severity: **High (friction)**

- **Evidence:** testing *any* framework change live required bump version → `dotnet pack` → retarget
  sample pins → restore — performed ~6 times this session, hitting: NU1605 downgrades, the
  "Tests project also pins the old version" gotcha, cross-sample landmines (AntShowcase pins
  `Scene 0.1.32` vs source `0.1.36`), and manual version juggling across 4 inter-dependent packages.
- **Recommendation / implementation plan:**
  1. An idempotent **dev-repack** command: "pack the touched `FS.GG.UI.*` projects to the local feed
     at a fresh dev version and retarget the touched sample's pins (Core/App/**Tests**), then
     restore" — one invocation. `scripts/refresh-local-feed-and-samples.fsx` is the starting point;
     it should handle the multi-project version bump + all three project files atomically.
  2. Surface the cross-sample consistency rule (all consumers of a bumped package move together) so
     a partial bump can't silently break a sibling sample's restore.
- **Effort:** M. **Risk:** low.

### T2. Two surface-baseline locations  · Severity: **Medium**

- **Evidence:** `scripts/refresh-surface-baselines.fsx` writes `tests/surface-baselines/`, but the
  gate (`SurfaceAreaTests`) reads `readiness/surface-baselines/`. Time was lost updating the wrong
  one.
- **Plan:** make the refresh script write the authoritative location (or collapse to one); the gate
  and the refresh must agree by construction.
- **Effort:** S. **Risk:** low.

### T3. Baseline run wasn't comprehensive  · Severity: **Medium**

- **Evidence:** the pre-change baseline ran 4 test projects + the Surface *filter*, missing full
  `tests/Package.Tests` — so its 8 pre-existing failures (Feature128 missing report, Feature163 stale
  AntShowcase pins) surprised the merge step.
- **Plan:** the "establish baseline" task should run **every** test project and record the full
  red/green set, so pre-existing failures are known up front and not mistaken for regressions.
- **Effort:** S. **Risk:** none.

---

## Part 3 — Spec Kit / process

### P1. The plan's bug hypotheses were wrong; only running found the real ones  · Severity: **High**

- **Evidence:** tasks.md assumed stamping/painting defects; the real bugs were the repaint trigger
  (F1), unkeyed siblings (F4), and the toggle payload (F3) — none in the plan. They were found only by
  running the app, late.
- **Plan:** add a **live exploratory smoke run as an early Foundational task** (right after the
  classification/root-cause map), where the actual app is driven and observed before building on the
  plan's assumptions. The Feature-168 evidence rules already *require* live evidence — pull it
  forward instead of deferring to US-end (T020/T028/T039). Treat "the plan's root-cause hypotheses
  are unverified until the app is run" as a standing assumption.
- **Effort:** S (process). **Risk:** none.

---

## Part 4 — Agent tools & skills

### S1. No "drive interaction → capture resulting frame" harness  · Severity: **Critical (for autonomy)**

- **Evidence:** the agent could screenshot *static* pages (`visual-readiness`) and drive *scripted
  input* (`responsiveness`), but not "click here, then screenshot the result" — so visual
  confirmation of every fix was outsourced to the operator (who reasonably asked "why can't you test
  these yourself?").
- **Plan:** compose the existing pieces (`runInteractiveViewerScript` + offscreen readback) into a
  skill: **script a pointer/key sequence, capture the final PNG, return it for the agent to view.**
  This closes the loop so interaction bugs are self-verifiable end-to-end. Single highest-value
  tooling change for agent autonomy on this codebase.
- **Effort:** M. **Risk:** low (pieces exist).

### S2. No reusable interaction-reproduction test helper  · Severity: **Medium**

- **Evidence:** host reconstructions for nav-focus and toggle were hand-assembled each time
  (`routeRetainedPointer`, `RetainedRender` are internal), slowly.
- **Plan:** ship a test harness — "drive a click/key through the real host; return dispatched msgs +
  focus + scene" — so reproducing a live bug deterministically (and turning it into a permanent
  regression) is a few lines. `Feature175NavFocusTests`/`Feature175ToggleTests` are templates to
  generalise.
- **Effort:** S–M. **Risk:** low.

### S3. Hand-rolled tracing instead of structured live trace  · Severity: **Low-Medium**

- **Evidence:** diagnosing the focus lag required adding env-gated `eprintfn` traces to the host and
  **repacking** to observe.
- **Plan:** extend the existing `RenderLagTrace` to cover focus/hover/scroll resolution and binding
  dispatch, with a read-back path, so live state is observable without a repack-to-instrument loop.
- **Effort:** S–M. **Risk:** low.

---

## Prioritisation

| ID | Item | Severity | Effort | Leverage |
|----|------|----------|--------|----------|
| F1 | General repaint/invalidation signal | Critical | M | One fix kills 3 bug classes; prevents recurrence |
| S1 | Drive-interaction-and-screenshot skill | Critical | M | Agent self-verifies interactions |
| F2 | Single offset-aware layout for paint+hit-test | High | M | Kills a test-invisible bug class |
| F3 | Uniform click→activation-value contract | High | M | Toggles/segmented/etc. correct by construction |
| F4 | Stamp identity == routing identity (or warn) | High | L–XL | Unkeyed-sibling bleed gone |
| P1 | Early live smoke run in the plan | High | S | Finds real bugs before building on wrong assumptions |
| T1 | Idempotent dev-repack command | High | M | Removes the biggest iteration friction |
| F5 | First-class ScrollViewer behaviour | Med-High | M–L | Products inherit correct scrolling |
| S2 | Interaction-reproduction test helper | Medium | S–M | Faster repro→regression |
| T2 | One surface-baseline location | Medium | S | Removes a footgun |
| T3 | Comprehensive baseline run | Medium | S | No mid-merge surprises |
| F6 | Toggle authoring + wheel normalisation | Low-Med | S | Consistency |
| S3 | Structured live tracing | Low-Med | S–M | Observe without repack |

## Suggested phased roadmap

- **Phase A — stop the bleeding (process + cheap, no code risk):** P1 (early smoke run), T2, T3, T1
  (dev-repack), S1 (interaction-screenshot skill), S2 (repro helper). These remove friction and let
  the next feature *find* live bugs early and self-verify.
- **Phase B — structural framework fixes:** F1 (invalidation channel) — promote the shipped patch to a
  real dirty/damage signal across all loops; F2 (single offset-aware layout); F3 (activation-value
  contract). Each ships with the regression tests this session prototyped.
- **Phase C — consolidation:** F4 (unify stamp/routing identity, starting with the diagnostic), F5
  (first-class ScrollViewer), F6, S3.

## Appendix — evidence anchors (merge `c99a37c`)

- Repaint trigger: `src/SkiaViewer/SkiaViewer.fs` (`handlePointer`/`handleKey`, "general repaint
  trigger"); 15-press/15-render trace finding.
- Offset-aware live hit-test: `src/Controls/Control.fs` (`applyScrollOffsets`, `evaluateLayout`),
  `src/Controls/Control.fsi` (internal `ControlInternals.applyScrollOffsets`),
  `src/Controls.Elmish/ControlsElmish.fs` (`hitLayout`).
- Toggle: `src/Controls.Elmish/ControlsElmish.fs` (`toggleChangedMessages`),
  `tests/Elmish.Tests/Feature175ToggleTests.fs`; parity update in `Feature090DispatchTests.fs`.
- Identity / keying: `samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fs` (`nav-<pageId>`).
- Scroll core: `Types.fs` (`ScrollState`), `ControlRuntime.fs`, `Pointer.fs` (`scrollKeyDelta`),
  `tests/Controls.Tests/Feature175ScrollStateTests.fs`, `tests/Elmish.Tests/Feature175ScrollRoutingTests.fs`.
- Pre-existing tech debt (out of scope, flagged): `tests/Package.Tests` Feature128 (missing
  gitignored design-system-template report) and Feature163 (stale AntShowcase pins).
