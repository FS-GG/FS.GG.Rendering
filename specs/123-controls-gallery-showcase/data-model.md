# Phase 1 Data Model: Controls Gallery Showcase

Entities are pure F# records/DUs in `ControlsGallery.Core`. None of this is a public
package surface (the sample is a consumer, `IsPackable=false`), so no `.fsi` is
mandated; the shapes below are the contract the `App` edge and the test project rely
on.

---

## Entity: GalleryPage

One of the **exactly 10** pages. Owns its family identity, the catalog controls it
showcases, and the seeded demo state used to populate them.

| Field | Type | Notes / Validation |
|-------|------|--------------------|
| `Id` | `string` | Stable kebab id, e.g. `display-typography`. Unique across the 10 pages. |
| `Index` | `int` | 1..10, nav-rail order. Unique, contiguous. |
| `Title` | `string` | Human label shown in the rail and app bar. |
| `Family` | `string` | The §10.1 family label. |
| `ControlIds` | `string list` | Catalog control ids on this page. Non-empty; each id appears on exactly one page across all 10. |
| `Build` | `DemoState -> Control<Msg>` | Pure page body builder, given seeded demo state. |

**Validation rules**

- The union of all pages' `ControlIds` equals `Catalog.supportedControls |> map .Id`
  (52 ids) with **no duplicates and no omissions** (enforced by the coverage check).
- `Index` values are exactly `1..10`.

---

## Entity: ShowcaseShell (the chrome)

The application frame composed in `Core.Shell.view`. Not a stored record — a rendered
structure — but its parts are fixed by FR-001:

| Part | Control(s) | Behavior |
|------|-----------|----------|
| Top app bar | dock region with title + **theme toggle** + **accent selector** | toggles `Mode`; selects `Accent` |
| Left nav rail | list of the 10 page titles | selects `CurrentPage` |
| Content region | scroll-viewer hosting the current page body | scrolls page content |
| Bottom status strip | text-block(s) | shows page index/id, theme/accent, control count |

---

## Entity: ThemeVariant

A combination of mode + accent resolving the one cohesive palette.

| Field | Type | Notes |
|-------|------|-------|
| `Mode` | `ThemeMode` (`Light` \| `Dark`) | From `FS.GG.UI.Controls.Theming`. |
| `Accent` | `Color` | Gallery-defined literal: **Indigo** (primary) or **Teal** (secondary), over the slate neutral base (see research R5). |

Resolved to a `Theme` for rendering via `Theming.toTheme (Theming.resolve Mode Accent)`.
**Invariant (FR-006/SC-003):** changing `Mode`/`Accent` changes only resolved visuals;
the control tree shape and accessibility metadata are identical across variants.

---

## Entity: CoverageMap

The mapping from each catalog control id to its single page, used by the coverage
check (FR-003). Derived from the `GalleryPage list`, not stored separately.

| Aspect | Definition |
|--------|------------|
| Domain | every id in `Catalog.supportedControls` (52) |
| Codomain | the 10 `GalleryPage.Id` values |
| Function | total + injective-per-control: each control id → exactly one page id |
| `check : unit -> CoverageResult` | returns `Ok` or lists `Unreferenced` / `Duplicated` control ids |

`CoverageResult` (DU or record): `{ Unreferenced: string list; Duplicated: string list }`
— empty/empty ⇒ pass.

---

## Entity: PageEvidenceRecord

The deterministic per-page output of headless mode (FR-008/FR-010). Mirrors the
existing harness `run.json` schema (re-implemented in the consumer; see research R3).

| Field | Type | Notes |
|-------|------|-------|
| `PageId` | `string` | Which page. |
| `Seed` | `int` | The explicit seed driving this run (FR-008). |
| `ProofLevel` | `string` | e.g. `deterministic` for the state portion. |
| `StateOutcome` | `FrameMetrics list` (serialized) | Golden count/bool fields only — `*Duration` fields excluded for determinism. |
| `Screenshot` | `ScreenshotEvidenceResult` (subset) | `frame.png` path, `ProvesScreenshot`, `BlockedStage`, `UnsupportedHostReason`, `Fallback`. |
| `AuthoritativeFor` | `string list` | What the run proves, e.g. `["determinism","tree-equality","non-blank-offscreen-png"]`. |
| `NotAuthoritativeFor` | `string list` | **Non-empty** disclosure (FR-010), e.g. `["renderer-vs-desktop-pixels","live-host","timing"]`. |

**Persisted as**: `artifacts/controls-gallery/<seed>/<page-id>/` →
`run.json` (machine), `summary.md` (human disclosure), `frame.png` (screenshot or
absent-with-reason), `state.txt` (golden metrics).

**Validation rules**

- `NotAuthoritativeFor` MUST be non-empty (FR-010).
- Two runs with the same `Seed` over all pages MUST yield **byte-identical** `run.json`
  + `state.txt` (+ `frame.png` where GL is present) (FR-009/SC-002).
- On no-GL hosts, `Screenshot.ProvesScreenshot = false` with a stated reason and the
  process exits 0 (FR-011/SC-004).

---

## Entity: GalleryModel / GalleryMsg (MVU, Principle IV)

The pure state and events; `init`/`update` live in `Core.Model`.

```text
type GalleryModel =
  { CurrentPage : string            // page Id
    Mode        : ThemeMode
    Accent      : Color
    PageState   : DemoState }        // per-control seeded interactive state

type GalleryMsg =
  | SelectPage of string
  | ToggleTheme
  | SelectAccent of Color
  | PageMsg of (* control interaction messages routed to the active page *) ...
```

- `init` → first page, `Light`, indigo accent, seeded `DemoState`.
- `update` is pure; the only effects (`ViewerEffect list`) are edge concerns
  (window/screenshot/file), interpreted by `App`.

---

## Entity: SeededScript (per page, headless)

The deterministic input script replayed per page in evidence mode.

| Field | Type | Notes |
|-------|------|-------|
| `PageId` | `string` | Target page. |
| `Frames` | `FrameInput<Msg> list` | `Key` / `Pointer` / `Tick(Δt)` / `Idle`; ticks use **injected** deltas (no wall-clock). |

Same seed ⇒ same script ⇒ same `FrameMetrics` ⇒ byte-identical evidence.
