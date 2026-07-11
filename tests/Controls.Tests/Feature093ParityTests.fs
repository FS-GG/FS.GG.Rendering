module Feature093ParityTests

// Feature 093 (E3) — migration parity + scope.
//   * SC-003 (T020/T021): for the migrated kinds (Button, CheckBox) the resolver-driven paint for
//     the default (no-class) case is structurally-`Scene`-equal to the PRIOR procedural output for
//     each (kind, theme, state). The oracle is a frozen, inline reproduction of the pre-refactor
//     `buttonGeom`/`checkboxGeom` geometry with inline theme colours (the same frozen-literal
//     technique `DesignTokenParityTests` uses); the migrated render must match it byte-for-byte.
//   * SC-007 (T022): an unmigrated kind shows no render-output delta — attaching a style class to
//     it changes nothing (the migration is additive and scoped to Button/CheckBox).

open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem
open FS.GG.TestSupport

let private box: Rect = { X = 10.0; Y = 40.0; Width = 284.0; Height = 92.0 }

let private mkText (theme: Theme) (x: float) (baseline: float) (size: float) (color: Color) (s: string) =
    Scene.textRun
        { Text = s
          Position = { X = x; Y = baseline }
          Font = { Family = theme.FontFamily; Size = size; Weight = None }
          Paint = Paint.fill color }

// ---- frozen pre-refactor procedural geometry (the parity oracle) ---------------------------
let private frozenButtonGeom (theme: Theme) (label: string) : Scene list =
    // #385: dimensions now flow from the theme metric model (Ant control-size + Space scale).
    // #384: typography now flows from it too — the base font tracks `theme.FontSize` (was a frozen 15.0).
    let h = theme.ControlHeight
    let textW = (Scene.measureText label { Family = theme.FontFamily; Size = theme.FontSize; Weight = None }).Width
    let w = min box.Width (max 70.0 (textW + 2.0 * theme.SpaceMd))
    let by = box.Y + box.Height / 2.0 - h / 2.0
    [ Scene.rectangle (box.X, by, w, h) theme.Accent
      mkText theme (box.X + theme.SpaceMd) (by + h / 2.0 + 5.0) theme.FontSize theme.Background label ]

let private frozenCheckboxGeom (theme: Theme) (on: bool) (label: string) : Scene list =
    let s = theme.ControlHeightSm + theme.SpaceXs
    let bx = box.X
    let cy = box.Y + box.Height / 2.0
    let by = cy - s / 2.0
    let boxRect = { X = bx; Y = by; Width = s; Height = s }
    let fill =
        if on then [ Scene.rectangle (bx, by, s, s) theme.Accent ]
        else [ Scene.rectangleWithPaint boxRect (Paint.stroke theme.Foreground 2.0) ]
    let tick =
        if on then
            [ Scene.line { X = bx + 6.0; Y = by + 15.0 } { X = bx + 12.0; Y = by + 21.0 } (Paint.stroke theme.Background 3.0)
              Scene.line { X = bx + 12.0; Y = by + 21.0 } { X = bx + 23.0; Y = by + 7.0 } (Paint.stroke theme.Background 3.0) ]
        else
            []
    let text = [ mkText theme (bx + s + theme.SpaceSm) (cy + 5.0) 13.0 theme.Foreground label ]
    fill @ tick @ text

let private themes = [ "light", Theme.light; "dark", Theme.dark ]

// The frozen-oracle baselines are COMMITTED goldens under readiness/parity/, regenerated only when
// PARITY_REGEN is set (then committed) — the Feature109 corpus pattern (env-gated regen + committed-
// golden compare). T020 asserts the committed file still equals the frozen oracle, so drift in either
// the oracle or the file fails loudly instead of being silently overwritten every run.
let private parityDir =
    Path.Combine(RepositoryRoot.value, "specs", "093-visual-state-style-layer", "readiness", "parity")

let private parityRegen =
    not (System.String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable "PARITY_REGEN"))

// Each baseline: its committed filename paired with the frozen-oracle serialization it must equal.
let private parityBaselines () : (string * string) list =
    [ for (tname, theme) in themes do
        sprintf "button.%s.normal.scene.txt" tname, sprintf "%A" (frozenButtonGeom theme "Save")
        sprintf "check-box.%s.normal.scene.txt" tname, sprintf "%A" (frozenCheckboxGeom theme false "Enabled")
        sprintf "check-box-checked.%s.normal.scene.txt" tname, sprintf "%A" (frozenCheckboxGeom theme true "Enabled") ]

