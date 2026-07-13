---
name: fs-gg-symbology
description: Author legible unit-symbology with the fixed channel grammar (Token -> Scene), drive the headless render->eyeball->tweak design loop, and keep the per-game stat mapping out of the library.
metadata:
  author: FS.GG
  source: specs/192-agent-unit-symbology
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
- Legibility linter `FS.GG.UI.Symbology.Legibility` (`src/Symbology/Legibility.fsi`): the **third**
  public module, shipped in the same pure `FS.GG.UI.Symbology` package and covered by the same surface
  baseline. It is what step 4a of the loop below runs. Pure, deterministic and advisory — it never
  mutates, and never raises on valid input.

  ```fsharp
  val table: ChannelSpec list                                  // the §4 Kind/Capacity columns, machine-readable
  val score: tokens: Token list -> Report                      // a static board, grammar-blind
  val scoreAnimated: board: (Motion * Token) list -> Report    // + whole-board motion load

  // Grammar-AWARE siblings: the same findings, plus the ones that depend on which grammar draws.
  val scoreIn: grammar: Grammar -> tokens: Token list -> Report
  val scoreAnimatedIn: grammar: Grammar -> board: (Motion * Token) list -> Report

  type Verdict     = Clean | HasWarnings              // Clean iff Findings is empty
  type Severity    = Warning | Error                  // Error = ungrammatical; Warning = encodable but overloaded
  type ChannelKind = Categorical | Ordered | Continuous
  type Report      = { Findings: Finding list; Usage: ChannelUsage list; Verdict: Verdict }
  type Finding     = { Channel: Channel; Severity: Severity; Message: string; Units: int list }
  type ChannelSpec = { Channel: Channel; Kind: ChannelKind; Capacity: int }
  type ChannelUsage= { Channel: Channel; Kind: ChannelKind; DistinctLevels: int; Capacity: int }
  ```

  `Channel` has **14** cases: the 12 per-unit channels of §4, plus `Motion` and `Label`. Neither of the
  last two is a `table` row: **`Motion` is whole-board** — no `ChannelKind`, no `ChannelUsage` entry —
  and reaches you only as a `Finding.Channel` from `scoreAnimated`'s motion-load check (its budget of 1
  lives in the finding's `Message`); **`Label` is budgeted in lines, per grammar**, so only
  `scoreIn`/`scoreAnimatedIn` ever raise it. `Findings` and `Usage` come back in table order — findings
  then by ascending unit index — so re-scoring an equal board yields an equal report. On an overload
  `Warning`, `Units` names only the units carrying levels **past** capacity, the smallest set a re-map
  has to move, not the whole board; whole-board findings carry `Units = []`.

  **`score` is grammar-blind by contract** and stays so (a test locks it in). `scoreIn` is strictly
  additive — it never removes a finding `score` would emit — and adds the two facts that depend on the
  drawing:

  | grammar-conditional fact | raised by | severity |
  |---|---|---|
  | Badge/Ring cannot draw `Motion.Spin` / `Motion.Moving` — the unit renders identically to `Idle` | `scoreAnimatedIn` | `Error` |
  | the identity label needs more lines than the grammar draws (Token 3, Badge 2, Ring 2) | `scoreIn` | `Warning` |

  The label check reads **hard line breaks only**: the drawn count also depends on greedy wrapping,
  which needs a text measurer, and the linter is measurement-free by contract. Wrapping only *adds*
  lines, so the check under-reports and never false-positives. It is a backstop, not a layout oracle.
- Render bridge `FS.GG.UI.Symbology.Render` (`src/Symbology.Render/Render.fsi`): `Render.toPng : Size
  -> Scene -> dir:string -> string`. Wraps the public `SkiaViewer.ReferenceRendering.run` via a
  `SceneCodec` round-trip and **fails loud** (raises with joined diagnostics) on any verdict that is
  not `ReferencePassed` with a real image path — never a blank success.

Surface changes require regenerating `readiness/surface-baselines/FS.GG.UI.Symbology.txt` and
`readiness/surface-baselines/FS.GG.UI.Symbology.Render.txt` (run `scripts/refresh-surface-baselines.fsx`)
with zero drift on the existing `Scene` / `SkiaViewer` / `Controls` / `Canvas` baselines.

## The fixed channel grammar (do not invent geometry — pick from this table)

`Legibility.table` is the **single source** of the `Kind` and `Capacity` columns below; a test
(`Symbology.Tests/LegibilityDoctrineTests.fs`) parses this table and fails if the two disagree. Change
the F# table, never this prose alone. Rows are in `Legibility.table` order, with whole-board `Motion`
last (it has no row).

