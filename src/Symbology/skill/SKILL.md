---
name: fs-gg-symbology
description: Author legible unit-symbology with the fixed channel grammar (Token -> Scene), drive the headless render->eyeball->tweak design loop, and keep the per-game stat mapping out of the library.
---

# Symbology Capability

## Scope

Owns `src/Symbology/` (the pure, Scene-only symbol vocabulary) and `src/Symbology.Render/`
(the headless Scene -> PNG bridge), their package tests, and the agent design loop that turns a
unit roster into legible abstract vector symbols. The per-game stat-to-channel mapping is **product
/ loop code shaped by this skill** — it does **not** live in the library.

## Public Contract

- Pure library `FS.GG.UI.Symbology` (`src/Symbology/Symbology.fsi`): the `Token` record (the full
  fixed channel set, whose `Label : LabelText option` is the opt-in explicit identity label, plus the
  opt-in `AutoLabel : AutoLabelSpec option` channel-projection request and `LabelMotion : LabelMotion
  option` label-bound motion — both `None` by default), the channel enums
  `Faction` / `Klass` / `Sigil` / `TokenState` / `Motion`, the label types `LabelRun` /
  `LabelText` (`Plain` | `Rich` | `Laid`), the auto-label / motion types `AutoField` / `AutoLabelSpec` /
  `LabelMotion`, and `module Symbology` with `defaultToken`, the label ctors
  `plainLabel` / `run` / `richLabel` / `paragraph` / `align` / `laidLabel` / `autoLabel` / `autoLabelSep`
  / `labelMotion`, the default-`Token` (`Grammar.Token`) renderers `token : Token -> Scene`, `animate :
  Motion -> Token -> phase:float -> Scene`, `gallery : cols -> spacing -> Token list -> Scene` and
  `filmstrip : samples -> (Motion * Token) list -> Scene`, and the **grammar-selecting** renderers
  `badge : Token -> Scene`, `ring : Token -> Scene`, `render : Grammar -> Token -> Scene`,
  `galleryIn : Grammar -> cols -> spacing -> Token list -> Scene`, `filmstripIn : Grammar -> samples ->
  (Motion * Token) list -> Scene` and `animateIn : Grammar -> Motion -> Token -> phase:float -> Scene`.
  References **only** `FS.GG.UI.Scene` — no IO, no GL, no codec call.
- Render bridge `FS.GG.UI.Symbology.Render` (`src/Symbology.Render/Render.fsi`): `Render.toPng : Size
  -> Scene -> dir:string -> string`. Wraps the public `SkiaViewer.ReferenceRendering.run` via a
  `SceneCodec` round-trip and **fails loud** (raises with joined diagnostics) on any verdict that is
  not `ReferencePassed` with a real image path — never a blank success.

Surface changes require regenerating `readiness/surface-baselines/FS.GG.UI.Symbology.txt` and
`readiness/surface-baselines/FS.GG.UI.Symbology.Render.txt` (run `scripts/refresh-surface-baselines.fsx`)
with zero drift on the existing `Scene` / `SkiaViewer` / `Controls` / `Canvas` baselines.

## The fixed channel grammar (do not invent geometry — pick from this table)

| Channel | Token field | Primitive | Reliable levels | Salience |
|---|---|---|---|---|
| Stroke **hue** -> faction | `Faction` | `Paint.stroke` colour | ~7 categorical | high |
| Motion **rhythm** -> activity | `Motion` (via `animate`) | overlay over phase | ~6 rhythms | high |
| **Size** -> magnitude | `R` | symbol radius | ~4 ordered | high |
| **Silhouette** + sigil -> class + identity | `Klass`, `Sigil` | `Path.create` + centre mark | ~6 + many | med |
| **Rotation** -> heading | `Heading` | point transform | continuous | med |
| Stroke **width** -> threat | `Threat` | `Paint.stroke` width | ~4 ordered | med |
| Interior **gradient** -> charge | `Charge` | `Shader.RadialGradient` | ~4 ordered | med |
| Belly **arc** -> health | `Health` | `Scene.arc` + green->red lerp | continuous | low |
| Tail **beads** -> speed | `Speed` | `Scene.circle` run | ~4 | low |
| Stroke **dash** -> confirmed/suspected | `TokenState` | `PathEffect.Dash` | ~3 | inspection |
| Corner **mount** -> shield | `Shield` | small mark | ~3 per slot | inspection |

