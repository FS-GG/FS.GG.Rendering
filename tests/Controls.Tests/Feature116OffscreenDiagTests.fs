module Feature116OffscreenDiagTests

// Feature 116 (US4, FR-011, SC-005) — the advisory offscreen-effect diagnostic. The pure detector
// `RetainedRender.offscreenEffect` and the wired emission on `RetainedRender.step`'s `Diagnostics`
// channel are reached through `[<assembly: InternalsVisibleTo("Controls.Tests")>]`. A control whose
// paint requires offscreen composition (a drop-shadow/image-filter, a `PathClip`, or a non-opaque
// paint over a multi-node group) is flagged with an advisory `OffscreenComposition` diagnostic naming
// the control + effect; a plain control is silent; in BOTH cases rendered output is byte-identical to
// the pre-feature state (advisory only, never alters paint). A `RectClip` (the ubiquitous cheap label
// clip) is intentionally NOT flagged. Render-only / deterministic ([[fs-gg-evidence-mode]]).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private rinit (t: Theme) (s: Size) (c: Control<'msg>) : RetainedRender<'msg> =
    (RetainedRender.init t s c).Retained

let private rect (x: float) (paint: Paint) : SceneNode = PaintedRectangle({ X = x; Y = 0.0; Width = 10.0; Height = 10.0 }, paint)
let private opaque = Paint.fill Colors.black
let private translucent = { Paint.fill Colors.black with Opacity = 0.4 }
let private shadowed = { Paint.fill Colors.black with ImageFilter = DropShadow(2.0, 2.0, 4.0, Colors.black) }
let private scene (nodes: SceneNode list) : Scene list = [ { Nodes = nodes } ]

[<Tests>]
let tests =
    testList "Feature 116 offscreen-effect diagnostic (US4, FR-011, SC-005)" [

        // ---- the pure detector (precise per-effect coverage) ------------------------------------

        test "detector: a drop-shadow / image-filter forces offscreen (FR-011)" {
            Expect.equal (RetainedRender.offscreenEffect (scene [ rect 0.0 shadowed ])) (Some "drop-shadow") "a drop-shadow is offscreen-forcing"
        }

        test "detector: a PathClip forces offscreen (FR-011)" {
            let clipped = ClipNode(PathClip(Path.create Winding [ Path.moveTo 0.0 0.0; Path.lineTo 5.0 5.0; Path.close ]), { Nodes = [ rect 0.0 opaque ] })
            Expect.equal (RetainedRender.offscreenEffect (scene [ clipped ])) (Some "path clip") "a PathClip is offscreen-forcing"
        }

        test "detector: a non-opaque paint over a multi-node group forces offscreen (FR-011)" {
            Expect.equal (RetainedRender.offscreenEffect (scene [ rect 0.0 translucent; rect 20.0 opaque ])) (Some "opacity group") "a low-opacity paint over >1 node is offscreen-forcing"
        }

        test "detector: a plain opaque single-node scene is silent (FR-011, SC-005)" {
            Expect.equal (RetainedRender.offscreenEffect (scene [ rect 0.0 opaque ])) None "a plain opaque scene forces no offscreen composition"
        }

        test "detector: a RectClip is the cheap label clip — NOT flagged" {
            let clipped = ClipNode(RectClip { X = 0.0; Y = 0.0; Width = 10.0; Height = 10.0 }, { Nodes = [ rect 0.0 opaque ] })
            Expect.equal (RetainedRender.offscreenEffect (scene [ clipped ])) None "a RectClip lowers to canvas.ClipRect (no layer) → not flagged"
        }

        // ---- wired through the step Diagnostics channel -----------------------------------------

        test "a control forcing offscreen composition surfaces an advisory diagnostic via step (FR-011, SC-005)" {
            // a line-chart with data paints a translucent area fill (opacity 0.22) over its line + dots
            // → an offscreen-forcing opacity group.
            let chart: Control<int> =
                LineChart.create
                    [ LineChart.series
                          [ { Name = "trend"
                              Points =
                                [ { X = 0.0; Y = 1.0; Label = None }
                                  { X = 1.0; Y = 3.0; Label = None }
                                  { X = 2.0; Y = 2.0; Label = None } ] } ] ]
                |> Control.withKey "chart"

            let r0 = rinit theme size chart
            let s = RetainedRender.step theme size r0 chart

            let offscreen = s.Diagnostics |> List.filter (fun d -> d.Code = OffscreenComposition)
            Expect.isNonEmpty offscreen "the offscreen-forcing chart is flagged"
            Expect.all offscreen (fun d -> d.Severity = ControlDiagnosticSeverity.Info) "the diagnostic is advisory (Info), never build-failing"

            // advisory only: the rendered output is byte-identical to a fresh full rebuild.
            let full = Control.renderTree theme size chart
            Expect.equal s.Render.Scene full.Scene "the diagnostic never alters rendered output"
        }

        test "a plain control surfaces NO offscreen diagnostic and renders identically (FR-011, SC-005)" {
            let plain: Control<int> =
                { Kind = "stack"
                  Key = None
                  Attributes = []
                  Children =
                    [ { Kind = "text-block"
                        Key = Some "t"
                        Attributes = [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 100.0 }; { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 } ]
                        Children = []
                        Content = Some "hello"
                        Accessibility = None } ]
                  Content = None
                  Accessibility = None }

            let r0 = rinit theme size plain
            let s = RetainedRender.step theme size r0 plain

            Expect.isEmpty (s.Diagnostics |> List.filter (fun d -> d.Code = OffscreenComposition)) "a plain control forces no offscreen composition"

            let full = Control.renderTree theme size plain
            Expect.equal s.Render.Scene full.Scene "output byte-identical (advisory only)"
        }
    ]
