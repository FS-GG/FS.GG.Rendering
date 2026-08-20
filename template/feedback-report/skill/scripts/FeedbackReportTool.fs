module FsGgFeedbackReportTool

open System
open System.Diagnostics
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

let private pathComparison =
    if OperatingSystem.IsWindows() then
        StringComparison.OrdinalIgnoreCase
    else
        StringComparison.Ordinal

let private isInside root candidate =
    let normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)
    let normalizedCandidate = Path.GetFullPath candidate
    let prefix = normalizedRoot + string Path.DirectorySeparatorChar

    normalizedCandidate.Equals(normalizedRoot, pathComparison)
    || normalizedCandidate.StartsWith(prefix, pathComparison)

let private canonicalizeExistingSegments path =
    let fullPath = Path.GetFullPath path

    match Path.GetPathRoot fullPath |> Option.ofObj with
    | None ->
        fullPath
    | Some root ->
        let separators = [| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |]
        let relative = Path.GetRelativePath(root, fullPath)

        relative.Split(separators, StringSplitOptions.RemoveEmptyEntries)
        |> Array.fold
            (fun current segment ->
                let next = Path.Combine(current, segment)

                let entry: FileSystemInfo =
                    if Directory.Exists next then
                        DirectoryInfo next
                    else
                        FileInfo next

                if entry.Exists && not (String.IsNullOrWhiteSpace entry.LinkTarget) then
                    match entry.ResolveLinkTarget(true) |> Option.ofObj with
                    | None -> next
                    | Some target -> target.FullName
                else
                    next)
            root
        |> Path.GetFullPath

let private containsPrivateLocatorMaterial (locator: string) =
    let absolutePath =
        Regex.IsMatch(locator, @"(^|[\s=:(""'])/(?!/)")
        || Regex.IsMatch(locator, @"(^|[\s=:(""'])([A-Za-z]:[\\/]|\\\\)")

    let secret =
        Regex.IsMatch(
            locator,
            @"(?i)(--?(token|password|secret|api[-_]?key)\b|(?:token|password|secret|api[-_]?key)\s*=)"
        )

    absolutePath || secret

let private normalizedJsonString value =
    match box value with
    | null -> ""
    | text -> (unbox<string> text).Trim()

/// The sole mutable ledger used by the audit-binding checker. A citation to
/// this file cannot retain a digest: every legitimate excuse rewrites it.
let auditBindingExceptionsPath = "scripts/audit-binding-exceptions.json"

let private auditBindingExceptionsReason =
    "this is the audit-binding excuse ledger itself: excusing any binding rewrites it and invalidates the digest just pinned"

/// A citation whose digest was intentionally not compared because it points to
/// the one file that cannot have a stable binding.
type NotBoundCitation =
    { findingId: string
      locator: string
      path: string
      reason: string }

type AuditValidation =
    { errors: string list
      notBound: NotBoundCitation list }

/// A digest-bound citation from a merged feedback audit whose cited path was
/// touched by the candidate commit. This is deliberately an index lookup, not
/// a replay of historical report validation.
type InvalidatedAuditBinding =
    { audit: string
      report: string
      findingId: string
      locator: string
      path: string
      priorSha256: string }

/// A successfully applied, exact exception-ledger entry. The immutable audit is
/// not rewritten: this receipt names the replacement evidence that supersedes
/// one digest-bound citation.
type AppliedAuditBindingException =
    { id: string
      audit: string
      findingId: string
      locator: string
      path: string
      priorSha256: string
      replacementPath: string
      replacementSha256: string
      evidenceLocator: string }

/// One audit document in an index: its workspace-relative path, and either the
/// exact bytes that document had in the indexed tree or the fail-closed reason
/// they could not be read. An unreadable document is never dropped — a document
/// nobody could read is not a document that turned out to hold no citation.
type IndexedAudit =
    { auditPath: string
      document: Result<string, string> }

/// The `feedback/audits/*.audit.json` documents present in ONE tree, the name of
/// the tree they came from, and any error met while enumerating it.
///
/// `subject` is part of the verdict rather than decoration. "No merged binding was
/// invalidated" means one thing when the index was the merged tree and something
/// else entirely when it was whatever happened to be on disk, and reading the
/// output cannot separate those two claims unless the output says which it made.
/// Issue #1243 is what the unstated version cost: the commit-aware `--base/--head`
/// form derived its changed paths from the given refs but indexed its audits from
/// the working tree, so a candidate that introduced an audit was refused by that
/// same audit, and an authorized repair round became a fixed point.
///
/// `errors` never degrades into an empty index. A tree that genuinely holds no
/// audits and a tree that could not be read are different facts, and only the
/// first of them is a safe pass.
type AuditIndex =
    { subject: string
      audits: IndexedAudit list
      errors: string list }

type AuditInvalidationCheck =
    { subject: string
      errors: string list
      invalidated: InvalidatedAuditBinding list
      dispositions: AppliedAuditBindingException list
      bindings: InvalidatedAuditBinding list }

let private normalizeWorkspacePath (path: string) =
    path.Trim().Replace('\\', '/').TrimStart('/')

/// Parse `git diff --name-status --find-renames --find-copies` output. Rename
/// and copy records contribute BOTH names; deletions contribute their source.
let changedPathsFromNameStatus (text: string) =
    (normalizeNewlines text)
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
    |> Array.collect (fun line ->
        let fields = line.Split('\t', StringSplitOptions.RemoveEmptyEntries)

        if fields.Length < 2 then
            [||]
        elif fields.[0].StartsWith("R", StringComparison.Ordinal) || fields.[0].StartsWith("C", StringComparison.Ordinal) then
            if fields.Length >= 3 then [| normalizeWorkspacePath fields.[1]; normalizeWorkspacePath fields.[2] |] else [||]
        else
            [| normalizeWorkspacePath fields.[1] |])
    |> Array.filter (String.IsNullOrWhiteSpace >> not)
    |> Array.distinct
    |> Array.sort
    |> Array.toList

/// The workspace-relative directory every audit index is drawn from.
let auditIndexRoot = "feedback/audits"

/// How a verdict names the checkout on disk.
let workingTreeSubject = "the working tree"

/// How a verdict names the tree of a Git ref.
let baseRefSubject (reference: string) = sprintf "base ref %s" reference

/// Index the audits in the checkout on disk.
///
/// This is `--changed`'s subject and it is deliberately NOT the commit-aware one:
/// the working tree holds whatever the candidate has written, including audits the
/// candidate itself introduced, so it cannot answer "which MERGED audit does this
/// commit invalidate?". `baseTreeAuditIndex` is the answer to that question.
let workingTreeAuditIndex (workspaceRoot: string) : AuditIndex =
    let auditRoot = Path.Combine(workspaceRoot, "feedback", "audits")

    if not (Directory.Exists auditRoot) then
        { subject = workingTreeSubject
          audits = []
          errors = [] }
    else
        let audits =
            Directory.EnumerateFiles(auditRoot, "*.audit.json", SearchOption.AllDirectories)
            |> Seq.map (fun auditPath ->
                let relative =
                    Path.GetRelativePath(workspaceRoot, auditPath).Replace(Path.DirectorySeparatorChar, '/')

                let document =
                    try
                        Ok(File.ReadAllText auditPath)
                    with ex ->
                        Error ex.Message

                { auditPath = relative; document = document })
            |> List.ofSeq

        { subject = workingTreeSubject
          audits = audits
          errors = [] }

