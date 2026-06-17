module Feature136TextRenderingTests

// Feature 136 (US1): the bundled-font text renderer. Pre-fix, `drawTextWithFallback` routed whole
// strings to a 5×7 bitmap whenever the host's `SKTypeface.Default` lacked a glyph (in the headless
// sandbox: always — see research.md R1), uppercasing everything and drawing unmapped chars as a `7`-ish
// wildcard. These tests assert the post-fix contract: `@` renders as `@` (never `7`), mixed case is
// preserved, decoratives are authored-or-deliberately-substituted, renders are deterministic, and every
// fallback/tofu is disclosed with no plausible-wrong-glyph ever produced.

open System
open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private font: FontSpec = { Family = None; Size = 16.0; Weight = None }

let private renderToPngBytes (w: int) (h: int) (scene: SceneNode) =
    let path = IO.Path.Combine(IO.Path.GetTempPath(), sprintf "fs136-%s.png" (Guid.NewGuid().ToString("N")))

    let request: ScreenshotEvidenceRequest =
        { Command = "screenshot"
          AppOrSample = "feature136"
          OutputPath = path
          Width = w
          Height = h
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = []
          Timeout = TimeSpan.FromSeconds 5.0 }

    let options: ViewerOptions =
        { Title = "feature136"
          InitialSize = { Width = w; Height = h }
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }

    Viewer.captureScreenshotEvidence request options scene |> ignore
    let bytes = if IO.File.Exists path then IO.File.ReadAllBytes path else [||]
    bytes

let private textScene (text: string) =
    (Scene.textRun
        { Text = text
          Position = { X = 20.0; Y = 40.0 }
          Font = font
          Paint = Paint.fill (Colors.rgb 20uy 20uy 20uy) })
        .Nodes
    |> List.head

[<Tests>]
let tests =
    testList
        "Feature136 text rendering (US1)"
        [ // T007
          test "@ renders as @ (not 7), mixed case preserved, decoratives authored-or-deliberate" {
              let at = Fonts.resolveText font "ada@example.com" |> List.find (fun rc -> rc.Original = '@')

              match at.Resolution with
              | Fonts.FallbackResolution.Authored _ -> ()
              | other -> failtestf "@ must be authored, was %A" other

              Expect.equal at.Rendered '@' "@ renders as @, never the 7-wildcard"

              let stable = Fonts.resolveText font "Stable" |> List.map (fun r -> r.Rendered) |> String.Concat
              Expect.equal stable "Stable" "mixed case preserved (not STABLE)"

              for c in [ '#'; '—'; '▸'; '·' ] do
                  let rc = Fonts.resolveText font (string c) |> List.head

                  match rc.Resolution with
                  | Fonts.FallbackResolution.Authored _ -> Expect.equal rc.Rendered c (sprintf "%c authored as itself" c)
                  | Fonts.FallbackResolution.Substituted(o, _, _) -> Expect.equal o c (sprintf "%c deliberately substituted" c)
                  | Fonts.FallbackResolution.Tofu _ -> failtestf "%c must not be tofu (the bundled chain covers it)" c
          }

          // T009
          test "two same-seed headless text renders are byte-identical (SC-005)" {
              let scene = textScene "ada@example.com Stable —#▸·"
              let b1 = renderToPngBytes 360 80 scene
              let b2 = renderToPngBytes 360 80 scene
              Expect.isGreaterThan b1.Length 0 "rendered PNG is non-empty"
              Expect.equal b1 b2 "byte-identical across two same-seed renders (host-independent fonts)"
          }

          // T010 — sequenced because it reads the process-wide disclosure accumulator after a render.
          testSequenced
          <| test "tofu disclosed; no plausible-wrong glyph is ever produced (FR-001)" {
              let tofu = Char.ConvertFromUtf32 0x4E00 // 一 — no bundled coverage in any face
              let resolved = Fonts.resolveText font (sprintf "A%sB" tofu)

              match resolved.[1].Resolution with
              | Fonts.FallbackResolution.Tofu o -> Expect.equal o tofu.[0] "uncovered char disclosed as tofu"
              | other -> failtestf "uncovered char must be tofu, was %A" other

              // Invariant: only Authored/Substituted/Tofu, and the rendered glyph is the original, a
              // deliberate substitute, or (tofu) the original — never an unrelated plausible glyph.
              for rc in Fonts.resolveText font "ada@example.com Stable —#▸·" do
                  match rc.Resolution with
                  | Fonts.FallbackResolution.Authored _ -> Expect.equal rc.Rendered rc.Original "authored renders the original"
                  | Fonts.FallbackResolution.Substituted(o, s, _) ->
                      Expect.equal rc.Rendered s "substituted renders the deliberate substitute"
                      Expect.equal o rc.Original "substituted records the original"
                  | Fonts.FallbackResolution.Tofu o -> Expect.equal o rc.Original "tofu records the original"

              // Disclosure surfaced through SkiaViewer after a render that contains the tofu char.
              renderToPngBytes 140 50 (textScene (sprintf "x%sy" tofu)) |> ignore
              let report = Text.fallbackReport ()
              Expect.isTrue (report.TofuCount >= 1) "tofu disclosed in the per-page report"
              Expect.isNonEmpty (Text.fallbackDiagnostics ()) "structured fallback diagnostics emitted"
          } ]
