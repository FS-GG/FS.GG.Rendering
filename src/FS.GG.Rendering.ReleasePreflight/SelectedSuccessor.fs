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
        // This source-only entry cannot substitute for the separately verified retained-input case.
        // Historical admission contracts and their exact counts are not successor evidence.
        if intent = Publication then findings.Add "successor-original-custody-and-receiver-unjoined"
        if findings.Count = 0 then Ok selected else Error (List.ofSeq findings |> List.distinct |> List.sort)

    // Accepted original inputs. These are evidence coordinates, never executor or writer grants.
    let Producer = "4ede04be66cedffc8551cd14045bab714aa358a7"
    let PlanHash = "4bcbb1c756a799ab634e14d1fc9191ac51a8cd5d3537aec75eeab1c1d545deaa"
    let CustodyHash = "89ea6424cae4d06969f0a60394679e9892bd2b873fb46ebd281aa10616809720"
    let QualificationHash = "dcfe9c9cbed9fea8fc7776061996a027cfa63f0d9c9fe18b212aa23ab99c0cac"
    let ObserverHash = "dc78a6ddfba6be92c960aa1c1e0a4020650afd1ecb81664d0594b7b7b6231abb"
    let Templates = "72a54c27f49eff90f1c86bc68e7a8ed8bfd9a431"
    let Caller = "e9a1c49afaacee106999c007dedf2679dddf54d4"
    let Effect = "rendering-0324-original40737624380-a1"
    let coordinates () =
        {| schema="fsgg.rendering.publication-attempt/v1"; producerSha=Producer
           producerRun=40737624380L; producerAttempt=1; producerArtifact=12838338215L
           outerSha256="baaa17b807bd52c9297dafbfd9161ec84fec4842317bb1f5df4e3d40d837c8bf"; outerSizeBytes=15375023
           custodySha256=CustodyHash; planPath=selected.PlanPath; planSha256=PlanHash
           version=selected.Version; baselineVersion=selected.Baseline; effectId=Effect
           qualification={| run=40744519859L; attempt=1; artifact=12841756761L; callerSha=Caller
                            templatesSha=Templates; outerSizeBytes=16655428
                            outerSha256="638c4ef806b0c7393094c62ee2c301fd08e448e49776af4e61d3080dcc8a8d8a" |}
           browser={|run=40737624380L; attempt=1; artifact=12839410997L; sourceSha=Producer; outerSizeBytes=73036
                     outerSha256="a69dcb62518e955717526d79754c40e2e469f72f098c46f7d83b44de0d52ef2a"|} |}

    type Evidence = {
        Qualification: string; Observer: string; Before: string; After: string
        Assets: string; Custody: string; Preflight: string; Browser: string
        BrowserOriginal: string; ProducerJobs: string; LockHash: string; TemplateHash: string
    }
    type QualifiedSelection = private QualifiedSelection of string
    let private hash (raw: string) =
        System.Security.Cryptography.SHA256.HashData(Text.Encoding.UTF8.GetBytes raw)
        |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()
    let private text (root: JsonElement) (key: string) =
        root.GetProperty(key).GetString()
        |> Option.ofObj
        |> Option.defaultWith (fun () -> invalidArg "evidence" ("null-text-" + key))
    let private json raw = JsonDocument.Parse(raw: string)
    let private need condition reason = if not condition then invalidArg "evidence" reason
    let private archivedRows (rows: JsonElement) =
        rows.EnumerateArray() |> Seq.map(fun row -> text row "id",text row "sha256") |> Map.ofSeq

    /// Interpret the actual accepted receiver and producer evidence, without manufacturing old journey counts.
    let qualify (e: Evidence) =
        try
            need (hash e.Qualification=QualificationHash && hash e.Observer=ObserverHash && hash e.Custody=CustodyHash) "accepted-input-bytes"
            use qdoc=json e.Qualification
            use odoc=json e.Observer
            use bdoc=json e.Before
            use adoc=json e.After
            use pdoc=json e.Preflight
            use cdoc=json e.Custody
            use assetsDoc=json e.Assets
            use browserDoc=json e.Browser
            use originalDoc=json e.BrowserOriginal
            use jobsDoc=json e.ProducerJobs
            let q,o,b,a,p,c = qdoc.RootElement,odoc.RootElement,bdoc.RootElement,adoc.RootElement,pdoc.RootElement,cdoc.RootElement
            need (text q "schema"="fsgg.palette-candidate-qualification/1" && text q "disposition"="passed") "receiver-disposition"
            need (text q "renderingSource"=Producer && text q "renderingVersion"=selected.Version && text q "templatesSource"=Templates) "receiver-source"
            need (not(q.GetProperty("publication").GetBoolean()) && not(q.GetProperty("installedPublicAcceptance").GetBoolean())) "receiver-scope"
            need (q.GetProperty("originalArchiveCount").GetInt32()=19 && text q "originalCustodySha256"=CustodyHash && text q "releasePlanSha256"=PlanHash) "receiver-custody"
            for key,raw in ["observerSha256",e.Observer;"consumerBeforeSha256",e.Before;"consumerAfterSha256",e.After;"nativePreflightSha256",e.Preflight] do
                need (text q key=hash raw) ("receiver-hash-"+key)
            need (text q "studioLockSha256"=e.LockHash && text q "templatesArchiveSha256"=e.TemplateHash && text q "orcaVersion"="46.1") "receiver-lock-template-runtime"
            let cleanup=q.GetProperty("serverCleanup")
            need (text cleanup "signal"="SIGTERM" && cleanup.GetProperty("expectedExit").GetInt32()=143 && cleanup.GetProperty("actualExit").GetInt32()=143 && cleanup.GetProperty("reaped").GetBoolean()) "server-reap"
            need (text p "renderingInputSource"="palette-heading" && text p "requestedPhase"="full" && text p "disposition"="preflight-passed") "preflight-scope"
            let caller=p.GetProperty("caller")
            let producer=p.GetProperty("producer")
            need (text caller "repository"="FS-GG/FS.GG.Rendering" && text caller "revision"=Caller && text caller "run"="40744519859" && text caller "attempt"="1") "caller-identity"
            need (text producer "revision"=Producer && text producer "run"="40737624380" && text producer "attempt"="1" && text producer "artifact"="12838338215" && text producer "custodySha256"=CustodyHash && text producer "rawPlanSha256"=PlanHash) "producer-identity"
            need (text (p.GetProperty("templates")) "revision"=Templates) "templates-identity"
            need (c.GetProperty("archives").GetArrayLength()=19 && text c "version"=selected.Version && text c "releasePlanSha256"=PlanHash) "original19"
            let originals=archivedRows(c.GetProperty("archives"))
            let consumed=archivedRows(q.GetProperty("archives"))
            let expected=set ["FS.GG.UI.Scene";"FS.GG.UI.KeyboardInput";"FS.GG.UI.Scene.SvgBrowser"]
            need ((consumed |> Map.keys |> Set.ofSeq)=expected && (consumed |> Map.forall(fun id sha -> originals.TryFind id=Some sha))) "three-original-consumers"
            for receipt in [b;a] do
                need (archivedRows(receipt.GetProperty("archives"))=consumed && text receipt "packageFolder"=text a "packageFolder") "private-cache-before-after"
            // Initial explicit restore and build have different legitimate assets files. Only after is retained.
            need (text a "assetsSha256"=hash e.Assets) "retained-after-assets"
            let folders=assetsDoc.RootElement.GetProperty("packageFolders").EnumerateObject() |> Seq.map _.Name |> Seq.toList
            need (folders=[text a "packageFolder"] && folders.Head.EndsWith("/palette-candidate/receiver/.nuget/packages",StringComparison.Ordinal)) "private-cache-folder"
            let libraries=assetsDoc.RootElement.GetProperty("libraries").EnumerateObject() |> Seq.map _.Name |> Seq.filter(fun key -> key.StartsWith("FS.GG.UI.",StringComparison.OrdinalIgnoreCase)) |> Set.ofSeq
            need (libraries=(expected |> Set.map(fun id -> id+"/"+selected.Version))) "private-cache-closure"
            need (text o "schema"="fsgg.svg-input-orca-observation/v1" && text o "result"="passed" && text o "composition"="generated-svg-studio") "actual-observer"
            for group,fields in ["keyboard",["navigation";"activation";"selection";"palette";"escape";"focusRestoration"];"atspiActions",["properties";"twoInstancesRefusal";"mode";"helpClose";"rebind";"validationFeedback"];"workspace",["mode";"palette";"help";"rebind";"focusRestoration"]] do
                for field in fields do need (text (o.GetProperty(group)) field="passed") (group+"-"+field)
            let announcement=o.GetProperty("paletteAnnouncement")
            need (text announcement "name"="Command palette" && text announcement "role"="dialog" && text announcement "initialFocus"="heading" && announcement.GetProperty("speechAfter").GetInt64()>=0L) "fresh-palette-announcement"
            need ((o.GetProperty("screenReader")).GetProperty("speechOutputObserved").GetBoolean()) "actual-speech"
            let browser=browserDoc.RootElement
            need (text browser "result"="incomplete" && text (browser.GetProperty("externalDimensions").GetProperty("assistiveTechnology")) "result"="unavailable") "producer-at-scope"
            need ((browser.GetProperty("automatedDimensions").EnumerateObject() |> Seq.map _.Name |> Set.ofSeq)=set ["functional";"visual";"resizeDprTouchReflow";"exportReload";"nativeGeometryFontTiming"]) "producer-dimension-closure"
            need (browser.GetProperty("browsers").GetArrayLength()=3) "producer-browser-count"
            for dimension in browser.GetProperty("automatedDimensions").EnumerateObject() do need (text dimension.Value "result"="pass") "producer-automated-dimension"
            let families=browser.GetProperty("browsers").EnumerateArray() |> Seq.map(fun row -> need (text row "result"="pass") "producer-browser-result";text (row.GetProperty("environment")) "browserFamily") |> Set.ofSeq
            need (families=set ["chromium";"firefox";"webkit"]) "producer-browser-families"
            need (archivedRows(originalDoc.RootElement.GetProperty("archives"))=consumed && text originalDoc.RootElement "custodySha256"=CustodyHash && text originalDoc.RootElement "planSha256"=PlanHash) "producer-original-browser-inputs"
            need (jobsDoc.RootElement.GetProperty("total_count").GetInt32()=jobsDoc.RootElement.GetProperty("jobs").GetArrayLength()) "producer-job-inventory"
            for job in jobsDoc.RootElement.GetProperty("jobs").EnumerateArray() do
                need (job.GetProperty("run_id").GetInt64()=40737624380L && job.GetProperty("run_attempt").GetInt32()=1 && text job "head_sha"=Producer) "producer-job-source"
            let jobs=jobsDoc.RootElement.GetProperty("jobs").EnumerateArray() |> Seq.map(fun row -> text row "name",row) |> Map.ofSeq
            for name in ["Static release source and plan guard";"Package consumption (release-only)";"Generated product (template instantiation, release-only)";"Qualify complete successor archive custody without publication"] do need (text jobs.[name] "conclusion"="success") "producer-job-result"
            let steps=jobs.["Qualify complete successor archive custody without publication"].GetProperty("steps").EnumerateArray() |> Seq.map(fun row -> text row "name",text row "conclusion") |> Map.ofSeq
            for name in ["Pack the complete source candidate and verify custody";"ApiCompat against the selected published baseline";"Qualify all three browser families against original candidate archives without repacking"] do need (steps.TryFind name=Some "success") "producer-step-result"
            Ok (QualifiedSelection QualificationHash)
        with error -> Error ["qualified-successor:"+error.Message]

    /// Fresh observations authorize one selected attempt, never resolve a lost send or old Unknown.
    let admitWriter (QualifiedSelection _) completeCensus statuses effectiveWrite unresolvedSend =
        completeCensus && List.length statuses=19 && List.forall ((=) (Some 200)) statuses
        && effectiveWrite="unknown-before-attempt" && not unresolvedSend
