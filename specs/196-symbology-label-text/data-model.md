# Phase 1 Data Model: Symbology Label / Glyph Text Channel

This feature adds **one optional field** to an existing value type and a set of pure, internal label-siting/fit helpers. There is no new public type. Entities below are grounded against `src/Symbology/Symbology.fsi`/`.fs` and `src/Scene/Scene.fsi` as of 2026-06-25.

## Entity: `Token` (existing, extended)

The fixed channel-set value describing one unit's encoded state. This feature adds exactly one optional channel — the identity label — leaving all existing fields and their meanings unchanged.

| Field | Type | Meaning | Change |
|---|---|---|---|
| `Cx`, `Cy` | `float` | centre position | unchanged |
| `R` | `float` | radius / magnitude (`R <= 0` ⇒ placeholder) | unchanged |
| `Heading` | `float` | rotation (radians) | unchanged |
| `Faction` | `Faction` | stroke hue | unchanged |
| `Klass` | `Klass` | body silhouette | unchanged |
| `Sigil` | `Sigil` | centre vector mark | unchanged |
| `State` | `TokenState` | solid/dashed stroke | unchanged |
| `Threat` | `float` | stroke width `[0,1]` | unchanged |
| `Charge` | `float` | interior gradient alpha `[0,1]` | unchanged |
| `Speed` | `int` | tail beads / speed pips | unchanged |
| `Health` | `float` | health arc `[0,1]` | unchanged |
| `Shield` | `bool` | corner mount | unchanged |
| **`Label`** | **`string option`** | **optional short identity string (name/callsign/code); `None` = no label (default)** | **NEW** |

- **`defaultToken`**: gains `Label = None`. Because every existing token defaulted/constructed without this field now carries `None`, and `None` emits no label node, `defaultToken` and all existing tokens render **byte-identically** to the pre-feature symbol (FR-002/SC-003).
- **Validation / normalisation rules** (applied by the grammars, not by the type):
  - `None` ⇒ no label (default).
  - `Some s` where `s.Trim()` is empty ⇒ treated as no label (no node, no throw) (FR-006).
  - `Some s` (non-empty after trim) ⇒ label is the trimmed string, fitted to the grammar's label region (FR-005).
  - The label is an **opaque short string**; this feature takes on no new shaping responsibility (Unicode/non-Latin handling follows whatever the installed real measurer/shaper supports).

## Entity: Identity label (new channel — conceptual)

An optional short text string read for identity directly. **Inspection-detail**, distinct from and complementary to the vector `Sigil`. Sited per grammar so it never collides with another channel; tofu-free when rendered through the real measurer. It is **not** a pre-attentive (pop-out) channel and is **not** added to the legibility capacity table.

## Entity: `Grammar` (existing, unchanged)

The selectable form factor (`Grammar.Token | Badge | Ring`). Each grammar sites the label in its own region; the label is part of the one shared vocabulary, so the grammar choice changes only **where/how** the label is drawn, never the per-game mapping. No change to the type or its `render`/`galleryIn`/`filmstripIn`/`animateIn` signatures.

## Per-grammar label-region siting (contract: FR-003; geometry: design-loop)

> The **contract** is FR-003 — the label, when present, observably alters the output and does **not overlap** the sigil, health, or other channels. The exact coordinates below are a design-loop detail resolved during implementation; they are the provisional starting point, not the contract.

| Grammar | Label region (provisional) | Alignment | Clear of |
|---|---|---|---|
| **Token** | short caption strip below the body belly, beneath the bottom health arc, centred on `Cx` | screen-aligned (does not rotate with `Heading`) | rotated body, centre sigil, bottom health arc, shield corner |
| **Badge** | caption band along the bottom inner edge of the frame | screen-aligned | speed-pip row, bottom health bar, frame stroke, edge heading pip |
| **Ring** | centred caption beneath the sigil, inside the inner disc | screen-aligned | outer ring stroke, health-arc segments, bottom-rim speed beads, centre heading needle |

## Internal helpers (omitted from `.fsi` — not public surface)

These live in `Symbology.fs`, carry **no** access modifiers (visibility is by absence from the `.fsi`, constitution II), and are pure.

| Helper (illustrative) | Shape | Responsibility |
|---|---|---|
| `fitLabel` | `region-width -> base-font -> string -> FittedLabel option` | trim; `None`/empty/whitespace ⇒ `None`; else measure via `Scene.measureTextResolved`, shrink toward a font floor, else ellipsis-truncate at a measured glyph boundary; returns the fitted text + font + placement, guaranteed within the region |
| `labelNode` (per grammar) | `Token -> Scene` | compute the grammar's label region, call `fitLabel`, emit `Scene.glyphRunProof` for a fitted label or `Scene.group []` (no node) when there is none |

`FittedLabel` (if introduced) is an **internal** record (fitted text, font size, baseline position) and stays out of the `.fsi`.

## Scene / measurement vocabulary consumed (existing, from `FS.GG.UI.Scene`)

| Member | Role in this feature |
|---|---|
| `FontSpec` | label font descriptor (family/size/weight) |
| `TextMetrics` | width/height/baseline used by the fit |
| `Scene.measureTextResolved : string -> FontSpec -> TextMetrics` | measure through the installed real measurer when present, else the pure heuristic (drives fit; provider-relative determinism) |
| `Scene.measureText` | the conservative pure fallback heuristic (no-provider default; never under-sizes the box) |
| `Scene.glyphRunProof : Point -> string -> FontSpec -> Paint -> Scene` | emit the label as a proof-bearing glyph-run node (`GlyphRunData` carries per-glyph `Missing` + `FallbackMode` — the tofu evidence) |
| `Scene.group` | compose the (possibly empty) label node into the grammar's scene |
| `Color` / `Paint` | label colour/paint |

No new Scene member is added; no `setRealTextMeasurer` call is made by the library (the render edge owns the measurer).

## Behaviour rules (cross-cut, testable)

| Rule | Source | Test surface |
|---|---|---|
| `Label = None` ⇒ scene byte-identical to pre-feature symbol | FR-002/SC-003 | `DeterminismTests` goldens unchanged |
| Two tokens differing only in `Label` ⇒ differing canonical bytes | US1 / SC | `ChannelPresenceTests` |
| Labelled token, fixed provider, render twice ⇒ byte-identical | FR-008/SC-004 | `DeterminismTests` render-twice + golden proxy |
| Empty/whitespace label ⇒ no node, no throw | FR-006/SC-005 | `LabelTests` |
| Overlong label ⇒ fitted within region, no mid-glyph cut, no overflow | FR-005/SC-005 | `LabelTests` (fitted measured ≤ region width) |
| Degenerate (`R <= 0`) + label ⇒ placeholder, no throw | FR-007/SC-005 | `PlaceholderTests` |
| Labelled token via render bridge ⇒ non-tofu glyph run | FR-004/SC-002 | `Symbology.Render.Tests` |
| Labelled roster reproducible per grammar on a board | FR-010 | `GalleryTests` |
| Label presence ⇒ linter `Report` unchanged, grammar-independent | FR-011/SC-006 | `LegibilityTests` |
| Only `FS.GG.UI.Symbology.txt` baseline moves (gains `Label`) | FR-013/SC-007 | surface-drift check |
