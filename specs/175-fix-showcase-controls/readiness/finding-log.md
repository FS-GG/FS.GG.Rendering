# Control-Pass Finding Log — 175-fix-showcase-controls (T005)

Per the `Finding` entity (data-model.md). Lifecycle: `Open → Fixed → ReVerified`. The feature is
accepted only when **zero findings remain not-`ReVerified`** (SC-005) and every control is
classified (SC-007). `FixTier`: `Tier1` = shared `FS.GG.UI.*` control surface; `Tier2` =
sample-local wiring.

Schema: `FindingId | ControlIds | PageId | Symptom | RootCause | FixTier | Status | Verification`.

## Phase 0/2 root-cause map (Open) — across the 13 interaction families

Code references confirmed against the current tree during Foundational (paths/lines may shift as
fixes land).

### F-001 — Content region does not scroll (FR-001/FR-002/FR-009) · Tier1
- ControlIds: `scroll-viewer` (kind) / `content-scroll` (sample binding) · Page: all (shell content)
- Symptom: dragging the scrollbar, wheeling over the region, and scroll keys do nothing; the thumb
  sits at the top; content below the fold is unreachable.
- RootCause (confirmed):
  1. No scroll-offset state exists. `ControlRuntimeMsg` (ControlRuntime.fsi:69) has no scroll case
     and `ControlRuntimeModel` owns no per-`scroll-viewer` offset. `Pointer.update` already emits
     `Scroll(control, dx, dy, x, y)` (Pointer.fs:242) but nothing consumes it into an offset.
  2. `Control.scrollAffordance` (Control.fs:1501) paints the thumb at `box.Y` (pinned top); thumb
     position is not derived from an offset.
  3. Content is not translated by `-offset` / re-clipped on scroll.
  4. `Layout.hitTestComputed` is not offset-aware for `scroll-viewer` descendants (FR-009).
- Fix (landed, verified by tests): `ScrollState` value + pure transition (`applyScrollDelta` clamp,
  thumb height/position, dead-zone) in `Types.fs`; `ScrollOffsets`/`ScrollControl`/`SetScrollExtent`
  in `ControlRuntime`; the `boundsById` offset transform in `Control.evaluateLayout(Incremental)` so
  **paint AND hit-test both read the shifted bounds** (FR-009, offset-aware hit-test confirmed); the
  thumb now tracks the live offset and is omitted when content fits (FR-002); `Pointer.scrollKeyDelta`
  maps the enumerated scroll keys. Byte-identical at offset 0 (no regression: 918 Controls tests +
  surface gate green). Tests: `Feature175ScrollStateTests` (16 cases — clamp, thumb, dead-zone,
  offset-aware hit-test invariant, scroll keys).
- Live wiring (landed + verified): the host (`ControlsElmish`) now persists per-`scroll-viewer`
  offsets (`scrollOffsets` ref), resolves the enclosing scroll-viewer + extent for each `Scroll`
  interaction (`routeRetainedPointer` → `resolveScrollDeltas`), advances the clamped offset
  (wheel-down → positive), carries it into the runtime model, and stamps it onto the tree via
  `ControlRuntime.applyScrollOffsets` before the retained step (identity at rest). T011
  (`Feature175ScrollRoutingTests`, 2): a wheel resolves to `(content-scroll, delta, contentH, viewportH)`
  and applying the offset changes the frame damage-locally (chrome sibling reused). T012: covered by
  existing `PointerInteractionTests` (WheelMsg→Scroll) + host tests. T018: no Shell change needed —
  the showcase content region is already keyed `content-scroll`; the host scrolls any keyed
  scroll-viewer. Full suite green (Controls 919, Elmish 200, SkiaViewer 202, surface gate 32).
- Remaining: **T020 live GUI evidence** — the package-consuming showcase must be repacked against the
  new framework before a real desktop scroll capture (the repack happens at the merge/polish step);
  deferred there rather than faked. The behavior is deterministically proven (T008–T011).
- FixTier: Tier1 · Status: **Fixed** (live wiring landed + deterministically verified; `ReVerified`
  pending the post-repack live GUI capture, T020)

