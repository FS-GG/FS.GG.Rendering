# Agent-Driven Unit-Symbology Design System — Analysis & Plan

**Date:** 2026-06-25
**Status:** Proposal (design only — no code committed beyond this document)
**Scope:** A reusable F# **symbology library** plus an **agent workflow** (local skills + FSI
scripting + headless render) that turns a game's *unit roster + stats* into a legible **visual
control set** — abstract vector symbols, not depictions — refined through a render→eyeball→tweak
feedback loop until the user is satisfied.
**Builds on:** the embedded `canvas` control and pure element/loop library
([spec 191](../../specs/191-embedded-canvas-control/plan.md);
[`docs/reports/2026-06-24-11-59-embedded-canvas-control-roadmap.md`](2026-06-24-11-59-embedded-canvas-control-roadmap.md)).
**Proof-of-concept:** a Directional-Token element + motion filmstrip were prototyped end-to-end
against the live Skia raster path during the 2026-06-25 session (see §3 and Appendix A).

---

## 1. Problem & vision

A game needs an at-a-glance visual vocabulary for its units: each unit rendered as a compact
**symbol** that encodes as much state as the eye can read — affiliation, class, threat, health,
heading, status, activity — through a *fixed grammar* of vector channels (shape, colour, stroke,
fill, motion). Designing that vocabulary by hand is slow and inconsistent; the channel-assignment
decisions are exactly the kind of bounded, iterable, evidence-producing work an **agent** does well
*if* it has (a) a grammar to reason in, (b) a library to emit symbols as data, and (c) a headless way
to *see* the result and iterate.

This document specifies that system:

> **Input:** a roster of units with stats (e.g. `name, faction, role, hp, dps, speed, armor, …`).
> **Process:** an agent loads a local **symbology skill** + the **`FS.GG.UI.Symbology` library**,
> writes an FSI script that maps each unit's stats onto encoding channels, renders a gallery PNG
> headlessly, shows it to the user, and **loops** — tweaking the mapping on feedback — until approved.
> **Output:** a committed F# module emitting the unit control set as pure `Scene`s, plus a design
> rationale (channel assignments + rejected alternatives + legibility notes).

The system is deliberately **declarative and deterministic**: a symbol is a pure `'props -> Scene`,
so identical stats yield byte-identical scenes, the gallery is reproducible, and the final set can be
pinned with golden tests like any other contracted surface.

---

## 2. Current foundations (what already exists)

| Capability | Anchor | Use in this system |
|---|---|---|
| Immutable `Scene` + full vector primitive set (Path/Bézier/Arc, gradients, dash, stroke, text) | `src/Scene/Types.fsi` (`SceneNode` `:450`, `Paint` `:89`, `PathCommand` `:102`, `Shader` `:60`, `PathEffect` `:82`) | The drawing substrate every symbol compiles to |
| Pure element model `Element<'props> = 'props -> Scene` + combinators (`at`/`layer`/`cached`) + content fingerprint | `src/Canvas/Elements.fs[i]` | The library extends this idiom; `cached` keys symbols by content |
| Deterministic fixed-timestep loop (`Loop.advance`, `alpha`) | `src/Canvas/Loop.fsi` | Drives the **motion channel** (phase → animated symbol) |
| Embedded `canvas` control (`scene`, `volatile'`, `onPointer/onKey`, `viewport`) | `src/Controls/Canvas.fsi` | Hosts a live, per-frame symbol board with cache-isolated chrome |
| Headless **Scene → PNG** raster (CPU `SKSurface`, no GL) | `ReferenceRendering.run : ReferenceRenderingRequest -> ReferenceRenderingEvidence`, `src/SkiaViewer/ReferenceRendering.fs` | The **eyeball** step — emits a content-hash-named PNG + evidence summary |
| `SceneCodec` export/import (canonical bytes, package identity) | used by `ReferenceRendering` + `samples/CanvasDemo/Game.fs` | Round-trips a scene for rendering and gives a free regression identity |
| Local skills for scene/canvas/diagnostics/Ant tokens | `fs-gg-scene`, `fs-gg-product-scene`, `fs-gg-ui-widgets`, `fs-gg-diagnostics`, `fs-gg-ant-design`, `fs-gg-samples` | The agent's existing competencies; this system adds one skill that orchestrates them |

**Two constraints discovered during the PoC and carried into the design:**

1. **`SceneRenderer` is `internal`** (`src/SkiaViewer/SceneRenderer.fs:17`). Scripts cannot call
   `paintNode` directly. The **public** headless path is `ReferenceRendering.run`, which round-trips
   the scene through `SceneCodec`. This drives decision **D2**.
2. **The codec preserves every paint channel.** The PoC verified that export→import→raster keeps
   `Path` geometry, `RadialGradient`/`LinearGradient`/`SweepGradient` shaders, `Dash` path effects,
   `Arc`, and stroke width/cap/join intact — so the full encoding vocabulary is safe to serialize,
   fingerprint, and ship as scene data.

---

## 3. Proof of concept (2026-06-25 session)

Before specifying the system, the **Directional Token** (one of three grammars sketched — Badge,
Token, Ring) was built end-to-end and rendered through the real raster path:

