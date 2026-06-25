---
name: fs-gg-symbology
description: Map a unit roster to legible vector symbols in a generated FS.GG.UI product, render boards headlessly, and run the render->eyeball->tweak design loop.
---

# Symbology Capability

## Scope

Use this skill for product code that turns per-unit stats into legible abstract vector symbols: build
a per-game `'stats -> Token` mapping, compose `gallery` / `filmstrip` boards, and rasterise them
headlessly to critique at the target on-board size. The grammar is fixed; the mapping is yours to edit.

## Public Contract

The signatures you consume are bundled with this product under `docs/api-surface/Symbology/` (the pure
`Symbology.fsi`) and `docs/api-surface/Symbology.Render/` (the `Render.fsi` bridge). The pure library
references only `Scene`; all raster/IO is in the render bridge. Build from `Symbology.defaultToken` and
override only the fields your game encodes.

## Usage

```fsharp
open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

type UnitStats = { Side: string; Role: string; Dps: float; Hp: float; HpMax: float; Facing: float }

// the editable per-game mapping (data — NOT library internals):
let mapUnit (u: UnitStats) : Token =
    { Symbology.defaultToken with
        R = 28.0
        Faction = (match u.Side with "blue" -> Ally | "red" -> Enemy | _ -> Neutral)
        Klass = (match u.Role with "tank" -> Heavy | "scout" -> Scout | _ -> Mobile)
        Threat = min 1.0 (u.Dps / 120.0)
        Health = u.Hp / u.HpMax
        Heading = u.Facing }

let board = Symbology.gallery 4 90.0 (roster |> List.map mapUnit)
let png   = Render.toPng { Width = 920; Height = 660 } board "./readiness/symbology/iter-001"
// -> read `png` back, critique at the target size, TWEAK mapUnit ONLY, repeat.
```

## Identity label (opt-in inspection-detail channel)

`Token` carries an optional `Label : string option` — a short callsign/code drawn screen-aligned in a
per-grammar region. Set it in `mapUnit` only when the abstract sigil cannot disambiguate identity:

```fsharp
{ Symbology.defaultToken with R = 28.0; Faction = Ally; Label = Some u.Callsign }
```

- **Inspection-detail, not pop-out.** It complements — never replaces — the vector `Sigil`; keep strings
  short. It is **not** in the legibility capacity table, so the linter's verdict is unchanged by labels.
- **Opt-in, zero-drift.** `Label = None` (the default) and empty/whitespace render byte-identically to the
  pre-feature symbol; the one mapping still drives all three grammars unchanged.
- **Tofu-free is a render-edge property.** The pure library emits a deterministic glyph-run node and never
  requires a measurer; legible non-tofu glyphs come from the render bridge. Overlong labels are fitted
  (shrink, then ellipsis-truncate); a degenerate (`R <= 0`) labelled token still degrades to the placeholder.

## Selectable grammars (form factors) — one mapping, three drawings

The same `'stats -> Token` mapping drives three interchangeable **grammars**, chosen as a value
`Grammar = Token | Badge | Ring`. Switching grammar changes only the drawing — the ChannelMap is
**unchanged**:

- **Token** (`Grammar.Token`) — heading-rotated silhouette; the v1 default; prefer when motion/heading is primary.
- **Badge** (`Grammar.Badge`) — compact, **screen-aligned** framed emblem (class-driven frame, bottom health bar, speed pips, edge heading pip); prefer for dense insignia walls where an upright frame reads faster.
- **Ring** (`Grammar.Ring`) — centred **radial gauge** (outer ring hue/threat/state, health **arc sweep** monotone in health, rim speed beads, heading needle); prefer when continuous channels should read radially.

Render a selected grammar with `Symbology.render grammar token`; build boards with `galleryIn` /
`filmstripIn` / `animateIn` (the `gallery`/`filmstrip`/`animate` args, plus a leading `Grammar`).
Badge/Ring are screen-aligned (heading is a discrete indicator) and take only grammar-agnostic motion
overlays (Pulse/Blink/Damage; directional rhythms degrade to the static base). Because the mapping is
identical across grammars, the legibility linter's verdict is **grammar-independent**. `Grammar.Token`
reproduces the existing functions byte-for-byte.

## Legibility rules to critique against

Assign-by-urgency (urgent state on hue/motion/size); redundancy on critical state; one active motion at
a time; never critical state on dash alone; faction (saturated hue) and inspection state (dash) never
share the hue channel. Check: faction separable? class distinct? health readable at the target size?

CRITIQUE with two complementary checks: (a) LINT — run the linter on the produced symbol set
(`Legibility.score (roster |> List.map mapUnit)`; animated boards use `scoreAnimated` over the
`(motion, token)` pairs) and read `report.Verdict` / `report.Findings`. The linter is pure/deterministic
and the mechanical backstop: a `Warning`/`Error` names the overloaded or out-of-domain `Channel`,
used-vs-capacity, and the contributing unit indices. A non-`Clean` verdict is a TWEAK trigger — the unit
of change stays the mapping, never the grammar. (b) EYE — the human-style self-check of the PNG vs the
rules above stays (the linter cannot see crowding, contrast, or label collisions). The approved roster
lints `Clean`, so a fresh finding is a real signal to re-tune the mapping.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned symbology mappings and board examples.

## Evidence

Record each loop iteration's *timestamped board PNG* + *mapping snapshot* under this product's
`readiness/` paths. `Render.toPng` fails loud on any non-passing verdict, so a critique never reasons
over a blank image. Re-rendering an unchanged mapping is byte-identical (determinism).

## Package Boundary

`FS.GG.UI.Symbology` must not reference the viewer host, layout, widgets, or Elmish — keep all raster/IO
in `FS.GG.UI.Symbology.Render`. Keep the game-symbol vocabulary off the core control surface.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own documentation),
then community sources. Record findings and resolving links in the feature's `specs/<feature>/feedback/`
folder. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-scene]] — supplies the pure primitives the grammar composes.
- [[fs-gg-skiaviewer]] — owns the headless render path the bridge wraps.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven render library): https://github.com/mono/SkiaSharp