[<Tests>]
let feature093ParityTests =
    testList "Feature 093 migration parity (SC-003/SC-007)" [

        test "T020 — the pre-refactor procedural baselines are committed goldens the frozen oracle still matches" {
            if parityRegen then
                Directory.CreateDirectory parityDir |> ignore
                for (name, content) in parityBaselines () do
                    File.WriteAllText(Path.Combine(parityDir, name), content)

            // Falsifiable AGAINST A COMMITTED FILE: fails if a golden was never regenerated+committed,
            // or if the frozen oracle has drifted away from the committed file — not a file the test
            // just wrote to itself. Run with PARITY_REGEN=1 and commit to (re)bless the goldens.
            Expect.isNonEmpty (parityBaselines ()) "the migrated kinds define at least one parity baseline"
            for (name, content) in parityBaselines () do
                let path = Path.Combine(parityDir, name)
                Expect.isTrue
                    (File.Exists path)
                    (sprintf "parity golden committed for %s (run PARITY_REGEN=1 to (re)generate, then commit)" name)
                Expect.equal content (File.ReadAllText path) (sprintf "frozen-oracle %s matches its committed golden" name)
        }

        test "Button no-class paint is structurally-Scene-equal to the procedural baseline, both themes (SC-003)" {
            for (tname, theme) in themes do
                let button = Button.create [ Button.text "Save" ]
                let actual = ControlInternals.faithfulContent theme box button
                Expect.equal actual (frozenButtonGeom theme "Save") (sprintf "button.%s no-class render matches the procedural baseline" tname)
        }

        test "CheckBox (unchecked) no-class paint matches the procedural baseline, both themes (SC-003)" {
            for (tname, theme) in themes do
                let cb = CheckBox.create [ CheckBox.text "Enabled" ]
                let actual = ControlInternals.faithfulContent theme box cb
                Expect.equal actual (frozenCheckboxGeom theme false "Enabled") (sprintf "check-box.%s unchecked render matches baseline" tname)
        }

        test "CheckBox (checked) no-class paint matches the procedural baseline, both themes (SC-003)" {
            for (tname, theme) in themes do
                let cb = CheckBox.create [ CheckBox.text "Enabled"; CheckBox.checked' true ]
                let actual = ControlInternals.faithfulContent theme box cb
                Expect.equal actual (frozenCheckboxGeom theme true "Enabled") (sprintf "check-box.%s checked render matches baseline" tname)
        }

        test "the migrated render is deterministic across calls (SC-003)" {
            let button = Button.create [ Button.text "Save" ]
            Expect.equal
                (ControlInternals.faithfulContent Theme.light box button)
                (ControlInternals.faithfulContent Theme.light box button)
                "the resolver-driven render is deterministic"
        }

        // ---- SC-007 — unmigrated kinds are unchanged --------------------------------------
        test "an unmigrated kind ignores attached style classes — no render delta (SC-007)" {
            // `progress-bar` is NOT migrated (096 widened slider/text-box/radio-group/switch, not
            // progress-bar); attaching a class must not change its render.
            let plain = ProgressBar.create [ ProgressBar.value 0.5 ]
            let classed = ProgressBar.create [ ProgressBar.value 0.5; Attr.styleClasses [ Variant StyleVariant.Danger ] ]
            Expect.equal
                (ControlInternals.faithfulContent Theme.light box classed)
                (ControlInternals.faithfulContent Theme.light box plain)
                "an unmigrated control's render is unchanged whether or not a class is attached"
        }

        test "a MIGRATED kind DOES respond to an attached class (US1 vertical slice / additive proof)" {
            let plain = Button.create [ Button.text "Save" ]
            let danger = Button.create [ Button.text "Save"; Attr.styleClasses [ Variant StyleVariant.Danger ] ]
            Expect.notEqual
                (ControlInternals.faithfulContent Theme.light box danger)
                (ControlInternals.faithfulContent Theme.light box plain)
                "attaching a Danger class changes the migrated Button's resolved paint"
        }
    ]