- A pure `token : Token -> Scene` composed from `Path` silhouette + `RadialGradient` energy fill +
  stroke (colour=team, width=threat, `Dash`=suspected) + centre vector **sigil** + trailing speed
  **beads** + green→red health **`Arc`** + corner **shield** pip; the whole body (nose + sigil + tail)
  rotates rigidly by **heading**.
- A **motion vocabulary** as pure `(Token, phase01) -> Scene` builders: pulse-ring (fired), spin
  (channeling), blink (alert), damage-throb (scale+hue punch), move (translate + echo trail).
- Both rendered headlessly to gallery + **filmstrip** PNGs via `ReferenceRendering.run`; frames are
  reproducible because every builder is pure (no wall-clock read).

This validated (a) the channel grammar is legible, (b) the codec preserves all channels, (c) the
headless render loop is fast and scriptable, (d) motion maps cleanly onto `volatile'` + `Loop`.
Condensed PoC source is in **Appendix A** as the seed of `FS.GG.UI.Symbology`.

---

## 4. The encoding-channel grammar

The grammar is the **fixed** part of the system (the library); per-game **mapping** (§5) is the
editable part. Channels are assigned by *salience* and a critical rule: **encode urgent state
redundantly across multiple pre-attentive channels; put detail on inspection-only channels.**

| Channel | Encodes (default) | Primitive | Reliable levels | Salience |
|---|---|---|---|---|
| Stroke hue | faction / affiliation | `Paint.Stroke` colour | ~7 categorical | ★★★ pop-out |
| Motion rhythm | activity / alert | `Loop` phase → overlay | ~4 rhythms | ★★★ pop-out |
| Size | magnitude / scale | symbol radius | ~4 ordered | ★★★ |
| Body silhouette + sigil | class + identity | `Path` (poly + stroked mark) | ~6 + many | ★★ |
| Whole-body rotation | heading / facing | point transform | continuous | ★★ |
| Stroke width | threat / rank | `Stroke.Width` | ~4 ordered | ★★ |
| Interior gradient | charge / energy | `Shader.RadialGradient` | ~4 ordered | ★★ |
| Belly arc (len + hue) | health / resource | `Arc` + colour lerp | continuous | ★ |
| Tail beads (count) | speed / queue depth | `Circle` run | ~4 | ★ |
| Stroke dash | confirmed vs suspected | `PathEffect.Dash` | ~3 | ☆ inspection |
| Corner mounts | boolean statuses (shield…) | small `Path`/`Circle` | ~3 per slot | ☆ inspection |

**Legibility budget.** There are ~11 channels but the eye cannot track all at once; the skill
encodes guardrails: one *active* motion at a time; never put a critical state on dash alone; keep
faction (hue) and state (e.g. Ant status semantics) on *different* channels so they never collide.
A future **legibility linter** (§9) can warn when a mapping overloads channels.

**Colour discipline.** State semantics reuse the repo's Ant status tokens (processing/warning/
error/success/default) via `fs-gg-ant-design`; faction hue uses a separate saturated palette so
*state* and *team* never share the hue channel.

---

## 5. System architecture

Five components, layered so the **fixed grammar**, the **per-game mapping**, and the **render bridge**
are independent and independently testable.

```
 unit roster (stats)                                     local skills
        │                                                ┌───────────────────────────┐
        ▼                                                │ fs-gg-symbology (NEW)      │
 ┌─────────────────┐   maps stats→channels   ┌──────────┤  + fs-gg-scene             │
 │ ChannelMap      │◄────── agent edits ──────│  AGENT   │  + fs-gg-ant-design        │
 │ 'stats -> Token │                          │  (FSI)   │  + fs-gg-diagnostics       │
 └────────┬────────┘                          └────┬─────┘  + fs-gg-samples           │
          │ token : Token -> Scene                 │        └───────────────────────────┘
          ▼                                         │ writes .fsx
 ┌─────────────────────────┐                        ▼
 │ FS.GG.UI.Symbology (lib) │   Scene   ┌────────────────────────┐  PNG   ┌──────────┐
 │  Token, token, motion,   │──────────▶│ render bridge          │───────▶│  user    │
 │  gallery/filmstrip       │           │ (ReferenceRendering.run)│        │ eyeballs │
 └─────────────────────────┘           └────────────────────────┘        └────┬─────┘
          ▲                                                                     │ feedback
          └─────────────────────── tweak ChannelMap / params ◀──────────────────┘
```

### 5.1 `FS.GG.UI.Symbology` — the library (pure, Scene-only)

A new dependency-light first-party project (mirrors `FS.GG.UI.Canvas`'s rationale: keep game-symbol
vocabulary off the core control surface; independently testable and packable). Depends only on
`FS.GG.UI.Scene`.

- **`Token`** — the props record: the full channel set as typed fields (§4).
- **`token : Token -> Scene`** — the Directional-Token element (Appendix A). Pure, deterministic.
- **Channel constructors / enums** — `Faction`, `Klass`, `Sigil`, `TokenState`, etc., with the
  fixed silhouette + sigil tables, so a game theme picks from a known set.
