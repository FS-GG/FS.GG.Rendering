module FsGgFeedbackReportTool

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.RegularExpressions

let surfaces =
    [ "scaffolding"
      "onboarding-guidance"
      "skills"
      "sdd-authoring"
      "implementation-apis"
      "dependencies-build"
      "testing"
      "evidence"
      "runtime-playtest"
      "performance"
      "documentation"
      "packaging-upgrade"
      "worker-git-pr" ]

let kinds =
    [ "positive-pattern"
      "defect"
      "friction"
      "capability-gap"
      "quality-gap"
      "documentation"
      "orchestration" ]

let criticStatuses =
    [ "actionable"; "incomplete"; "unsupported"; "duplicate"; "positive-pattern" ]

let evidenceResults =
    [ "verified"; "missing"; "stale"; "non-reproducing"; "contradictory"; "claim-only" ]

let private requiredFrontmatter =
    [ "feedbackSchema"; "date"; "workspace"; "cycle"; "lane"; "toolVersion"; "commit" ]

let private requiredSections =
    [ for number, title in
          [ 1, "Provenance and confidence"
            2, "What worked"
            3, "What did not"
            4, "Findings"
            5, "Did not exercise"
            6, "Doc-versus-behavior contradictions"
            7, "Workarounds still in the tree"
            8, "Friction and avoidable cost"
            9, "Skill value and gaps"
            10, "Outcome markers"
            11, "Falsifiable improvements"
            12, "Development-surface coverage" ] ->
        sprintf "## §%d %s" number title ]

let private requiredFindingFields =
    [ "Kind"
      "Impact"
      "Expected"
      "Observed"
      "Evidence"
      "Version"
      "Owner"
      "Recurrence"
      "Avoidable cost"
      "Disposition" ]

let private normalizeNewlines (text: string) =
    text.Replace("\r\n", "\n").Replace("\r", "\n")

let sha256Text (text: string) =
    use sha = SHA256.Create()

    text
    |> normalizeNewlines
    |> Encoding.UTF8.GetBytes
    |> sha.ComputeHash
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

let private frontmatter (text: string) =
    let lines = normalizeNewlines text |> fun value -> value.Split '\n'

    if lines.Length < 3 || lines.[0].Trim() <> "---" then
        None
    else
        lines
        |> Array.skip 1
        |> Array.tryFindIndex (fun line -> line.Trim() = "---")
        |> Option.map (fun closing ->
            lines.[1..closing]
            |> Array.choose (fun line ->
                let separator = line.IndexOf ':'

                if separator <= 0 then
                    None
                else
                    Some(line.Substring(0, separator).Trim(), line.Substring(separator + 1).Trim()))
            |> Map.ofArray)

let private sectionText (text: string) (startHeading: string) (endHeading: string) =
    let startIndex = text.IndexOf(startHeading, StringComparison.Ordinal)

    if startIndex < 0 then
        ""
    else
        let contentStart = startIndex + startHeading.Length
        let endIndex = text.IndexOf(endHeading, contentStart, StringComparison.Ordinal)

        if endIndex < 0 then
            text.Substring contentStart
        else
            text.Substring(contentStart, endIndex - contentStart)

