module Issue384TypographyClassTests

// Issue #384 (child of #361 — "wire design tokens into non-color rendering"). Two coupled gaps:
//
//   1. The class/state overlay was COLOUR-ONLY: `Style.applyVariant`/`applyCustom` only ever
//      rewrote Fill/Stroke/Foreground, so no attached `StyleClass` could restyle typography and
//      `ResolvedStyle.FontSize`/`FontWeight` always equalled the per-kind base. `StyleClass.Font`
//      now carries a `FontDelta` into the same fold, overlaying only the typography fields it names.
//
//   2. `StyleResolver.baseStyleFor` hardcoded `FontSize = 15.0`, overriding `theme.FontSize` (14).
//      The base now tracks the theme, and `buttonGeom`/`textFieldGeom` measure their width at the
//      RESOLVED size so paint and measure agree once the theme (or a `Font` class) varies the size.
//
// (1) is pinned at the resolver seam (`Style.resolve`) and end-to-end through `buttonGeom`; (2) is
// pinned by the themed base plus a width-tracking check that only holds because the width oracle
// follows the resolved typography.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private buttonBase = StyleResolver.baseStyleFor theme "button"

/// The widest `Rectangle` in a rendered widget — the button box (labels/borders are narrower).
let private widestRect (scenes: Scene list) : float =
    scenes
    |> List.collect (fun s -> s.Nodes)
    |> List.choose (fun n ->
        match n with
        | Rectangle((_, _, w, _), _) -> Some w
        | _ -> None)
    |> function
        | [] -> failwith "no Rectangle node in the button geometry"
        | ws -> List.max ws

/// The one label `TextRun` carrying `text` in a rendered widget.
let private labelFont (scenes: Scene list) (text: string) : FontSpec =
    scenes
    |> List.collect (fun s -> s.Nodes)
    |> List.choose (fun n ->
        match n with
        | TextRun r when r.Text = text -> Some r.Font
        | _ -> None)
    |> function
        | [ f ] -> f
        | other -> failwithf "expected exactly one %A label, got %d" text (List.length other)

[<Tests>]
let issue384TypographyClassTests =
    testList
        "Issue384 · a StyleClass.Font restyles typography through the resolver"
        [
          // ---- (1) the Font class carries typography into the fold ----------------------------
          test "Font Size overrides the base size; a None weight leaves the folded weight" {
              let s = Style.resolve theme buttonBase [ StyleClass.Font { Size = Some 24.0; Weight = None } ] Normal
              Expect.equal s.FontSize 24.0 "the Font class Size overrides the base size"
              Expect.equal s.FontWeight buttonBase.FontWeight "a None weight leaves the folded weight untouched"
          }

          test "Font Weight overrides the base weight; a None size leaves the folded size" {
              let s = Style.resolve theme buttonBase [ StyleClass.Font { Size = None; Weight = Some 700 } ] Normal
              Expect.equal s.FontWeight (Some 700) "the Font class Weight overrides the base weight"
              Expect.equal s.FontSize buttonBase.FontSize "a None size leaves the folded size untouched"
          }

          test "a Font class changes ONLY typography — every colour field is untouched" {
              let s = Style.resolve theme buttonBase [ StyleClass.Font { Size = Some 20.0; Weight = Some 600 } ] Normal
              Expect.equal s.Fill buttonBase.Fill "Fill unchanged"
              Expect.equal s.Stroke buttonBase.Stroke "Stroke unchanged"
              Expect.equal s.Foreground buttonBase.Foreground "Foreground unchanged"
          }

          test "Font composes with a colour class — orthogonal fields, so attach order is immaterial" {
              let font = StyleClass.Font { Size = Some 18.0; Weight = Some 500 }
              let colour = StyleClass.Variant StyleVariant.Danger
              let a = Style.resolve theme buttonBase [ colour; font ] Normal
              let b = Style.resolve theme buttonBase [ font; colour ] Normal
              Expect.equal a.FontSize 18.0 "typography comes from the Font class"
              Expect.equal a.Fill theme.Danger "colour comes from the Variant class"
              Expect.equal a b "typography and colour are orthogonal fields ⇒ both orders resolve identically"
          }

          test "a later Font wins over an earlier one on the fields it names (left-to-right fold)" {
              let s =
                  Style.resolve
                      theme
                      buttonBase
                      [ StyleClass.Font { Size = Some 12.0; Weight = Some 400 }
                        StyleClass.Font { Size = Some 30.0; Weight = None } ]
                      Normal
              Expect.equal s.FontSize 30.0 "the later Font's Size wins"
              Expect.equal s.FontWeight (Some 400) "the later Font's None leaves the earlier weight (400)"
          }

          // ---- (2) themed base + the width oracle follows the resolved size --------------------
          test "a neutral button's base FontSize tracks theme.FontSize (part 2)" {
              Expect.equal buttonBase.FontSize theme.FontSize "baseStyleFor tracks the theme body size, not a frozen 15.0"
          }

          test "a Font class reaches the emitted label AND widens the button to fit it" {
              let box: Rect = { X = 0.0; Y = 0.0; Width = 1000.0; Height = 80.0 }
              let label = "Typography"
              let plain = WidgetGeometry.buttonGeom theme box [] Normal "button" "" label
              let big =
                  WidgetGeometry.buttonGeom theme box [ StyleClass.Font { Size = Some 48.0; Weight = Some 700 } ] Normal "button" "" label

              // the resolved typography reaches the painted label...
              Expect.equal (labelFont plain label).Size theme.FontSize "plain label paints the themed base size"
              Expect.equal (labelFont big label).Size 48.0 "the Font class size reaches the label"
              Expect.equal (labelFont big label).Weight (Some 700) "the Font class weight reaches the label"

              // ...and the button box, measured at the RESOLVED size, grows to fit the larger label.
              Expect.isGreaterThan (widestRect big) (widestRect plain) "the big-font button is wider than the plain one"
          }
        ]