- **Motion** — `Motion` DU + `animate : Motion -> Token -> phase:float -> Scene`.
- **Layout** — `gallery : Token list -> Scene` and `filmstrip : (Motion * Token) list -> samples:int
  -> Scene` for review boards.
- **Future grammars** — `Badge`/`Ring` (§4 alternatives) can land as sibling elements behind the
  same channel vocabulary.

> The library is **pure scene data**: no IO, no GL, no codec. Determinism = identical `Token` ⇒
> identical `Scene` (FNV-1a content fingerprint reuse from `Elements`).

### 5.2 `ChannelMap` — the per-game mapping (the editable artifact)

The designer's decisions live in a *data* function `'stats -> Token`, **not** in the library. This is
what the agent tweaks each loop iteration. Example (RTS):

```fsharp
let mapUnit (u: UnitStats) : Token =
    { defaultToken with
        Faction  = faction u.Side                       // hue
        Klass    = match u.Role with
                   | "tank" -> Heavy | "scout" -> Scout | _ -> Mobile
        Sigil    = sigilFor u.Role                       // identity mark
        Threat   = norm u.Dps 0.0 120.0                  // stroke width  (0..1)
        Health   = u.Hp / u.HpMax                        // belly arc
        Speed    = beadsFor u.Speed                       // 0..4 tail beads
        Shield   = u.Armor > armorThreshold              // corner mount
        Heading  = u.Facing }
```

Keeping grammar (fixed) and mapping (per-game) separate means the agent reasons about *encoding
choices* in one small file, and the legibility rules apply uniformly.

### 5.3 The render bridge — headless Scene → PNG (D2)

The loop needs the agent to **see** a scene. Because `SceneRenderer` is internal (§2), v1 backs onto
the public `ReferenceRendering.run`:

```fsharp
let renderPng (size: Size) (scene: Scene) (dir: string) : string =
    let ev = ReferenceRendering.run
                 { PackageBytes = (SceneCodec.export scene).CanonicalBytes
                   OutputDirectory = dir; OutputSize = size; Resources = [] }
    ev.ImagePath |> Option.defaultWith (fun () -> failwith (String.concat "; " ev.Diagnostics))
```

This is a thin **`FS.GG.UI.Symbology.Render`** helper (references `SkiaViewer`), kept *out* of the
pure library so `FS.GG.UI.Symbology` stays Scene-only. Bonus: `ReferenceRendering` writes a
content-hash-named PNG + `reference-evidence.md`, giving each iteration a free **identity** for
regression. (v2: promote a dedicated public raster entry that skips the codec round-trip if its cost
ever matters — see D2.)

### 5.4 `fs-gg-symbology` — the orchestrating skill (NEW)

A local skill that teaches any agent to run this loop consistently. It encodes:

- the **grammar** (§4 channel table) and the **legibility rules** (assign-by-urgency, redundancy,
  one-active-motion, hue-collision avoidance);
- the **library API** and the `ChannelMap` pattern (grammar vs mapping separation);
- the **FSI scripting recipe**: `#r` the library + render bridge, define roster, define `ChannelMap`,
  `renderPng (gallery …)`, **read back the PNG**, iterate;
- the **feedback protocol**: each iteration writes a timestamped gallery + a mapping snapshot under a
  working dir, so decisions are auditable;
- **composition** with `fs-gg-scene` (primitives), `fs-gg-ant-design` (status colour), and
  `fs-gg-diagnostics` (visual/readback evidence).

### 5.5 The agent workflow & feedback loop

```
1. INTAKE   read roster + stats; pick grammar (default: Directional Token).
2. MAP      draft ChannelMap 'stats -> Token (assign-by-urgency; redundancy on critical state).
3. RENDER   FSI: build gallery Scene; renderPng → PNG; read the image back.
4. CRITIQUE self-check vs legibility rules (faction separable? class distinct? health readable
            at target size? any channel overloaded?).
5. REVIEW   present PNG to the user; capture feedback.
6. TWEAK    adjust ChannelMap / token params (NOT library internals); goto 3.
7. APPROVE  on satisfaction: emit final F# control-set module + rationale; pin a golden gallery.
```

The loop's unit of change is the **`ChannelMap`** (and occasionally a theme palette), never the
grammar — which keeps iterations small, legible, and reversible.

---

## 6. Design decisions

