module Issue383TypographyResolverTests

// Issue #383 (child of #361 — "wire design tokens into non-color rendering").
//
// Before this change, the `*Geom` producers that already resolve a `ResolvedStyle` consumed its
// COLOUR fields but DISCARDED its typography: they passed a literal font size to `mkText`, and
// `mkText` hardcoded `Weight = None`. So `ResolvedStyle.FontSize`/`FontWeight` were dead at the
// text sites — a theme could not restyle the label font even though the resolver computed one.
//
// The fix routes those sites through `mkTextW theme x y style.FontSize style.FontWeight …`. It is
// BYTE-IDENTICAL for every shipped theme/state (each per-kind base `FontSize` already equals the
// former literal, and `FontWeight` is `None`) — the existing parity oracles (Feature093ParityTests)
// still pass. These tests add the guard the parity oracles cannot: they prove the typography is now
// CONSUMED, and pin the byte-identity anchor so a future edit cannot silently regress to a literal.
//
// The observable seam is `buttonGeom`, which resolves through `StyleResolver.resolve` and therefore
// the ACTIVE THEME's `IntentPolicy`. A policy that varies `FontSize`/`FontWeight` must now reach the
// emitted label; today no shipped `IntentPolicy`/overlay varies typography (that is sibling #384),
// so a bespoke policy is the only way to exercise the wiring — which is exactly the point.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private size: Size = { Width = 320; Height = 120 }

/// Recursively gather every emitted text run (label carriers) from a rendered scene, recursing
/// through the transparent wrapper nodes (mirrors Feature362RenderParityTests' walker).
let rec private textRuns (s: Scene) : FS.GG.UI.Scene.TextRun list = s.Nodes |> List.collect textRunsNode

and private textRunsNode (n: SceneNode) : FS.GG.UI.Scene.TextRun list =
    match n with
    | TextRun r -> [ r ]
    | ClipNode(_, inner)
    | Translate(_, inner)
    | ColorSpaceNode(_, inner)
    | PerspectiveNode(_, inner) -> textRuns inner
    | Group scenes -> scenes |> List.collect textRuns
    | _ -> []

let private labelFont (theme: Theme) (control: Control<'msg>) (text: string) : FontSpec =
    (Control.renderTree theme size control).Scene
    |> textRuns
    |> List.filter (fun (r: FS.GG.UI.Scene.TextRun) -> r.Text = text)
    |> List.map (fun r -> r.Font)
    |> function
        | [ font ] -> font
        | [] -> failwithf "no TextRun carried the label %A" text
        | many -> failwithf "expected exactly one TextRun for %A, got %d" text (List.length many)

/// A theme whose `IntentPolicy` overrides typography for every intent — the seam that proves the
/// resolver's `FontSize`/`FontWeight` reach the label rather than being dropped for a literal.
let private typographyTheme: Theme =
    { Theme.light with
        IntentPolicy =
            { Name = "issue-383-typography"
              ApplyIntent = fun _ _ _ style -> { style with FontSize = 42.0; FontWeight = Some 700 } } }

[<Tests>]
let issue383TypographyResolverTests =
    testList
        "Issue383 · resolver typography reaches the text sites"
        [ test "a button label carries the RESOLVED FontSize/FontWeight, not a discarded literal" {
              // The policy sets FontSize 42.0 / Weight 700; the wired buttonGeom must emit them.
              let font = labelFont typographyTheme (Button.create [ Button.text "Go" ]) "Go"
              Expect.equal font.Size 42.0 "the label font size is the resolved size, not the literal 15.0"
              Expect.equal font.Weight (Some 700) "the label font weight is the resolved weight, not a hardcoded None"
          }

          test "under the neutral Default policy the button label tracks theme.FontSize (#384)" {
              // Theme.light carries IntentPolicy.neutral, so the resolved base is unperturbed. #384
              // reconciled the base FontSize (was a frozen 15.0) to `theme.FontSize`, so a neutral
              // button now paints the theme's body size; weight stays the hardcoded None.
              let font = labelFont Theme.light (Button.create [ Button.text "Go" ]) "Go"
              Expect.equal font.Size Theme.light.FontSize "default button label tracks theme.FontSize (#384)"
              Expect.equal font.Weight None "default button label weight stays None"
          } ]