/// Selectively find digest-bearing `file:` citations in an INDEXED set of audit
/// documents whose paths intersect `changedPaths`. It never reads reports or
/// evidence bytes, so audits unrelated to this commit are not revalidated.
///
/// The index is a parameter rather than a filesystem walk because which tree is
/// indexed is the whole contract (#1243). Any error the index carried survives
/// into this result: an index that could not be enumerated must not be able to
/// present as an index that found nothing.
let findInvalidatedAuditBindingsIn (index: AuditIndex) (changedPaths: string list) =
    let errors = ResizeArray<string>(index.errors)
    let changed = changedPaths |> List.map normalizeWorkspacePath |> Set.ofList
    let invalidated = ResizeArray<InvalidatedAuditBinding>()
    let bindings = ResizeArray<InvalidatedAuditBinding>()

    for indexed in index.audits do
        let relativeAudit = indexed.auditPath

        match indexed.document with
        | Error detail -> errors.Add(sprintf "invalidation: unreadable audit %s: %s" relativeAudit detail)
        | Ok auditText ->

            try
                let parsed = JsonSerializer.Deserialize<ActionabilityAudit>(auditText)

                if obj.ReferenceEquals(parsed, null) then
                    errors.Add(sprintf "invalidation: malformed audit %s: expected a JSON object" relativeAudit)
                else
                    let audit = unbox<ActionabilityAudit> (box parsed)

                    if audit.auditSchema <> 1 then
                        errors.Add(sprintf "invalidation: malformed audit %s: auditSchema must be 1" relativeAudit)
                    elif String.IsNullOrWhiteSpace(normalizedJsonString audit.report) then
                        errors.Add(sprintf "invalidation: malformed audit %s: report is required" relativeAudit)
                    elif not (Regex.IsMatch(normalizedJsonString audit.reportSha256, "^[0-9a-f]{64}$")) then
                        errors.Add(sprintf "invalidation: malformed audit %s: reportSha256 must be 64 lowercase hex characters" relativeAudit)
                    elif isNull (box audit.findings) || List.isEmpty audit.findings then
                        errors.Add(sprintf "invalidation: malformed audit %s: findings is required" relativeAudit)
                    else
                        for finding in audit.findings do
                            if obj.ReferenceEquals(box finding, null) then
                                errors.Add(sprintf "invalidation: malformed audit %s: findings must not contain null entries" relativeAudit)
                            elif String.IsNullOrWhiteSpace(normalizedJsonString finding.id) then
                                errors.Add(sprintf "invalidation: malformed audit %s: finding id is required" relativeAudit)
                            elif isNull (box finding.checkedEvidence) then
                                errors.Add(sprintf "invalidation: malformed audit %s: %s has no checkedEvidence" relativeAudit (normalizedJsonString finding.id))
                            else
                                for evidence in finding.checkedEvidence do
                                    if obj.ReferenceEquals(box evidence, null) then
                                        errors.Add(sprintf "invalidation: malformed audit %s: %s has null checkedEvidence" relativeAudit (normalizedJsonString finding.id))
                                    else
                                        let locator = normalizedJsonString evidence.locator
                                        let result = normalizedJsonString evidence.result
                                        let digest = evidence.sha256 |> Option.map normalizedJsonString

                                        if String.IsNullOrWhiteSpace result || not (List.contains result evidenceResults) then
                                            errors.Add(sprintf "invalidation: malformed audit %s: %s has an invalid evidence result" relativeAudit (normalizedJsonString finding.id))
                                        elif locator.StartsWith("file:", StringComparison.Ordinal) && digest.IsNone then
                                            errors.Add(sprintf "invalidation: malformed audit %s: %s file evidence needs sha256" relativeAudit (normalizedJsonString finding.id))
                                        elif digest |> Option.exists (fun value -> not (Regex.IsMatch(value, "^[0-9a-f]{64}$"))) then
                                            errors.Add(sprintf "invalidation: malformed audit %s: %s evidence sha256 must be 64 lowercase hex characters" relativeAudit (normalizedJsonString finding.id))
                                        elif locator.StartsWith("file:", StringComparison.Ordinal) && digest.IsSome then
                                            let path = locator.Substring("file:".Length) |> normalizeWorkspacePath

                                            if String.IsNullOrWhiteSpace path then
                                                errors.Add(sprintf "invalidation: malformed audit %s: %s has an empty file locator" relativeAudit (normalizedJsonString finding.id))
                                            else
                                                let binding =
                                                    { audit = relativeAudit
                                                      report = normalizedJsonString audit.report
                                                      findingId = normalizedJsonString finding.id
                                                      locator = locator
                                                      path = path
                                                      priorSha256 = Option.get digest }

                                                bindings.Add binding

                                                if changed.Contains path then
                                                    invalidated.Add binding
            with ex ->
                errors.Add(sprintf "invalidation: malformed audit %s: %s" relativeAudit ex.Message)

    { subject = index.subject
      errors = errors |> Seq.sort |> List.ofSeq
      invalidated =
          invalidated
          |> Seq.sortBy (fun item -> item.audit, item.report, item.findingId, item.path, item.locator, item.priorSha256)
          |> List.ofSeq
      dispositions = []
      bindings =
          bindings
          |> Seq.sortBy (fun item -> item.audit, item.report, item.findingId, item.path, item.locator, item.priorSha256)
          |> List.ofSeq }

type private AuditBindingExceptionEntry =
    { id: string
      audit: string
      findingId: string
      locator: string
      path: string
      priorSha256: string
      replacementPath: string
      replacementSha256: string
      evidenceLocator: string }

type private CandidateEvidenceSubject =
    { subject: string
      readBytes: string -> Result<byte[] option, string> }

let private sha256EvidenceBytes (bytes: byte[]) =
    try
        bytes
        |> UTF8Encoding(false, true).GetString
        |> sha256Text
        |> Ok
    with ex -> Error(sprintf "replacement evidence is not valid UTF-8 text: %s" ex.Message)

let private exactObjectProperties label expected (element: JsonElement) =
    if element.ValueKind <> JsonValueKind.Object then
        [ sprintf "%s must be a JSON object" label ]
    else
        let names = element.EnumerateObject() |> Seq.map _.Name |> List.ofSeq
        let duplicateErrors =
            names
            |> List.countBy id
            |> List.choose (fun (name, count) ->
                if count > 1 then Some(sprintf "%s contains duplicate property '%s'" label name) else None)

        let actual = names |> Set.ofList
        let expected = expected |> Set.ofList
        let missing = Set.difference expected actual |> Seq.map (sprintf "%s is missing property '%s'" label)
        let extra = Set.difference actual expected |> Seq.map (sprintf "%s contains unknown property '%s'" label)
        duplicateErrors @ List.ofSeq missing @ List.ofSeq extra

