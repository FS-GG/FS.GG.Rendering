---
name: fs-gg-symbology
description: Map a unit roster to legible vector symbols in a generated FS.GG.UI product, and run the headless render-and-look loop — render a frame (any Scene) to a PNG with `Render.toPng` and look at it to visually inspect the output, the fastest way to catch defects no requirements-derived test does.
---

# Symbology Capability

## Scope

Use this skill for product code that turns per-unit stats into legible abstract vector symbols: build
a per-game `'stats -> Token` mapping, compose `gallery` / `filmstrip` boards, and rasterise them
headlessly to critique at the target on-board size. The grammar is fixed; the mapping is yours to edit.

## Public Contract

The signatures you consume are bundled with this product under `docs/api-surface/Symbology/` (the pure
`Symbology.fsi` **and** `Legibility.fsi`) and `docs/api-surface/Symbology.Render/` (the `Render.fsi`
bridge). The pure library references only `Scene`; all raster/IO is in the render bridge. Build from
`Symbology.defaultToken` and override only the fields your game encodes.

`Legibility` is the pure package's third public module — the linter your CRITIQUE step runs. Pure,
deterministic, advisory; it never mutates and never raises on valid input:

```fsharp
val table: ChannelSpec list                                  // the fixed capacity table, machine-readable
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

`Finding.Units` are 0-based indices into the list you scored, so they point straight back at your roster.
On an overload `Warning` they name only the units carrying levels **past** capacity — the smallest set your
re-map has to move — not the whole board; whole-board findings carry `Units = []`.

**Read `Legibility.table`, never a copy of it** — including this page. Each row's `Kind` tells you what the
linter does with that channel: `Categorical` and `Ordered` channels have their distinct levels **counted**
and overload past `Capacity`, while `Continuous` ones are read as a position on a scale and are
**overload-exempt** — only their domain is checked. `report.Usage` hands you the `Kind`, `DistinctLevels`
and `Capacity` of every scored channel for the board you just linted. `Channel` also carries a whole-board
`Motion` case — no `ChannelKind`, no `table` row, no `ChannelUsage` entry — raised by `scoreAnimated` when
more than one non-`Idle` rhythm is live at once, and a `Label` case, budgeted in lines rather than levels.

**`score` is grammar-blind by contract**: it reads your `Token` channel values, never which grammar draws
them. That is deliberate, and it means a channel the grammar *cannot draw* is invisible to it. Pass the
grammar to **`scoreIn`** (or `scoreAnimatedIn`) to price those too — it only ever *adds* findings:

| grammar-conditional fact | raised by | severity |
|---|---|---|
| Badge/Ring cannot draw `Motion.Spin` / `Motion.Moving` — the unit renders identically to `Idle` | `scoreAnimatedIn` | `Error` |
| the identity label needs more lines than the grammar draws (Token 3, Badge 2, Ring 2) | `scoreIn` | `Warning` |

The label check counts **hard line breaks only**. The drawn count also depends on greedy wrapping, which
needs a text measurer the pure linter does not have; wrapping only *adds* lines, so the check under-reports
and never false-positives.

`Coverage` is the pure package's coverage module — the **visual analog of match exhaustiveness**. Where
`Legibility` scores the tokens you DID map, `Coverage` asks whether every gameplay element is mapped **at
all**: a door/bomb/explosion/projectile/`EnemyKind` you add and forget to give a visual renders nothing,
with no error, and only a human eyeballing a `Render.toPng` board would catch it. Same pure/deterministic/
advisory contract. The element type is **yours** — the library owns the check and the opt-out, never your
per-game list:

```fsharp
// A declared element's visual disposition — one visual "match arm".
type Representation = Shown of token: Token | Hidden of reason: string
type Gap            = Missing | Unreasoned                 // Missing = forgotten; Unreasoned = blank opt-out
type Verdict        = Covered | HasGaps                    // Covered iff Findings is empty
type Finding<'element> = { Element: 'element; Gap: Gap; Message: string }
type Report<'element>  = { Findings: Finding<'element> list  // declared-element order (deterministic)
                           OptedOut: ('element * string) list // the explicit hidden-by-mechanic ledger
                           Verdict: Verdict }

