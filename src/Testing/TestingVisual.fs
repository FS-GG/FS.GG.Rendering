namespace FS.GG.UI.Testing

open System
open System.IO
open System.Security.Cryptography
open System.Text
open FS.GG.UI.Scene
open SkiaSharp
// Testing.fs was split into per-domain files; re-open the package namespace AFTER the third-party
// opens so the Testing types win unqualified-name resolution exactly as in the original single file.
open FS.GG.UI.Testing

module VisualCaptureMatrix =
    let private blank (value: string) = String.IsNullOrWhiteSpace value

    let private duplicateValues values =
        values
        |> List.countBy id
        |> List.choose (fun (value, count) -> if count > 1 then Some value else None)

    let private normalRelativePath (relativePath: string) =
        relativePath.Replace('\\', '/').Trim()

    let private pathIsSafe (relativePath: string) =
        let normalized = normalRelativePath relativePath
        let hasParentTraversal =
            normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
            |> Array.exists (fun part -> part = "..")

        not (blank normalized)
        && not (Path.IsPathRooted normalized)
        && not hasParentTraversal

    let targetId (page: VisualPage) (theme: VisualTheme) (size: VisualSize) (relativePath: string) =
        let normalizedPath = normalRelativePath relativePath
        $"{size.Role}:{size.Width}x{size.Height}:{theme.ThemeId}:{page.PageId}:{normalizedPath}"

    let expand (pages: VisualPage list) (themes: VisualTheme list) (sizes: VisualSize list) (pathFor: VisualPage -> VisualTheme -> VisualSize -> string) =
        let pageDuplicates = pages |> List.map _.PageId |> duplicateValues
        let themeDuplicates = themes |> List.map _.ThemeId |> duplicateValues
        let sizeDuplicates = sizes |> List.map (fun size -> $"{size.Role}:{size.Width}x{size.Height}") |> duplicateValues

        let declarationDiagnostics =
            [ for page in pages do
                  if blank page.PageId then
                      "visual page id must be non-empty"
              for theme in themes do
                  if blank theme.ThemeId then
                      "visual theme id must be non-empty"
              for size in sizes do
                  if blank size.Role then
                      "visual size role must be non-empty"
                  if size.Width <= 0 || size.Height <= 0 then
                      $"visual size must be positive: {size.Role}"
              for duplicate in pageDuplicates do
                  $"duplicate page id: {duplicate}"
              for duplicate in themeDuplicates do
                  $"duplicate theme id: {duplicate}"
              for duplicate in sizeDuplicates do
                  $"duplicate size: {duplicate}" ]

        let orderedPages = pages |> List.sortBy (fun page -> page.Order, page.PageId)
        let orderedThemes = themes |> List.sortBy (fun theme -> theme.Order, theme.ThemeId)
        let orderedSizes = sizes |> List.sortBy (fun size -> size.Order, size.Role, size.Width, size.Height)

        let targets =
            [ for size in orderedSizes do
                  for theme in orderedThemes do
                      for page in orderedPages do
                          let relativePath = pathFor page theme size |> normalRelativePath
                          { TargetId = targetId page theme size relativePath
                            Page = page
                            Theme = theme
                            Size = size
                            RelativePath = relativePath
                            Required = page.Required } ]

        let targetDuplicates = targets |> List.map _.TargetId |> duplicateValues
        let pathDuplicates = targets |> List.map _.RelativePath |> duplicateValues

        let targetDiagnostics =
            [ for target in targets do
                  if not (pathIsSafe target.RelativePath) then
                      $"relative path escapes evidence root: {target.RelativePath}"
              for duplicate in targetDuplicates do
                  $"duplicate target id: {duplicate}"
              for duplicate in pathDuplicates do
                  $"duplicate relative path: {duplicate}" ]

        let diagnostics = declarationDiagnostics @ targetDiagnostics
        if diagnostics.IsEmpty then Ok targets else Result.Error diagnostics

module VisualCompleteness =
    let statusText status =
        match status with
        | VisualCaptureComplete -> "complete"
        | VisualCaptureMissing -> "missing"
        | VisualCaptureWrongSize -> "wrong-size"
        | VisualCaptureUndecodable -> "undecodable"
        | VisualCaptureDegraded -> "degraded"
        | VisualCaptureBlocked -> "blocked"

    let private normalizeRelativePath (relativePath: string) =
        relativePath.Replace('\\', '/').Trim()

    let private absolutePath evidenceRoot relativePath =
        Path.Combine(evidenceRoot, normalizeRelativePath relativePath).Replace('/', Path.DirectorySeparatorChar)

    let private hashFile path =
        use stream = File.OpenRead path
        use sha = SHA256.Create()
        sha.ComputeHash stream
        |> Array.map (fun b -> b.ToString("x2"))
        |> String.concat ""

    let private missingArtifact (target: VisualCaptureTarget) =
        { RelativePath = target.RelativePath
          Exists = false
          ByteCount = None
          DecodedWidth = None
          DecodedHeight = None
          ContentHash = None
          DecodeError = Some "missing" }

    let private record (target: VisualCaptureTarget) status (artifact: VisualCaptureArtifact option) reason diagnostics =
        { Target = target
          Status = status
          Artifact = artifact
          ExpectedWidth = target.Size.Width
          ExpectedHeight = target.Size.Height
          ObservedWidth = artifact |> Option.bind _.DecodedWidth
          ObservedHeight = artifact |> Option.bind _.DecodedHeight
          Reason = reason
          Diagnostics = diagnostics }

    let degraded (target: VisualCaptureTarget) reason =
        if String.IsNullOrWhiteSpace reason then
            record target VisualCaptureBlocked None (Some "missing degraded reason") [ "degraded capture requires a non-empty reason" ]
        else
            record target VisualCaptureDegraded None (Some reason) [ $"degraded capture: {reason}" ]

    let private validateOne evidenceRoot (target: VisualCaptureTarget) =
        let path = absolutePath evidenceRoot target.RelativePath

        if not (File.Exists path) then
            let artifact = missingArtifact target
            record target VisualCaptureMissing (Some artifact) (Some "missing artifact") [ $"missing screenshot: {target.RelativePath}" ]
        else
            let info = FileInfo path
            let byteCount = info.Length

            if byteCount = 0L then
                let artifact =
                    { RelativePath = target.RelativePath
                      Exists = true
                      ByteCount = Some byteCount
                      DecodedWidth = None
                      DecodedHeight = None
                      ContentHash = Some(hashFile path)
                      DecodeError = Some "zero-byte artifact" }

                record target VisualCaptureUndecodable (Some artifact) (Some "zero-byte artifact") [ $"zero-byte screenshot: {target.RelativePath}" ]
            else
                try
                    let contentHash = hashFile path
                    use bitmap = SKBitmap.Decode path

                    if isNull bitmap then
                        let artifact =
                            { RelativePath = target.RelativePath
                              Exists = true
                              ByteCount = Some byteCount
                              DecodedWidth = None
                              DecodedHeight = None
                              ContentHash = Some contentHash
                              DecodeError = Some "SKBitmap.Decode returned null" }

                        record target VisualCaptureUndecodable (Some artifact) (Some "undecodable PNG") [ $"undecodable screenshot: {target.RelativePath}" ]
                    else
                        let artifact =
                            { RelativePath = target.RelativePath
                              Exists = true
                              ByteCount = Some byteCount
                              DecodedWidth = Some bitmap.Width
                              DecodedHeight = Some bitmap.Height
                              ContentHash = Some contentHash
                              DecodeError = None }

                        if bitmap.Width = target.Size.Width && bitmap.Height = target.Size.Height then
                            record target VisualCaptureComplete (Some artifact) None []
                        else
                            let diagnostic =
                                $"wrong-size screenshot: {target.RelativePath} expected {target.Size.Width}x{target.Size.Height} observed {bitmap.Width}x{bitmap.Height}"

                            record target VisualCaptureWrongSize (Some artifact) (Some "wrong-size artifact") [ diagnostic ]
                with ex ->
                    let artifact =
                        { RelativePath = target.RelativePath
                          Exists = true
                          ByteCount = Some byteCount
                          DecodedWidth = None
                          DecodedHeight = None
                          ContentHash = None
                          DecodeError = Some ex.Message }

                    record target VisualCaptureUndecodable (Some artifact) (Some "artifact decode failed") [ $"undecodable screenshot: {target.RelativePath}: {ex.Message}" ]

    let private staleDiagnostics evidenceRoot (targets: VisualCaptureTarget list) =
        if Directory.Exists evidenceRoot then
            let targetPaths = targets |> List.map (fun target -> normalizeRelativePath target.RelativePath) |> Set.ofList

            Directory.EnumerateFiles(evidenceRoot, "*.png", SearchOption.AllDirectories)
            |> Seq.choose (fun path ->
                let relative = Path.GetRelativePath(evidenceRoot, path) |> normalizeRelativePath
                if targetPaths.Contains relative then None else Some $"stale artifact outside target matrix: {relative}")
            |> Seq.toList
        else
            []

    let validate evidenceRoot (targets: VisualCaptureTarget list) =
        let records = targets |> List.map (validateOne evidenceRoot)
        records, staleDiagnostics evidenceRoot targets

