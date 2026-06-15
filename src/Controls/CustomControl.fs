namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.Layout

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

module CustomControl =
    // Feature 122 (FR-006): a null/blank Id (or a null effect string) must yield a validation
    // diagnostic, not a NullReferenceException — the consumer reaches `validate`/`create` via the
    // reflection/preview path before any explicit guard. `String.IsNullOrWhiteSpace` is null-safe where
    // `s.Trim() = ""` dereferences a null `s`.
    let validate definition =
        [ if System.String.IsNullOrWhiteSpace definition.Id then
              yield Diagnostics.missingRequired None "custom-control" "id"
          if definition.Accessibility.IsNone then
              yield Diagnostics.missingAccessibility (Some definition.Id) "custom-control"
          for effect in definition.Effects do
              if System.String.IsNullOrWhiteSpace effect then
                  yield Diagnostics.missingRequired (Some definition.Id) "custom-control" "effect"
          yield! definition.Diagnostics ]

    let create (definition: CustomControlDefinition<'msg>) (attrs: Attr<'msg> list) =
        // Feature 122 (FR-006): a null/blank Id falls back to a safe synthetic id for the default
        // accessibility metadata so `Accessibility.defaultFor` is never handed a null.
        let safeId =
            if System.String.IsNullOrWhiteSpace definition.Id then
                "custom-control"
            else
                definition.Id

        Control.create "custom-control" (Attr.accessibility (definition.Accessibility |> Option.defaultValue (Accessibility.defaultFor "custom-control" safeId)) :: attrs)
        |> Control.withKey safeId
