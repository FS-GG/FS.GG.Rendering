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
    if root.GetProperty("intent").GetString()="coordinates" then
        printfn "%s" (JsonSerializer.Serialize(SelectedSuccessor.coordinates()))
        exit 0
    if root.GetProperty("intent").GetString()="qualified-publication" then
        let value=root.GetProperty("evidence")
        let field name=value.GetProperty(name: string).GetString()
        let evidence : SelectedSuccessor.Evidence = {
            Qualification=field "Qualification"; Observer=field "Observer"; Before=field "Before"; After=field "After"
            Assets=field "Assets"; Custody=field "Custody"; Preflight=field "Preflight"; Browser=field "Browser"
            BrowserOriginal=field "BrowserOriginal"; ProducerJobs=field "ProducerJobs"; LockHash=field "LockHash"; TemplateHash=field "TemplateHash" }
        match SelectedSuccessor.qualify evidence with
        | Ok _ -> printfn "{\"result\":\"pass\",\"scope\":\"qualified-retained-inputs\",\"publicationAdmitted\":false}"; exit 0
        | Error findings -> failwith (String.concat ";" findings)
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
