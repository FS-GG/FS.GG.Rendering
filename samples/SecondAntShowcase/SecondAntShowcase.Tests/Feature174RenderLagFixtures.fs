module SecondAntShowcase.Tests.Feature174RenderLagFixtures

open System
open System.IO
open System.Text.Json
open SecondAntShowcase.App

let tempDir () =
    Path.Combine(Path.GetTempPath(), "second-antshowcase-feature174-" + Guid.NewGuid().ToString("N"))

let withForcedSubstitute action =
    let previous = Environment.GetEnvironmentVariable "FS_GG_RENDER_LAG_FORCE_SUBSTITUTE"

    try
        Environment.SetEnvironmentVariable("FS_GG_RENDER_LAG_FORCE_SUBSTITUTE", "1")
        action ()
    finally
        Environment.SetEnvironmentVariable("FS_GG_RENDER_LAG_FORCE_SUBSTITUTE", previous)

let runProbe scenario =
    let outDir = tempDir ()

    let code =
        withForcedSubstitute
            (fun () ->
                RenderLagProbe.run
                    [ "--scenario"; scenario
                      "--theme"; "light"
                      "--out"; outDir ])

    code, outDir

let summaryFile outDir =
    Directory.GetFiles(outDir, "summary.json", SearchOption.AllDirectories)
    |> Array.exactlyOne

let phaseRecordsFile outDir =
    Directory.GetFiles(outDir, "phase-records.jsonl", SearchOption.AllDirectories)
    |> Array.exactlyOne

let traceFile outDir =
    Directory.GetFiles(outDir, "trace.log", SearchOption.AllDirectories)
    |> Array.exactlyOne

let summaryMarkdownFile outDir =
    Directory.GetFiles(outDir, "summary.md", SearchOption.AllDirectories)
    |> Array.exactlyOne

let runRoot outDir =
    summaryFile outDir
    |> Directory.GetParent
    |> fun parent ->
        match parent with
        | null -> failwith "summary has no run directory"
        | directory -> directory.FullName

let summaryJson outDir =
    summaryFile outDir
    |> File.ReadAllText
    |> JsonDocument.Parse

let firstPhaseRecord outDir =
    phaseRecordsFile outDir
    |> File.ReadAllLines
    |> Array.head
    |> JsonDocument.Parse

let arrayStrings (element: JsonElement) =
    element.EnumerateArray()
    |> Seq.map (fun value -> value.GetString())
    |> Seq.toList