let private requiredJsonString (label: string) (propertyName: string) (element: JsonElement) =
    match element.TryGetProperty propertyName with
    | true, property when property.ValueKind = JsonValueKind.String ->
        let value = property.GetString() |> Option.ofObj |> Option.defaultValue ""
        if String.IsNullOrWhiteSpace value then Error(sprintf "%s.%s must not be empty" label propertyName) else Ok value
    | true, _ -> Error(sprintf "%s.%s must be a JSON string" label propertyName)
    | false, _ -> Error(sprintf "%s.%s is required" label propertyName)

let private parseAuditBindingExceptionLedger (bytes: byte[]) =
    let errors = ResizeArray<string>()
    let entries = ResizeArray<AuditBindingExceptionEntry>()

    try
        use document = JsonDocument.Parse bytes
        let root = document.RootElement

        for error in exactObjectProperties "exception ledger" [ "schemaVersion"; "exceptions" ] root do
            errors.Add error

        match root.TryGetProperty "schemaVersion" with
        | true, value when value.ValueKind = JsonValueKind.Number ->
            match value.TryGetInt32() with
            | true, 1 -> ()
            | _ -> errors.Add "exception ledger schemaVersion must be 1"
        | true, _ -> errors.Add "exception ledger schemaVersion must be the number 1"
        | false, _ -> ()

        match root.TryGetProperty "exceptions" with
        | true, value when value.ValueKind = JsonValueKind.Array ->
            for index, entry in value.EnumerateArray() |> Seq.indexed do
                let label = sprintf "exception ledger entry %d" (index + 1)
                let expected =
                    [ "id"; "audit"; "findingId"; "locator"; "path"; "priorSha256"
                      "replacementPath"; "replacementSha256"; "evidenceLocator" ]

                for error in exactObjectProperties label expected entry do
                    errors.Add error

                let fields =
                    expected
                    |> List.map (fun name -> name, requiredJsonString label name entry)

                for _, result in fields do
                    match result with
                    | Error error -> errors.Add error
                    | Ok _ -> ()

                if fields |> List.forall (snd >> Result.isOk) then
                    let field name =
                        fields
                        |> List.find (fst >> (=) name)
                        |> snd
                        |> function Ok value -> value | Error _ -> failwith "validated above"

                    entries.Add
                        { id = field "id"
                          audit = field "audit"
                          findingId = field "findingId"
                          locator = field "locator"
                          path = field "path"
                          priorSha256 = field "priorSha256"
                          replacementPath = field "replacementPath"
                          replacementSha256 = field "replacementSha256"
                          evidenceLocator = field "evidenceLocator" }
        | true, _ -> errors.Add "exception ledger exceptions must be a JSON array"
        | false, _ -> ()
    with ex ->
        errors.Add(sprintf "exception ledger is malformed JSON: %s" ex.Message)

    entries |> List.ofSeq, errors |> List.ofSeq

let private exactWorkspacePathError label (path: string) =
    let normalized = normalizeWorkspacePath path
    let segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)

    if path <> path.Trim() then Some(sprintf "%s must not have surrounding whitespace" label)
    elif String.IsNullOrWhiteSpace normalized then Some(sprintf "%s must not be empty" label)
    elif Path.IsPathRooted path || path.StartsWith("/", StringComparison.Ordinal) then Some(sprintf "%s must be workspace-relative" label)
    elif path.Contains('\\') then Some(sprintf "%s must use '/' separators" label)
    elif path.Contains("//", StringComparison.Ordinal) then Some(sprintf "%s must use canonical single '/' separators" label)
    elif path |> Seq.exists Char.IsControl then Some(sprintf "%s must not contain control characters" label)
    elif path.IndexOfAny([| '*'; '?'; '['; ']' |]) >= 0 then Some(sprintf "%s must be exact; wildcard paths are overbroad" label)
    elif segments |> Array.exists (fun segment -> segment = "." || segment = "..") then Some(sprintf "%s must not contain '.' or '..' segments" label)
    elif normalized.EndsWith("/", StringComparison.Ordinal) then Some(sprintf "%s must name a file, not a directory" label)
    else None

let private exceptionBindingKey (entry: AuditBindingExceptionEntry) =
    entry.audit, entry.findingId, entry.locator, entry.path, entry.priorSha256

let private auditBindingKey (binding: InvalidatedAuditBinding) =
    binding.audit, binding.findingId, binding.locator, binding.path, binding.priorSha256

let private explicitlyNamesWorkspacePath (locator: string) (path: string) =
    Regex.IsMatch(
        locator,
        @"(^|[\s""'=:(,])" + Regex.Escape(path) + @"($|[\s""',;)])",
        RegexOptions.CultureInvariant
    )

