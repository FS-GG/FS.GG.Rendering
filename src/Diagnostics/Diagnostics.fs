namespace FS.GG.UI.Diagnostics

open System
open System.Globalization
open System.IO
open System.Text
open System.Text.Json

type DiagnosticSeverity =
    | Informational
    | Warning
    | Error

type DiagnosticCategory =
    | Environment
    | BackendCost
    | RenderingLimitation
    | ReadinessBlocker
    | DeveloperAction

type DiagnosticReadinessImpact =
    | NonBlocking
    | BlocksReadiness
    | RequiresReview
    | EnvironmentLimited

type ReadinessDiagnosticStatus =
    | Accepted
    | Blocked
    | ReviewRequired
    | EnvironmentLimitedStatus

type DiagnosticSource =
    { PackageId: string option
      Subsystem: string
      LaneId: string option
      SampleId: string option }

type DiagnosticContext =
    { RunId: string option
      TimestampUtc: DateTime option
      OutputPath: string option
      Details: (string * string) list }

type RuntimeDiagnostic =
    { Id: string
      Source: DiagnosticSource
      Code: string option
      Severity: DiagnosticSeverity option
      Category: DiagnosticCategory option
      Message: string
      Action: string option
      Context: DiagnosticContext
      Fingerprint: string }

type DiagnosticException =
    { ExceptionId: string
      Scope: string
      Reason: string
      ExpiresOn: DateOnly option
      AcceptedBy: string option }

type AggregatedDiagnostic =
    { Fingerprint: string
      Source: DiagnosticSource
      Code: string option
      Severity: DiagnosticSeverity option
      Category: DiagnosticCategory option
      Message: string
      Action: string option
      OccurrenceCount: int
      FirstOccurrence: DiagnosticContext
      LastOccurrence: DiagnosticContext
      ExampleIds: string list }

type DiagnosticSummary =
    { RunId: string option
      Status: ReadinessDiagnosticStatus
      CountsBySeverity: (DiagnosticSeverity * int) list
      CountsByCategory: (DiagnosticCategory * int) list
      BlockerCount: int
      UnclassifiedCount: int
      ReviewRequiredCount: int
      ExceptionCount: int
      ArtifactPaths: string list
      Groups: AggregatedDiagnostic list
      Exceptions: DiagnosticException list
      ArtifactWriteDiagnostics: RuntimeDiagnostic list }

