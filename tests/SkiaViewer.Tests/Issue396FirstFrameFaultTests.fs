module Issue396FirstFrameFaultTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Issue #396. #365 guarded every *runtime* product `Update`/`View` fold with `tryProductStep`, so a
// throwing step drops that input and keeps the persistent window alive on its last-good scene rather
// than tearing it down and mislabeling it `frameRenderFailed`. #365 deliberately scoped itself to the
// runtime dispatch paths, leaving the *first* product `View` — the startup frame in the persistent
// runners, and the single frame of the one-shot evidence/smoke helpers — unguarded. A product that
// throws on its very first `View` therefore escaped as an *uncaught* exception, not a classified
// `App`-stage `ProductDefect` the way #365 established every product-code fault should read.
//
// Unlike the runtime guard there is no last-good scene to fall back to, so a first-frame throw cannot
// be dropped: it fails the run as a startup `App`-stage `ProductDefect`. The two persistent runners
// gate on `runtimeCapability.PersistentWindow` (false headless) and are not drivable here — the same
// limitation #365 records for its live loops — but the one-shot helpers `runAppEvidence` and
// `GeneratedAppHost.smoke` reach the guard on the bounded/offscreen path, so these drive them directly:
// a product whose first `View` throws yields a typed `ProductDefect` failure, never a raw exception.

type private Model = { Frame: int }

let private white = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

/// A generated-app host whose very first `View` raises — the fault #396 must classify rather than let
/// escape. `Init`/`Update`/`MapKey`/`Tick` are all well-behaved; only the product's rendering blows up.
let private firstFrameFaultHost: GeneratedAppHost<Model, unit> =
    { Init = fun () -> { Frame = 0 }, []
      Update = fun _ model -> model, []
      View = fun _ -> failwith "product blew up on first View"
      MapKey = fun _ _ -> None
      Tick = fun _ -> None
      Diagnostics = Viewer.defaultDiagnostics }

/// The same host with a healthy `View`, to prove the guard is transparent when the product is fine.
let private healthyHost: GeneratedAppHost<Model, unit> =
    { firstFrameFaultHost with
        View = fun model -> Text((0.0, 0.0), $"frame {model.Frame}", white) }

let private evidenceOptions: ViewerOptions =
    { Title = "Product"
      InitialSize = { Width = 640; Height = 480 }
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None
      LogicalSize = None }

let private firstFrameRequest evidencePath : ViewerRunRequest =
    { Target = FirstFrame
      Timeout = TimeSpan.FromSeconds 2.0
      Diagnostics = Viewer.defaultDiagnostics
      RendererMode = "skia"
      EvidencePath = evidencePath }

[<Tests>]
let tests =
    testList
        "first-frame / one-shot product View faults are classified, not uncaught (issue #396)"
        [ testList
              "Viewer.runAppEvidence"
              [ test "a product that throws on its first View fails as an App-stage ProductDefect, not a crash" {
                    let evidencePath =
                        IO.Path.Combine(IO.Path.GetTempPath(), $"fs-gg-396-evidence-{Guid.NewGuid():N}.txt")

                    // The whole point of #396: this call RETURNS a typed failure instead of propagating
                    // the product's exception out of runAppEvidence.
                    match Viewer.runAppEvidence (firstFrameRequest (Some evidencePath)) evidenceOptions firstFrameFaultHost with
                    | Result.Ok _ -> failtest "a first-frame product throw must not read as a successful evidence run"
                    | Result.Error failure ->
                        Expect.equal failure.Classification ProductDefect "a first-frame product fault is a product defect, not an environment failure"
                        Expect.equal failure.BlockedStage ViewerRunBlockedStage.App "the fault is staged at App — application code, not a render/environment stage"
                        Expect.stringContains failure.Message "View" "the failure names which product step raised"
                        Expect.stringContains failure.Message "product blew up on first View" "the underlying exception message survives into the failure"

                    // Honesty: the startup failure must NOT reuse the runtime wording (#365's diagnostic
                    // claims the input was dropped and the window kept alive — neither is true here).
                    if IO.File.Exists evidencePath then
                        let evidenceText = IO.File.ReadAllText evidencePath
                        Expect.stringContains evidenceText "status=failed" "the serialized evidence records the failed run"
                        Expect.stringContains evidenceText "classification=ProductDefect" "the serialized evidence records the product-defect classification"
                        Expect.stringContains evidenceText "blocked-stage=App" "the serialized evidence records the App stage"
                }

                test "a healthy first View still runs the bounded evidence path to success (the guard is transparent)" {
                    match Viewer.runAppEvidence (firstFrameRequest None) evidenceOptions healthyHost with
                    | Result.Ok outcome -> Expect.equal outcome.Command (Some "runAppEvidence") "a well-behaved product is unaffected by the first-frame guard"
                    | Result.Error failure -> failtestf "a healthy product must not be blocked by the first-frame guard: %A" failure
                } ]

          testList
              "GeneratedAppHost.smoke"
              [ test "a product that throws on its first View fails as an App-stage ProductDefect, not a crash" {
                    // smoke lives outside module `Viewer` and guards inline through the same public #365
                    // seam; assert it reaches the identical classification.
                    match GeneratedAppHost.smoke firstFrameFaultHost (firstFrameRequest None) with
                    | Result.Ok _ -> failtest "a first-frame product throw must not read as a successful smoke run"
                    | Result.Error failure ->
                        Expect.equal failure.Classification ProductDefect "a first-frame product fault is a product defect, not an environment failure"
                        Expect.equal failure.BlockedStage ViewerRunBlockedStage.App "the fault is staged at App — application code, not a render/environment stage"
                        Expect.stringContains failure.Message "View" "the failure names which product step raised"
                        Expect.stringContains failure.Message "first frame" "the startup failure message is honest about the first-frame origin, not the runtime drop-and-continue wording"
                }

                test "a healthy first View still runs smoke to success (the guard is transparent)" {
                    match GeneratedAppHost.smoke healthyHost (firstFrameRequest None) with
                    | Result.Ok _ -> ()
                    | Result.Error failure -> failtestf "a healthy product must not be blocked by the first-frame guard: %A" failure
                } ] ]
