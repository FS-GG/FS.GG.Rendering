module SmokeTests

open System
open System.Diagnostics
open System.IO
open Expecto
open FS.GG.TestSupport

let repositoryRoot = RepositoryRoot.value

// The parity samples/ tree was not imported at migration Stage R4. Each parity smoke test below
// exercises a parity samples/* project, so when that tree is absent they skip-with-reason (Ignored)
// rather than fail — self-restoring to real assertions once the parity samples are imported.
//
// Feature 123 added a *different*, package-consuming sample at samples/ControlsGallery (a consumer
// of the packed FS.GG.UI.* surface, not a src/ project-reference parity sample). It has its own
// dedicated, GL-free contract test (`controlsGalleryConsumerContract` below). To keep the parity
// checks dormant until Stage R4, gate them on a parity marker (BasicViewer) rather than the mere
// existence of samples/ — which feature 123 now populates.
let paritySamplesPresent = Directory.Exists(Path.Combine(repositoryRoot, "samples", "BasicViewer"))
let skipIfNoSamples () =
    if not paritySamplesPresent then
        skiptest "parity samples/ not imported (migration Stage R4 pending) — smoke contract checks skipped, not failed"

let readinessPath segments =
    let activeFeature = Path.Combine(repositoryRoot, "specs", "011-controls-boundary-refactor")
    let historicalFeature = Path.Combine(repositoryRoot, "specs", "004-keyboard-state-display")
    let readinessRoot =
        if Directory.Exists(Path.Combine(activeFeature, "readiness")) then
            Path.Combine(activeFeature, "readiness")
        elif File.Exists(Path.Combine(historicalFeature, "spec.md")) then
            Path.Combine(historicalFeature, "readiness")
        else
            Path.Combine(repositoryRoot, "readiness")

    Path.Combine(Array.ofList (readinessRoot :: segments))

let runProcess (fileName: string) (arguments: string) =
    let startInfo = ProcessStartInfo(fileName, arguments)
    startInfo.WorkingDirectory <- repositoryRoot
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.UseShellExecute <- false

    use proc =
        match Process.Start startInfo |> Option.ofObj with
        | Some proc -> proc
        | None -> failwithf "Could not start %s %s" fileName arguments

    let stdoutTask = proc.StandardOutput.ReadToEndAsync()
    let stderrTask = proc.StandardError.ReadToEndAsync()
    let timeoutMilliseconds = 120000

    if proc.WaitForExit(timeoutMilliseconds) then
        let stdout = stdoutTask.GetAwaiter().GetResult()
        let stderr = stderrTask.GetAwaiter().GetResult()
        proc.ExitCode, stdout, stderr
    else
        try
            proc.Kill(true)
        with _ ->
            ()

        let stdout =
            if stdoutTask.Wait(1000) then
                stdoutTask.Result
            else
                ""

        let stderr =
            if stderrTask.Wait(1000) then
                stderrTask.Result
            else
                ""

        failtestf
            "%s %s timed out after %d ms. stdout:\n%s\nstderr:\n%s"
            fileName
            arguments
            timeoutMilliseconds
            stdout
            stderr