// Report every declared element with no Token AND no reasoned opt-out.
val check: elements: 'element list -> resolve: ('element -> Representation option) -> Report<'element>
// The canonical pattern: a forgotten element is an absent key. ≡ check elements (fun e -> Map.tryFind e table).
val checkMap: elements: 'element list -> table: Map<'element, Representation> -> Report<'element>
```

An element passes coverage **iff** it maps to a `Shown` token **OR** carries an explicit `Hidden` opt-out
with a non-blank reason. A hidden element (fog of war, stealth, an off-screen or internal marker) is legal
— but it must be a **reasoned decision**, not silence: a `Hidden ""` is `Unreasoned` and rejected, and a
`resolve` that returns `None` is `Missing`. The reasoned opt-outs collect in `report.OptedOut` as your
audit trail. **Gate your product with `Expect.equal report.Verdict Coverage.Covered`** over your declared
element set, so adding an element without a visual (or an explicit opt-out) reds before ship.

### The scaffold ships this gate for you (game profile)

You don't hand-author that gate — a **game** product is scaffolded with it already wired. The scaffold
emits an **element↔visual catalog** artifact and a coverage test that reads it:

- **The catalog** lives at `tests/Product.Tests/element-visuals.catalog` — the machine-readable,
  deterministic text form of your renderable-element set. It is a versioned header line followed by one
  tab-separated row per gameplay element: `element<TAB>shown<TAB>token-handle`, or
  `element<TAB>hidden<TAB>reason`. The `shown` handle is a **stable name** into your own symbol module
  (the renderer resolves it — coverage only asks whether the element is shown *at all*). This is the
  format the `FS.GG.UI.Symbology.Catalog` module renders and parses; the design loop that authors and
  maintains it is the **`fs-gg-symbol-design`** skill. A starter catalog covering the Pong elements:

  ```text
  # fs-gg element-visual catalog v1
  Ball	shown	scene/ball
  LeftPaddle	shown	scene/left-paddle
  RightPaddle	shown	scene/right-paddle
  Score	shown	scene/score
  Playfield	shown	scene/playfield
  ```

- **The gate** is `tests/Product.Tests/CoverageGateTests.fs`. Its subject set comes from the game
  profile's typed, production-owned gameplay-visual inventory source, never from the catalog's own
  rows. It calls
  `Catalog.audit` with that inventory, the element-bound registry, observed bindings, and computed
  inventory/catalog/runtime-render digests from the projection consumed by `View.view`, and reds on
  `Missing`, `Stale`, `Unbound`, `Unobserved`, or
  `UnsupportedHidden`. Adding a gameplay element is finished only when production declares it, the
  catalog disposes it, and representative production rendering exercises its binding.

The v1 text format stays compatible. `Catalog.validate` remains a catalog self-consistency check, but it
cannot establish product completeness because deleting a row deletes its own subject. Use
`Catalog.audit productionIds catalog registeredBindings observedBindings evidenceDigests` for the ship gate.
For compatibility, `Catalog.declaredElements` still exposes row order, `Catalog.coverage` still checks
an explicitly supplied set without binding evidence, and `Catalog.toRepresentation` still bridges one
persisted disposition to the lower-level `Coverage` API.

Before visual evidence is finalized, run a fresh-context visual-coverage critic over gameplay
types/inventory, catalog, production projection, representative states, and candidate frames at the
exact commit proposed for landing. Persist the verdict outside the authored tree as an independently
attributable PR review or equivalent immutable review-system receipt. An in-repo JSON file,
author-entered reviewer name, or same-context fallback does not prove independence. A missing, unbound,
unsupported-hidden, or unresolved ambiguous status blocks, and `Catalog.audit` must independently be
`Complete`; neither line of defense can manufacture the other.

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

**A runnable version ships with this product**: [`reference.fsx`](reference.fsx), beside this file. Run it
with `dotnet fsi` — it drives the *whole* loop end to end (roster → ChannelMap → LINT → TWEAK → re-lint →
`galleryIn` across all three grammars → `Render.toPng`), and is the fastest way to see a `Warning` raised
and then tuned away. Pin its `#r "nuget: FS.GG.UI.*"` lines to your `FsGgUiVersion` from
`Directory.Packages.props` for version coherence; unpinned resolves the latest published set.