| Channel | Token field | Primitive | Kind | Capacity | Salience |
|---|---|---|---|---|---|
| Stroke **hue** -> faction | `Faction` | `Paint.stroke` colour | Categorical | 7 | high |
| **Silhouette** -> class | `Klass` | `Path.create` | Categorical | 6 | med |
| Centre **sigil** -> identity | `Sigil` | centre mark | Categorical | 12 | med |
| Stroke **dash** -> confirmed/suspected | `State` | `PathEffect.Dash` | Categorical | 3 | inspection |
| Corner **mount** -> shield | `Shield` | small mark | Categorical | 3 | inspection |
| Tail **beads** -> speed | `Speed` | `Scene.circle` run | Ordered | 4 | low |
| **Size** -> magnitude | `R` | symbol radius | Ordered | 4 | high |
| Stroke **width** -> threat | `Threat` | `Paint.stroke` width | Ordered | 4 | med |
| Interior **gradient** -> charge | `Charge` | `Shader.RadialGradient` | Ordered | 4 | med |
| Belly **arc** -> health | `Health` | `Scene.arc` + green->red lerp | Continuous | — | low |
| **Rotation** -> heading | `Heading` | point transform | Continuous | — | med |
| **Barrel** -> secondary heading | `SecondaryHeading` | centre-out line + tip mark | Continuous | — | med |
| Motion **rhythm** -> activity | `Motion` (via `animate`) | overlay over phase | whole board | budget 1 | high |

**Capacity is what the eye separates, not what the grammar can draw.** `Speed` renders `0..6` beads —
all seven bead counts are in-domain, and `Legibility.score` errors *outside* that range — but only its
capacity-many are reliably *ranked* at board size, so spending more distinct speeds than the capacity is
a `Warning`, not an `Error`. The same split holds for `Faction` and `Sigil`, whose domains are open
(`Custom` colours, `Mark` paths). Domain violations are per unit; overloads are per board.

`Size`, `Threat` and `Charge` carry a `float`, and it is your **mapping's job to quantise it**: a radius
ramp of twelve distinct values is twelve levels, and the linter says so. `Health` and the two rotations
are the only genuinely continuous channels — read as a position on a scale, never as a rank.

`Motion` is scored **per board, not per unit**: the grammar offers five non-`Idle` rhythms (`Pulse`,
`Spin`, `Blink`, `Damage`, `Moving`) and you may have **one** of them live across the whole board
(`Legibility.scoreAnimated` warns above that). It is a palette to choose from, not a rank to spend — the
strictest channel in the grammar, because a second rhythm competes for the same attention grab instead
of adding a level to it. It has no `Legibility.table` row and no `ChannelUsage` entry.

A zero/empty-area `Token` (`R <= 0`) renders a visible **placeholder**, never a blank or a crash.

## Two rotations (opt-in second heading)

`Heading` is where the unit **faces**. `SecondaryHeading : float option` is where it **points**, when
that is a different thing — a turret on a hull, a weapon arc, a sensor or gaze direction. It is `None`
by default, and a `None` token renders byte-identically to one with no such channel at all.

```fsharp
{ Symbology.defaultToken with Heading = hull.Facing; SecondaryHeading = Some turret.Facing }
```

Both are absolute angles, `0.0` = north; they wrap, so any finite value is in-domain. Each grammar
draws the second one as a **barrel with a tip mark**, starting clear of the centre sigil and sited so it
cannot be misread as the primary indicator: it overshoots the hull in `Token`, stops inside the rim pip
in `Badge`, and pushes its tip outside the ring in `Ring`. Leave it `None` unless the two angles
genuinely differ — a barrel that always agrees with the nose spends a channel to say nothing.

A barrel is the widest thing a symbol draws, so `filmstrip` widens its cells when any token sets the
channel (and keeps the historic spacing exactly when none does).

## Identity label (opt-in inspection-detail channel)

Three optional `Token` fields — all `None` by default — form **one** channel: a short identity (name /
callsign / code) drawn screen-aligned in a per-grammar region. Use it only when the abstract `Sigil`
alone cannot disambiguate identity (eight infantry variants that share a silhouette; a board that wants
callsigns). One `'stats -> Token` mapping still drives all three grammars.

| Field | Type | What it does |
|---|---|---|
| `Label` | `LabelText option` | the explicit identity — `Plain` text, `Rich` styled runs, or `Laid` paragraphs |
| `AutoLabel` | `AutoLabelSpec option` | projects the label from the `Token`'s **own encoded channels** |
| `LabelMotion` | `LabelMotion option` | binds the resolved label to the motion phase the board already supplies |

