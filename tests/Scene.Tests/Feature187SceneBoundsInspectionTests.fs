module SceneCapability.Feature187SceneBoundsInspectionTests

open Expecto
open FS.GG.UI.Scene

let private viewport : Rect =
    { X = 0.0; Y = 0.0; Width = 200.0; Height = 100.0 }

let private knownBounds (row: SceneInspectionNode) =
    match row.Bounds with
    | SceneDrawableBounds.Known bounds -> bounds
    | other -> failtestf "expected known bounds for %s, got %A" row.Path other

[<Tests>]
let tests =
    testList "Feature187 deterministic scene bounds and hierarchy inspection" [
        test "MiniTank clipped authored text is an early structural red signal" {
            let clipped = Scene.sizedText (190.0, 20.0) "HUD" 20.0 Colors.white
            let rows = SceneInspection.inspect viewport clipped
            let text = rows |> List.exactlyOne

            Expect.equal text.Path "/nodes/0" "stable authored path"
            Expect.equal text.Kind SizedTextElement "text kind"
            Expect.equal text.ViewportRelation SceneViewportRelation.PartiallyOutside "clipped text is detected"
            Expect.equal (SceneInspection.outsideViewport rows |> List.map _.Path) [ "/nodes/0" ] "outside query"
            Expect.isGreaterThan (knownBounds text).Width 10.0 "text uses deterministic font metrics"

            let corrected = Scene.sizedText (150.0, 20.0) "HUD" 20.0 Colors.white
            let correctedText = SceneInspection.inspect viewport corrected |> List.exactlyOne
            Expect.equal correctedText.ViewportRelation SceneViewportRelation.Inside "policy correction clears the red signal"
        }

        test "Sojourn background subtree contributions remain attributable by authored hierarchy" {
            let background =
                Scene.group [
                    Scene.filledRectangle
                        { X = 0.0; Y = 0.0; Width = 200.0; Height = 100.0 }
                        (Colors.rgb 5uy 10uy 20uy)
                    Scene.textAt { X = 12.0; Y = 24.0 } "orbit guide" Colors.white
                ]
            let deepScreen =
                Scene.group [
                    background
                    Scene.filledRectangle
                        { X = 20.0; Y = 10.0; Width = 160.0; Height = 80.0 }
                        (Colors.rgb 30uy 40uy 50uy)
                ]

            let rows = SceneInspection.inspect viewport deepScreen
            let backgroundPath = "/nodes/0/group/0/nodes/0"
            let contributions = SceneInspection.contributingDescendants backgroundPath rows

            Expect.equal
                (contributions |> List.map _.Path)
                [ backgroundPath
                  backgroundPath + "/group/0/nodes/0"
                  backgroundPath + "/group/1/nodes/0" ]
                "the excluded/background subtree and both drawable descendants are visible"

            let corrected =
                Scene.group [
                    // Keep the authored page slot stable while removing its background contribution.
                    Scene.empty
                    Scene.filledRectangle
                        { X = 20.0; Y = 10.0; Width = 160.0; Height = 80.0 }
                        (Colors.rgb 30uy 40uy 50uy)
                ]
            let correctedRows = SceneInspection.inspect viewport corrected
            Expect.isEmpty
                (SceneInspection.contributingDescendants backgroundPath correctedRows)
                "omitting the background subtree gives a deterministic absence proof"
        }

        test "translation, perspective, clip, shaped text, and typed unknowns are explicit" {
            let affine : PerspectiveTransform =
                { M11 = 1.0; M12 = 0.0; M13 = 5.0
                  M21 = 0.0; M22 = 1.0; M23 = 7.0
                  M31 = 0.0; M32 = 0.0; M33 = 1.0 }
            let shaped =
                Scene.buildGlyphRun "abc" { Family = None; Size = 16.0; Weight = None }
                |> fun data -> Scene.glyphRun { X = 1.0; Y = 20.0 } data (Paint.fill Colors.white)
            let scene =
                Scene.group [
                    Scene.translate 10.0 2.0 (
                        Scene.withPerspective affine (
                            Scene.clipped
                                (RectClip { X = 0.0; Y = 0.0; Width = 20.0; Height = 30.0 })
                                shaped))
                    { Nodes = [ SceneNode.Path(Path.create Winding [], Paint.fill Colors.white) ] }
                    Scene.line
                        { X = 40.0; Y = 10.0 }
                        { X = 40.0; Y = 50.0 }
                        (Paint.fill Colors.white)
                    Scene.withPerspective
                        { affine with M13 = 0.0; M23 = 0.0; M31 = 1.0; M33 = -5.0 }
                        (Scene.filledRectangle
                            { X = 0.0; Y = 0.0; Width = 10.0; Height = 10.0 }
                            Colors.white)
                ]

            let rows = SceneInspection.inspect viewport scene
            let glyph =
                rows |> List.find (fun row -> row.Kind = GlyphRunElement)
            let glyphBounds = knownBounds glyph
            Expect.floatClose Accuracy.high glyphBounds.X 16.0 "translation and affine matrix compose"
            Expect.isLessThanOrEqual glyphBounds.Width 20.0 "effective bound is intersected with clip"

            let emptyPath =
                rows |> List.find (fun row -> row.Kind = PathElement)
            Expect.equal
                emptyPath.Bounds
                (SceneDrawableBounds.Unknown SceneBoundsUnknownReason.EmptyGeometry)
                "unsupported/empty geometry reports typed unknown rather than false-safe empty bounds"
            Expect.equal emptyPath.ViewportRelation SceneViewportRelation.Unknown "unknown stays unknown at viewport layer"

            let verticalLine = rows |> List.find (fun row -> row.Kind = LineElement)
            Expect.isTrue verticalLine.Contributes "a zero-width line still has effective stroke bounds"
            Expect.isGreaterThan (knownBounds verticalLine).Width 0.0 "default line stroke is represented"

            let horizonRectangle =
                rows
                |> List.find (fun row ->
                    row.Kind = RectangleElement
                    && row.Path.Contains("/perspective/"))
            Expect.equal
                horizonRectangle.Bounds
                (SceneDrawableBounds.Unknown SceneBoundsUnknownReason.PerspectiveHorizon)
                "a projective horizon crossing reports typed unknown instead of a finite corner box"
        }

        test "paint effects and joins conservatively expose viewport bleed" {
            let bounds = { X = 180.0; Y = 30.0; Width = 10.0; Height = 20.0 }
            let blurred =
                Paint.fill Colors.white
                |> Paint.withMaskFilter (Blur 4.0)
                |> Scene.rectangleWithPaint bounds
            let shadowed =
                Paint.fill Colors.white
                |> Paint.withImageFilter (DropShadow(25.0, 0.0, 0.0, Colors.black))
                |> Scene.rectangleWithPaint { bounds with X = 170.0 }
            let mitered =
                Paint.stroke Colors.white 8.0
                |> Paint.withMiter 4.0
                |> Scene.path
                    (Path.create Winding
                        [ Path.moveTo 180.0 20.0
                          Path.lineTo 195.0 30.0
                          Path.lineTo 180.0 40.0 ])
            let discrete =
                Paint.stroke Colors.white 2.0
                |> Paint.withPathEffect (Discrete(4.0, 10.0))
                |> Scene.path
                    (Path.create Winding
                        [ Path.moveTo 180.0 55.0
                          Path.lineTo 195.0 55.0 ])

            let rows =
                Scene.group [ blurred; shadowed; mitered; discrete ]
                |> SceneInspection.inspect viewport

            for kind, fragment in
                [ RectangleElement, "/group/0/"
                  RectangleElement, "/group/1/"
                  PathElement, "/group/2/"
                  PathElement, "/group/3/" ] do
                let row =
                    rows
                    |> List.find (fun row -> row.Kind = kind && row.Path.Contains fragment)
                Expect.equal
                    row.ViewportRelation
                    SceneViewportRelation.PartiallyOutside
                    $"{fragment} paint extent must not be reported false-safe inside"
                Expect.isGreaterThan
                    ((knownBounds row).X + (knownBounds row).Width)
                    viewport.Width
                    $"{fragment} effective bound reaches beyond the viewport"
        }

        test "chart bounds union only bars that the renderer actually draws" {
            let chart = Scene.chart [ 1.0; 0.0; -2.0 ]
            let row = SceneInspection.inspect { viewport with Width = 300.0; Height = 500.0 } chart |> List.exactlyOne
            let bounds = knownBounds row

            Expect.floatClose Accuracy.high bounds.X 32.0 "first positive bar sets the left edge"
            Expect.floatClose Accuracy.high bounds.Width 32.0 "trailing zero/negative values add no pixels"
            Expect.floatClose Accuracy.high bounds.Y 180.0 "the maximum positive bar reaches chart top"
            Expect.floatClose Accuracy.high bounds.Height 220.0 "the maximum positive bar reaches chart bottom"

            let shifted =
                Scene.chart [ 0.0; 2.0; 0.0 ]
                |> SceneInspection.inspect { viewport with Width = 300.0; Height = 500.0 }
                |> List.exactlyOne
                |> knownBounds
            Expect.floatClose Accuracy.high shifted.X 76.0 "a leading zero does not create a drawable bar"
            Expect.floatClose Accuracy.high shifted.Width 32.0 "only the positive bar contributes"

            let empty = Scene.chart [ 0.0; -1.0 ] |> SceneInspection.inspect viewport |> List.exactlyOne
            Expect.equal empty.Bounds SceneDrawableBounds.NoDrawableContent "non-positive charts draw nothing"
        }
    ]