### The golden reference (upstream)

The approved, lint-clean roster is **not** vendored into your product — read it upstream in
[`FS-GG/FS.GG.Rendering/samples/SymbologyBoard/`](https://github.com/FS-GG/FS.GG.Rendering/tree/main/samples/SymbologyBoard):
`Roster.fs` is the approved mapping (its test asserts a `Clean` verdict), and `GrammarCompare.fs` is the
executable form of the "one mapping, three drawings" claim — one `Token` set drawn as three stacked bands.

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
- **Outside the capacity table.** `score` ignores the label, so its verdict is unchanged by labels.
  Never use a label to dodge a channel-overload warning — fix the encoding.
- **But it has a per-grammar LINE budget**: 3 lines under `Grammar.Token`, 2 under `Badge` and `Ring`.
  Past that the surplus lines are dropped and the last drawn line gains an ellipsis. `scoreIn grammar`
  raises a `Label` `Warning` naming the units whose lines will vanish.
- **Tofu-free is a render-edge property.** Assert it through `Symbology.Render`, never from a pure unit
  test — see [Troubleshooting](#troubleshooting).
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
overlays (Pulse/Blink/Damage); directional rhythms (Spin/Moving) are **dropped** there — the symbol comes
out byte-identical to the `Idle` one, so `scoreAnimatedIn` errors on them. Because the mapping is identical
across grammars, `Legibility.score`'s verdict is **grammar-independent**; run `scoreIn grammar` to also
catch what the selected grammar cannot draw. `Grammar.Token` reproduces the existing functions byte-for-byte.

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

CRITIQUE with two complementary checks: (a) LINT — run the linter on the produced symbol set, passing the
grammar you picked (`Legibility.scoreIn grammar (roster |> List.map mapUnit)`; animated boards use
`scoreAnimatedIn grammar` over the `(motion, token)` pairs), and read `report.Verdict` /
`report.Findings`. `scoreIn` is the grammar-aware backstop: it is `score` plus the findings that depend on
the drawing, so it catches a mapping that is legal in the abstract and illegible as rendered. The linter is
pure/deterministic
and the mechanical backstop: a `Warning`/`Error` names the overloaded or out-of-domain `Channel`,
used-vs-capacity, and the contributing unit indices. A non-`Clean` verdict is a TWEAK trigger — the unit
of change stays the mapping, never the grammar. (b) EYE — the human-style self-check of the PNG vs the
rules above stays (the linter cannot see crowding, contrast, or label collisions). The approved roster
(the golden reference above) lints `Clean`, so a fresh finding is a real signal to re-tune the mapping.

## When the grammar can't encode it

The grammar is fixed and you may not invent geometry — so when your game holds a state that **no channel
expresses**, the doctrine as stated leaves you no legal move. It has exactly one sanctioned exit: **ask
for the channel.**

Do **not** invent geometry. Do **not** overload a channel that already means something else: the linter
counts a channel's **distinct levels**, and cannot see that two of them now mean different *kinds* of
thing — a second meaning that fits inside the capacity lints `Clean` under `scoreIn` and still fails the
eye. Instead open a `cross-repo:request` issue against the **`fs-gg-symbology`** contract on
`FS-GG/FS.GG.Rendering` — the `cross-repo-coordination` skill files it — naming the state you cannot
encode and what you would draw for it. Three outcomes are possible, and which one is **Rendering's call,
not yours**:

1. **A caller-drawn `Sigil.Mark of PathSpec`** — cheap, and usually wrong. The grammar does not rotate or
   animate a caller path, so `Legibility` scores `Sigil` as the identity channel it is and never the
   rotation you routed through it. `Sigil` is also the *identity* slot, so spending it costs you your
   identity mark.
2. **An additive opt-in channel**, `None` by default and rendering byte-identically when unset — as
   `Label` / `AutoLabel` / `LabelMotion` / `SecondaryHeading` each did. Growing the fixed set is the
   **library's** act, never your mapping's.
3. **Declared out of scope** — the vocabulary draws unit *symbols*, not vehicle *schematics*.

Filing the request *is* the move the doctrine asks of you. Do not work around it silently: a workaround
is a channel the linter cannot see.

**Worked example.** FS.GG.Game mapped a tank and found `Token` carried a single `Heading` — but a tank
rotates twice, the **hull** (which armor plate faces you) and the **turret** (where the gun points).
Neither is inspection detail. They filed
<!-- skill-refs: closed-ok FS.GG.Rendering#260 — cited as the issue that WAS filed and answered (ADR-0102 below is the answer), not as somewhere to go. Closed is correct; it stays closed. -->
[FS.GG.Rendering#260](https://github.com/FS-GG/FS.GG.Rendering/issues/260) instead of working around it,
naming the cost of each option and leaving the choice to Rendering. The answer was **(2)** —
`Token.SecondaryHeading : float option`, per
[ADR-0102](https://github.com/FS-GG/FS.GG.Rendering/blob/main/docs/product/decisions/0102-symbology-secondary-heading-channel.md)
— which is why the [Two rotations](#two-rotations-opt-in-second-heading) channel exists for you to map
today. A proposed *"the two headings nearly agree"* `Warning` was **rejected**: a tank driving forward
with its gun forward is the normal rest state, so it would have fired on correct input. The hazard was
answered by **drawing** (each grammar sites the barrel clear of its primary indicator), not by linting.

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

## Troubleshooting

The recurring failure modes, collected. Three of the four are the contract working as designed.

- **Tofu boxes (`□□□`) where a label should be** — you asserted glyph content from a **pure unit test**.
  Tofu-free is a **render-edge** property: the pure library emits deterministic glyph-run *proof* nodes
  and never requires a measurer. Real glyphs come from the render bridge, so verify through
  `Symbology.Render` — sampling phases when the label is motion-bound. Not a bug in the pure layer.
- **`Render.toPng` raised** — that is the **fail-loud contract**, not a bug. Any non-passing verdict
  raises with the joined diagnostics; it never returns a blank success, which is why a critique never
  reasons over an empty PNG. Read the diagnostics.
- **A blank or placeholder symbol** — `R <= 0`. You get a **fixed 12px grey box with an X**, at any radius.
  The guard runs *before* label resolution, so it swallows the body, the `Sigil`, the label and the
  auto-label alike. It is **not** a motion guard, though — only `Pulse` suppresses itself on a degenerate
  token. `Blink` still draws its red dot on top (that dot has a 2px floor), `Moving` draws the echo as a
  *second* offset placeholder, and `Spin` / `Damage` emit overlay nodes that are simply degenerate at
  `R = 0`. So a placeholder box with a stray dot beside it is a bad `R`, not a motion bug. Fix `mapUnit`.
- **`token` won't drop into a `SceneNode` layer** — `token : Token -> Scene` returns a `Scene`
  (`{ Nodes: SceneNode list }`), **not** a `SceneNode`, while a view is `Model -> SceneNode` and
  `SceneNode.Group` takes a `Scene list`. So `Sym.token tok` will not typecheck straight into a
  `SceneNode list`. Compose it one of two ways: wrap it — `Group [ Sym.token tok ]` — or splice its
  nodes — `yield! (Sym.token tok).Nodes`. Same for `animate` / `render` / the grammar-selecting
  renderers; every one returns a `Scene`.
- **`NU1403` on restore** — a poisoned NuGet cache. Restore against a **scratch `NUGET_PACKAGES`
  directory**; do **not** clear the shared cache.

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