A zero/empty-area `Token` (`R <= 0`) renders a visible **placeholder**, never a blank or a crash.

## Identity label (opt-in inspection-detail channel)

The `Token` carries an **optional** `Label : LabelText option` — a short identity (name / callsign /
code) drawn screen-aligned in a per-grammar label region. It is an **inspection-detail** channel: read
**after** attention lands, complementary to — never a replacement for — the vector `Sigil` and the
pre-attentive channels above. Use it only when the abstract sigil alone cannot disambiguate identity
(e.g. eight infantry variants that share a silhouette, or a board that wants callsigns).

- **Opt-in, zero-drift.** `Label = None` is the default and renders **byte-identically** to the
  pre-feature symbol — the same `'stats -> Token` mapping drives all three grammars with no per-grammar
  mapping. An empty/whitespace label is treated as no label.
- **Requires the real measurer to be tofu-free.** The pure library emits a deterministic glyph-run proof
  node and **never installs or requires a measurer**; legible, **tofu-free** glyphs come from the real
  bundled-font registry the render bridge installs. Verify tofu-free output through `Symbology.Render`,
  not from a pure unit test.
- **Keep strings short.** Overlong labels are fitted to the region (shrink, then ellipsis-truncate at a
  measured glyph boundary), so a long string degrades rather than overflowing — but short callsigns read
  best. A degenerate (`R <= 0`) labelled token still degrades to the placeholder (placeholder wins).
- **Multi-line is the SAME field — opt-in, still inspection-detail.** The one `Label : LabelText option`
  carries more than one line: embedded `\n` (and `\r\n`) are **hard breaks**, and a long line **soft-wraps**
  at whitespace to the region width. No new field, no second channel, no per-grammar mapping. Use it only
  when one line cannot carry the identity (a callsign over a code); it **complements, never replaces**, the
  vector `Sigil`. Keep to a **few short lines** — the per-grammar budget is **Token ≤ 3, Badge ≤ 2,
  Ring ≤ 2** (the ring's inner disc is tightest). Lines stack **downward** from the same spec-196 baseline,
  screen-aligned (the block never rotates with heading).
- **How multi-line surplus degrades: wrap → cap → ellipsis.** A long line **wraps** at whitespace (a single
  unbroken word too wide just shrinks/ellipsis-truncates on its own line — no mid-word break); the drawn
  line count is **capped** to the grammar budget; when lines are dropped, the **last drawn line ends with
  `…`**. Empty / whitespace / blank-lines-only ⇒ no label (interior blanks collapse, no wasted gap). A
  one-line-fitting label stays **byte-identical** to the single-line render, so adding `\n` is the only way
  to force a break. Tofu-free is still a **render-edge** property — **every** line draws real glyphs only
  through the real measurer the render bridge installs; the pure library emits the line nodes but requires
  no measurer.
- **Not governed by the linter.** The label is **not** in the legibility capacity table — `Legibility.score`
  ignores it, so its verdict is unchanged and grammar-independent. Do not use the label to dodge a
  channel-overload warning; fix the pre-attentive encoding instead.

### Rich-text runs — per-run colour / weight / size (still the SAME channel)

The label's content is a `LabelText`: **`LabelText.Plain s`** (the unstyled single-/multi-line label
above, verbatim) or **`LabelText.Rich runs`** — a short ordered sequence of styled spans. Each `LabelRun`
carries `{ Text; Color; Weight; Scale }` (plus the four feature-199 decoration/slant/tracking attributes
`Italic` / `Underline` / `Strike` / `Tracking` documented in the next section — eight fields in all), where
**`Color` / `Weight` / `Scale` are each optional** and inherit the default label style when `None` (so an
all-default run reproduces the plain label exactly).
Construct with `Symbology.plainLabel`, `Symbology.run` (a default span), and `Symbology.richLabel`; style
by record-copy, e.g. `{ Symbology.run "BRAVO-6" with Weight = Some 700; Color = Some teamBlue }`.