module VisualReviewerClassifications =
    let severityText severity =
        match severity with
        | VisualReviewerPending -> "pending"
        | VisualReviewerNone -> "none"
        | VisualReviewerMinor -> "minor"
        | VisualReviewerMajor -> "major"
        | VisualReviewerBlocking -> "blocking"

    let private parseSeverity (text: string) =
        match text.Trim().ToLowerInvariant() with
        | "pending"
        | "pending review" -> Ok VisualReviewerPending
        | "none" -> Ok VisualReviewerNone
        | "minor" -> Ok VisualReviewerMinor
        | "major" -> Ok VisualReviewerMajor
        | "blocking"
        | "critical" -> Ok VisualReviewerBlocking
        | other -> Result.Error $"malformed reviewer severity: {other}"

    let private sizeText (size: VisualSize) = $"{size.Role}:{size.Width}x{size.Height}"

    let writeTemplate (targets: VisualCaptureTarget list) =
        let header =
            [ "# Visual Readiness Reviewer Classifications"
              ""
              "| targetId | pageId | themeId | size | severity | defectClass | readinessImpact | reviewer | timestamp | notes |"
              "|---|---|---|---|---|---|---|---|---|---|" ]

        let rows =
            targets
            |> List.filter _.Required
            |> List.sortBy (fun target -> target.Size.Order, target.Theme.Order, target.Page.Order, target.TargetId)
            |> List.map (fun target ->
                $"| {target.TargetId} | {target.Page.PageId} | {target.Theme.ThemeId} | {sizeText target.Size} | pending | none | pending review | pending | pending | pending review |")

        String.concat Environment.NewLine (header @ rows) + Environment.NewLine

    let private splitRow (line: string) =
        line.Trim().Trim('|').Split('|', StringSplitOptions.None)
        |> Array.map (fun cell -> cell.Trim())
        |> Array.toList

    let private isTableRow (line: string) =
        let trimmed = line.Trim()
        trimmed.StartsWith("|") && trimmed.EndsWith("|") && not (trimmed.Contains("---"))

    let parse (markdown: string) (targets: VisualCaptureTarget list) =
        let targetIds = targets |> List.filter _.Required |> List.map _.TargetId
        let targetSet = targetIds |> Set.ofList

        let rows =
            markdown.Split([| "\r\n"; "\n" |], StringSplitOptions.None)
            |> Array.toList
            |> List.filter isTableRow
            |> List.map splitRow
            |> List.filter (fun cells ->
                match cells with
                | "targetId" :: _ -> false
                | _ -> true)

        let mutable seen: Set<string> = Set.empty
        let mutable duplicateIds: string list = []
        let mutable unknownIds: string list = []
        let mutable malformedRows: string list = []
        let mutable pendingIds: string list = []
        let mutable classifications: VisualReviewerClassification list = []

        for cells in rows do
            match cells with
            | targetId :: _pageId :: _themeId :: _size :: severityText :: defectClass :: impact :: reviewer :: timestamp :: notesParts ->
                if not (targetSet.Contains targetId) then
                    unknownIds <- targetId :: unknownIds
                elif seen.Contains targetId then
                    duplicateIds <- targetId :: duplicateIds
                else
                    seen <- seen.Add targetId

                match parseSeverity severityText with
                | Result.Error _diagnostic -> malformedRows <- String.concat " | " cells :: malformedRows
                | Ok severity ->
                    let notes = String.concat " | " notesParts

                    if severity = VisualReviewerPending
                       || impact.Equals("pending review", StringComparison.OrdinalIgnoreCase)
                       || notes.Contains("pending review", StringComparison.OrdinalIgnoreCase) then
                        pendingIds <- targetId :: pendingIds

                    classifications <-
                        { TargetId = targetId
                          Severity = severity
                          DefectClass = defectClass
                          ReadinessImpact = impact
                          Reviewer = reviewer
                          ReviewedAt = timestamp
                          Notes = notes }
                        :: classifications
            | _ -> malformedRows <- String.concat " | " cells :: malformedRows

        let missingIds =
            targetIds
            |> List.filter (fun targetId -> seen.Contains targetId |> not)

        let duplicateIds = duplicateIds |> List.rev
        let unknownIds = unknownIds |> List.rev
        let malformedRows = malformedRows |> List.rev
        let pendingIds = pendingIds |> List.rev
        let classifications = classifications |> List.rev

        let diagnostics =
            [ for targetId in missingIds do
                  $"missing reviewer row: {targetId}"
              for targetId in duplicateIds do
                  $"duplicate reviewer row: {targetId}"
              for targetId in unknownIds do
                  $"unknown reviewer target: {targetId}"
              for row in malformedRows do
                  $"malformed reviewer row: {row}"
              for targetId in pendingIds do
                  $"pending reviewer row: {targetId}" ]

        { Classifications = classifications
          MissingTargetIds = missingIds
          DuplicateTargetIds = duplicateIds
          UnknownTargetIds = unknownIds
          MalformedRows = malformedRows
          PendingTargetIds = pendingIds
          Diagnostics = diagnostics }

