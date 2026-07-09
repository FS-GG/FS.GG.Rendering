module Issue181IntentStateTests

// Issue #181 — a destructive `danger` button rendered in the primary accent colour on hover.
//
// `Style.applyState` overwrote `Fill` unconditionally (`Hover -> theme.Accent`), so the class layer's
// intent colour was discarded the moment the pointer arrived: a red "Delete" button turned primary
// blue with a red border, at the exact instant it was about to be clicked. `success` and `warning`
// lost their intent the same way. The precedence rule ("state wins over class") was implemented
// correctly; the rule itself was wrong.
//
// The state layer now MODULATES the fill it is handed — `Hover` lightens it, `Pressed` and `Selected`
// darken it — so the hue survives whichever layer contributed the colour (structural base, class, or
// the theme's own `IntentPolicy`). The two states that deliberately do NOT preserve intent are
// `Disabled` (an inactive control must not advertise an action it will not perform) and `Focused`
// (the focus ring is an accessibility affordance, not an intent).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

// ---- hue, the invariant under test -----------------------------------------------------------

/// HSV hue in degrees [0, 360). `None` for an achromatic colour (grey/black/white), whose hue is
/// genuinely undefined rather than zero — asserting equality on it would assert nothing.
let private hue (c: Color) : float option =
    let r, g, b = float c.Red / 255.0, float c.Green / 255.0, float c.Blue / 255.0
    let mx = max r (max g b)
    let mn = min r (min g b)
    let d = mx - mn

    if d = 0.0 then
        None
    else
        let h =
            if mx = r then 60.0 * (((g - b) / d) % 6.0)
            elif mx = g then 60.0 * (((b - r) / d) + 2.0)
            else 60.0 * (((r - g) / d) + 4.0)

        Some(if h < 0.0 then h + 360.0 else h)

/// Scaling every channel toward a common endpoint is a hue-preserving operation in exact arithmetic;
/// rounding each channel to a byte perturbs it by a fraction of a degree. 1.5° is well inside the
/// margin that separates the shipped intent colours (red 0°, green 142°, amber 26°, blue 217°).
let private hueEpsilon = 1.5

/// Hue is an angle: 359.3° and 0.2° are 0.9° apart, not 359.1°. Ant's `#ff4d4f` sits just below the
/// wrap, so a naive subtraction would report a false drift.
let private hueDistance (a: float) (b: float) =
    let d = abs (a - b) % 360.0
    min d (360.0 - d)

let private expectHueClose (actual: Color) (expected: Color) (msg: string) =
    match hue actual, hue expected with
    | Some a, Some e -> Expect.isLessThan (hueDistance a e) hueEpsilon (sprintf "%s (hue %f vs %f)" msg a e)
    | None, None -> ()
    | a, e -> failtestf "%s: one colour is achromatic and the other is not (%A vs %A)" msg a e

// Fully qualified: `open FS.GG.UI.DesignSystem` brings the `Theme` TYPE into scope, which shadows the
// `Theme` module this line wants (the same hazard Feature127ColorPolicyTests documents).
let private theme = FS.GG.UI.Themes.Default.Theme.light

/// The intent-bearing variants: each owns an opaque fill in a distinct hue. `Neutral` is excluded
/// because it declares "no intent"; `Ghost` because its fill is deliberately transparent (it carries
/// its intent in the stroke and label) — both are covered by their own tests below.
let private intentVariants =
    [ "primary", StyleVariant.Primary
      "danger", StyleVariant.Danger
      "success", StyleVariant.Success
      "warning", StyleVariant.Warning ]

/// The states that must preserve an intent-owned fill. `Focused` is absent because it touches only
/// `Stroke`; `Disabled`, `Normal`, `Loading` and the `Validation` states are covered separately.
let private intentPreservingStates =
    [ "hover", Hover
      "pressed", Pressed
      "selected", Selected
      "focused-hover", FocusedHover ]

let private resolveWith variant state =
    StyleResolver.resolve theme "button" "" [ Variant variant ] state