| # | Decision | Rationale | Rejected alternative |
|---|---|---|---|
| **D1** | New `FS.GG.UI.Symbology` library (Scene-only), not folded into `Canvas`/`Controls` | Keeps game-symbol vocabulary off the core control surface; independently testable/packable; mirrors spec-191's `FS.GG.UI.Canvas` reasoning | Folding into `Canvas` bloats its surface with game-domain types unrelated to drawing combinators |
| **D2** | Render bridge backs onto public `ReferenceRendering.run` (codec round-trip) in a *separate* `Symbology.Render` helper | `SceneRenderer` is `internal`; codec verified to preserve all paint channels; gives free PNG identity/evidence; zero new core surface | (a) make `SceneRenderer` public — widens `SkiaViewer` surface for a niche need; (b) re-implement `paintNode` in scripts — duplication & drift |
| **D3** | Grammar (fixed library) vs mapping (`'stats -> Token` data) are separate layers | The agent iterates on *encoding choices* in one small data file; legibility rules apply uniformly; the library stays stable | Bake per-game choices into the element — every game forks the library |
| **D4** | Symbols are pure `'props -> Scene`; no wall-clock, no IO | Determinism ⇒ reproducible galleries ⇒ goldenable final set; matches `Elements`/`Loop` constitution | Stateful/imperative drawing — non-reproducible, untestable |
| **D5** | Motion is `(Token, phase) -> Scene`, phase owned by the model | Maps 1:1 onto `volatile'` canvas + `Loop.advance`; keeps the animation channel deterministic and headlessly sampleable (filmstrip) | Timer-driven imperative animation — breaks determinism & cache isolation |
| **D6** | Agent behaviour lives in a `fs-gg-symbology` **skill**, not ad-hoc prompting | Any agent run applies the same grammar + legibility rules + loop protocol; composes existing skills | Re-explaining the grammar per session — inconsistent, error-prone |
| **D7** | Each iteration writes a **timestamped gallery + mapping snapshot** | Auditable design history; supports A/B comparison and "why did we change this" | Throwaway renders — no provenance, no regression baseline |
| **D8** | State colour reuses Ant status tokens; faction uses a separate palette | Prevents hue collisions between *state* and *team*; aligns canvas with chrome | One palette for everything — ambiguous, breaks the budget |

---

## 7. Public surface sketch (Tier-1, `.fsi`-first)

```fsharp
namespace FS.GG.UI.Symbology
open FS.GG.UI.Scene

type Faction   = Ally | Enemy | Neutral | Custom of Color
type Klass     = Mobile | Heavy | Scout                 // → silhouette
type Sigil     = Bolt | Ring | Fang | Mark of PathSpec   // → centre mark
type TokenState = Confirmed | Suspected                  // → solid / dashed
type Motion    = Idle | Pulse | Spin | Blink | Damage | Moving

type Token =
    { Cx: float; Cy: float; R: float; Heading: float
      Faction: Faction; Klass: Klass; Sigil: Sigil; State: TokenState
      Threat: float; Speed: int; Health: float; Shield: bool }

[<RequireQualifiedAccess>]
module Symbology =
    val defaultToken : Token
    val token        : Token -> Scene
    val animate      : Motion -> Token -> phase: float -> Scene
    val gallery      : cols: int -> spacing: float -> Token list -> Scene
    val filmstrip    : samples: int -> (Motion * Token) list -> Scene

// thin IO helper in a SEPARATE project (references SkiaViewer); not part of the pure lib
namespace FS.GG.UI.Symbology.Render
module Render =
    val toPng : size: Size -> scene: Scene -> dir: string -> string   // returns image path
```

Per Constitution I/II this lands `.fsi`-first with semantic tests fail-before/pass-after and a new
surface baseline `FS.GG.UI.Symbology.txt`.

---

## 8. Determinism, testing & governance

- **Golden symbols.** Each `Token` archetype pins a `SceneCodec` package identity; the approved
  control set is a golden gallery (byte-identity), so visual drift is caught like surface drift.
- **Determinism tests.** `token`/`animate` are pure: property tests assert identical props ⇒ identical
  scene; `filmstrip` frames are reproducible from a phase schedule (no wall clock) — mirrors
  `Loop.advance` determinism tests.
- **Legibility evidence.** `fs-gg-diagnostics` readback/visual evidence captures non-blank,
  channel-present proofs per symbol at the *target on-board size* (legibility is size-dependent).
- **Fail-loud.** Render bridge surfaces `ReferenceRendering` diagnostics rather than emitting a blank
  PNG; a zero-area/empty token degrades to a visible placeholder (inherits the canvas FR-013 rule).

---

## 9. Risks & open questions

1. **Text/glyph legibility on the CPU raster.** The PoC used *vector* sigils (not `Text`) partly
   because pure-CPU text can render as tofu without a real measurer installed. **Decision for v1:**
   identity = vector sigils only (also honours "symbol, not depiction"). Revisit if label text is
   wanted (would require the `setRealTextMeasurer` path, `fs-gg-scene`).
2. **Codec round-trip cost** in a tight loop (D2). Acceptable for design-time galleries; promote a
   direct public raster entry only if profiling shows it matters.
3. **Channel overload.** Roster with many independent stats may exceed the legibility budget. Future:
   a **legibility linter** that scores a `ChannelMap` against §4 capacities and warns.
4. **Rotating vs screen-aligned gauges.** The PoC kept the health arc screen-aligned (readable) while
   the body rotates; a per-theme toggle may be wanted. Open.
5. **Live board vs still gallery.** This doc covers the *design-time* loop; wiring the approved set
   into a runnable `volatile'` canvas board (drive it live) is a natural follow-on sample (§10 P3).
6. **Grammar breadth.** Only the Token grammar is prototyped; Badge/Ring (§4) are specified but
   unbuilt.

---

## 10. Delivery roadmap