module VisualReadiness =
    // Migrated onto the shared FS.GG.UI.Diagnostics.ReadinessStatus vocabulary (Feature 180). The
    // domain-specific PendingReview case keeps its existing literal; all shared cases route through the
    // single statusToken table. Output tokens are byte-identical to the prior per-domain mapper.
    let private toShared status =
        match status with
        | VisualReadinessAccepted -> FS.GG.UI.Diagnostics.ReadinessStatus.Accepted
        | VisualReadinessPendingReview -> FS.GG.UI.Diagnostics.ReadinessStatus.Pending
        | VisualReadinessBlocked -> FS.GG.UI.Diagnostics.ReadinessStatus.Blocked
        | VisualReadinessEnvironmentLimited -> FS.GG.UI.Diagnostics.ReadinessStatus.EnvironmentLimited
        | VisualReadinessIncomplete -> FS.GG.UI.Diagnostics.ReadinessStatus.Incomplete

    let statusText status =
        match status with
        | VisualReadinessPendingReview -> "pending-review"
        | other -> FS.GG.UI.Diagnostics.ReadinessStatus.statusToken (toShared other)

    let private countByText (textOf: 'a -> string) (values: 'a list) =
        values
        |> List.countBy textOf
        |> List.sortBy fst

    let evaluate
        (runId: string)
        (evidenceRoot: string)
        (targets: VisualCaptureTarget list)
        (captures: VisualCaptureRecord list)
        (reviewerClassifications: VisualReviewerClassification list)
        (contactSheets: VisualContactSheet list)
        (caveats: string list)
        (acceptedExceptions: string list)
        =
        let requiredTargets = targets |> List.filter _.Required
        let requiredTargetIds = requiredTargets |> List.map _.TargetId |> Set.ofList
        let acceptedExceptionIds = acceptedExceptions |> Set.ofList
        let captureByTarget = captures |> List.map (fun capture -> capture.Target.TargetId, capture) |> Map.ofList

        let missingCaptureIds =
            requiredTargets
            |> List.choose (fun target ->
                if captureByTarget.ContainsKey target.TargetId then None else Some target.TargetId)

        let captureBlocks =
            requiredTargets
            |> List.choose (fun target ->
                captureByTarget
                |> Map.tryFind target.TargetId
                |> Option.bind (fun capture ->
                    if acceptedExceptionIds.Contains target.TargetId then
                        None
                    else
                        match capture.Status with
                        | VisualCaptureMissing
                        | VisualCaptureWrongSize
                        | VisualCaptureUndecodable
                        | VisualCaptureBlocked -> Some(target.TargetId, VisualCompleteness.statusText capture.Status)
                        | _ -> None))

        let degradedIds =
            requiredTargets
            |> List.choose (fun target ->
                captureByTarget
                |> Map.tryFind target.TargetId
                |> Option.bind (fun capture ->
                    if capture.Status = VisualCaptureDegraded && not (acceptedExceptionIds.Contains target.TargetId) then
                        Some target.TargetId
                    else
                        None))

        let reviewByTarget =
            reviewerClassifications
            |> List.filter (fun review -> requiredTargetIds.Contains review.TargetId)
            |> List.groupBy _.TargetId
            |> Map.ofList

        let missingReviewIds =
            requiredTargets
            |> List.choose (fun target ->
                match reviewByTarget |> Map.tryFind target.TargetId with
                | None -> Some target.TargetId
                | Some reviews when reviews |> List.exists (fun review -> review.Severity = VisualReviewerPending) -> Some target.TargetId
                | Some _ -> None)

        let duplicateReviewIds =
            reviewByTarget
            |> Map.toList
            |> List.choose (fun (targetId, reviews) -> if reviews.Length > 1 then Some targetId else None)

        let blockingReviewIds =
            reviewerClassifications
            |> List.choose (fun review ->
                if review.Severity = VisualReviewerBlocking && requiredTargetIds.Contains review.TargetId then
                    Some review.TargetId
                else
                    None)

        let diagnostics =
            [ for targetId in missingCaptureIds do
                  $"missing capture record: {targetId}"
              for targetId, status in captureBlocks do
                  $"blocking capture status: {targetId} {status}"
              for targetId in degradedIds do
                  $"degraded capture blocks accepted readiness: {targetId}"
              for targetId in missingReviewIds do
                  $"missing or pending reviewer classification: {targetId}"
              for targetId in duplicateReviewIds do
                  $"duplicate reviewer classification: {targetId}"
              for targetId in blockingReviewIds do
                  $"blocking reviewer defect: {targetId}" ]

        let status =
            if not missingCaptureIds.IsEmpty then
                VisualReadinessIncomplete
            elif not captureBlocks.IsEmpty || not duplicateReviewIds.IsEmpty || not blockingReviewIds.IsEmpty then
                VisualReadinessBlocked
            elif not degradedIds.IsEmpty then
                VisualReadinessEnvironmentLimited
            elif not missingReviewIds.IsEmpty then
                VisualReadinessPendingReview
            elif diagnostics.IsEmpty then
                VisualReadinessAccepted
            else
                VisualReadinessBlocked

        { RunId = runId
          EvidenceRoot = evidenceRoot
          Targets = targets
          Captures = captures
          ReviewerClassifications = reviewerClassifications
          ContactSheets = contactSheets
          CaptureStatusCounts = captures |> countByText (fun capture -> VisualCompleteness.statusText capture.Status)
          ReviewerStatusCounts = reviewerClassifications |> countByText (fun review -> VisualReviewerClassifications.severityText review.Severity)
          ReadinessStatus = status
          Caveats = caveats
          Diagnostics = diagnostics }

/// Shared Markdown/JSON formatting helpers (Feature 180, US3), consumed by the readiness/evidence Markdown
/// emitters below. Reproduces the previously-duplicated per-module copies byte-for-byte: comma-SPACE
/// jsonStringArray, and the counts serializers verbatim. Internal to FS.GG.UI.Testing (absent from
/// Testing.fsi), so the public surface is unchanged. The Diagnostics.fs System.Text.Json variant is
/// behaviorally distinct (no comma-space; tokenOf-parameterized) and is intentionally left intact.
module ReadinessFormatting =
    let esc (text: string) =
        text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n")

    let q text = "\"" + esc text + "\""

    let jsonStringArray values =
        "[" + (values |> List.map q |> String.concat ", ") + "]"

    let jsonCounts values =
        values
        |> List.map (fun (name, count) -> $"    {q name}: {count}")
        |> String.concat ",\n"

    let countsText values =
        if List.isEmpty values then
            "none"
        else
            values |> List.map (fun (name, count) -> $"{name}={count}") |> String.concat ", "