[<Tests>]
let issue181IntentStateTests =
    testList
        "Issue181 state deltas preserve intent"
        [
          // The reported defect, stated as its reproduction: a `danger` button under the pointer.
          test "a danger button stays red through Hover / Pressed / Selected (issue #181)" {
              let normal = resolveWith StyleVariant.Danger Normal
              Expect.equal normal.Fill theme.Danger "at rest the danger fill is the danger token"

              for name, state in intentPreservingStates do
                  let styled = resolveWith StyleVariant.Danger state

                  Expect.notEqual
                      styled.Fill
                      theme.Accent
                      (sprintf "danger/%s must not wear the primary accent colour" name)

                  Expect.notEqual styled.Fill theme.Muted (sprintf "danger/%s must not wear the muted colour" name)

                  expectHueClose styled.Fill theme.Danger (sprintf "danger/%s keeps the danger hue" name)
          }

          // The acceptance criterion as a property over the whole closed domain, not one example.
          test "every (intent variant x state) pair preserves the variant's hue (issue #181)" {
              for variantName, variant in intentVariants do
                  let normal = resolveWith variant Normal

                  let baseHue =
                      match hue normal.Fill with
                      | Some h -> h
                      | None -> failtestf "%s: the resting fill must be chromatic for the hue test to mean anything" variantName

                  for stateName, state in intentPreservingStates do
                      let styled = resolveWith variant state
                      let label = sprintf "%s/%s" variantName stateName

                      Expect.equal styled.Fill.Alpha 255uy (sprintf "%s stays opaque" label)

                      match hue styled.Fill with
                      | None -> failtestf "%s: modulation must not wash the fill out to grey" label
                      | Some h ->
                          Expect.isLessThan
                              (hueDistance h baseHue)
                              hueEpsilon
                              (sprintf "%s: hue %f drifted from the variant's %f" label h baseHue)
          }

          // Hue survives, but the state must still be VISIBLE — a modulation of zero would satisfy the
          // property above and give the user no affordance at all. Each step moves the fill further
          // from the label it carries, so emphasis and readability rise together instead of trading off.
          // Measured with the real WCAG engine, over BOTH theme polarities: a fixed lighten/darken
          // direction would pass on one and fail on the other.
          test "each emphasis step raises the label's contrast, on light and dark themes (issue #181)" {
              let themes =
                  [ "light", FS.GG.UI.Themes.Default.Theme.light
                    "dark", FS.GG.UI.Themes.Default.Theme.dark
                    "ant-dark", FS.GG.UI.Themes.AntDesign.AntTheme.antDark ]

              for themeName, t in themes do
                  for variantName, variant in intentVariants do
                      let contrastOf state =
                          let s = StyleResolver.resolve t "button" "" [ Variant variant ] state
                          FS.GG.UI.Color.Contrast.ratio s.Foreground s.Fill

                      let rest = contrastOf Normal
                      let hovered = contrastOf Hover
                      let pressed = contrastOf Pressed
                      let selected = contrastOf Selected
                      let label = sprintf "%s/%s" themeName variantName

                      Expect.isGreaterThan hovered rest (sprintf "%s: Hover is a visible step off the resting fill" label)
                      Expect.isGreaterThan pressed hovered (sprintf "%s: Pressed presses further than Hover" label)
                      Expect.isGreaterThan selected pressed (sprintf "%s: Selected commits deeper than Pressed" label)
          }

          // `success` / `warning` behave consistently — and, crucially, DIFFERENTLY from one another
          // and from `primary`. Before the fix all three hovered to the same accent blue, which is
          // exactly what made the defect invisible to a test that only checked "state wins".
          test "each intent hovers to its own distinct colour (issue #181)" {
              let hovered =
                  intentVariants |> List.map (fun (name, v) -> name, (resolveWith v Hover).Fill)

              let colours = hovered |> List.map snd
              Expect.equal (List.distinct colours).Length colours.Length "no two intents hover to the same fill"

              // and none of them is simply the accent, the colour they all used to collapse onto
              for name, fill in hovered do
                  if name <> "primary" then
                      Expect.notEqual fill theme.Accent (sprintf "%s no longer collapses onto the accent" name)
          }

          // `ghost` carries its intent in stroke + label over a deliberately transparent fill. The
          // modulation is defined to leave a fully transparent colour alone: nudging the channels of
          // something nobody can see would invent an invisible colour, and — worse — hand the contrast
          // gate a boundary to measure where the control has none. Before the fix, `ghost` hover
          // painted a solid accent fill under a dark label (ratio 2.84, a WCAG failure).
          test "a ghost button's transparent fill modulates to itself (issue #181)" {
              for stateName, state in intentPreservingStates do
                  let styled = resolveWith StyleVariant.Ghost state

                  Expect.equal
                      styled.Fill
                      Colors.transparent
                      (sprintf "ghost/%s keeps its transparent fill" stateName)

                  // …and its label stays the readable one it started with. `Selected` used to repaint
                  // the foreground `theme.Background`, which over a transparent fill painted the label
                  // straight onto the background it was standing on (ratio 1.00, invisible).
                  Expect.equal
                      styled.Foreground
                      theme.Foreground
                      (sprintf "ghost/%s keeps a label readable against the canvas" stateName)
          }

          // The same holds for a fill an INTENT POLICY contributed rather than a class: the modulation
          // is origin-agnostic, so no layer has to publish "who owns Fill". The Ant theme's policy
          // makes `danger` structurally distinct (Feature 173); its hovered fill must stay in the
          // danger hue too.
          test "an IntentPolicy-owned fill is modulated, not replaced (issue #181)" {
              let ant = FS.GG.UI.Themes.AntDesign.AntTheme.antLight
              let rest = StyleResolver.resolve ant "button" "danger" [] Normal
              let hovered = StyleResolver.resolve ant "button" "danger" [] Hover

              if rest.Fill.Alpha > 0uy then
                  Expect.notEqual hovered.Fill ant.Accent "an Ant danger button must not hover to the accent"
                  expectHueClose hovered.Fill rest.Fill "the Ant danger hue survives hover"
          }

          // The two documented exceptions. `Disabled` discards the intent on purpose; `Focused` paints
          // the accent ring on purpose and leaves the fill alone.
          test "Disabled discards intent and Focused leaves the fill alone (issue #181)" {
              for variantName, variant in intentVariants do
                  let disabled = resolveWith variant Disabled
                  Expect.equal disabled.Fill theme.Muted (sprintf "%s/disabled greys out — an inactive control claims no intent" variantName)
                  Expect.equal disabled.Foreground theme.Muted (sprintf "%s/disabled greys its label too" variantName)

                  let focused = resolveWith variant Focused
                  let rest = resolveWith variant Normal
                  Expect.equal focused.Fill rest.Fill (sprintf "%s/focused leaves the fill untouched" variantName)
                  Expect.equal focused.Stroke theme.Accent (sprintf "%s/focused wears the accent focus ring" variantName)
          }

          // Parity guard (FR-005/SC-003): the no-class, `Normal` identity that the 093 contract fixes.
          test "resolve theme base [] Normal is still exactly base (issue #181 preserves parity)" {
              let baseStyle = StyleResolver.baseStyleFor theme "button"
              Expect.equal (Style.resolve theme baseStyle [] Normal) baseStyle "the default path is identity"
              Expect.equal (Style.resolve theme baseStyle [] Loading) baseStyle "Loading paints like Normal"
          }
        ]
