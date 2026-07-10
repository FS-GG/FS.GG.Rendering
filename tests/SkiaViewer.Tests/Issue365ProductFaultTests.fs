module Issue365ProductFaultTests

open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

// Issue #365. The frame-*render* path is resilient (issue #179: classified, bounded, torn down as a
// `frameLoopAbandoned` fatal). The *update/view* path was not: `GlHost.dispatch` folded `program.Update`
// and the interactive loop folded `host.Update`/`host.View` unguarded, so a single throwing product
// step escaped the Silk callback and was caught only by the run's outer handler — which tore the whole
// persistent window down and mislabeled it `frameRenderFailed`.
//
// The fix guards those folds with `tryProductStep`. A product-code fault is deterministic (retrying the
// same (msg, model) just re-throws), so the policy is drop-and-continue, not retry: the offending step
// is dropped, reported as an `App`-stage defect (never `frameRenderFailed`), and the window kept alive
// on its last-good state. The live `dispatch`/`dispatchHostMsg` loops are not drivable headless (the
// same limitation issue #179 records), so — exactly as issue #179's tests drive `observeFrameFailed` —
// these drive `tryProductStep` directly, which is the guard those loops call.

let private boom () : int = failwith "product blew up"

[<Tests>]
let tests =
    testList
        "product update/view faults are guarded, not fatal (issue #365)"
        [ testList
              "GlHost.tryProductStep"
              [ test "a succeeding step returns its result and reports nothing" {
                    let mutable reports = []
                    let result = GlHost.tryProductStep (fun d -> reports <- d :: reports) "Update" (fun () -> 41 + 1)

                    Expect.equal result (Some 42) "the step's result flows through untouched"
                    Expect.isEmpty reports "a healthy step produces no diagnostic"
                }

                test "a throwing step is dropped and reported as an App-stage Error — never frameRenderFailed" {
                    let mutable reports = []
                    let result = GlHost.tryProductStep (fun d -> reports <- d :: reports) "Update" boom

                    Expect.isNone result "the failing step is dropped so the caller keeps its last-good state"

                    match reports with
                    | [ diagnostic ] ->
                        Expect.equal diagnostic.Severity DiagnosticSeverity.Error "a product fault is an error, not fatal — the window lives"
                        Expect.equal diagnostic.Stage DiagnosticStage.App "an update/view fault is application code, not a draw failure"
                        Expect.notEqual diagnostic.Stage DiagnosticStage.FrameRender "the whole point of #365: it is NOT mislabeled a render failure"
                        Expect.equal diagnostic.Cause (Some "product blew up") "the underlying exception message survives into the cause"
                    | other -> failtestf "exactly one diagnostic should be reported, got %i" (List.length other)
                }

                test "the App-stage constructor carries the phase and the detail" {
                    let diagnostic = Diagnostics.productStepFailed "View" "null reference"

                    Expect.equal diagnostic.Severity DiagnosticSeverity.Error "a product fault is an error"
                    Expect.equal diagnostic.Stage DiagnosticStage.App "staged at App, not FrameRender"
                    Expect.stringContains diagnostic.Message "View" "the message names which product step raised"
                    Expect.equal diagnostic.Cause (Some "null reference") "the detail is preserved as the cause"
                } ]

          testList
              "Viewer.tryProductStep (the presented interactive host)"
              [ test "a succeeding step returns its result and reports nothing" {
                    let mutable reports = []
                    let result = Viewer.tryProductStep (fun d -> reports <- d :: reports) "View" (fun () -> "scene")

                    Expect.equal result (Some "scene") "the step's result flows through untouched"
                    Expect.isEmpty reports "a healthy step produces no diagnostic"
                }

                test "a throwing step is dropped and reported as an App-stage Error — never a Frame failure" {
                    let mutable reports = []
                    let result = Viewer.tryProductStep (fun d -> reports <- d :: reports) "Update" boom

                    Expect.isNone result "the failing input is dropped so the window keeps its last-good scene"

                    match reports with
                    | [ diagnostic ] ->
                        Expect.equal diagnostic.Level ViewerDiagnosticLevel.Error "a product fault is an error, not a window teardown"
                        Expect.equal diagnostic.Stage (Some ViewerRunBlockedStage.App) "an update/view fault is staged at App"
                        Expect.notEqual diagnostic.Category ViewerDiagnosticCategory.Frame "it must not read as a render/frame failure"
                        Expect.stringContains diagnostic.Message "Update" "the message names which product step raised"
                    | other -> failtestf "exactly one diagnostic should be reported, got %i" (List.length other)
                }

                test "the App-stage diagnostic surfaces under the default capture policy" {
                    // A dropped input the operator never sees is a silent failure; the diagnostic must pass
                    // `defaultDiagnostics` (Error level, Scene category is in the default set).
                    let diagnostic = Viewer.productDefectDiagnostic "View" "boom"

                    Expect.isTrue
                        (Viewer.shouldCaptureDiagnostic Viewer.defaultDiagnostics diagnostic)
                        "a product-defect diagnostic is captured by default, not filtered into silence"
                } ]

          test "the two hosts agree: neither reuses the frame-render stage for a product fault" {
              let gl = Diagnostics.productStepFailed "Update" "x"
              let interactive = Viewer.productDefectDiagnostic "Update" "x"

              Expect.equal gl.Stage DiagnosticStage.App "GL host: App, not FrameRender"
              Expect.equal interactive.Stage (Some ViewerRunBlockedStage.App) "interactive host: App, not a render stage"
          } ]
