module ControlsRenderingTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed

[<Tests>]
let typedGalleryRenderingTests =
    testList "Feature 071 typed gallery rendering (US2)" [
        // SC-004 / SC-005 / contract G5: the typed-authored gallery panel renders through
        // the existing IR path at >=2 viewports with no diagnostics, and covers >=1 control
        // from every required mechanic group (resolved via the catalog `Category` crosswalk).
        test "typed gallery panel renders at two viewports and covers every mechanic group (SC-004, SC-005, G5)" {
            let panel = ControlsTypedGalleryPanel.panel

            for width, height in [ 320, 240; 1024, 768 ] do
                let rendered = Control.render Theme.light panel
                let evidence = Scene.renderReadbackEvidence { Width = width; Height = height } rendered.Scene
                Expect.isEmpty rendered.Diagnostics $"typed gallery panel has no diagnostics at {width}x{height}"
                Expect.isNonEmpty evidence.DeterministicHash $"typed gallery panel render evidence has a deterministic hash at {width}x{height}"

            // The 8 gallery mechanic groups resolve onto these catalog categories
            // (contract crosswalk): display, input, layout, navigation, overlay, selection,
            // chart, graph. `data`/`feedback`/`custom` are not required groups.
            let required =
                Set.ofList [ "display"; "input"; "layout"; "navigation"; "overlay"; "selection"; "chart"; "graph" ]
            let covered = ControlsTypedGalleryPanel.coveredCategories panel

            Expect.isTrue
                (Set.isSubset required covered)
                $"typed gallery panel covers every required mechanic group; missing: {Set.difference required covered}"
        }
    ]

[<Tests>]
let expansionRenderingTests =
    // Feature 072 (T014, T026, SC-005): each new control renders through the existing
    // IR path at >=2 viewports with no diagnostics and a stable node count.
    let newControls : (string * Control<int>) list =
        [ "toggle-button",
          ToggleButton.view { ToggleButton.defaults with Text = "Bold"; IsOn = true; OnToggle = Some(fun _ -> 1) }
          |> Widget.toControl
          "split-button",
          SplitButton.view
              { SplitButton.defaults with
                  Text = "Save"
                  IsOpen = true
                  Items = [ { Key = "cut"; Label = "Cut" }; { Key = "copy"; Label = "Copy" } ]
                  OnClick = Some 1
                  OnSelected = Some(fun _ -> 2) }
          |> Widget.toControl
          "date-picker",
          DatePicker.view { DatePicker.defaults with Value = Some(DateOnly(2026, 6, 15)); IsOpen = true; OnChange = Some(fun _ -> 1) }
          |> Widget.toControl
          "time-picker",
          TimePicker.view { TimePicker.defaults with Value = Some(TimeOnly(10, 30)); OnChange = Some(fun _ -> 1) }
          |> Widget.toControl
          "color-picker",
          ColorPicker.view
              { ColorPicker.defaults with
                  Swatches =
                      [ { Name = "Red"; Color = { Red = 255uy; Green = 0uy; Blue = 0uy; Alpha = 255uy } }
                        { Name = "Blue"; Color = { Red = 0uy; Green = 0uy; Blue = 255uy; Alpha = 255uy } } ]
                  Selected = Some { Name = "Red"; Color = { Red = 255uy; Green = 0uy; Blue = 0uy; Alpha = 255uy } }
                  OnSelected = Some(fun _ -> 1) }
          |> Widget.toControl ]

    testList "Feature 072 new-control rendering (SC-005)" [
        test "every new control renders at two viewports with no diagnostics and a stable node count" {
            for name, control in newControls do
                let counts =
                    [ for width, height in [ 320, 240; 1024, 768 ] do
                          let rendered = Control.render Theme.light control
                          let evidence = Scene.renderReadbackEvidence { Width = width; Height = height } rendered.Scene
                          Expect.isEmpty rendered.Diagnostics $"{name} renders with no diagnostics at {width}x{height}"
                          Expect.isNonEmpty evidence.DeterministicHash $"{name} render evidence has a deterministic hash at {width}x{height}"
                          yield rendered.NodeCount ]

                Expect.allEqual counts (List.head counts) $"{name} node count is stable across viewports"
                Expect.isGreaterThanOrEqual (List.head counts) 1 $"{name} lowers to at least one node"
        }
    ]

