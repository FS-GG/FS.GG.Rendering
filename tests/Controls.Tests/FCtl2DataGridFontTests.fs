module FCtl2DataGridFontTests

// F-CTL-2 (2026-07-15 repo review). `DataGridGeometry.cellText` painted `cellFontSize = 11.0`
// RAW — the only cell-family site not routed through the resolver — so no theme class / visual
// state could rescale grid-cell text (unlike radio/slider, whose font literals already feed the
// resolver as `baseStyle`; see Issue383/Issue384). The fix builds a `baseStyle` at `cellFontSize`,
// resolves through `Style.resolve theme baseStyle classes state`, and paints `mkTextW` at the
// RESOLVED size/weight; `ContentRender` threads the cell's attached classes + visual state in.
//
// These tests pin BOTH halves: the byte-identity anchor (a plain cell still paints 11.0, so no
// shipped output moved) AND the newly-live seam (a `StyleClass.Font` now reaches the cell label).
// The seam test REDs against the pre-fix raw literal — the resolved size never reached the paint.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private box: Rect = { X = 0.0; Y = 0.0; Width = 160.0; Height = 28.0 }

/// Recursively gather every emitted text run from a rendered `Scene list`, recursing through the
/// transparent wrapper nodes (the cell text is nested inside a `Clip → Group`).
let rec private textRuns (s: Scene) : TextRun list = s.Nodes |> List.collect textRunsNode

and private textRunsNode (n: SceneNode) : TextRun list =
    match n with
    | TextRun r -> [ r ]
    | ClipNode(_, inner)
    | Translate(_, inner)
    | ColorSpaceNode(_, inner)
    | PerspectiveNode(_, inner) -> textRuns inner
    | Group scenes -> scenes |> List.collect textRuns
    | _ -> []

/// The one label `TextRun` carrying `text` across a rendered cell's `Scene list`.
let private labelFont (scenes: Scene list) (text: string) : FontSpec =
    scenes
    |> List.collect textRuns
    |> List.filter (fun (r: TextRun) -> r.Text = text)
    |> function
        | [ r ] -> r.Font
        | other -> failwithf "expected exactly one %A cell label, got %d" text (List.length other)

[<Tests>]
let fCtl2DataGridFontTests =
    testList
        "F-CTL-2 · DataGrid cell typography flows through the resolver"
        [ test "a plain body cell still paints the 11.0 base size (byte-identity anchor)" {
              let font = labelFont (DataGridGeometry.cellGeom theme box [] Normal "42") "42"
              Expect.equal font.Size 11.0 "an unthemed cell keeps the former raw literal size"
              Expect.equal font.Weight None "an unthemed cell weight stays None"
          }

          test "a plain header cell still paints the 11.0 base size (byte-identity anchor)" {
              let font = labelFont (DataGridGeometry.headerCellGeom theme box [] Normal "Name") "Name"
              Expect.equal font.Size 11.0 "an unthemed header cell keeps the former raw literal size"
          }

          test "a Font class rescales the body-cell label — the seam is live, not a raw literal" {
              // REDs against the pre-fix raw `cellFontSize`: the resolved size never reached paint.
              let classes = [ StyleClass.Font { Size = Some 22.0; Weight = Some 700 } ]
              let font = labelFont (DataGridGeometry.cellGeom theme box classes Normal "42") "42"
              Expect.equal font.Size 22.0 "the Font class Size reaches the cell label"
              Expect.equal font.Weight (Some 700) "the Font class Weight reaches the cell label"
          }

          test "a Font class rescales the header-cell label too" {
              let classes = [ StyleClass.Font { Size = Some 18.0; Weight = None } ]
              let font = labelFont (DataGridGeometry.headerCellGeom theme box classes Normal "Name") "Name"
              Expect.equal font.Size 18.0 "the Font class Size reaches the header-cell label"
          } ]
