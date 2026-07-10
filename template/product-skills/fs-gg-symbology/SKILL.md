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

## Two rotations (opt-in second heading)

`Heading` is where a unit **faces**; `SecondaryHeading : float option` is where it **points**, when the
two differ — a turret on a hull, a weapon arc, a sensor or gaze direction. It is `None` by default, and
a `None` token renders byte-identically to one with no such channel:

```fsharp
{ Symbology.defaultToken with Heading = u.HullFacing; SecondaryHeading = Some u.TurretFacing }
```

- Both are **absolute** angles, `0.0` = north, and they wrap — any finite value is in-domain.
- Every grammar draws the second as a **barrel with a tip mark** that starts clear of the centre sigil,
  sited so it never reads as the primary nose / rim pip / needle.
- Leave it `None` unless the angles genuinely differ. A barrel that always agrees with the nose spends a
  channel to say nothing — map it in `mapUnit` only when your units really have two facings.

## Identity label (opt-in inspection-detail channel)

Three optional `Token` fields — all `None` by default — form **one** channel: a short callsign/code drawn
screen-aligned in a per-grammar region. Set them in `mapUnit` only when the abstract `Sigil` cannot
disambiguate identity.

| Field | Type | What it does |
|---|---|---|
| `Label` | `LabelText option` | the explicit identity — `Plain` text, `Rich` styled runs, or `Laid` paragraphs |
| `AutoLabel` | `AutoLabelSpec option` | projects the label from the `Token`'s **own encoded channels** |
| `LabelMotion` | `LabelMotion option` | binds the resolved label to the motion phase the board already supplies |

```fsharp
// The common case — an explicit callsign:
{ Symbology.defaultToken with R = 28.0; Faction = Ally; Label = Some (Symbology.plainLabel u.Callsign) }
```

```fsharp
// Or derive it from the unit's own channels — no callsign typed:
{ Symbology.defaultToken with R = 28.0; Faction = Ally; Health = 0.9; Speed = 2
                              AutoLabel = Some(Symbology.autoLabel [ FactionCode; HealthTier; SpeedPips ]) }
```

An explicit `Label` **always wins** over `AutoLabel`, so there is always **exactly one** resolved label, or
none. Per-grammar line budget: **`Token` ≤ 3, `Badge` ≤ 2, `Ring` ≤ 2** (the ring's inner disc is tightest).

The remaining ctors, all styled by record-copy — see [`reference/labels.md`](reference/labels.md) for when
to reach for each:

```fsharp
Symbology.richLabel [ { Symbology.run "BRAVO-6" with Weight = Some 700 } ] // Rich — styled runs
Symbology.laidLabel [ Symbology.paragraph [ Symbology.run "BRAVO-6" ]      // Laid — paragraphs
                      Symbology.align Trailing [ Symbology.run "R-12" ] ]
```

`Symbology.autoLabelSep sep fields` joins a projection with a separator other than a space, and
`Symbology.labelMotion kind` builds the `LabelMotion` value.

### The invariants — they hold for every field, style, projection and phase

- **Opt-in, layered zero-drift.** Each layer is byte-identical to the one beneath it when unused: `None` ≡
  the pre-feature symbol; `Plain` ≡ the single-line label; an all-default `Rich` ≡ `Plain`; a default
  `Center` `Laid` ≡ `Rich`; `AutoLabel` = `None` ≡ the explicit-label symbol; and `LabelMotion` = `None`
  ≡ the static label **across the whole timeline**, as is any motion-bound label **at rest**. Your one
  mapping still drives all three grammars.
- **Inspection-detail.** It **complements — never replaces** — the vector `Sigil`; keep strings short.
- **Outside the capacity table.** The linter ignores the label, so its verdict is unchanged by labels.
  Never use a label to dodge a channel-overload warning — fix the encoding.
- **Tofu-free is a render-edge property.** The pure library emits deterministic glyph-run nodes and never
  requires a measurer; real glyphs come from the render bridge. Verify through `Symbology.Render`.
- **Surplus degrades: wrap → cap → ellipsis.** Lines wrap at whitespace, the count is **capped** to the
  budget, and the last drawn line ends with `…`. Empty, whitespace, or a fully dropped projection ⇒ **no
  label**. A degenerate (`R <= 0`) token shows the **placeholder** — it always wins.
- **Do not impersonate the pre-attentive encodings.** A label styled to mimic the faction or state palettes
  misleads, and the linter will not catch it. This is a **loop caveat, not a runtime rule**: your colours,
  alignment and decoration are used **as-is**, never re-mapped or rejected.

The per-feature detail and worked examples — styled runs, paragraph alignment and justification, the
`AutoField` codes, and the four `LabelMotion` kinds — live in [`reference/labels.md`](reference/labels.md).

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

Assign-by-urgency (urgent state on hue/motion/size); redundancy on critical state; **one active rhythm
per board** (at most one non-`Idle` `Motion` across the whole board — a single symbol cannot stack
rhythms, `animate` takes one `Motion`); never critical state on dash alone; faction (saturated hue) and
inspection state (dash) never share the hue channel. Check: faction separable? class distinct? health
readable at the target size?

**`Legibility.table` is the single source of every channel's capacity** — read it (`Legibility.table |>
List.iter (printfn "%A")`) rather than memorising numbers, and note that a capacity is what the eye
separates, not what the grammar can draw (`Speed` renders `0..6` beads but ranks fewer of them, so a
board spending more distinct speeds than the capacity warns even though every unit is in-domain). `R`,
`Threat` and
`Charge` carry a `float` and are `Ordered`, so **quantise them in `mapUnit`** — a ramp of twelve
distinct radii is twelve levels and lints as an overload. Only `Health` and the two rotations are
`Continuous`, and only they are overload-exempt.

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
then community sources. If your product uses Spec Kit, record findings and resolving links under the
feature's `specs/<feature>/feedback/` folder; otherwise record them in this skill's **Sources** /
durable-lessons line (and any product-local `docs/` location). Offline, the mandate degrades to
recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-scene]] — supplies the pure primitives the grammar composes.
- [[fs-gg-skiaviewer]] — owns the headless render path the bridge wraps.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven render library): https://github.com/mono/SkiaSharp
