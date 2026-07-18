---
name: fs-gg-symbol-design
description: Design a game's visual language iteratively, in situ. You hand the agent the game structure (world / unit / stats types), the full roster with stats, and a captured gamestate frame (unit positions + terrain); it drafts SEVERAL competing visual directions, renders each as a FAITHFUL frame — real symbols at their real positions over the real board via `Symbology.Render` — screens them with the `Legibility` linter and the eye, presents a contact sheet, and converges with you to one approved symbol language. The divergent, whole-frame exploration loop on top of the single-mapping mechanics of [[fs-gg-symbology]].
metadata:
  author: FS.GG
  source: FS.GG.Rendering symbol-design loop (builds on specs/192-agent-unit-symbology)
---

# Symbol-Design Loop

## Scope

Use this skill to **design a game's visual language** — not to hand-write one mapping, but to explore
**several competing directions at once**, render each as a **faithful gamestate frame** (every unit's
real symbol at its real board position, over the real terrain), and **converge with a human** to the
one that reads best. It is the divergent, whole-frame exploration loop that sits **on top of**
[[fs-gg-symbology]].

What you author with this skill is **product / loop code and design artifacts** — candidate
`'stats -> Token` ChannelMaps, a frame-placement projection, rendered contact sheets, a decision
record, and a pinned golden frame. It edits **no library**. The fixed channel grammar, the `Token`
record, and the `Legibility` linter all belong to [[fs-gg-symbology]] and are consumed unchanged.

**This skill does not restate the grammar.** The channel table, the capacity rules, the three
grammars, the label channel, the "when the grammar can't encode it → ask for the channel" doctrine,
and the single-mapping RENDER → LINT → TWEAK mechanics all live in [[fs-gg-symbology]]. Read that
first; this skill assumes it and only adds the two things it does not cover: **many candidates** and
**the whole frame**.

## The seam — what this adds over `fs-gg-symbology`

| | [[fs-gg-symbology]] | **fs-gg-symbol-design** (this skill) |
|---|---|---|
| Unit of work | ONE `'stats -> Token` mapping | SEVERAL competing **candidates** at once |
| What you render | `Symbology.gallery` — symbols on a neutral grid, in isolation | the **actual frame** — real positions, real terrain, real clutter |
| Question answered | "is this symbol legible on its own?" | "which visual language wins **in play**, and does it survive a crowded frame?" |
| Output | a lint-clean mapping + a golden board | a **decision** across directions + a golden **frame** + a rationale of what lost and why |

The gallery answers legibility-in-isolation; it cannot show you occlusion, faction-vs-terrain
contrast, two allies overlapping at a chokepoint, or a Ring gauge that reads beautifully alone and
vanishes against a busy tile set. Those are **frame** properties, and they are where visual-language
decisions are actually won or lost. This skill renders the frame so you decide on what the player sees.

## What you feed it (INTAKE)

Three inputs, all supplied by the consuming product — none invented here:

1. **The game structure** — the product's `'world`, its per-unit record, and the `'stats` the mapping
   will read. `FS.GG.Game.Core` is generic over `'world` (`Loop.StepState<'world>`); there is **no
   fixed `Unit` or `GameState` type** in the framework. The unit record and its stat fields are the
   product's, and the ChannelMap is written against **them**.
2. **The full roster + stats** — every unit type and the stat ranges they span, so a candidate is
   judged against the whole cast, not a lucky subset. Min/max of each ordered stat matters: the
   mapping's quantisation (`Speed`, `Threat`, `Charge`, `R`) is chosen against the real spread.
3. **A captured gamestate frame** — one `StepState.Current` (or any snapshot carrying each unit's
   **position** plus the **terrain** it stands on). This is what makes the render *faithful* rather
   than a gallery. Capture it from the product's own loop:

   ```fsharp
   // In the product, at the frame you want to design against:
   let frame : StepState<World> = // ...advance the loop to the moment of interest
   let snapshot = frame.Current   // units with positions + terrain, as a plain value — no clock, replayable
   // Serialize `snapshot` (its own codec, or a hand-built literal) and hand it to the loop.
   ```

   Pick a frame that is **hard on purpose**: peak unit count, a contested chokepoint, mixed factions
   overlapping, the alert state you most need to read fast. A visual language that survives the worst
   frame survives the rest.

