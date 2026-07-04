namespace FS.GG.UI.Canvas

open FS.GG.UI.Scene

/// Feature 191 (US3, C3/FR-008): a pure, position-independent drawable — a function from its props to
/// an immutable `Scene`. Authored in canvas-local coordinates (origin top-left, y-down). Composes with
/// `Elements.at`/`Elements.layer`; never mutates.
type Element<'props> = 'props -> Scene

/// Feature 191 (US3, C3/FR-008): pure scene combinators for building canvas content. Every function
/// returns an immutable `Scene` and is deterministic — identical arguments yield an identical `Scene`.
/// Position-independent: each primitive is authored at the local origin and placed with `at`.
[<RequireQualifiedAccess>]
module Elements =

    /// A filled/stroked rectangle of the given size at the local origin (0,0).
    val rect: w: float -> h: float -> paint: Paint -> Scene

    /// An image/sprite of the given size at the local origin (0,0).
    val sprite: image: string -> w: float -> h: float -> Scene

    /// A filled circle of radius `r` centred at the local origin (0,0).
    val circle: r: float -> fill: Color -> Scene

    /// A connected poly-line through `points` (canvas-local coordinates).
    val polyline: points: Point list -> paint: Paint -> Scene

    /// Translate a sub-scene to (x, y) in canvas-local space. Nesting composes additively.
    val at: x: float -> y: float -> scene: Scene -> Scene

    /// Group sub-scenes into one (paint order = list order).
    val layer: scenes: Scene list -> Scene

    /// Wrap an expensive fragment as a backend replay-cache boundary keyed on `key` (identity) and the
    /// fragment's structural content (so an unchanged fragment replays, a changed one re-records).
    /// Transparent to deterministic goldens — every Scene consumer recurses straight into the content.
    val cached: key: string -> scene: Scene -> Scene
