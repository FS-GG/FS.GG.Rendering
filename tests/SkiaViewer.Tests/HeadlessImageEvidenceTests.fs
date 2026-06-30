module HeadlessImageEvidenceTests

// Feature 221 (Headless Image Evidence Path). Pre-fix, `SceneEvidence.renderPng` returned the UTF-8
// bytes of a structural HASH string for `Format = Png` — a tiny non-image stub, not a decodable PNG —
// so a headless CI agent could obtain no pixel proof of the game (live viewer presents direct-to-
// swapchain; the offscreen route needs GL). These tests pin the post-fix contract: with the SkiaViewer
// CPU rasterizer injected (no GPU/GL/X/display), `renderPng` yields a real, decodable, non-blank,
// byte-identical PNG (US1); with no rasterizer injected it returns a typed `UnsupportedEnvironment`
// failure and never a stub (US3). The seam is process-wide mutable, so the whole list is `testSequenced`
// and each test sets the seam state it needs; the focused `--filter "HeadlessImageEvidence"` lane runs
// these alone (no cross-file seam races).

open System
open System.Threading.Tasks
open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// ── T006: the canonical "representative game scene" fixture ──────────────────────────────────────
// A single named constructor at a FIXED size, so FR-003/SC-001/SC-002 rest on a pinned, reproducible
// input. Exercises geometry (background + token + HUD panel), colour, and bundled-font text (the
// headless font path). Identity recorded in specs/221-headless-image-evidence/readiness/fixture.md.
let representativeGameSceneSize: Size = { Width = 800; Height = 600 }

let representativeGameScene () : Scene =
    Scene.group
        [ Scene.rectangle (0.0, 0.0, 800.0, 600.0) (Colors.rgb 18uy 22uy 30uy)
          Scene.circle { X = 400.0; Y = 300.0 } 120.0 (Colors.rgb 220uy 80uy 60uy)
          Scene.rectangle (40.0, 40.0, 260.0, 64.0) (Colors.rgba 255uy 255uy 255uy 48uy)
          Scene.sizedText (56.0, 84.0) "HP 100 / SCORE 42" 24.0 Colors.white ]

// A deliberately distinct second pinned scene for the concurrency test, so interleaved renders that
// interfered would diverge from their own sequential baseline.
let secondaryScene () : Scene =
    Scene.group
        [ Scene.rectangle (0.0, 0.0, 800.0, 600.0) (Colors.rgb 12uy 40uy 24uy)
          Scene.circle { X = 200.0; Y = 200.0 } 80.0 (Colors.rgb 80uy 160uy 220uy)
          Scene.sizedText (40.0, 60.0) "WAVE 7" 32.0 Colors.white ]

/// Decode PNG bytes and report (decoded?, width, height, distinctPixelColours).
let private decodePng (bytes: byte[]) =
    use bitmap = SKBitmap.Decode bytes
    if isNull bitmap then
        false, 0, 0, 0
    else
        let distinct = bitmap.Pixels |> Array.distinct |> Array.length
        true, bitmap.Width, bitmap.Height, distinct

let private renderOnce scene =
    SceneEvidence.renderPng representativeGameSceneSize scene