let validateReportText (rawText: string) =
    let text = normalizeNewlines rawText
    let errors = ResizeArray<string>()

    match frontmatter text with
    | None -> errors.Add "frontmatter: expected an opening and closing --- block"
    | Some fields ->
        for field in requiredFrontmatter do
            match Map.tryFind field fields with
            | None -> errors.Add(sprintf "frontmatter: missing %s" field)
            | Some value when String.IsNullOrWhiteSpace value ->
                errors.Add(sprintf "frontmatter: %s must not be empty" field)
            | _ -> ()

        match Map.tryFind "feedbackSchema" fields with
        | Some "2" -> ()
        | Some value -> errors.Add(sprintf "frontmatter: feedbackSchema must be 2, got %s" value)
        | None -> ()

    let mutable previousIndex = -1

    for heading in requiredSections do
        let headingPattern = "(?m)^" + Regex.Escape heading + @"\s*$"
        let matches = Regex.Matches(text, headingPattern)

        if matches.Count = 0 then
            errors.Add(sprintf "sections: missing '%s'" heading)
        elif matches.Count > 1 then
            errors.Add(sprintf "sections: duplicate '%s'" heading)
        else
            let currentIndex = matches.[0].Index

            if currentIndex < previousIndex then
                errors.Add(sprintf "sections: '%s' is out of order" heading)

            previousIndex <- currentIndex

    let findings = sectionText text requiredSections.[3] requiredSections.[4]
    let findingMatches = Regex.Matches(findings, @"(?m)^#### §4\.(\d+) .+$")

    if findingMatches.Count = 0 then
        if not (findings.Contains("None observed.", StringComparison.OrdinalIgnoreCase)) then
            errors.Add "findings: use structured §4.n records or write 'None observed.'"
    else
        for index in 0 .. findingMatches.Count - 1 do
            let findingNumber = findingMatches.[index].Groups.[1].Value
            let expectedNumber = string (index + 1)

            if findingNumber <> expectedNumber then
                errors.Add(
                    sprintf "findings: expected §4.%s, got §4.%s" expectedNumber findingNumber
                )

            let chunkStart = findingMatches.[index].Index

            let chunkEnd =
                if index + 1 < findingMatches.Count then
                    findingMatches.[index + 1].Index
                else
                    findings.Length

            let chunk = findings.Substring(chunkStart, chunkEnd - chunkStart)

            for field in requiredFindingFields do
                let fieldPattern =
                    @"(?m)^- \*\*" + Regex.Escape field + @":\*\*\s+\S.*$"

                if not (Regex.IsMatch(chunk, fieldPattern)) then
                    errors.Add(sprintf "findings: §4.%s is missing '%s'" findingNumber field)

            let fieldValue name =
                let matched =
                    Regex.Match(
                        chunk,
                        @"(?m)^- \*\*" + Regex.Escape name + @":\*\*\s+(.+?)\s*$"
                    )

                if matched.Success then matched.Groups.[1].Value.Trim() else ""

            let expected = fieldValue "Expected"
            let observed = fieldValue "Observed"

            if
                not (String.IsNullOrWhiteSpace expected)
                && expected.Equals(observed, StringComparison.OrdinalIgnoreCase)
            then
                errors.Add(
                    sprintf "findings: §4.%s Expected and Observed must describe a delta" findingNumber
                )

            let kindPattern =
                @"(?m)^- \*\*Kind:\*\*\s+(" + String.concat "|" (List.map Regex.Escape kinds) + @")\s*$"

            if not (Regex.IsMatch(chunk, kindPattern)) then
                errors.Add(
                    sprintf
                        "findings: §4.%s Kind must be one of %s"
                        findingNumber
                        (String.concat ", " kinds)
                )

    let coverage = sectionText text requiredSections.[11] "\u0000"
    let rowPattern = Regex(@"(?m)^\|\s*([^|]+?)\s*\|\s*(exercised|partial|not-exercised)\s*\|")
    let rows = rowPattern.Matches coverage

    let observed =
        [ for row in rows do
              yield row.Groups.[1].Value.Trim() ]

    for surface in surfaces do
        let count = observed |> List.filter ((=) surface) |> List.length

        if count = 0 then
            errors.Add(sprintf "coverage: missing surface '%s'" surface)
        elif count > 1 then
            errors.Add(sprintf "coverage: duplicate surface '%s'" surface)

    for surface in observed do
        if not (List.contains surface surfaces) then
            errors.Add(sprintf "coverage: unknown surface '%s'" surface)

    List.ofSeq errors

type EvidenceCheck =
    { locator: string
      result: string
      sha256: string option }

type FindingAudit =
    { id: string
      status: string
      missingFacts: string list
      checkedEvidence: EvidenceCheck list
      confidenceLimits: string list }

type ActionabilityAudit =
    { auditSchema: int
      report: string
      reportSha256: string
      criticMode: string
      criticPromptVersion: string
      findings: FindingAudit list }

let private findingContracts (reportText: string) =
    let findings = sectionText reportText requiredSections.[3] requiredSections.[4]
    let matches = Regex.Matches(findings, @"(?m)^#### (§4\.\d+) .+$")

    [ for index in 0 .. matches.Count - 1 do
          let chunkStart = matches.[index].Index

          let chunkEnd =
              if index + 1 < matches.Count then matches.[index + 1].Index else findings.Length

          let chunk = findings.Substring(chunkStart, chunkEnd - chunkStart)
          let kindMatch = Regex.Match(chunk, @"(?m)^- \*\*Kind:\*\*\s+(\S+)\s*$")
          let evidenceMatch = Regex.Match(chunk, @"(?m)^- \*\*Evidence:\*\*\s+(.+?)\s*$")

          let evidence =
              if evidenceMatch.Success then
                  evidenceMatch.Groups.[1].Value.Split(';')
                  |> Array.map _.Trim()
                  |> Array.filter (String.IsNullOrWhiteSpace >> not)
                  |> Set.ofArray
              else
                  Set.empty

          yield
              matches.[index].Groups.[1].Value,
              (if kindMatch.Success then kindMatch.Groups.[1].Value else ""),
              evidence ]

