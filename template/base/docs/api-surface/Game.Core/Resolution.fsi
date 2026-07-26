// See skill: fs-gg-collision
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Resolution.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract module exposed by the FS.GG.Game.Core package.
/// The arcade/kinematic collision **response** layer — kept separate from detection (`Geometry`) per
/// the game-logic corpus's "detection separate from response" rule. Consumes the detection `Contact`
/// and the grid `Cell` and produces transforms; it never re-detects. All functions are pure, total
/// (NaN-safe; degenerate inputs never throw), and byte-deterministic. Impulse-based physics
/// (mass/restitution/friction) is a separate, out-of-scope heavy layer.
[<RequireQualifiedAccess>]
module Resolution =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Separate an overlapping body out of penetration along the minimum-translation vector: returns
    /// `position − contact.Normal × contact.Depth`. A zero-`Depth` contact returns `position`
    /// unchanged. No slop is applied (a caller wanting anti-jitter slop passes a `Contact` with reduced
    /// `Depth`). Pure and total: a NaN operand flows through arithmetically without throwing.
    val pushOut: position: Point -> contact: Contact -> Point

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Kinematic slide/stop: remove the component of `velocity` along the contact `normal`, keeping the
    /// tangential component — `velocity − (velocity · normal) × normal`. `normal` is assumed unit (every
    /// `Contact.Normal` is), so there is no internal normalization; the result's normal component is
    /// zero. Pure and total (NaN flows through).
    val slide: velocity: Point -> normal: Point -> Point

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// How a cell answers a unit trying to enter it. Three relations, because two are not enough: a
    /// binary "blocked" predicate can say *stop before it* and *walk through it*, and there is no way
    /// to say **enter it and stop there** — which is what water, a chasm, and a pit are. Mark water
    /// blocked and a unit shoved at the lake halts on dry land; mark it passable and the unit walks out
    /// the far side. Neither is the game.
    type CellStep =
        /// The unit moves onto this cell and may keep going. Ordinary ground — and lava, which is
        /// entered, hurts, and is left again.
        | Enter
        /// The unit moves onto this cell and stops there. Water, a chasm: a destination, usually fatal.
        | Stop
        /// The unit cannot enter. It stops on the previous cell. A wall, a mountain, an occupied cell,
        /// or off-board.
        | Block

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// Why a `push` ended, and where. The cell each case carries is the one that ended it — the cell
    /// entered-and-halted on, or the cell that refused entry — and it is exactly what the caller needs
    /// to attribute collision damage to the obstacle, or to drown the unit in the water it landed in.
    type PushStop =
        /// All `distance` steps were taken; nothing interrupted the walk.
        | Completed
        /// The unit entered this cell and stopped there. It occupies this cell.
        | Stopped of Cell
        /// The unit could not enter this cell. It occupies the previous one, which is `Push.Final`.
        | Blocked of Cell

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// The full record of a discrete displacement: the cells actually **entered**, in order and
    /// excluding `start`; the cell occupied at the end; and why the walk stopped.
    ///
    /// `Entered` is what a per-cell terrain tick folds over — a unit shoved across two lava tiles takes
    /// the tick twice — and it is the part the old `knockback` threw away.
    type Push =
        { Entered: Cell list
          Final: Cell
          Outcome: PushStop }

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Discrete grid displacement: advance from `start` by the per-cell delta `step` up to `distance`
    /// times, asking `classify` what each next cell does. `classify` is the sole coupling to the world,
    /// exactly as `cast` is for `Ballistics.step`: it absorbs flight (a flier's water is `Enter` where a
    /// ground unit's is `Stop`), occupancy, and board bounds without this module learning any of them.
    /// An unpushable unit is pushed `distance = 0`.
    ///
    /// `start` is assumed already occupied and is never classified; only each next cell is. A
    /// non-positive `distance` returns `start` with no `Entered` cells and `Completed`. A zero `step`
    /// re-enters the same cell each iteration and still terminates, bounded by `distance`.
    ///
    /// A fixed-order walk of at most `distance` steps: deterministic, and total for any total
    /// `classify`. Pushing several units into one another is order-dependent **by design** — the caller
    /// sequences them and closes `classify` over the occupancy each push updates.
    ///
    /// **`distance` is a memory bound, not only a loop bound.** Totality here is mathematical: the walk
    /// terminates for every `distance`. Operationally `Entered` accumulates one `Cell` per entered cell,
    /// so `push start step System.Int32.MaxValue (fun _ -> Enter)` exhausts memory rather than merely
    /// running long. `distance` is caller-controlled and is 1–3 in every game in the corpus, so this
    /// module does not clamp it: a maximum push distance is a game-defining parameter, and a module
    /// constant would be the wrong place to decide it. Callers that derive `distance` from content (a
    /// knockback stat, a designer's field) should bound it where that content is authored.
    val push: start: Cell -> step: Cell -> distance: int -> classify: (Cell -> CellStep) -> Push

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Discrete grid knockback: advance from `start` by the per-cell delta `step` up to `distance`
    /// times, stopping in the last free cell before the first cell for which `blocked` is true. The
    /// returned cell is never `blocked`, and the number of steps taken never exceeds `distance`. A
    /// non-positive `distance`, or a first next-cell that is immediately `blocked`, returns `start`
    /// unchanged (`start` is assumed free — only each next cell is tested). A fixed-order walk, so it is
    /// deterministic; total for any total `blocked` predicate.
    ///
    /// **Deprecated.** A binary `blocked` predicate cannot express a cell that is entered *and* stops
    /// the walk, and it discards both the stop reason and the cells crossed. It is exactly
    /// `(push start step distance (fun c -> if blocked c then Block else Enter)).Final` and is retained
    /// only so that this is an additive `game-sim-core` change.
    [<System.Obsolete("Use Resolution.push, which reports why the walk stopped and which cells were entered. See docs/reports/2026-07-10-effects-damage-pipeline-design.md §5.")>]
    val knockback: start: Cell -> step: Cell -> distance: int -> blocked: (Cell -> bool) -> Cell
