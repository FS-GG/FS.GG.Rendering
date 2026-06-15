module Feature121LiveHostPacingTests

// Feature 121 (US1, FR-001/002/003) — the live persistent loop is consumer-paceable. The pure
// `GlHost.shouldAdvanceFrame` pacing decision (reached via InternalsVisibleTo, like 120's
// `shouldPresent`) gates BOTH update and present cadence, and a non-positive `ViewerOptions.FrameRateCap`
// is rejected at option validation. The persistent window itself is not drivable headless (recorded in
// readiness/runtime-limitations.md); these assert the extracted pure decision + the validation seam.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

let private options cap : ViewerOptions =
    { Title = "Product"
      InitialSize = { Width = 320; Height = 200 }
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = cap }

[<Tests>]
let tests =
    testList
        "Feature 121 live host pacing (US1, FR-001/002/003)"
        [ test "shouldAdvanceFrame advances only once the frame interval has elapsed (SC-001)" {
              let interval = 1.0 / 60.0
              Expect.isFalse (GlHost.shouldAdvanceFrame 0.0 (interval / 2.0) interval) "before the interval, no advance"
              Expect.isTrue (GlHost.shouldAdvanceFrame 0.0 interval interval) "at the interval boundary, advance"
              Expect.isTrue (GlHost.shouldAdvanceFrame 0.0 (interval * 2.0) interval) "past the interval, advance"
          }

          test "a tighter frame cap yields strictly fewer advances over the same window (FR-002/SC-001)" {
              // Simulate a 1s wall window polled every 1ms; count advances gated by shouldAdvanceFrame.
              // This is exactly how runEventLoop now gates DoUpdate+DoRender, so it bounds render cadence.
              let advancesAtCap cap =
                  let interval = 1.0 / float cap
                  let mutable last = 0.0
                  let mutable count = 0
                  let mutable t = 0.0

                  while t <= 1.0 do
                      if GlHost.shouldAdvanceFrame last t interval then
                          last <- t
                          count <- count + 1

                      t <- t + 0.001

                  count

              let at30 = advancesAtCap 30
              let at60 = advancesAtCap 60
              // The load-bearing FR-002 claim: a tighter cap advances strictly fewer times. The exact
              // counts drift a little with 1ms-step float accumulation, so the per-cap bands are generous.
              Expect.isTrue (at30 < at60) (sprintf "a 30 FPS cap advances strictly fewer times than 60 (got %d vs %d)" at30 at60)
              Expect.isTrue (at30 >= 27 && at30 <= 32) (sprintf "~30 advances/sec at cap 30, got %d" at30)
              Expect.isTrue (at60 >= 55 && at60 <= 62) (sprintf "~60 advances/sec at cap 60, got %d" at60)
          }

          test "a non-positive FrameRateCap is rejected at option validation (SC-005/FR-003)" {
              let scene = Group []

              match Viewer.runUntilFirstFrame (options (Some 0)) scene with
              | Result.Error failure ->
                  Expect.equal failure.Classification ProductDefect "a zero cap is a product defect"
                  Expect.stringContains failure.Message "frame-rate cap" "the diagnostic names the frame-rate cap"
              | Result.Ok evidence -> failtestf "expected a frame-rate-cap validation failure, got %A" evidence

              match Viewer.runUntilFirstFrame (options (Some -5)) scene with
              | Result.Error failure -> Expect.stringContains failure.Message "frame-rate cap" "a negative cap is rejected too"
              | Result.Ok evidence -> failtestf "expected a frame-rate-cap validation failure, got %A" evidence
          }

          test "a positive FrameRateCap clears option validation (FR-001)" {
              // With a valid cap, option validation passes and the frame-count (0) is what fails — proving
              // the cap was accepted, not rejected, and that omitting/setting it is otherwise inert.
              let scene = Group []

              match Viewer.runForFrames 0 (options (Some 30)) scene with
              | Result.Error failure ->
                  Expect.stringContains failure.Message "frame count" "a positive cap clears option validation; frame-count is the failure"
              | Result.Ok evidence -> failtestf "expected a frame-count failure, got %A" evidence
          } ]