### F-002 — No live hover feedback on interactive controls (FR-003) · Tier1
- ControlIds: all interactive kinds · Page: all
- Symptom: pointer-over shows no Ant hover state.
- RootCause (hypothesis to confirm in US2): `deriveVisualState` already maps
  `HoveredControl → Hover` and `applyRuntimeVisualState` stamps it **generically for every kind**
  (ControlRuntime.fs:219/249) — so per-kind stamping is NOT the gap. The break is in (a) the live
  retained repaint not being triggered on `HoverControl` (hover-enter/leave), and/or (b) per-kind
  `*Geom` painters not resolving a visible Hover delta via `Style.resolve`.
- FixTier: Tier1 · Status: Open

### F-003 — No live focus feedback on interactive controls (FR-004) · Tier1
- Symptom: keyboard focus shows no distinct affordance; affordance does not move with focus.
- RootCause: same shape as F-002 for `FocusControl`/`FocusChanged`; confirm the focus-change
  retained repaint trigger and that `Focused` resolves a visible per-kind delta distinct from Hover.
- FixTier: Tier1 · Status: Open

### F-004 — `ghost` nav buttons show no hover/focus (FR-003/FR-004) · Tier1
- ControlIds: left-nav `Button`s with `StyleClass.Custom "ghost"` (Shell.fs:65) · Page: all (nav rail)
- Symptom: the 19 nav buttons (the most-used controls) give no hover/focus feedback.
- RootCause (to confirm): `Style.resolve theme baseStyle [Custom "ghost"] state`
  (buttonGeom, Control.fs:1353) likely yields no visible Hovered/Focused delta for the `ghost`
  class. Fix is in the shared style resolution / ghost token path (one semantic control set).
- FixTier: Tier1 · Status: Open

### F-002/F-003 — Hover/focus live feedback (FR-003/FR-004) · Tier1 · Status: **Fixed**
- RootCause (confirmed, refining the Phase-0 hypothesis): the stamping (`deriveVisualState` +
  `applyRuntimeVisualState`) and the live model-unchanged repaint (Feature 108/110/112 targeted
  stamp; the viewer re-renders each frame reading `pointerState`) ALREADY work generically for every
  kind. The visible gaps were specifically **F-004** (button focus) and **F-005** (combined) below;
  the style resolver already differentiates Hover (Accent fill) and Focused (Accent stroke).
- Verification: `Feature175HoverFocusTests` (hover/focus restyle per kind incl. ghost; T023
  damage-local re-stamp). T024/T025 needed no change — the generic bridge already covers every kind.

