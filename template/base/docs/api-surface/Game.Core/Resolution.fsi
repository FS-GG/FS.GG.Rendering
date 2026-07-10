// See skill: fs-gg-collision
// Mirrored from FS-GG/FS.GG.Game @ 0.3.0 (src/Game.Core/Resolution.fsi); regenerate when $(FsGgGameVersion) moves.
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

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Discrete grid knockback: advance from `start` by the per-cell delta `step` up to `distance`
    /// times, stopping in the last free cell before the first cell for which `blocked` is true. The
    /// returned cell is never `blocked`, and the number of steps taken never exceeds `distance`. A
    /// non-positive `distance`, or a first next-cell that is immediately `blocked`, returns `start`
    /// unchanged (`start` is assumed free — only each next cell is tested). A fixed-order walk, so it is
    /// deterministic; total for any total `blocked` predicate.
    val knockback: start: Cell -> step: Cell -> distance: int -> blocked: (Cell -> bool) -> Cell