## What a "candidate" is

A candidate is a **complete visual direction**, not a tweak. It is the triple:

- **grammar** — `Grammar.Token` (heading-rotated silhouette), `Grammar.Badge` (upright framed emblem),
  or `Grammar.Ring` (radial gauge). One `'stats -> Token` map feeds any of them unchanged; the choice
  changes the *drawing*. (Full trade-offs: [[fs-gg-symbology]] §"Selectable grammars".)
- **the editorial ChannelMap** — *which stat rides which salient channel*. This is the real design
  decision: does threat ride stroke-width or size? does health go on the belly arc (quiet) or get
  redundant reinforcement on hue when critical? Assign-by-urgency is the rule; the *assignment* is the
  candidate.
- **the board treatment** — terrain contrast, on-board symbol size (`R`), label on/off, and the one
  live `Motion` rhythm (if any).

**Vary ONE axis across a candidate set, hold the frame fixed.** If candidate A changes the grammar
*and* the urgency assignment *and* the size, a human who prefers it cannot tell you *why*, and the next
round has nothing to steer on. A good set is "same frame, same roster, three grammars" — or "same
frame, Token grammar, three urgency assignments." Diversity that is attributable is diversity you can
converge on. (More on picking the axis and the count: [`reference/candidates.md`](reference/candidates.md).)

## Faithful-frame rendering — the core technique (do NOT use `gallery`)

`Symbology.gallery`/`galleryIn` lay symbols out on a neutral grid and **ignore `Token.Cx/Cy`** — that
is the isolation board [[fs-gg-symbology]] critiques against. This skill needs the opposite: each
symbol **at its own board position**, over terrain. Three moves, in **pure `Scene`** — a position is
just two floats, so no extra package is needed and the skill never out-reaches Symbology's (R-REACH, FS.GG.Rendering#430):
<!-- skill-refs: closed-ok FS.GG.Rendering#430 — cited as the reach gate that motivates keeping this loop in pure Scene; history, stays closed. -->

1. **Place** — the ChannelMap sets `Cx`/`Cy` from the unit's frame position (not a grid slot).
   `Symbology.render grammar token` draws the symbol centred at `token.Cx/Cy`, so placement is just
   filling those two fields. For a tile game, the cell centre is arithmetic:

   ```fsharp
   let center (col, row) = float col * cellSize + cellSize / 2.0, float row * cellSize + cellSize / 2.0
   let cx, cy = center unit.Cell
   Symbology.render grammar { mapUnit unit.Stats with Cx = cx; Cy = cy }
   ```

   For continuous sim coordinates, use the world `X`/`Y` directly (apply your own world→screen
   scale/origin if the frame is larger than the canvas).
2. **Compose** — draw the terrain first, then the symbols on top, and `Scene.group` them:

   ```fsharp
   let tile (col, row) = Scene.filledRectangle { X = float col*cellSize; Y = float row*cellSize; Width = cellSize; Height = cellSize } floorColor
   let terrain = Scene.group [ for c in walkableCells -> tile c ]
   let units   = frameRoster |> List.map (fun u -> Symbology.render grammar (placeUnit u))
   let frameScene = Scene.group (terrain :: units)
   ```
3. **Rasterise faithfully** — `Render.toPng size frameScene dir`. It wraps the real `SkiaViewer`
   reference path and **fails loud** (raises with joined diagnostics) on any verdict that is not
   `ReferencePassed` with a real image — so a contact sheet is **never** reasoned over an empty PNG.

Determinism holds end to end: the library and the render bridge read no clock, so an unchanged
candidate over an unchanged frame re-renders **byte-identically**. Timestamps and filenames are the
**loop's** to stamp, never library code's.

> **If your product already depends on `FS.GG.Game.Render`**, its `Adapter.cellCentre` /
> `Adapter.drawCells` do exactly this projection from sim `Cell`s (and `Adapter.point` from continuous
> `FS.GG.Game.Core.Point`s) — reach for them instead of hand-rolling the arithmetic. This skill keeps
> to pure `Scene` only so it never obliges a profile to pin the render adapter it does not otherwise need.

## The loop (the unit of change is the candidate set → the mapping, never the grammar library)

