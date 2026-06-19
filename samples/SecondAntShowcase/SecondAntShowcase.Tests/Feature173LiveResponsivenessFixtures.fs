module SecondAntShowcase.Tests.Feature173LiveResponsivenessFixtures

open System
open System.IO
open System.Text.Json
open SecondAntShowcase.App

let tempDir () =
    Path.Combine(Path.GetTempPath(), "second-antshowcase-feature173-" + Guid.NewGuid().ToString("N"))

let runHeadlessRequireLive () =
    let outDir = tempDir ()
    let code =
        Responsiveness.run
            [ "--script"; "representative"
              "--theme"; "light"
              "--all-interactive"
              "--require-live"
              "--out"; outDir
              "--json" ]

    code, outDir

let summaryFile outDir =
    Directory.GetFiles(outDir, "summary.json", SearchOption.AllDirectories)
    |> Array.exactlyOne

let environmentFile outDir =
    Directory.GetFiles(outDir, "environment.md", SearchOption.AllDirectories)
    |> Array.exactlyOne

let recordsFile outDir =
    Directory.GetFiles(outDir, "records.jsonl", SearchOption.AllDirectories)
    |> Array.exactlyOne

let summaryJson outDir =
    summaryFile outDir
    |> File.ReadAllText
    |> JsonDocument.Parse

let records outDir =
    recordsFile outDir
    |> File.ReadAllLines
    |> Array.toList

let firstRecord outDir =
    records outDir
    |> List.head
    |> JsonDocument.Parse

let arrayStrings (element: JsonElement) =
    element.EnumerateArray()
    |> Seq.map (fun value -> value.GetString())
    |> Seq.toList

let hasLimitationContaining (text: string) (root: JsonElement) =
    root.GetProperty("environmentLimitations").EnumerateArray()
    |> Seq.exists (fun item ->
        match item.GetString() with
        | null -> false
        | value -> value.Contains(text, StringComparison.Ordinal))

// SYNTHETIC: unit tests use deterministic JSON fixtures to isolate schema rules from a live GL session.
let syntheticMeasuredTotals = [ 12.0; 24.0; 48.0; 80.0; 96.0 ]