- **When to use.** Express an **emphasis hierarchy** inside one identity — a loud, bold callsign next to a
  dim, smaller code — so two pieces of identity can be triaged at a glance. It is still **inspection-detail**
  and still **complements, never replaces**, the vector `Sigil`.
- **Supported attributes here are colour / weight / size.** `Color` is any scene `Color`; `Weight` maps
  onto `FontSpec.Weight`; `Scale` multiplies the grammar's base label size. **Italic / underline /
  strike-through / letter-spacing** are added by **full rich-text layout** (feature 199, see below);
  **per-glyph styling and per-run font family remain out of scope** (use the sigil/geometry for anything
  louder).
- **Keep runs few and the palette restrained.** A couple of short runs with one or two deliberate styles
  reads; a rainbow of runs is noise. **Do NOT colour a run to impersonate the faction or state
  pre-attentive encodings** — those palettes are reserved for the pop-out channels; a label that mimics them
  misleads. This is a **loop guidance caveat**, not a runtime rule: author colours are used **as-is**, never
  re-mapped or rejected, and the linter's pre-attentive governance is unchanged.
- **Layered zero-drift.** `None` ≡ the pre-feature symbol; `Plain` ≡ the spec-197 label byte-for-byte; a
  `Rich` label whose runs are all default-styled ≡ the equivalent `Plain` label byte-for-byte. Only a run
  with a real colour/weight/size override changes the bytes.
- **Fitted per run, capped, tofu-free at the edge.** Each run is measured and fitted **in its own style**;
  runs flow and wrap to the region, each line's height follows its **tallest** run on a common baseline, the
  line count is **capped** to the grammar budget, and surplus ends in `…`. A run that is empty/whitespace
  drops; `Rich []` ⇒ no label. As with the plain label, **tofu-free is a render-edge property** — verify
  every run draws real glyphs through `Symbology.Render`, not from a pure unit test; the pure library
  requires no measurer and never throws without one.

### Full rich-text layout — alignment / justification / explicit breaks + decoration (feature 199, still the SAME channel)

This completes the rich-text label with the two things spec 198 deferred: **paragraph layout** and the
**typographic run attributes beyond colour/weight/size**. It is still one opt-in inspection-detail channel,
still fitted to the per-grammar region, still tofu-free at the render edge, still byte-identical to 198 when
unused.

- **Per-run decoration / slant / tracking.** Each `LabelRun` gains four optional attributes on top of
  colour/weight/size — **`Italic`** (synthetic slant), **`Underline`**, **`Strike`**, **`Tracking`**
  (letter-spacing, an em-fraction of the run size) — each `None`/`false`/`0.0`-defaulted. Set them by
  record-copy, e.g. `{ Symbology.run "quoted" with Italic = Some true }`. Use them to let a run read as
  *quoted*, deleted, tagged, or spaced **without** spending the weight/colour budget.
- **Paragraph layout — `LabelText.Laid of LabelParagraph list`.** Each `LabelParagraph` is
  `{ Runs; Align }` with **`Align = Leading | Center | Trailing | Justify`**. Construct with
  `Symbology.paragraph` (a `Center` paragraph), `Symbology.align alignment runs`, and `Symbology.laidLabel`.
  Paragraph breaks are the list boundaries; hard line breaks inside a paragraph use the runs' embedded `\n`.
  Each paragraph carries its own alignment.