[<Tests>]
let smokeContractTests =
    testList "Sample smoke contract" [
        test "all parity samples expose contract smoke entry points" {
            skipIfNoSamples ()
            [ "BasicViewer", "samples/BasicViewer/BasicViewer.fsproj"
              "InteractiveViewer", "samples/InteractiveViewer/InteractiveViewer.fsproj"
              "ParityGallery", "samples/ParityGallery/ParityGallery.fsproj"
              "EffectsGallery", "samples/EffectsGallery/EffectsGallery.fsproj"
              "LayoutGraphGallery", "samples/LayoutGraphGallery/LayoutGraphGallery.fsproj"
              "ScreenshotGallery", "samples/ScreenshotGallery/ScreenshotGallery.fsproj"
              "DemoReel", "samples/DemoReel/DemoReel.fsproj"
              "KeyboardInputGallery", "samples/KeyboardInputGallery/KeyboardInputGallery.fsproj"
              "ChartsGallery", "samples/ChartsGallery/ChartsGallery.fsproj"
              "DataGridGallery", "samples/DataGridGallery/DataGridGallery.fsproj" ]
            |> List.iter (fun (sample, project) ->
                let projectPath = Path.Combine(repositoryRoot, project)
                let programPath = Path.Combine(Path.GetDirectoryName projectPath |> Option.ofObj |> Option.defaultValue repositoryRoot, "Program.fs")
                let source = File.ReadAllText programPath

                Expect.isTrue (File.Exists projectPath) $"{sample} project exists"
                Expect.stringContains source "--contract-smoke" $"{sample} has a contract smoke argument"
                Expect.stringContains source "status=ok" $"{sample} reports smoke success"
                Expect.stringContains source $"sample={sample}" $"{sample} identifies itself")
        }

        test "KeyboardInputGallery contract smoke captures keyboard state display evidence" {
            skipIfNoSamples ()
            let exitCode, stdout, stderr =
                runProcess "dotnet" "run --project samples/KeyboardInputGallery/KeyboardInputGallery.fsproj --no-build --no-restore -- --contract-smoke"

            let evidencePath =
                readinessPath [ "sample-smoke"; "keyboard-input-gallery-state-display.txt" ]

            Path.GetDirectoryName evidencePath
            |> Option.ofObj
            |> Option.iter (Directory.CreateDirectory >> ignore)

            let evidence = stdout + stderr
            File.WriteAllText(evidencePath, evidence)

            Expect.equal exitCode 0 "KeyboardInputGallery contract smoke exits successfully"
            Expect.stringContains stdout "status=ok" "sample reports success"
            Expect.stringContains stdout "sample=KeyboardInputGallery" "sample identifies itself"
            Expect.stringContains stdout "compact-labels=" "smoke includes compact display model evidence"
            Expect.stringContains stdout "expanded-stack=" "smoke includes expanded display model evidence"
            Expect.stringContains stdout "hidden=KeyboardStateDisplayHidden" "smoke includes hidden display evidence"
            Expect.stringContains stdout "TextRunElement" "smoke includes rendered scene text primitive"
        }

        test "Controls boundary gallery contract smoke sources cover Controls-owned chart DataGrid and adapter paths" {
            skipIfNoSamples ()
            [ "ChartsGallery",
              "samples/ChartsGallery/ChartsGallery.fsproj",
              [ "LineChart.create"
                "BarChart.create"
                "printfn \"sample=ChartsGallery\""
                "selection-owned-by-model=" ]
              "DataGridGallery",
              "samples/DataGridGallery/DataGridGallery.fsproj",
              [ "DataGrid.create"
                "DataGrid.update"
                "printfn \"sample=DataGridGallery\""
                "state-owned-by-model="
                "selection-effects="
                "focus-effects=" ] ]
            |> List.iter (fun (sample, project, requiredSource) ->
                let projectPath = Path.Combine(repositoryRoot, project)
                let sourcePath = Path.Combine(Path.GetDirectoryName projectPath |> Option.ofObj |> Option.defaultValue repositoryRoot, "Program.fs")
                let projectContent = File.ReadAllText projectPath
                let source = File.ReadAllText sourcePath

                Expect.stringContains source "--contract-smoke" $"{sample} exposes contract smoke"
                Expect.stringContains source "status=ok" $"{sample} reports smoke success"
                Expect.stringContains source $"sample={sample}" $"{sample} identifies itself"
                Expect.stringContains projectContent @"..\..\src\Controls\Controls.fsproj" $"{sample} uses Controls project"
                Expect.isFalse (projectContent.Contains("FS.GG.UI.Charts", StringComparison.Ordinal)) $"{sample} does not use Charts package"
                Expect.isFalse (projectContent.Contains(@"..\..\src\Charts\Charts.fsproj", StringComparison.Ordinal)) $"{sample} does not use removed Charts project"

                requiredSource
                |> List.iter (fun required ->
                    Expect.stringContains source required $"{sample} source includes {required}"))
        }
    ]

// Feature 123 — Controls Gallery Showcase. A package-consuming sample kept outside the main
// solution and the default test tier (research R8). These checks read source only: they keep the
// gallery honest in CI WITHOUT depending on a display/GL (FR-016) or on the packed packages being
// restored — the gallery's own deterministic Expecto suite covers behavior.
[<Tests>]
let controlsGalleryConsumerContract =
    let galleryRoot = Path.Combine(repositoryRoot, "samples", "ControlsGallery")
    testList "Controls Gallery (feature 123) consumer-path contract" [
        test "the gallery's three projects exist" {
            [ "ControlsGallery.Core/ControlsGallery.Core.fsproj"
              "ControlsGallery.App/ControlsGallery.App.fsproj"
              "ControlsGallery.Tests/ControlsGallery.Tests.fsproj" ]
            |> List.iter (fun proj ->
                Expect.isTrue (File.Exists(Path.Combine(galleryRoot, proj))) $"{proj} exists")
        }

        test "consumes packed FS.GG.UI.* packages only — no src/ project references (FR-013/SC-005)" {
            let projects = Directory.GetFiles(galleryRoot, "*.fsproj", SearchOption.AllDirectories)
            Expect.isGreaterThan projects.Length 0 "gallery projects found"
            projects
            |> Array.iter (fun f ->
                let content = File.ReadAllText f
                Expect.isFalse (content.Contains(@"..\..\src\", StringComparison.Ordinal)) $"{Path.GetFileName f}: no Windows-style src project ref"
                Expect.isFalse (content.Contains("/src/", StringComparison.Ordinal)) $"{Path.GetFileName f}: no posix src project ref"
                Expect.isTrue (content.Contains("FS.GG.UI.Controls", StringComparison.Ordinal)) $"{Path.GetFileName f}: references the packed Controls package")
        }

        test "nuget.config points at the local packed feed (the consumer path, SC-005)" {
            let cfg = Path.Combine(galleryRoot, "nuget.config")
            Expect.isTrue (File.Exists cfg) "nuget.config exists"
            Expect.stringContains (File.ReadAllText cfg) "nuget-local" "local packed feed configured"
        }

        test "the page registry references all 10 page ids (52→10 coverage source)" {
            let pages = File.ReadAllText(Path.Combine(galleryRoot, "ControlsGallery.Core", "Pages.fs"))
            [ "display-typography"; "buttons"; "text-numeric-input"; "selection-toggles"
              "data-collections"; "layout-containers"; "navigation-menus"; "overlays-feedback"
              "charts"; "pointer-custom" ]
            |> List.iter (fun pid -> Expect.stringContains pages pid $"registry references page {pid}")
        }
    ]