module VisualReadinessMarkdown =
    open ReadinessFormatting

    let startMarker = "<!-- FS.GG VISUAL READINESS START -->"
    let endMarker = "<!-- FS.GG VISUAL READINESS END -->"

    let renderSummary (report: VisualReadinessReport) =
        let sb = StringBuilder()
        let line (text: string) = sb.AppendLine(text) |> ignore

        line "## Visual Readiness"
        line ""
        line $"- run: `{report.RunId}`"
        line $"- status: **{VisualReadiness.statusText report.ReadinessStatus}**"
        line $"- targets: `{report.Targets.Length}`"
        line $"- required targets: `{report.Targets |> List.filter _.Required |> List.length}`"
        line $"- capture status counts: `{countsText report.CaptureStatusCounts}`"
        line $"- reviewer status counts: `{countsText report.ReviewerStatusCounts}`"

        if not report.ContactSheets.IsEmpty then
            line ""
            line "### Contact Sheets"
            for sheet in report.ContactSheets do
                line $"- `{sheet.RelativePath}` ({sheet.SheetId})"

        let problemCaptures =
            report.Captures
            |> List.filter (fun capture -> capture.Status <> VisualCaptureComplete)

        if not problemCaptures.IsEmpty then
            line ""
            line "### Capture Diagnostics"
            for capture in problemCaptures do
                let reason = capture.Reason |> Option.defaultValue (VisualCompleteness.statusText capture.Status)
                line $"- `{capture.Target.TargetId}` {VisualCompleteness.statusText capture.Status}: {reason}"

        if not report.Caveats.IsEmpty then
            line ""
            line "### Caveats"
            for caveat in report.Caveats do
                line $"- {caveat}"

        if not report.Diagnostics.IsEmpty then
            line ""
            line "### Diagnostics"
            for diagnostic in report.Diagnostics do
                line $"- {diagnostic}"

        sb.ToString()

    let renderJson (report: VisualReadinessReport) =
        let targetJson =
            report.Targets
            |> List.map (fun target ->
                let size = $"{target.Size.Width}x{target.Size.Height}"
                let required = string target.Required |> fun value -> value.ToLowerInvariant()
                $"    {{ \"targetId\": {q target.TargetId}, \"pageId\": {q target.Page.PageId}, \"themeId\": {q target.Theme.ThemeId}, \"size\": {q size}, \"relativePath\": {q target.RelativePath}, \"required\": {required} }}")
            |> String.concat ",\n"

        let captureJson =
            report.Captures
            |> List.map (fun capture ->
                let observed =
                    match capture.ObservedWidth, capture.ObservedHeight with
                    | Some width, Some height -> q $"{width}x{height}"
                    | _ -> "null"

                $"    {{ \"targetId\": {q capture.Target.TargetId}, \"status\": {q (VisualCompleteness.statusText capture.Status)}, \"relativePath\": {q capture.Target.RelativePath}, \"observedSize\": {observed}, \"diagnostics\": {jsonStringArray capture.Diagnostics} }}")
            |> String.concat ",\n"

        let reviewerJson =
            report.ReviewerClassifications
            |> List.map (fun review ->
                $"    {{ \"targetId\": {q review.TargetId}, \"severity\": {q (VisualReviewerClassifications.severityText review.Severity)}, \"defectClass\": {q review.DefectClass}, \"readinessImpact\": {q review.ReadinessImpact}, \"reviewer\": {q review.Reviewer}, \"timestamp\": {q review.ReviewedAt}, \"notes\": {q review.Notes} }}")
            |> String.concat ",\n"

        let sheetJson =
            report.ContactSheets
            |> List.map (fun sheet ->
                $"    {{ \"sheetId\": {q sheet.SheetId}, \"relativePath\": {q sheet.RelativePath}, \"targetIds\": {jsonStringArray sheet.TargetIds}, \"missingTargetIds\": {jsonStringArray sheet.MissingTargetIds}, \"diagnostics\": {jsonStringArray sheet.Diagnostics} }}")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": {q report.RunId},"
              $"  \"evidenceRoot\": {q report.EvidenceRoot},"
              $"  \"targetCount\": {report.Targets.Length},"
              $"  \"requiredTargetCount\": {report.Targets |> List.filter _.Required |> List.length},"
              $"  \"readinessStatus\": {q (VisualReadiness.statusText report.ReadinessStatus)},"
              "  \"captureStatusCounts\": {"
              jsonCounts report.CaptureStatusCounts
              "  },"
              "  \"reviewerStatusCounts\": {"
              jsonCounts report.ReviewerStatusCounts
              "  },"
              "  \"targets\": ["
              targetJson
              "  ],"
              "  \"captures\": ["
              captureJson
              "  ],"
              "  \"reviewerClassifications\": ["
              reviewerJson
              "  ],"
              "  \"contactSheets\": ["
              sheetJson
              "  ],"
              $"  \"caveats\": {jsonStringArray report.Caveats},"
              $"  \"diagnostics\": {jsonStringArray report.Diagnostics}"
              "}" ]
        + "\n"

    let private countOccurrences (text: string) (pattern: string) =
        let mutable count = 0
        let mutable start = 0
        let mutable finished = false

        while not finished do
            let index = text.IndexOf(pattern, start, StringComparison.Ordinal)
            if index < 0 then
                finished <- true
            else
                count <- count + 1
                start <- index + pattern.Length

        count

    let updateManagedSection (existingText: string) (generatedMarkdown: string) : VisualSummarySectionUpdate =
        let startCount = countOccurrences existingText startMarker
        let endCount = countOccurrences existingText endMarker

        let sectionText =
            startMarker
            + Environment.NewLine
            + generatedMarkdown.TrimEnd()
            + Environment.NewLine
            + endMarker

        match startCount, endCount with
        | 0, 0 ->
            let separator =
                if String.IsNullOrEmpty existingText then
                    ""
                elif existingText.EndsWith(Environment.NewLine, StringComparison.Ordinal) then
                    Environment.NewLine
                else
                    Environment.NewLine + Environment.NewLine

            { UpdatedText = existingText + separator + sectionText + Environment.NewLine
              SafeToWrite = true
              InsertedMarkers = true
              Diagnostics = [] }
        | 1, 1 ->
            let startIndex = existingText.IndexOf(startMarker, StringComparison.Ordinal)
            let endIndex = existingText.IndexOf(endMarker, StringComparison.Ordinal)

            if startIndex > endIndex then
                { UpdatedText = existingText
                  SafeToWrite = false
                  InsertedMarkers = false
                  Diagnostics = [ "visual readiness managed markers are reversed" ] }
            else
                let prefix = existingText.Substring(0, startIndex)
                let suffix = existingText.Substring(endIndex + endMarker.Length)

                { UpdatedText = prefix + sectionText + suffix
                  SafeToWrite = true
                  InsertedMarkers = false
                  Diagnostics = [] }
        | _ ->
            { UpdatedText = existingText
              SafeToWrite = false
              InsertedMarkers = false
              Diagnostics = [ "visual readiness managed section must contain exactly one start marker and one end marker" ] }