module RuntimeDiagnostics =
    let private trimOption value =
        value
        |> Option.bind (fun text ->
            if String.IsNullOrWhiteSpace text then
                None
            else
                Some(text.Trim()))

    let private nonEmpty fallback (value: string) =
        if String.IsNullOrWhiteSpace value then fallback else value.Trim()

    let source packageId subsystem laneId sampleId =
        { PackageId = trimOption packageId
          Subsystem = nonEmpty "unknown" subsystem
          LaneId = trimOption laneId
          SampleId = trimOption sampleId }

    let context runId timestampUtc outputPath details =
        { RunId = trimOption runId
          TimestampUtc = timestampUtc
          OutputPath = trimOption outputPath
          Details =
            details
            |> List.choose (fun (key, value) ->
                if String.IsNullOrWhiteSpace key then
                    None
                else
                    Some(key.Trim(), value)) }

    let severityToken severity =
        match severity with
        | Informational -> "informational"
        | Warning -> "warning"
        | Error -> "error"

    let categoryToken category =
        match category with
        | Environment -> "environment"
        | BackendCost -> "backend-cost"
        | RenderingLimitation -> "rendering-limitation"
        | ReadinessBlocker -> "readiness-blocker"
        | DeveloperAction -> "developer-action"

    let readinessStatusToken status =
        match status with
        | Accepted -> "accepted"
        | Blocked -> "blocked"
        | ReviewRequired -> "review-required"
        | EnvironmentLimitedStatus -> "environment-limited"

    let tryParseReadinessStatus token =
        match nonEmpty "" token |> fun text -> text.ToLowerInvariant() with
        | "accepted" -> Some Accepted
        | "blocked" -> Some Blocked
        | "review-required" -> Some ReviewRequired
        | "environment-limited" -> Some EnvironmentLimitedStatus
        | _ -> None

    let private sourceKey source =
        [ source.PackageId |> Option.defaultValue ""
          source.Subsystem
          source.LaneId |> Option.defaultValue ""
          source.SampleId |> Option.defaultValue "" ]
        |> List.map (fun text -> text.Trim().ToLowerInvariant())
        |> String.concat "/"

    let private normalizedText (text: string) =
        text.Split([| ' '; '\t'; '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> String.concat " "
        |> fun value -> value.Trim().ToLowerInvariant()

    let private fingerprintOf source code severity category message action =
        [ sourceKey source
          code |> Option.defaultValue "uncoded" |> normalizedText
          category |> Option.map categoryToken |> Option.defaultValue "unclassified-category"
          severity |> Option.map severityToken |> Option.defaultValue "unclassified-severity"
          normalizedText message
          action |> Option.defaultValue "" |> normalizedText ]
        |> String.concat ":"

    let private stableHash (text: string) =
        let mutable hash = 2166136261u

        for ch in text do
            hash <- hash ^^^ uint32 ch
            hash <- hash * 16777619u

        hash.ToString("x8", CultureInfo.InvariantCulture)

    let create source code severity category message action context =
        let message = nonEmpty "<missing diagnostic message>" message
        let action = trimOption action
        let fingerprint = fingerprintOf source (trimOption code) severity category message action

        { Id = "diag-" + stableHash fingerprint
          Source = source
          Code = trimOption code
          Severity = severity
          Category = category
          Message = message
          Action = action
          Context = context
          Fingerprint = fingerprint }

    let aggregate (diagnostics: RuntimeDiagnostic list) : AggregatedDiagnostic list =
        diagnostics
        |> List.groupBy _.Fingerprint
        |> List.map (fun (fingerprint, group: RuntimeDiagnostic list) ->
            let first = List.head group
            let last = List.last group

            { Fingerprint = fingerprint
              Source = first.Source
              Code = first.Code
              Severity = first.Severity
              Category = first.Category
              Message = first.Message
              Action = first.Action
              OccurrenceCount = List.length group
              FirstOccurrence = first.Context
              LastOccurrence = last.Context
              ExampleIds = group |> List.map _.Id |> List.distinct |> List.truncate 5 })
        |> List.sortBy (fun group ->
            let severityRank =
                match group.Severity with
                | Some Error -> 0
                | Some Warning -> 1
                | Some Informational -> 2
                | None -> -1

            severityRank, group.Fingerprint)

    let private countByOccurrence (projection: AggregatedDiagnostic -> 'key option) (groups: AggregatedDiagnostic list) =
        groups
        |> List.choose (fun group -> projection group |> Option.map (fun key -> key, group.OccurrenceCount))
        |> List.groupBy fst
        |> List.map (fun (key, rows) -> key, rows |> List.sumBy snd)
        |> List.sortBy fst

    let private exceptionIsValid now (ex: DiagnosticException) =
        not (String.IsNullOrWhiteSpace ex.ExceptionId)
        && not (String.IsNullOrWhiteSpace ex.Scope)
        && not (String.IsNullOrWhiteSpace ex.Reason)
        && (ex.ExpiresOn |> Option.forall (fun expires -> expires >= now))

    let private sourceScopes (source: DiagnosticSource) =
        [ yield source.Subsystem
          match source.PackageId with
          | Some value -> yield value
          | None -> ()
          match source.LaneId with
          | Some value -> yield value
          | None -> ()
          match source.SampleId with
          | Some value -> yield value
          | None -> ()
          yield sourceKey source ]

    let private exceptionMatchesGroup (ex: DiagnosticException) (group: AggregatedDiagnostic) =
        let scope = ex.Scope.Trim()

        let candidates =
            [ yield group.Fingerprint
              yield! sourceScopes group.Source
              match group.Code with
              | Some code -> yield code
              | None -> ()
              match group.Category with
              | Some category -> yield categoryToken category
              | None -> ()
              match group.Severity with
              | Some severity -> yield severityToken severity
              | None -> () ]

        candidates
        |> List.exists (fun candidate -> String.Equals(candidate, scope, StringComparison.OrdinalIgnoreCase))

    let private exceptionProblemDiagnostic (code: string) (message: string) (exceptionId: string) : RuntimeDiagnostic =
        let src = source (Some "FS.GG.UI.Diagnostics") "diagnostic-exception" None None
        let ctx = context None None None [ "exceptionId", exceptionId ]

        create
            src
            (Some code)
            (Some Warning)
            (Some DeveloperAction)
            message
            (Some "Fix or remove the diagnostic exception before accepting readiness.")
            ctx

    let private developerActionRequiresReview (group: AggregatedDiagnostic) =
        match group.Category, group.Severity with
        | Some DeveloperAction, Some Warning
        | Some DeveloperAction, Some Error -> true
        | _ -> false

    let private environmentLimits (group: AggregatedDiagnostic) =
        match group.Category, group.Severity with
        | Some Environment, Some Error -> true
        | _ -> false

    let summarize (runId: string option) (exceptions: DiagnosticException list) (artifactPaths: string list) (diagnostics: RuntimeDiagnostic list) =
        let now = DateOnly.FromDateTime(DateTime.UtcNow)
        let initialGroups = aggregate diagnostics

        let exceptionProblems =
            exceptions
            |> List.choose (fun ex ->
                if not (exceptionIsValid now ex) then
                    Some(exceptionProblemDiagnostic "InvalidDiagnosticException" $"Diagnostic exception `{ex.ExceptionId}` is invalid or expired." ex.ExceptionId)
                elif initialGroups |> List.exists (exceptionMatchesGroup ex) |> not then
                    Some(exceptionProblemDiagnostic "UnmatchedDiagnosticException" $"Diagnostic exception `{ex.ExceptionId}` did not match any runtime diagnostic." ex.ExceptionId)
                else
                    None)

        let diagnostics = diagnostics @ exceptionProblems
        let groups = aggregate diagnostics
        let validMatchedExceptions =
            exceptions
            |> List.filter (fun ex -> exceptionIsValid now ex && initialGroups |> List.exists (exceptionMatchesGroup ex))

        let excepted group =
            validMatchedExceptions |> List.exists (fun ex -> exceptionMatchesGroup ex group)

        let unclassifiedCount =
            groups
            |> List.filter (fun group -> group.Severity.IsNone || group.Category.IsNone)
            |> List.sumBy _.OccurrenceCount

        let blockerCount =
            groups
            |> List.filter (fun group -> group.Category = Some ReadinessBlocker && not (excepted group))
            |> List.sumBy _.OccurrenceCount

        let developerReviewCount =
            groups
            |> List.filter (fun group -> developerActionRequiresReview group && not (excepted group))
            |> List.sumBy _.OccurrenceCount

        let reviewRequiredCount = unclassifiedCount + developerReviewCount

        let status =
            if unclassifiedCount > 0 || not exceptionProblems.IsEmpty then
                ReviewRequired
            elif blockerCount > 0 then
                Blocked
            elif developerReviewCount > 0 then
                ReviewRequired
            elif groups |> List.exists environmentLimits then
                EnvironmentLimitedStatus
            else
                Accepted

        { RunId = runId
          Status = status
          CountsBySeverity = groups |> countByOccurrence _.Severity
          CountsByCategory = groups |> countByOccurrence _.Category
          BlockerCount = blockerCount
          UnclassifiedCount = unclassifiedCount
          ReviewRequiredCount = reviewRequiredCount
          ExceptionCount = validMatchedExceptions.Length
          ArtifactPaths = artifactPaths
          Groups = groups
          Exceptions = validMatchedExceptions
          ArtifactWriteDiagnostics = [] }

    let private json value = JsonSerializer.Serialize(value)

    let private jsonOption value =
        value |> Option.map json |> Option.defaultValue "null"

    let private jsonDate value =
        value
        |> Option.map (fun (date: DateTime) -> json (date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)))
        |> Option.defaultValue "null"

    let private jsonDateOnly value =
        value
        |> Option.map (fun (date: DateOnly) -> json (date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
        |> Option.defaultValue "null"

    let private jsonStringArray values =
        "[" + (values |> List.map json |> String.concat ",") + "]"

    let private jsonDetails details =
        "{"
        + (details
           |> List.map (fun (key, value) -> json key + ":" + json value)
           |> String.concat ",")
        + "}"

    let private renderSourceJson source =
        "{"
        + String.concat
            ","
            [ "\"packageId\":" + jsonOption source.PackageId
              "\"subsystem\":" + json source.Subsystem
              "\"laneId\":" + jsonOption source.LaneId
              "\"sampleId\":" + jsonOption source.SampleId ]
        + "}"

    let private renderContextJson (context: DiagnosticContext) =
        "{"
        + String.concat
            ","
            [ "\"runId\":" + jsonOption context.RunId
              "\"timestampUtc\":" + jsonDate context.TimestampUtc
              "\"outputPath\":" + jsonOption context.OutputPath
              "\"details\":" + jsonDetails context.Details ]
        + "}"

    let private renderDiagnosticJson (diagnostic: RuntimeDiagnostic) =
        "{"
        + String.concat
            ","
            [ "\"id\":" + json diagnostic.Id
              "\"source\":" + renderSourceJson diagnostic.Source
              "\"code\":" + jsonOption diagnostic.Code
              "\"severity\":" + (diagnostic.Severity |> Option.map severityToken |> jsonOption)
              "\"category\":" + (diagnostic.Category |> Option.map categoryToken |> jsonOption)
              "\"message\":" + json diagnostic.Message
              "\"action\":" + jsonOption diagnostic.Action
              "\"context\":" + renderContextJson diagnostic.Context
              "\"fingerprint\":" + json diagnostic.Fingerprint ]
        + "}"

    let private renderGroupJson (group: AggregatedDiagnostic) =
        "{"
        + String.concat
            ","
            [ "\"fingerprint\":" + json group.Fingerprint
              "\"source\":" + renderSourceJson group.Source
              "\"code\":" + jsonOption group.Code
              "\"severity\":" + (group.Severity |> Option.map severityToken |> jsonOption)
              "\"category\":" + (group.Category |> Option.map categoryToken |> jsonOption)
              "\"message\":" + json group.Message
              "\"action\":" + jsonOption group.Action
              "\"occurrenceCount\":" + string group.OccurrenceCount
              "\"firstOccurrence\":" + renderContextJson group.FirstOccurrence
              "\"lastOccurrence\":" + renderContextJson group.LastOccurrence
              "\"exampleIds\":" + jsonStringArray group.ExampleIds ]
        + "}"

    let private jsonCounts tokenOf values =
        "{"
        + (values
           |> List.map (fun (key, count) -> json (tokenOf key) + ":" + string count)
           |> String.concat ",")
        + "}"

    let private renderExceptionJson ex =
        "{"
        + String.concat
            ","
            [ "\"exceptionId\":" + json ex.ExceptionId
              "\"scope\":" + json ex.Scope
              "\"reason\":" + json ex.Reason
              "\"expiresOn\":" + jsonDateOnly ex.ExpiresOn
              "\"acceptedBy\":" + jsonOption ex.AcceptedBy ]
        + "}"

    let renderJson summary =
        "{"
        + String.concat
            ","
            [ "\"schemaVersion\":\"runtime-diagnostics-v1\""
              "\"runId\":" + jsonOption summary.RunId
              "\"status\":" + json (readinessStatusToken summary.Status)
              "\"countsBySeverity\":" + jsonCounts severityToken summary.CountsBySeverity
              "\"countsByCategory\":" + jsonCounts categoryToken summary.CountsByCategory
              "\"blockerCount\":" + string summary.BlockerCount
              "\"unclassifiedCount\":" + string summary.UnclassifiedCount
              "\"reviewRequiredCount\":" + string summary.ReviewRequiredCount
              "\"exceptionCount\":" + string summary.ExceptionCount
              "\"artifactPaths\":" + jsonStringArray summary.ArtifactPaths
              "\"groups\":[" + (summary.Groups |> List.map renderGroupJson |> String.concat ",") + "]"
              "\"exceptions\":[" + (summary.Exceptions |> List.map renderExceptionJson |> String.concat ",") + "]"
              "\"artifactWriteDiagnostics\":[" + (summary.ArtifactWriteDiagnostics |> List.map renderDiagnosticJson |> String.concat ",") + "]" ]
        + "}"

    let renderJsonLines (diagnostics: RuntimeDiagnostic list) =
        diagnostics
        |> List.map renderDiagnosticJson
        |> String.concat Environment.NewLine
        |> fun text -> if text = "" then "" else text + Environment.NewLine

    let private countsText (tokenOf: 'a -> string) (counts: ('a * int) list) =
        if counts.IsEmpty then
            "none"
        else
            counts
            |> List.map (fun (key, count) -> $"{tokenOf key}={count}")
            |> String.concat " "

    let private sourceText source =
        [ source.PackageId; Some source.Subsystem; source.LaneId; source.SampleId ]
        |> List.choose id
        |> String.concat "/"

    let renderMarkdown summary =
        let sb = StringBuilder()
        let line (text: string) = sb.AppendLine(text) |> ignore
        let renderedRunId = summary.RunId |> Option.defaultValue "none"
        line "# Runtime Diagnostics Summary"
        line ""
        line $"- run id: `{renderedRunId}`"
        line $"- status: `{readinessStatusToken summary.Status}`"
        line $"- severity counts: `{countsText severityToken summary.CountsBySeverity}`"
        line $"- category counts: `{countsText categoryToken summary.CountsByCategory}`"
        line $"- blockers: `{summary.BlockerCount}`"
        line $"- review required: `{summary.ReviewRequiredCount}`"
        line $"- unclassified: `{summary.UnclassifiedCount}`"
        line $"- accepted exceptions: `{summary.ExceptionCount}`"

        if not summary.ArtifactPaths.IsEmpty then
            line ""
            line "## Artifacts"
            for path in summary.ArtifactPaths do
                line $"- `{path}`"

        if not summary.Exceptions.IsEmpty then
            line ""
            line "## Accepted Exceptions"
            for ex in summary.Exceptions do
                line $"- `{ex.ExceptionId}` scope `{ex.Scope}`: {ex.Reason}"

        line ""
        line "## Groups"
        line ""
        line "| Source | Code | Severity | Category | Count | Message | Action |"
        line "|---|---|---|---|---:|---|---|"

        for group in summary.Groups do
            let severity = group.Severity |> Option.map severityToken |> Option.defaultValue "unclassified"
            let category = group.Category |> Option.map categoryToken |> Option.defaultValue "unclassified"
            let code = group.Code |> Option.defaultValue ""
            let action = group.Action |> Option.defaultValue ""
            line $"| `{sourceText group.Source}` | `{code}` | `{severity}` | `{category}` | {group.OccurrenceCount} | {group.Message} | {action} |"

        if not summary.ArtifactWriteDiagnostics.IsEmpty then
            line ""
            line "## Artifact Write Warnings"
            for diagnostic in summary.ArtifactWriteDiagnostics do
                line $"- {diagnostic.Message}"

        sb.ToString()

    let private firstSource predicate summary =
        summary.Groups
        |> List.tryFind predicate
        |> Option.map (fun group -> sourceText group.Source)
        |> Option.defaultValue "none"

    let renderConsole verbose maxDefaultLines summary =
        let artifactText =
            if summary.ArtifactPaths.IsEmpty then
                "none"
            else
                String.concat " " summary.ArtifactPaths

        let header =
            [ $"Diagnostics: {readinessStatusToken summary.Status}"
              $"Severity: {countsText severityToken summary.CountsBySeverity}"
              $"Category: {countsText categoryToken summary.CountsByCategory}"
              $"Blockers: {summary.BlockerCount} (first: {firstSource (fun group -> group.Category = Some ReadinessBlocker) summary})"
              $"Review required: {summary.ReviewRequiredCount}"
              $"Artifacts: {artifactText}" ]

        let groupLine group =
            let severity = group.Severity |> Option.map severityToken |> Option.defaultValue "unclassified"
            let category = group.Category |> Option.map categoryToken |> Option.defaultValue "unclassified"
            let code = group.Code |> Option.defaultValue "uncoded"
            let action = group.Action |> Option.defaultValue "no action guidance"
            $"- {category}/{severity} {code} x{group.OccurrenceCount}: {group.Message} ({action})"

        let selected =
            if verbose then
                summary.Groups
            else
                let important =
                    summary.Groups
                    |> List.filter (fun group ->
                        group.Category = Some ReadinessBlocker
                        || group.Severity.IsNone
                        || group.Category.IsNone
                        || developerActionRequiresReview group)

                if important.IsEmpty then
                    summary.Groups |> List.truncate 1
                else
                    important

        let lines = header @ (selected |> List.map groupLine)

        if verbose || lines.Length <= maxDefaultLines then
            lines
        elif maxDefaultLines <= 0 then
            []
        elif maxDefaultLines = 1 then
            [ "..." ]
        else
            (lines |> List.truncate (maxDefaultLines - 1)) @ [ "..." ]

    let private artifactFailureDiagnostic path message =
        let src = source (Some "FS.GG.UI.Diagnostics") "diagnostic-artifact" None None
        let ctx = context None None (Some path) [ "path", path; "error", message ]

        create
            src
            (Some "ArtifactWriteFailed")
            (Some Warning)
            (Some DeveloperAction)
            $"Could not write diagnostic artifact `{path}`: {message}"
            (Some "Fix the artifact output path and rerun diagnostics; in-memory classification completed.")
            ctx

    let writeArtifacts (outputDirectory: string) (runId: string option) (exceptions: DiagnosticException list) (diagnostics: RuntimeDiagnostic list) =
        let jsonPath = Path.Combine(outputDirectory, "diagnostics-summary.json")
        let markdownPath = Path.Combine(outputDirectory, "diagnostics-summary.md")
        let jsonLinesPath = Path.Combine(outputDirectory, "diagnostics-records.jsonl")
        let artifactPaths = [ jsonPath; markdownPath; jsonLinesPath ]
        let mutable writeDiagnostics: RuntimeDiagnostic list = []

        let tryWrite (path: string) (text: string) =
            try
                match Path.GetDirectoryName path with
                | null
                | "" -> ()
                | parent -> Directory.CreateDirectory parent |> ignore

                File.WriteAllText(path, text)
            with ex ->
                writeDiagnostics <- writeDiagnostics @ [ artifactFailureDiagnostic path ex.Message ]

        let initial = summarize runId exceptions artifactPaths diagnostics
        tryWrite jsonPath (renderJson initial + Environment.NewLine)
        tryWrite markdownPath (renderMarkdown initial)
        tryWrite jsonLinesPath (renderJsonLines diagnostics)

        let finalSummary = summarize runId exceptions artifactPaths (diagnostics @ writeDiagnostics)
        { finalSummary with ArtifactWriteDiagnostics = writeDiagnostics }
