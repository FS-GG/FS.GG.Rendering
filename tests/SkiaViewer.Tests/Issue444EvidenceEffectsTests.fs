module Issue444EvidenceEffectsTests

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Issue #444 — the highest-severity member of the silent-no-op family (.github#416), and the purest:
// it compiled, ran, reported success, and did nothing.
//
// A product's `Update` emits `CaptureScreenshot` / `CaptureImageEvidence` / `WriteVisualEvidence` /
// `WriteRunEvidence`. The persistent launch loops' effect fold pattern-matched all four into the
// DISCARD group next to the window-lifecycle effects: no file was written, no error was raised, and
// the run reported success. Compose that with SDD#349 — the lifecycle never opens the `artifacts:`
// path it records — and you get a green, ship-ready verdict with nothing whatsoever behind it. Two
// fail-open holes chaining into a claim that cannot be falsified end to end.
//
// The fix HONORS the effects rather than merely announcing the drop: the viewer already owned every
// writer (they served the bounded path), so the generated-product path can simply use them.
//
// WHERE THESE TESTS AIM. The live persistent runners gate on `runtimeCapability.PersistentWindow`
// (false headless) and are not drivable here — the same limitation #365/#396/#429 record for their
// loops. So they aim at the two `internal` seams the loops are built from, which is the exact code
// that used to discard the evidence: `Viewer.interpretViewerEffects` (does the effect REACH a sink?)
// and `Viewer.productEvidenceSink` (does the sink WRITE, and does a failed write SAY SO?).
//
// The failure leg is asserted on a reason string, per #266: a fix whose failure leg is untested is
// exactly how this class of defect survives a green suite.

let private white = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }
let private scene = Text((0.0, 0.0), "evidence", white)
let private size: Size = { Width = 64; Height = 32 }

let private runEvidence: ViewerRunEvidence =
    { FramesRendered = 7
      Elapsed = TimeSpan.FromMilliseconds 250.0
      InitialOutputSize = size
      RendererMode = "cpu"
      LastDiagnosticSummary = Some "all good"
      EvidencePath = None }

let private visualArtifact: ViewerVisualEvidenceArtifact =
    { Kind = MetadataHash
      Path = Some "subject.png"
      ImageDecodable = Some true
      ProvesSceneRendering = true
      ProvesDesktopVisibility = false
      Message = "rasterized offscreen" }

/// Drive the real sink the way a live loop does, collecting whatever it reported.
let private sinkInto (effect: ViewerEffect) =
    let diagnostics = ResizeArray<ViewerDiagnosticEvent>()
    Viewer.productEvidenceSink diagnostics.Add (fun () -> size) (fun () -> scene) effect
    List.ofSeq diagnostics

/// A fresh directory per case, so one test's artifact can never satisfy another's assertion.
let private scratch (name: string) =
    let dir = Path.Combine(Path.GetTempPath(), $"fsgg-444-{name}-{Guid.NewGuid():N}")
    Directory.CreateDirectory dir |> ignore
    dir

let private pngMagic = [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy |]

/// `ViewerEffect` carries a `SceneNode` and so has no structural equality — compare what the sink was
/// asked to write (the effect and its path), which is the thing under test anyway.
let private describe (effect: ViewerEffect) =
    match effect with
    | CaptureScreenshot path -> $"CaptureScreenshot {path}"
    | CaptureImageEvidence path -> $"CaptureImageEvidence {path}"
    | WriteVisualEvidence(path, _) -> $"WriteVisualEvidence {path}"
    | WriteRunEvidence(path, _) -> $"WriteRunEvidence {path}"
    | other -> $"NOT-EVIDENCE {other}"

