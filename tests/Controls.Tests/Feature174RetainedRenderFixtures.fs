module Feature174RetainedRenderFixtures

open FS.GG.UI.Scene
open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type Msg =
    | PrimaryClicked
    | NavigateToTextNumericInput

let theme = Theme.light
let size: Size = { Width = 960; Height = 640 }

let text key value : Control<Msg> =
    TextBlock.create [ TextBlock.text value; Attr.width 220.0; Attr.height 28.0 ]
    |> Control.withKey key

let button key label msg : Control<Msg> =
    Button.create [ Button.text label; Button.onClick msg; Attr.width 160.0; Attr.height 40.0 ]
    |> Control.withKey key

let shell pageKey pageContent : Control<Msg> =
    Stack.create
        [ Attr.width 760.0
          Attr.height 520.0
          Attr.gap 0.0
          Stack.children
              [ text "showcase-title" "Second Ant Showcase"
                text "stable-chrome" "Controls"
                Stack.create
                    [ Attr.width 720.0
                      Attr.height 420.0
                      Attr.gap 0.0
                      Stack.children pageContent ]
                  |> Control.withKey pageKey ] ]
    |> Control.withKey "showcase-root"

let buttonScenario label : Control<Msg> =
    shell
        "buttons-page"
        [ text "buttons-heading" "Buttons"
          button "primary-button" label PrimaryClicked
          text "buttons-note" "Primary action" ]

let pageNavigationSource () : Control<Msg> =
    shell
        "buttons-page"
        [ text "buttons-heading" "Buttons"
          button "primary-button" "Primary" PrimaryClicked
          button "nav-text-numeric" "Text inputs" NavigateToTextNumericInput ]

let textNumericDestination () : Control<Msg> =
    shell
        "text-numeric-input"
        [ text "inputs-heading" "Text and numeric inputs"
          Stack.create
              [ Attr.width 700.0
                Attr.height 260.0
                Attr.gap 0.0
                Stack.children
                    [ text "name-label" "Name"
                      text "name-value" "Ada"
                      text "amount-label" "Amount"
                      text "amount-value" "42"
                      text "notes-label" "Notes"
                      text "notes-value" "Dense nested content" ] ]
            |> Control.withKey "input-panel" ]

let noReplayCachePage () : Control<Msg> =
    shell
        "plain-page"
        [ for i in 0 .. 7 ->
              text (sprintf "plain-%d" i) (sprintf "Plain row %d" i) ]

let internal retainedStep before after =
    let init = RetainedRender.init theme size before
    RetainedRender.step theme size init.Retained after

let direct after = Control.renderTree theme size after

let assertRenderMetadataParity label (retained: ControlRenderResult<Msg>) (oracle: ControlRenderResult<Msg>) =
    Expect.equal retained.Bounds oracle.Bounds (label + ": bounds")
    Expect.equal retained.Diagnostics oracle.Diagnostics (label + ": diagnostics")
    Expect.equal retained.EventBindings.Length oracle.EventBindings.Length (label + ": event binding count")
    Expect.equal retained.BoundIds oracle.BoundIds (label + ": bound ids")
    Expect.equal retained.NodeCount oracle.NodeCount (label + ": node count")