> Turns the architecture (§5) and decisions (§6) into a **sequenced, gated** plan. Every edit site
> is anchored against the current tree (verified 2026-06-25). Read §1–§9 for the *why*; read this for
> the *what / when / in-what-order*. Each milestone is independently shippable and ends on an
> evidence gate. P0 (the PoC) is complete; M1 onward is the build.

### 10.1 Objectives & success criteria

**O1 — Legible symbol library.** A pure `token : Token -> Scene` renders the full channel grammar
(§4) into a clipped board symbol. *Done when:* golden-scene tests show every channel observably
altering the output, distinguishable at the target on-board size.

**O2 — Deterministic motion + review boards.** `animate`, `gallery`, `filmstrip` produce
reproducible scenes. *Done when:* a filmstrip is byte-reproducible from a phase schedule (no
wall-clock read), mirroring `Loop.advance` determinism.

**O3 — Public headless render bridge.** A scriptable `Scene → PNG` path with no core-surface
change. *Done when:* `Render.toPng` returns a non-blank, `ReferencePassed` PNG from FSI without
reaching any `internal`.

**O4 — Skill-driven repeatable loop.** Any agent runs the §5.5 loop identically. *Done when:* the
`fs-gg-symbology` skill drives intake→map→render→critique→tweak end-to-end and passes the skill-parity
check (`fs-gg-diagnostics`).

**O5 — End-to-end workflow with provenance.** A real roster becomes an approved control set with an
audit trail. *Done when:* a dry-run produces iterated galleries + mapping snapshots and a pinned
final set.

| ID | Success criterion | Verified by |
|---|---|---|
| **SC-001** | Determinism: identical `Token` ⇒ byte-identical `Scene`; gallery package identity stable across runs | property + `SceneCodec` identity test |
| **SC-002** | Channel presence: each channel observably changes the rendered PNG | `fs-gg-diagnostics` readback at target size |
| **SC-003** | Codec fidelity: export→import→raster preserves Path / gradient / `Dash` / `Arc` / stroke | regression test (proven in P0) |
| **SC-004** | No core-surface drift: only `FS.GG.UI.Symbology(.Render)` baselines change | `tests/Package.Tests` surface gate |
| **SC-005** | Skill parity: `fs-gg-symbology` present + consistent across skill trees | `fs-gg-diagnostics` skill-parity check |
| **SC-006** | Loop reproducibility: filmstrip frames byte-reproducible from a phase schedule | determinism test (no wall clock) |
| **SC-007** | Legibility at size: non-blank + separable faction/class at the target board size | readback evidence |

### 10.2 Milestones & sequencing

```
P0 done ─▶ M0 gate ─▶ M1 core lib ─▶ M2 motion+layout ─┐
  (PoC)     (½d)        (2–3d)            (1–2d)        ├─▶ M5 loop dry-run ─▶ M6 live board ─▶ M7 backlog
                          └──────▶ M3 render bridge ────┤        (1d)            (2–3d)         (∞)
                                      (½–1d)            │
                          M4 skill (after M3) ─────────┘
                              (1–2d)
```

**Critical path:** P0 → M0 → M1 → M3 → M4 → M5 → M6. M2 (motion/layout) depends only on M1 and can
overlap M3/M4. M3 (render bridge) needs only the `Scene` type, so it can start right after M0 in
parallel with M1. **M1→M4 (~5–7d) is the minimum viable agent loop**; M6 adds the live board.
Rough total to M6 ≈ **8–13 working days**.

### 10.3 Gated phase plan

#### M0 — Decision gate & scope lock  *(½ day)* — **gate before production code**
Resolve the four gates (§10.4). Spike (T0.1): a ~20-line throwaway confirming `Render.toPng` (the
§5.3 wrapper) compiles and renders a one-token gallery from a *new* project against the public
`ReferenceRendering.run`. **Exit:** spike renders `ReferencePassed`; G1–G4 recorded in §11.

#### M1 — `FS.GG.UI.Symbology` core library  *(2–3 days)* → **O1**
| Task | Deliverable / edit site |
|---|---|
| T1.1 | New project `src/Symbology/Symbology.fsproj` (references **only** `FS.GG.UI.Scene`); register in `FS.GG.Rendering.slnx` |
| T1.2 | `Symbology.fsi` authored **first**: `Faction`/`Klass`/`Sigil`/`TokenState` enums, `Token`, `defaultToken`, `token` (§7) |
| T1.3 | `Symbology.fs`: silhouette + sigil tables; `token` body (Appendix A); non-public helpers (`place`/`pathOf`/`strokePaint`/`lerpColor`) |
| T1.4 | `tests/Symbology.Tests`: golden token scenes; determinism (identical props ⇒ identical scene, **SC-001**); channel-presence readback (**SC-002**); codec-fidelity pin (**SC-003**) |
| T1.5 | Surface baseline `readiness/surface-baselines/FS.GG.UI.Symbology.txt`; surface gate expects the additions (**SC-004**) |

**Exit (O1):** `token` paints all channels; golden + determinism green; baseline pinned.