let private applyAuditBindingExceptions (candidate: CandidateEvidenceSubject) (result: AuditInvalidationCheck) =
    match candidate.readBytes auditBindingExceptionsPath with
    | Error detail ->
        { result with errors = (sprintf "invalidation: could not read exception ledger from %s: %s" candidate.subject detail) :: result.errors |> List.sort }
    | Ok None -> result
    | Ok(Some ledgerBytes) ->
        let entries, parseErrors = parseAuditBindingExceptionLedger ledgerBytes
        let errors = ResizeArray<string>(parseErrors)
        let digestPattern = "^[0-9a-f]{64}$"

        entries
        |> List.countBy _.id
        |> List.iter (fun (id, count) ->
            if count > 1 then errors.Add(sprintf "exception ledger contains duplicate id '%s'" id))

        entries
        |> List.countBy exceptionBindingKey
        |> List.iter (fun ((audit, findingId, locator, path, prior), count) ->
            if count > 1 then
                errors.Add(sprintf "exception ledger contains duplicate binding %s %s %s %s %s" audit findingId locator path prior))

        let bindingCounts = result.bindings |> List.countBy auditBindingKey |> Map.ofList
        let knownBindings = bindingCounts |> Map.keys |> Set.ofSeq

        for entry in entries do
            if not (Regex.IsMatch(entry.id, "^[a-z0-9][a-z0-9._-]*$")) then
                errors.Add(sprintf "exception ledger entry '%s' has an invalid id" entry.id)

            for label, path in [ "audit", entry.audit; "path", entry.path; "replacementPath", entry.replacementPath ] do
                match exactWorkspacePathError (sprintf "exception ledger entry '%s' %s" entry.id label) path with
                | Some error -> errors.Add error
                | None -> ()

            if not (entry.audit.EndsWith(".audit.json", StringComparison.Ordinal)) then
                errors.Add(sprintf "exception ledger entry '%s' audit must name one .audit.json file" entry.id)

            if entry.locator <> "file:" + entry.path then
                errors.Add(sprintf "exception ledger entry '%s' locator must be exactly file:%s" entry.id entry.path)

            if not (Regex.IsMatch(entry.priorSha256, digestPattern)) then
                errors.Add(sprintf "exception ledger entry '%s' priorSha256 must be 64 lowercase hex characters" entry.id)

            if not (Regex.IsMatch(entry.replacementSha256, digestPattern)) then
                errors.Add(sprintf "exception ledger entry '%s' replacementSha256 must be 64 lowercase hex characters" entry.id)

            if not (entry.evidenceLocator.StartsWith("command:", StringComparison.Ordinal)) then
                errors.Add(sprintf "exception ledger entry '%s' evidenceLocator must be a command: locator" entry.id)
            elif containsPrivateLocatorMaterial entry.evidenceLocator then
                errors.Add(sprintf "exception ledger entry '%s' evidenceLocator contains private or host-specific material" entry.id)
            elif not (explicitlyNamesWorkspacePath entry.evidenceLocator entry.replacementPath) then
                errors.Add(sprintf "exception ledger entry '%s' evidenceLocator must explicitly inspect %s" entry.id entry.replacementPath)

            if not (knownBindings.Contains(exceptionBindingKey entry)) then
                errors.Add(sprintf "exception ledger entry '%s' is unused: no immutable audit binding matches it exactly" entry.id)
            elif bindingCounts.[exceptionBindingKey entry] <> 1 then
                errors.Add(sprintf "exception ledger entry '%s' is overbroad: it matches multiple immutable audit bindings" entry.id)

            match exactWorkspacePathError (sprintf "exception ledger entry '%s' replacementPath" entry.id) entry.replacementPath with
            | Some _ -> ()
            | None ->
                match candidate.readBytes entry.replacementPath with
                | Error detail -> errors.Add(sprintf "exception ledger entry '%s' replacement evidence is unreadable: %s" entry.id detail)
                | Ok None -> errors.Add(sprintf "exception ledger entry '%s' replacement evidence is missing: %s" entry.id entry.replacementPath)
                | Ok(Some bytes) ->
                    match sha256EvidenceBytes bytes with
                    | Error detail -> errors.Add(sprintf "exception ledger entry '%s' %s" entry.id detail)
                    | Ok actual when actual <> entry.replacementSha256 ->
                        errors.Add(sprintf "exception ledger entry '%s' replacement digest is stale: expected %s, got %s" entry.id entry.replacementSha256 actual)
                    | Ok _ -> ()

        if errors.Count > 0 then
            { result with errors = (result.errors @ List.ofSeq errors) |> List.sort }
        else
            let byBinding = entries |> List.map (fun entry -> exceptionBindingKey entry, entry) |> Map.ofList
            let applied, remaining =
                result.invalidated
                |> List.partition (auditBindingKey >> byBinding.ContainsKey)

            let dispositions : AppliedAuditBindingException list =
                applied
                |> List.map (fun binding ->
                    let entry : AuditBindingExceptionEntry = byBinding.[auditBindingKey binding]
                    let disposition : AppliedAuditBindingException =
                        { id = entry.id
                          audit = entry.audit
                          findingId = entry.findingId
                          locator = entry.locator
                          path = entry.path
                          priorSha256 = entry.priorSha256
                          replacementPath = entry.replacementPath
                          replacementSha256 = entry.replacementSha256
                          evidenceLocator = entry.evidenceLocator }
                    disposition)
                |> List.sortBy (fun (disposition: AppliedAuditBindingException) -> disposition.audit, disposition.findingId, disposition.path, disposition.id)

            { result with invalidated = remaining; dispositions = dispositions }

/// The working-tree spelling of the scan, preserved verbatim for `--changed` and
/// for direct library callers. Its index subject is the checkout on disk.
let findInvalidatedAuditBindings (workspaceRoot: string) (changedPaths: string list) =
    let root = Path.GetFullPath workspaceRoot
    let candidate =
        { subject = workingTreeSubject
          readBytes =
            fun relative ->
                try
                    let path = Path.GetFullPath(Path.Combine(root, relative))
                    let canonicalRoot = canonicalizeExistingSegments root
                    let canonicalPath = canonicalizeExistingSegments path
                    let info = FileInfo path
                    if not (isInside canonicalRoot canonicalPath) then Error "path resolves outside the workspace"
                    elif not (String.IsNullOrWhiteSpace info.LinkTarget) then Error "path is a symbolic link, not a regular file"
                    elif Directory.Exists path then Error "path names a directory"
                    elif File.Exists path then Ok(Some(File.ReadAllBytes path))
                    else Ok None
                with ex -> Error ex.Message }

    findInvalidatedAuditBindingsIn (workingTreeAuditIndex workspaceRoot) changedPaths
    |> applyAuditBindingExceptions candidate

let private workspaceRelative (workspaceRoot: string) (resolved: string) =
    let canonicalRoot = canonicalizeExistingSegments workspaceRoot

    Path
        .GetRelativePath(canonicalRoot, resolved)
        .Replace(Path.DirectorySeparatorChar, '/')

let private digestExemption relative =
    if String.Equals(relative, auditBindingExceptionsPath, StringComparison.Ordinal) then
        Some auditBindingExceptionsReason
    else
        None

let private resolveEvidencePath (workspaceRoot: string) (locator: string) =
    if
        String.IsNullOrWhiteSpace locator
        || not (locator.StartsWith("file:", StringComparison.Ordinal))
    then
        None
    else
        let relative = locator.Substring("file:".Length).Trim()

        if String.IsNullOrWhiteSpace relative || Path.IsPathRooted relative then
            None
        else
            let root = Path.GetFullPath workspaceRoot
            let candidate = Path.GetFullPath(Path.Combine(root, relative))

            if not (isInside root candidate) then
                None
            else
                let canonicalRoot = canonicalizeExistingSegments root
                let canonicalCandidate = canonicalizeExistingSegments candidate

                if isInside canonicalRoot canonicalCandidate then
                    Some canonicalCandidate
                else
                    None

type private GitRun =
    { exitCode: int
      stdout: string
      stderr: string }

let private runGit (workspaceRoot: string) (arguments: string list) =
    try
        let start = ProcessStartInfo("git")
        start.WorkingDirectory <- workspaceRoot
        start.UseShellExecute <- false
        start.RedirectStandardOutput <- true
        start.RedirectStandardError <- true
        // Pin the decoding rather than inherit the console's. Audit documents now
        // travel through this pipe (`baseTreeAuditIndex`), Git writes them as the
        // UTF-8 bytes it stored, and this repository's own audits carry non-ASCII
        // finding ids (`§4.1`) — so an inherited encoding would decide whether an
        // audit parses. The other callers read hex object names and are indifferent.
        start.StandardOutputEncoding <- UTF8Encoding false
        start.StandardErrorEncoding <- UTF8Encoding false
        arguments |> List.iter start.ArgumentList.Add

        match Process.Start start with
        | null -> Error "could not start git"
        | child ->
            use child = child
            let stdout = child.StandardOutput.ReadToEndAsync()
            let stderr = child.StandardError.ReadToEndAsync()
            child.WaitForExit()

            Ok
                { exitCode = child.ExitCode
                  stdout = stdout.Result
                  stderr = stderr.Result.Trim() }
    with ex ->
        Error ex.Message

