namespace FS.GG.UI.Controls

open FS.GG.UI.DesignSystem
open FS.GG.UI.Scene

/// Request used to inspect a rendered control tree as structured visual facts.
type ControlInspectionRequest<'msg> =
    { Scope: VisualInspectionScope
      Theme: Theme
      OutputSize: Size
      Control: Control<'msg>
      Presentation: string
      RunId: string option
      RelatedVisualEvidence: string list }

/// One retained-render transition to inspect.
type RetainedControlTransition<'msg> =
    { TransitionId: string
      PriorControl: Control<'msg> option
      CurrentControl: Control<'msg>
      InteractionId: string option
      ExpectedAffectedRegionIds: string list
      MaximumDirtyPercentage: float option
      IntentionalExceptions: IntentionalDamageException list }

/// Request used to inspect retained-render output and damage facts.
type RetainedControlInspectionRequest<'msg> =
    { Scope: VisualInspectionScope
      Theme: Theme
      OutputSize: Size
      Presentation: string
      RunId: string option
      Transition: RetainedControlTransition<'msg>
      RelatedVisualEvidence: string list }

/// Controls-owned adapter from `Control.renderTree` output to visual inspection artifacts.
module ControlInspection =
    /// Inspect a control tree by running the same `Control.renderTree` path used by the host.
    val inspect: request: ControlInspectionRequest<'msg> -> VisualInspectionArtifact

    /// Inspect an already rendered control tree when callers own the render step.
    val inspectRendered:
        scope: VisualInspectionScope ->
        presentation: string ->
        outputSize: Size ->
        control: Control<'msg> ->
        render: ControlRenderResult<'msg> ->
            VisualInspectionArtifact

    /// Inspect retained-render output by running `RetainedRender.init` and, when prior input exists, `RetainedRender.step`.
    val inspectRetained: request: RetainedControlInspectionRequest<'msg> -> RetainedInspectionArtifact
