# Phase 1 Data Model — Symbology Live Board Sample (M6)

All entities live inside the sample (`samples/SymbologyBoard/`) and its test project. None is public package surface (FR-012). Types are pure values; equal inputs ⇒ equal `Scene` ⇒ equal canonical bytes (the determinism identity).

## Entity: `UnitStats` (Roster.fs) — the per-game roster row

Ported verbatim from the approved `FinalSymbolSet.fsx` (FR-001). The raw game stats the approved mapping consumes.

| Field | Type | Notes |
|---|---|---|
| `Name` | `string` | display/debug only; not encoded by the grammar |
| `Side` | `string` | `"blue"`/`"red"`/other → `Faction` via `factionOf` |
| `Role` | `string` | `"tank"`/`"scout"`/other → `Klass` + `Sigil` |
| `Dps` | `float` | → `Threat` (stroke width) and `Charge` (interior gradient) |
| `Hp` / `HpMax` | `float` | → `Health` = `Hp / HpMax` |
| `Speed` | `float` | → discrete `Speed` channel |
| `Armor` | `float` | → `Shield` (mount) when `> 40` |
| `Suspected` | `bool` | → `TokenState` (Confirmed/Suspected) |
| `Facing` | `float` | → `Heading` |

**Mapping (reused, unchanged)**: `mapUnit : UnitStats -> Token` exactly as in `FinalSymbolSet.fsx` (`R = 30.0`, faction/klass/sigil/threat/charge/speed/health/state/shield/heading). The fixed roster is a `roster : UnitStats list` literal (6–10 units) carried in `Roster.fs`.

**Derived for motion** — `motionOf : UnitStats -> Token -> Motion` (D3): `Suspected` → `Blink`; high `Threat` (e.g. `≥ 0.66`) → `Pulse`; else `Moving` for `Mobile`/`Scout`, `Spin` for `Heavy`. Pure; chosen from already-approved channels, no grammar change.

## Entity: `BoardUnit` (Board.fs) — one symbol's live placement

The per-unit simulation state placed on the board.

| Field | Type | Notes |
|---|---|---|
| `Token` | `FS.GG.UI.Symbology.Token` | from `mapUnit`; fixed for the run |
| `Motion` | `FS.GG.UI.Symbology.Motion` | from `motionOf`; selects the `animate` overlay |
| `X` / `Y` | `float` | board-space centre; advanced each fixed step |
| `Vx` / `Vy` | `float` | velocity (board units/sec); seeded from `seed` + index; negated on edge bounce |

**Validation / invariants**:
- `radius ≤ X ≤ BoardWidth - radius` and likewise for `Y` after every step (FR-011) — enforced by the bounce in `integrate`.
- A zero-area token already degrades to a visible placeholder in the grammar (`Symbology.token`), so a degenerate unit still renders (edge case) — the board adds no clipping of its own.

## Entity: `World` (Board.fs) — the deterministic simulation state

| Field | Type | Notes |
|---|---|---|
| `Units` | `BoardUnit list` | the live roster placements |
| `T` | `float` | accumulated world time (sum of fixed `dt` steps) — the `animate` phase source (D3); never a wall clock |

`seedWorld : int -> World` builds `Units` from `roster` (`mapUnit`/`motionOf`) with positions/velocities derived from `seed` + index; `T = 0`. Different `seed` ⇒ different positions/velocities ⇒ different board (FR-006/SC-002).

## Entity: `Model` (Board.fs) — the MVU model

| Field | Type | Notes |
|---|---|---|
| `Step` | `StepState<World>` | from `FS.GG.UI.Canvas` `Loop.init`/`advance`; brackets `Previous`/`Current` for interpolation |
| `Seed` | `int` | echoed for diagnostics |

`init : int -> Model = fun seed -> { Step = Loop.init (seedWorld seed); Seed = seed }`.

## Entity: `Msg` (Board.fs) — MVU messages

| Case | Payload | Notes |
|---|---|---|
| `Tick` | `float` (elapsed seconds) | advances the sim by whole fixed steps via `Loop.advance dt integrate` |

(Optional, deferred per D8/FR-014: `Point`/`Key` cases reconstructed deterministically; not in the first cut and never feed `evidence`.)

## Behavior: simulation step and scene production

- `integrate : World -> float -> World` (pure): for each `BoardUnit`, advance `X += Vx*dt`, `Y += Vy*dt`, bounce `Vx`/`Vy` at the board edges (clamped so the symbol radius stays on-board); set `T += dt`. Reads only `(world, dt)` — no IO, no wall clock.
- `update : Msg -> Model -> Model`: `Tick elapsed -> { model with Step = Loop.advance dt integrate elapsed model.Step }`.
- `renderScene : Model -> Scene`: interpolate `Previous`→`Current` unit positions with `Loop.alpha dt model.Step`, and for each unit compose `Symbology.animate unit.Motion unit.Token world.T` translated to its interpolated `(X,Y)` (via the sample's own placement, mirroring CanvasDemo's `lerp`/`Elements.at`). Empty/degenerate roster still yields a non-blank board.

## Entity: Evidence fingerprint

- `evidence : int -> Msg list -> string` (pure): `script |> List.fold (fun m msg -> update msg m) (init seed)` then `SceneCodec.packageIdentity (SceneCodec.export (renderScene final)).CanonicalBytes`.
- **Identity**: equal `(seed, script)` ⇒ equal final `World` ⇒ equal `Scene` ⇒ equal canonical bytes ⇒ equal fingerprint (SC-001). Different `seed` ⇒ different `World` ⇒ different fingerprint (SC-002).

## Entity: Captured evidence artifact (readiness)

`specs/193-symbology-live-board/readiness/board-evidence.md` — the milestone exit record (FR-013/SC-006): the seed, the canonical fingerprint, confirmation that two same-seed runs matched (byte-identical), the differing fingerprint from a second seed, and the exact command that regenerates it.
