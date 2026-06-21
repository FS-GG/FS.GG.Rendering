module Feature126TokenTaxonomyTests

// Feature 126 (Workstream F, F1) — the Ant-derived design-token taxonomy.
//   * US1/SC-001/SC-005: every layer (seed/map/alias/component) + supplementary group
//     (space/density/type/elevation) is nameable and resolves to its expected value, for both
//     Light and Dark where the layer is mode-specific (reached via InternalsVisibleTo).
//   * US3/FR-003/FR-010: the committed DesignTokensExt.fs is in lock-step with the DTCG source
//     (the generator's --check reports no drift) and is marked generated.

open System
open System.Diagnostics
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

[<Tests>]
let feature126TokenTaxonomyTests =
    testList "Feature 126 token taxonomy" [
        // US1 (SC-001/SC-005): name a token from EVERY layer + supplementary group.
        test "every taxonomy layer is nameable and resolves to its expected value (SC-001)" {
            // seed
            Expect.equal DesignTokensExt.Seed.colorPrimary (Colors.rgba 22uy 119uy 255uy 255uy) "seed.colorPrimary"
            Expect.equal DesignTokensExt.Seed.controlHeight 32.0 "seed.controlHeight"
            // map (both modes)
            Expect.equal DesignTokensExt.Map.Light.colorText (Colors.rgba 31uy 41uy 55uy 255uy) "map.light.colorText"
            Expect.equal DesignTokensExt.Map.Dark.colorText (Colors.rgba 241uy 245uy 249uy 255uy) "map.dark.colorText"
            // alias (dotted key -> camelCase; both modes)
            Expect.equal DesignTokensExt.Alias.Light.textDefault (Colors.rgba 31uy 41uy 55uy 255uy) "alias.light.text.default"
            Expect.equal DesignTokensExt.Alias.Dark.textDefault (Colors.rgba 241uy 245uy 249uy 255uy) "alias.dark.text.default"
            // component
            Expect.equal DesignTokensExt.Component.Button.primaryBg (Colors.rgba 22uy 119uy 255uy 255uy) "component.button.primaryBg"
            // supplementary groups
            Expect.equal DesignTokensExt.Space.md 16.0 "space.md"
            Expect.equal DesignTokensExt.Density.comfortable 1.0 "density.comfortable (equals today's default)"
            Expect.equal DesignTokensExt.Type.Body.fontSize 14.0 "type.body.fontSize"
            Expect.equal DesignTokensExt.Elevation.medium "0 4 12 #00000014" "elevation.medium"
        }

        // US1 (FR-007): map/alias define a Light and a Dark variant; for a colour-mode token they differ.
        test "map and alias provide both Light and Dark variants (FR-007)" {
            Expect.notEqual
                DesignTokensExt.Map.Light.colorBgContainer
                DesignTokensExt.Map.Dark.colorBgContainer
                "map.colorBgContainer differs by mode"
            Expect.notEqual
                DesignTokensExt.Alias.Light.surfaceContainer
                DesignTokensExt.Alias.Dark.surfaceContainer
                "alias.surface.container differs by mode"
        }

        // US2 (FR-011): Ant seed informs structure but existing primitives are preserved — the seed's
        // success/warning/error align to the existing Light token values (no value drift).
        test "seed functional colours align to the existing Light primitives (FR-011)" {
            Expect.equal DesignTokensExt.Seed.colorSuccess DesignTokens.Light.success "seed.colorSuccess == DesignTokens.Light.success"
            Expect.equal DesignTokensExt.Seed.colorWarning DesignTokens.Light.warning "seed.colorWarning == DesignTokens.Light.warning"
            Expect.equal DesignTokensExt.Seed.colorTextBase DesignTokens.Light.foreground "seed.colorTextBase == DesignTokens.Light.foreground"
        }

        // US3 (FR-003/FR-010): the committed generated module is in lock-step with the DTCG source.
        test "DesignTokensExt is up to date with the DTCG source — generator --check passes (FR-010)" {
            let psi = ProcessStartInfo("dotnet", "fsi scripts/generate-design-tokens.fsx --check")
            psi.WorkingDirectory <- repositoryRoot
            psi.RedirectStandardError <- true
            psi.RedirectStandardOutput <- true
            psi.UseShellExecute <- false
            use p = Process.Start psi
            let stderr = p.StandardError.ReadToEnd()
            p.WaitForExit()
            Expect.equal p.ExitCode 0 (sprintf "generate-design-tokens.fsx --check reported drift: %s" stderr)
        }

        // US3 (FR-010): the artifact is marked generated and names its source.
        test "DesignTokensExt.fs is marked generated and names the DTCG source (FR-010)" {
            let path = Path.Combine(repositoryRoot, "src", "DesignSystem", "DesignTokensExt.fs")
            let text = File.ReadAllText path
            Expect.stringStarts text "// GENERATED — do not edit." "carries the generated marker"
            Expect.stringContains text "design-tokens.tokens.json" "references the DTCG source"
        }
    ]