let private readGitBlob (workspaceRoot: string) (reference: string) (relative: string) =
    match runGit workspaceRoot [ "ls-tree"; "-z"; reference; "--"; relative ] with
    | Error detail -> Error detail
    | Ok listing when listing.exitCode <> 0 -> Error listing.stderr
    | Ok listing when String.IsNullOrEmpty listing.stdout -> Ok None
    | Ok listing ->
        let records = listing.stdout.Split('\000', StringSplitOptions.RemoveEmptyEntries)

        let parsed =
            if records.Length <> 1 then
                Error "path does not resolve to one exact entry in the candidate tree"
            else
                let separator = records.[0].IndexOf '\t'

                if separator <= 0 then
                    Error "candidate tree entry is malformed"
                else
                    let header = records.[0].Substring(0, separator).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    let path = records.[0].Substring(separator + 1)

                    if header.Length <> 3 || path <> relative then
                        Error "path does not resolve to one exact entry in the candidate tree"
                    else
                        Ok(header.[0], header.[1])

        match parsed with
        | Error detail -> Error detail
        | Ok(mode, objectType) when
            objectType <> "blob"
            || (mode <> "100644" && mode <> "100755") ->
            Error(sprintf "path is not a regular Git blob (mode %s type %s)" mode objectType)
        | Ok _ ->
            try
                let start = ProcessStartInfo("git")
                start.WorkingDirectory <- workspaceRoot
                start.UseShellExecute <- false
                start.RedirectStandardOutput <- true
                start.RedirectStandardError <- true
                start.ArgumentList.Add "cat-file"
                start.ArgumentList.Add "blob"
                start.ArgumentList.Add(reference + ":" + relative)

                match Process.Start start with
                | null -> Error "could not start git"
                | child ->
                    use child = child
                    use output = new MemoryStream()
                    let copy = child.StandardOutput.BaseStream.CopyToAsync output
                    let error = child.StandardError.ReadToEndAsync()
                    child.WaitForExit()
                    copy.Wait()

                    if child.ExitCode = 0 then Ok(Some(output.ToArray()))
                    else Error(error.Result.Trim())
            with ex -> Error ex.Message

/// Index the audits present in the tree of `reference` — the MERGED state a
/// candidate is being checked against, and the only index that can answer the
/// question `check-invalidation` asks.
///
/// It reads Git, never the disk. An audit the candidate introduced is absent from
/// this index and so cannot select itself (#1243); an audit the candidate deleted,
/// renamed or rewrote is still IN it and so keeps guarding the evidence it cites,
/// which closes the same defect's other half — clearing the refusal by editing the
/// durable audit out of the way.
///
/// A ref that does not resolve is an error, not an empty index. A ref that resolves
/// to a tree with no `feedback/audits` entries genuinely has no merged audits, and
/// `git ls-tree` reports that as success with no output; the two are kept apart.
let baseTreeAuditIndex (workspaceRoot: string) (reference: string) : AuditIndex =
    let subject = baseRefSubject reference

    let failed detail =
        { subject = subject
          audits = []
          errors = [ sprintf "invalidation: could not read the audit index at %s: %s" reference detail ] }

    match runGit workspaceRoot [ "ls-tree"; "-r"; "-z"; "--name-only"; reference; "--"; auditIndexRoot ] with
    | Error detail -> failed detail
    | Ok listing when listing.exitCode <> 0 -> failed listing.stderr
    | Ok listing ->
        let audits =
            listing.stdout.Split('\000', StringSplitOptions.RemoveEmptyEntries)
            |> Array.map normalizeWorkspacePath
            |> Array.filter (fun entry -> entry.EndsWith(".audit.json", StringComparison.Ordinal))
            |> Array.sort
            |> Array.map (fun entry ->
                let document =
                    match runGit workspaceRoot [ "cat-file"; "blob"; reference + ":" + entry ] with
                    | Error detail -> Error detail
                    | Ok blob when blob.exitCode <> 0 -> Error blob.stderr
                    | Ok blob -> Ok blob.stdout

                { auditPath = entry; document = document })
            |> List.ofArray

        { subject = subject
          audits = audits
          errors = [] }

/// The complete changed-path set between two refs: rename and copy records
/// contribute BOTH names and deletions contribute their removed source, exactly as
/// `changedPathsFromNameStatus` documents. Fail-closed on an unresolvable ref —
/// "git could not tell us what changed" is never "nothing changed".
let changedPathsBetween (workspaceRoot: string) (baseRef: string) (headRef: string) =
    match runGit workspaceRoot [ "diff"; "--name-status"; "--find-renames"; "--find-copies"; baseRef; headRef ] with
    | Error detail -> Error(sprintf "invalidation: could not read commit path changes: %s" detail)
    | Ok diff when diff.exitCode <> 0 -> Error(sprintf "invalidation: could not read commit path changes: %s" diff.stderr)
    | Ok diff -> Ok(changedPathsFromNameStatus diff.stdout)

/// The commit-aware check: audits indexed from `baseRef`'s tree, changed paths
/// derived from `baseRef`→`headRef`. Both halves share one left-hand side by
/// construction, which is the property #1243 found missing.
let checkInvalidationBetweenRefs (workspaceRoot: string) (baseRef: string) (headRef: string) =
    let index = baseTreeAuditIndex workspaceRoot baseRef

    match changedPathsBetween workspaceRoot baseRef headRef with
    | Error detail ->
        { subject = index.subject
          errors = detail :: index.errors |> List.sort
          invalidated = []
          dispositions = []
          bindings = [] }
    | Ok changed ->
        let candidate =
            { subject = sprintf "head ref %s" headRef
              readBytes = readGitBlob workspaceRoot headRef }

        findInvalidatedAuditBindingsIn index changed
        |> applyAuditBindingExceptions candidate

let private reportCommit reportText =
    frontmatter reportText
    |> Option.bind (Map.tryFind "commit")
    |> Option.map _.Trim()
    |> Option.filter (String.IsNullOrWhiteSpace >> not)

let private boundedFileEvidenceGuidance =
    "commit the artifact, use a stable committed receipt, or use a command: locator that regenerates and inspects it"

/// Resolve the immutable Git tree a feedback report describes.  This is intentionally
/// separate from the working-tree path checks: a local file is not evidence that a
/// reviewer can recover from the report's stated commit.
let private resolveReportHead (workspaceRoot: string) (reportText: string) =
    match runGit workspaceRoot [ "rev-parse"; "--is-inside-work-tree" ] with
    | Error detail -> Error(sprintf "cannot establish Git workspace state (%s); %s" detail boundedFileEvidenceGuidance)
    | Ok state when state.exitCode <> 0 || state.stdout.Trim() <> "true" ->
        Error(sprintf "cannot establish Git workspace state (%s); %s" state.stderr boundedFileEvidenceGuidance)
    | Ok _ ->
        match reportCommit reportText with
        | None -> Error(sprintf "cannot establish the report commit; %s" boundedFileEvidenceGuidance)
        | Some commit ->
            match runGit workspaceRoot [ "rev-parse"; "--verify"; commit + "^{commit}" ] with
            | Ok resolved when resolved.exitCode = 0 -> Ok(resolved.stdout.Trim())
            | Ok resolved ->
                Error(
                    sprintf
                        "cannot resolve report commit '%s' (%s); %s"
                        commit
                        resolved.stderr
                        boundedFileEvidenceGuidance
                )
            | Error detail -> Error(sprintf "cannot resolve report commit '%s' (%s); %s" commit detail boundedFileEvidenceGuidance)

