namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput

/// Feature 191 (US1–US2, C2): the public constructor surface for the embedded `canvas` control kind —
/// one semantic kind, no per-theme fork (mirrors the `Display`/`Buttons` constructor-module pattern).
/// A canvas paints an application-supplied immutable `Scene` into its laid-out box; it can be marked
/// `volatile'` (per-frame, cache-isolated) and forwards raw pointer/keyboard input to the model.
module Canvas =

    /// Author-supplied immutable scene, painted into the control's box. Authored in canvas-local
    /// coordinates: origin (0,0) top-left, y-down, logical units (translated to the box origin and
    /// clipped to the box at paint time).
    val scene: scene: Scene -> Attr<'msg>

    /// Optional internal viewport transform (pan/zoom) applied to the content only — the laid-out box
    /// size and the hit-test box are unchanged (FR-016).
    val viewport: transform: PerspectiveTransform -> Attr<'msg>

    /// Mark this canvas volatile: bypass picture caching and wall it behind a repaint boundary so its
    /// per-frame change cannot dirty surrounding cached chrome (FR-004/FR-005).
    val volatile': Attr<'msg>

    /// Forward raw pointer samples (position, button, wheel) in canvas-local space to the model when the
    /// pointer is inside the box (FR-006).
    val onPointer: map: (PointerSample -> 'msg) -> Attr<'msg>

    /// Forward raw key events (`ViewerKey` + `KeyModifiers`) to a focused canvas (FR-007).
    val onKey: map: (ViewerKey -> KeyModifiers -> 'msg) -> Attr<'msg>

    /// Construct the canvas control from its attributes. With no `scene` attr the control paints a
    /// design-time placeholder.
    val create: attrs: Attr<'msg> list -> Control<'msg>