```
1. INTAKE    read game structure + full roster/stats; capture the hard frame (positions + terrain).
             Fix ONE axis of variation for this round (grammar, OR urgency assignment, OR treatment).

2. DIVERGE   draft N candidates (2–4) that differ ONLY on that axis. Each is a full triple:
             grammar + editorial ChannelMap + board treatment. Build every ChannelMap from
             `Symbology.defaultToken`, overriding only the fields the game encodes.

3. RENDER    for EACH candidate, build the FAITHFUL frame (place → compose → Render.toPng) — NOT a
             gallery. Read every PNG back. One image per candidate, same frame, same size.

4. SCREEN    two complementary checks, per candidate, at the target on-board size:
             (a) LINT  `Legibility.scoreIn grammar (roster |> List.map mapUnit)` (animated frames:
                       `scoreAnimatedIn grammar`). A non-`Clean` verdict eliminates or re-tunes a
                       candidate before a human ever sees it — cost you did not spend their attention on.
             (b) EYE   look at the FRAME (the linter cannot see occlusion, crowding, faction-vs-terrain
                       contrast, or two symbols colliding at a chokepoint — the frame-eye check is the
                       whole reason this skill renders the frame and not the gallery).

5. PRESENT   assemble the surviving candidates into ONE contact sheet (label each with its grammar +
             the one-line editorial choice + its lint verdict) and show the human. Ask for a DIRECTION,
             not pixels: "which reads fastest under pressure?", not "nudge this blue."

6. CONVERGE  capture the pick + the reason. Narrow to 1–2. If the human splits, the reason names the
             next axis to vary — run steps 2–5 again on THAT axis. Diverge-then-narrow, don't polish
             a loser.

7. ITERATE   on the chosen direction, hand off to the single-mapping RENDER → LINT → TWEAK loop of
             [[fs-gg-symbology]]: adjust the ChannelMap / Token params ONLY until the linter is `Clean`
             and the frame-eye check passes.

8. APPROVE   emit the final symbol-set module (the winning ChannelMap + the placement projection), a
             design rationale (channel assignments + the candidates that LOST and why), and a pinned
             golden FRAME with a stable `SceneCodec` identity.
```

Steps 2→6 are this skill's contribution; step 7 is where it dissolves into [[fs-gg-symbology]]. Do not
re-implement that inner loop here — call it.

## Convergence, not endless generation

The failure mode of a multi-candidate loop is **generating forever**. Guards:

- **Cap the round at 2–4 candidates.** More than four faithful frames is not a richer choice, it is a
  contact sheet no human compares fairly.
- **Each round must eliminate.** If a round ends with the same number of live directions it began with,
  the axis you varied was not decision-relevant — pick a different axis, do not add candidates.
- **A `Warning`/`Error` from `scoreIn` is a real signal**, measured against the approved lint-clean
  reference in [[fs-gg-symbology]]. Screen it out in step 4, before PRESENT — never spend a human's
  comparison budget on a candidate the linter already rejected.
- **Stop at APPROVE.** Once a direction is picked and lints `Clean` on the hard frame, the design is
  done; further candidates are the loop failing to converge, not improving.

## When a candidate needs a channel the grammar doesn't have

Sometimes the winning *direction* wants to encode a state no channel expresses (a tank's hull-vs-turret
rotation, a second allegiance). That is **not** a licence to invent geometry or overload a channel — it
is the exact situation [[fs-gg-symbology]] §"When the grammar can't encode it" governs: **file a
`cross-repo:request` against the `fs-gg-symbology` contract** naming the state and what you'd draw, via
the `cross-repo-coordination` skill, and let Rendering decide (caller `Sigil.Mark`, an additive opt-in
channel, or out-of-scope). Design *around* the fixed grammar; when you genuinely cannot, ask — do not
work around it inside a candidate.

## Provenance the loop MUST write

- **Every round** → under the working dir: the per-candidate **frame PNGs**, the **contact sheet**, and
  a snapshot of **each candidate's ChannelMap** — the auditable record of what was compared.
- **Every convergence** → the **direction chosen and the reason**, and the candidates eliminated with
  why. This is the design rationale; it is the durable artifact, more than any single image.
- **On APPROVE** → the final symbol-set module (winning ChannelMap + placement projection), the
  rationale, and a **pinned golden frame** with a stable `SceneCodec` identity, so a re-render is a
  byte comparison and a future regression is visible.
