# `.fsi` Seam Drafts — Foundational (T006/T007)

Drafted public-surface deltas for the Tier 1 seams, designed first per Constitution Principle I.
These are committed to `.fsi`+`.fs` **together** in the owning user story (an `.fsi` `val` with no
`.fs` body is a compile error, so we do not land a broken `.fsi` in Foundational — that would break
the green-at-every-phase bar). Each draft notes the failing-first test that drives it.

## Scroll seam (T006) — drives US1 (T013–T019)

### Layering correction to plan.md (recorded)

plan.md placed scroll-offset state + `applyScrollDelta` in `src/Controls/Widgets/Containers.fs`
(namespace `FS.GG.UI.Controls.Typed`). But the runtime that consumes scroll input —
`ControlRuntimeModel`/`ControlRuntimeMsg` in `FS.GG.UI.Controls` — sits **below** `Typed.Containers`
in the compile graph and cannot reference a type defined there. Therefore the `ScrollState` value
type lives in the **core `FS.GG.UI.Controls` layer** (alongside `ControlCaret`/`ControlSelection`,
which `ControlRuntimeMsg` already references), not in `Typed.Containers`. `Typed.Containers` keeps
only the authoring `ScrollViewerProps.OnChanged` optional report (unchanged surface). Exact source
file/compile-order is pinned at the start of US1 against `Controls.fsproj`.

### `ScrollState` (new core value type + pure transition)

```fsharp
/// Per-`scroll-viewer` scroll model (data-model.md §Scroll state). Pure value; derived fields are
/// functions of (Offset, ContentHeight, ViewportHeight). minThumb is the dead-zone floor.
type ScrollState =
    { Offset: float
      ContentHeight: float
      ViewportHeight: float }

module ScrollState =
    val empty: ScrollState
    /// Derived: ContentHeight > ViewportHeight + 1px dead-zone (one-pixel overflow ⇒ non-scrollable).
    val scrollable: state: ScrollState -> bool
    /// max(0, ContentHeight - ViewportHeight) — the maximum offset.
    val maxOffset: state: ScrollState -> float
    /// Record the measured extents (host sets these when the region is laid out).
    val withExtent: contentHeight: float -> viewportHeight: float -> state: ScrollState -> ScrollState
    /// Offset' = clamp(Offset + delta, 0, maxOffset). No overscroll. (FR-001/FR-002)
    val applyScrollDelta: delta: float -> state: ScrollState -> ScrollState
    /// Thumb height = max(minThumb, ViewportHeight * ViewportHeight/ContentHeight); 0 when not scrollable.
    val thumbHeight: state: ScrollState -> float
    /// Thumb top (px from track top) = Offset / maxOffset * (track - thumbHeight); 0 when not scrollable.
    val thumbPosition: trackHeight: float -> state: ScrollState -> float
```
- Failing-first: T008 (clamp at both bounds, no overscroll), T009 (thumb height/position + no-thumb
  when `ContentHeight <= ViewportHeight` incl. one-pixel dead-zone) — `tests/Controls.Tests`.

### `ControlRuntime.fsi` delta (scroll consumption)

```fsharp
type ControlRuntimeModel =
    { …existing…
      ScrollOffsets: Map<ControlId, ScrollState> }   // NEW

type ControlRuntimeMsg =
    | …existing…
    | SetScrollExtent of ControlId * contentHeight: float * viewportHeight: float   // NEW
    | ScrollControl of ControlId * delta: float                                     // NEW
```
- `update` handles `ScrollControl` via `ScrollState.applyScrollDelta` (clamped against the stored
  extent) and `SetScrollExtent` via `ScrollState.withExtent`; an unknown id starts from
  `ScrollState.empty`. A read accessor `scrollOffsetOf: ControlId -> ControlRuntimeModel -> float`
  is exposed for the painter/hit-test.
- Failing-first: T011 (Scroll routing updates offset + damage-local repaint) — `tests/Elmish.Tests`.

### `Control.fsi` delta (offset-aware paint + hit-test)