let private committedFileText (workspaceRoot: string) (head: string) (relative: string) =
    let absent classification =
        Error(sprintf "file evidence is %s at report head: file:%s; %s" classification relative boundedFileEvidenceGuidance)

    match runGit workspaceRoot [ "ls-tree"; "-z"; head; "--"; relative ] with
    | Error detail -> Error(sprintf "cannot inspect Git tree for file:%s (%s); %s" relative detail boundedFileEvidenceGuidance)
    | Ok tree when tree.exitCode <> 0 ->
        Error(sprintf "cannot inspect Git tree for file:%s (%s); %s" relative tree.stderr boundedFileEvidenceGuidance)
    | Ok tree when String.IsNullOrEmpty tree.stdout ->
        match runGit workspaceRoot [ "check-ignore"; "--quiet"; "--"; relative ] with
        | Ok ignored when ignored.exitCode = 0 -> absent "ignored"
        | Ok ignored when ignored.exitCode <> 1 ->
            Error(sprintf "cannot classify file:%s in Git (%s); %s" relative ignored.stderr boundedFileEvidenceGuidance)
        | Error detail -> Error(sprintf "cannot classify file:%s in Git (%s); %s" relative detail boundedFileEvidenceGuidance)
        | Ok _ ->
            match runGit workspaceRoot [ "ls-files"; "--error-unmatch"; "--"; relative ] with
            | Ok tracked when tracked.exitCode = 0 -> absent "absent"
            | Ok tracked when tracked.exitCode = 1 && File.Exists(Path.Combine(workspaceRoot, relative)) -> absent "untracked"
            | Ok tracked when tracked.exitCode = 1 -> absent "absent"
            | Ok tracked -> Error(sprintf "cannot classify file:%s in Git (%s); %s" relative tracked.stderr boundedFileEvidenceGuidance)
            | Error detail -> Error(sprintf "cannot classify file:%s in Git (%s); %s" relative detail boundedFileEvidenceGuidance)
    | Ok tree ->
        let metadata = tree.stdout.Split('\000').[0]
        let fields = metadata.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)

        if fields.Length < 3 || not (fields.[0].StartsWith("100", StringComparison.Ordinal)) then
            Error(sprintf "file evidence is not a regular committed file at report head: file:%s; %s" relative boundedFileEvidenceGuidance)
        else
            match runGit workspaceRoot [ "show"; head + ":" + relative ] with
            | Ok content when content.exitCode = 0 -> Ok content.stdout
            | Ok content -> Error(sprintf "cannot read committed file evidence file:%s (%s); %s" relative content.stderr boundedFileEvidenceGuidance)
            | Error detail -> Error(sprintf "cannot read committed file evidence file:%s (%s); %s" relative detail boundedFileEvidenceGuidance)

let private validateActionabilityAuditDetailedWithGitTree
    (requireCommittedTree: bool)
    (workspaceRoot: string)
    (reportPath: string)
    (reportText: string)
    (auditText: string)
    =
    let errors = ResizeArray<string>()
    let notBound = ResizeArray<NotBoundCitation>()
    let reportHead = lazy (resolveReportHead workspaceRoot reportText)

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

        let audits =
            (if isNull (box audit.findings) then [] else audit.findings)
            |> List.choose (fun finding ->
                if obj.ReferenceEquals(box finding, null) then
                    errors.Add "audit: findings must not contain null entries"
                    None
                else
                    Some finding)

        for id, kind, declaredEvidence in expectedFindings do
            let matches =
                audits
                |> List.filter (fun finding -> normalizedJsonString finding.id = id)

            if List.isEmpty matches then
                errors.Add(sprintf "audit: missing finding '%s'" id)
            elif matches.Length > 1 then
                errors.Add(sprintf "audit: duplicate finding '%s'" id)
            else
                let finding = matches.Head
                let status = normalizedJsonString finding.status

                if not (List.contains status criticStatuses) then
                    errors.Add(sprintf "audit: %s has unknown status '%s'" id status)

                if kind = "positive-pattern" && status <> "positive-pattern" then
                    errors.Add(sprintf "audit: %s positive-pattern must keep that disposition" id)

                if kind <> "positive-pattern" && status = "positive-pattern" then
                    errors.Add(sprintf "audit: %s is not a positive-pattern finding" id)

                if status = "incomplete" || status = "unsupported" then
                    errors.Add(
                        sprintf
                            "actionability: %s remains %s and cannot be handed off as actionable"
                            id
                            status
                    )

                let missingFacts =
                    if isNull (box finding.missingFacts) then [] else finding.missingFacts

                if
                    (status = "actionable" || status = "positive-pattern")
                    && not (List.isEmpty missingFacts)
                then
                    errors.Add(
                        sprintf
                            "audit: %s cannot be %s while missing facts are recorded"
                            id
                            status
                    )

                let checks =
                    (if isNull (box finding.checkedEvidence) then
                         []
                     else
                         finding.checkedEvidence)
                    |> List.choose (fun check ->
                        if obj.ReferenceEquals(box check, null) then
                            errors.Add(sprintf "audit: %s checkedEvidence must not contain null entries" id)
                            None
                        else
                            Some check)

                if List.isEmpty checks then
                    errors.Add(sprintf "audit: %s has no checked evidence" id)

                let checkedLocators =
                    checks |> List.map (fun check -> normalizedJsonString check.locator) |> Set.ofList

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
                    let locator = normalizedJsonString check.locator
                    let result = normalizedJsonString check.result

                    let digest =
                        check.sha256
                        |> Option.map normalizedJsonString

                    if String.IsNullOrWhiteSpace locator then
                        errors.Add(sprintf "audit: %s evidence locator must not be empty" id)
                    elif containsPrivateLocatorMaterial locator then
                        errors.Add(
                            sprintf
                                "audit: %s evidence locator exposes an absolute path or secret material"
                                id
                        )

                    match digest with
                    | Some value when not (Regex.IsMatch(value, "^[0-9a-f]{64}$")) ->
                        errors.Add(sprintf "audit: %s evidence sha256 must be 64 lowercase hex characters" id)
                    | _ -> ()

                    if not (List.contains result evidenceResults) then
                        errors.Add(
                            sprintf "audit: %s evidence has unknown result '%s'" id result
                        )

                    if
                        (status = "actionable" || status = "positive-pattern")
                        && result <> "verified"
                    then
                        errors.Add(
                            sprintf
                                "audit: %s cannot be %s with evidence result '%s'"
                                id
                                status
                                result
                        )

                    if locator.StartsWith("file:", StringComparison.Ordinal) then
                        match resolveEvidencePath workspaceRoot locator with
                        | None ->
                            errors.Add(
                                sprintf
                                    "audit: %s evidence locator must be a workspace-relative file: path"
                                    id
                            )
                        | Some path ->
                            let relative = workspaceRelative workspaceRoot path

                            let validateDigest text =
                                match digestExemption relative with
                                | Some reason ->
                                    notBound.Add
                                        { findingId = id
                                          locator = locator
                                          path = relative
                                          reason = reason }
                                | None ->
                                    match digest with
                                    | None -> errors.Add(sprintf "audit: %s file evidence needs sha256" id)
                                    | Some digest when digest <> sha256Text text ->
                                        errors.Add(sprintf "audit: %s evidence digest is stale: %s" id locator)
                                    | Some _ -> ()

                            if requireCommittedTree then
                                match reportHead.Force() with
                                | Error detail -> errors.Add(sprintf "audit: %s %s" id detail)
                                | Ok head ->
                                    match committedFileText workspaceRoot head relative with
                                    | Error detail -> errors.Add(sprintf "audit: %s %s" id detail)
                                    | Ok committedText -> validateDigest committedText
                            elif not (File.Exists path) then
                                errors.Add(sprintf "audit: %s evidence file is missing: %s" id locator)
                            else
                                File.ReadAllText path |> validateDigest
                    elif not (String.IsNullOrWhiteSpace locator) && Path.IsPathRooted locator then
                        errors.Add(sprintf "audit: %s evidence locator exposes an absolute path" id)

        let expectedIds = expectedFindings |> List.map (fun (id, _, _) -> id) |> Set.ofList

        for finding in audits do
            let findingId = normalizedJsonString finding.id

            if String.IsNullOrWhiteSpace findingId then
                errors.Add "audit: finding id must not be empty"
            elif not (Set.contains findingId expectedIds) then
                errors.Add(sprintf "audit: unknown finding '%s'" findingId)

    { errors = List.ofSeq errors
      notBound =
        notBound
        |> Seq.distinctBy (fun citation -> citation.findingId, citation.locator)
        |> Seq.sortBy (fun citation -> citation.findingId, citation.locator)
        |> List.ofSeq }