let private resolveEvidencePath (workspaceRoot: string) (locator: string) =
    if not (locator.StartsWith("file:", StringComparison.Ordinal)) then
        None
    else
        let relative = locator.Substring("file:".Length).Trim()

        if String.IsNullOrWhiteSpace relative || Path.IsPathRooted relative then
            None
        else
            let root = Path.GetFullPath workspaceRoot
            let candidate = Path.GetFullPath(Path.Combine(root, relative))
            let prefix = root.TrimEnd(Path.DirectorySeparatorChar) + string Path.DirectorySeparatorChar

            if candidate.StartsWith(prefix, StringComparison.Ordinal) then Some candidate else None

let validateActionabilityAudit
    (workspaceRoot: string)
    (reportPath: string)
    (reportText: string)
    (auditText: string)
    =
    let errors = ResizeArray<string>()

    let audit =
        try
            let parsed = JsonSerializer.Deserialize<ActionabilityAudit>(auditText)

            if obj.ReferenceEquals(parsed, null) then
                invalidArg "auditText" "expected a JSON object"

            Some(unbox<ActionabilityAudit> (box parsed))
        with ex ->
            errors.Add(sprintf "audit: invalid JSON: %s" ex.Message)
            None

    match audit with
    | None -> ()
    | Some audit ->
        if audit.auditSchema <> 1 then
            errors.Add(sprintf "audit: auditSchema must be 1, got %d" audit.auditSchema)

        let expectedReport =
            Path.GetRelativePath(workspaceRoot, Path.GetFullPath reportPath)
                .Replace(Path.DirectorySeparatorChar, '/')

        if expectedReport.StartsWith("../", StringComparison.Ordinal) then
            errors.Add "audit: report must be inside the workspace"

        if audit.report <> expectedReport then
            errors.Add(sprintf "audit: report binding must be '%s'" expectedReport)

        let expectedDigest = sha256Text reportText

        if audit.reportSha256 <> expectedDigest then
            errors.Add "audit: reportSha256 does not bind the current report bytes"

        if
            audit.criticMode <> "fresh-context-subagent"
            && audit.criticMode <> "separated-critic-pass"
        then
            errors.Add
                "audit: criticMode must be fresh-context-subagent or separated-critic-pass"

        if String.IsNullOrWhiteSpace audit.criticPromptVersion then
            errors.Add "audit: criticPromptVersion must not be empty"

        let expectedFindings = findingContracts reportText
        let audits = if isNull (box audit.findings) then [] else audit.findings

        for id, kind, declaredEvidence in expectedFindings do
            let matches = audits |> List.filter (fun finding -> finding.id = id)

            if List.isEmpty matches then
                errors.Add(sprintf "audit: missing finding '%s'" id)
            elif matches.Length > 1 then
                errors.Add(sprintf "audit: duplicate finding '%s'" id)
            else
                let finding = matches.Head

                if not (List.contains finding.status criticStatuses) then
                    errors.Add(sprintf "audit: %s has unknown status '%s'" id finding.status)

                if kind = "positive-pattern" && finding.status <> "positive-pattern" then
                    errors.Add(sprintf "audit: %s positive-pattern must keep that disposition" id)

                if kind <> "positive-pattern" && finding.status = "positive-pattern" then
                    errors.Add(sprintf "audit: %s is not a positive-pattern finding" id)

                if finding.status = "incomplete" || finding.status = "unsupported" then
                    errors.Add(
                        sprintf
                            "actionability: %s remains %s and cannot be handed off as actionable"
                            id
                            finding.status
                    )

                let missingFacts =
                    if isNull (box finding.missingFacts) then [] else finding.missingFacts

                if
                    (finding.status = "actionable" || finding.status = "positive-pattern")
                    && not (List.isEmpty missingFacts)
                then
                    errors.Add(
                        sprintf
                            "audit: %s cannot be %s while missing facts are recorded"
                            id
                            finding.status
                    )

                let checks =
                    if isNull (box finding.checkedEvidence) then [] else finding.checkedEvidence

                if List.isEmpty checks then
                    errors.Add(sprintf "audit: %s has no checked evidence" id)

                let checkedLocators = checks |> List.map _.locator |> Set.ofList

                for locator in Set.difference declaredEvidence checkedLocators do
                    errors.Add(
                        sprintf
                            "audit: %s report evidence has no matching check: %s"
                            id
                            locator
                    )

                for locator in Set.difference checkedLocators declaredEvidence do
                    errors.Add(
                        sprintf
                            "audit: %s checked evidence is not declared by the report: %s"
                            id
                            locator
                    )

                for check in checks do
                    if not (List.contains check.result evidenceResults) then
                        errors.Add(
                            sprintf "audit: %s evidence has unknown result '%s'" id check.result
                        )

                    if
                        (finding.status = "actionable" || finding.status = "positive-pattern")
                        && check.result <> "verified"
                    then
                        errors.Add(
                            sprintf
                                "audit: %s cannot be %s with evidence result '%s'"
                                id
                                finding.status
                                check.result
                        )

                    if check.locator.StartsWith("file:", StringComparison.Ordinal) then
                        match resolveEvidencePath workspaceRoot check.locator with
                        | None ->
                            errors.Add(
                                sprintf
                                    "audit: %s evidence locator must be a workspace-relative file: path"
                                    id
                            )
                        | Some path when not (File.Exists path) ->
                            errors.Add(sprintf "audit: %s evidence file is missing: %s" id check.locator)
                        | Some path ->
                            match check.sha256 with
                            | None -> errors.Add(sprintf "audit: %s file evidence needs sha256" id)
                            | Some digest ->
                                let actual = File.ReadAllText path |> sha256Text

                                if digest <> actual then
                                    errors.Add(
                                        sprintf "audit: %s evidence digest is stale: %s" id check.locator
                                    )
                    elif Path.IsPathRooted check.locator then
                        errors.Add(sprintf "audit: %s evidence locator exposes an absolute path" id)

        let expectedIds = expectedFindings |> List.map (fun (id, _, _) -> id) |> Set.ofList

        for finding in audits do
            if not (Set.contains finding.id expectedIds) then
                errors.Add(sprintf "audit: unknown finding '%s'" finding.id)

    List.ofSeq errors

