module SmokeTests

open System
open System.Diagnostics
open System.IO
open Expecto

let rec findRepositoryRoot (directory: string) =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || File.Exists(Path.Combine(directory, "build.fsx")) then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | Some parent -> findRepositoryRoot parent.FullName
        | None -> failwithf "Could not locate repository root from %s" directory

let repositoryRoot = findRepositoryRoot AppContext.BaseDirectory

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
            [ "BasicViewer", "samples/BasicViewer/BasicViewer.fsproj"
              "InteractiveViewer", "samples/InteractiveViewer/InteractiveViewer.fsproj"
              "ParityGallery", "samples/ParityGallery/ParityGallery.fsproj"
              "EffectsGallery", "samples/EffectsGallery/EffectsGallery.fsproj"
              "LayoutGraphGallery", "samples/LayoutGraphGallery/LayoutGraphGallery.fsproj"
              "ScreenshotGallery", "samples/ScreenshotGallery/ScreenshotGallery.fsproj"
              "DemoReel", "samples/DemoReel/DemoReel.fsproj"
              "KeyboardInputGallery", "samples/KeyboardInputGallery/KeyboardInputGallery.fsproj"
              "ChartsGallery", "samples/ChartsGallery/ChartsGallery.fsproj"
              "DataGridGallery", "samples/DataGridGallery/DataGridGallery.fsproj"
              "ControlsGallery", "samples/ControlsGallery/ControlsGallery.fsproj" ]
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
            [ "ControlsGallery",
              "samples/ControlsGallery/ControlsGallery.fsproj",
              [ "LineChart.create"
                "GraphView.create"
                "DataGrid.create"
                "ControlsElmish.program"
                "Keyboard.update"
                "ControlRuntime.update"
                "printfn \"sample=ControlsGallery\"" ]
              "ChartsGallery",
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
                Expect.isFalse (projectContent.Contains("FS.Skia.UI.Charts", StringComparison.Ordinal)) $"{sample} does not use Charts package"
                Expect.isFalse (projectContent.Contains(@"..\..\src\Charts\Charts.fsproj", StringComparison.Ordinal)) $"{sample} does not use removed Charts project"

                requiredSource
                |> List.iter (fun required ->
                    Expect.stringContains source required $"{sample} source includes {required}"))
        }
    ]