/// The reusable validation core keeps filesystem-only behavior for embedders that
/// intentionally validate an in-memory or synthetic fixture.
let validateActionabilityAuditDetailed
    (workspaceRoot: string)
    (reportPath: string)
    (reportText: string)
    (auditText: string)
    =
    validateActionabilityAuditDetailedWithGitTree false workspaceRoot reportPath reportText auditText

/// The command-facing validator proves every `file:` locator from the report's
/// committed Git tree, rather than accepting an artifact created in a dirty tree.
let validateActionabilityAuditAtReportHeadDetailed
    (workspaceRoot: string)
    (reportPath: string)
    (reportText: string)
    (auditText: string)
    =
    validateActionabilityAuditDetailedWithGitTree true workspaceRoot reportPath reportText auditText

/// Compatibility wrapper for existing callers that only need validation errors.
let validateActionabilityAudit
    (workspaceRoot: string)
    (reportPath: string)
    (reportText: string)
    (auditText: string)
    =
    (validateActionabilityAuditDetailed workspaceRoot reportPath reportText auditText).errors

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

type ZeroEventActivationReceipt =
    { activationSchema: int
      receiptKind: string
      timestampUtc: string
      cycle: string
      exercisedPhases: string list
      evidence: string list
      reasonNoEventQualified: string }

let private requireValue name value =
    if String.IsNullOrWhiteSpace value then
        invalidArg name (sprintf "%s must not be empty" name)

let private requireCycle cycle =
    requireValue "cycle" cycle

    if not (Regex.IsMatch(cycle, "^[a-z0-9][a-z0-9-]*$")) then
        invalidArg "cycle" "cycle must be lowercase letters, digits, and hyphens"

let private checkpointDirectory root =
    Path.Combine(root, "feedback", "checkpoints")

let private checkpointEventPath root cycle =
    Path.Combine(checkpointDirectory root, cycle + ".jsonl")

let activationReceiptPath root cycle =
    Path.Combine(checkpointDirectory root, cycle + ".activation.json")

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

    requireCycle cycle

    if not (List.contains surface surfaces) then
        invalidArg "surface" (sprintf "unknown surface '%s'" surface)

    if not (List.contains kind kinds) then
        invalidArg "kind" (sprintf "unknown kind '%s'" kind)

    let receiptPath = activationReceiptPath root cycle

    if File.Exists receiptPath || Directory.Exists receiptPath then
        invalidArg
            "cycle"
            (sprintf
                "cycle '%s' already has a zero-event activation receipt; remove the contradiction before recording an event"
                cycle)

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

    let directory = checkpointDirectory root
    Directory.CreateDirectory directory |> ignore
    let path = checkpointEventPath root cycle
    let line = JsonSerializer.Serialize checkpoint + Environment.NewLine
    File.AppendAllText(path, line, UTF8Encoding(false))
    path

let appendZeroEventActivation root cycle exercisedPhases evidence reasonNoEventQualified =
    requireCycle cycle
    requireValue "reason" reasonNoEventQualified

    let requireNonEmptyValues name values =
        if List.isEmpty values then
            invalidArg name (sprintf "%s must contain at least one value" name)

        for value in values do
            requireValue name value

    requireNonEmptyValues "phases" exercisedPhases
    requireNonEmptyValues "evidence" evidence

    if exercisedPhases |> List.distinct |> List.length <> List.length exercisedPhases then
        invalidArg "phases" "phases must not contain duplicates"

    if evidence |> List.exists containsPrivateLocatorMaterial then
        invalidArg "evidence" "evidence must not expose an absolute path or secret material"

    let eventPath = checkpointEventPath root cycle

    if File.Exists eventPath || Directory.Exists eventPath then
        invalidArg
            "cycle"
            (sprintf
                "cycle '%s' already has checkpoint event state; a zero-event receipt would contradict it"
                cycle)

    let directory = checkpointDirectory root
    Directory.CreateDirectory directory |> ignore
    let path = activationReceiptPath root cycle

    let receipt =
        { activationSchema = 1
          receiptKind = "zero-event-activation"
          timestampUtc = DateTimeOffset.UtcNow.ToString "O"
          cycle = cycle
          exercisedPhases = exercisedPhases
          evidence = evidence
          reasonNoEventQualified = reasonNoEventQualified }

    use stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)
    use writer = new StreamWriter(stream, UTF8Encoding(false))
    writer.Write(JsonSerializer.Serialize receipt)
    writer.Write Environment.NewLine
    path

let private validateCheckpointFileForCycle (expectedCycle: string) (path: string) =
    let errors = ResizeArray<string>()

    if not (File.Exists path) then
        [ sprintf "checkpoints: file not found: %s" path ]
    else
        try
            let mutable lineCount = 0

            for index, line in File.ReadLines path |> Seq.indexed do
                lineCount <- lineCount + 1

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
                        let cycle = valueOf "cycle"
                        let surface = valueOf "surface"
                        let kind = valueOf "kind"

                        if cycle <> expectedCycle then
                            errors.Add(
                                sprintf
                                    "checkpoints: line %d cycle must be '%s', got '%s'"
                                    (index + 1)
                                    expectedCycle
                                    cycle
                            )

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
                            sprintf
                                "checkpoints: line %d is invalid JSON: %s"
                                (index + 1)
                                ex.Message
                        )

            if lineCount = 0 then
                errors.Add(
                    "checkpoints: event file contains no events; record a zero-event activation receipt"
                )

            List.ofSeq errors
        with ex ->
            [ sprintf "checkpoints: state is unreadable: %s" ex.Message ]