### F-004 — Keyboard focus invisible on buttons (FR-004) · Tier1 · Status: **Fixed**
- ControlIds: `button` kind (incl. the ghost nav buttons) · Page: all
- RootCause: `buttonGeom`'s filled branch painted only `style.Fill`; `Focused` moves only
  `style.Stroke`, which that branch ignored — so keyboard focus produced NO visible change on any
  button (the showcase's nav rail is all buttons → "no focus" under real input).
- Fix: `buttonGeom` now paints a focus ring (the resolved focus stroke) when the state involves
  focus; Normal/Hover/Pressed add nothing, so non-focused buttons are byte-identical (no regression).
- Verification: `Feature175HoverFocusTests` (button + ghost focus restyle). Full suite green.

### F-005 — Combined hover+focus collapsed to Focused (FR-005) · Tier1 · Status: **Fixed**
- RootCause: `VisualState` is single-valued and `deriveVisualState` ranked `Focused` above `Hover`,
  so a hovered+focused control derived `Focused` only — the hover fill was suppressed.
- Fix: added the combined `VisualState.FocusedHover` (nullary — no surface-baseline delta);
  `applyState FocusedHover` applies BOTH orthogonal deltas (Accent fill + Accent stroke);
  `deriveVisualState` returns it when focused AND hovered (precedence
  Pressed > Selected > FocusedHover > Focused > Hover > Normal). Only `Style.fs` matched VisualState
  exhaustively, so the case was contained.
- Verification: `Feature175HoverFocusTests` (combined ≠ hover-only and ≠ focus-only on the ghost
  button; precedence) + updated `Feature096` precedence test. Full suite green.

### F-009 — Default (accent-filled) button hover (FR-003) · Tier1 · Status: **Fixed**
- ControlIds: default `button` (resting `Fill = theme.Accent`) · Page: all
- Symptom: a default button's resting fill is already `theme.Accent`, so `Hover → Accent` is a no-op
  (no visible hover on default buttons; ghost/transparent-resting controls already show hover).
- Fix (surgical, no global color change): `buttonGeom` lightens the button fill on hover
  (`lerpColor style.Fill Colors.white 0.18`) — buttons only, so `applyState Hover` is untouched and
  there is no resolver-test / visual-baseline blast radius. Non-hover states keep `style.Fill`
  (byte-identical). Verified by `Feature175HoverFocusTests` (default-button hover restyle); full
  Controls suite green (927).
- FixTier: Tier1 · Status: Fixed

### F-006 — context-menu classification vs FR-013 · Tier2/contract · Status: **Resolved (decision)**
- ControlIds: `context-menu` · Page: navigation-menus / overlays
- Symptom: FR-013 names context-menu as overlay-bearing (must open/dismiss under real input), but
  the sample classifies it DisplayOnly ("without host context gesture in headless evidence").
- Decision: **FR-013's overlay open/dismiss + focus-return behaviour is a FRAMEWORK contract**, owned
  by the shared `OverlayState` machine and already verified by `Feature143*`/`Feature144*`
  (open/dismiss, modal hit-testing, and `recoveryFocus` → trigger/`RecoveryTarget` on close). The
  SecondAntShowcase **sample** presents the context-menu as a *menu pattern* and keeps it DisplayOnly
  for the sample's pure-Core-state coverage — it adds no NEW overlay behaviour to verify at the sample
  level. So: keep `context-menu` DisplayOnly in the sample; FR-013 is satisfied by the framework
  overlay tests (not the sample). The display-only reason is clarified accordingly (see
  coverage-classification.md). No reclassification or new sample contract needed.
- FixTier: Tier2 (sample classification) · Status: Resolved (no code change; documented)

### F-007 — Overlay open/dismiss + focus-return (FR-013) · Tier1 · Status: **Already satisfied**
- ControlIds: drawer, popover, popconfirm, tooltip, dialog, tour, (context-menu per F-006) · Page: overlays
- Finding: the framework ALREADY returns focus on close — `OverlayState.closeSurface` emits
  `RequestFocus (recoveryFocus surface)`, and `recoveryFocus` resolves
  `FocusScope.RecoveryTarget` → `Trigger.RecoveryTarget` → parent-surface focus, i.e. the opening
  trigger (or its recovery target / nearest focusable ancestor), exactly per the clarified FR-013.
  The host interprets `RequestFocus` into `FocusControl` (`ControlsElmish.interpretOverlayEffect`).
  Verified by `Feature143OverlayFocusTests` / `Feature144OverlayFocusRoutingTests` (open/dismiss +
  focus routing). No framework change needed.
- FixTier: Tier1 · Status: Already satisfied (existing framework behaviour; re-confirmed)

### F-008 — Per-family live-vs-scripted activation parity (FR-006/FR-007) · Tier1+Tier2
- ControlIds: the 13 contract families · Page: per family
- Symptom: a subset of controls respond to scripted `Model.update` but not to real input.
- RootCause: per-control causes are recorded here as US3 confirms each family (shared pointer
  routing / focus traversal / offset-aware activation = Tier1; unbound `OnChanged` / page wiring =
  Tier2). Sub-findings F-008a… are appended during US3 (T032/T034).
- FixTier: mixed · Status: Open

## Status roll-up

| FindingId | FixTier | Status |
|-----------|---------|--------|
| F-001 scroll | Tier1 | Fixed (ReVerified pending T020 live capture) |
| F-002 hover | Tier1 | Fixed |
| F-003 focus | Tier1 | Fixed |
| F-004 button focus ring | Tier1 | Fixed |
| F-005 combined | Tier1 | Fixed |
| F-006 context-menu class | Tier2 | Resolved (decision, no code change) |
| F-007 overlay focus-return | Tier1 | Already satisfied (existing framework) |
| F-008 per-family parity | mixed | Pending sample repack + verification |
| F-009 default-button hover (lighten on hover) | Tier1 | Fixed |

**All framework-level findings are Fixed** (F-001…F-007, F-009). `ReVerified` (live GUI evidence) and
the sample-level parity sweep (F-008) are gated on the local-feed **repack** that lets the
package-consuming showcase consume this framework — done at the polish/merge consolidation. After the
repack + sample verification, only the live-GUI evidence captures remain as evidence artifacts (the
behaviour itself is deterministically proven by 30+ new tests).