```fsharp
// scrollAffordance gains the current offset so the thumb tracks (was: thumb pinned at box.Y):
val scrollAffordance: theme -> box: Rect -> content: ScrollState -> Scene list   // signature change
// Offset-aware hit-test seam for scroll-viewer descendants (subtract region offset before resolve):
val hitTestScrolled: offsets: (ControlId -> float) -> policy -> layout -> x: float -> y: float -> ControlId option
```
- Failing-first: T010 (offset-aware hit-test resolves the correct control after scroll) —
  `tests/Controls.Tests`. Exact public-vs-internal split (some of this is `ControlInternals`) is
  pinned in US1 against the current `Control.fsi`.

### `Pointer.fsi` delta (thumb-drag + scroll keys)

```fsharp
// No new type; the existing Scroll interaction is reused. Add a keyboard-scroll mapper that the host
// reduces to ScrollControl, and thumb-drag already flows through the Drag interactions:
val scrollKeyDelta: key: string -> viewportHeight: float -> float option   // Arrow/Page/Home/End/Space
```
- Scroll keys per `contracts/scroll-interaction.md`: ArrowUp/Down (line step), PageUp/Down
  (viewport-height step), Home/End (to top/bottom — emit a large signed delta clamped by
  applyScrollDelta), Space/Shift+Space (page down/up). Failing-first folded into T011/T016.

## Interaction-state seam (T007) — drives US2 (T024–T027)

### `ControlRuntime.fsi` delta — **likely empty** (recorded finding)

`deriveVisualState` already maps `HoveredControl → Hover` and `FocusedControl → Focused`
**generically for every kind**, and `applyRuntimeVisualState` stamps it (ControlRuntime.fs:219/249).
So the US2 "coverage seam" needs **no new `ControlRuntime` public surface** — the existing stamping
already covers every interactive kind, including `ghost` buttons (the stamp is kind-agnostic). This
reduces Tier 1 surface churn: US2's real changes are the **repaint trigger** (ControlsElmish) and
the **per-kind painter deltas** (Control.fs `*Geom`/`Style.resolve` for a visible Hover/Focused
diff, esp. the `ghost` class). If US2 uncovers a genuine gap, a delta is added then.
- Failing-first: T021 (stamp correct VisualState per interactive kind incl. ghost), T022 (combined
  hover+focus; display-only stays Normal) — `tests/Controls.Tests`.

### `ControlsElmish.fsi` delta — hover/focus retained-repaint trigger

```fsharp
// Ensure HoverControl/FocusControl drive a damage-local retained repaint on the live loop even when
// the product Model is unchanged (model-unchanged repaint). Current surface exposes the live host;
// the precise seam (a predicate/flag that marks a runtime-only repaint) is pinned in US2 against the
// current ControlsElmish.fsi. Candidate: extend the retained-repaint cause to include runtime
// visual-state change, so a hover-enter/leave or focus-change triggers exactly one damage-local frame.
```
- Failing-first: T023 (HoverChanged/FocusChanged → model-unchanged, damage-local retained repaint;
  does not rebuild the view tree per pointer move) — `tests/Elmish.Tests`.

## Net Tier 1 public-surface footprint (estimated)

| File | Expected delta |
|------|----------------|
| core `FS.GG.UI.Controls` (ScrollState owner) | NEW `ScrollState` type + module |
| `ControlRuntime.fsi` | `ScrollOffsets` field + `ScrollControl`/`SetScrollExtent` msgs + `scrollOffsetOf` |
| `Control.fsi` | `scrollAffordance` signature change + offset-aware hit-test seam |
| `Pointer.fsi` | `scrollKeyDelta` mapper |
| `ControlsElmish.fsi` | repaint-trigger seam (TBD US2; possibly internal-only) |
| `Containers.fsi` | none (OnChanged already present) |

Surface-baseline files to refresh on these: `FS.GG.UI.Controls.txt`, `FS.GG.UI.Controls.Elmish.txt`.