[<Tests>]
let tests =
    testSequenced
    <| testList
        "HeadlessImageEvidence"
        [
          // ── T008 [US1]: determinism + dimensions + non-blank ──────────────────────────────────
          test "renderPng yields a decodable, correctly-sized, non-blank, deterministic PNG (FR-003/SC-001/SC-002)" {
              Text.installPngRasterizer ()
              let scene = representativeGameScene ()

              match renderOnce scene, renderOnce scene with
              | Result.Ok first, Result.Ok second ->
                  let decoded, w, h, distinct = decodePng first
                  Expect.isTrue decoded "first render must decode as a valid PNG"
                  Expect.equal w representativeGameSceneSize.Width "PNG width must equal the requested width"
                  Expect.equal h representativeGameSceneSize.Height "PNG height must equal the requested height"
                  Expect.isGreaterThan distinct 1 "PNG must be non-blank (more than one distinct pixel colour, i.e. real scene content, not a flat clear or a hash stub)"
                  Expect.isGreaterThan first.Length 64 "a real PNG is far larger than the prior ~64-byte hash stub (SC-005)"
                  Expect.equal first second "the same (scene, size) must render byte-for-byte identical bytes (FR-003)"
              | other -> failtestf "expected two Ok PNG renders, got %A" other
          }

          // ── T009 [US1]: cross-instance determinism (SC-002 "across machines" proxy) ────────────
          // Distinct from T008: two INDEPENDENTLY constructed scene instances must render identical
          // bytes — proves determinism rests on scene VALUE, not on a shared object identity / cache.
          test "renderPng is deterministic across independent scene instances (SC-002)" {
              Text.installPngRasterizer ()
              match renderOnce (representativeGameScene ()), renderOnce (representativeGameScene ()) with
              | Result.Ok a, Result.Ok b -> Expect.equal a b "two independently-built instances of the same scene must render identical bytes"
              | other -> failtestf "expected two Ok renders, got %A" other
          }

          // ── T010 [US1]: concurrency edge case ─────────────────────────────────────────────────
          // Several independent headless renders run concurrently; each result must equal its own
          // sequential baseline (no shared-state interference in the injected rasterizer) and the two
          // distinct scenes must not bleed into each other.
          test "concurrent renderPng calls are isolated and individually deterministic (Edge Case: concurrency)" {
              Text.installPngRasterizer ()
              let baselineA = renderOnce (representativeGameScene ())
              let baselineB = renderOnce (secondaryScene ())

              match baselineA, baselineB with
              | Result.Ok expectedA, Result.Ok expectedB ->
                  let jobs =
                      [ for i in 1..16 ->
                            let scene = if i % 2 = 0 then representativeGameScene () else secondaryScene ()
                            let expected = if i % 2 = 0 then expectedA else expectedB
                            Task.Run(fun () ->
                                match renderOnce scene with
                                | Result.Ok bytes -> bytes = expected
                                | Result.Error _ -> false) ]

                  let results = Task.WhenAll(jobs).GetAwaiter().GetResult()
                  Expect.isTrue (Array.forall id results) "every concurrent render must equal its sequential baseline (isolated + deterministic)"
              | other -> failtestf "expected Ok baselines, got %A" other
          }

          // ── T018 [US3]: honest failure when no rasterizer is injected (FR-005/SC-005) ──────────
          test "renderPng returns a typed UnsupportedEnvironment failure (never a stub) when no rasterizer is injected" {
              Text.clearPngRasterizer ()
              try
                  match renderOnce (representativeGameScene ()) with
                  | Result.Error failure ->
                      Expect.equal failure.Classification SceneEvidenceFailureClassification.UnsupportedEnvironment "no rasterizer ⇒ unsupported-environment classification"
                      Expect.equal failure.BlockedStage "renderer" "the blocked stage must name the renderer"
                      Expect.isFalse (String.IsNullOrWhiteSpace failure.Message) "the failure must carry a human-readable message"
                  | Result.Ok bytes -> failtestf "expected a typed failure, got %d bytes (success-shaped non-image is forbidden by SC-005)" bytes.Length
              finally
                  Text.installPngRasterizer ()
          }

          // ── T019 [US3]: size edge cases — ProductDefect on non-positive, never a stub on large ──
          test "renderPng rejects non-positive sizes as ProductDefect and never emits a stub for large sizes" {
              Text.installPngRasterizer ()
              for badSize in [ { Width = 0; Height = 600 }; { Width = 800; Height = 0 }; { Width = -1; Height = -1 } ] do
                  match SceneEvidence.renderPng badSize (representativeGameScene ()) with
                  | Result.Error failure -> Expect.equal failure.Classification SceneEvidenceFailureClassification.ProductDefect $"size {badSize.Width}x{badSize.Height} must classify as product-defect"
                  | Result.Ok bytes -> failtestf "non-positive size must fail, got %d bytes" bytes.Length

              // Very large size: succeeds within bounds with a real PNG, or fails typed — never a stub.
              match SceneEvidence.renderPng { Width = 2000; Height = 1500 } (representativeGameScene ()) with
              | Result.Ok bytes ->
                  let decoded, w, h, _ = decodePng bytes
                  Expect.isTrue decoded "large render that succeeds must be a valid PNG"
                  Expect.equal (w, h) (2000, 1500) "large render must honour the requested dimensions"
              | Result.Error failure -> Expect.isFalse (String.IsNullOrWhiteSpace failure.Message) "a large-size failure must be a typed diagnostic, never a stub"
          }

          // ── T022 [US3]: degradation is disclosed, not silently dropped ─────────────────────────
          // The CPU raster path shares the SAME exhaustive `SceneRenderer.paintNode` as the GL path, so
          // no SceneNode kind is silently dropped. The one deterministic degradation — a code point with
          // no bundled-font coverage — is DISCLOSED through the existing fallback channel rather than
          // drawn as a plausible-wrong glyph. Assert the disclosure fires for an unmapped glyph.
          test "headless render discloses bundled-font fallback rather than silently substituting (Edge Case: fonts)" {
              Text.installPngRasterizer ()
              Text.resetFallbackDisclosure ()
              // U+4E2D (中) is outside the bundled Latin faces → tofu/fallback, which must be disclosed.
              let scene = Scene.group [ Scene.sizedText (40.0, 60.0) "score 中" 28.0 Colors.white ]

              match renderOnce scene with
              | Result.Ok _ ->
                  let report = Text.fallbackReport ()
                  Expect.isGreaterThan (report.SubstitutedCount + report.TofuCount) 0 "an uncovered code point must be disclosed via the fallback report, not silently substituted"
              | other -> failtestf "expected a successful render with disclosed fallback, got %A" other
          }
        ]
