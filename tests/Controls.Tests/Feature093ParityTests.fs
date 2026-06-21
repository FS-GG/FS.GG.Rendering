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

let private box: Rect = { X = 10.0; Y = 40.0; Width = 284.0; Height = 92.0 }

let private mkText (theme: Theme) (x: float) (baseline: float) (size: float) (color: Color) (s: string) =
    Scene.textRun
        { Text = s
          Position = { X = x; Y = baseline }
          Font = { Family = theme.FontFamily; Size = size; Weight = None }
          Paint = Paint.fill color }

// ---- frozen pre-refactor procedural geometry (the parity oracle) ---------------------------
let private frozenButtonGeom (theme: Theme) (label: string) : Scene list =
    let h = 38.0
    let textW = (Scene.measureText label { Family = theme.FontFamily; Size = 15.0; Weight = None }).Width
    let w = min box.Width (max 70.0 (textW + 32.0))
    let by = box.Y + box.Height / 2.0 - h / 2.0
    [ Scene.rectangle (box.X, by, w, h) theme.Accent
      mkText theme (box.X + 16.0) (by + h / 2.0 + 5.0) 15.0 theme.Background label ]

let private frozenCheckboxGeom (theme: Theme) (on: bool) (label: string) : Scene list =
    let s = 28.0
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
    let text = [ mkText theme (bx + s + 10.0) (cy + 5.0) 13.0 theme.Foreground label ]
    fill @ tick @ text

let private themes = [ "light", Theme.light; "dark", Theme.dark ]

// Capture the frozen-oracle baselines to readiness/parity/<kind>.<theme>.scene.txt (T020).
let private captureBaselines () =
    let repoRoot =
        let rec find dir =
            if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then dir
            else match Directory.GetParent dir |> Option.ofObj with Some p -> find p.FullName | None -> dir
        find __SOURCE_DIRECTORY__
    let dir = Path.Combine(repoRoot, "specs", "093-visual-state-style-layer", "readiness", "parity")
    Directory.CreateDirectory dir |> ignore
    // Returns the list of baseline files written, so callers can assert they actually landed on disk.
    [ for (tname, theme) in themes do
        let buttonPath = Path.Combine(dir, sprintf "button.%s.normal.scene.txt" tname)
        File.WriteAllText(buttonPath, sprintf "%A" (frozenButtonGeom theme "Save"))
        yield buttonPath
        let checkPath = Path.Combine(dir, sprintf "check-box.%s.normal.scene.txt" tname)
        File.WriteAllText(checkPath, sprintf "%A" (frozenCheckboxGeom theme false "Enabled"))
        yield checkPath
        let checkedPath = Path.Combine(dir, sprintf "check-box-checked.%s.normal.scene.txt" tname)
        File.WriteAllText(checkedPath, sprintf "%A" (frozenCheckboxGeom theme true "Enabled"))
        yield checkedPath ]

[<Tests>]
let feature093ParityTests =
    testList "Feature 093 migration parity (SC-003/SC-007)" [

        test "T020 — capture the pre-refactor procedural baselines for the migrated kinds" {
            let written = captureBaselines ()
            // Falsifiable: fails if captureBaselines writes nothing, to the wrong path, or empty files.
            Expect.isNonEmpty written "captureBaselines wrote at least one frozen-oracle baseline"
            for path in written do
                Expect.isTrue (File.Exists path) (sprintf "baseline written under readiness/parity/: %s" path)
                Expect.isTrue (FileInfo(path).Length > 0L) (sprintf "baseline is non-empty: %s" path)
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
