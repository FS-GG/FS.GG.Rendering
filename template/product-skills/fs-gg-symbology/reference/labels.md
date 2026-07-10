# Identity label — reference

The full detail behind the **Identity label** section of [`../SKILL.md`](../SKILL.md). That section
states the channel's invariants once; this file holds the per-feature semantics and the worked examples.

Everything here obeys the invariants in `SKILL.md` — opt-in and layered zero-drift, inspection-detail,
outside the capacity table, tofu-free only at the render edge, `wrap → cap → ellipsis`, and the
don't-impersonate-the-pre-attentive-encodings caveat. They are not restated per feature below.

## `Plain` — text, one or more lines

`Label` may carry more than one line: embedded `\n` are hard breaks, and a long line soft-wraps at
whitespace to the region width. No new field, no second channel.

```fsharp
// A callsign over a code — two lines in the one field:
{ Symbology.defaultToken with R = 28.0; Faction = Ally; Label = Some (Symbology.plainLabel (u.Callsign + "\n" + u.Code)) }
```

- Lines stack downward from the label's baseline, **screen-aligned** — the block never rotates with heading.
- A single unbroken word too wide to fit shrinks and ellipsis-truncates **on its own line**; no mid-word break.
- A one-line-fitting label is byte-identical to the single-line render, so `\n` is the only way to force a break.
- Interior blank lines collapse (no wasted gap); a blank-lines-only label is no label.
- Keep strings short — overlong labels shrink, then ellipsis-truncate at a measured glyph boundary.

## `Rich` — per-run colour / weight / size

`LabelText.Rich` carries a short ordered sequence of `LabelRun` spans. Each run has **eight** fields:
`Text`, plus optional `Color` / `Weight` / `Scale` (inherit the default label style when `None`, so an
all-default `Rich` ≡ the equivalent `plainLabel` byte-for-byte) and optional `Italic` / `Underline` /
`Strike` / `Tracking` (letter-spacing, an em-fraction of the run size).

```fsharp
// A loud bold callsign next to a dim, smaller code — one styled label:
{ Symbology.defaultToken with
    R = 28.0
    Faction = Ally
    Label =
        Some(
            Symbology.richLabel
                [ { Symbology.run u.Callsign with Weight = Some 700 }
                  { Symbology.run ("  " + u.Code) with Scale = Some 0.7; Color = Some (Colors.rgb 150uy 150uy 150uy) } ]) }
```

- **Use it for an emphasis hierarchy** — a loud bold callsign + a dim small code — so two pieces of
  identity can be triaged at a glance.
- **Use the decoration attributes** to let a run read as *quoted*, deleted, tagged, or spaced **without**
  spending the weight/colour budget — distinct in *kind*, not just louder.
- `Color` is any scene `Color`; `Weight` maps onto `FontSpec.Weight`; `Scale` multiplies the grammar's
  base label size. **Per-glyph styling and per-run font family stay out of scope.**
- Keep runs **few** and the palette **restrained** — a rainbow of runs is noise.
- Each run is measured and fitted **in its own style**; runs flow and wrap to the region, and each line's
  height follows its **tallest** run on a common baseline. An empty/whitespace run drops; `Rich []` ⇒ no label.

## `Laid` — alignment, justification, explicit paragraphs

`LabelText.Laid of LabelParagraph list` carries explicit paragraphs, each `{ Runs; Align }` with
`Align = Leading | Center | Trailing | Justify`. Build with `Symbology.paragraph` / `align` / `laidLabel`.

```fsharp
// A centred callsign over a justified descriptor and a struck-through retired code:
{ Symbology.defaultToken with
    R = 28.0
    Faction = Ally
    Label =
        Some(
            Symbology.laidLabel
                [ Symbology.align Center [ { Symbology.run u.Callsign with Weight = Some 700 } ]
                  Symbology.align Justify [ Symbology.run u.Descriptor ]
                  Symbology.align Trailing [ { Symbology.run u.RetiredCode with Strike = Some true } ] ]) }
```