type Checkpoint =
    { timestampUtc: string
      cycle: string
      phase: string
      surface: string
      kind: string
      summary: string
      evidence: string
      cost: string
      owner: string }

let private requireValue name value =
    if String.IsNullOrWhiteSpace value then
        invalidArg name (sprintf "%s must not be empty" name)

let appendCheckpoint root cycle phase surface kind summary evidence cost owner =
    for name, value in
        [ "cycle", cycle
          "phase", phase
          "surface", surface
          "kind", kind
          "summary", summary
          "evidence", evidence
          "cost", cost
          "owner", owner ] do
        requireValue name value

    if not (Regex.IsMatch(cycle, "^[a-z0-9][a-z0-9-]*$")) then
        invalidArg "cycle" "cycle must be lowercase letters, digits, and hyphens"

    if not (List.contains surface surfaces) then
        invalidArg "surface" (sprintf "unknown surface '%s'" surface)

    if not (List.contains kind kinds) then
        invalidArg "kind" (sprintf "unknown kind '%s'" kind)

    let checkpoint =
        { timestampUtc = DateTimeOffset.UtcNow.ToString "O"
          cycle = cycle
          phase = phase
          surface = surface
          kind = kind
          summary = summary
          evidence = evidence
          cost = cost
          owner = owner }

    let directory = Path.Combine(root, "feedback", "checkpoints")
    Directory.CreateDirectory directory |> ignore
    let path = Path.Combine(directory, cycle + ".jsonl")
    let line = JsonSerializer.Serialize checkpoint + Environment.NewLine
    File.AppendAllText(path, line, UTF8Encoding(false))
    path

let validateCheckpointFile path =
    let errors = ResizeArray<string>()

    if not (File.Exists path) then
        [ sprintf "checkpoints: file not found: %s" path ]
    else
        for index, line in File.ReadLines path |> Seq.indexed do
            if String.IsNullOrWhiteSpace line then
                errors.Add(sprintf "checkpoints: line %d is empty" (index + 1))
            else
                try
                    use document = JsonDocument.Parse line
                    let root = document.RootElement

                    let readProperty (name: string) =
                        match root.TryGetProperty name with
                        | true, value when value.ValueKind = JsonValueKind.String ->
                            value.GetString() |> Option.ofObj |> Option.defaultValue ""
                        | _ ->
                            errors.Add(
                                sprintf "checkpoints: line %d is missing %s" (index + 1) name
                            )

                            ""

                    let values =
                        [ for name in
                              [ "timestampUtc"
                                "cycle"
                                "phase"
                                "surface"
                                "kind"
                                "summary"
                                "evidence"
                                "cost"
                                "owner" ] do
                              yield name, readProperty name ]

                    for name, value in values do
                        if String.IsNullOrWhiteSpace value then
                            errors.Add(
                                sprintf "checkpoints: line %d has empty %s" (index + 1) name
                            )

                    let valueOf name = values |> List.find (fst >> (=) name) |> snd
                    let surface = valueOf "surface"
                    let kind = valueOf "kind"

                    if not (List.contains surface surfaces) then
                        errors.Add(
                            sprintf
                                "checkpoints: line %d has unknown surface '%s'"
                                (index + 1)
                                surface
                        )

                    if not (List.contains kind kinds) then
                        errors.Add(
                            sprintf
                                "checkpoints: line %d has unknown kind '%s'"
                                (index + 1)
                                kind
                        )
                with ex ->
                    errors.Add(
                        sprintf "checkpoints: line %d is invalid JSON: %s" (index + 1) ex.Message
                    )

        List.ofSeq errors