- Filenames/timestamps are stamped by the **loop**, never by library code (determinism — see above).

## FSI recipe (multi-candidate faithful-frame render)

```fsharp
#r "nuget: FS.GG.UI.Symbology"
#r "nuget: FS.GG.UI.Symbology.Render"
open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

// The product's own frame: each unit's stats + its board cell. (Shape is the game's, not the framework's.)
type Stats = { Side: string; Role: string; Dps: float; Hp: float; HpMax: float; Speed: float; Facing: float }
type Placed = { Stats: Stats; Cell: int * int }
let cellSize = 48.0

// --- ONE editorial ChannelMap = ONE candidate's stat→channel assignment (data you edit each round) ---
let mapUnitA (s: Stats) : Token =
    { Symbology.defaultToken with
        R       = 22.0
        Faction = (match s.Side with "blue" -> Ally | "red" -> Enemy | _ -> Neutral)
        Klass   = (match s.Role with "tank" -> Heavy | "scout" -> Scout | _ -> Mobile)
        Threat  = min 1.0 (s.Dps / 120.0)     // candidate A: threat rides stroke WIDTH
        Health  = s.Hp / s.HpMax
        Speed   = int (min 4.0 (s.Speed / 4.0))
        Heading = s.Facing }

// candidate B differs on ONE axis only — here, threat rides SIZE instead of width:
let mapUnitB (s: Stats) : Token =
    { mapUnitA s with R = 16.0 + 14.0 * (min 1.0 (s.Dps / 120.0)); Threat = 0.0 }

// Place a mapped token at its real board position (this is what `gallery` refuses to do):
let place (grammarMap: Stats -> Token) (u: Placed) : Scene =
    let col, row = u.Cell
    let cx, cy = float col * cellSize + cellSize / 2.0, float row * cellSize + cellSize / 2.0
    Symbology.render Grammar.Token { grammarMap u.Stats with Cx = cx; Cy = cy }

// Build ONE faithful frame for a candidate: terrain first, symbols on top.
let tile (col, row) =
    Scene.filledRectangle { X = float col * cellSize; Y = float row * cellSize; Width = cellSize; Height = cellSize }
                          (Color.rgb 30uy 32uy 38uy)
let frameScene (grammarMap: Stats -> Token) (terrain: Scene) (roster: Placed list) : Scene =
    Scene.group (terrain :: (roster |> List.map (place grammarMap)))

let terrain = Scene.group [ for c in walkableCells -> tile c ]
let size    = { Width = 960; Height = 640 }

// Render each candidate as a faithful frame + lint it (screen BEFORE showing a human):
for (name, m) in [ "A-width", mapUnitA; "B-size", mapUnitB ] do
    let png    = Render.toPng size (frameScene m terrain frameRoster) (sprintf "./work/round-01/%s" name)
    let report = Legibility.scoreIn Grammar.Token (frameRoster |> List.map (fun u -> m u.Stats))
    printfn "%s -> %s  (%A)" name png report.Verdict
// -> assemble the PNGs into ONE contact sheet, PRESENT, capture the DIRECTION, narrow, iterate.
```

See [`reference.fsx`](reference.fsx) for a runnable in-tree version, and
[`reference/candidates.md`](reference/candidates.md) for choosing the axis and the candidate count.

## Related

- [[fs-gg-symbology]] — the fixed channel grammar, the `Token` record, the `Legibility` linter, and the
  single-mapping RENDER → LINT → TWEAK loop this skill orchestrates over. **Read it first.**
- [[fs-gg-scene]] — the pure `Scene` / `Color` / `Point` primitives and `Scene.group` used to compose a frame.
- [[fs-gg-skiaviewer]] — the `ReferenceRendering` path `Render.toPng` wraps.
- [[fs-gg-game-core]] — the generic `'world` / `StepState` a captured frame is a snapshot of.
- [[fs-gg-collision]] / [[fs-gg-grids]] / [[fs-gg-visibility]] — the sim frame the visual language dresses.
- The `cross-repo-coordination` process skill — files the `fs-gg-symbology` channel request when no candidate can encode a state (see *"When a candidate needs a channel…"*).

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven rendering library): https://github.com/mono/SkiaSharp