module VisualInspectionValidation =
    let rule (ruleId: string) : VisualInspectionRule = { RuleId = ruleId; Required = true }

    let defaultRules : VisualInspectionRule list =
        [ "required-region-present"
          "required-region-painted"
          "ordinary-regions-disjoint"
          "text-contained-in-owner"
          "clip-intent-classified"
          "overlay-overlap-classified"
          "visual-order-stable"
          "unsupported-required-fact"
          "identity-stable" ]
        |> List.map rule

    let private isBlank value = String.IsNullOrWhiteSpace value

    let private isFiniteRect (rect: Rect) =
        not (Double.IsNaN rect.X)
        && not (Double.IsNaN rect.Y)
        && not (Double.IsNaN rect.Width)
        && not (Double.IsNaN rect.Height)
        && rect.Width >= 0.0
        && rect.Height >= 0.0

    let private intersects (a: Rect) (b: Rect) =
        a.X < b.X + b.Width
        && a.X + a.Width > b.X
        && a.Y < b.Y + b.Height
        && a.Y + a.Height > b.Y

    let private contains (outer: Rect) (inner: Rect) =
        inner.X >= outer.X
        && inner.Y >= outer.Y
        && inner.X + inner.Width <= outer.X + outer.Width
        && inner.Y + inner.Height <= outer.Y + outer.Height

    let private isOverlayRole role =
        match role with
        | VisualInspectionSurfaceRole.Overlay
        | VisualInspectionSurfaceRole.Popup
        | VisualInspectionSurfaceRole.Floating -> true
        | _ -> false

    let private finding ruleId severity nodeIds regionIds message expected actual =
        VisualInspection.finding ruleId severity nodeIds regionIds message expected actual

    let private affectedIds (finding: VisualInspectionFinding) =
        finding.AffectedNodeIds @ finding.AffectedRegionIds |> List.sort

    let private exceptionValid (ex: VisualInspectionException) =
        not (isBlank ex.ExceptionId)
        && not (isBlank ex.RuleId)
        && not (isBlank ex.OwnerId)
        && not ex.AffectedIds.IsEmpty
        && not (isBlank ex.Reason)

    let private exceptionMatches (finding: VisualInspectionFinding) (ex: VisualInspectionException) =
        if not (exceptionValid ex) || ex.RuleId <> finding.RuleId then
            false
        else
            Set.ofList ex.AffectedIds = Set.ofList (affectedIds finding)

    let private requiredRegionPresent (artifact: VisualInspectionArtifact) requiredRegionIds =
        let regionsById = artifact.Regions |> List.map (fun region -> region.RegionId, region) |> Map.ofList
        let requiredIds =
            (requiredRegionIds @ (artifact.Regions |> List.filter _.Required |> List.map _.RegionId))
            |> List.distinct

        [ for regionId in requiredIds do
              match regionsById |> Map.tryFind regionId with
              | None ->
                  finding
                      "required-region-present"
                      VisualInspectionSeverity.Blocking
                      []
                      [ regionId ]
                      $"required region `{regionId}` is missing"
                      "required region present with finite bounds"
                      "missing"
              | Some region ->
                  match region.Bounds with
                  | Some bounds when isFiniteRect bounds -> ()
                  | _ ->
                      finding
                          "required-region-present"
                          VisualInspectionSeverity.Blocking
                          region.OwnerNodeIds
                          [ region.RegionId ]
                          $"required region `{region.RegionId}` has missing or invalid bounds"
                          "finite non-negative bounds"
                          "missing or invalid bounds" ]

    let private requiredRegionPainted (artifact: VisualInspectionArtifact) =
        let coverageByTarget =
            artifact.PaintCoverage
            |> List.groupBy _.TargetId
            |> Map.ofList

        [ for region in artifact.Regions do
              if region.Required then
                  let coverage = coverageByTarget |> Map.tryFind region.RegionId |> Option.defaultValue []
                  if coverage.IsEmpty then
                      finding
                          "required-region-painted"
                          VisualInspectionSeverity.Blocking
                          region.OwnerNodeIds
                          [ region.RegionId ]
                          $"required region `{region.RegionId}` has no paint coverage fact"
                          "complete intentional paint coverage"
                          "missing coverage fact"
                  else
                      for fact in coverage do
                          match fact.CoverageStatus with
                          | VisualInspectionCoverageStatus.Complete -> ()
                          | VisualInspectionCoverageStatus.Unsupported
                          | VisualInspectionCoverageStatus.Unavailable ->
                              finding
                                  "required-region-painted"
                                  VisualInspectionSeverity.Unsupported
                                  region.OwnerNodeIds
                                  [ region.RegionId ]
                                  $"required region `{region.RegionId}` paint coverage is unsupported"
                                  "complete intentional paint coverage"
                                  (VisualInspection.coverageStatusText fact.CoverageStatus)
                          | VisualInspectionCoverageStatus.Partial
                          | VisualInspectionCoverageStatus.Missing ->
                              finding
                                  "required-region-painted"
                                  VisualInspectionSeverity.Blocking
                                  region.OwnerNodeIds
                                  [ region.RegionId ]
                                  $"required region `{region.RegionId}` is not fully painted"
                                  "complete intentional paint coverage"
                                  (VisualInspection.coverageStatusText fact.CoverageStatus) ]

    let private ordinaryRegionsDisjoint (artifact: VisualInspectionArtifact) =
        [ for firstIndex, first in artifact.Regions |> List.indexed do
              for second in artifact.Regions |> List.skip (firstIndex + 1) do
                  match first.Bounds, second.Bounds with
                  | Some a, Some b when not (isOverlayRole first.Role) && not (isOverlayRole second.Role) && intersects a b && not (contains a b) && not (contains b a) ->
                      finding
                          "ordinary-regions-disjoint"
                          VisualInspectionSeverity.Blocking
                          (first.OwnerNodeIds @ second.OwnerNodeIds)
                          [ first.RegionId; second.RegionId ]
                          $"ordinary regions `{first.RegionId}` and `{second.RegionId}` overlap"
                          "ordinary regions are disjoint unless explicitly classified"
                          "overlap"
                  | _ -> () ]

    let private textContainedInOwner (artifact: VisualInspectionArtifact) =
        [ for textRun in artifact.TextRuns do
              if textRun.Required then
                  match textRun.FitStatus with
                  | VisualInspectionFitStatus.Inside ->
                      match textRun.OwnerBounds, textRun.TextBounds with
                      | Some owner, Some textBounds when not (contains owner textBounds) ->
                          finding
                              "text-contained-in-owner"
                              VisualInspectionSeverity.Blocking
                              [ textRun.OwnerNodeId ]
                              []
                              $"text `{textRun.TextId}` is classified inside but exceeds its owner bounds"
                              "text bounds inside owner bounds"
                              "outside owner bounds"
                      | _ -> ()
                  | VisualInspectionFitStatus.Wrapped
                  | VisualInspectionFitStatus.Truncated -> ()
                  | VisualInspectionFitStatus.Overflow
                  | VisualInspectionFitStatus.Clipped ->
                      finding
                          "text-contained-in-owner"
                          VisualInspectionSeverity.Blocking
                          [ textRun.OwnerNodeId ]
                          []
                          $"text `{textRun.TextId}` does not fit inside owner `{textRun.OwnerNodeId}`"
                          "inside, wrapped, or intentionally truncated text"
                          (VisualInspection.fitStatusText textRun.FitStatus)
                  | VisualInspectionFitStatus.Unsupported
                  | VisualInspectionFitStatus.Unavailable ->
                      finding
                          "text-contained-in-owner"
                          VisualInspectionSeverity.Unsupported
                          [ textRun.OwnerNodeId ]
                          []
                          $"text `{textRun.TextId}` fit facts are unavailable"
                          "inspectable text fit facts"
                          (VisualInspection.fitStatusText textRun.FitStatus) ]

    let private clipIntentClassified (artifact: VisualInspectionArtifact) =
        [ for clip in artifact.ClipFacts do
              match clip.ClipStatus with
              | VisualInspectionClipStatus.None
              | VisualInspectionClipStatus.Intentional -> ()
              | VisualInspectionClipStatus.Accidental ->
                  finding
                      "clip-intent-classified"
                      VisualInspectionSeverity.Blocking
                      [ clip.NodeId ]
                      []
                      $"node `{clip.NodeId}` has accidental clipping"
                      "no clipping or intentional owned clipping"
                      "accidental clipping"
              | VisualInspectionClipStatus.Unsupported
              | VisualInspectionClipStatus.Unavailable ->
                  finding
                      "clip-intent-classified"
                      VisualInspectionSeverity.Unsupported
                      [ clip.NodeId ]
                      []
                      $"node `{clip.NodeId}` clipping facts are unavailable"
                      "inspectable clipping facts"
                      (VisualInspection.clipStatusText clip.ClipStatus) ]

    let private overlayOverlapClassified (artifact: VisualInspectionArtifact) =
        [ for firstIndex, first in artifact.Regions |> List.indexed do
              for second in artifact.Regions |> List.skip (firstIndex + 1) do
                  if isOverlayRole first.Role || isOverlayRole second.Role then
                      match first.Bounds, second.Bounds with
                      | Some a, Some b when intersects a b ->
                          finding
                              "overlay-overlap-classified"
                              VisualInspectionSeverity.Blocking
                              (first.OwnerNodeIds @ second.OwnerNodeIds)
                              [ first.RegionId; second.RegionId ]
                              $"overlay overlap between `{first.RegionId}` and `{second.RegionId}` needs classification"
                              "explicit owner and reason for overlay overlap"
                              "unclassified overlay overlap"
                      | _ -> () ]

    let private unsupportedRequiredFacts (artifact: VisualInspectionArtifact) (environmentLimitations: string list) =
        [ for fact in artifact.UnsupportedFacts do
              if fact.Required then
                  let severity =
                      if fact.EnvironmentLimited || not environmentLimitations.IsEmpty then
                          VisualInspectionSeverity.EnvironmentLimited
                      else
                          VisualInspectionSeverity.Unsupported

                  finding
                      "unsupported-required-fact"
                      severity
                      (fact.OwnerId |> Option.map List.singleton |> Option.defaultValue [])
                      []
                      $"required inspection fact `{fact.Fact}` is unsupported"
                      "required fact inspectable or explicitly environment-limited"
                      fact.Reason ]

    let private identityStable (artifact: VisualInspectionArtifact) (previous: VisualInspectionArtifact option) =
        match previous with
        | None -> []
        | Some previousArtifact ->
            let previousIds = previousArtifact.Nodes |> List.filter (fun n -> not n.Dynamic) |> List.map _.NodeId |> Set.ofList
            let currentIds = artifact.Nodes |> List.filter (fun n -> not n.Dynamic) |> List.map _.NodeId |> Set.ofList

            if previousIds = currentIds then
                []
            else
                [ finding
                      "identity-stable"
                      VisualInspectionSeverity.Blocking
                      (Set.union previousIds currentIds |> Set.toList)
                      []
                      "static node identities changed between inspection runs"
                      "same static node id set"
                      "identity set changed" ]

    let private visualOrderStable (artifact: VisualInspectionArtifact) (previous: VisualInspectionArtifact option) =
        match previous with
        | None -> []
        | Some previousArtifact ->
            let orderOf (a: VisualInspectionArtifact) =
                a.Nodes |> List.filter (fun n -> not n.Dynamic) |> List.sortBy (fun n -> n.ZOrder, n.NodeId) |> List.map _.NodeId

            let previousOrder = orderOf previousArtifact
            let currentOrder = orderOf artifact

            if previousOrder = currentOrder then
                []
            else
                [ finding
                      "visual-order-stable"
                      VisualInspectionSeverity.Blocking
                      (previousOrder @ currentOrder |> List.distinct)
                      []
                      "static node visual order changed between inspection runs"
                      "same static visual order"
                      "visual order changed" ]

    let private findingsForRule (check: VisualInspectionValidationCheck) (rule: VisualInspectionRule) =
        match rule.RuleId with
        | "required-region-present" -> requiredRegionPresent check.Artifact check.RequiredRegionIds
        | "required-region-painted" -> requiredRegionPainted check.Artifact
        | "ordinary-regions-disjoint" -> ordinaryRegionsDisjoint check.Artifact
        | "text-contained-in-owner" -> textContainedInOwner check.Artifact
        | "clip-intent-classified" -> clipIntentClassified check.Artifact
        | "overlay-overlap-classified" -> overlayOverlapClassified check.Artifact
        | "unsupported-required-fact" -> unsupportedRequiredFacts check.Artifact check.EnvironmentLimitations
        | "identity-stable" -> identityStable check.Artifact check.PreviousArtifact
        | "visual-order-stable" -> visualOrderStable check.Artifact check.PreviousArtifact
        | unknown when rule.Required ->
            [ finding unknown VisualInspectionSeverity.Unsupported [] [] $"rule `{unknown}` is not implemented" "implemented validation rule" "unknown rule" ]
        | _ -> []

    let validateCheck (check: VisualInspectionValidationCheck) : VisualInspectionValidationResult =
        let invalidExceptions =
            check.Exceptions
            |> List.filter (exceptionValid >> not)
            |> List.map _.ExceptionId

        let initialFindings =
            check.Rules
            |> List.collect (findingsForRule check)
            |> List.append check.Artifact.Findings
            |> List.sortBy _.FindingId

        let validExceptions = check.Exceptions |> List.filter exceptionValid
        let applied = ResizeArray<string>()

        let findings =
            initialFindings
            |> List.map (fun f ->
                match validExceptions |> List.tryFind (exceptionMatches f) with
                | Some ex when f.Severity = VisualInspectionSeverity.Blocking ->
                    applied.Add ex.ExceptionId
                    { f with
                        Severity = VisualInspectionSeverity.Pass
                        ExceptionId = Some ex.ExceptionId
                        Diagnostics = f.Diagnostics @ [ $"accepted by visual inspection exception `{ex.ExceptionId}`: {ex.Reason}" ] }
                | _ -> f)
            |> List.distinctBy _.FindingId
            |> List.sortBy _.FindingId

        let appliedIds = applied |> Seq.distinct |> Seq.toList
        let unused =
            validExceptions
            |> List.map _.ExceptionId
            |> List.filter (fun id -> not (List.contains id appliedIds))

        let diagnostics =
            (VisualInspection.artifactDiagnostics check.Artifact)
            @ (invalidExceptions |> List.map (fun id -> $"invalid visual inspection exception: {id}"))
            @ (unused |> List.map (fun id -> $"unused visual inspection exception: {id}"))
            |> List.distinct

        let has severity =
            findings |> List.exists (fun f -> f.Severity = severity)

        let status =
            if not invalidExceptions.IsEmpty || has VisualInspectionSeverity.Blocking then
                VisualInspectionStatus.Blocked
            elif has VisualInspectionSeverity.EnvironmentLimited then
                VisualInspectionStatus.EnvironmentLimited
            elif has VisualInspectionSeverity.Unsupported then
                if check.EnvironmentLimitations.IsEmpty then
                    VisualInspectionStatus.Unsupported
                else
                    VisualInspectionStatus.EnvironmentLimited
            else
                match check.Artifact.ReadinessStatus with
                | VisualInspectionStatus.NotRun
                | VisualInspectionStatus.NotInspected
                | VisualInspectionStatus.Incomplete -> VisualInspectionStatus.Incomplete
                | VisualInspectionStatus.Blocked -> VisualInspectionStatus.Blocked
                | VisualInspectionStatus.Unsupported -> VisualInspectionStatus.Unsupported
                | VisualInspectionStatus.EnvironmentLimited -> VisualInspectionStatus.EnvironmentLimited
                | VisualInspectionStatus.Accepted -> VisualInspectionStatus.Accepted

        { ArtifactId = check.Artifact.ArtifactId
          ReadinessStatus = status
          Findings = findings
          AppliedExceptions = appliedIds
          InvalidExceptions = invalidExceptions
          UnusedExceptions = unused
          Diagnostics = diagnostics }

    let validate (artifact: VisualInspectionArtifact) (rules: VisualInspectionRule list) (exceptions: VisualInspectionException list) : VisualInspectionValidationResult =
        validateCheck
            { Artifact = artifact
              Rules = rules
              Exceptions = exceptions
              RequiredRegionIds = []
              PreviousArtifact = None
              EnvironmentLimitations = [] }

