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

    /// Verdict for one `Module.member` an FS.GG skill documents inside an F# code fence.
    type SymbolStatus =
        /// Present in the public surface baseline, and called by at least one test source. This says
        /// a test names the API in code — not that the test asserts anything meaningful about it.
        | Exercised
        /// Present in the public surface baseline, but no test calls it — a seam that may be dead.
        | Unexercised
        /// Absent from the public surface baseline — the skill documents an API that does not exist.
        | Unresolved

    /// A repository-owned artifact that a skill's process guidance must point at. Both cases are
    /// adjudicated by a closed world — the filesystem, or the harness dispatch table — never by the
    /// skill's vocabulary.
    type ArtifactRef =
        | RepoPath of path: string
        | HarnessCommand of verb: string

    /// Verdict for one `GuardedTheme` against one skill in its scope.
    type ArtifactStatus =
        | ArtifactResolved
        | ArtifactDangling
        | ArtifactUnnamed

    type GuardedTheme =
        { ThemeId: string
          Intent: string
          Artifacts: ArtifactRef list
          ApplicablePatterns: string list }

    type ArtifactReference =
        { ThemeId: string
          Intent: string
          SkillName: string
          SurfaceId: string
          Path: string
          Reference: ArtifactRef option
          Expected: ArtifactRef list
          Status: ArtifactStatus }

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
        | UnresolvedApiSymbol
        | UnexercisedApiSymbol
        | UnresolvedArtifactReference
        | MissingRequiredArtifact
        | MetadataDrift
        | UnresolvedMetadataSource
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

    /// One `Module.member` a skill documents, resolved against the surface baseline and the test corpus.
    type ApiSymbol =
        { Symbol: string
          SkillName: string
          SurfaceId: string
          Path: string
          Status: SymbolStatus }

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
          Symbol: string option
          Message: string
          Remediation: string
          ExceptionId: string option }

    type SeverityCounts =
        { Critical: int
          High: int
          Warning: int
          Info: int }

    type SkillSymbolSummary =
        { SkillName: string
          Documented: int
          Exercised: int
          Unexercised: int
          Unresolved: int }

    type ThemeArtifactSummary =
        { ThemeId: string
          Scoped: int
          Resolved: int
          Dangling: int
          Unnamed: int }

    type ParityReport =
        { CheckedAtUtc: DateTime
          RepositoryRoot: string
          OverallStatus: OverallStatus
          SupportedSurfaces: SkillSurface list
          CanonicalSourceCount: int
          WrapperCount: int
          FindingCountsBySeverity: SeverityCounts
          ApiSymbolCoverage: SkillSymbolSummary list
          GuardedThemeCoverage: ThemeArtifactSummary list
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
          ListSymbolsOnly: bool
          JsonOutput: bool }

    type Model =
        { Request: ParityCheckRequest
          Surfaces: SkillSurface list
          Entries: SkillEntry list
          Findings: ParityFinding list
          Symbols: ApiSymbol list
          Artifacts: ArtifactReference list
          Report: ParityReport option
          Diagnostics: string list }

    type Msg =
        | InventoryRequested
        | InventoryLoaded of SkillSurface list * SkillEntry list
        | SymbolsResolved of ApiSymbol list
        | ArtifactsResolved of ArtifactReference list
        | FindingsClassified of ParityFinding list
        | ReportGenerated of ParityReport
        | WorkflowFailed of string

    type Effect =
        | ReadSkillSurfaces
        | ResolveApiSymbols
        | ResolveArtifactReferences
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

    let symbolStatusToken status =
        match status with
        | Exercised -> "exercised"
        | Unexercised -> "unexercised"
        | Unresolved -> "unresolved"

    let artifactStatusToken status =
        match status with
        | ArtifactResolved -> "resolved"
        | ArtifactDangling -> "dangling"
        | ArtifactUnnamed -> "unnamed"

    let artifactRefToken reference =
        match reference with
        | RepoPath path -> path
        | HarnessCommand verb -> verb

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
        | UnresolvedApiSymbol -> "unresolved-api-symbol"
        | UnexercisedApiSymbol -> "unexercised-api-symbol"
        | UnresolvedArtifactReference -> "unresolved-artifact-reference"
        | MissingRequiredArtifact -> "missing-required-artifact"
        | MetadataDrift -> "metadata-drift"
        | UnresolvedMetadataSource -> "unresolved-metadata-source"
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

    /// Qualified `Module.member` occurrences. Members are lower-camel by F# convention, so an
    /// uppercase second segment (a nested type, a union case, a property) is deliberately not a match.
    let private symbolPattern = Regex(@"\b([A-Z][A-Za-z0-9_]*)\.([a-z][A-Za-z0-9_]*)\b", RegexOptions.Compiled)

    let private qualifiedSymbols (text: string) =
        symbolPattern.Matches text
        |> Seq.map (fun m -> $"{m.Groups[1].Value}.{m.Groups[2].Value}")
        |> Set.ofSeq

    /// Tolerates an info string (```` ```fsharp {highlight=1} ````) and casing, so a decorated fence
    /// does not silently drop out of coverage.
    let private fencePattern =
        Regex(
            @"```fsharp[^\r\n]*\r?\n(.*?)```",
            RegexOptions.Compiled ||| RegexOptions.Singleline ||| RegexOptions.IgnoreCase
        )

    let private fsharpFences (content: string) =
        fencePattern.Matches content
        |> Seq.map (fun m -> m.Groups[1].Value)
        |> List.ofSeq

    let private surfaceBaselineDir root =
        Path.Combine(root, "readiness", "surface-baselines", "members")

    let private testSourceDir root = Path.Combine(root, "tests")

    let private blockCommentPattern = Regex(@"\(\*.*?\*\)", RegexOptions.Compiled ||| RegexOptions.Singleline)

    let private tripleQuotedPattern = Regex("\"\"\".*?\"\"\"", RegexOptions.Compiled ||| RegexOptions.Singleline)

    let private stringLiteralPattern = Regex("@?\"(?:\\\\.|\"\"|[^\"\\\\\\n])*\"", RegexOptions.Compiled)

    let private lineCommentPattern = Regex("//.*$", RegexOptions.Compiled ||| RegexOptions.Multiline)

    /// Strip everything in an F# source that is not code, so that *mentioning* an API in a comment or
    /// a string literal cannot pass for *exercising* it. Order matters: block comments and strings go
    /// first, so a `//` inside a URL literal is gone before line comments are stripped.
    let private codeOnly (source: string) =
        let stripped = blockCommentPattern.Replace(source, " ")
        let stripped = tripleQuotedPattern.Replace(stripped, " ")
        let stripped = stringLiteralPattern.Replace(stripped, " ")
        lineCommentPattern.Replace(stripped, " ")

    let private trailingGenericsPattern = Regex(@"<[^>]*>$", RegexOptions.Compiled)

    /// `FS.GG.UI.Controls.DataGrid.visibleRange<msg>(...) : Attr<msg>` -> ("DataGrid", "visibleRange").
    let private parseBaselineMember (line: string) =
        let trimmed = line.Trim()

        if trimmed = "" then
            None
        else
            let beforeParen =
                match trimmed.IndexOf '(' with
                | -1 -> trimmed
                | index -> trimmed.Substring(0, index)

            let beforeReturnType =
                match beforeParen.IndexOf ':' with
                | -1 -> beforeParen
                | index -> beforeParen.Substring(0, index)

            let withoutGenerics = trailingGenericsPattern.Replace(beforeReturnType.Trim(), "")
            let parts = withoutGenerics.Split '.'

            if parts.Length < 2 then
                None
            else
                Some(parts[parts.Length - 2], parts[parts.Length - 1])

    /// Declaring module -> its public member names, read from the member-granular surface baseline.
    /// This is the closed world: a `Module.member` whose module is absent here is product-local or
    /// pseudo-code in a skill's example, and is not the checker's business.
    ///
    /// Modules are keyed by simple name, because that is how a skill's fence writes them. Two
    /// consequences, both chosen to keep the check free of false positives:
    ///   * Same-named modules in different namespaces (`Controls.Button` and `Controls.Typed.Button`)
    ///     merge into one member set, so a member removed from only one of them still resolves.
    ///   * Members of a nested type (`ControlsElmish+Perf.runScript`) key under `ControlsElmish+Perf`,
    ///     so a fence writing `Perf.runScript` is treated as product-local and is not judged. Keying
    ///     them under `Perf` instead would make `Model` a known module and turn the model-swap skill's
    ///     product-local `Model.update` into a false finding.
    let loadSurfaceMembers repositoryRoot =
        let dir = surfaceBaselineDir repositoryRoot

        if not (Directory.Exists dir) then
            None
        else
            Directory.GetFiles(dir, "*.txt")
            |> Array.collect File.ReadAllLines
            |> Array.choose parseBaselineMember
            |> Array.fold
                (fun acc (moduleName, memberName) ->
                    let existing = acc |> Map.tryFind moduleName |> Option.defaultValue Set.empty
                    acc |> Map.add moduleName (existing |> Set.add memberName))
                Map.empty
            |> Some

    /// Every qualified symbol a test source *calls*. Comments and string literals are stripped first,
    /// so naming an API in prose cannot pass for exercising it.
    let loadExercisedSymbols repositoryRoot =
        let dir = testSourceDir repositoryRoot

        if not (Directory.Exists dir) then
            None
        else
            Directory.GetFiles(dir, "*.fs", SearchOption.AllDirectories)
            |> Array.map (File.ReadAllText >> codeOnly >> qualifiedSymbols)
            |> Set.unionMany
            |> Some

    /// Resolve every API symbol the canonical and command skills document in their F# code fences.
    let evaluateApiSymbols
        (surfaceMembers: Map<string, Set<string>>)
        (exercised: Set<string>)
        (entries: SkillEntry list)
        =
        entries
        |> List.filter (fun entry ->
            entry.EntryKind = CanonicalEntry
            || entry.EntryKind = CommandEntry)
        |> List.collect (fun entry ->
            // `codeOnly` first: a fence's comments name filenames (`Program.fs`) and cautionary APIs
            // that read as `Module.member` but document nothing.
            fsharpFences entry.Content
            |> List.map (codeOnly >> qualifiedSymbols)
            |> Set.unionMany
            |> Set.toList
            |> List.choose (fun symbol ->
                let parts = symbol.Split '.'
                let moduleName = parts[0]
                let memberName = parts[1]

                match surfaceMembers |> Map.tryFind moduleName with
                | None -> None
                | Some members ->
                    let status =
                        if not (members |> Set.contains memberName) then Unresolved
                        elif exercised |> Set.contains symbol then Exercised
                        else Unexercised

                    Some
                        { Symbol = symbol
                          SkillName = entry.SkillName
                          SurfaceId = entry.SurfaceId
                          Path = entry.Path
                          Status = status }))

    /// The themes that outlived the substring-matched guidance layer removed in #189. A theme is kept
    /// only where its prose pointed at a repository artifact that can be resolved; the four themes whose
    /// prose named nothing resolvable (`evidence-honesty`, `visual-readiness`, `responsiveness-diagnostics`,
    /// `validation-output-isolation`) were deliberately not carried over, because a check that cannot fail
    /// on a real regression is the false assurance #189 set out to delete. The disposition of all seven is
    /// recorded in `specs/235-gate-cadence-from-slnx/guidance-rule-disposition.md`.
    ///
    /// Scopes are the ones the deleted rules carried, so this narrows coverage without silently widening it.
    let defaultGuardedThemes () =
        [ { ThemeId = "package-pin-drift"
            Intent = "Package-consuming samples prove their FS.GG.UI.* pins against the local feed."
            Artifacts = [ HarnessCommand "package-feed"; RepoPath "scripts/refresh-local-feed-and-samples.fsx" ]
            ApplicablePatterns =
                [ "speckit-implement"
                  "speckit-merge"
                  "src/testing"
                  "template/fragments/samples"
                  "src/controls"
                  "src/skiaviewer"
                  "fs-gg-project" ] }
          { ThemeId = "post-merge-package-bump"
            Intent = "Merge work packs to the local feed and re-validates sample package pins."
            Artifacts = [ HarnessCommand "package-feed" ]
            ApplicablePatterns = [ "speckit-merge" ] }
          { ThemeId = "readiness-allowlisting"
            Intent = "Committed readiness evidence is allowlisted against the repository ignore rules."
            // `git check-ignore` was the third reference of the deleted rule. It is not repository-owned,
            // so it is not resolvable here and is not required — the ignore file is.
            Artifacts = [ RepoPath ".gitignore" ]
            ApplicablePatterns = [ "speckit-implement"; "speckit-merge"; "src/testing"; "fs-gg-project" ] } ]

    let private harnessCliPath root = Path.Combine(root, "tools", "Rendering.Harness", "Cli.fs")

    /// A dispatch arm: `| "package-feed" :: rest -> ...`. Internal arms (`__viewer`) and options
    /// (`--help`) do not match, so a skill cannot point at one.
    let private dispatchArmPattern =
        Regex(@"^\s*\|\s*""([a-z][a-z0-9-]*)""\s*::", RegexOptions.Compiled ||| RegexOptions.Multiline)

    /// The closed world for `HarnessCommand`: every verb the harness actually dispatches. Read from the
    /// dispatch table rather than a hand-kept list, so renaming a verb moves this set with it.
    let loadHarnessCommands repositoryRoot =
        let path = harnessCliPath repositoryRoot

        if not (File.Exists path) then
            None
        else
            dispatchArmPattern.Matches(File.ReadAllText path)
            |> Seq.map (fun m -> m.Groups[1].Value)
            |> Set.ofSeq
            |> Some

    /// Inline code spans only. Prose cannot point at an artifact — a skill has to write it as code, and
    /// the token it writes then has to resolve. Fence bodies and fence openers carry no single-backtick
    /// span, so they contribute nothing here.
    let private codeSpanPattern = Regex(@"`([^`\r\n]+)`", RegexOptions.Compiled)

    let private codeSpans (content: string) =
        codeSpanPattern.Matches content
        |> Seq.map (fun m -> m.Groups[1].Value)
        |> List.ofSeq

    /// A span names a command when one of its words *is* the verb (so `` `package-feed` `` and
    /// `` `harness package-feed --check` `` both point at it, and `package-feedback` does not), and names
    /// a path when the span contains it (so a full command line still points at the script it runs).
    let private spanNames (span: string) reference =
        match reference with
        | HarnessCommand verb ->
            span.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.exists (fun word -> word = verb)
        | RepoPath path -> span.Contains(path, StringComparison.Ordinal)

    let private artifactResolves root (harnessCommands: Set<string>) reference =
        match reference with
        | HarnessCommand verb -> harnessCommands |> Set.contains verb
        | RepoPath path ->
            let full = Path.Combine(root, path)
            File.Exists full || Directory.Exists full

    let private themeApplies (theme: GuardedTheme) (entry: SkillEntry) =
        let haystack = $"{entry.SkillName} {entry.Description} {entry.Path}".ToLowerInvariant()

        theme.ApplicablePatterns
        |> List.exists (fun pattern -> haystack.Contains(pattern.ToLowerInvariant()))

    /// Resolve each theme against the skills in its scope.
    ///
    /// The scan finds *candidate* references; the **verdict is resolution**. That is the whole difference
    /// from the deleted layer: `content.Contains "local feed"` was satisfiable by writing the phrase,
    /// whereas `HarnessCommand "package-feed"` is satisfiable only if the harness still dispatches that
    /// verb. Deleting the guidance leaves the theme `ArtifactUnnamed`; renaming the artifact underneath
    /// intact guidance leaves it `ArtifactDangling`. Both are the regressions the words could hide.
    let evaluateArtifactReferences
        (repositoryRoot: string)
        (harnessCommands: Set<string>)
        (themes: GuardedTheme list)
        (entries: SkillEntry list)
        =
        entries
        |> List.filter (fun entry ->
            entry.EntryKind = CanonicalEntry
            || entry.EntryKind = CommandEntry)
        |> List.collect (fun entry ->
            let spans = codeSpans entry.Content

            themes
            |> List.filter (fun theme -> themeApplies theme entry)
            |> List.map (fun theme ->
                let named =
                    theme.Artifacts
                    |> List.filter (fun reference -> spans |> List.exists (fun span -> spanNames span reference))

                let reference, status =
                    match named with
                    | [] -> None, ArtifactUnnamed
                    | candidates ->
                        // A theme offering alternatives is satisfied by any one that resolves; it is
                        // dangling only when every artifact it names has gone away.
                        match candidates |> List.tryFind (artifactResolves repositoryRoot harnessCommands) with
                        | Some resolved -> Some resolved, ArtifactResolved
                        | None -> Some(List.head candidates), ArtifactDangling

                { ThemeId = theme.ThemeId
                  Intent = theme.Intent
                  SkillName = entry.SkillName
                  SurfaceId = entry.SurfaceId
                  Path = entry.Path
                  Reference = reference
                  Expected = theme.Artifacts
                  Status = status }))

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
          ListSymbolsOnly = false
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

    /// The ADR-0019/0021 coordination kit: externally-owned (FS-GG/.github) process/command skills synced
    /// verbatim by coordination-sync. Excluded from wrapper parity (their byte-coherence is the
    /// coordination-coherence gate's job); add a kit skill here when coordination-sync introduces it.
    let private coordinationKitSkills =
        Set.ofList [ "cross-repo-coordination"; "intra-repo-parallel-work"; "check-board"; "pnext-item" ]

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
                && not ((parentDirectoryName path).StartsWith("speckit-", StringComparison.OrdinalIgnoreCase))
                // The ADR-0019 coordination kit (cross-repo-coordination + intra-repo-parallel-work per
                // ADR-0021, plus the check-board / pnext-item command skills) is externally-owned
                // (FS-GG/.github): process skills synced verbatim by coordination-sync, not repo wrappers
                // routing to an internal canonical. Their byte-coherence is enforced by the
                // coordination-coherence gate, so exclude them from wrapper parity exactly like the Ant
                // canonical and the externally-owned speckit-* command skills above.
                && not (coordinationKitSkills |> Set.contains (parentDirectoryName path)))
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
                      Symbol = None
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
                          Symbol = None
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
                          Symbol = None
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
                      Symbol = None
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
                let isProductSkill =
                    entry.Path.Contains("template/product-skills", StringComparison.OrdinalIgnoreCase)
                let exposedAsAlias = isProductSkill && names.Contains productAliasName
                // A product skill's wrapper requirement is satisfied ONLY by its fs-gg-product-* alias;
                // a bare same-named framework wrapper must not mask a missing product wrapper (Feature 223).
                let canonicalSatisfies = (not isProductSkill) && names.Contains(canonicalName)
                let antCanonicalSelfExposed = entry.SurfaceId = "ant-canonical" && surfaceId = "claude"

                if canonicalSatisfies || exposedAsAlias || antCanonicalSelfExposed then
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
                          Symbol = None
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
                          Symbol = None
                          Message = "Duplicate canonical sources with the same skill name diverge."
                          Remediation = "Choose one canonical source or document a specific variant exception."
                          ExceptionId = None })
                else
                    [])

    let private symbolFindings (symbols: ApiSymbol list) =
        symbols
        |> List.choose (fun item ->
            let finding category severity message remediation =
                Some
                    { FindingId = $"{categoryToken category}:{item.SurfaceId}:{item.SkillName}:{item.Symbol}"
                      SkillName = item.SkillName
                      SurfaceId = item.SurfaceId
                      Category = category
                      Severity = severity
                      CanonicalPath = Some item.Path
                      WrapperPath = None
                      Symbol = Some item.Symbol
                      Message = message
                      Remediation = remediation
                      ExceptionId = None }

            match item.Status with
            | Unresolved ->
                finding
                    UnresolvedApiSymbol
                    High
                    $"Skill documents `{item.Symbol}`, which is absent from the public surface baseline."
                    "Correct the skill's example, or refresh the surface baselines if the API was added."
            | Unexercised ->
                finding
                    UnexercisedApiSymbol
                    Warning
                    $"Skill documents `{item.Symbol}`, but no test calls it — the seam may be dead."
                    "Add a test that calls the documented API, or stop documenting it."
            | Exercised -> None)

    let private artifactFindings (references: ArtifactReference list) =
        references
        |> List.choose (fun item ->
            let finding category severity message remediation =
                Some
                    { FindingId = $"{categoryToken category}:{item.SurfaceId}:{item.SkillName}:{item.ThemeId}"
                      SkillName = item.SkillName
                      SurfaceId = item.SurfaceId
                      Category = category
                      Severity = severity
                      CanonicalPath = Some item.Path
                      WrapperPath = None
                      Symbol = item.Reference |> Option.map artifactRefToken
                      Message = message
                      Remediation = remediation
                      ExceptionId = None }

            let expected =
                item.Expected
                |> List.map (fun reference -> $"`{artifactRefToken reference}`")
                |> String.concat " or "

            match item.Status with
            | ArtifactResolved -> None
            | ArtifactDangling ->
                let named = item.Reference |> Option.map artifactRefToken |> Option.defaultValue ""

                finding
                    UnresolvedArtifactReference
                    High
                    $"Skill's `{item.ThemeId}` guidance points at `{named}`, which no longer exists."
                    "Point the guidance at the artifact's current name, or restore the artifact."
            | ArtifactUnnamed ->
                finding
                    MissingRequiredArtifact
                    High
                    $"Skill is in scope for `{item.ThemeId}` but names none of its artifacts ({expected})."
                    $"Restore the guidance — {item.Intent} — naming {expected}; or narrow the theme's scope.")

    /// Issue #475 / #465: assert wrapper coverage against the MANIFEST, not against what the
    /// filesystem scan happened to discover.
    ///
    /// `missingWrapperFindings` above can only speak about entries the surface scan found, and
    /// `requiresWrapper` decides whether an entry needs a wrapper by testing whether its PATH contains
    /// `template/product-skills`. Both are properties of where a file happens to sit, and neither is
    /// the question we actually care about, which is: *does this skill ship into a generated
    /// workspace?* The manifest answers that directly — `scope: product` — and two skills answer it
    /// "yes" while living somewhere else entirely:
    ///
    ///     fs-gg-feedback-report   scope=product  materializes-when=always  supplied-by=template/feedback-report/skill/
    ///     fs-gg-feedback-capture  scope=product                            supplied-by=template/feedback/skill/
    ///
    /// So both were exempt from the wrapper requirement and absent from the report entirely. That is
    /// how #465 happened: `fs-gg-feedback-report` materialized into every workspace with no wrapper in
    /// this repo, and `MissingWrapper` — the diagnostic that exists to catch exactly that — never fired.
    /// A check whose subject list is assembled by a path glob cannot report on a skill the glob misses,
    /// and it will report `passed` while doing so.
    ///
    /// This closes it from the other end: every `scope: product` row in the manifest must have an
    /// activation wrapper under BOTH orchestrator roots, or it is a finding — whatever the scan saw.
    /// It is deliberately manifest-driven and filesystem-checked, so it stays true for a skill supplied
    /// from a directory nobody has thought of yet.
    ///
    /// Either wrapper name satisfies it: the `fs-gg-product-*` alias (the `template/product-skills`
    /// convention) or the canonical id (what the two feedback skills use). Feature 223's alias-only rule
    /// exists so a same-named FRAMEWORK wrapper cannot mask a missing PRODUCT wrapper; both feedback
    /// wrappers were read and do route to their product canonical, so there is nothing being masked.
    /// The naming inconsistency itself is real but is not this check's business — filed separately.
    let private manifestCoverageFindings (request: ParityCheckRequest) =
        // Fixture mode uses synthetic trees with no manifest; it is not real repository parity evidence.
        if request.FixtureMode.IsSome then
            []
        else

        let manifestPath =
            Path.Combine(request.RepositoryRoot, "template", "skill-manifest", "skill-manifest.json")

        if not (File.Exists manifestPath) then
            [ { FindingId = findingId MissingWrapper "manifest" "skill-manifest"
                SkillName = "skill-manifest"
                SurfaceId = "manifest"
                Category = MissingWrapper
                Severity = High
                CanonicalPath = Some "template/skill-manifest/skill-manifest.json"
                WrapperPath = None
                Symbol = None
                Message = "The skill manifest is missing, so product-skill wrapper coverage cannot be asserted."
                Remediation = "Restore template/skill-manifest/skill-manifest.json (dotnet fsi scripts/generate-skill-manifest.fsx)."
                ExceptionId = None } ]
        else

        let productIds =
            try
                use doc = JsonDocument.Parse(File.ReadAllText manifestPath)
                let skills =
                    match doc.RootElement.ValueKind with
                    | JsonValueKind.Array -> doc.RootElement.EnumerateArray() |> Seq.toList
                    | _ ->
                        match doc.RootElement.TryGetProperty "skills" with
                        | true, arr when arr.ValueKind = JsonValueKind.Array -> arr.EnumerateArray() |> Seq.toList
                        | _ -> []

                skills
                |> List.choose (fun s ->
                    let prop name =
                        match s.TryGetProperty(name: string) with
                        | true, v when v.ValueKind = JsonValueKind.String -> Option.ofObj (v.GetString())
                        | _ -> None

                    match prop "id", prop "scope" with
                    | Some id, Some "product" -> Some id
                    | _ -> None)
            with _ ->
                []

        // Both orchestrator roots are required: a skill activated for one agent and not the other is
        // half-shipped, and that asymmetry is precisely what the parity report exists to surface.
        //
        // These are the REAL surface ids from `supportedSurfaces` — `codex-local` is rooted at
        // `.agents/skills` (there is no `.codex/` in this repo), not at anything called "agents". Reusing
        // them (rather than inventing a surface name) keeps two invariants: the finding names a surface
        // that appears in the report's Supported Surfaces table, and it shares a FindingId with the
        // scan-driven `missingWrapperFindings` above — so when both fire for the same skill+surface, the
        // `List.distinctBy` in `classifyFindings` collapses them to one finding instead of reporting the
        // same missing wrapper twice under two different names.
        let roots = [ "claude", ".claude"; "codex-local", ".agents" ]

        productIds
        |> List.collect (fun id ->
            let alias = id.Replace("fs-gg-", "fs-gg-product-")

            roots
            |> List.choose (fun (surfaceId, root) ->
                let present name =
                    File.Exists(Path.Combine(request.RepositoryRoot, root, "skills", name, "SKILL.md"))

                if present alias || present id then
                    None
                else
                    Some
                        { FindingId = findingId MissingWrapper surfaceId id
                          SkillName = id
                          SurfaceId = surfaceId
                          Category = MissingWrapper
                          Severity = High
                          CanonicalPath = Some "template/skill-manifest/skill-manifest.json"
                          WrapperPath = None
                          Symbol = None
                          Message =
                            $"Manifest declares `{id}` as a product skill (it materializes into generated workspaces), but no activation wrapper exists under `{root}/skills/`."
                          Remediation =
                            $"Add `{root}/skills/{alias}/SKILL.md` (or `{root}/skills/{id}/SKILL.md`) routing to the canonical body, or drop the skill's `scope: product` in the manifest."
                          ExceptionId = None }))

    /// The ONLY authors whose `metadata.source` is upstream provenance rather than a citation of this repo.
    ///
    /// A CLOSED ALLOW-LIST, and deliberately the inverse of the obvious design. Keying the rule on "is the author
    /// FS.GG?" and exempting everyone else looks equivalent and is not: it fails OPEN. This repo's own skills are
    /// authored under more than one name — `FS.GG` and `fs-gg-ui` — and #466's actual file
    /// (template/feedback/skill/SKILL.md) is an `fs-gg-ui` one, so an FS.GG-only rule would have exempted the very
    /// skill this guard exists to catch, and reported green while doing it.
    ///
    /// Listing the VENDORED authors instead makes the default enforcement: a new author name, or a typo in an old
    /// one, is CHECKED rather than silently excused. That is the same argument this file already makes for a
    /// missing `author:` below — an exemption must be entered deliberately, never by omission or by accident.
    let private vendoredAuthors = set [ "github-spec-kit" ]

    /// Does `resolved` actually live inside this repository?
    ///
    /// `absolutePath` honours a rooted path and resolves `..`, so without this a `source:` of `/etc/passwd` — or
    /// `../<some-other-checkout>/specs/…` — resolves to something that EXISTS and the citation passes. That is
    /// #466's shape exactly: a pointer carried in from a different repo that happens to resolve on the author's
    /// disk. The repo root itself is not "inside" it: a `source:` of `.` or `..` cites nothing.
    let private withinRepository (root: string) (resolved: string) =
        let root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)
        let resolved = Path.GetFullPath resolved

        resolved.StartsWith(root + string Path.DirectorySeparatorChar, StringComparison.Ordinal)

    /// #529 (from #466) — a skill that defers its authority to a document nobody can open has no authority.
    ///
    /// #466 was a `template/**` SKILL.md whose `metadata.source` named a spec path that had NEVER existed in this
    /// repo (it was imported wholesale, carrying the pre-migration repo's pointer with it). Its FR-014/FR-015/FR-016
    /// citations then landed on *unrelated* features holding those same numbers — worse than a dead link, because
    /// it resolves to something plausible and wrong. Nothing checked the citation, so it stayed wrong for as long
    /// as nobody tried to follow it. This is the check.
    ///
    /// EXEMPTION IS THE HARD PART, and it is inverted on purpose — see `vendoredAuthors`. Thirty vendored spec-kit
    /// skills declare `metadata.source` as UPSTREAM PROVENANCE — a path inside the github/spec-kit repo
    /// (`templates/commands/analyze.md`), or an `<extension>:` scheme — which cannot resolve here by construction.
    /// Failing the gate on those would be a red nobody can clear: they are synced from upstream, so an edit here is
    /// reverted by the next sync. That is the same reason a generated artifact is never reserved
    /// (FS-GG/.github#309) — a gate that fires on content you do not own is a gate that gets switched off.
    ///
    /// So exactly two things earn an exemption, and both must be DECLARED:
    ///   * a `metadata.author` on the vendored allow-list, or
    ///   * nothing else. A missing `author:` is a finding in its own right, because otherwise the cheapest way to
    ///     silence this rule is to delete a line — which is precisely the silent drift it exists to stop.
    let private metadataSourceFindings (root: string) (entries: SkillEntry list) =
        entries
        |> List.choose (fun entry ->
            let declared key =
                entry.Metadata
                |> Map.tryFind key
                |> Option.map (fun value -> value.Trim())
                |> Option.filter (fun value -> value <> "")

            // Two entries can share a SurfaceId AND a SkillName (the spec-kit surface concatenates the .agents and
            // .claude trees), and `classifyFindings` dedupes by FindingId — so without the path, two broken files
            // collapse into one finding and the fixer only ever sees one of them. `CanonicalDrift` disambiguates
            // the same way, for the same reason.
            let identity =
                findingId UnresolvedMetadataSource entry.SurfaceId entry.SkillName
                + ":"
                + entry.Path.Replace("/", "-")

            let finding message remediation =
                Some
                    { FindingId = identity
                      SkillName = entry.SkillName
                      SurfaceId = entry.SurfaceId
                      Category = UnresolvedMetadataSource
                      Severity = High
                      CanonicalPath = Some entry.Path
                      WrapperPath = None
                      Symbol = None
                      Message = message
                      Remediation = remediation
                      ExceptionId = None }

            match declared "source" with
            | None -> None
            | Some source ->
                match declared "author" with
                | None ->
                    finding
                        $"`{entry.Path}` declares `metadata.source: {source}` but no `metadata.author`, so nothing says whether that path is a citation of THIS repo (which must resolve) or upstream provenance (which cannot)."
                        "Declare `metadata.author`. A skill authored here is then held to a `source` that resolves; only the vendored authors are exempt, and the exemption must be claimed, not inherited by leaving the field out."
                | Some author when vendoredAuthors.Contains(normalizeText author) ->
                    // Vendored. `source` is provenance in the upstream repo, not a path in this one.
                    None
                | Some _ ->
                    let resolved = absolutePath root source

                    let exists = File.Exists resolved || Directory.Exists resolved

                    if exists && withinRepository root resolved then
                        None
                    elif exists then
                        finding
                            $"`{entry.Path}` declares `metadata.source: {source}`, which resolves OUTSIDE this repository (to `{resolved}`). It exists on this machine and nowhere else — which is exactly how #466's pointer survived: it was carried in from another repo and read as though it had been checked."
                            $"Point `metadata.source` at a path inside this repository, or remove it. `{source}` cites a document no other checkout of this repo can open."
                    else
                        finding
                            $"`{entry.Path}` declares `metadata.source: {source}`, which does not resolve to any file or directory in this repository. The skill defers its authority to a document nobody can open, and any FR-nnn it cites now hangs on a number that may resolve to an unrelated feature (#466)."
                            $"Point `metadata.source` at the path that actually backs this skill, or remove it. Do not leave it naming `{source}` — a citation that cannot be followed is worse than none, because it reads as though it were checked.")

    let private classifyFindings request entries symbols artifacts =
        wrapperFindings entries
        @ missingWrapperFindings entries
        @ canonicalDriftFindings request entries
        @ symbolFindings symbols
        @ artifactFindings artifacts
        @ manifestCoverageFindings request
        @ metadataSourceFindings request.RepositoryRoot entries
        |> List.distinctBy (fun finding -> finding.FindingId)

    let private severityCounts findings =
        { Critical = findings |> List.filter (fun f -> f.Severity = Critical) |> List.length
          High = findings |> List.filter (fun f -> f.Severity = High) |> List.length
          Warning = findings |> List.filter (fun f -> f.Severity = Warning) |> List.length
          Info = findings |> List.filter (fun f -> f.Severity = Info) |> List.length }

    let private symbolSummary (symbols: ApiSymbol list) =
        symbols
        |> List.groupBy (fun symbol -> symbol.SkillName)
        |> List.map (fun (skillName, items) ->
            let counts = items |> List.countBy (fun item -> item.Status) |> Map.ofList
            let countOf status = counts |> Map.tryFind status |> Option.defaultValue 0

            { SkillName = skillName
              Documented = items.Length
              Exercised = countOf Exercised
              Unexercised = countOf Unexercised
              Unresolved = countOf Unresolved })
        |> List.sortBy (fun summary -> summary.SkillName)

    /// Every theme appears, including one with no skill in scope — a theme that silently stopped applying
    /// to anything would otherwise read as a theme that passed.
    let private themeSummary (themes: GuardedTheme list) (references: ArtifactReference list) =
        themes
        |> List.map (fun theme ->
            let items = references |> List.filter (fun item -> item.ThemeId = theme.ThemeId)
            let countOf status = items |> List.filter (fun item -> item.Status = status) |> List.length

            { ThemeId = theme.ThemeId
              Scoped = items.Length
              Resolved = countOf ArtifactResolved
              Dangling = countOf ArtifactDangling
              Unnamed = countOf ArtifactUnnamed })
        |> List.sortBy (fun summary -> summary.ThemeId)

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

        // The report is committed, so the regenerate line must not bake in the absolute path of
        // whichever checkout produced it — otherwise every worktree rewrites the file.
        let path = relativePath request.RepositoryRoot

        $"dotnet fsi scripts/check-agent-skill-parity.fsx --out {path request.OutDir} --report {path request.ReportPath} --summary-json {path request.SummaryJsonPath}{fixture} --fail-on {severityToken request.FailOnSeverity}"

    let private buildReport
        (request: ParityCheckRequest)
        (surfaces: SkillSurface list)
        (entries: SkillEntry list)
        (symbols: ApiSymbol list)
        (symbolCaveats: string list)
        (artifacts: ArtifactReference list)
        (artifactCaveats: string list)
        (findings: ParityFinding list)
        =
        let counts = severityCounts findings

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
          ApiSymbolCoverage = symbolSummary symbols
          GuardedThemeCoverage = themeSummary (defaultGuardedThemes ()) artifacts
          Findings = findings
          IntentionalExceptions = []
          GeneratedReportPath = request.ReportPath
          StructuredSummaryPath = request.SummaryJsonPath
          Caveats =
            [ "Global Codex skill installation paths are excluded from required repository parity."
              if request.FixtureMode.IsSome then
                  "Fixture mode uses synthetic skill files and is not real repository parity evidence."
              yield! symbolCaveats
              yield! artifactCaveats ]
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

        // The symbol layer needs both of its inputs to run at all, so every fixture gets a synthetic
        // surface baseline and a synthetic test corpus. `Widget.render` is public and exercised,
        // `Widget.hidden` is public and untested, and `Widget.missing` does not exist.
        let baselinePath = full [ "readiness"; "surface-baselines"; "members"; "FS.GG.Fixture.txt" ]
        ensureParent baselinePath

        File.WriteAllText(
            baselinePath,
            "FS.GG.Fixture.Widget.render(System.String) : System.String\n"
            + "FS.GG.Fixture.Widget.hidden(System.String) : System.String\n"
        )

        let testPath = full [ "tests"; "FixtureTests.fs" ]
        ensureParent testPath
        File.WriteAllText(testPath, "module FixtureTests\n\nlet exercised = Widget.render \"fixture\"\n")

        let documents symbol =
            $"Synthetic fixture body.\n\n```fsharp\nlet value = {symbol} \"fixture\"\n```\n"

        let alignedBody = documents "Widget.render"

        if includeCase "passing" then
            createSkillFile (full [ "canonical"; "passing"; "SKILL.md" ]) "fs-gg-fixture-passing" "Aligned fixture skill." alignedBody
            createWrapper (full [ "codex"; "passing"; "SKILL.md" ]) "fs-gg-fixture-passing" "Aligned fixture skill." "../../canonical/passing/SKILL.md"
            createWrapper (full [ "claude"; "passing"; "SKILL.md" ]) "fs-gg-fixture-passing" "Aligned fixture skill." "../../canonical/passing/SKILL.md"

        if includeCase "missing-wrapper" then
            createSkillFile (full [ "canonical"; "missing-wrapper"; "SKILL.md" ]) "fs-gg-fixture-missing" "Missing wrapper fixture." alignedBody

        if includeCase "wrapper-only" then
            createSkillFile (full [ "codex"; "wrapper-only"; "SKILL.md" ]) "fs-gg-fixture-wrapper-only" "Wrapper only fixture." "No canonical route."

        if includeCase "stale-description" then
            createSkillFile (full [ "canonical"; "stale-description"; "SKILL.md" ]) "fs-gg-fixture-stale" "Current canonical description." alignedBody
            createWrapper (full [ "codex"; "stale-description"; "SKILL.md" ]) "fs-gg-fixture-stale" "Old wrapper description." "../../canonical/stale-description/SKILL.md"

        if includeCase "broken-target" then
            createWrapper (full [ "codex"; "broken-target"; "SKILL.md" ]) "fs-gg-fixture-broken" "Broken target fixture." "../../canonical/does-not-exist/SKILL.md"

        if includeCase "canonical-drift" then
            createSkillFile (full [ "canonical"; "drift-a"; "SKILL.md" ]) "fs-gg-fixture-drift" "Canonical description A." alignedBody
            createSkillFile (full [ "canonical"; "drift-b"; "SKILL.md" ]) "fs-gg-fixture-drift" "Canonical description B." alignedBody

        if includeCase "unresolved-api-symbol" then
            createSkillFile
                (full [ "canonical"; "unresolved-api-symbol"; "SKILL.md" ])
                "fs-gg-fixture-unresolved"
                "Documents an API that does not exist."
                (documents "Widget.missing")

        if includeCase "unexercised-api-symbol" then
            createSkillFile
                (full [ "canonical"; "unexercised-api-symbol"; "SKILL.md" ])
                "fs-gg-fixture-unexercised"
                "Documents a public API that no test exercises."
                (documents "Widget.hidden")

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

    /// Materializes the fixture tree when one is requested, so every caller sees the same world.
    let private effectiveRequestFor request =
        match request.FixtureMode with
        | Some fixtureName ->
            let fixtureRoot = Path.Combine(request.OutDir, "_skill-parity-fixture")
            createFixture fixtureRoot fixtureName
            { request with RepositoryRoot = fixtureRoot }
        | None -> { request with RepositoryRoot = Path.GetFullPath request.RepositoryRoot }

    /// Both inputs are required to say anything honest about a documented symbol: the baseline decides
    /// whether it exists, the test corpus whether anything exercises it. Missing either, the layer
    /// stays silent and says so rather than reporting a green it did not earn.
    /// Canonical/command skills that show F# examples but name no public API in them. Their examples
    /// are product-local, so the symbol layer judged nothing — say so, rather than let an unchecked
    /// skill look like a clean one by its absence from the coverage table.
    let private unjudgedSkills entries (symbols: ApiSymbol list) =
        let judged = symbols |> List.map (fun symbol -> symbol.SkillName) |> Set.ofList

        entries
        |> List.filter (fun entry ->
            (entry.EntryKind = CanonicalEntry || entry.EntryKind = CommandEntry)
            && not (fsharpFences entry.Content).IsEmpty)
        |> List.map (fun entry -> entry.SkillName)
        |> List.distinct
        |> List.filter (fun name -> not (judged.Contains name))
        |> List.sort

    let private resolveSymbols effectiveRequest entries =
        let root = effectiveRequest.RepositoryRoot

        match loadSurfaceMembers root, loadExercisedSymbols root with
        | Some surfaceMembers, Some exercised ->
            let symbols = evaluateApiSymbols surfaceMembers exercised entries

            let caveats =
                match unjudgedSkills entries symbols with
                | [] -> []
                | skills ->
                    [ $"""{skills.Length} skill(s) show F# examples that name no public API symbol, so none was judged: {String.concat ", " skills}.""" ]

            Ok(symbols, caveats)
        | surfaceMembers, exercised ->
            let missing =
                [ if surfaceMembers.IsNone then surfaceBaselineDir root
                  if exercised.IsNone then testSourceDir root ]
                |> List.map (relativePath root)
                |> String.concat " and "

            Error $"API symbol resolution skipped: {missing} not found — documented APIs were not checked."

    /// The dispatch table is the only closed world this layer needs. Without it a `HarnessCommand` cannot
    /// be told from a typo, so the layer stays silent and says so rather than reporting a green it did not
    /// earn. Fixture roots have no harness, and degrade here by design.
    let private resolveArtifacts effectiveRequest entries =
        let root = effectiveRequest.RepositoryRoot

        match loadHarnessCommands root with
        | Some harnessCommands -> Ok(evaluateArtifactReferences root harnessCommands (defaultGuardedThemes ()) entries)
        | None ->
            Error
                $"Guarded-theme resolution skipped: {relativePath root (harnessCliPath root)} not found — process guidance was not checked."

    let runCheck request =
        let effectiveRequest = effectiveRequestFor request
        let surfaces = effectiveSurfaces effectiveRequest effectiveRequest.RepositoryRoot
        let entries = inventorySkills effectiveRequest surfaces

        let symbols, symbolCaveats =
            match resolveSymbols effectiveRequest entries with
            | Ok (symbols, caveats) -> symbols, caveats
            | Error reason -> [], [ reason ]

        let artifacts, artifactCaveats =
            match resolveArtifacts effectiveRequest entries with
            | Ok artifacts -> artifacts, []
            | Error reason -> [], [ reason ]

        let findings = classifyFindings effectiveRequest entries symbols artifacts
        buildReport effectiveRequest surfaces entries symbols symbolCaveats artifacts artifactCaveats findings

    let private markdownTableRow (values: string list) =
        "| " + (values |> List.map (fun value -> value.Replace("\n", " ")) |> String.concat " | ") + " |"

    let renderMarkdown report =
        let sb = StringBuilder()

        sb.AppendLine("# Skill Parity Report") |> ignore
        sb.AppendLine() |> ignore
        // Issue #475: NO wall-clock timestamp here. This report is COMMITTED, and the gate that keeps it
        // honest regenerates it and fails on any diff — which a `Checked at UTC` line makes impossible,
        // because it changes on every run and so every run diffs. The committed markdown must be a pure
        // function of the repo, exactly as the `Regenerate` line below already is (it deliberately emits a
        // repo-relative path so a checkout's location does not rewrite the file).
        //
        // Losing the line loses nothing. It was never evidence of freshness — it was an *assertion* of it,
        // written by the run that produced the file and true only of that run. On main it read
        // `2026-07-10T16:25:25Z` while the report was a day stale and wrong (a whole skill had dropped out
        // of the table), which is worse than no timestamp: a reader who checks the date is reassured by it.
        // Freshness is now a property the gate enforces on every PR, not a claim the artifact makes about
        // itself. The real timestamp still goes to the JSON summary, which is a per-run CI artifact and the
        // right place for it.
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
        sb.AppendLine("## API Symbol Coverage") |> ignore

        match report.ApiSymbolCoverage with
        | [] -> sb.AppendLine("No skill documents an API symbol from the public surface baseline.") |> ignore
        | coverage ->
            sb.AppendLine(markdownTableRow [ "Skill"; "Documented"; "Exercised"; "Unexercised"; "Unresolved" ]) |> ignore
            sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---"; "---" ]) |> ignore

            for summary in coverage do
                sb.AppendLine(
                    markdownTableRow
                        [ summary.SkillName
                          string summary.Documented
                          string summary.Exercised
                          string summary.Unexercised
                          string summary.Unresolved ]
                )
                |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine("## Guarded Theme Coverage") |> ignore

        match report.GuardedThemeCoverage with
        | [] -> sb.AppendLine("Guarded themes were not resolved — see caveats.") |> ignore
        | coverage ->
            sb.AppendLine(markdownTableRow [ "Theme"; "Scoped"; "Resolved"; "Dangling"; "Unnamed" ]) |> ignore
            sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---"; "---" ]) |> ignore

            for summary in coverage do
                sb.AppendLine(
                    markdownTableRow
                        [ summary.ThemeId
                          string summary.Scoped
                          string summary.Resolved
                          string summary.Dangling
                          string summary.Unnamed ]
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
            report.ApiSymbolCoverage
            |> List.map (fun item ->
                {| skillName = item.SkillName
                   documented = item.Documented
                   exercised = item.Exercised
                   unexercised = item.Unexercised
                   unresolved = item.Unresolved |})

        let themeCoverage =
            report.GuardedThemeCoverage
            |> List.map (fun item ->
                {| themeId = item.ThemeId
                   scoped = item.Scoped
                   resolved = item.Resolved
                   dangling = item.Dangling
                   unnamed = item.Unnamed |})

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
                   symbol = nullable finding.Symbol
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
               apiSymbolCoverage = coverage
               guardedThemeCoverage = themeCoverage
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
        sb.AppendLine("# Skill API Symbol Coverage") |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine($"Overall status: `{overallStatusToken report.OverallStatus}`") |> ignore
        sb.AppendLine() |> ignore

        sb.AppendLine(
            "Each `Module.member` a skill documents in an F# code fence is resolved against the "
            + "member-granular public surface baseline, then against the test corpus. A symbol whose "
            + "module is known but whose member is absent is a `high` finding; a symbol no test calls "
            + "is a `warning`. Symbols from modules outside the baseline are product-local and not judged."
        )
        |> ignore

        sb.AppendLine() |> ignore
        sb.AppendLine(markdownTableRow [ "Skill"; "Documented"; "Exercised"; "Unexercised"; "Unresolved" ]) |> ignore
        sb.AppendLine(markdownTableRow [ "---"; "---"; "---"; "---"; "---" ]) |> ignore

        for summary in report.ApiSymbolCoverage do
            sb.AppendLine(
                markdownTableRow
                    [ summary.SkillName
                      string summary.Documented
                      string summary.Exercised
                      string summary.Unexercised
                      string summary.Unresolved ]
            )
            |> ignore

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
                let path = Path.Combine(request.OutDir, "..", "api-symbol-coverage.md") |> Path.GetFullPath
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
          Symbols = []
          Artifacts = []
          Report = None
          Diagnostics = [] },
        [ ReadSkillSurfaces ]

    let update msg model =
        match msg with
        | InventoryRequested -> model, [ ReadSkillSurfaces ]
        | InventoryLoaded (surfaces, entries) -> { model with Surfaces = surfaces; Entries = entries }, [ ResolveApiSymbols ]
        | SymbolsResolved symbols -> { model with Symbols = symbols }, [ ResolveArtifactReferences ]
        | ArtifactsResolved artifacts -> { model with Artifacts = artifacts }, [ ClassifyFindings ]
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
            ListSymbolsOnly = hasFlag "--list-symbols" args
            JsonOutput = hasFlag "--json" args }

    /// Same pipeline as `runCheck`, minus the report — so `--list-symbols` and the report can never
    /// disagree about what a symbol resolves to, and `--fixture` materializes here too.
    let private printSymbols request =
        let effectiveRequest = effectiveRequestFor request
        let surfaces = effectiveSurfaces effectiveRequest effectiveRequest.RepositoryRoot
        let entries = inventorySkills effectiveRequest surfaces

        match resolveSymbols effectiveRequest entries with
        | Error reason ->
            eprintfn "skill-parity: %s" reason
            1
        | Ok (symbols, caveats) ->
            symbols
            |> List.iter (fun symbol ->
                printfn "%s\t%s\t%s" symbol.Symbol (symbolStatusToken symbol.Status) symbol.SkillName)

            caveats |> List.iter (eprintfn "skill-parity: %s")
            0

    let private knownFlags =
        set [ "--repo"
              "--out"
              "--report"
              "--summary-json"
              "--fixture"
              "--surface"
              "--allow-exception"
              "--fail-on"
              "--list-symbols"
              "--json" ]

    let runCli argv =
        // `--list-rules` was removed with the guidance layer. Silently ignoring it would run a full
        // check and rewrite the committed report — so an unrecognized option is a configuration error.
        let unknown =
            argv
            |> List.filter (fun (arg: string) ->
                arg.StartsWith("--", StringComparison.Ordinal)
                && not (knownFlags.Contains arg))

        if not unknown.IsEmpty then
            eprintfn "skill-parity: unknown option(s): %s" (String.concat " " unknown)
            2
        else

        let request = requestFromArgs argv

        if request.ListSymbolsOnly then
            printSymbols request
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
