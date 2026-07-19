---
name: fs-gg-playtest
description: Prove a gameplay requirement headlessly — drive the world through the real Command frontier with FS.GG.Game.Harness, fingerprint the trace as a value, and keep synthetic shortcuts honestly labeled.
---

# Headless Playtest (Gameplay Evidence) Capability

## Scope

Use this skill to **prove a gameplay requirement with a test** — the non-synthetic, replayable kind an
FR-level gate accepts. `FS.GG.Game.Harness` drives your world headlessly through the **standard
`Command` input frontier** (raw key → keymap → `Command` → fixed step), folds whole fixed steps, and
fingerprints the resulting world **as a value**, so a replay is a comparison, not a tolerance check. It
gives you a scripted driver, an in-process `Bot` policy, a multi-seed bot-vs-bot matrix, and a **typed**
synthetic-state escape hatch whose cost is visible.

This is the test-time companion to the simulation half ([[fs-gg-game:fs-gg-game-core]]): that skill
teaches how the world *steps*; this one teaches how you *prove it plays correctly*. It materializes for
the `game` and `sample-pack` profiles. The harness reaches up to nothing beyond `FS.GG.Game.Core` and
the BCL — it performs no I/O and reads no wall clock, which is what lets its evidence be deterministic.

## Public Contract

The signatures you consume are bundled with this product under `docs/api-surface/Game.Harness/`:

- `Trace.fsi` — `Origin` (`InputDriven` | `Synthetic`) and the opaque `Trace<'f>`. `Trace.frames`
  is the value you assert on; `Trace.isSynthetic` is the provenance bit a gate reads.
- `Playable.fsi` — `Playable<'world,'key>` (init, keymap, apply, step, dt) and the `Bot<'view>` policy.
- `Driver.fsi` — `runScript` (raw key → keymap → `Command`), `runCommands`, and `runBot` with capture.
- `Matrix.fsi` — `Seat`, `MatchSetup`, `Match`, `runMatrix`, and `winRate`.
- `Synthetic.fsi` — the labeled fallback entry point.

The world `'world` and the raw key token `'key` are yours; the harness never learns a concrete world.
Every `Bot` draw threads a seeded value `Rng` — deconstruct with `let struct (cmds, next) = …`.

## Describe your game as a `Playable`, then drive the real input route

A `Playable` is the whole contract the scripted and bot drivers need. `Apply` and `Step` are pure
transitions (Model–Update–Effect); `Keymap` is *your* game's real binding, so a scripted test hits the
exact route a player does. `runScript` resolves each raw key through the keymap into a `Command` before
applying it — an **unbound token produces no command** — then advances exactly one whole fixed step at
`Playable.Dt`; no variable `dt`, and no render interpolant is fed back.

```fsharp
open FS.GG.Game.Core
open FS.GG.Game.Harness

// Your game as data the harness can drive. Apply and Step are pure: no I/O, no wall clock.
type World = { PaddleY: int; Tick: int }

let playable: Playable<World, string> =
    { Init = { PaddleY = 4; Tick = 0 }
      Keymap = Map [ "w", Command.MoveNorth; "s", Command.MoveSouth ]
      Apply =
        fun cmd w ->
            match cmd with
            | Command.MoveNorth -> { w with PaddleY = w.PaddleY - 1 }
            | Command.MoveSouth -> { w with PaddleY = w.PaddleY + 1 }
            | _ -> w
      Step = fun w _dt -> { w with Tick = w.Tick + 1 }
      Dt = 1.0 / 60.0 }

// Drive the real input route: raw key -> keymap -> Command -> one fixed step per frame.
let script = [ [ "w" ]; [ "w" ]; [ "s" ]; [] ] // one frame = the keys held that tick
let a = Driver.runScript playable Driver.identityFingerprint script
let b = Driver.runScript playable Driver.identityFingerprint script
let replays: bool = Trace.equalFrames a b // true — two runs of one script are byte-identical
```

`identityFingerprint` compares the whole `'world` by structural equality — the default. When the world
is large or carries fields irrelevant to the assertion, pass a projection `('world -> 'f)` instead and
the trace compares only that.