#### M2 — Motion + layout  *(1–2 days; overlaps M3/M4)* → **O2**
| Task | Deliverable / edit site |
|---|---|
| T2.1 | `Motion` DU + `animate : Motion -> Token -> phase:float -> Scene` (pulse / spin / blink / damage / moving — Appendix A) in `Symbology.fs[i]` |
| T2.2 | `gallery : cols -> spacing -> Token list -> Scene` and `filmstrip : samples -> (Motion * Token) list -> Scene` |
| T2.3 | Tests: per-motion golden; `filmstrip` byte-reproducible from a phase schedule (**SC-006**) |

**Exit (O2):** deterministic motion overlays + review boards; baseline updated.

#### M3 — Render bridge  *(½–1 day; starts after M0)* → **O3**
| Task | Deliverable / edit site |
|---|---|
| T3.1 | New project `src/Symbology.Render/Symbology.Render.fsproj` (references `Symbology` + `SkiaViewer`); register in slnx |
| T3.2 | `Render.toPng : Size -> Scene -> dir -> string` wrapping `ReferenceRendering.run` (§5.3); **fail-loud** — raise with diagnostics on any non-`ReferencePassed` verdict |
| T3.3 | Smoke test: render a gallery → assert `ReferencePassed` + non-blank PNG |

**Exit (O3):** public, scriptable `Scene → PNG`; **no** `Controls`/`Canvas`/`SkiaViewer` surface change (**SC-004**).

#### M4 — `fs-gg-symbology` skill + scripting recipe  *(1–2 days; after M3)* → **O4**
| Task | Deliverable / edit site |
|---|---|
| T4.1 | `.claude/skills/fs-gg-symbology/SKILL.md` **mirrored** to `.agents/skills/` and `template/product-skills/` (matches existing `fs-gg-*` mirroring) |
| T4.2 | Skill content: the §4 grammar + legibility rules (assign-by-urgency, redundancy, one-active-motion, hue-collision avoidance), the library API, the `ChannelMap` pattern, the FSI recipe (`#r` lib + render bridge → roster → map → `Render.toPng (Symbology.gallery …)` → read PNG back), and the §5.5 feedback protocol (timestamped galleries + mapping snapshots) |
| T4.3 | A reference `.fsx` template (roster → `ChannelMap` → gallery render) checked in beside the skill |
| T4.4 | Skill-parity check green (**SC-005**) |

**Exit (O4):** an agent runs the §5.5 loop consistently end-to-end on a sample roster.

#### M5 — End-to-end loop dry-run  *(1 day)* → **O5**
| Task | Deliverable / edit site |
|---|---|
| T5.1 | Run the full workflow on a real roster (e.g. 6–10 RTS units); iterate the `ChannelMap` across ≥2 feedback rounds; emit the approved control-set module |
| T5.2 | Provenance per **D7**: timestamped galleries + mapping snapshots under a working dir; pin a golden final gallery |

**Exit (O5):** demonstrated render→tweak→approve loop with an audit trail (**SC-007**).

#### M6 — Live board sample  *(2–3 days)*
| Task | Deliverable / edit site |
|---|---|
| T6.1 | `samples/SymbologyBoard`: the approved set on a `volatile'` canvas + `Loop`-driven motion (raw input optional); seeded headless evidence — mirrors `samples/CanvasDemo` |
| T6.2 | Register in slnx; in-tree `ProjectReference`s now, package-feed swap at publish (same caveat as CanvasDemo) |

**Exit:** runnable board; deterministic seeded evidence.

#### M7 — Governance & breadth  *(COMPLETE — all three threads shipped 2026-06-25)*
Legibility **linter** (score a `ChannelMap` against §4 channel capacities; warn on overload);
**Badge** and **Ring** grammars (§4) behind the same channel vocabulary; optional **label text** via
the `setRealTextMeasurer` path (`fs-gg-scene`) once tofu-free text is needed.

- ✅ **Legibility linter** — [spec 194](../../specs/194-symbology-legibility-linter/spec.md) (`Legibility.score`/`scoreAnimated`).
- ✅ **Badge / Ring grammars** — [spec 195](../../specs/195-symbology-badge-ring-grammars/spec.md) (`Grammar.Token|Badge|Ring`, `render`/`galleryIn`/`filmstripIn`/`animateIn`).
- ✅ **Optional identity label** — [spec 196](../../specs/196-symbology-label-text/spec.md): one optional
  `Token.Label : string option`, sited per grammar, tofu-free at the render edge, opt-in byte-identical
  when unused; the §9-risk-1 "identity = vector sigils only" open item is now resolved (label is an
  opt-in inspection-detail channel that complements, never replaces, the sigil).
- ✅ **Multi-line / paragraph label** — [spec 197](../../specs/197-symbology-multiline-label/spec.md):
  the same `Label` field carries several short lines (hard `\n` breaks + measured soft-wrap), capped and
  fitted per grammar; a one-line-fitting label stays byte-identical to 196.
- ✅ **Rich-text label runs** — [spec 198](../../specs/198-symbology-rich-text-label/spec.md):
  `Token.Label : LabelText option` with `LabelText.Rich of LabelRun list` carrying per-run
  **colour / weight / size**; an all-default run is byte-identical to the plain label.
