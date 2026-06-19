module SecondAntShowcase.Tests.Feature172ResponsivenessFixtures

open System
open System.IO
open System.Text.Json

let tempDir () =
    Path.Combine(Path.GetTempPath(), "antshowcase-feature172-" + Guid.NewGuid().ToString("N"))

let summaryFile outDir =
    Directory.GetFiles(outDir, "summary.json", SearchOption.AllDirectories)
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

let getString (name: string) (element: JsonElement) =
    element.GetProperty(name).GetString()
