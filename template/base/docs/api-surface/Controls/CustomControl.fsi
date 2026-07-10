// See skill: fs-gg-ui-widgets
namespace FS.GG.UI.Controls

/// The author-supplied definition of a product-owned wrapper control: an `Id`, author-declared
/// `Effects` names, plus optional `Accessibility` and `Diagnostics`.
///
/// A `custom-control` does **not** paint author-supplied geometry — `renderTree` paints a labeled
/// placeholder frame for it. To draw arbitrary geometry use the `canvas` kind (`Canvas.create` with
/// `Canvas.scene`), which carries an immutable `Scene` through the render path; to compose must-show
/// chrome, build it from the primitive controls (`Border`/`TextBlock`/`Stack`).
type CustomControlDefinition =
    { Id: ControlId
      /// Author-declared effect names. Metadata only: surfaced through `CustomControl.validate`
      /// diagnostics, never applied by the renderer.
      Effects: string list
      Accessibility: AccessibilityMetadata option
      Diagnostics: ControlDiagnostic list }

/// Author a bespoke control from a `CustomControlDefinition`: `create` it as a `Control<'msg>` and `validate` it.
module CustomControl =
    /// Build a `Control<'msg>` from a `CustomControlDefinition` and the supplied `attrs`.
    val create: definition: CustomControlDefinition -> attrs: Attr<'msg> list -> Control<'msg>
    /// Check a `CustomControlDefinition` for authoring errors, returning any `ControlDiagnostic` list.
    val validate: definition: CustomControlDefinition -> ControlDiagnostic list
