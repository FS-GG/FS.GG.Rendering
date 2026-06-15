module Feature122PresentPathTests

// Feature 122 (US1, FR-001/002) — the live DirectToSwapchain present must never leave a swapchain
// buffer undrawn on a static scene (the interleaved-black blink the Spread3 consumer reported on
// Wayland windowed-fullscreen). The pure `GlHost.planPresent` decision is exposed (like 120's
// `shouldPresent` / 121's `shouldAdvanceFrame`); these assert the decision + the host's bounded
// buffer-fill state machine. The actual Wayland windowed-fullscreen visual blink is not reproducible
// headless (recorded in readiness/runtime-limitations.md), so the real evidence is this pure model
// plus the unchanged offscreen/readback goldens (the readback path is untouched).
//
// FR-003/005 parity (T018): the controls overload `runInteractiveAppWithWindowBehavior` with the
// default behavior reduces, BY CONSTRUCTION, to the same viewer call as `runInteractiveApp` —
// `runInteractiveViewer o h = runInteractiveViewerWithWindowBehavior o defaultWindowBehavior h`
// (SkiaViewer.fs) — so the default path is byte-identical; the only observable here is that
// `defaultWindowBehavior` is the windowed-fullscreen default the no-flag path uses.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

// Mirrors the (private) GlHost.bufferFillDepth — 3 covers typical triple-buffering.
let private bufferFillDepth = 3

let private sceneA: Scene = { Nodes = [] }
let private sceneB: Scene = { Nodes = [ Group [] ] }

/// Drive planPresent exactly as GlHost.renderFrame does: reset the idle window on a real paint,
/// decrement on a re-present, hold on a full skip. Returns the per-frame action sequence.
let private simulate (scenes: Scene list) : GlHost.PresentAction list =
    let mutable prev: Scene option = None
    let mutable remaining = 0
    let actions = ResizeArray<GlHost.PresentAction>()

    for scene in scenes do
        let action = GlHost.planPresent prev scene false remaining

        match action with
        | GlHost.PresentAction.PaintAndPresent ->
            prev <- Some scene
            remaining <- bufferFillDepth - 1
        | GlHost.PresentAction.RepresentLastGood -> remaining <- remaining - 1
        | GlHost.PresentAction.SkipPresent -> ()

        actions.Add action

    List.ofSeq actions

[<Tests>]
let tests =
    testList
        "Feature 122 present path (US1, FR-001/002)"
        [ test "planPresent paints on change, re-presents to fill buffers, then idles (SC-001)" {
              // One paint then a static run: paint fills buffer 1, two re-presents fill buffers 2 & 3,
              // then full idle. No undrawn buffer is ever presented.
              let actual = simulate [ sceneB; sceneB; sceneB; sceneB; sceneB ]

              Expect.equal
                  actual
                  [ GlHost.PresentAction.PaintAndPresent
                    GlHost.PresentAction.RepresentLastGood
                    GlHost.PresentAction.RepresentLastGood
                    GlHost.PresentAction.SkipPresent
                    GlHost.PresentAction.SkipPresent ]
                  "paint → represent×(depth-1) → skip…"
          }

          test "a scene change resets the buffer-fill window (FR-001)" {
              let actual = simulate [ sceneA; sceneA; sceneB; sceneB; sceneB; sceneB ]

              Expect.equal
                  actual
                  [ GlHost.PresentAction.PaintAndPresent // first frame
                    GlHost.PresentAction.RepresentLastGood // static
                    GlHost.PresentAction.PaintAndPresent // changed → repaint, window resets
                    GlHost.PresentAction.RepresentLastGood
                    GlHost.PresentAction.RepresentLastGood
                    GlHost.PresentAction.SkipPresent ]
                  "a change re-opens the fill window before idling again"
          }

          test "no buffer is left undrawn: depth presents fill before the first skip (SC-001)" {
              // The load-bearing invariant: after a change, at least `bufferFillDepth` buffer-filling
              // presents (paint + re-presents) occur before any SkipPresent — so every buffer in a
              // depth-deep rotation holds the frame.
              let actual = simulate (List.replicate 10 sceneB)
              let fillingBeforeFirstSkip =
                  actual
                  |> List.takeWhile (fun a -> a <> GlHost.PresentAction.SkipPresent)
                  |> List.length

              Expect.isGreaterThanOrEqual
                  fillingBeforeFirstSkip
                  bufferFillDepth
                  "every swapchain buffer is populated before the host idles"
          }

          test "idle preserved: a long static run reaches full SkipPresent (FR-002)" {
              let actual = simulate (List.replicate 8 sceneB)
              Expect.equal (List.last actual) GlHost.PresentAction.SkipPresent "steady-state is a full idle skip"
          }

          test "continuous animation never re-presents or skips (FR-002)" {
              // Every frame differs → every frame is a real paint; the bounded re-present never triggers.
              let alternating = [ for i in 0..7 -> if i % 2 = 0 then sceneA else sceneB ]
              let actual = simulate alternating

              Expect.isTrue
                  (actual |> List.forall (fun a -> a = GlHost.PresentAction.PaintAndPresent))
                  "a changing scene paints every frame"
          }

          test "a framebuffer size change forces a repaint regardless of idle window (FR-001)" {
              Expect.equal
                  (GlHost.planPresent (Some sceneB) sceneB true 0)
                  GlHost.PresentAction.PaintAndPresent
                  "a size change always repaints, never re-presents a stale frame"
          }

          test "default window behavior is windowed-fullscreen — controls overload parity (FR-003/005)" {
              // runInteractiveApp ≡ runInteractiveAppWithWindowBehavior _ defaultWindowBehavior _ by
              // construction (both reduce to runInteractiveViewerWithWindowBehavior _ defaultWindowBehavior _);
              // the observable default is the windowed-fullscreen startup state.
              Expect.equal
                  Viewer.defaultWindowBehavior.StartupState
                  ViewerWindowStartupState.WindowedFullscreen
                  "the no-flag default path uses windowed-fullscreen"
          } ]
