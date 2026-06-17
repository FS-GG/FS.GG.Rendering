module Feature136MeasurementSeamTests

// Feature 136 (US1/T008): the measurement seam. The truncation defect was a measure-vs-draw advance
// disagreement (heuristic 0.58·size sizing the box vs the 5×7 vector path drawing at 0.857·size). The
// fix introduces `Scene.measureTextResolved`, which returns the bundled-font renderer's true advances
// when the rendering edge installs a real measurer — so the advance used to SIZE a text box equals the
// advance used to DRAW it. These pure-`Scene` tests assert the seam contract and that the retained pure
// heuristic stays conservative (never narrower than the real draw) so pure-path callers never clip.
//
// Pre-fix this suite has nothing to test against: `measureTextResolved`/`setRealTextMeasurer` did not
// exist and the heuristic disagreed with the (wider) vector draw advance, so boxes clipped.

open Expecto
open FS.GG.UI.Scene

let private font: FontSpec = { Family = None; Size = 16.0; Weight = None }

let private labels = [ "Stable"; "Upload"; "Refresh"; "1234567890" ]

[<Tests>]
let tests =
    // Sequenced: these mutate the process-wide real-measurer slot.
    testSequenced
    <| testList
        "Feature136 measurement seam (US1/T008)"
        [ test "measureTextResolved uses the injected real measurer — size advance == draw advance" {
              // Model the renderer's per-char real advance as a stub; the box-sizing path must return
              // exactly the same advance the renderer would draw at.
              let drawAdvance (text: string) (f: FontSpec) : TextMetrics =
                  { Width = 0.5 * f.Size * float text.Length
                    Height = f.Size
                    Baseline = f.Size * 0.8 }

              try
                  Scene.setRealTextMeasurer (Some drawAdvance)

                  for label in labels do
                      let sized = Scene.measureTextResolved label font
                      let drawn = drawAdvance label font
                      Expect.equal sized.Width drawn.Width (sprintf "size advance == draw advance for '%s'" label)
              finally
                  Scene.setRealTextMeasurer None
          }

          test "pure heuristic is conservative — never narrower than the real draw advance (no clip)" {
              // With no real measurer (pure callers / pure goldens), the heuristic must size boxes at
              // least as wide as the bundled font actually draws (~0.49·size for Noto Sans, per the R1
              // probe) so text never clips mid-word even on the pure path.
              for label in labels do
                  let pureWidth = (Scene.measureText label font).Width
                  let realDraw = 0.49 * font.Size * float label.Length
                  Expect.isTrue (pureWidth >= realDraw) (sprintf "heuristic (%f) >= real draw (%f) for '%s'" pureWidth realDraw label)
          }

          test "with no measurer installed, resolved == pure (pure-caller default is byte-identical)" {
              Scene.setRealTextMeasurer None
              let resolved = Scene.measureTextResolved "Refresh" font
              let pure' = Scene.measureText "Refresh" font
              Expect.equal resolved pure' "measureTextResolved falls back to the pure heuristic"
          } ]
