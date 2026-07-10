# Identity label — reference

The full detail behind the **Identity label** section of [`../SKILL.md`](../SKILL.md). That section
states the channel's invariants once; this file is the per-feature specification an agent reaches for
when it needs the exact semantics.

Everything here is governed by the invariants in `SKILL.md` — opt-in and layered zero-drift,
inspection-detail, outside the capacity table, tofu-free only at the render edge, `wrap → cap →
ellipsis`, and the don't-impersonate-the-pre-attentive-encodings caveat. They are not restated per
feature below.

## The one field set

`Token` carries three optional label fields, all `None` by default:

Pick **one** of `Label` or `AutoLabel` — they are two ways to resolve the same label, not two labels:

```fsharp
// Hand-authored identity, animated:
{ Symbology.defaultToken with
    Label       = Some (Symbology.plainLabel "BRAVO-6")
    LabelMotion = Some (Symbology.labelMotion TypeOn) }

// Or projected from the token's own channels, animated the same way:
{ Symbology.defaultToken with
    AutoLabel   = Some (Symbology.autoLabel [ FactionCode; HealthTier ])
    LabelMotion = Some (Symbology.labelMotion TypeOn) }
```

An explicit `Label` **always wins** over `AutoLabel`, so setting both silently ignores the projection.
There is always **exactly one** resolved label, or none. `AutoLabel` resolves *first*, then `LabelMotion`
animates whatever resolved — auto and motion compose.

The same `'stats -> Token` mapping drives all three grammars; there is no per-grammar label mapping.

## `LabelText` — Plain, Rich, Laid

`Label : LabelText option` holds one of three contents.

### `Plain of string` — text, one or more lines

- Embedded `\n` (and `\r\n`) are **hard breaks**; a long line **soft-wraps** at whitespace to the
  region width. Multi-line is the **same field** — no new field, no second channel.
- A single unbroken word too wide to fit shrinks and ellipsis-truncates **on its own line**; there is
  **no mid-word break**.
- Lines stack **downward** from the label's baseline, **screen-aligned** — the block never rotates with
  heading, in any grammar.
- A one-line-fitting label is **byte-identical** to the single-line render, so adding `\n` is the only
  way to force a break.
- Interior blank lines collapse (no wasted gap); a blank-lines-only label is no label.
- Keep strings short. Overlong labels are fitted — shrink, then ellipsis-truncate at a **measured glyph
  boundary** — so a long string degrades rather than overflowing. Short callsigns read best.

### `Rich of LabelRun list` — per-run colour / weight / size

A short ordered sequence of styled spans. Each `LabelRun` carries **eight** fields:

| Field | Default | Meaning |
|---|---|---|
| `Text` | — | the span's text |
| `Color` | `None` | any scene `Color`; inherits the default label style |
| `Weight` | `None` | maps onto `FontSpec.Weight` |
| `Scale` | `None` | multiplies the grammar's base label size |
| `Italic` | `None` | synthetic slant |
| `Underline` | `None` | a non-text rule under each drawn fragment |
| `Strike` | `None` | a non-text rule through each drawn fragment |
| `Tracking` | `0.0` | letter-spacing, an em-fraction of the run size |

Construct with `Symbology.run` (a default span) and `Symbology.richLabel`; style by record-copy:

```fsharp
{ Symbology.run "BRAVO-6" with Weight = Some 700; Color = Some teamBlue }
```

- **When to use.** Express an **emphasis hierarchy** inside one identity — a loud, bold callsign next to
  a dim, smaller code — so two pieces of identity can be triaged at a glance.
- **Use the decoration attributes** (`Italic` / `Underline` / `Strike` / `Tracking`) to let a run read as
  *quoted*, deleted, tagged, or spaced **without** spending the weight/colour budget — i.e. when a run
  must read as distinct in *kind*, not just louder.
- **Keep runs few and the palette restrained.** A couple of short runs with one or two deliberate styles
  reads; a rainbow of runs is noise.
- **Fitted per run.** Each run is measured and fitted **in its own style**; runs flow and wrap to the
  region; each line's height follows its **tallest** run on a common baseline.
- A run that is empty/whitespace **drops**; `Rich []` ⇒ no label.

### `Laid of LabelParagraph list` — paragraph layout

Each `LabelParagraph` is `{ Runs; Align }` with `Align = Leading | Center | Trailing | Justify`.
Construct with `Symbology.paragraph` (a `Center` paragraph), `Symbology.align alignment runs`, and
`Symbology.laidLabel`.

- **Paragraph breaks are the list boundaries**; hard line breaks *inside* a paragraph use the runs'
  embedded `\n`. Each paragraph carries its own alignment.