- **When to use.** Reach for `Laid` when a label needs **document structure** — a centred callsign over a
  justified descriptor, a trailing retired-code line — beyond 198's flush flow. Reach for the decoration
  attributes when a run must read as distinct in *kind* (quoted/deleted/tagged), not just louder.
- **`Center` is the default and reproduces the 198 flow.** A single `Center` paragraph of all-default runs
  is **byte-identical** to the equivalent `richLabel`/`plainLabel` (layered zero drift: `None` ≡ pre-feature,
  `Plain` ≡ 197, all-default `Rich` ≡ `Plain`, default `Center` `Laid` ≡ 198). Only a non-default alignment,
  >1 paragraph, or a set decoration/slant/tracking attribute changes the bytes.
- **Justify fills the width; the last line never stretches.** `Justify` distributes measured inter-word
  space so each **wrapped** line fills the region; the **last line of each paragraph** and any **single-token
  line** fall back to leading (un-justified) — never a stretched final line, never a stretched glyph.
- **Keep it restrained — same governance caveat.** Keep paragraphs short, use a restrained alignment +
  decoration set, and **do NOT** let underline/strike/italic/justification crowd the region or **impersonate
  the faction/state pre-attentive encodings**. This is a **loop guidance caveat**, not a runtime rule:
  alignment / decoration / colours are author-supplied and used **as-is**, never re-mapped or rejected, and
  the legibility linter's pre-attentive governance is unchanged (the label — however laid out — stays
  inspection-detail, grammar-independent).
- **Fitted, capped, tofu-free — under every alignment.** Letter-spacing is folded into measurement so it
  never pushes the block past the region; underline/strike follow each **drawn fragment's** geometry (a
  wrapped run is decorated per line) and never extend past a clipped glyph; lines wrap/shrink at measured
  boundaries, the count is **capped** to the grammar budget, and surplus ends in `…`. Empty/whitespace
  paragraphs and runs drop; `Laid []` ⇒ no label. A degenerate token (`R <= 0`) still shows the placeholder
  (placeholder wins over the label). **Tofu-free is a render-edge property** — slant wraps real glyphs,
  decoration is a non-text rule, tracking splits into per-glyph real glyphs; verify through
  `Symbology.Render`, never from a pure unit test. The pure library requires no measurer and never throws
  without one. It **complements, never replaces, the vector `Sigil`**.