[<Tests>]
let renderingTests =
    testList "Controls rendering and collections" [
        test "large data visible range stays bounded for ten thousand items" {
            let model, effects = Collections.init "orders" 10_000 24.0 240.0
            Expect.equal model.VisibleRange.Count 11 "visible range includes only the viewport plus one buffer row"
            Expect.equal effects [ VisibleRangeChanged model.VisibleRange ] "initial range effect is emitted"

            let scrolled, _ = Collections.update (ScrollTo(24.0 * 250.0)) model
            Expect.equal scrolled.VisibleRange.FirstIndex 250 "scroll offset maps to first visible row"
            Expect.isLessThan scrolled.VisibleRange.Count 30 "visible range remains bounded"
        }

        test "render output covers viewport sizes and scale factors without diagnostics" {
            let screen =
                Stack.create [
                    Stack.children [
                        TextBlock.create [ TextBlock.text "Catalog" ]
                        ProgressBar.create [ ProgressBar.value 0.4 ]
                        GraphView.create [ GraphView.nodes [ "a"; "b"; "c" ] ]
                    ]
                ]

            for width, height in [ 320, 240; 640, 480; 1024, 768 ] do
                for scale in [ 1.0; 2.0 ] do
                    let theme = Theme.light |> Theme.withDensity scale
                    let rendered = Control.render theme screen
                    let evidence = Scene.renderReadbackEvidence { Width = width; Height = height } rendered.Scene
                    Expect.isEmpty rendered.Diagnostics $"no rendering diagnostics at {width}x{height}@{scale}"
                    Expect.isNonEmpty evidence.DeterministicHash "render evidence has deterministic hash"
        }

        test "rich text reports unsupported Skia effect diagnostics during measurement" {
            let block =
                { RichText.block [ RichText.run "Hello" (RichText.defaultStyle Theme.light) ] with
                    MaxWidth = Some 32.0
                    Clip = true
                    Effects = [ "drop-shadow" ] }

            let measurement = RichText.measure block
            let rendered = Control.render Theme.light (RichText.create block [])
            let evidence = Scene.renderReadbackEvidence { Width = 160; Height = 90 } rendered.Scene

            Expect.isLessThanOrEqual measurement.Width 32.0 "measurement respects max width"
            Expect.exists measurement.Diagnostics (fun item -> item.Code = UnsupportedEnvironment && item.Message.Contains "drop-shadow") "unsupported effect is diagnosed"
            Expect.isNonEmpty evidence.DeterministicHash "rich text render produces readback evidence"
        }

        test "typed views render byte-for-byte identical to legacy IR at multiple viewports" {
            let typedScreen =
                FS.GG.UI.Controls.Typed.Stack.view
                    { FS.GG.UI.Controls.Typed.Stack.defaults with
                        Children =
                            [ FS.GG.UI.Controls.Typed.TextBlock.view
                                  { FS.GG.UI.Controls.Typed.TextBlock.defaults with Text = "Catalog" }
                              FS.GG.UI.Controls.Typed.Button.view
                                  { FS.GG.UI.Controls.Typed.Button.defaults with
                                      Text = "Save"
                                      OnClick = Some() } ] }
                |> Widget.toControl

            let legacyScreen =
                Stack.create
                    [ Attr.create "orientation" Layout (TextValue "vertical")
                      Attr.create "spacing" Layout (FloatValue 0.0)
                      Stack.children
                          [ TextBlock.create [ TextBlock.text "Catalog" ]
                            Button.create
                                [ Button.text "Save"
                                  Button.enabled true
                                  Attr.style "primary"
                                  Button.onClick () ] ] ]

            Expect.equal typedScreen.Accessibility legacyScreen.Accessibility "root accessibility metadata matches legacy"

            for width, height in [ 320, 240; 1024, 768 ] do
                let theme = Theme.light
                let typedRender = Control.render theme typedScreen
                let legacyRender = Control.render theme legacyScreen
                let typedEvidence = Scene.renderReadbackEvidence { Width = width; Height = height } typedRender.Scene
                let legacyEvidence = Scene.renderReadbackEvidence { Width = width; Height = height } legacyRender.Scene

                Expect.isEmpty typedRender.Diagnostics $"typed view has no diagnostics at {width}x{height}"
                Expect.equal typedRender.NodeCount legacyRender.NodeCount $"node count matches legacy at {width}x{height}"
                Expect.equal
                    typedEvidence.DeterministicHash
                    legacyEvidence.DeterministicHash
                    $"typed render hash equals legacy at {width}x{height}"
        }
    ]