- ✅ **Full rich-text layout** — [spec 199](../../specs/199-rich-text-layout/spec.md): completes the two
  items 198 deferred — **paragraph layout** (`LabelText.Laid of LabelParagraph list`, per-paragraph
  `LabelAlign = Leading | Center | Trailing | Justify` incl. measured justification + explicit breaks)
  and the **typographic run attributes** (`Italic` / `Underline` / `Strike` / `Tracking`). Pure
  scene-only (synthetic slant via `withPerspective`, decoration via `Scene.line`, tracking via per-glyph
  `glyphRunProof` — no new primitive / font / GPU path); `Center` default reproduces the 198 flow
  byte-for-byte (layered zero drift). Deferred the two completion items below to a dedicated thread.
- ✅ **Auto-label & label-bound motion** — [spec 200](../../specs/200-auto-label-bound-motion/spec.md):
  lifts the two items 199 deferred. **Auto-label** — `Token.AutoLabel : AutoLabelSpec option` projects a
  compact, game-agnostic label from the `Token`'s OWN encoded channels (`AutoField` selectors:
  faction/class/state codes, health/threat tiers, speed pips, shield flag), explicit `Label` always wins,
  per-game stats stay the caller's. **Label-bound motion** — `Token.LabelMotion : LabelMotion option`
  (`TypeOn | Fade | Pulse | Scroll`) animates the resolved label as a pure function of the phase the board
  already supplies — no new clock, no board/motion signature change. Pure scene-only (prefix reveal via
  `glyphRunProof`, alpha via `Paint.withOpacity`, scale via `withPerspective`, in-region offset via
  `Scene.clipped`/`translate` — no new primitive / font / GPU path); `None`/rest-phase reproduces the 199
  symbol byte-for-byte (layered zero drift); tofu-free verified at the render edge; the legibility linter
  is unchanged (label stays inspection-detail). **The rich-text identity-label thread, M7, and the M0–M7
  roadmap are complete.**

### 10.4 Decision gates (resolve at M0)
- **G1 — Library home.** Dedicated `FS.GG.UI.Symbology` vs folding into `FS.GG.UI.Canvas`. *Default:
  dedicated (D1).*
- **G2 — Render bridge.** `ReferenceRendering` round-trip (v1) vs a new public direct-raster entry in
  `SkiaViewer`. *Default: round-trip — zero new core surface, codec fidelity proven (D2);* promote
  direct-raster only if loop latency demands it.
- **G3 — v1 grammar scope.** Ship the **Token** grammar only; defer Badge/Ring to M7. *Default: yes.*
- **G4 — Skill mirroring.** Which skill trees to populate. *Default: all three (`.claude`, `.agents`,
  `template/product-skills`) to match existing `fs-gg-*` skills.*

### 10.5 Workstream dependencies
- G1–G4 (M0) block M1/M3/M4 edit sites — resolve first.
- M2, M3 depend only on M1's `Token`/`Scene` types (M3 only on `Scene`) — both can overlap.
- M4 depends on M3 (the recipe calls the render bridge) and on M1/M2 (the API it documents).
- M5 depends on M1–M4 (the full loop); M6 depends on M5's approved set + the `canvas`/`Loop`
  prerequisites (already shipped, spec 191).
- **SC-004 surface gate** runs inside M1/M2/M3 — only the new `Symbology(.Render)` baselines may move.

### 10.6 Acceptance evidence per milestone
Each milestone closes only on captured evidence (Constitution V): M1/M2 — golden + determinism +
readback under `readiness/`; M3 — `ReferencePassed` smoke PNG; M4 — skill-parity report; M5 — the
iterated-gallery audit trail + pinned final; M6 — seeded reproducible sample evidence. No assertion
weakened, no test skipped; any golden delta reviewed, never silent.

### 10.7 Delivery risks (beyond §9)
| Risk | Likelihood | Mitigation |
|---|---|---|
| New-project overhead (two `Symbology*` projects) inflates the solution | Med | Mirrors the accepted spec-191 `FS.GG.UI.Canvas` precedent; render bridge is tiny; revisit folding if overhead outweighs separation |
| Render-bridge codec round-trip too slow for a tight loop | Low–Med | Acceptable for design-time galleries; G2 leaves the direct-raster promotion open; profile in M3 |
| Skill drift across the three mirrored trees | Med | Single authored `SKILL.md` + skill-parity check (SC-005) gates divergence |
| Stale `file:line` / project anchors as the tree evolves | Med | Anchors verified 2026-06-25; re-verify at M0 |

### 10.8 Out of scope (this iteration)
GPU/compute shaders beyond `Scene` + Skia; an ECS/entity runtime for very large rosters; tofu-free
label text (M7); Badge/Ring grammars (M7); a full-window game host (covered by spec 191's scope, not
here); auto-generating the `ChannelMap` from stats without a human-in-the-loop (the loop is
deliberately human-approved).

---