- **When to use.** Reach for `Laid` when a label needs **document structure** — a centred callsign over a
  justified descriptor, a trailing retired-code line — beyond the flush `Rich` flow.
- **`Center` is the default and reproduces the `Rich` flow**: a single `Center` paragraph of all-default
  runs is byte-identical to the equivalent `richLabel` / `plainLabel`. Only a non-default alignment, more
  than one paragraph, or a set decoration/slant/tracking attribute changes the bytes.
- **`Justify` fills the width; the last line never stretches.** It distributes **measured inter-word
  space** so each *wrapped* line fills the region. The **last line of each paragraph** and any
  **single-token line** fall back to leading (un-justified) — never a stretched final line, never a
  stretched glyph.
- **Tracking is folded into measurement**, so letter-spacing never pushes the block past the region.
- **Underline / strike follow each drawn fragment's geometry** — a wrapped run is decorated per line —
  and never extend past a clipped glyph.
- Empty/whitespace paragraphs and runs **drop**; `Laid []` ⇒ no label.

### Out of scope (use geometry or the `Sigil` instead)

Inline images, hyperlinks, bullet/numbered lists, per-glyph styling, per-run font family, **per-game stat
→ label semantics inside the library** (the `'stats -> Token` mapping stays the caller's), advanced bidi,
any new GPU/compute path, and new font files — slant, underline and strike are **synthesised** from
existing primitives.

## `AutoLabel` — derive the label from the Token's own channels

Set `AutoLabel : AutoLabelSpec option` and the library projects a compact, game-agnostic readout from
**that `Token`'s own encoded channels** — never a game's raw stats (FR-002). Build the spec with
`Symbology.autoLabel fields` (space-joined) or `Symbology.autoLabelSep sep fields`.

| `AutoField` | Reads | Emits |
|---|---|---|
| `FactionCode` | `Faction` | `ALY` / `ENY` / `NEU` / `CUS` |
| `KlassCode` | `Klass` | `MOB` / `HVY` / `SCT` |
| `StateCode` | `TokenState` | `CFM` / `SUS` |
| `HealthTier` | `Health` | `H` + `round(Health * 100)` |
| `ThreatTier` | `Threat` | `T0`..`T4` |
| `SpeedPips` | `Speed` | `S0`..`S4` |
| `ShieldFlag` | `Shield` | `SHD` — **dropped** when `Shield = false` |

- **Deterministic.** Identical channels ⇒ a byte-identical projection.
- **Degrade-safe.** An empty `Fields`, or a projection that renders to nothing (e.g. only a dropped
  `ShieldFlag`), ⇒ **no label** — treated exactly like an empty hand-authored label, and never a throw.
- The projected label rides the **same** fit / wrap / cap / decoration path as a hand-authored one, in
  every grammar.
- **When to auto-derive vs hand-author.** Auto-label for at-a-glance state readouts on a roster (faction
  + health tier + speed) where typing a callsign per unit is noise; hand-author for names, callsigns, and
  any text not derivable from a channel. **Keep projections compact** — the `Ring` region is the tightest;
  pick 2–3 fields, not the whole set.

## `LabelMotion` — animate the resolved label over the existing timeline

Set `LabelMotion : LabelMotion option` and the **resolved** label (explicit or auto-derived) animates as a
pure function of the motion phase the board already supplies (`animate` / `animateIn` / `filmstrip` /
`filmstripIn`). **No new entry point, no signature change, no wall-clock.**

| Kind | Effect |
|---|---|
| `TypeOn` | reveals a whole-glyph **prefix** — never mid-glyph |
| `Fade` | ramps run alpha |
| `Pulse` | oscillates size about the region centre, **capped so the scaled label still fits** |
| `Scroll` | offsets an overlong line and **clips to the region** — no overflow into adjacent channels |

- Each kind stays **fitted at every phase** and tofu-free (glyphs are unchanged, or re-emitted as real
  glyph runs).
- **Rest = static.** At the rest phase (`phase ⇒ 0`) every kind is the **identity** transform, so a
  motion-bound label at rest is byte-identical to the static label. The static entry points
  (`token` / `badge` / `ring` / `render` / `gallery` / `galleryIn`) always draw the rest frame.
- A motion bound to an **empty** label draws nothing.
- Keep motion **restrained** — it is inspection-detail, not a pop-out channel, and must not compete with
  the faction/state encodings.

## Verifying

Tofu-free output is a **render-edge** property, under every style, alignment, projection and phase:
slant wraps real glyphs, decoration is a non-text rule, and tracking splits a run into per-glyph real
glyph runs. Verify through `Symbology.Render` (sampling phases for a motion-bound label), **never** from
a pure unit test — the pure library emits the glyph-run proof nodes, requires no measurer, and never
throws without one.