- **Paragraph breaks are the list boundaries**; hard breaks *inside* a paragraph use the runs' embedded `\n`.
- **`Center` is the default and reproduces the `Rich` flow byte-for-byte** (a single `Center` all-default
  paragraph ≡ the equivalent `richLabel` / `plainLabel`). Only a non-default alignment, more than one
  paragraph, or a set decoration/slant/tracking attribute changes the bytes.
- **`Justify` fills wrapped lines**; the **last line of each paragraph** and any **single-token line** stay
  un-justified — never a stretched final line, never a stretched glyph.
- **Tracking is folded into measurement**, so it never overflows the region; **underline / strike follow
  each drawn fragment** (a wrapped run is decorated per line) and never extend past a clipped glyph.
- Empty paragraphs and runs drop; `Laid []` ⇒ no label. Keep paragraphs **short**.

## `AutoLabel` — derive the label from the Token's own channels

Set `AutoLabel : AutoLabelSpec option` (`Symbology.autoLabel fields` / `autoLabelSep sep fields`) and the
library projects a compact, game-agnostic readout from the **`Token`'s own encoded channels** — never your
raw stats. The per-game `'stats -> Token` mapping stays yours.

```fsharp
// A state readout projected from the unit's own channels — no callsign typed:
{ Symbology.defaultToken with R = 28.0; Faction = Ally; Health = 0.9; Speed = 2
                              AutoLabel = Some(Symbology.autoLabel [ FactionCode; HealthTier; SpeedPips ]) }
```

| `AutoField` | Emits |
|---|---|
| `FactionCode` | `ALY` / `ENY` / `NEU` / `CUS` |
| `KlassCode` | `MOB` / `HVY` / `SCT` |
| `StateCode` | `CFM` / `SUS` |
| `HealthTier` | `H` + `round(Health * 100)` |
| `ThreatTier` | `T0`..`T4` |
| `SpeedPips` | `S0`..`S4` |
| `ShieldFlag` | `SHD` — **dropped** when `Shield = false` |

- An explicit `Label` **always wins**; identical channels ⇒ an identical label; an empty or fully dropped
  projection ⇒ no label (never a throw).
- The projection rides the **same** fit / wrap / cap / decoration path as a hand-authored label.
- **Auto-derive** for at-a-glance state readouts on a roster (faction + health tier + speed) where typing a
  callsign per unit is noise; **hand-author** for names, callsigns, and any text not derivable from a channel.
- Keep projections compact — pick **2–3 fields**, not the whole set. `Ring` is the tightest region.

## `LabelMotion` — animate the resolved label (no new clock)

Set `LabelMotion : LabelMotion option` and the resolved label (explicit or auto-derived) animates as a pure
function of the **phase the board already supplies** (`animate` / `filmstrip` / `animateIn` / `filmstripIn`) —
no signature change, no wall-clock. `AutoLabel` resolves *first*, then the resolved label animates.

| Kind | Effect |
|---|---|
| `TypeOn` | reveals a whole-glyph **prefix** — never mid-glyph |
| `Fade` | ramps run alpha |
| `Pulse` | oscillates size about the region centre, **capped so the scaled label still fits** |
| `Scroll` | offsets an overlong line and **clips to the region** — no overflow |

- **Rest = static.** At the rest phase every kind is the identity transform, so a motion-bound label at rest
  is byte-identical to the static label; the static entry points always draw the rest frame.
- Each kind stays **fitted at every phase** and tofu-free. A motion bound to an empty label draws nothing.
- Keep motion **restrained** — it is inspection-detail, not a pop-out channel.

## Verifying

Tofu-free output is a **render-edge** property under every style, alignment, projection and phase. Verify
through `Symbology.Render` (sampling phases for a motion-bound label), never from a pure unit test — the pure
library emits the glyph-run nodes, requires no measurer, and never throws without one.
