#load "../src/FS.GG.Rendering.ReleasePreflight/Plan.fs"
#load "../src/FS.GG.Rendering.ReleasePreflight/SelectedSuccessor.fs"

open System
open System.Text.Json
open FS.GG.Rendering.ReleasePreflight

// Transport envelope is emitted from exact Git source by the existing Python adapter.
// Interpretation and the source/publication distinction are owned by the maintained typed core.
try
    use input = JsonDocument.Parse(Console.In.ReadToEnd())
    let root = input.RootElement
    let text (name: string) = root.GetProperty(name).GetString()
    let source = root.GetProperty("sourcePackages").EnumerateArray() |> Seq.map(fun row ->
        { Id = row.GetProperty("id").GetString(); Kind = row.GetProperty("kind").GetString() }) |> Seq.toList
    let intent =
        match text "intent" with
        | "source-qualification" -> SelectedSuccessor.SourceQualification
        | "publication" -> SelectedSuccessor.Publication
        | _ -> failwith "successor-intent"
    match SelectedSuccessor.inspect source (text "sourceSha") (text "planPath") (text "version") intent (text "rawPlan") with
    | Ok _ -> printfn "{\"result\":\"pass\",\"scope\":\"source-qualification-only\",\"publicationAdmitted\":false}"
    | Error findings -> failwith (String.concat ";" findings)
with error ->
    eprintfn "selected-successor: %s" error.Message
    exit 1
