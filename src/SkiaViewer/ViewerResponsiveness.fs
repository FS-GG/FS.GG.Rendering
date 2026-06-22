namespace FS.GG.UI.SkiaViewer

open System
open System.IO
open System.Globalization
open System.Text.Json

/// Feature 187 (US1): responsiveness latency/summary/json/markdown bodies + the 4 responsiveness
/// token encoders, carved out of the `Viewer` module (bodies out, contracts stay). No `.fsi`;
/// `module internal` keeps it off the public surface (FR-007). Pure encoders over Viewer.Types
/// records — verbatim moves, so Feature167 responsiveness-summary output is byte-identical.
/// `module Viewer` keeps byte-identical public delegators to these.
module internal ViewerResponsiveness =
    let responsivenessInputKindToken kind =
        match kind with
        | ViewerResponsivenessInputKind.PointerMove -> "pointer-move"
        | ViewerResponsivenessInputKind.PointerDiscrete -> "pointer-discrete"
        | ViewerResponsivenessInputKind.KeyDown -> "key-down"
        | ViewerResponsivenessInputKind.KeyUp -> "key-up"
        | ViewerResponsivenessInputKind.Wheel -> "wheel"
        | ViewerResponsivenessInputKind.Resize -> "resize"
        | ViewerResponsivenessInputKind.Tick -> "tick"
        | ViewerResponsivenessInputKind.Lifecycle -> "lifecycle"

    let responsivenessVisibleResponseToken response =
        match response with
        | ViewerResponsivenessVisibleResponse.PresentedFrame -> "presented-frame"
        | ViewerResponsivenessVisibleResponse.NoVisibleResponse -> "no-visible-response"
        | ViewerResponsivenessVisibleResponse.Failed -> "failed"
        | ViewerResponsivenessVisibleResponse.EnvironmentLimited -> "environment-limited"
        | ViewerResponsivenessVisibleResponse.NotRun -> "not-run"

    let responsivenessEnvironmentStatusToken status =
        match status with
        | ViewerResponsivenessEnvironmentStatus.Measured -> "measured"
        | ViewerResponsivenessEnvironmentStatus.MissingBoundary -> "missing-boundary"
        | ViewerResponsivenessEnvironmentStatus.LowPrecisionTimestamp -> "low-precision-timestamp"
        | ViewerResponsivenessEnvironmentStatus.NonMonotonicTimestamp -> "non-monotonic-timestamp"
        | ViewerResponsivenessEnvironmentStatus.NoVisibleSurface -> "no-visible-surface"
        | ViewerResponsivenessEnvironmentStatus.HeadlessSubstitute -> "headless-substitute"
        | ViewerResponsivenessEnvironmentStatus.WriteFailed -> "write-failed"
        | ViewerResponsivenessEnvironmentStatus.Failed -> "failed"

    let responsivenessReadinessToken readiness =
        match readiness with
        | ViewerResponsivenessReadiness.Accepted -> "accepted"
        | ViewerResponsivenessReadiness.Rejected -> "rejected"
        | ViewerResponsivenessReadiness.Blocked -> "blocked"
        | ViewerResponsivenessReadiness.Incomplete -> "incomplete"
        | ViewerResponsivenessReadiness.EnvironmentLimited -> "environment-limited"
        | ViewerResponsivenessReadiness.Failed -> "failed"

    let createResponsivenessRunId () =
        let stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
        let suffix = Guid.NewGuid().ToString("N").Substring(0, 6)
        $"resp-{stamp}-{suffix}"

    let nullableFloat (value: TimeSpan option) : Nullable<float> =
        match value with
        | Some duration -> Nullable duration.TotalMilliseconds
        | None -> Nullable<float>()

    let nullableInt (value: int option) : Nullable<int> =
        match value with
        | Some n -> Nullable n
        | None -> Nullable<int>()

    let nullableInt64 (value: int64 option) : Nullable<int64> =
        match value with
        | Some n -> Nullable n
        | None -> Nullable<int64>()

    let jsonOptions =
        JsonSerializerOptions(WriteIndented = false)

    let jsonOptionsIndented =
        JsonSerializerOptions(WriteIndented = true)

    let jsonNull =
        use document = JsonDocument.Parse("null")
        document.RootElement.Clone()

    let optionJsonString (value: string option) : JsonElement =
        match value with
        | Some text -> JsonSerializer.SerializeToElement(text, jsonOptions)
        | None -> jsonNull

    let latencyRecordToJsonLine (latency: ViewerLatencyRecord) =
        let timing = latency.PhaseTiming

        let dirty: JsonElement =
            match latency.DirtyRegion with
            | Some region ->
                JsonSerializer.SerializeToElement(
                    {| dirtyRectCount = nullableInt region.DirtyRectCount
                       dirtyArea = nullableInt region.DirtyArea
                       repaintedNodeCount = nullableInt region.RepaintedNodeCount
                       status = responsivenessEnvironmentStatusToken region.Status |},
                    jsonOptions
                )
            | None -> jsonNull

        JsonSerializer.Serialize(
            {| recordId = latency.RecordId
               runId = latency.RunId
               inputSequenceId = latency.InputSequenceId
               inputKind = responsivenessInputKindToken latency.InputKind
               inputName = optionJsonString latency.InputName
               page = optionJsonString latency.Page
               controlGroup = optionJsonString latency.ControlGroup
               receiptTimestamp = latency.ReceiptTimestamp
               queueDepthAtReceipt = latency.QueueDepthAtReceipt
               queueDepthAtDrain = latency.QueueDepthAtDrain
               coalescedMovementCount = latency.CoalescedMovementCount
               productMessageCount = latency.ProductMessageCount
               productStateChanged = latency.ProductStateChanged
               runtimeStateChanged = latency.RuntimeStateChanged
               visibleResponse = responsivenessVisibleResponseToken latency.VisibleResponse
               presentedFrameId = nullableInt64 latency.PresentedFrameId
               environmentStatus = responsivenessEnvironmentStatusToken latency.EnvironmentStatus
               phaseTiming =
                {| receiptDurationMs = nullableFloat timing.ReceiptDuration
                   queueDelayMs = nullableFloat timing.QueueDelay
                   routingDurationMs = nullableFloat timing.RoutingDuration
                   updateDurationMs = nullableFloat timing.UpdateDuration
                   viewDurationMs = nullableFloat timing.ViewDuration
                   retainedStepDurationMs = nullableFloat timing.RetainedStepDuration
                   layoutDurationMs = nullableFloat timing.LayoutDuration
                   textDurationMs = nullableFloat timing.TextDuration
                   paintDurationMs = nullableFloat timing.PaintDuration
                   presentDurationMs = nullableFloat timing.PresentDuration
                   totalInputToVisibleMs = nullableFloat timing.TotalInputToVisibleDuration |}
               dirtyRegion = dirty
               longFrame = latency.LongFrame
               diagnostics = latency.Diagnostics |},
            jsonOptions
        )

    let percentile percentileValue (values: TimeSpan list) =
        match values |> List.sortBy _.Ticks with
        | [] -> None
        | sorted ->
            let rank =
                Math.Ceiling((percentileValue / 100.0) * float sorted.Length)
                |> int
                |> max 1
                |> min sorted.Length

            Some sorted.[rank - 1]

    let maxTime (values: TimeSpan list) =
        values |> List.sortBy (fun value -> value.Ticks) |> List.tryLast

    let failedBudget kind scope inputKind measured budget : ViewerResponsivenessFailedBudget =
        { Kind = kind
          Scope = scope
          InputKind = inputKind
          Measured = measured
          Budget = budget }

    let firstFailedBudget (scope: string) (budget: ViewerResponsivenessBudget) (records: ViewerLatencyRecord list) =
        let environmentBoundaryFailure =
            records
            |> List.tryFind (fun record ->
                record.EnvironmentStatus <> ViewerResponsivenessEnvironmentStatus.Measured
                || record.VisibleResponse = ViewerResponsivenessVisibleResponse.EnvironmentLimited
                || record.VisibleResponse = ViewerResponsivenessVisibleResponse.NotRun)
            |> Option.map (fun record ->
                failedBudget
                    "environment-boundary"
                    (record.Page |> Option.orElse (Some scope))
                    (Some record.InputKind)
                    (TimeSpan.FromMilliseconds 1.0)
                    TimeSpan.Zero)

        let receiptDurations =
            records |> List.choose (fun record -> record.PhaseTiming.ReceiptDuration)

        let visibleDurations =
            records
            |> List.choose (fun record ->
                match record.VisibleResponse, record.PhaseTiming.TotalInputToVisibleDuration with
                | ViewerResponsivenessVisibleResponse.PresentedFrame, Some duration -> Some duration
                | _ -> None)

        match environmentBoundaryFailure with
        | Some failure -> Some failure
        | None ->
            match percentile 95.0 receiptDurations with
            | Some p95 when p95 > budget.InputReceiptP95 ->
                Some(failedBudget "input-receipt-p95" (Some scope) None p95 budget.InputReceiptP95)
            | _ ->
                match maxTime receiptDurations with
                | Some maxReceipt when maxReceipt > budget.InputReceiptMax ->
                    Some(failedBudget "input-receipt-max" (Some scope) None maxReceipt budget.InputReceiptMax)
                | _ ->
                    match percentile 95.0 visibleDurations with
                    | Some p95 when p95 > budget.InputToVisibleP95 ->
                        Some(failedBudget "input-to-visible-p95" (Some scope) None p95 budget.InputToVisibleP95)
                    | _ ->
                        match maxTime visibleDurations with
                        | Some maxVisible when maxVisible > budget.InputToVisibleMax ->
                            Some(failedBudget "input-to-visible-max" (Some scope) None maxVisible budget.InputToVisibleMax)
                        | _ ->
                            records
                            |> List.tryFind (fun record -> record.LongFrame)
                            |> Option.map (fun record ->
                                let measured =
                                    [ record.PhaseTiming.RetainedStepDuration
                                      record.PhaseTiming.PaintDuration
                                      record.PhaseTiming.PresentDuration
                                      record.PhaseTiming.TotalInputToVisibleDuration ]
                                    |> List.choose id
                                    |> maxTime
                                    |> Option.defaultValue (budget.LongFrameThreshold + TimeSpan.FromMilliseconds 1.0)

                                failedBudget "long-frame" (record.Page |> Option.orElse (Some scope)) (Some record.InputKind) measured budget.LongFrameThreshold)

    let dominantPhase (timing: ViewerResponsivenessPhaseTiming) =
        [ "receipt", timing.ReceiptDuration
          "queue", timing.QueueDelay
          "routing", timing.RoutingDuration
          "update", timing.UpdateDuration
          "view", timing.ViewDuration
          "retained-step", timing.RetainedStepDuration
          "layout", timing.LayoutDuration
          "text", timing.TextDuration
          "paint", timing.PaintDuration
          "present", timing.PresentDuration ]
        |> List.choose (fun (name, value) -> value |> Option.map (fun duration -> name, duration))
        |> List.sortByDescending (fun (_, duration) -> duration.Ticks)
        |> List.tryHead
        |> Option.map fst

    let summarizeResponsivenessRecords
        (runId: string)
        (scope: string)
        (recordsPath: string)
        (startedUtc: DateTimeOffset)
        (completedUtc: DateTimeOffset)
        (budget: ViewerResponsivenessBudget)
        (records: ViewerLatencyRecord list)
        : ViewerResponsivenessSummary
        =
        let environmentLimitations =
            records
            |> List.choose (fun record ->
                if record.EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.Measured then
                    None
                else
                    Some $"{responsivenessEnvironmentStatusToken record.EnvironmentStatus}:{record.RecordId}")
            |> List.distinct

        let firstFailed = firstFailedBudget scope budget records

        let groups =
            records
            |> List.groupBy (fun record -> record.Page, record.InputKind, record.ControlGroup)
            |> List.map (fun ((page, inputKind, controlGroup), groupRecords) ->
                let measured =
                    groupRecords
                    |> List.choose (fun record ->
                        match record.VisibleResponse, record.PhaseTiming.TotalInputToVisibleDuration with
                        | ViewerResponsivenessVisibleResponse.PresentedFrame, Some duration -> Some duration
                        | _ -> None)

                let p95 = percentile 95.0 measured
                let longFrames = groupRecords |> List.filter (fun record -> record.LongFrame) |> List.length

                let readiness =
                    if
                        groupRecords
                        |> List.exists (fun record ->
                            record.VisibleResponse = ViewerResponsivenessVisibleResponse.Failed
                            || record.EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.Failed
                            || record.EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.WriteFailed)
                    then
                        ViewerResponsivenessReadiness.Failed
                    elif
                        p95 |> Option.exists (fun value -> value > budget.InputToVisibleP95)
                        || measured |> maxTime |> Option.exists (fun value -> value > budget.InputToVisibleMax)
                        || longFrames > 0
                    then
                        ViewerResponsivenessReadiness.Rejected
                    elif
                        groupRecords
                        |> List.exists (fun record ->
                            record.VisibleResponse = ViewerResponsivenessVisibleResponse.EnvironmentLimited
                            || record.EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.NoVisibleSurface
                            || record.EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.HeadlessSubstitute
                            || record.EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.MissingBoundary)
                    then
                        ViewerResponsivenessReadiness.EnvironmentLimited
                    elif List.isEmpty measured then
                        ViewerResponsivenessReadiness.Incomplete
                    else
                        ViewerResponsivenessReadiness.Accepted

                { Page = page
                  InputKind = inputKind
                  ControlGroup = controlGroup
                  Count = groupRecords.Length
                  P50 = percentile 50.0 measured
                  P95 = p95
                  Max = maxTime measured
                  LongFrameCount = longFrames
                  Readiness = readiness })

        let slowest =
            records
            |> List.choose (fun record ->
                record.PhaseTiming.TotalInputToVisibleDuration
                |> Option.map (fun duration -> duration, record))
            |> List.sortByDescending (fun (duration, _) -> duration.Ticks)
            |> List.truncate 5
            |> List.map (fun (_, record) ->
                { RecordId = record.RecordId
                  InputSequenceId = record.InputSequenceId
                  TotalInputToVisible = record.PhaseTiming.TotalInputToVisibleDuration
                  DominantPhase = dominantPhase record.PhaseTiming })

        let failedRecord =
            records
            |> List.exists (fun record ->
                record.VisibleResponse = ViewerResponsivenessVisibleResponse.Failed
                || record.EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.Failed
                || record.EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.WriteFailed)

        let overall =
            if List.isEmpty records then ViewerResponsivenessReadiness.Incomplete
            elif failedRecord then ViewerResponsivenessReadiness.Failed
            elif not (List.isEmpty environmentLimitations) then ViewerResponsivenessReadiness.EnvironmentLimited
            elif Option.isSome firstFailed then ViewerResponsivenessReadiness.Rejected
            elif groups |> List.exists (fun group -> group.Readiness = ViewerResponsivenessReadiness.Incomplete) then ViewerResponsivenessReadiness.Incomplete
            else ViewerResponsivenessReadiness.Accepted

        { RunId = runId
          Scope = scope
          OverallReadiness = overall
          StartedUtc = startedUtc
          CompletedUtc = completedUtc
          RecordsPath = recordsPath
          Budgets = budget
          FirstFailedBudget = firstFailed
          Groups = groups
          SlowestInteractions = slowest
          EnvironmentLimitations = environmentLimitations
          Diagnostics = records |> List.collect (fun record -> record.Diagnostics) |> List.distinct }

    let timeMs (value: TimeSpan) = value.TotalMilliseconds
    let nullableMs value = value |> Option.map timeMs |> function Some n -> Nullable n | None -> Nullable<float>()

    let responsivenessSummaryToJson (summary: ViewerResponsivenessSummary) =
        let failed: JsonElement =
            match summary.FirstFailedBudget with
            | Some budget ->
                JsonSerializer.SerializeToElement(
                    {| kind = budget.Kind
                       scope = optionJsonString budget.Scope
                       inputKind = budget.InputKind |> Option.map responsivenessInputKindToken |> optionJsonString
                       measuredMs = timeMs budget.Measured
                       budgetMs = timeMs budget.Budget |},
                    jsonOptions
                )
            | None -> jsonNull

        JsonSerializer.Serialize(
            {| runId = summary.RunId
               scope = summary.Scope
               overallReadiness = responsivenessReadinessToken summary.OverallReadiness
               startedUtc = summary.StartedUtc
               completedUtc = summary.CompletedUtc
               recordsPath = summary.RecordsPath
               budgets =
                {| inputReceiptP95Ms = timeMs summary.Budgets.InputReceiptP95
                   inputReceiptMaxMs = timeMs summary.Budgets.InputReceiptMax
                   inputToVisibleP95Ms = timeMs summary.Budgets.InputToVisibleP95
                   inputToVisibleMaxMs = timeMs summary.Budgets.InputToVisibleMax
                   longFrameThresholdMs = timeMs summary.Budgets.LongFrameThreshold |}
               firstFailedBudget = failed
               groups =
                (summary.Groups
                 |> List.map (fun group ->
                     {| page = optionJsonString group.Page
                        inputKind = responsivenessInputKindToken group.InputKind
                        controlGroup = optionJsonString group.ControlGroup
                        count = group.Count
                        p50Ms = nullableMs group.P50
                        p95Ms = nullableMs group.P95
                        maxMs = nullableMs group.Max
                        longFrameCount = group.LongFrameCount
                        readiness = responsivenessReadinessToken group.Readiness |})
                 |> List.toArray)
               slowestInteractions =
                (summary.SlowestInteractions
                 |> List.map (fun interaction ->
                     {| recordId = interaction.RecordId
                        inputSequenceId = interaction.InputSequenceId
                        totalInputToVisibleMs = nullableMs interaction.TotalInputToVisible
                        dominantPhase = optionJsonString interaction.DominantPhase |})
                 |> List.toArray)
               environmentLimitations = summary.EnvironmentLimitations
               diagnostics = summary.Diagnostics |},
            jsonOptionsIndented
        )

    let responsivenessSummaryToMarkdown (summary: ViewerResponsivenessSummary) =
        let fmt (value: TimeSpan option) =
            value
            |> Option.map (fun duration -> duration.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) + " ms")
            |> Option.defaultValue "n/a"

        let budgetLine =
            match summary.FirstFailedBudget with
            | Some budget ->
                let measured = budget.Measured.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)
                let budgetMs = budget.Budget.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)
                $"- first failed budget: {budget.Kind} measured={measured} ms budget={budgetMs} ms"
            | None -> "- first failed budget: none"

        let groupLines =
            summary.Groups
            |> List.map (fun group ->
                let page = group.Page |> Option.defaultValue "unknown"
                let control = group.ControlGroup |> Option.defaultValue "unknown"
                $"| {page} | {responsivenessInputKindToken group.InputKind} | {control} | {group.Count} | {fmt group.P50} | {fmt group.P95} | {fmt group.Max} | {group.LongFrameCount} | {responsivenessReadinessToken group.Readiness} |")

        let slowLines =
            summary.SlowestInteractions
            |> List.map (fun item ->
                let dominant = item.DominantPhase |> Option.defaultValue "unknown"
                $"- {item.RecordId}: seq={item.InputSequenceId}, total={fmt item.TotalInputToVisible}, dominant={dominant}")

        let envLines =
            match summary.EnvironmentLimitations with
            | [] -> [ "- environment limitations: none" ]
            | values -> values |> List.map (fun value -> $"- environment limitation: {value}")

        String.concat
            Environment.NewLine
            ([ $"# Responsiveness summary {summary.RunId}"
               ""
               $"- scope: {summary.Scope}"
               $"- overall readiness: {responsivenessReadinessToken summary.OverallReadiness}"
               $"- records: {summary.RecordsPath}"
               budgetLine
               ""
               "| Page | Input | Control | Count | p50 | p95 | max | long frames | readiness |"
               "|------|-------|---------|-------|-----|-----|-----|-------------|-----------|" ]
             @ groupLines
             @ [ ""; "## Slowest interactions" ]
             @ (if List.isEmpty slowLines then [ "- none" ] else slowLines)
             @ [ ""; "## Environment" ]
             @ envLines
             @ [ "" ])

    let writeResponsivenessRun (outputRoot: string) (summary: ViewerResponsivenessSummary) (records: ViewerLatencyRecord list) =
        let runRoot = Path.Combine(outputRoot, summary.RunId)
        Directory.CreateDirectory runRoot |> ignore

        let recordsPath = Path.Combine(runRoot, "records.jsonl")
        let summaryJsonPath = Path.Combine(runRoot, "summary.json")
        let summaryMarkdownPath = Path.Combine(runRoot, "summary.md")
        let environmentPath = Path.Combine(runRoot, "environment.md")

        File.WriteAllLines(recordsPath, records |> List.map latencyRecordToJsonLine)
        File.WriteAllText(summaryJsonPath, responsivenessSummaryToJson summary)
        File.WriteAllText(summaryMarkdownPath, responsivenessSummaryToMarkdown summary)

        let environmentLines =
            [ "# Responsiveness environment"
              ""
              $"- readiness: {responsivenessReadinessToken summary.OverallReadiness}"
              $"- startedUtc: {summary.StartedUtc:O}"
              $"- completedUtc: {summary.CompletedUtc:O}" ]
            @ (match summary.EnvironmentLimitations with
               | [] -> [ "- limitations: none" ]
               | values -> values |> List.map (fun value -> $"- limitation: {value}"))

        File.WriteAllText(environmentPath, String.concat Environment.NewLine environmentLines + Environment.NewLine)

        [ recordsPath; summaryJsonPath; summaryMarkdownPath; environmentPath ]