module VisualInspectionReadiness =
    let private statusRank status =
        match status with
        | VisualInspectionStatus.Blocked -> 0
        | VisualInspectionStatus.Unsupported -> 1
        | VisualInspectionStatus.EnvironmentLimited -> 2
        | VisualInspectionStatus.Incomplete -> 3
        | VisualInspectionStatus.NotRun -> 4
        | VisualInspectionStatus.NotInspected -> 5
        | VisualInspectionStatus.Accepted -> 6

    let private worstStatus statuses =
        statuses
        |> List.sortBy statusRank
        |> List.tryHead
        |> Option.defaultValue VisualInspectionStatus.Accepted

    let private countBy values =
        values |> List.countBy id |> List.sortBy fst

    let aggregate
        (runId: string)
        (artifacts: VisualInspectionArtifact list)
        (results: VisualInspectionValidationResult list)
        (relatedVisualEvidence: string list)
        (caveats: string list)
        =
        let resultByArtifact = results |> List.map (fun result -> result.ArtifactId, result) |> Map.ofList
        let statuses =
            artifacts
            |> List.map (fun artifact ->
                resultByArtifact
                |> Map.tryFind artifact.ArtifactId
                |> Option.map _.ReadinessStatus
                |> Option.defaultValue artifact.ReadinessStatus)

        let findings =
            results |> List.collect _.Findings

        { RunId = runId
          OverallStatus = worstStatus statuses
          ArtifactCount = artifacts.Length
          InspectedScopes =
            artifacts
            |> List.filter (fun a -> a.ReadinessStatus <> VisualInspectionStatus.NotInspected && a.ReadinessStatus <> VisualInspectionStatus.NotRun)
            |> List.map _.Scope.ScopeId
            |> List.sort
          NotInspectedScopes =
            artifacts
            |> List.filter (fun a -> a.ReadinessStatus = VisualInspectionStatus.NotInspected)
            |> List.map _.Scope.ScopeId
            |> List.sort
          NotRunScopes =
            artifacts
            |> List.filter (fun a -> a.ReadinessStatus = VisualInspectionStatus.NotRun)
            |> List.map _.Scope.ScopeId
            |> List.sort
          StatusCounts = statuses |> List.map VisualInspection.statusText |> countBy
          FindingCounts = findings |> List.map (fun finding -> VisualInspection.severityText finding.Severity) |> countBy
          BlockingFindings = findings |> List.filter (fun finding -> finding.Severity = VisualInspectionSeverity.Blocking)
          UnsupportedFacts = artifacts |> List.collect _.UnsupportedFacts
          AcceptedExceptions = results |> List.collect _.AppliedExceptions |> List.distinct |> List.sort
          InvalidExceptions = results |> List.collect _.InvalidExceptions |> List.distinct |> List.sort
          RelatedVisualEvidence = relatedVisualEvidence |> List.distinct |> List.sort
          Caveats = caveats
          Diagnostics = results |> List.collect _.Diagnostics |> List.distinct }