An explicit `Label` **always wins** over `AutoLabel`, so there is always **exactly one** resolved label,
or none. Per-grammar line budget: **`Token` ≤ 3, `Badge` ≤ 2, `Ring` ≤ 2** (the ring's inner disc is
tightest). Construct with the ctors below, and style by record-copy:

```fsharp
Symbology.plainLabel "BRAVO-6"                                             // Plain — one or more lines
Symbology.richLabel [ { Symbology.run "BRAVO-6" with Weight = Some 700 } ] // Rich — styled runs
Symbology.laidLabel [ Symbology.paragraph [ Symbology.run "BRAVO-6" ]      // Laid — paragraphs
                      Symbology.align Trailing [ Symbology.run "R-12" ] ]
Symbology.autoLabel [ FactionCode; HealthTier ]                            // project from channels
```

`Symbology.autoLabelSep sep fields` joins a projection with a separator other than a space, and
`Symbology.labelMotion kind` builds the `LabelMotion` value.

### The invariants — they hold for every field, style, projection and phase

- **Opt-in, layered zero-drift.** Each layer is byte-identical to the one beneath it when unused: `None`
  ≡ the pre-feature symbol; `Plain` ≡ the single-line label; an all-default `Rich` ≡ `Plain`; a default
  `Center` `Laid` ≡ `Rich`; `AutoLabel` = `None` ≡ the explicit-label symbol; and `LabelMotion` = `None`
  ≡ the static label **across the whole timeline**, as is any motion-bound label **at rest**. Only a real
  override changes the bytes.
- **Inspection-detail.** Read **after** attention lands. It **complements — never replaces** — the vector
  `Sigil` and the pre-attentive channels above.
- **Outside the capacity table.** `Legibility.score` ignores the label, so its verdict is unchanged and
  grammar-independent. Never use a label to dodge a channel-overload warning — fix the pre-attentive
  encoding instead.
- **But it has a per-grammar LINE budget**: 3 lines under `Grammar.Token`, 2 under `Badge` and `Ring`.
  Past that, `wrapLabel` drops the surplus and marks the last drawn line with an ellipsis — silently, as
  far as `score` is concerned. `Legibility.scoreIn grammar` raises a `Label` `Warning` naming the units
  whose lines will vanish. A label is still not a channel; the budget is about what survives the draw.
