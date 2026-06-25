# Contract — SymbologyBoard deterministic core (internal module sketch)

The sample's pure core, exercised by `tests/SymbologyBoard.Tests`. These are **internal sample modules**, not public package surface (no `.fsi`, no baseline — FR-012, CanvasDemo precedent). Sketched here `.fsi`-style only to pin shapes before implementation (Constitution I).

## `Roster` (Roster.fs) — approved M5 mapping (reused unchanged, FR-001)

```fsharp
type UnitStats =
    { Name: string; Side: string; Role: string
      Dps: float; Hp: float; HpMax: float
      Speed: float; Armor: float; Suspected: bool; Facing: float }

// Ported verbatim from specs/192-.../readiness/dry-run/FinalSymbolSet.fsx:
val mapUnit : UnitStats -> FS.GG.UI.Symbology.Token   // approved channel assignment, unchanged
val motionOf : UnitStats -> FS.GG.UI.Symbology.Token -> FS.GG.UI.Symbology.Motion  // overlay from approved channels (D3)
val roster : UnitStats list                            // the fixed approved set (6–10 units)
```

## `Board` (Board.fs) — deterministic simulation + scene + evidence

```fsharp
val dt : float                       // fixed step seconds (1.0/60.0), also the host tick interval
val BoardWidth : float               // fixed board extent
val BoardHeight : float

type BoardUnit = { Token: Token; Motion: Motion; X: float; Y: float; Vx: float; Vy: float }
type World     = { Units: BoardUnit list; T: float }   // T = accumulated step phase (no wall clock)
type Model     = { Step: StepState<World>; Seed: int }
type Msg       = Tick of float                          // (Point/Key deferred — D8/FR-014)

val init        : seed: int -> Model                    // seedWorld via Loop.init; seed drives positions/velocities
val update      : Msg -> Model -> Model                 // pure; Tick -> Loop.advance dt integrate
val renderScene : Model -> Scene                        // interpolate (Loop.alpha) + animate each unit at its pos; bounce keeps it on-board
val evidence    : seed: int -> script: Msg list -> string  // ONE call ⇒ ONE fingerprint: packageIdentity of renderScene's canonical bytes
```

> **Naming note (see cli-contract.md).** `Board.evidence` is the pure *fingerprint* function — one call returns one fingerprint. The two-run reproducibility *check* (call it twice, compare, report, exit-code) lives in the `evidence` **subcommand** in `Program.fs`, not in this function. Tests call `Board.evidence` directly: reproducible ⇒ `evidence s script = evidence s script`; seed-sensitive ⇒ `evidence s1 script <> evidence s2 script`.

## Behavioral contracts the tests assert

| Test | Asserts | Maps to |
|---|---|---|
| reproducible | `evidence s script = evidence s script` (byte-identical) | SC-001, FR-005 |
| seed-sensitive | `evidence s1 script <> evidence s2 script` for `s1 <> s2` | SC-002, FR-006 |
| on-board invariant | after N `update` steps, every `BoardUnit` centre is within `[radius, extent-radius]` on both axes | FR-011, SC-003 |
| non-empty board | a single-unit / degenerate roster still produces a non-blank `Scene` (non-empty canonical bytes) | edge case (spec) |
| no wall clock | `World.T` advances only via `Tick`; equal script ⇒ equal `T` (covered by reproducible) | FR-003 |

## Determinism contract

Equal `(seed, script)` ⇒ equal final `World` ⇒ equal `renderScene` `Scene` ⇒ equal `SceneCodec.export(...).CanonicalBytes` ⇒ equal `packageIdentity`. No member of the core reads a wall clock, performs IO, or draws on render-time randomness.
