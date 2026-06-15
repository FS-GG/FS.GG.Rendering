module ControlsFeature086PreviewParityTests

// Feature 086 — FR-010 regression guard. The 080 single-control PREVIEW
// (`Control.render` / `Widget.render`) must stay byte-identical despite the
// renderTree layout changes (collision-free keying, Bounds, hitTest) and the
// additive `Bounds` field on `ControlRenderResult`.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

type private Msg = Clicked

let private theme = Theme.light

let private samples: (string * Control<Msg>) list =
    [ "text-block", TextBlock.create [ TextBlock.text "Hello" ]
      "button", Button.create [ Button.text "Save"; Button.onClick Clicked ]
      "text-box", TextBox.create [ TextBox.value "name" ]
      "stack",
      Stack.create
          [ Stack.children
                [ TextBlock.create [ TextBlock.text "a" ]
                  Button.create [ Button.text "b"; Button.onClick Clicked ] ] ] ]

[<Tests>]
let feature086PreviewParityTests =
    testList "Feature 086 080-preview parity (FR-010)" [

        test "Control.render and Widget.render produce identical preview scenes" {
            for name, control in samples do
                let viaControl = Control.render theme control
                let viaWidget = Widget.render theme (Widget.ofControl control)
                Expect.equal viaWidget.Scene viaControl.Scene $"{name}: Widget.render delegates to Control.render byte-for-byte"
                Expect.equal viaWidget.NodeCount viaControl.NodeCount $"{name}: node counts match"
        }

        test "Control.render preview is deterministic (stable across calls)" {
            for name, control in samples do
                let first = Control.render theme control
                let second = Control.render theme control
                Expect.equal first.Scene second.Scene $"{name}: preview scene is deterministic"
        }

        test "Control.render preview Bounds stays empty — renderTree owns evaluated bounds" {
            for name, control in samples do
                let preview = Control.render theme control
                Expect.isEmpty preview.Bounds $"{name}: the 080 preview does not populate per-control Bounds"
        }
    ]