module VisualInspectionMarkdown =
    open ReadinessFormatting

    let startMarker = "<!-- FS.GG VISUAL INSPECTION START -->"
    let endMarker = "<!-- FS.GG VISUAL INSPECTION END -->"

    let renderSummary (summary: VisualInspectionSummary) =
        let sb = StringBuilder()
        let line (text: string) = sb.AppendLine(text) |> ignore

        let inspectedScopesText = String.concat ", " summary.InspectedScopes

        line "## Visual Inspection"
        line ""
        line $"- run: `{summary.RunId}`"
        line $"- status: **{VisualInspection.statusText summary.OverallStatus}**"
        line $"- artifacts: `{summary.ArtifactCount}`"
        line $"- inspected scopes: `{inspectedScopesText}`"
        line $"- status counts: `{countsText summary.StatusCounts}`"
        line $"- finding counts: `{countsText summary.FindingCounts}`"

        if not summary.BlockingFindings.IsEmpty then
            line ""
            line "### Blocking Findings"
            line "| finding | rule | affected | message |"
            line "|---|---|---|---|"
            for finding in summary.BlockingFindings do
                let affected = String.concat ", " (finding.AffectedRegionIds @ finding.AffectedNodeIds)
                line $"| `{finding.FindingId}` | `{finding.RuleId}` | `{affected}` | {finding.Message} |"

        if not summary.UnsupportedFacts.IsEmpty then
            line ""
            line "### Unsupported Facts"
            for fact in summary.UnsupportedFacts do
                let owner = fact.OwnerId |> Option.defaultValue "scope"
                line $"- `{fact.Fact}` on `{owner}`: {fact.Reason}"

        if not summary.AcceptedExceptions.IsEmpty then
            line ""
            line "### Accepted Exceptions"
            for exceptionId in summary.AcceptedExceptions do
                line $"- `{exceptionId}`"

        if not summary.InvalidExceptions.IsEmpty then
            line ""
            line "### Invalid Exceptions"
            for exceptionId in summary.InvalidExceptions do
                line $"- `{exceptionId}`"

        if not summary.RelatedVisualEvidence.IsEmpty then
            line ""
            line "### Related Visual Evidence"
            for path in summary.RelatedVisualEvidence do
                line $"- `{path}`"

        if not summary.Caveats.IsEmpty then
            line ""
            line "### Caveats"
            for caveat in summary.Caveats do
                line $"- {caveat}"

        if not summary.Diagnostics.IsEmpty then
            line ""
            line "### Diagnostics"
            for diagnostic in summary.Diagnostics do
                line $"- {diagnostic}"

        sb.ToString()

    let renderJson (summary: VisualInspectionSummary) =
        let findingJson =
            summary.BlockingFindings
            |> List.map (fun finding ->
                let affected = finding.AffectedRegionIds @ finding.AffectedNodeIds
                $"    {{ \"findingId\": {q finding.FindingId}, \"ruleId\": {q finding.RuleId}, \"severity\": {q (VisualInspection.severityText finding.Severity)}, \"affectedIds\": {jsonStringArray affected}, \"message\": {q finding.Message} }}")
            |> String.concat ",\n"

        let unsupportedJson =
            summary.UnsupportedFacts
            |> List.map (fun fact ->
                let ownerJson = fact.OwnerId |> Option.map q |> Option.defaultValue "null"
                $"    {{ \"fact\": {q fact.Fact}, \"ownerId\": {ownerJson}, \"required\": {fact.Required.ToString().ToLowerInvariant()}, \"reason\": {q fact.Reason}, \"environmentLimited\": {fact.EnvironmentLimited.ToString().ToLowerInvariant()} }}")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": {q summary.RunId},"
              $"  \"overallStatus\": {q (VisualInspection.statusText summary.OverallStatus)},"
              $"  \"artifactCount\": {summary.ArtifactCount},"
              $"  \"inspectedScopes\": {jsonStringArray summary.InspectedScopes},"
              $"  \"notInspectedScopes\": {jsonStringArray summary.NotInspectedScopes},"
              $"  \"notRunScopes\": {jsonStringArray summary.NotRunScopes},"
              "  \"statusCounts\": {"
              jsonCounts summary.StatusCounts
              "  },"
              "  \"findingCounts\": {"
              jsonCounts summary.FindingCounts
              "  },"
              "  \"blockingFindings\": ["
              findingJson
              "  ],"
              "  \"unsupportedFacts\": ["
              unsupportedJson
              "  ],"
              $"  \"acceptedExceptions\": {jsonStringArray summary.AcceptedExceptions},"
              $"  \"invalidExceptions\": {jsonStringArray summary.InvalidExceptions},"
              $"  \"relatedVisualEvidence\": {jsonStringArray summary.RelatedVisualEvidence},"
              $"  \"caveats\": {jsonStringArray summary.Caveats},"
              $"  \"diagnostics\": {jsonStringArray summary.Diagnostics}"
              "}" ]
        + "\n"

    let private countOccurrences (text: string) (pattern: string) =
        let mutable count = 0
        let mutable start = 0
        let mutable finished = false

        while not finished do
            let index = text.IndexOf(pattern, start, StringComparison.Ordinal)
            if index < 0 then
                finished <- true
            else
                count <- count + 1
                start <- index + pattern.Length

        count

    let updateManagedSection (existingText: string) (generatedMarkdown: string) : VisualInspectionSummarySectionUpdate =
        let startCount = countOccurrences existingText startMarker
        let endCount = countOccurrences existingText endMarker
        let sectionText = startMarker + Environment.NewLine + generatedMarkdown.TrimEnd() + Environment.NewLine + endMarker

        match startCount, endCount with
        | 0, 0 ->
            let separator =
                if String.IsNullOrEmpty existingText then
                    ""
                elif existingText.EndsWith(Environment.NewLine, StringComparison.Ordinal) then
                    Environment.NewLine
                else
                    Environment.NewLine + Environment.NewLine

            { UpdatedText = existingText + separator + sectionText + Environment.NewLine
              SafeToWrite = true
              InsertedMarkers = true
              Diagnostics = [] }
        | 1, 1 ->
            let startIndex = existingText.IndexOf(startMarker, StringComparison.Ordinal)
            let endIndex = existingText.IndexOf(endMarker, StringComparison.Ordinal)

            if startIndex > endIndex then
                { UpdatedText = existingText
                  SafeToWrite = false
                  InsertedMarkers = false
                  Diagnostics = [ "visual inspection managed markers are reversed" ] }
            else
                let prefix = existingText.Substring(0, startIndex)
                let suffix = existingText.Substring(endIndex + endMarker.Length)

                { UpdatedText = prefix + sectionText + suffix
                  SafeToWrite = true
                  InsertedMarkers = false
                  Diagnostics = [] }
        | _ ->
            { UpdatedText = existingText
              SafeToWrite = false
              InsertedMarkers = false
              Diagnostics = [ "visual inspection managed section must contain exactly one start marker and one end marker" ] }