## Let a bot generate states, then replay its playthrough

A `Bot` decides from a **caller-supplied view and a seeded `Rng` only** — the full world model cannot
appear in its signature, so a policy genuinely cannot cheat by reading the whole board. `runBot`
captures every command it issued, and that capture replays through `runCommands` to a byte-identical
trace — a bot (or agent) playthrough becomes a regression golden for free.

```fsharp
open FS.GG.Game.Core
open FS.GG.Game.Harness

type World = { BallRow: int; PaddleRow: int; Tick: int }
type View = { Ball: int; Paddle: int } // a projection — never the world model

let playable: Playable<World, string> =
    { Init = { BallRow = 0; PaddleRow = 4; Tick = 0 }
      Keymap = Map.empty
      Apply =
        fun cmd w ->
            match cmd with
            | Command.MoveNorth -> { w with PaddleRow = w.PaddleRow - 1 }
            | Command.MoveSouth -> { w with PaddleRow = w.PaddleRow + 1 }
            | _ -> w
      Step = fun w _ -> { w with Tick = w.Tick + 1 }
      Dt = 1.0 / 60.0 }

let bot: Bot<View> =
    { Decide =
        fun view rng ->
            if view.Ball < view.Paddle then struct ([ Command.MoveNorth ], rng)
            else struct ([ Command.MoveSouth ], rng) }

let observe (w: World) : View = { Ball = w.BallRow; Paddle = w.PaddleRow }
let run = Driver.runBot playable observe bot 1234UL 600 Driver.identityFingerprint
// The captured commands replay through the scripted driver to a byte-identical trace.
let replay = Driver.runCommands playable Driver.identityFingerprint run.Captured
let same: bool = Trace.equalFrames run.Trace replay
```

## Assert a balance property across many seeds

`Matrix.runMatrix` runs a set of `(bot, bot, seed)` matches, one outcome per match, **independent of
the order** they are supplied — each match is a pure function of its seed. `outcome : 'world -> 'o` is
yours, so the world type never leaks into the runner. `winRate` folds the outcomes into the band an
assertion checks.

```fsharp
open FS.GG.Game.Core
open FS.GG.Game.Harness

type World = { Ticks: int; Lead: int }
type Outcome =
    | ChallengerWon
    | BaselineWon
    | Drawn

let challenger: Bot<int> = { Decide = fun _ rng -> struct ([ Command.MoveNorth ], rng) }
let baseline: Bot<int> = { Decide = fun _ rng -> struct ([], rng) }

let setup: MatchSetup<World, int> =
    { Dt = 1.0
      Init = fun _rng -> { Ticks = 0; Lead = 0 }
      Observe = fun _seat w -> w.Ticks
      Apply =
        fun seat _cmd w ->
            match seat with
            | Seat.A -> { w with Lead = w.Lead + 1 }
            | Seat.B -> { w with Lead = w.Lead - 1 }
      Step = fun w _ -> { w with Ticks = w.Ticks + 1 }
      IsOver = fun w -> w.Ticks >= 100
      MaxSteps = 200 }

let winner (w: World) : Outcome =
    if w.Lead > 0 then ChallengerWon
    elif w.Lead < 0 then BaselineWon
    else Drawn

let matches = [ for seed in 0UL..49UL -> { Seed = seed; A = challenger; B = baseline } ]
let outcomes = Matrix.runMatrix setup winner matches |> List.map snd
let winRateChallenger: float = Matrix.winRate (fun o -> o = ChallengerWon) outcomes
```

## Evidence: synthetic is a labeled fallback, and it never satisfies

`Synthetic.trace` lets you start from hand-built worlds when driving from real input is too expensive.
It is the **only** route to an `Origin.Synthetic` trace, and there is no route from it to
`Origin.InputDriven` — the provenance is unforgeable, so any trace or evidence derived from a hand-built
world is self-identifying.

