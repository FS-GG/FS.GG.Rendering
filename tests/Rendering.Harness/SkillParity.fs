namespace Rendering.Harness

open System
open System.Globalization
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.RegularExpressions

module SkillParity =

    type SurfaceKind =
        | Canonical
        | Wrapper
        | Mixed
        | Command

    type AgentSurface =
        | Codex
        | Claude
        | GeneratedProduct
        | Package
        | SpecKit
        | Repository

    type EntryKind =
        | CanonicalEntry
        | WrapperEntry
        | CommandEntry
        | WrapperOnlyEntry

    type CoverageStatus =
        | Covered
        | Partial
        | Missing
        | NotApplicable
        | Excepted

    type FindingSeverity =
        | Info
        | Warning
        | High
        | Critical

    type FindingCategory =
        | MissingWrapper
        | WrapperOnly
        | StaleDescription
        | BrokenTarget
        | CanonicalDrift
        | GuidanceRuleGap
        | MetadataDrift
        | IntentionalExceptionFinding
        | UnreadableSurface

    type OverallStatus =
        | Passed
        | WarningStatus
        | Failed

    type SkillSurface =
        { SurfaceId: string
          DisplayName: string
          RootPath: string
          Kind: SurfaceKind
          Agent: AgentSurface
          IsRequired: bool
          Notes: string list }

    type WrapperTarget =
        { RawTarget: string
          ResolvedPath: string
          Exists: bool
          CanonicalSkillName: string option
          CanonicalDescription: string option
          TargetHash: string option }

    type SkillEntry =
        { SkillName: string
          Description: string
          Path: string
          AbsolutePath: string
          SurfaceId: string
          EntryKind: EntryKind
          Metadata: Map<string, string>
          BodyHash: string
          Content: string
          WrapperTarget: WrapperTarget option }

    type GuidanceRule =
        { RuleId: string
          Theme: string
          Description: string
          RequiredReferences: string list list
          ApplicablePatterns: string list
          MinimumCoverage: string }

    type GuidanceCoverage =
        { RuleId: string
          SkillName: string
          SurfaceId: string
          Path: string
          Status: CoverageStatus
          Evidence: string list
          MissingReferences: string list
          ExceptionId: string option }

    type IntentionalException =
        { ExceptionId: string
          SkillName: string
          SurfaceId: string
          Category: string
          Reason: string
          Owner: string
          ReviewDate: string
          Scope: string }

    type ParityFinding =
        { FindingId: string
          SkillName: string
          SurfaceId: string
          Category: FindingCategory
          Severity: FindingSeverity
          CanonicalPath: string option
          WrapperPath: string option
          RuleId: string option
          Message: string
          Remediation: string
          ExceptionId: string option }

    type SeverityCounts =
        { Critical: int
          High: int
          Warning: int
          Info: int }

    type RuleCoverageSummary =
        { RuleId: string
          Covered: int
          Partial: int
          Missing: int
          Excepted: int
          NotApplicable: int }

    type ParityReport =
        { CheckedAtUtc: DateTime
          RepositoryRoot: string
          OverallStatus: OverallStatus
          SupportedSurfaces: SkillSurface list
          CanonicalSourceCount: int
          WrapperCount: int
          FindingCountsBySeverity: SeverityCounts
          GuidanceRuleCoverage: RuleCoverageSummary list
          Findings: ParityFinding list
          IntentionalExceptions: IntentionalException list
          GeneratedReportPath: string
          StructuredSummaryPath: string
          Caveats: string list
          Command: string }

    type ParityCheckRequest =
        { RepositoryRoot: string
          OutDir: string
          ReportPath: string
          SummaryJsonPath: string
          FixtureMode: string option
          SurfaceOverrides: (string * string) list
          AllowedExceptionIds: Set<string>
          FailOnSeverity: FindingSeverity
          ListRulesOnly: bool
          JsonOutput: bool }

    type Model =
        { Request: ParityCheckRequest
          Surfaces: SkillSurface list
          Entries: SkillEntry list
          Findings: ParityFinding list
          Coverage: GuidanceCoverage list
          Report: ParityReport option
          Diagnostics: string list }

    type Msg =
        | InventoryRequested
        | InventoryLoaded of SkillSurface list * SkillEntry list
        | CoverageEvaluated of GuidanceCoverage list
        | FindingsClassified of ParityFinding list
        | ReportGenerated of ParityReport
        | WorkflowFailed of string

    type Effect =
        | ReadSkillSurfaces
        | EvaluateGuidanceRules
        | ClassifyFindings
        | WriteMarkdownReport
        | WriteSummaryJson

    let surfaceKindToken kind =
        match kind with
        | Canonical -> "canonical"
        | Wrapper -> "wrapper"
        | Mixed -> "mixed"
        | Command -> "command"

    let agentToken agent =
        match agent with
        | Codex -> "codex"
        | Claude -> "claude"
        | GeneratedProduct -> "generated-product"
        | Package -> "package"
        | SpecKit -> "spec-kit"
        | Repository -> "repository"

    let entryKindToken kind =
        match kind with
        | CanonicalEntry -> "canonical"
        | WrapperEntry -> "wrapper"
        | CommandEntry -> "command"
        | WrapperOnlyEntry -> "wrapper-only"

    let coverageToken status =
        match status with
        | Covered -> "covered"
        | Partial -> "partial"
        | Missing -> "missing"
        | NotApplicable -> "not-applicable"
        | Excepted -> "excepted"

    let severityToken severity =
        match severity with
        | Info -> "info"
        | Warning -> "warning"
        | High -> "high"
        | Critical -> "critical"

    let categoryToken category =
        match category with
        | MissingWrapper -> "missing-wrapper"
        | WrapperOnly -> "wrapper-only"
        | StaleDescription -> "stale-description"
        | BrokenTarget -> "broken-target"
        | CanonicalDrift -> "canonical-drift"
        | GuidanceRuleGap -> "guidance-rule-gap"
        | MetadataDrift -> "metadata-drift"
        | IntentionalExceptionFinding -> "intentional-exception"
        | UnreadableSurface -> "unreadable-surface"

    let overallStatusToken status =
        match status with
        | Passed -> "passed"
        | WarningStatus -> "warning"
        | Failed -> "failed"

    let private severityRank severity =
        match severity with
        | Info -> 0
        | Warning -> 1
        | High -> 2
        | Critical -> 3

    let private normalizeSeparators (path: string) =
        path.Replace('\\', '/')

    let private absolutePath (root: string) (path: string) =
        if String.IsNullOrWhiteSpace path then
            root
        elif Path.IsPathRooted path then
            Path.GetFullPath path
        else
            Path.GetFullPath(Path.Combine(root, path))

    let private relativePath (root: string) (path: string) =
        try
            Path.GetRelativePath(root, path) |> normalizeSeparators
        with _ ->
            normalizeSeparators path

    let private ensureParent (path: string) =
        match Path.GetDirectoryName path with
        | null
        | "" -> ()
        | directory -> Directory.CreateDirectory directory |> ignore

    let private containsIgnoreCase (needle: string) (haystack: string) =
        haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0

    let private normalizeText (value: string) =
        value.Trim().Trim('"').Trim('\'').Trim().TrimEnd('.').ToLowerInvariant()

    let private sha256 (text: string) =
        use sha = SHA256.Create()
        let bytes = Encoding.UTF8.GetBytes text
        sha.ComputeHash bytes
        |> Array.map (fun b -> b.ToString("x2", CultureInfo.InvariantCulture))
        |> String.concat ""

    let parseFrontMatter (content: string) =
        let normalized = content.Replace("\r\n", "\n")
        let lines = normalized.Split('\n')

        if lines.Length > 0 && lines[0].Trim() = "---" then
            let closing =
                lines
                |> Array.mapi (fun index line -> index, line)
                |> Array.tryFind (fun (index, line) -> index > 0 && line.Trim() = "---")

            match closing with
            | Some (closingIndex, _) ->
                let metadata =
                    lines[1 .. closingIndex - 1]
                    |> Array.choose (fun line ->
                        let trimmed = line.Trim()

                        if trimmed = "" || trimmed.StartsWith("#", StringComparison.Ordinal) then
                            None
                        else
                            let colon = trimmed.IndexOf(':')

                            if colon <= 0 then
                                None
                            else
                                let key = trimmed.Substring(0, colon).Trim()
                                let value = trimmed.Substring(colon + 1).Trim().Trim('"').Trim('\'')
                                Some(key, value))
                    |> Map.ofArray

                let body =
                    if closingIndex + 1 < lines.Length then
                        String.Join("\n", lines[closingIndex + 1 ..])
                    else
                        ""

                metadata, body
            | None -> Map.empty, content
        else
            Map.empty, content

    let private metadataValue key (metadata: Map<string, string>) =
        metadata |> Map.tryFind key |> Option.defaultValue ""

    let defaultGuidanceRules () =
        [ { RuleId = "package-pin-drift"
            Theme = "Package-consuming samples check current FS.GG.UI.* package pins and use local-feed proof."
            Description = "Package-consuming samples compare current package versions and use the package-feed proof workflow."
            RequiredReferences =
                [ [ "FS.GG.UI." ]
                  [ "scripts/refresh-local-feed-and-samples.fsx"; "package-feed" ]
                  [ "stale package pins"; "package pins" ]
                  [ "local feed" ] ]
            ApplicablePatterns =
                [ "speckit-implement"
                  "speckit-merge"
                  "fs-gg-testing"
                  "src/testing"
                  "template/product-skills/fs-gg-testing"
                  "template/fragments/samples"
                  "src/controls"
                  "src/skiaviewer"
                  "fs-gg-project" ]
            MinimumCoverage = "required" }
          { RuleId = "readiness-allowlisting"
            Theme = "Committed readiness evidence is ignored by default until allowlisted."
            Description = "Readiness evidence uses specs/*/readiness/ allowlists and git check-ignore proof."
            RequiredReferences =
                [ [ "specs/*/readiness/" ]
                  [ ".gitignore" ]
                  [ "git check-ignore" ] ]
            ApplicablePatterns =
                [ "speckit-implement"
                  "fs-gg-testing"
                  "src/testing"
                  "template/product-skills/fs-gg-testing"
                  "fs-gg-project"
                  "speckit-merge" ]
            MinimumCoverage = "required" }
          { RuleId = "validation-output-isolation"
            Theme = "Same project/configuration validation is not parallelized unless output paths are isolated."
            Description = "Same project/configuration test runs require isolated output paths."
            RequiredReferences =
                [ [ "dotnet test" ]
                  [ "same project/configuration"; "same project and configuration" ]
                  [ "isolated output"; "BaseOutputPath" ] ]
            ApplicablePatterns =
                [ "speckit-implement"
                  "fs-gg-testing"
                  "src/testing"
                  "template/product-skills/fs-gg-testing"
                  "fs-gg-project" ]
            MinimumCoverage = "required" }
          { RuleId = "visual-readiness"
            Theme = "Real screenshots, degraded capture disclosure, reviewer classification, and summary caveat preservation are required."
            Description = "Visual readiness prefers real screenshots and keeps degraded or pending review caveats visible."
            RequiredReferences =
                [ [ "screenshot" ]
                  [ "degraded" ]
                  [ "reviewer" ]
                  [ "accepted readiness" ]
                  [ "generated summary"; "managed section" ] ]
            ApplicablePatterns =
                [ "speckit-implement"
                  "fs-gg-testing"
                  "src/testing"
                  "template/product-skills/fs-gg-testing"
                  "src/controls"
                  "template/fragments/controls"
                  "fs-gg-ui-widgets"
                  "src/skiaviewer"
                  "fs-gg-skiaviewer"
                  "speckit-merge" ]
            MinimumCoverage = "required" }
          { RuleId = "responsiveness-diagnostics"
            Theme = "Interactive readiness validates pointer and keyboard activation separately and separates routing from update/render/present latency."
            Description = "Responsiveness evidence separates activation, routing, update, render, and present latency."
            RequiredReferences =
                [ [ "pointer" ]
                  [ "keyboard" ]
                  [ "responsiveness" ]
                  [ "routing" ]
                  [ "render"; "present" ] ]
            ApplicablePatterns =
                [ "src/skiaviewer"
                  "fs-gg-skiaviewer"
                  "src/controls"
                  "template/fragments/controls"
                  "fs-gg-ui-widgets"
                  "fs-gg-testing"
                  "src/testing"
                  "template/product-skills/fs-gg-testing" ]
            MinimumCoverage = "required" }
          { RuleId = "post-merge-package-bump"
            Theme = "Merge/post-merge work records package bump, local-feed pack, sample pin alignment, restore/validation, and readiness ledger updates."
            Description = "Merge work records package bump evidence and local feed/sample validation."
            RequiredReferences =
                [ [ "package bump" ]
                  [ "local feed" ]
                  [ "sample package pins" ]
                  [ "restore"; "validation" ]
                  [ "readiness ledger" ] ]
            ApplicablePatterns = [ "speckit-merge" ]
            MinimumCoverage = "required" }
          { RuleId = "evidence-honesty"
            Theme = "Canceled, timed-out, skipped, synthetic, substitute, degraded, pending-review, and environment-limited checks are visibly caveated."
            Description = "Evidence caveats remain visible and are not reported as fully green."
            RequiredReferences =
                [ [ "canceled"; "cancelled"; "timed out"; "timed-out" ]
                  [ "synthetic"; "substitute" ]
                  [ "environment-limited"; "pending-review"; "pending review" ]
                  [ "caveat" ] ]
            ApplicablePatterns =
                [ "speckit-implement"
                  "speckit-merge"
                  "fs-gg-testing"
                  "src/testing"
                  "template/product-skills/fs-gg-testing"
                  "src/controls"
                  "template/fragments/controls"
                  "fs-gg-ui-widgets"
                  "src/skiaviewer"
                  "fs-gg-skiaviewer"
                  "template/fragments/samples"
                  "fs-gg-project" ]
            MinimumCoverage = "required" } ]

    let defaultRequest repositoryRoot =
        let root = Path.GetFullPath repositoryRoot
        let outDir = Path.Combine(root, "artifacts", "skill-parity")

        { RepositoryRoot = root
          OutDir = outDir
          ReportPath = Path.Combine(root, "docs", "reports", "skills-parity.md")
          SummaryJsonPath = Path.Combine(outDir, "skill-parity-summary.json")
          FixtureMode = None
          SurfaceOverrides = []
          AllowedExceptionIds = Set.empty
          FailOnSeverity = High
          ListRulesOnly = false
          JsonOutput = false }

    let discoverDefaultSurfaces repositoryRoot =
        let root = Path.GetFullPath repositoryRoot

        [ { SurfaceId = "codex-local"
            DisplayName = "Codex/local agent wrappers"
            RootPath = relativePath root (Path.Combine(root, ".agents", "skills"))
            Kind = Wrapper
            Agent = Codex
            IsRequired = true
            Notes = [] }
          { SurfaceId = "claude"
            DisplayName = "Claude wrappers"
            RootPath = relativePath root (Path.Combine(root, ".claude", "skills"))
            Kind = Wrapper
            Agent = Claude
            IsRequired = true
            Notes = [] }
          { SurfaceId = "package-canonical"
            DisplayName = "Package-owned canonical skills"
            RootPath = "src"
            Kind = Canonical
            Agent = Package
            IsRequired = true
            Notes = [] }
          { SurfaceId = "template-canonical"
            DisplayName = "Generated-product and template canonical skills"
            RootPath = "template"
            Kind = Canonical
            Agent = GeneratedProduct
            IsRequired = true
            Notes = [] }
          { SurfaceId = "ant-canonical"
            DisplayName = "Ant Design canonical skill"
            RootPath = ".claude/skills/fs-gg-ant-design/SKILL.md"
            Kind = Canonical
            Agent = Repository
            IsRequired = true
            Notes = [ "Routed to by the Codex Ant wrapper." ] }
          { SurfaceId = "spec-kit-command"
            DisplayName = "Spec Kit command skills"
            RootPath = ".agents/skills/speckit-* and .claude/skills/speckit-*"
            Kind = Command
            Agent = SpecKit
            IsRequired = true
            Notes = [ "Command surfaces are reported but do not require canonical wrappers." ] } ]

    let private fixtureSurfaces root =
        [ { SurfaceId = "fixture-canonical"
            DisplayName = "Synthetic fixture canonical skills"
            RootPath = "canonical"
            Kind = Canonical
            Agent = Repository
            IsRequired = true
            Notes = [ "SYNTHETIC fixture surface." ] }
          { SurfaceId = "fixture-codex"
            DisplayName = "Synthetic Codex wrappers"
            RootPath = "codex"
            Kind = Wrapper
            Agent = Codex
            IsRequired = true
            Notes = [ "SYNTHETIC fixture surface." ] }
          { SurfaceId = "fixture-claude"
            DisplayName = "Synthetic Claude wrappers"
            RootPath = "claude"
            Kind = Wrapper
            Agent = Claude
            IsRequired = true
            Notes = [ "SYNTHETIC fixture surface." ] } ]
        |> List.map (fun surface -> { surface with RootPath = relativePath root (Path.Combine(root, surface.RootPath)) })

    let private commandSkillName (name: string) =
        name.StartsWith("speckit-", StringComparison.OrdinalIgnoreCase)

    let private targetFromContent (absoluteSkillPath: string) (content: string) =
        let routeIndex = content.IndexOf("Before acting", StringComparison.OrdinalIgnoreCase)
        let matches = Regex.Matches(content, "`([^`]*SKILL\\.md)`", RegexOptions.IgnoreCase)

        matches
        |> Seq.cast<Match>
        |> Seq.tryFind (fun m -> routeIndex < 0 || m.Index > routeIndex)
        |> Option.map (fun m ->
            let raw = m.Groups[1].Value.Trim()
            let baseDir =
                match Path.GetDirectoryName absoluteSkillPath with
                | null
                | "" -> "."
                | directory -> directory

            let resolved =
                if Path.IsPathRooted raw then
                    Path.GetFullPath raw
                else
                    Path.GetFullPath(Path.Combine(baseDir, raw))

            let exists = File.Exists resolved

            let targetMetadata, targetBody =
                if exists then
                    File.ReadAllText resolved |> parseFrontMatter
                else
                    Map.empty, ""

            { RawTarget = raw
              ResolvedPath = normalizeSeparators resolved
              Exists = exists
              CanonicalSkillName = targetMetadata |> Map.tryFind "name"
              CanonicalDescription = targetMetadata |> Map.tryFind "description"
              TargetHash = if exists then Some(sha256 targetBody) else None })

    let private readEntry (repositoryRoot: string) (surface: SkillSurface) (absoluteSkillPath: string) =
        let content = File.ReadAllText absoluteSkillPath
        let metadata, body = parseFrontMatter content
        let name = metadataValue "name" metadata
        let description = metadataValue "description" metadata
        let target = targetFromContent absoluteSkillPath content

        let kind =
            if commandSkillName name then
                CommandEntry
            elif surface.Kind = Canonical || surface.SurfaceId = "ant-canonical" then
                CanonicalEntry
            elif target.IsSome then
                WrapperEntry
            else
                WrapperOnlyEntry

        { SkillName = name
          Description = description
          Path = relativePath repositoryRoot absoluteSkillPath
          AbsolutePath = normalizeSeparators absoluteSkillPath
          SurfaceId = surface.SurfaceId
          EntryKind = kind
          Metadata = metadata
          BodyHash = sha256 body
          Content = content
          WrapperTarget = target }

    let private parentDirectoryName (path: string) =
        match Directory.GetParent path with
        | null -> ""
        | parent -> parent.Name

    let private filesForSurface (repositoryRoot: string) (surface: SkillSurface) =
        let rootPath = absolutePath repositoryRoot surface.RootPath

        let safeFiles (directory: string) (pattern: string) (search: SearchOption) =
            if Directory.Exists directory then
                Directory.GetFiles(directory, pattern, search) |> Array.toList
            else
                []

        match surface.SurfaceId with
        | "package-canonical" ->
            safeFiles (Path.Combine(repositoryRoot, "src")) "SKILL.md" SearchOption.AllDirectories
            |> List.filter (fun path -> normalizeSeparators path |> containsIgnoreCase "/skill/SKILL.md")
        | "template-canonical" ->
            safeFiles (Path.Combine(repositoryRoot, "template")) "SKILL.md" SearchOption.AllDirectories
            |> List.filter (fun path ->
                let normalized = normalizeSeparators path
                not (containsIgnoreCase "/.agents/skills/" normalized)
                && not (containsIgnoreCase "/.claude/skills/" normalized))
        | "ant-canonical" ->
            let path = Path.Combine(repositoryRoot, ".claude", "skills", "fs-gg-ant-design", "SKILL.md")
            if File.Exists path then [ path ] else []
        | "spec-kit-command" ->
            let agents = safeFiles (Path.Combine(repositoryRoot, ".agents", "skills")) "SKILL.md" SearchOption.AllDirectories
            let claude = safeFiles (Path.Combine(repositoryRoot, ".claude", "skills")) "SKILL.md" SearchOption.AllDirectories

            (agents @ claude)
            |> List.filter (fun path ->
                let name = parentDirectoryName path
                name.StartsWith("speckit-", StringComparison.OrdinalIgnoreCase))
        | "codex-local"
        | "claude" ->
            safeFiles rootPath "SKILL.md" SearchOption.AllDirectories
            |> List.filter (fun path ->
                let normalized = normalizeSeparators path
                not (containsIgnoreCase "/.claude/skills/fs-gg-ant-design/SKILL.md" normalized)
                && not ((parentDirectoryName path).StartsWith("speckit-", StringComparison.OrdinalIgnoreCase)))
        | _ ->
            if File.Exists rootPath then
                [ rootPath ]
            elif Directory.Exists rootPath then
                safeFiles rootPath "SKILL.md" SearchOption.AllDirectories
            else
                []

    let inventorySkills request surfaces =
        surfaces
        |> List.collect (fun surface ->
            filesForSurface request.RepositoryRoot surface
            |> List.choose (fun path ->
                try
                    Some(readEntry request.RepositoryRoot surface (Path.GetFullPath path))
                with _ ->
                    None))
        |> List.distinctBy (fun entry -> entry.SurfaceId, entry.Path)

    let private ruleApplies (rule: GuidanceRule) (entry: SkillEntry) =
        let haystack = $"{entry.SkillName} {entry.Description} {entry.Path}".ToLowerInvariant()

        rule.ApplicablePatterns
        |> List.exists (fun pattern -> haystack.Contains(pattern.ToLowerInvariant()))

    let evaluateGuidanceCoverage rules entries =
        entries
        |> List.filter (fun entry ->
            entry.EntryKind = CanonicalEntry
            || entry.EntryKind = CommandEntry)
        |> List.collect (fun entry ->
            let content = entry.Content.ToLowerInvariant()

            rules
            |> List.map (fun rule ->
                if not (ruleApplies rule entry) then
                    { RuleId = rule.RuleId
                      SkillName = entry.SkillName
                      SurfaceId = entry.SurfaceId
                      Path = entry.Path
                      Status = NotApplicable
                      Evidence = []
                      MissingReferences = []
                      ExceptionId = None }
                else
                    let matched, missing =
                        rule.RequiredReferences
                        |> List.map (fun alternatives ->
                            let hit =
                                alternatives
                                |> List.tryFind (fun token -> content.Contains(token.ToLowerInvariant()))

                            match hit with
                            | Some token -> Choice1Of2 token
                            | None -> Choice2Of2(String.concat " or " alternatives))
                        |> List.partition (function Choice1Of2 _ -> true | Choice2Of2 _ -> false)

                    let evidence =
                        matched
                        |> List.choose (function Choice1Of2 token -> Some token | _ -> None)

                    let missingReferences =
                        missing
                        |> List.choose (function Choice2Of2 token -> Some token | _ -> None)

                    let status =
                        if missingReferences.IsEmpty then
                            Covered
                        elif evidence.IsEmpty then
                            Missing
                        else
                            Partial

                    { RuleId = rule.RuleId
                      SkillName = entry.SkillName
                      SurfaceId = entry.SurfaceId
                      Path = entry.Path
                      Status = status
                      Evidence = evidence
                      MissingReferences = missingReferences
                      ExceptionId = None }))

    let private findingId category surface skill =
        $"{categoryToken category}:{surface}:{skill}"

    let private productAliasTarget (skillName: string) =
        let normalized = normalizeText skillName

        if normalized.StartsWith("fs-gg-product-") then
            Some(normalized.Replace("fs-gg-product-", "fs-gg-"))
        else
            None

    let private isIntentionalProductAlias (wrapperName: string) (targetName: string) =
        productAliasTarget wrapperName
        |> Option.exists (fun expected -> expected = normalizeText targetName)

    let private wrapperFindings (entries: SkillEntry list) =
        entries
        |> List.choose (fun entry ->
            match entry.EntryKind, entry.WrapperTarget with
            | WrapperEntry, Some target when not target.Exists ->
                Some
                    { FindingId = findingId BrokenTarget entry.SurfaceId entry.SkillName
                      SkillName = entry.SkillName
                      SurfaceId = entry.SurfaceId
                      Category = BrokenTarget
                      Severity = High
                      CanonicalPath = Some target.RawTarget
                      WrapperPath = Some entry.Path
                      RuleId = None
                      Message = "Wrapper target does not resolve."
                      Remediation = "Update the wrapper target path or restore the canonical skill source."
                      ExceptionId = None }
            | WrapperEntry, Some target ->
                match target.CanonicalSkillName, target.CanonicalDescription with
                | Some targetName, _ when normalizeText targetName <> normalizeText entry.SkillName
                                          && not (isIntentionalProductAlias entry.SkillName targetName) ->
                    Some
                        { FindingId = findingId MetadataDrift entry.SurfaceId entry.SkillName
                          SkillName = entry.SkillName
                          SurfaceId = entry.SurfaceId
                          Category = MetadataDrift
                          Severity = Warning
                          CanonicalPath = Some(relativePath (Path.GetFullPath ".") target.ResolvedPath)
                          WrapperPath = Some entry.Path
                          RuleId = None
                          Message = "Wrapper skill name differs from the routed canonical skill."
                          Remediation = "Align wrapper metadata or document an intentional command exception."
                          ExceptionId = None }
                | _, Some targetDescription when normalizeText targetDescription <> normalizeText entry.Description ->
                    Some
                        { FindingId = findingId StaleDescription entry.SurfaceId entry.SkillName
                          SkillName = entry.SkillName
                          SurfaceId = entry.SurfaceId
                          Category = StaleDescription
                          Severity = Warning
                          CanonicalPath = Some target.RawTarget
                          WrapperPath = Some entry.Path
                          RuleId = None
                          Message = "Wrapper description differs from the canonical skill description."
                          Remediation = "Refresh the wrapper description or add an explicit exception."
                          ExceptionId = None }
                | _ -> None
            | WrapperOnlyEntry, None ->
                Some
                    { FindingId = findingId WrapperOnly entry.SurfaceId entry.SkillName
                      SkillName = entry.SkillName
                      SurfaceId = entry.SurfaceId
                      Category = WrapperOnly
                      Severity = Warning
                      CanonicalPath = None
                      WrapperPath = Some entry.Path
                      RuleId = None
                      Message = "Wrapper entry has no canonical target."
                      Remediation = "Add a canonical source route or classify the entry as an intentional command skill."
                      ExceptionId = None }
            | _ -> None)

    let private requiresWrapper (entry: SkillEntry) =
        entry.EntryKind = CanonicalEntry
        && (entry.SurfaceId = "package-canonical"
            || entry.SurfaceId = "ant-canonical"
            || entry.Path.Contains("template/product-skills", StringComparison.OrdinalIgnoreCase)
            || entry.SurfaceId = "fixture-canonical")

    let private missingWrapperFindings (entries: SkillEntry list) =
        let wrapperNames surfaceId =
            entries
            |> List.filter (fun entry -> entry.SurfaceId = surfaceId && entry.EntryKind = WrapperEntry)
            |> List.map (fun entry -> normalizeText entry.SkillName)
            |> Set.ofList

        let codexNames = wrapperNames "codex-local" + wrapperNames "fixture-codex"
        let claudeNames = wrapperNames "claude" + wrapperNames "fixture-claude"

        entries
        |> List.filter requiresWrapper
        |> List.collect (fun entry ->
            [ "codex-local", codexNames
              "claude", claudeNames ]
            |> List.choose (fun (surfaceId, names) ->
                let canonicalName = normalizeText entry.SkillName
                let productAliasName = canonicalName.Replace("fs-gg-", "fs-gg-product-")
                let exposedAsAlias =
                    entry.Path.Contains("template/product-skills", StringComparison.OrdinalIgnoreCase)
                    && names.Contains productAliasName
                let antCanonicalSelfExposed = entry.SurfaceId = "ant-canonical" && surfaceId = "claude"

                if names.Contains(canonicalName) || exposedAsAlias || antCanonicalSelfExposed then
                    None
                else
                    Some
                        { FindingId = findingId MissingWrapper surfaceId entry.SkillName
                          SkillName = entry.SkillName
                          SurfaceId = surfaceId
                          Category = MissingWrapper
                          Severity = Warning
                          CanonicalPath = Some entry.Path
                          WrapperPath = None
                          RuleId = None
                          Message = "Canonical skill is not exposed on this supported wrapper surface."
                          Remediation = "Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception."
                          ExceptionId = None }))

    let private canonicalDriftFindings request (entries: SkillEntry list) =
        if request.FixtureMode.IsNone then
            []
        else
            entries
            |> List.filter (fun entry -> entry.EntryKind = CanonicalEntry)
            |> List.groupBy (fun entry -> normalizeText entry.SkillName)
            |> List.collect (fun (_, group) ->
                let descriptions =
                    group
                    |> List.map (fun entry -> normalizeText entry.Description)
                    |> Set.ofList

                if group.Length > 1 && descriptions.Count > 1 then
                    group
                    |> List.map (fun entry ->
                        { FindingId = findingId CanonicalDrift entry.SurfaceId entry.SkillName + ":" + entry.Path.Replace("/", "-")
                          SkillName = entry.SkillName
                          SurfaceId = entry.SurfaceId
                          Category = CanonicalDrift
                          Severity = High
                          CanonicalPath = Some entry.Path
                          WrapperPath = None
                          RuleId = None
                          Message = "Duplicate canonical sources with the same skill name diverge."
                          Remediation = "Choose one canonical source or document a specific variant exception."
                          ExceptionId = None })
                else
                    [])

    let private guidanceFindings (coverage: GuidanceCoverage list) =
        coverage
        |> List.choose (fun item ->
            match item.Status with
            | Missing
            | Partial ->
                let severity =
                    if item.Status = Missing then High else Warning

                Some
                    { FindingId = $"{categoryToken GuidanceRuleGap}:{item.SurfaceId}:{item.SkillName}:{item.RuleId}"
                      SkillName = item.SkillName
                      SurfaceId = item.SurfaceId
                      Category = GuidanceRuleGap
                      Severity = severity
                      CanonicalPath = Some item.Path
                      WrapperPath = None
                      RuleId = Some item.RuleId
                      Message = $"Guidance rule {item.RuleId} is {coverageToken item.Status}."
                      Remediation = "Add the missing concrete command/path/status caveat or record an explicit exception."
                      ExceptionId = item.ExceptionId }
            | _ -> None)

    let private classifyFindings request entries coverage =
        wrapperFindings entries
        @ missingWrapperFindings entries
        @ canonicalDriftFindings request entries
        @ guidanceFindings coverage
        |> List.distinctBy (fun finding -> finding.FindingId)

    let private severityCounts findings =
        { Critical = findings |> List.filter (fun f -> f.Severity = Critical) |> List.length
          High = findings |> List.filter (fun f -> f.Severity = High) |> List.length
          Warning = findings |> List.filter (fun f -> f.Severity = Warning) |> List.length
          Info = findings |> List.filter (fun f -> f.Severity = Info) |> List.length }

    let private coverageSummary (rules: GuidanceRule list) (coverage: GuidanceCoverage list) =
        rules
        |> List.map (fun rule ->
            let items = coverage |> List.filter (fun item -> item.RuleId = rule.RuleId)

            { RuleId = rule.RuleId
              Covered = items |> List.filter (fun item -> item.Status = Covered) |> List.length
              Partial = items |> List.filter (fun item -> item.Status = Partial) |> List.length
              Missing = items |> List.filter (fun item -> item.Status = Missing) |> List.length
              Excepted = items |> List.filter (fun item -> item.Status = Excepted) |> List.length
              NotApplicable = items |> List.filter (fun item -> item.Status = NotApplicable) |> List.length })

    let private reportStatus findings =
        if findings |> List.exists (fun f -> f.Severity = Critical || f.Severity = High) then
            Failed
        elif findings |> List.exists (fun f -> f.Severity = Warning || f.Severity = Info) then
            WarningStatus
        else
            Passed

    let private commandText request =
        let fixture =
            request.FixtureMode
            |> Option.map (fun mode -> $" --fixture {mode}")
            |> Option.defaultValue ""

        $"dotnet fsi scripts/check-agent-skill-parity.fsx --out {request.OutDir} --report {request.ReportPath} --summary-json {request.SummaryJsonPath}{fixture} --fail-on {severityToken request.FailOnSeverity}"

    let private buildReport
        (request: ParityCheckRequest)
        (surfaces: SkillSurface list)
        (entries: SkillEntry list)
        (coverage: GuidanceCoverage list)
        (findings: ParityFinding list)
        =
        let counts = severityCounts findings
        let rules = defaultGuidanceRules ()

        { CheckedAtUtc = DateTime.UtcNow
          RepositoryRoot = request.RepositoryRoot
          OverallStatus = reportStatus findings
          SupportedSurfaces = surfaces
          CanonicalSourceCount =
            entries
            |> List.filter (fun entry -> entry.EntryKind = CanonicalEntry)
            |> List.distinctBy (fun entry -> entry.Path)
            |> List.length
          WrapperCount = entries |> List.filter (fun entry -> entry.EntryKind = WrapperEntry) |> List.length
          FindingCountsBySeverity = counts
          GuidanceRuleCoverage = coverageSummary rules coverage
          Findings = findings
          IntentionalExceptions = []
          GeneratedReportPath = request.ReportPath
          StructuredSummaryPath = request.SummaryJsonPath
          Caveats =
            [ "Global Codex skill installation paths are excluded from required repository parity."
              if request.FixtureMode.IsSome then
                  "Fixture mode uses synthetic skill files and is not real repository parity evidence." ]
          Command = commandText request }

    let private createSkillFile path name description body =
        ensureParent path

        let content =
            $"""---
name: {name}
description: {description}
---

# {name}

{body}
"""

        File.WriteAllText(path, content)

    let private createWrapper path name description target =
        ensureParent path

        let content =
            $"""---
name: {name}
description: {description}
---

# {name}

Synthetic wrapper for fixture parity.

Before acting, read the canonical instructions in:

`{target}`
"""

        File.WriteAllText(path, content)

    let createFixture root fixtureName =
        if Directory.Exists root then
            Directory.Delete(root, true)

        Directory.CreateDirectory root |> ignore

        let includeCase name =
            fixtureName = "all" || fixtureName = name

        let full parts = Path.Combine(Array.ofList (root :: parts))

        let coveredBody =
            "FS.GG.UI. package pins use scripts/refresh-local-feed-and-samples.fsx and package-feed local feed proof for stale package pins. specs/*/readiness/ is allowlisted through .gitignore and git check-ignore. dotnet test for the same project/configuration needs isolated output or BaseOutputPath. screenshot evidence records degraded capture, reviewer accepted readiness, and generated summary caveats. pointer and keyboard responsiveness separate routing from update render present latency. package bump uses local feed, sample package pins, restore validation, and readiness ledger updates. canceled timed-out synthetic substitute environment-limited pending-review checks keep caveats visible."

        if includeCase "passing" then
            createSkillFile (full [ "canonical"; "passing"; "SKILL.md" ]) "fs-gg-fixture-passing" "Aligned fixture skill." coveredBody
            createWrapper (full [ "codex"; "passing"; "SKILL.md" ]) "fs-gg-fixture-passing" "Aligned fixture skill." "../../canonical/passing/SKILL.md"
            createWrapper (full [ "claude"; "passing"; "SKILL.md" ]) "fs-gg-fixture-passing" "Aligned fixture skill." "../../canonical/passing/SKILL.md"

        if includeCase "missing-wrapper" then
            createSkillFile (full [ "canonical"; "missing-wrapper"; "SKILL.md" ]) "fs-gg-fixture-missing" "Missing wrapper fixture." coveredBody

        if includeCase "wrapper-only" then
            createSkillFile (full [ "codex"; "wrapper-only"; "SKILL.md" ]) "fs-gg-fixture-wrapper-only" "Wrapper only fixture." "No canonical route."

        if includeCase "stale-description" then
            createSkillFile (full [ "canonical"; "stale-description"; "SKILL.md" ]) "fs-gg-fixture-stale" "Current canonical description." coveredBody
            createWrapper (full [ "codex"; "stale-description"; "SKILL.md" ]) "fs-gg-fixture-stale" "Old wrapper description." "../../canonical/stale-description/SKILL.md"

        if includeCase "broken-target" then
            createWrapper (full [ "codex"; "broken-target"; "SKILL.md" ]) "fs-gg-fixture-broken" "Broken target fixture." "../../canonical/does-not-exist/SKILL.md"

        if includeCase "canonical-drift" then
            createSkillFile (full [ "canonical"; "drift-a"; "SKILL.md" ]) "fs-gg-fixture-drift" "Canonical description A." coveredBody
            createSkillFile (full [ "canonical"; "drift-b"; "SKILL.md" ]) "fs-gg-fixture-drift" "Canonical description B." coveredBody

        if includeCase "guidance-gap" then
            createSkillFile (full [ "canonical"; "guidance-gap"; "SKILL.md" ]) "fs-gg-testing" "Testing guidance gap fixture." "This skill intentionally omits required tokens."

    let private effectiveSurfaces request root =
        if request.SurfaceOverrides.IsEmpty then
            match request.FixtureMode with
            | Some _ -> fixtureSurfaces root
            | None -> discoverDefaultSurfaces root
        else
            request.SurfaceOverrides
            |> List.map (fun (surfaceId, path) ->
                { SurfaceId = surfaceId
                  DisplayName = surfaceId
                  RootPath = path
                  Kind = Mixed
                  Agent = Repository
                  IsRequired = true
                  Notes = [ "Operator-supplied surface override." ] })

    let runCheck request =
        let repositoryRoot = Path.GetFullPath request.RepositoryRoot

        let effectiveRequest =
            match request.FixtureMode with
            | Some fixtureName ->
                let fixtureRoot = Path.Combine(request.OutDir, "_skill-parity-fixture")
                createFixture fixtureRoot fixtureName
                { request with RepositoryRoot = fixtureRoot }
            | None -> { request with RepositoryRoot = repositoryRoot }

        let surfaces = effectiveSurfaces effectiveRequest effectiveRequest.RepositoryRoot
        let entries = inventorySkills effectiveRequest surfaces
        let rules = defaultGuidanceRules ()
        let coverage = evaluateGuidanceCoverage rules entries
        let findings = classifyFindings effectiveRequest entries coverage
        buildReport effectiveRequest surfaces entries coverage findings

    let private markdownTableRow (values: string list) =
        "| " + (values |> List.map (fun value -> value.Replace("\n", " ")) |> String.concat " | ") + " |"

    let renderMarkdown report =
        let sb = StringBuilder()
        let checkedAt = report.CheckedAtUtc.ToString("O", CultureInfo.InvariantCulture)

        sb.AppendLine("# Skill Parity Report") |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine($"Checked at UTC: `{checkedAt}`") |> ignore
        sb.AppendLine($"Overall status: `{overallStatusToken report.OverallStatus}`") |> ignore
        sb.AppendLine($"Canonical sources: `{report.CanonicalSourceCount}`") |> ignore
        sb.AppendLine($"Wrappers: `{report.WrapperCount}`") |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine("## Supported Surfaces") |> ignore
        sb.AppendLine(markdownTableRow [ "Surface"; "Kind"; "Agent"; "Root"; "Required" ]) |> ignore
        sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---"; "---" ]) |> ignore

        for surface in report.SupportedSurfaces do
            sb.AppendLine(
                markdownTableRow
                    [ surface.SurfaceId
                      surfaceKindToken surface.Kind
                      agentToken surface.Agent
                      surface.RootPath
                      string surface.IsRequired ]
            )
            |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine("## Severity Counts") |> ignore
        sb.AppendLine(markdownTableRow [ "Critical"; "High"; "Warning"; "Info" ]) |> ignore
        sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---" ]) |> ignore
        sb.AppendLine(
            markdownTableRow
                [ string report.FindingCountsBySeverity.Critical
                  string report.FindingCountsBySeverity.High
                  string report.FindingCountsBySeverity.Warning
                  string report.FindingCountsBySeverity.Info ]
        )
        |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine("## Guidance Coverage") |> ignore
        sb.AppendLine(markdownTableRow [ "Rule"; "Covered"; "Partial"; "Missing"; "Excepted"; "Not applicable" ]) |> ignore
        sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---"; "---"; "---" ]) |> ignore

        for coverage in report.GuidanceRuleCoverage do
            sb.AppendLine(
                markdownTableRow
                    [ coverage.RuleId
                      string coverage.Covered
                      string coverage.Partial
                      string coverage.Missing
                      string coverage.Excepted
                      string coverage.NotApplicable ]
            )
            |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine("## Findings") |> ignore

        match report.Findings with
        | [] -> sb.AppendLine("No unresolved parity findings.") |> ignore
        | findings ->
            sb.AppendLine(markdownTableRow [ "Skill"; "Surface"; "Category"; "Severity"; "Path"; "Message"; "Next action" ]) |> ignore
            sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---"; "---"; "---"; "---" ]) |> ignore

            for finding in findings do
                let path =
                    finding.WrapperPath
                    |> Option.orElse finding.CanonicalPath
                    |> Option.defaultValue ""

                sb.AppendLine(
                    markdownTableRow
                        [ finding.SkillName
                          finding.SurfaceId
                          categoryToken finding.Category
                          severityToken finding.Severity
                          path
                          finding.Message
                          finding.Remediation ]
                )
                |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine("## Intentional Exceptions") |> ignore

        match report.IntentionalExceptions with
        | [] -> sb.AppendLine("No intentional exceptions were applied.") |> ignore
        | exceptions ->
            sb.AppendLine(markdownTableRow [ "Id"; "Skill"; "Surface"; "Reason"; "Review" ]) |> ignore
            sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---"; "---" ]) |> ignore

            for exceptionItem in exceptions do
                sb.AppendLine(
                    markdownTableRow
                        [ exceptionItem.ExceptionId
                          exceptionItem.SkillName
                          exceptionItem.SurfaceId
                          exceptionItem.Reason
                          exceptionItem.ReviewDate ]
                )
                |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine("## Caveats") |> ignore

        for caveat in report.Caveats do
            sb.AppendLine($"- {caveat}") |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine("## Regenerate") |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine("```sh") |> ignore
        sb.AppendLine(report.Command) |> ignore
        sb.AppendLine("```") |> ignore

        sb.ToString()

    let renderSummaryJson report =
        let nullable (value: string option) : obj | null =
            match value with
            | Some text -> box text
            | None -> null

        let options = JsonSerializerOptions(WriteIndented = true)

        let surfaces =
            report.SupportedSurfaces
            |> List.map (fun surface ->
                {| surfaceId = surface.SurfaceId
                   kind = surfaceKindToken surface.Kind
                   rootPath = surface.RootPath
                   skillCount = 0
                   required = surface.IsRequired |})

        let coverage =
            report.GuidanceRuleCoverage
            |> List.map (fun item ->
                {| ruleId = item.RuleId
                   covered = item.Covered
                   partial = item.Partial
                   missing = item.Missing
                   excepted = item.Excepted
                   notApplicable = item.NotApplicable |})

        let findings =
            report.Findings
            |> List.map (fun finding ->
                {| findingId = finding.FindingId
                   skillName = finding.SkillName
                   surfaceId = finding.SurfaceId
                   category = categoryToken finding.Category
                   severity = severityToken finding.Severity
                   canonicalPath = nullable finding.CanonicalPath
                   wrapperPath = nullable finding.WrapperPath
                   ruleId = nullable finding.RuleId
                   message = finding.Message
                   remediation = finding.Remediation
                   exceptionId = nullable finding.ExceptionId |})

        JsonSerializer.Serialize(
            {| checkedAtUtc = report.CheckedAtUtc.ToString("O", CultureInfo.InvariantCulture)
               overallStatus = overallStatusToken report.OverallStatus
               repositoryRoot = report.RepositoryRoot
               surfaces = surfaces
               canonicalSourceCount = report.CanonicalSourceCount
               wrapperCount = report.WrapperCount
               findingCountsBySeverity =
                {| critical = report.FindingCountsBySeverity.Critical
                   high = report.FindingCountsBySeverity.High
                   warning = report.FindingCountsBySeverity.Warning
                   info = report.FindingCountsBySeverity.Info |}
               guidanceRuleCoverage = coverage
               findings = findings
               caveats = report.Caveats |},
            options
        )

    let private generatedStart = "<!-- SKILL-PARITY:START -->"
    let private generatedEnd = "<!-- SKILL-PARITY:END -->"

    let private generatedBlock (content: string) =
        generatedStart + Environment.NewLine + content.TrimEnd() + Environment.NewLine + generatedEnd + Environment.NewLine

    let private mergeGeneratedSection (existing: string) (generated: string) =
        let startIndex = existing.IndexOf(generatedStart, StringComparison.Ordinal)
        let endIndex = existing.IndexOf(generatedEnd, StringComparison.Ordinal)

        if startIndex >= 0 && endIndex > startIndex then
            let before = existing.Substring(0, startIndex)
            let after = existing.Substring(endIndex + generatedEnd.Length)
            before + generated + after.TrimStart('\r', '\n')
        else
            generated

    let private writeGenerated path content =
        ensureParent path
        let generated = generatedBlock content

        let finalContent =
            if File.Exists path then
                mergeGeneratedSection (File.ReadAllText path) generated
            else
                generated

        File.WriteAllText(path, finalContent)
        path

    let private renderCoverageMarkdown report =
        let sb = StringBuilder()
        sb.AppendLine("# Feature 168 Guidance Coverage") |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine($"Overall status: `{overallStatusToken report.OverallStatus}`") |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine(markdownTableRow [ "Rule"; "Covered"; "Partial"; "Missing"; "Excepted"; "Not applicable" ]) |> ignore
        sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---"; "---"; "---" ]) |> ignore

        for coverage in report.GuidanceRuleCoverage do
            sb.AppendLine(
                markdownTableRow
                    [ coverage.RuleId
                      string coverage.Covered
                      string coverage.Partial
                      string coverage.Missing
                      string coverage.Excepted
                      string coverage.NotApplicable ]
            )
            |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine("## Required Rules") |> ignore

        for rule in defaultGuidanceRules () do
            sb.AppendLine($"- `{rule.RuleId}`: {rule.Theme}") |> ignore

        sb.ToString()

    let writeReport request report =
        let reportPath = writeGenerated report.GeneratedReportPath (renderMarkdown report)
        ensureParent report.StructuredSummaryPath
        File.WriteAllText(report.StructuredSummaryPath, renderSummaryJson report)

        let readinessReport =
            if String.IsNullOrWhiteSpace request.OutDir || request.FixtureMode.IsSome then
                None
            else
                let path = Path.Combine(request.OutDir, "..", "skill-parity-report.md") |> Path.GetFullPath
                Some(writeGenerated path (renderMarkdown report))

        let coveragePath =
            if String.IsNullOrWhiteSpace request.OutDir || request.FixtureMode.IsSome then
                None
            else
                let path = Path.Combine(request.OutDir, "..", "guidance-coverage.md") |> Path.GetFullPath
                Some(writeGenerated path (renderCoverageMarkdown report))

        [ Some reportPath
          Some report.StructuredSummaryPath
          readinessReport
          coveragePath ]
        |> List.choose id

    let init request =
        { Request = request
          Surfaces = []
          Entries = []
          Findings = []
          Coverage = []
          Report = None
          Diagnostics = [] },
        [ ReadSkillSurfaces ]

    let update msg model =
        match msg with
        | InventoryRequested -> model, [ ReadSkillSurfaces ]
        | InventoryLoaded (surfaces, entries) -> { model with Surfaces = surfaces; Entries = entries }, [ EvaluateGuidanceRules ]
        | CoverageEvaluated coverage -> { model with Coverage = coverage }, [ ClassifyFindings ]
        | FindingsClassified findings -> { model with Findings = findings }, []
        | ReportGenerated report -> { model with Report = Some report }, [ WriteMarkdownReport; WriteSummaryJson ]
        | WorkflowFailed reason -> { model with Diagnostics = model.Diagnostics @ [ reason ] }, []

    let private parseSeverity (token: string) =
        match token.ToLowerInvariant() with
        | "info" -> Some Info
        | "warning" -> Some Warning
        | "high" -> Some High
        | "critical" -> Some Critical
        | _ -> None

    let private flagValue flag args =
        let rec loop rest =
            match rest with
            | f :: value :: _ when f = flag -> Some value
            | _ :: tail -> loop tail
            | [] -> None

        loop args

    let private flagValues flag args =
        let rec loop acc rest =
            match rest with
            | f :: value :: tail when f = flag -> loop (acc @ [ value ]) tail
            | _ :: tail -> loop acc tail
            | [] -> acc

        loop [] args

    let private hasFlag flag args =
        args |> List.exists ((=) flag)

    let private parseSurfaceOverride (value: string) =
        let index = value.IndexOf('=')

        if index <= 0 then
            None
        else
            Some(value.Substring(0, index), value.Substring(index + 1))

    let private requestFromArgs args =
        let repo =
            flagValue "--repo" args
            |> Option.defaultValue (Directory.GetCurrentDirectory())
            |> Path.GetFullPath

        let baseRequest = defaultRequest repo
        let outDir = flagValue "--out" args |> Option.defaultValue baseRequest.OutDir
        let fixtureMode = flagValue "--fixture" args

        let reportPath =
            match flagValue "--report" args, fixtureMode with
            | Some path, _ -> path
            | None, Some _ -> Path.Combine(outDir, "fixture-results.md")
            | None, None -> baseRequest.ReportPath

        let summaryPath =
            flagValue "--summary-json" args
            |> Option.defaultValue (Path.Combine(outDir, "skill-parity-summary.json"))

        let failOn =
            flagValue "--fail-on" args
            |> Option.bind parseSeverity
            |> Option.defaultValue High

        { baseRequest with
            OutDir = outDir
            ReportPath = reportPath
            SummaryJsonPath = summaryPath
            FixtureMode = fixtureMode
            SurfaceOverrides = flagValues "--surface" args |> List.choose parseSurfaceOverride
            AllowedExceptionIds = flagValues "--allow-exception" args |> Set.ofList
            FailOnSeverity = failOn
            ListRulesOnly = hasFlag "--list-rules" args
            JsonOutput = hasFlag "--json" args }

    let private printRules () =
        for rule in defaultGuidanceRules () do
            printfn "%s\t%s" rule.RuleId rule.Theme

    let runCli argv =
        let request = requestFromArgs argv

        if request.ListRulesOnly then
            printRules ()
            0
        else
            let report = runCheck request
            writeReport request report |> ignore

            if request.JsonOutput then
                printfn
                    "{\"summaryJson\":%s,\"report\":%s,\"overallStatus\":%s,\"critical\":%i,\"high\":%i,\"warning\":%i,\"info\":%i}"
                    (JsonSerializer.Serialize report.StructuredSummaryPath)
                    (JsonSerializer.Serialize report.GeneratedReportPath)
                    (JsonSerializer.Serialize(overallStatusToken report.OverallStatus))
                    report.FindingCountsBySeverity.Critical
                    report.FindingCountsBySeverity.High
                    report.FindingCountsBySeverity.Warning
                    report.FindingCountsBySeverity.Info
            else
                printfn "skill-parity status: %s" (overallStatusToken report.OverallStatus)
                printfn "report: %s" report.GeneratedReportPath
                printfn "summary-json: %s" report.StructuredSummaryPath
                printfn
                    "findings: critical=%i high=%i warning=%i info=%i"
                    report.FindingCountsBySeverity.Critical
                    report.FindingCountsBySeverity.High
                    report.FindingCountsBySeverity.Warning
                    report.FindingCountsBySeverity.Info

            if report.Findings |> List.exists (fun finding -> severityRank finding.Severity >= severityRank request.FailOnSeverity) then
                1
            else
                0