## 11. Changelog
- **2026-06-25** — Document created. PoC (P0) completed: Directional-Token element + motion
  filmstrip rendered headlessly through the live Skia raster path; codec preservation of all paint
  channels (SC-003) and pure-`'props -> Scene` determinism confirmed. Decisions D1–D8 recorded;
  comprehensive gated roadmap (M0–M7, objectives O1–O5, success criteria SC-001…SC-007) added.
  *(G1–G4 decisions to be recorded here at M0.)*

---

## Appendix A — PoC source (seed of `FS.GG.UI.Symbology`)

Condensed from the 2026-06-25 session (full harness lived in scratch). Composes only public
`Scene`/`Path`/`Paint` primitives; rendered via `ReferenceRendering.run`.

```fsharp
// local silhouette (units of R), nose toward +x
let silhouette = function
    | Mobile -> [ -0.70,-0.60; 0.30,-0.60; 1.00,0.0; 0.30,0.60; -0.70,0.60 ]
    | Heavy  -> [ -0.80,-0.50; 0.20,-0.72; 0.85,-0.28; 0.85,0.28; 0.20,0.72; -0.80,0.50 ]
    | Scout  -> [ -0.95,0.0; 0.0,-0.52; 1.10,0.0; 0.0,0.52 ]

// world point from local (lx,ly): centre + scale + heading rotation
let place t (lx,ly) =
    let c,s = cos t.Heading, sin t.Heading
    { X = t.Cx + t.R*(lx*c - ly*s); Y = t.Cy + t.R*(lx*s + ly*c) }

let token (t: Token) : Scene =
    let body   = silhouette t.Klass |> List.map (place t)
    let cmds   = pathOf true body
    let centre = place t (0.0, 0.0)
    let glow   = Scene.path (Path.create PathFillType.Winding cmds)               // energy fill
                   (Paint.fill (teamColorA 30 t.Faction) |> Paint.withAntialias true
                    |> Paint.withShader (RadialGradient(centre, t.R*1.05,
                          [ teamColorA 220 t.Faction; teamColorA 32 t.Faction ])))
    let outline = Scene.path (Path.create PathFillType.Winding cmds)              // hue/width/dash
                    (strokePaint (teamColor t.Faction) (1.5 + t.Threat*4.5) (t.State = Suspected))
    let beads   = [ for i in 0 .. t.Speed-1 ->                                    // speed
                      Scene.circle (place t (-1.05 - float i*0.26, 0.0)) (t.R*0.12)
                        (teamColorA (max 40 (170 - i*35)) t.Faction) ]
    let sigil   = sigilScene t centre                                            // identity mark
    let hr      = t.R*1.18                                                        // health arc
    let health  = Scene.arc { X=t.Cx-hr; Y=t.Cy-hr; Width=hr*2.0; Height=hr*2.0 }
                    40.0 (100.0*t.Health)
                    (Paint.stroke (lerpColor red green t.Health) (max 2.5 (t.R*0.10))
                     |> Paint.withAntialias true |> Paint.withStrokeCap StrokeCap.Round)
    let shield  = if t.Shield then [ shieldPip t ] else []
    Scene.group ([ glow ] @ beads @ [ outline; sigil; health ] @ shield)

// motion overlays are pure (Token, phase01) -> Scene: animPulse / animSpin / animBlink /
// animDamage / animMove — drive them from a model Phase advanced by Loop.advance, painted into
// a `volatile'` canvas so chrome stays picture-cached.
```

**Render bridge used in the PoC** (the §5.3 helper):

```fsharp
let evidence =
    ReferenceRendering.run
        { PackageBytes = (SceneCodec.export scene).CanonicalBytes
          OutputDirectory = outDir; OutputSize = { Width = 920; Height = 660 }; Resources = [] }
// evidence.ImagePath -> content-hash-named PNG; evidence.Verdict = ReferencePassed
```

## 11. Gate decision record (resolved at M0 — implemented 2026-06-25)

The four M0 decision gates from §10.4 were resolved during planning (research.md "Gate decision
record") and confirmed by the M1–M5 implementation:

- **G1 — Library home = dedicated `FS.GG.UI.Symbology`** (Scene-only), not folded into Canvas/Controls
  (R1/D1). Shipped: `src/Symbology/` references `FS.GG.UI.Scene` alone.
- **G2 — Render bridge = `ReferenceRendering.run` round-trip, fail-loud on the real 3-case verdict**
  (R2/D2). Shipped: `src/Symbology.Render/` returns the image path only on `ReferencePassed` + a real
  `ImagePath`; raises with joined diagnostics otherwise. Direct-raster entry deferred (no measured
  latency problem). M0 spike confirmed `ReferencePassed` + non-blank PNG in this checkout.
- **G3 — v1 grammar = Directional Token only** (R3); Badge/Ring deferred to M7. Shipped: one `Token`
  record + `Faction`/`Klass`/`Sigil`/`TokenState`/`Motion` enums.
- **G4 — Skill mirroring = all three trees** (`.claude`, `.agents`, `template/product-skills`) plus the
  package-canonical under `src/Symbology/skill/` (R4). Shipped: `fs-gg-symbology` authored once + mirrored;
  `scripts/check-agent-skill-parity.fsx --fail-on high` is green (critical=0 high=0 warning=0).
