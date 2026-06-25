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
  fixed channel set), the channel enums `Faction` / `Klass` / `Sigil` / `TokenState` / `Motion`, and
  `module Symbology` with `defaultToken`, `token : Token -> Scene`, `animate : Motion -> Token ->
  phase:float -> Scene`, `gallery : cols -> spacing -> Token list -> Scene`, and `filmstrip : samples
  -> (Motion * Token) list -> Scene`. References **only** `FS.GG.UI.Scene` — no IO, no GL, no codec call.
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

The `Token` carries an **optional** `Label : string option` — a short identity string (name / callsign /
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
- **Multi-line is the SAME field — opt-in, still inspection-detail.** The one `Label : string option`
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