- **Still out of scope** (use geometry/the sigil instead): inline images, hyperlinks, bullet/numbered lists,
  per-glyph styling, per-run font family, **per-game stat → label semantics inside the library** (the
  `'stats -> Token` mapping stays the caller's), advanced bidi, any new GPU/compute path, and new font files
  (slant/underline/strike are synthesised from existing primitives).

### Auto-label — derive the label from the Token's own channels (feature 200, still the SAME channel)

Instead of hand-authoring every `Label`, a `Token` can **opt into** an auto-derived one: set
`AutoLabel : AutoLabelSpec option` and the library projects a compact, game-agnostic readout from **that
`Token`'s own encoded channels** — never a game's raw stats. Build the spec with
`Symbology.autoLabel fields` (space-joined) or `Symbology.autoLabelSep sep fields`.

- **Channel-only projection.** `AutoField` selects which channel to read and emits a fixed code:
  `FactionCode` → `ALY/ENY/NEU/CUS`, `KlassCode` → `MOB/HVY/SCT`, `StateCode` → `CFM/SUS`,
  `HealthTier` → `H`+`round(Health*100)`, `ThreatTier` → `T0..T4`, `SpeedPips` → `S0..S4`,
  `ShieldFlag` → `SHD` (dropped when `Shield = false`). The per-game `'stats -> Token` mapping stays the
  caller's — auto-label reads only the **encoded** channels (FR-002).
- **Explicit always wins.** When both an explicit `Label` and an `AutoLabel` are present, the **explicit**
  label is drawn and the projection is ignored — there is always **exactly one** resolved label or none.
- **Deterministic & degrade-safe.** Identical channels ⇒ byte-identical projection; an empty `Fields`, or a
  projection that renders to nothing (e.g. only a dropped `ShieldFlag`), ⇒ **no label** (treated exactly
  like an empty hand-authored label, no throw). The projected label rides the **same** fit/wrap/cap/decoration
  path as a hand-authored label, in every grammar.
- **When to auto-derive vs hand-author.** Auto-label for at-a-glance state readouts on a roster (faction +
  health tier + speed) where typing a callsign per unit is noise; hand-author for names / callsigns / any
  text not derivable from a channel. **Keep auto-projections compact** — the Ring region is the tightest;
  pick 2–3 fields, not the whole set. Don't **impersonate** the faction/state pre-attentive encodings or
  crowd the region. `AutoLabel = None` is the default and is **byte-identical to the pre-200 symbol**.

### Label-bound motion — animate the resolved label over the existing timeline (feature 200, no new clock)

A `Token` can bind its **resolved** label (explicit or auto-derived) to the symbology motion timeline by
setting `LabelMotion : LabelMotion option` — `LabelMotion.TypeOn | Fade | Pulse | Scroll`. The label
animates as a **pure function of the motion phase the board already supplies** (`animate`/`animateIn`/
`filmstrip`/`filmstripIn`) — **no new entry point, no signature change, no wall-clock.**

- **The four kinds.** `TypeOn` reveals a whole-glyph **prefix** (never mid-glyph); `Fade` ramps run alpha;
  `Pulse` oscillates size about the region centre (capped so the scaled label **still fits**); `Scroll`
  offsets an overlong line and **clips to the region** (no overflow into adjacent channels). Each stays
  **fitted at every phase** and **tofu-free** (glyphs are unchanged or re-emitted as real glyph runs).
- **Rest = static.** At the rest phase (`phase ⇒ 0`) every kind is the **identity** transform, so a
  motion-bound label at rest is **byte-identical to the static spec-199 label**. The static entry points
  (`token`/`badge`/`ring`/`render`/`gallery`/`galleryIn`) always draw the rest frame.
- **Auto + motion compose.** Set both: the projection resolves **first**, then the resolved label animates —
  deterministic, fitted, tofu-free.
- **Restraint + degrade-safe.** Keep motion **restrained** (it is inspection-detail, not a pop-out channel —
  don't let it compete with the faction/state encodings). `LabelMotion = None` is the default and is
  **byte-identical to the pre-200 symbol across the whole timeline**. A motion bound to an empty label draws
  nothing; a degenerate (`R <= 0`) token shows the **placeholder** (placeholder wins over auto/motion); the
  pure library needs no measurer and never throws.
- **Tofu-free is a render-edge property** — verify auto-derived + motion-bound output through
  `Symbology.Render` under the real measurer (every run non-tofu at sampled phases), never from a pure unit
  test. Auto-label and label-bound motion **complement, never replace, the vector `Sigil`**, and the
  legibility linter's pre-attentive governance is **unchanged** (the label, however derived or animated,
  stays inspection-detail and out of the capacity table).

## Selectable grammars (form factors) — one channel set, three drawings

The **same fixed channel set above** drives three interchangeable symbol **grammars**. The choice is a
first-class value `Grammar = Token | Badge | Ring`; one `'stats -> Token` ChannelMap feeds any of them
**unchanged** — switching grammar changes only the *drawing*, never the per-game mapping.

| Grammar | `Symbology.badge` / `ring` / `token` | Shape | Prefer when |
|---|---|---|---|
| **Token** (`Grammar.Token`) | `token` | heading-rotated silhouette | motion/heading is primary; the v1 default |
| **Badge** (`Grammar.Badge`) | `badge` | compact, **screen-aligned** framed emblem (class-driven frame, bottom health bar, speed-pip row, edge heading pip) | dense rosters / insignia walls where a stable upright frame reads faster than a rotating body |
| **Ring** (`Grammar.Ring`) | `ring` | centred **radial gauge** (outer ring hue/threat/state, health **arc sweep** monotone in health, rim speed beads, heading needle) | continuous channels (health, charge) should read as radial quantities at a glance |

- Render a selected grammar with `Symbology.render grammar token`; build review boards with
  `galleryIn grammar …`, `filmstripIn grammar …`, `animateIn grammar …` to A/B form factors.
- **Screen-aligned (Badge/Ring)**: the frame/ring never rotate with heading — heading is a discrete edge
  pip (Badge) or centre needle (Ring), so upright legibility holds at any heading.
- **Grammar-agnostic motion only** on Badge/Ring: `animateIn` applies the centre/radius rhythms
  (Pulse/Blink/Damage); directional rhythms (Spin/Moving) degrade to the static base symbol there.
- The **ChannelMap is identical across grammars**, so the legibility linter's verdict is
  **grammar-independent** — it scores the `Token` channel values, never which grammar draws them.
- `Grammar.Token` reproduces the existing `token`/`gallery`/`filmstrip`/`animate` **byte-for-byte**.

## Legibility rules — encode these and CRITIQUE every board against them at the target size

- **Assign-by-urgency**: the most urgent state goes on the most salient channels (hue, motion, size).
- **Redundancy on critical state**: encode urgent state across *multiple* pre-attentive channels.
- **One active motion at a time**: never stack motion rhythms on one symbol (`animate` takes one `Motion`).
- **Never critical state on dash alone**: dash + corner mounts are inspection-only channels.
- **No faction/state hue collision (FR-019)**: faction rides the saturated stroke-hue palette; inspection
  state rides the dash channel — they never share the hue channel. (State *semantics* that need colour
  reuse the repo's Ant status tokens via `fs-gg-ant-design`, never the faction palette.)
- **Critique checklist**: faction separable? class distinct? health readable at the target on-board
  size? any channel overloaded beyond its reliable level count above?

## Grammar vs mapping — the pattern

The **grammar** (this library) is fixed. The **mapping** `'stats -> Token` is per-game *data* you edit
each iteration. Build from `Symbology.defaultToken` and override only the fields the game encodes, so the
unit of change every round is the mapping, never the library.

## FSI recipe (the loop's core move)

```fsharp
#r "nuget: FS.GG.UI.Symbology"          // or #r the built in-tree DLLs
#r "nuget: FS.GG.UI.Symbology.Render"
open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

type UnitStats = { Side: string; Role: string; Dps: float; Hp: float; HpMax: float; Speed: float; Armor: float; Facing: float }

// the editable per-game ChannelMap (data — NOT library internals):
let mapUnit (u: UnitStats) : Token =
    { Symbology.defaultToken with
        R       = 28.0
        Faction = (match u.Side with "blue" -> Ally | "red" -> Enemy | _ -> Neutral)
        Klass   = (match u.Role with "tank" -> Heavy | "scout" -> Scout | _ -> Mobile)
        Threat  = min 1.0 (u.Dps / 120.0)
        Health  = u.Hp / u.HpMax
        Speed   = int (min 4.0 (u.Speed / 4.0))
        Shield  = u.Armor > 30.0
        Heading = u.Facing }

let board = Symbology.gallery 4 90.0 (roster |> List.map mapUnit)
let png   = Render.toPng { Width = 920; Height = 660 } board "./work/iter-001"
// -> read `png` back, CRITIQUE at the target size, capture feedback, TWEAK mapUnit ONLY, repeat.
```

See `reference.fsx` in this skill folder for a runnable in-tree version.

## The fixed feedback loop (FR-014 / FR-016 — the unit of change is the mapping, never the grammar)

```
1. INTAKE   read roster + stats; pick grammar — Token (default), Badge, or Ring (all share the ChannelMap).
2. MAP      draft ChannelMap : 'stats -> Token  (assign-by-urgency; redundancy on critical state).
3. RENDER   FSI: build `Symbology.gallery ...`; `Render.toPng size scene dir`; READ THE PNG BACK.
4. CRITIQUE two complementary checks against the legibility rules at the target size:
            (a) LINT   `Legibility.score (roster |> List.map mapUnit)` (animated boards: `scoreAnimated`
                       over the `(motion, token)` pairs) — the mechanical backstop. Inspect `report.Verdict`
                       and `report.Findings`: any `Warning`/`Error` names the overloaded/out-of-domain
                       `Channel`, used-vs-capacity, and the contributing unit indices. Treat a non-`Clean`
                       verdict as a TWEAK trigger (the unit of change stays the mapping — never the grammar).
            (b) EYE    human-style self-check of the PNG vs the rules (the linter cannot see crowding,
                       contrast, or label collisions — the eyeball check stays).
5. REVIEW   present the PNG to the human; capture feedback.
6. TWEAK    adjust the ChannelMap / Token params ONLY (never library internals) until the linter is `Clean`
            and the eyeball check passes; goto 3.
7. APPROVE  on satisfaction: emit final symbol-set module + rationale; pin a golden board.
```

> The linter (`FS.GG.UI.Symbology.Legibility`) is pure/deterministic and scores the *produced symbol set*
> against the fixed §4 capacities — it is the mechanical complement to the eyeball check, not a replacement.
> The approved M5/M6 roster lints `Clean`, so a fresh `Warning` is a real signal to re-tune the mapping.

## Provenance the loop MUST write (FR-017 / FR-018)

- **Every iteration** -> under the working dir: a *timestamped board image* (the rendered gallery PNG)
  **and** a *snapshot of the mapping* that produced it. Together these form an auditable history.
- **On approval** -> a *final symbol-set module* (pure drawing-producing functions), a *design
  rationale* (channel assignments + rejected alternatives + legibility notes), and a *pinned golden
  board* with a stable `SceneCodec` identity.
- Timestamps/filenames are stamped by the **workflow**, not by library code — the library and render
  helper read no clock, so a re-render of an unchanged mapping is byte-identical (determinism).

## Build Commands

Run `dotnet build src/Symbology/Symbology.fsproj` and `dotnet build src/Symbology.Render/Symbology.Render.fsproj`.

## Test Commands

Run `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` (pure: determinism, channel presence,
codec fidelity, placeholder, gallery, motion, filmstrip) and
`dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` (render smoke + fail-loud).

## Evidence

Determinism identity is `SceneCodec.export scene |> _.CanonicalBytes`; the render bridge additionally
emits a content-addressable PNG + `reference-evidence.md` per call (a regression identity). Stable
public surface baselines live under `readiness/surface-baselines/`.

## Package Boundary

`FS.GG.UI.Symbology` references **only** `FS.GG.UI.Scene` — never SkiaViewer, Controls, Canvas, Elmish,
Layout, or any host/IO. All raster/IO stays in `FS.GG.UI.Symbology.Render`, which is the only component
that may reference `SkiaViewer`. Keep the game-symbol vocabulary off the core control surface.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own documentation),
then community sources (forums, Reddit, Q&A sites, issue trackers and changelogs). Record findings and
resolving links in the feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this
skill's **Sources** line. Offline, the mandate degrades to recording "research blocked — <why>" rather
than hard-failing the phase.

## Related

- [[fs-gg-scene]] supplies the pure primitives (`Path`, `Paint`, `Shader.RadialGradient`, `arc`) this grammar composes.
- [[fs-gg-skiaviewer]] owns the `ReferenceRendering` path the render bridge wraps.
- [[fs-gg-ant-design]] supplies the status palette used for state semantics (kept off the faction hue).
- [[fs-gg-testing]] validates determinism, codec fidelity, and render readiness evidence.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven rendering library): https://github.com/mono/SkiaSharp
