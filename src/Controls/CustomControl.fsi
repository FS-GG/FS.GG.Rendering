namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.Layout

/// The author-supplied definition of a custom control: measure/render/layout/hit-test/event callbacks
/// plus optional `Accessibility` and `Diagnostics`, keyed by `Id`.
type CustomControlDefinition<'msg> =
    { Id: ControlId
      Measure: unit -> float * float
      Render: unit -> Scene
      Draw: unit -> Scene
      Layout: unit -> LayoutNode
      Clip: (float * float * float * float) option
      Effects: string list
      HitTest: float -> float -> bool
      Event: ControlEvent -> 'msg option
      Accessibility: AccessibilityMetadata option
      Diagnostics: ControlDiagnostic list }

/// Author a bespoke control from a `CustomControlDefinition`: `create` it as a `Control<'msg>` and `validate` it.
module CustomControl =
    /// Build a `Control<'msg>` from a `CustomControlDefinition` and the supplied `attrs`.
    val create: definition: CustomControlDefinition<'msg> -> attrs: Attr<'msg> list -> Control<'msg>
    /// Check a `CustomControlDefinition` for authoring errors, returning any `ControlDiagnostic` list.
    val validate: definition: CustomControlDefinition<'msg> -> ControlDiagnostic list
