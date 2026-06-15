module ControlsAccessibilityTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

[<Tests>]
let typedGalleryAccessibilityTests =
    testList "Feature 071 typed gallery accessibility (US2)" [
        // SC-005 / contract G6: the typed-authored gallery panel renders through the
        // existing path at >=2 viewports and exposes the expected accessibility roles
        // for its mechanic-group representatives.
        test "typed gallery panel exposes expected accessibility roles at two viewports (SC-005, G6)" {
            let panel = ControlsTypedGalleryPanel.panel

            for width, height in [ 320, 240; 1024, 768 ] do
                let rendered = Control.render Theme.light panel
                let evidence = Scene.renderReadbackEvidence { Width = width; Height = height } rendered.Scene
                Expect.isEmpty rendered.Diagnostics $"typed gallery panel has no diagnostics at {width}x{height}"
                Expect.isNonEmpty evidence.DeterministicHash $"typed gallery panel render evidence has a deterministic hash at {width}x{height}"

            let kinds = ControlsTypedGalleryPanel.kindsPresent panel
            let roleOf kind = (Accessibility.defaultFor kind "typed").Role

            [ "button", AccessibilityRole.Button
              "text-area", AccessibilityRole.TextBox
              "check-box", AccessibilityRole.CheckBox
              "list-box", AccessibilityRole.List
              "tabs", AccessibilityRole.Tab
              "line-chart", AccessibilityRole.Chart
              "graph-view", AccessibilityRole.Graph ]
            |> List.iter (fun (kind, role) ->
                Expect.isTrue (Set.contains kind kinds) $"typed gallery panel includes a typed {kind}"
                Expect.equal (roleOf kind) role $"{kind} exposes the {role} accessibility role")
        }
    ]

[<Tests>]
let expansionAccessibilityTests =
    // Feature 072 (T014, T026, FR-009): each new control's lowered tree carries its
    // catalog accessibility role, an accessible name, and a focusable keyboard
    // affordance (activation + popup arrow navigation), with no a11y diagnostics.
    let cases : (string * AccessibilityRole * Control<int>) list =
        [ "toggle-button",
          AccessibilityRole.Button,
          ToggleButton.view { ToggleButton.defaults with Text = "Bold"; IsOn = true; OnToggle = Some(fun _ -> 1) } |> Widget.toControl
          "split-button",
          AccessibilityRole.Menu,
          SplitButton.view
              { SplitButton.defaults with
                  Text = "Save"
                  IsOpen = true
                  Items = [ { Key = "cut"; Label = "Cut" } ]
                  OnSelected = Some(fun _ -> 1) }
          |> Widget.toControl
          "date-picker",
          AccessibilityRole.TextBox,
          DatePicker.view { DatePicker.defaults with Value = Some(DateOnly(2026, 6, 15)); IsOpen = true; OnChange = Some(fun _ -> 1) } |> Widget.toControl
          "time-picker",
          AccessibilityRole.TextBox,
          TimePicker.view { TimePicker.defaults with Value = Some(TimeOnly(10, 30)); OnChange = Some(fun _ -> 1) } |> Widget.toControl
          "color-picker",
          AccessibilityRole.List,
          ColorPicker.view
              { ColorPicker.defaults with
                  Swatches = [ { Name = "Red"; Color = { Red = 255uy; Green = 0uy; Blue = 0uy; Alpha = 255uy } } ]
                  OnSelected = Some(fun _ -> 1) }
          |> Widget.toControl ]

    testList "Feature 072 new-control accessibility (FR-009, SC-005)" [
        test "every new control carries its catalog role and a focusable keyboard affordance" {
            for name, role, control in cases do
                match control.Accessibility with
                | None -> failtestf "%s lowered tree carries no accessibility metadata" name
                | Some metadata ->
                    Expect.equal metadata.Role role $"{name} carries its catalog accessibility role"
                    Expect.isTrue metadata.Keyboard.Focusable $"{name} exposes a focusable trigger"
                    Expect.contains metadata.Keyboard.ActivationKeys "Enter" $"{name} activates with Enter"
                    Expect.contains metadata.Keyboard.ActivationKeys "Space" $"{name} activates with Space"
                    Expect.isNonEmpty metadata.Keyboard.NavigationKeys $"{name} declares keyboard navigation"
                    Expect.isEmpty
                        (Accessibility.validate control |> List.filter (fun d -> d.Severity = ControlDiagnosticSeverity.Error))
                        $"{name} has no accessibility diagnostics"
        }

        test "popup-bearing new controls navigate with arrow keys" {
            let navOf control =
                match (control: Control<int>).Accessibility with
                | Some m -> m.Keyboard.NavigationKeys
                | None -> []

            let byName = cases |> List.map (fun (n, _, c) -> n, c) |> Map.ofList
            Expect.contains (navOf byName.["date-picker"]) "ArrowDown" "date-picker calendar moves with arrows"
            Expect.contains (navOf byName.["split-button"]) "ArrowDown" "split-button menu moves with arrows"
            Expect.contains (navOf byName.["color-picker"]) "ArrowRight" "color-picker grid moves with arrows"
        }
    ]

[<Tests>]
let accessibilityTests =
    testList "Controls accessibility metadata" [
        test "interactive controls expose role name state focus keyboard and contrast metadata" {
            let metadata = Accessibility.defaultFor "button" "Save"
            Expect.equal metadata.Role AccessibilityRole.Button "button role is declared"
            Expect.equal metadata.NameSource "Save" "name source is available"
            Expect.isNonEmpty metadata.State "state metadata is present"
            Expect.isTrue metadata.Keyboard.Focusable "button is focusable"
            Expect.contains metadata.Keyboard.ActivationKeys "Enter" "keyboard operation is documented"
            Expect.isSome metadata.Contrast "contrast evidence is present"
        }

        test "missing accessibility and low contrast fail diagnostics" {
            let lowContrast =
                Accessibility.metadata AccessibilityRole.Button "Low" [ "normal" ] (Some 1) (Accessibility.keyboard true [ "Enter" ] [ "Tab" ]) (Some(Accessibility.contrast FS.GG.UI.Scene.Colors.black FS.GG.UI.Scene.Colors.black 1.0 4.5)) None

            let control = Button.create [ Button.text "Low"; Attr.accessibility lowContrast ]
            let diagnostics = Accessibility.validate control
            Expect.exists diagnostics (fun item -> item.Code = ContrastFailure) "contrast failure is reported"
        }
    ]