let validateCheckpointFile (path: string) =
    let expectedCycle =
        Path.GetFileNameWithoutExtension path |> Option.ofObj |> Option.defaultValue ""

    validateCheckpointFileForCycle expectedCycle path

let validateZeroEventActivationFile (workspaceRoot: string) (expectedCycle: string) (path: string) =
    if Directory.Exists path then
        [ sprintf "checkpoints: zero-event activation receipt is unreadable: %s" path ]
    elif not (File.Exists path) then
        [ sprintf "checkpoints: zero-event activation receipt not found: %s" path ]
    else
        let errors = ResizeArray<string>()

        try
            let canonicalRoot = canonicalizeExistingSegments workspaceRoot
            let canonicalPath = canonicalizeExistingSegments path

            if not (isInside canonicalRoot canonicalPath) then
                raise (
                    InvalidDataException(
                        "zero-event activation receipt resolves outside the workspace"
                    )
                )

            use document = JsonDocument.Parse(File.ReadAllText canonicalPath)
            let root = document.RootElement

            if root.ValueKind <> JsonValueKind.Object then
                raise (JsonException "activation receipt root must be a JSON object")

            let allowedProperties =
                set
                    [ "activationSchema"
                      "receiptKind"
                      "timestampUtc"
                      "cycle"
                      "exercisedPhases"
                      "evidence"
                      "reasonNoEventQualified" ]

            let properties = root.EnumerateObject() |> Seq.toList

            for property in properties do
                if not (Set.contains property.Name allowedProperties) then
                    errors.Add(
                        sprintf
                            "checkpoints: activation receipt contains unknown property '%s'"
                            property.Name
                    )

            for name, count in
                properties
                |> List.countBy (fun property -> property.Name)
                |> List.filter (fun (_, count) -> count > 1) do
                errors.Add(
                    sprintf "checkpoints: activation receipt contains duplicate property '%s'" name
                )

            let readString (name: string) =
                match root.TryGetProperty name with
                | true, value when value.ValueKind = JsonValueKind.String ->
                    value.GetString() |> Option.ofObj |> Option.defaultValue ""
                | _ ->
                    errors.Add(sprintf "checkpoints: activation receipt is missing %s" name)
                    ""

            let readStringArray (name: string) =
                match root.TryGetProperty name with
                | true, value when value.ValueKind = JsonValueKind.Array ->
                    [ for item in value.EnumerateArray() do
                          if item.ValueKind = JsonValueKind.String then
                              yield item.GetString() |> Option.ofObj |> Option.defaultValue ""
                          else
                              errors.Add(
                                  sprintf
                                      "checkpoints: activation receipt %s must contain only strings"
                                      name
                              ) ]
                | _ ->
                    errors.Add(sprintf "checkpoints: activation receipt is missing %s" name)
                    []

            match root.TryGetProperty "activationSchema" with
            | true, value when value.ValueKind = JsonValueKind.Number ->
                match value.TryGetInt32() with
                | true, 1 -> ()
                | _ -> errors.Add "checkpoints: activationSchema must be 1"
            | _ -> errors.Add "checkpoints: activation receipt is missing activationSchema"

            let receiptKind = readString "receiptKind"
            let timestampUtc = readString "timestampUtc"
            let cycle = readString "cycle"
            let phases = readStringArray "exercisedPhases"
            let evidence = readStringArray "evidence"
            let reason = readString "reasonNoEventQualified"

            if receiptKind <> "zero-event-activation" then
                errors.Add "checkpoints: receiptKind must be zero-event-activation"

            match DateTimeOffset.TryParse timestampUtc with
            | true, timestamp when timestamp.Offset = TimeSpan.Zero -> ()
            | _ -> errors.Add "checkpoints: timestampUtc must be a UTC timestamp"

            if cycle <> expectedCycle then
                errors.Add(
                    sprintf
                        "checkpoints: activation receipt cycle must be '%s', got '%s'"
                        expectedCycle
                        cycle
                )

            for name, values in [ "exercisedPhases", phases; "evidence", evidence ] do
                if List.isEmpty values then
                    errors.Add(sprintf "checkpoints: activation receipt %s must not be empty" name)

                if values |> List.exists String.IsNullOrWhiteSpace then
                    errors.Add(
                        sprintf
                            "checkpoints: activation receipt %s must not contain empty values"
                            name
                    )

            if phases |> List.distinct |> List.length <> List.length phases then
                errors.Add "checkpoints: activation receipt exercisedPhases must not contain duplicates"

            if evidence |> List.exists containsPrivateLocatorMaterial then
                errors.Add(
                    "checkpoints: activation receipt evidence exposes an absolute path or secret material"
                )

            if String.IsNullOrWhiteSpace reason then
                errors.Add "checkpoints: activation receipt reasonNoEventQualified must not be empty"

            List.ofSeq errors
        with
        | :? JsonException as ex ->
            [ sprintf "checkpoints: activation receipt is malformed JSON: %s" ex.Message ]
        | :? InvalidDataException as ex ->
            [ sprintf "checkpoints: zero-event activation receipt is unreadable: %s" ex.Message ]
        | ex ->
            [ sprintf "checkpoints: zero-event activation receipt is unreadable: %s" ex.Message ]

let validateCheckpointState (root: string) (cycle: string) =
    let errors = ResizeArray<string>()

    try
        requireCycle cycle
    with :? ArgumentException as ex ->
        errors.Add(sprintf "checkpoints: %s" ex.Message)

    if errors.Count > 0 then
        List.ofSeq errors
    else
        let eventPath = checkpointEventPath root cycle
        let receiptPath = activationReceiptPath root cycle
        let hasEvents = File.Exists eventPath || Directory.Exists eventPath
        let hasReceipt = File.Exists receiptPath || Directory.Exists receiptPath

        match hasEvents, hasReceipt with
        | true, true ->
            [ sprintf
                  "checkpoints: cycle '%s' has both checkpoint events and a zero-event activation receipt"
                  cycle ]
        | true, false when Directory.Exists eventPath ->
            [ sprintf "checkpoints: checkpoint event state is unreadable: %s" eventPath ]
        | true, false ->
            try
                let canonicalRoot = canonicalizeExistingSegments root
                let canonicalEventPath = canonicalizeExistingSegments eventPath

                if not (isInside canonicalRoot canonicalEventPath) then
                    [ sprintf
                          "checkpoints: checkpoint event state is unreadable: event file resolves outside the workspace" ]
                else
                    validateCheckpointFileForCycle cycle canonicalEventPath
            with ex ->
                [ sprintf "checkpoints: checkpoint event state is unreadable: %s" ex.Message ]
        | false, true -> validateZeroEventActivationFile root cycle receiptPath
        | false, false ->
            [ sprintf
                  "checkpoints: cycle '%s' is missing both checkpoint events and a zero-event activation receipt"
                  cycle ]