- **Tofu-free is a render-edge property.** Assert it through `Symbology.Render`, never from a pure unit
  test — see [Troubleshooting](#troubleshooting).
- **Surplus degrades: wrap → cap → ellipsis.** Lines wrap at whitespace, the drawn line count is **capped**
  to the grammar budget, and the last drawn line ends with `…`. Empty, whitespace, or a projection that
  renders to nothing ⇒ **no label**. A degenerate (`R <= 0`) token shows the **placeholder** — it always wins.
- **Do not impersonate the pre-attentive encodings.** A label styled to mimic the faction or state palettes
  misleads, and the linter will not catch it. This is a **loop guidance caveat, not a runtime rule**: author
  colours, alignment and decoration are used **as-is** — never re-mapped or rejected.

The per-feature detail — the eight `LabelRun` attributes, paragraph alignment and justification, the
`AutoField` codes, the four `LabelMotion` kinds, and the exact degrade order — lives in
[`reference/labels.md`](reference/labels.md).

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
  (Pulse/Blink/Damage); directional rhythms (Spin/Moving) are **dropped**, not degraded — the symbol is
  byte-identical to the `Idle` one, so the channel is gone rather than quieter. `scoreAnimatedIn` errors.
- The **ChannelMap is identical across grammars**, so `Legibility.score`'s verdict is
  **grammar-independent** — it scores the `Token` channel values, never which grammar draws them. That is
  a deliberate contract, not a complete story: a channel the selected grammar *cannot draw* is invisible
  to it. Run **`scoreIn grammar`** (or `scoreAnimatedIn`) to price that; it adds findings, never removes them.
- `Grammar.Token` reproduces the existing `token`/`gallery`/`filmstrip`/`animate` **byte-for-byte**.

## Legibility rules — encode these and CRITIQUE every board against them at the target size

- **Assign-by-urgency**: the most urgent state goes on the most salient channels (hue, motion, size).
- **Redundancy on critical state**: encode urgent state across *multiple* pre-attentive channels.
- **One active rhythm per board**: across the whole board, use at most one non-`Idle` `Motion` — a
  second rhythm competes with the first instead of adding a level. (Stacking rhythms on a *single*
  symbol is impossible by type: `animate` takes one `Motion`. The rule you can break is the board one,
  and `scoreAnimated` is what catches it.)
- **Never critical state on dash alone**: dash + corner mounts are inspection-only channels.
- **No faction/state hue collision (FR-019)**: faction rides the saturated stroke-hue palette; inspection
  state rides the dash channel — they never share the hue channel. (State *semantics* that need colour
  reuse the repo's Ant status tokens via `fs-gg-ant-design`, never the faction palette.)
- **Critique checklist**: faction separable? class distinct? health readable at the target on-board
  size? any channel overloaded beyond its capacity above?

## Grammar vs mapping — the pattern

The **grammar** (this library) is fixed. The **mapping** `'stats -> Token` is per-game *data* you edit
each iteration. Build from `Symbology.defaultToken` and override only the fields the game encodes, so the
unit of change every round is the mapping, never the library.

## When the grammar can't encode it

The two rules above — *do not invent geometry*, and *the grammar is fixed* — together leave **no legal
move** when a game holds a state that no channel expresses. That gap is real, not hypothetical, and it
has exactly one sanctioned exit: **ask for the channel**.

Do **not** invent geometry. Do **not** overload a channel that already means something else: the linter
counts a channel's **distinct levels**, and cannot see that two of them now mean different *kinds* of
thing — a second meaning that fits inside the capacity lints `Clean` and still fails the eye. Instead
open a `cross-repo:request` issue against the **`fs-gg-symbology`** contract — the
[[cross-repo-coordination]] skill files it — naming the state you cannot encode and what you would draw
for it. Three outcomes are possible, and which one is **Rendering's call, not the consumer's**:

1. **A caller-drawn `Sigil.Mark of PathSpec`** — cheap, and usually wrong. The grammar does not rotate
   or animate a caller path, so you re-implement the transform outside the vocabulary, where
   `Legibility` cannot see it: the linter scores `Sigil` as the *identity* channel it is, never the
   rotation you smuggled through it. And `Sigil` is the identity slot, so spending it costs you your
   identity mark.
2. **An additive opt-in channel**, `None` by default and rendering byte-identically when unset — the
   zero-drift pattern `Label` / `AutoLabel` / `LabelMotion` / `SecondaryHeading` have now established
   four times. Adding to the fixed set is a deliberate, reviewed, one-time act **by the library**; it is
   never a thing a mapping may do for itself.
3. **Declared out of scope** — the vocabulary draws unit *symbols*, not vehicle *schematics*.

Filing the request *is* the move the doctrine asks of you. "The grammar is fixed" constrains the
mapping; it is not a refusal to grow.

### Worked example — the turret with nowhere to go

<!-- skill-refs: closed-ok FS.GG.Rendering#260 — cited as the issue that PROMPTED ADR-0102, which answered it. History; it stays closed. -->


FS.GG.Game mapped a tank and found `Token` carried a single `Heading`. A tank rotates twice: the **hull**
decides which armor plate a shot lands on, the **turret** where the gun points. Neither is inspection
detail, and the skill offered no legal way to say both. They filed
[#260](https://github.com/FS-GG/FS.GG.Rendering/issues/260) rather than working around it — and said
plainly that they did not object to any of the three options, they just wanted the outcome *recorded*.

The answer was **(2)**, recorded as
[ADR-0102](../../../docs/product/decisions/0102-symbology-secondary-heading-channel.md):
`Token.SecondaryHeading : float option`. All three grammars draw it and **none degrades**; it is
`Continuous` in `Legibility.table`, therefore overload-exempt, and only a non-finite angle is an `Error`.

The instructive part is what was **rejected**. A per-unit *"the two headings nearly agree"* `Warning` was
considered and dropped: a tank driving forward with its gun forward is the **normal rest state**, so the
check would have fired constantly on correct input. Two rotations on one glyph genuinely *is* a
legibility hazard — and it was discharged by **drawing**, not by linting. Each grammar sites the barrel
where no primary indicator lives, so extent and form separate the two even when the angles agree.

Two lessons generalise past this case:

- **A hazard the grammar can draw its way out of is not a linter finding.** Reach for form and siting
  before you reach for a `Warning`; a rule that fires on the common correct input teaches users to
  ignore the linter.
- **The consumer's job ended at "we cannot encode this."** They named the state, named the cost of each
  option, and stopped. Inventing the answer — a rotated `Sigil.Mark`, a second meaning on `Speed` — is
  what the doctrine exists to prevent, and it is what filing the request buys you out of.

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

### The golden reference — [`samples/SymbologyBoard/`](../../../samples/SymbologyBoard/)

An approved, lint-clean, three-grammar mapping you can read instead of inventing one:

- [`Roster.fs`](../../../samples/SymbologyBoard/Roster.fs) — the **approved M5/M6 mapping**, compiled
  unchanged from the M5 dry run. `tests/SymbologyBoard.Tests/BoardTests.fs` asserts it lints `Clean`, so
  it is the reference every "a fresh `Warning` is a real signal" claim below is measured against.
- [`GrammarCompare.fs`](../../../samples/SymbologyBoard/GrammarCompare.fs) — the **executable form of the
  "one mapping, three drawings" claim**: one `Token` set, one grid, rendered as three stacked bands
  (Token / Badge / Ring) so you can A/B form factors.
- [`Board.fs`](../../../samples/SymbologyBoard/Board.fs) — the same roster on a deterministic live board,
  with each unit's approved `Symbology.animate` motion overlay.

## The fixed feedback loop (FR-014 / FR-016 — the unit of change is the mapping, never the grammar)

```
1. INTAKE   read roster + stats; pick grammar — Token (default), Badge, or Ring (all share the ChannelMap).
2. MAP      draft ChannelMap : 'stats -> Token  (assign-by-urgency; redundancy on critical state).
3. RENDER   FSI: build `Symbology.gallery ...`; `Render.toPng size scene dir`; READ THE PNG BACK.
4. CRITIQUE two complementary checks against the legibility rules at the target size:
            (a) LINT   `Legibility.scoreIn grammar (roster |> List.map mapUnit)` (animated boards:
                       `scoreAnimatedIn grammar` over the `(motion, token)` pairs) — the mechanical backstop.
                       Pass the grammar you picked in step 1: `scoreIn` is `score` plus the findings that
                       depend on the drawing (a rhythm Badge/Ring cannot draw; a label over the grammar's
                       line budget), so it is what catches a mapping that is legal in the abstract and
                       illegible as rendered. Inspect `report.Verdict` and `report.Findings`: any
                       `Warning`/`Error` names the overloaded/out-of-domain `Channel`, used-vs-capacity,
                       and the contributing unit indices. Treat a non-`Clean` verdict as a TWEAK trigger
                       (the unit of change stays the mapping — never the grammar).
            (b) EYE    human-style self-check of the PNG vs the rules (the linter cannot see crowding,
                       contrast, or label collisions — the eyeball check stays).
5. REVIEW   present the PNG to the human; capture feedback.
6. TWEAK    adjust the ChannelMap / Token params ONLY (never library internals) until the linter is `Clean`
            and the eyeball check passes; goto 3.
7. APPROVE  on satisfaction: emit final symbol-set module + rationale; pin a golden board.
```

> The linter (`FS.GG.UI.Symbology.Legibility`) is pure/deterministic and scores the *produced symbol set*
> against the fixed §4 capacities — it is the mechanical complement to the eyeball check, not a replacement.
> The approved M5/M6 roster ([`samples/SymbologyBoard/Roster.fs`](../../../samples/SymbologyBoard/Roster.fs))
> lints `Clean`, so a fresh `Warning` is a real signal to re-tune the mapping.

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

## Troubleshooting

The recurring failure modes, collected. Three of the four are the contract working as designed.

- **Tofu boxes (`□□□`) where a label should be** — you asserted glyph content from a **pure unit test**.
  Tofu-free is a **render-edge** property: the pure library emits deterministic glyph-run *proof* nodes
  and **never installs, requires, or throws without** a measurer. Real glyphs come from the bundled-font
  registry that the render bridge installs, so verify through `Symbology.Render` — sampling phases when
  the label is motion-bound. Not a bug in the pure layer.
- **`Render.toPng` raised** — that is the **fail-loud contract**, not a bug. Any verdict that is not
  `ReferencePassed` with a real image path raises with the joined diagnostics. It never returns a blank
  success, which is precisely why a critique never reasons over an empty PNG. Read the diagnostics.
- **A blank or placeholder symbol** — `R <= 0`. You get a **fixed 12px grey box with an X**, at any radius.
  The guard sits in `drawSymbolAt`, *before* label resolution, so it swallows the body, the `Sigil`, the
  label and the auto-label alike. It is **not** a motion guard, though — only `Pulse` suppresses itself on
  a degenerate token. `Blink` still draws its red dot on top (that dot has a 2px floor), `Moving` draws the
  echo as a *second* offset placeholder, and `Spin` / `Damage` emit overlay nodes that are simply degenerate
  at `R = 0`. So a placeholder box with a stray dot beside it is a bad radius, not a motion bug. Fix the
  mapping.
- **`NU1403` on restore** — the known poisoned-NuGet-cache trap. Restore against a **scratch
  `NUGET_PACKAGES` directory**; do **not** clear the shared cache.

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
