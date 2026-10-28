namespace FS.GG.Rendering.ReleasePreflight

open System
open System.Text.Json
open System.Text.RegularExpressions

/// The selected successor has no publication attempt until real custody and receiver inputs exist.
/// Source qualification cannot manufacture a producer run, archive identity, or writer admission.
module SelectedSuccessor =
    type Intent = SourceQualification | Publication
    type Selection = private { PlanPath: string; Version: string; Baseline: string }

    let private selected = {
        PlanPath = "eng/release/palette-heading-0.32.4.json"
        Version = "0.32.4"
        Baseline = "0.32.3"
    }

    let inspect (source: SourcePackage list) (sourceSha: string) (planPath: string) (version: string) (intent: Intent) (raw: string) =
        let findings = ResizeArray<string>(Plan.inspect source version raw)
        if not (Regex.IsMatch(sourceSha, "^[0-9a-f]{40}$")) then findings.Add "successor-source-sha"
        if planPath <> selected.PlanPath || version <> selected.Version then findings.Add "successor-selection"
        try
            use document = JsonDocument.Parse(raw: string)
            let root = document.RootElement
            let mutable baseline = Unchecked.defaultof<JsonElement>
            if not (root.TryGetProperty("baselineVersion", &baseline)) || baseline.ValueKind <> JsonValueKind.String
               || baseline.GetString() <> selected.Baseline then findings.Add "successor-baseline"
            let mutable ready = Unchecked.defaultof<JsonElement>
            if not (root.TryGetProperty("publicationReady", &ready)) || ready.ValueKind <> JsonValueKind.False then
                findings.Add "successor-must-remain-unqualified"
        with
        | :? JsonException | :? InvalidOperationException -> findings.Add "successor-plan-shape"
        // Deliberately no qualified case: no retained0.32.4 producer/receiver tuple has been acquired.
        // Historical admission contracts and their exact counts are not successor evidence.
        if intent = Publication then findings.Add "successor-original-custody-and-receiver-unjoined"
        if findings.Count = 0 then Ok selected else Error (List.ofSeq findings |> List.distinct |> List.sort)