[<Tests>]
let tests =
    testList
        "issue-444 evidence effects are honored, not discarded"
        [
          // THE regression, at the fold. Before #444 every one of these fell through to the discard
          // group and the sink was never called at all.
          test "the shared fold routes every evidence effect to the evidence sink" {
            let routed = ResizeArray<ViewerEffect>()

            let effects =
                [ CaptureScreenshot "shot.png"
                  CaptureImageEvidence "image.png"
                  WriteVisualEvidence("visual.txt", visualArtifact)
                  WriteRunEvidence("run.txt", runEvidence) ]

            Viewer.interpretViewerEffects ignore ignore ignore ignore ignore routed.Add effects
            |> ignore

            Expect.equal
                (routed |> Seq.map describe |> List.ofSeq)
                (effects |> List.map describe)
                "all four evidence effects reach the sink, in dispatch order — none is discarded"
          }

          // The fold must not have grown a leak in the other direction: effects that genuinely cannot
          // be honored inside a running loop must NOT be handed to a writer.
          test "effects that are not evidence never reach the evidence sink" {
            let routed = ResizeArray<ViewerEffect>()

            Viewer.interpretViewerEffects
                ignore
                ignore // #535 persistence sink
                ignore
                ignore
                ignore
                routed.Add
                [ RenderScene scene; ReadPixels; QueryNativeWindowState; CheckDesktopSession; CloseWindow ]
            |> ignore

            Expect.isEmpty routed "only the four evidence effects are evidence"
          }

          test "WriteRunEvidence writes the payload the product handed it" {
            let dir = scratch "run"
            let path = Path.Combine(dir, "nested", "run.txt")

            let diagnostics = sinkInto (WriteRunEvidence(path, runEvidence))

            Expect.isTrue (File.Exists path) "the file the product asked for exists — it did not before #444"
            Expect.isEmpty diagnostics "a write that succeeded reports no failure"

            let text = File.ReadAllText path
            Expect.stringContains text "framesRendered=7" "the product's own payload is what landed on disk"
            Expect.stringContains text "rendererMode=cpu" "every field is serialized, not just the first"
          }

          // `runBounded` already rules that a `.png` evidence path gets the rasterized scene and any other
          // path gets the text summary (`writeRunEvidence`). The live loops must apply the SAME rule, or
          // one effect means two different things depending on which host ran it — the two-copies drift
          // #429 took out of this fold, sneaking back one level down.
          test "WriteRunEvidence to a .png path rasterizes the scene, exactly as the bounded path does" {
            let dir = scratch "runpng"
            let path = Path.Combine(dir, "run.png")

            let diagnostics = sinkInto (WriteRunEvidence(path, runEvidence))

            Expect.isEmpty diagnostics "the write succeeded"

            Expect.equal
                (File.ReadAllBytes path |> Array.truncate 4)
                pngMagic
                "a .png run-evidence path gets a real PNG here too — not the text summary"
          }

          test "WriteVisualEvidence serializes the artifact record the product handed it" {
            let dir = scratch "visual"
            let path = Path.Combine(dir, "visual.txt")

            let diagnostics = sinkInto (WriteVisualEvidence(path, visualArtifact))

            Expect.isTrue (File.Exists path) "the artifact file exists"
            Expect.isEmpty diagnostics "a write that succeeded reports no failure"

            let text = File.ReadAllText path
            Expect.stringContains text "kind=MetadataHash" "the artifact's verdict is recorded verbatim"
            Expect.stringContains text "proves-scene-rendering=true" "including the claims it makes"
            Expect.stringContains text "message=rasterized offscreen" "including its message"
          }

          // Both capture effects rasterize the CURRENT scene offscreen — they depict what the product
          // drew, not the presented GL framebuffer. The disclosure is in SkiaViewer.fsi; the file is
          // what matters here, because before #444 there was none.
          test "CaptureScreenshot and CaptureImageEvidence each write a real PNG of the current scene" {
            for name, effect in
                [ "screenshot", (fun (p: string) -> CaptureScreenshot p)
                  "image", (fun p -> CaptureImageEvidence p) ] do
                let dir = scratch name
                let path = Path.Combine(dir, $"{name}.png")

                let diagnostics = sinkInto (effect path)

                Expect.isTrue (File.Exists path) $"{name}: the capture wrote its file"
                Expect.isEmpty diagnostics $"{name}: a capture that succeeded reports no failure"

                let bytes = File.ReadAllBytes path
                Expect.isGreaterThan bytes.Length 0 $"{name}: the PNG is not empty"

                Expect.equal
                    (Array.truncate 4 bytes)
                    pngMagic
                    $"{name}: the bytes are a real PNG, not a placeholder"
          }

          // ---- THE FAILURE LEG (#266). A fix whose failure path is untested is how this class lives on.
          //
          // Evidence I/O must never take a live render loop down, so the sink cannot throw. But it must
          // not go quiet either — that is the very defect. It reports on the diagnostics channel that was
          // already wired five lines above the old discard, and the reason string names the effect, the
          // path, and why.
          test "a write that cannot succeed raises a diagnostic naming the effect, the path and the reason" {
            let dir = scratch "unwritable"
            // A FILE where the sink needs a DIRECTORY: creating the parent of `blocker/run.txt` must fail.
            let blocker = Path.Combine(dir, "blocker")
            File.WriteAllText(blocker, "I am a file, not a directory.")
            let path = Path.Combine(blocker, "run.txt")

            let diagnostics = sinkInto (WriteRunEvidence(path, runEvidence))

            Expect.isFalse (File.Exists path) "nothing was written — that is the premise of this test"

            let diagnostic =
                Expect.wantSome (List.tryExactlyOne diagnostics) "the failed write raises exactly one diagnostic"

            Expect.equal diagnostic.Level ViewerDiagnosticLevel.Error "a dropped evidence write is an Error"

            Expect.equal
                diagnostic.Stage
                (Some ViewerRunBlockedStage.ArtifactWrite)
                "staged where it failed: the artifact write"

            Expect.stringContains
                diagnostic.Message
                "WriteRunEvidence"
                "the reason names the EFFECT that was dropped"

            Expect.stringContains diagnostic.Message path "the reason names the PATH that was not written"

            // The whole point: this must not be silent. A diagnostic the default options filter away is
            // just a slower way of dropping it on the floor.
            Expect.isTrue
                (Viewer.shouldCaptureDiagnostic Viewer.defaultDiagnostics diagnostic)
                "and it survives the DEFAULT diagnostics filter — otherwise the failure is still silent"
          }

          test "an empty path is reported rather than written, and does not throw" {
            let diagnostic =
                Expect.wantSome
                    (sinkInto (CaptureScreenshot "") |> List.tryExactlyOne)
                    "an empty path raises exactly one diagnostic"

            Expect.equal diagnostic.Level ViewerDiagnosticLevel.Error "an unwritable capture is an Error"

            Expect.stringContains
                diagnostic.Message
                "CaptureScreenshot"
                "the reason names the effect that was dropped"

            Expect.stringContains diagnostic.Message "empty path" "and says what was wrong with it"
          }

          // The loops call this on every dispatched message. A throw here would tear down the window —
          // the #365 failure — so the sink swallows the exception and reports instead.
          test "the sink never throws, whatever the product asks it to write" {
            let dir = scratch "nothrow"
            let blocker = Path.Combine(dir, "blocker")
            File.WriteAllText(blocker, "file")
            let doomed = Path.Combine(blocker, "artifact")

            for effect in
                [ CaptureScreenshot doomed
                  CaptureImageEvidence doomed
                  WriteVisualEvidence(doomed, visualArtifact)
                  WriteRunEvidence(doomed, runEvidence) ] do
                let diagnostics = sinkInto effect

                Expect.isNonEmpty
                    diagnostics
                    $"{effect} reports its failure instead of throwing it into the render loop"
          }
        ]
