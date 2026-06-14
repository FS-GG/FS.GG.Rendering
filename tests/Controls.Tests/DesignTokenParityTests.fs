module DesignTokenParityTests

// Feature 069 — behavior-preservation + typed-surface evidence (US2, US3).
//   * SC-002: the 10x2 value-parity table — each Theme.light/dark.<Field> equals its
//     pre-feature literal (data-model §4), and DesignTokens.Light/Dark.* resolve identically.
//   * SC-003: render parity — re-rendering against the token-derived themes is byte-identical
//     to the frozen pre-feature themes (deterministic readback hash).
//   * SC-007: the dependency guard — Controls.fsproj gains no new package reference.
//   * SC-008: the typed token surface is consumer-referenceable and additive-only.

open System.IO
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

let private repositoryRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then
            dir
        else
            match Directory.GetParent dir |> Option.ofObj with
            | Some p -> find p.FullName
            | None -> dir

    find __SOURCE_DIRECTORY__

// The frozen pre-feature literals (Theme.fs:7-26 before feature 069) — the parity oracle.
let private frozenLight : Theme =
    { Name = "light"
      Foreground = Colors.rgba 31uy 41uy 55uy 255uy
      Background = Colors.rgba 248uy 250uy 252uy 255uy
      Accent = Colors.rgba 37uy 99uy 235uy 255uy
      Danger = Colors.rgba 185uy 28uy 28uy 255uy
      Muted = Colors.rgba 100uy 116uy 139uy 255uy
      FontFamily = None
      FontSize = 14.0
      Density = 1.0
      CornerRadius = 4.0
      ContrastRequiredRatio = 4.5 }

let private frozenDark : Theme =
    { frozenLight with
        Name = "dark"
        Foreground = Colors.rgba 241uy 245uy 249uy 255uy
        Background = Colors.rgba 17uy 24uy 39uy 255uy
        Accent = Colors.rgba 96uy 165uy 250uy 255uy
        // Feature 083 (FR-010): dark.danger was the pre-069 `{light.danger}` alias (#b91c1c),
        // which measured only 2.74:1 on the dark background and failed the WCAG ContrastCheck
        // gate. It was deliberately brought into conformance with a Radix dark-red text step
        // (#ff9592, 8.42:1). The parity oracle tracks that intentional accessibility fix.
        Danger = Colors.rgba 255uy 149uy 146uy 255uy
        Muted = Colors.rgba 148uy 163uy 184uy 255uy }

[<Tests>]
let designTokenParityTests =
    testList "Feature 069 design-token parity" [
        // T019 (US2, SC-002): the 10x2 value-parity table.
        test "Theme.light is field-identical to the frozen pre-feature literal (SC-002)" {
            Expect.equal Theme.light frozenLight "Theme.light equals the pre-feature literal record"
        }

        test "Theme.dark is field-identical to the frozen pre-feature literal (SC-002)" {
            Expect.equal Theme.dark frozenDark "Theme.dark equals the pre-feature literal record"
        }

        test "every DesignTokens.Light value feeds the matching Theme.light field (SC-002)" {
            Expect.equal DesignTokens.Light.foreground frozenLight.Foreground "light foreground"
            Expect.equal DesignTokens.Light.background frozenLight.Background "light background"
            Expect.equal DesignTokens.Light.accent frozenLight.Accent "light accent"
            Expect.equal DesignTokens.Light.danger frozenLight.Danger "light danger"
            Expect.equal DesignTokens.Light.muted frozenLight.Muted "light muted"
            Expect.equal DesignTokens.Light.fontFamily frozenLight.FontFamily "light fontFamily"
            Expect.equal DesignTokens.Light.fontSize frozenLight.FontSize "light fontSize"
            Expect.equal DesignTokens.Light.density frozenLight.Density "light density"
            Expect.equal DesignTokens.Light.cornerRadius frozenLight.CornerRadius "light cornerRadius"
            Expect.equal DesignTokens.Light.contrastRequiredRatio frozenLight.ContrastRequiredRatio "light contrastRequiredRatio"
        }

        test "every DesignTokens.Dark value feeds the matching Theme.dark field (SC-002)" {
            Expect.equal DesignTokens.Dark.foreground frozenDark.Foreground "dark foreground"
            Expect.equal DesignTokens.Dark.background frozenDark.Background "dark background"
            Expect.equal DesignTokens.Dark.accent frozenDark.Accent "dark accent"
            Expect.equal DesignTokens.Dark.danger frozenDark.Danger "dark danger (alias-resolved)"
            Expect.equal DesignTokens.Dark.muted frozenDark.Muted "dark muted"
            Expect.equal DesignTokens.Dark.fontFamily frozenDark.FontFamily "dark fontFamily"
            Expect.equal DesignTokens.Dark.fontSize frozenDark.FontSize "dark fontSize"
            Expect.equal DesignTokens.Dark.density frozenDark.Density "dark density"
            Expect.equal DesignTokens.Dark.cornerRadius frozenDark.CornerRadius "dark cornerRadius"
            Expect.equal DesignTokens.Dark.contrastRequiredRatio frozenDark.ContrastRequiredRatio "dark contrastRequiredRatio"
        }

        // T021 (US2, SC-003): render parity against the frozen themes.
        test "the controls gallery renders byte-identically against the token-derived themes (SC-003)" {
            let screen =
                Stack.create [
                    Stack.children [
                        TextBlock.create [ TextBlock.text "Catalog" ]
                        Button.create [ Button.text "Run" ]
                        CheckBox.create [ CheckBox.text "Enabled" ]
                        ProgressBar.create [ ProgressBar.value 0.4 ]
                    ]
                ]

            let hashOf theme =
                let rendered = Control.render theme screen
                Expect.isEmpty rendered.Diagnostics "render produces no diagnostics"
                (Scene.renderReadbackEvidence { Width = 640; Height = 480 } rendered.Scene).DeterministicHash

            Expect.equal (hashOf Theme.light) (hashOf frozenLight) "token-derived light render matches the frozen light render"
            Expect.equal (hashOf Theme.dark) (hashOf frozenDark) "token-derived dark render matches the frozen dark render"
        }

        // T020 (US2, SC-007): the dependency guard.
        test "Controls.fsproj gains no new package reference (SC-007)" {
            let project = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Controls", "Controls.fsproj"))

            Expect.isFalse (project.Contains "<PackageReference") "Controls.fsproj declares no NuGet package reference"

            for forbidden in [ "Fable.Elmish"; "System.Text.Json"; "Newtonsoft.Json"; "FSharp.Data" ] do
                Expect.isFalse (project.Contains forbidden) (sprintf "Controls.fsproj does not reference %s" forbidden)
        }

        // T024 (US3, SC-008): a consumer references a generated token by typed name.
        test "a consumer can reference a generated token by typed name and it resolves to the DTCG value (SC-008)" {
            // DesignTokens.Light.accent is greppable and compiles; build a theme override from it.
            let branded = Theme.light |> Theme.withAccent DesignTokens.Light.accent
            Expect.equal branded.Accent (Colors.rgba 37uy 99uy 235uy 255uy) "the typed token resolves to the DTCG accent value"
        }
    ]