```fsharp
open FS.GG.Game.Harness

type World = { AboutToWin: bool }

// The ONLY route to an Origin.Synthetic trace: its evidence is self-identifying and never satisfies.
let project (w: World) : bool = w.AboutToWin
let hand = Synthetic.trace project [ { AboutToWin = true }; { AboutToWin = false } ]
let disclosed: bool = Trace.isSynthetic hand // true — recorded as disclosed, not satisfying
```

That label has teeth. Under the SDD satisfaction rule an obligation is satisfied **only** by
`result: pass` **and** `synthetic: false`, and Governance's non-relaxable `evidenceNotSynthetic` gate
taints synthetic evidence down the DAG. So:

> A gameplay FR is satisfied only by an `Origin.InputDriven` trace — a script or a bot driven through
> the real `Command` frontier. A `Synthetic.trace` **discloses** a stand-in and can never close the
> obligation, no matter how green it looks.

The hatch is a distinct, typed surface by deliberate design of the harness: the shortcut is available,
but its cost is visible at the type level and in the evidence record, never silent. The per-FR gate
that reads that provenance bit and refuses a synthetic pass is ADR-0048's — this package is the
capability that gate presumes, not the other way round. Reach for real input first; use
`Synthetic.trace` only to reach a state that real input genuinely cannot, and expect its obligation to
read as *disclosed, not satisfied*.

## Package Boundary

The harness depends only on `FS.GG.Game.Core` and the BCL — no render/input stack, no keymap type, no
I/O, no wall clock. Keep your `Playable.Apply`/`Step` pure and thread the seeded `Rng`; the moment a
step reads a clock or an ambient RNG, replay dies and the FR gate is right to reject it. The keymap the
scripted driver folds through is *your* game's own binding (author it beside your input handling); the
harness never learns a device — mapping a real device to a `Command` is the render layer's job
([[fs-gg-rendering:fs-gg-keyboard-input]]).

## Test Commands

```bash
dotnet test                                          # run the whole gameplay suite
dotnet test --filter "FullyQualifiedName~Playtest"   # just the headless gameplay tests
```

Record the run as an `observedRun` receipt (a TRX/JUnit report) so the per-FR gate reads a run that
actually happened, not a self-attested pass.

## Common pitfalls

- **A `Step` that reads a wall clock or ambient RNG.** Determinism dies silently; the trace stops
  replaying. Thread the seeded `Rng` and take `dt` as a parameter.
- **Feeding the render interpolant back into the sim.** `alpha` is presentation only; never step with it.
- **A too-wide fingerprint golden.** If the world carries render-only or timestamp-like fields, project
  them out — otherwise the golden is brittle for reasons unrelated to the FR.
- **Reaching for `Synthetic.trace` to make a red FR green.** It cannot; it records as disclosed. Drive
  the real input instead, or accept the obligation as unmet.
- **A bot view that is the whole world.** Instantiating `'view = 'world` technically compiles, but it
  throws away the point of the boundary — project the view down to what the policy legitimately sees.

## Generated Product

Author a `Playable` beside your world, write one headless test per gameplay FR driving `runScript` or
`runBot` through it, assert on `Trace.frames`, and record the TRX. Balance-shaped FRs use `runMatrix` +
`winRate`; a state real input cannot reach uses `Synthetic.trace` and is declared synthetic in its
evidence.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs), then community sources. If your product uses
Spec Kit, record findings under the feature's `specs/<feature>/feedback/`; otherwise record them in this
skill's **Sources** line. Offline, the mandate degrades to recording "research blocked — <why>".

## Related

- [[fs-gg-game:fs-gg-game-core]] — the simulation the harness drives: the fixed-step `Loop`, the seeded
  `Rng`, and why `alpha` must never re-enter the sim.
- [[fs-gg-game:fs-gg-ai]] — `Ai.TeamView`, the sealed fog boundary you can instantiate a `Bot`'s `'view`
  as to keep a policy honestly blind to the full model.
- [[fs-gg-rendering:fs-gg-keyboard-input]] — map a real device to the `Command` values your keymap binds.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SplitMix64 (the RNG determinism relies on): https://prng.di.unimi.it/splitmix64.c
