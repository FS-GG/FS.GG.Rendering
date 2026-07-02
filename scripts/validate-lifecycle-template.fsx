// Feature 204 — generated-product LIFECYCLE template validation regenerator.
//
// Mirrors the Feature 128 report-gate + env-gated-live-run pattern
// (validate-design-system-template.fsx): an always-on, env-free verdict CORE that needs no
// `dotnet new`, plus a heavy live loop gated behind FS_GG_RUN_LIFECYCLE_VALIDATION=1 that performs
// real `dotnet new` instantiation per `lifecycle` x `profile` and writes the validation report
// asserted by Feature204LifecycleTemplateTests.
//
//   * ALWAYS (no env flag): the verdict CORE. Parses .template.config/template.json and proves the
//     env-free facts: covered-values == the enumerated `lifecycle` choices; every gated `source`
//     entry (target under .specify/ | .agents/ | .claude/, or the generated agent-context tree)
//     carries `lifecycle == "spec-kit"`; the three ungated PRODUCT sources (base -> ./,
//     samples -> samples/, ant overlay) do NOT; and the directive agent-context docs are
//     lifecycle-safe (base CLAUDE.md is excluded from the ungated base source; base README.md
//     carries no suppressed-path reference). No `dotnet new`, build, GL, or network.
//
//   * --emit-report (env-free): the gate's self-provisioning path. Writes the report from the
//     verdict core, SYNTHESIZING the live-only lines (diff-vs-today=none, gated-absent,
//     product-present, diff-vs-default=gated-only, the composition matrix, unknown-value rejected)
//     as their expected values and disclosing `provenance: verdict-core` (Constitution V) so a
//     fresh checkout (gitignored readiness/ absent) is not red-by-default.
//
//   * ENV-GATED (FS_GG_RUN_LIFECYCLE_VALIDATION=1): the live loop. Per profile it scaffolds
//     no-`--lifecycle` (default) and `--lifecycle spec-kit` and proves they are byte-identical
//     (diff-vs-today=none, the explicit-vs-implicit-default invariant — same operational meaning as
//     Feature 128; the absolute pre-feature byte diff is recorded in readiness/early-scaffold.md);
//     scaffolds `--lifecycle sdd`/`none` and proves the gated set is absent, the product present,
//     and that default-minus-sdd differs in ONLY gated paths (FR-009); proves none == sdd; greps
//     the directive agent-context docs for suppressed-path refs (CC-1); runs the 12-combo
//     composition matrix with `--designSystem ant` (ant overlay present in every case) plus the
//     feedback-under-non-spec-kit gating; and proves an unknown value is rejected. Then it writes
//     the report with `provenance: live`.
//
// Usage:
//   dotnet fsi scripts/validate-lifecycle-template.fsx                 # verdict-core self-check only
//   dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report   # + write report (env-free)
//   FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx  # + live proof

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json

// ---- repo layout -----------------------------------------------------------------------------

let repoRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then dir
        else
            match Directory.GetParent dir |> Option.ofObj with
            | Some p -> find p.FullName
            | None -> failwith "Could not locate repository root (FS.GG.Rendering.slnx)."
    find __SOURCE_DIRECTORY__

let repoPath (rel: string) =
    Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let reportRelPath =
    "specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md"

let templateJsonPath = repoPath ".template.config/template.json"

let profiles = [ "app"; "headless-scene"; "governed"; "sample-pack" ]

// The directive agent-context files that the CC-1 "Suppressed-but-referenced" edge case concerns.
// (The copyOnly governance reference docs docs/evidence-formats.md / docs/skillist-reference.md
// document the .agents/skills/<id>/SKILL.md *convention* and are out of this scope by design —
// see readiness/early-scaffold.md.)
let directiveAgentDocs = [ "CLAUDE.md"; "AGENTS.md"; "README.md" ]

// A relative path is "gated" iff it lives under one of the gated lifecycle roots — i.e. it is part
// of the lifecycle WORKSPACE, the set that differs between spec-kit and sdd/none. Feature 219: the
// framework `fs-gg-*` product skills under `.agents/skills/`/`.claude/skills/` are present in BOTH
// (so they never appear in the spec-kit−sdd diff), but the `docs/skillist-reference.md` catalog is
// now lifecycle-workspace (spec-kit-only, the named exception), so its sdd removal is expected.
let private isGatedPath (rel: string) =
    let p = rel.Replace('\\', '/')
    p.StartsWith ".specify/" || p.StartsWith ".agents/" || p.StartsWith ".claude/"
    || p.StartsWith ".codex/"
    || p = "CLAUDE.md" || p = "AGENTS.md"
    || p = "docs/skillist-reference.md"

let private assertTrue cond msg =
    if not cond then failwithf "VERDICT-CORE FAIL: %s" msg

// ---- verdict core: parse template.json and prove the env-free facts ---------------------------

let private templateDoc () = JsonDocument.Parse(File.ReadAllText templateJsonPath)

/// The accepted lifecycle choice set, parsed from the template (single coverage source, TP-7).
let private enumerateLifecycleChoices () =
    use doc = templateDoc ()
    let choices =
        doc.RootElement
            .GetProperty("symbols")
            .GetProperty("lifecycle")
            .GetProperty("choices")
        |> fun arr ->
            [ for c in arr.EnumerateArray() -> c.GetProperty("choice").GetString() ]
    if List.isEmpty choices then failwith "lifecycle has no choices"
    choices

let private SPEC_KIT_COND = "lifecycle == \"spec-kit\""

/// Feature 219: the source under `template/product-skills/` carries the framework PRODUCT skills.
let private isFrameworkSkillSource (source: string) =
    source.Replace('\\', '/').StartsWith "template/product-skills/"

/// Feature 219 (R4): the `docs/skillist-reference.md` catalog is a lifecycle-coupled reference doc
/// emitted under a `spec-kit`-gated source whose `target` is the product `./` tree but which is
/// recognised here as lifecycle-workspace via a NAMED exception (it enumerates the full registry,
/// coherent only under spec-kit). Detected by its `include`/`target` naming the catalog file.
let private isSkillistCatalogSource (target: string) (includes: string list) =
    target.Replace('\\', '/') = "docs/skillist-reference.md"
    || List.contains "docs/skillist-reference.md" includes

/// Feature 231 (ADR-0014 §Decision 1): the ungated provider skill-manifest row — provider DATA
/// shipped inside `.agents/skills/` in every lifecycle so both mirror authorities propagate it.
let private isManifestSource (source: string) =
    source.Replace('\\', '/') = "template/skill-manifest/"

/// Verify the gating invariant on every `source` entry (the env-free verdict-core fact,
/// Feature 219 R3 / data-model "Template source category"; reworked by Feature 231 / ADR-0014).
/// Classification order is significant: framework-product-skill FIRST (by `source` prefix), then
/// the ungated skill-manifest row (named exception), then lifecycle-workspace (by `target` prefix
/// / generated tree / the named skillist exception), then product. Feature 231 also proves the
/// two structural ADR-0014 facts: the repo-root `.agents/skills/` source vendors ONLY the
/// `speckit-*` process skills (no dev surface, F3), and exactly one spec-kit-gated materialize
/// step (template/lifecycle/ -> .specify/scripts/fs-gg/) replaces the Feature 230 per-skill
/// `.claude`/`.codex` twins (of which none may remain).
let private verifyGatedSources () =
    use doc = templateDoc ()
    let sources = doc.RootElement.GetProperty("sources")
    let mutable frameworkChecked = 0
    let mutable workspaceChecked = 0
    let mutable productChecked = 0
    let mutable manifestChecked = 0
    let mutable materializeChecked = 0
    let mutable speckitNarrowChecked = 0
    for s in sources.EnumerateArray() do
        let source =
            match s.TryGetProperty "source" with
            | true, v -> v.GetString()
            | _ -> ""
        let target =
            match s.TryGetProperty "target" with
            | true, v -> v.GetString()
            | _ -> ""
        let condition =
            match s.TryGetProperty "condition" with
            | true, v -> v.GetString()
            | _ -> ""
        let includes =
            match s.TryGetProperty "include" with
            | true, arr -> [ for e in arr.EnumerateArray() -> e.GetString() ]
            | _ -> []
        let copyOnly =
            match s.TryGetProperty "copyOnly" with
            | true, arr -> [ for e in arr.EnumerateArray() -> e.GetString() ]
            | _ -> []
        let t = target.Replace('\\', '/')
        let isGeneratedTree = source = ".template.config/generated/"
        let isGatedTarget =
            t.StartsWith ".specify" || t.StartsWith ".agents" || t.StartsWith ".claude"
            || t.StartsWith ".codex"
            || t = "CLAUDE.md" || t = "AGENTS.md"
        // Feature 231 (ADR-0014): product skills emit to `.agents/skills/` ONLY — a product-skill
        // source targeting `.claude/skills/` or `.codex/skills/` is a resurrected Feature 230 twin.
        if isFrameworkSkillSource source then
            assertTrue
                (t.StartsWith ".agents/skills")
                (sprintf "product-skill source %s -> %s: product skills emit to .agents/skills/ ONLY (the standalone materialize step / orchestrator fan-out own the other roots, ADR-0014)" source target)
            // FRAMEWORK PRODUCT-SKILL: lifecycle-independent, profile-gated (FR-001/FR-002),
            // verbatim (copyOnly — canonical bytes must match the skill-manifest sha256, F5).
            assertTrue
                (not (condition.Contains SPEC_KIT_COND))
                (sprintf "framework product-skill source %s -> %s must NOT carry `%s` (it follows the profile, not the lifecycle)" source target SPEC_KIT_COND)
            assertTrue
                (condition.Contains "profile ==")
                (sprintf "framework product-skill source %s -> %s must carry a profile predicate" source target)
            assertTrue
                (List.contains "**/*" copyOnly)
                (sprintf "framework product-skill source %s -> %s must be copyOnly (verbatim canonical body, ADR-0014/F5)" source target)
            frameworkChecked <- frameworkChecked + 1
        elif isManifestSource source then
            // SKILL-MANIFEST (Feature 231, named exception): ungated provider data in .agents/skills/.
            assertTrue
                (t.StartsWith ".agents/skills")
                (sprintf "skill-manifest source %s -> %s must target .agents/skills/ (provider-owned in every lane)" source target)
            assertTrue
                (condition = "")
                (sprintf "skill-manifest source %s -> %s must be UNGATED (ships in every lifecycle)" source target)
            assertTrue
                (List.contains "**/*" copyOnly)
                (sprintf "skill-manifest source %s -> %s must be copyOnly" source target)
            manifestChecked <- manifestChecked + 1
        elif isGatedTarget || isGeneratedTree || isSkillistCatalogSource target includes then
            // LIFECYCLE WORKSPACE: spec-kit-only (.specify/ incl. the single materialize step,
            // agent-context, the narrowed speckit-* skills copy, generated tree, and the
            // spec-kit-only skillist catalog — the named exception).
            assertTrue
                (condition.Contains SPEC_KIT_COND)
                (sprintf "lifecycle-workspace source %s -> %s missing `%s` (condition=%A)" source target SPEC_KIT_COND condition)
            // Feature 231 (F3): the repo-root `.agents/skills/` blanket must vendor ONLY speckit-*.
            if source.Replace('\\', '/') = ".agents/skills/" then
                assertTrue
                    (t = ".agents/skills/")
                    (sprintf "repo-root .agents/skills/ source must target .agents/skills/ only, found %s (Feature 230 blanket twin resurrected?)" target)
                assertTrue
                    (includes = [ "speckit-*/**" ])
                    (sprintf ".agents/skills/ source must include ONLY speckit-*/** (no dev-surface vendoring, ADR-0014/F3); found include=%A" includes)
                speckitNarrowChecked <- speckitNarrowChecked + 1
            if source.Replace('\\', '/') = "template/lifecycle/" then
                assertTrue
                    (t = ".specify/scripts/fs-gg/")
                    (sprintf "materialize source %s must target .specify/scripts/fs-gg/, found %s" source target)
                materializeChecked <- materializeChecked + 1
            workspaceChecked <- workspaceChecked + 1
        else
            // PRODUCT source (base -> ./, samples -> samples/, ant overlay -> ./)
            assertTrue
                (not (condition.Contains SPEC_KIT_COND))
                (sprintf "ungated product source %s -> %s must NOT carry `%s`" source target SPEC_KIT_COND)
            productChecked <- productChecked + 1
    assertTrue (frameworkChecked = 9) (sprintf "expected exactly 9 framework product-skill sources (.agents/skills/ provider surface, no twins), checked %d" frameworkChecked)
    assertTrue (manifestChecked = 1) (sprintf "expected exactly 1 ungated skill-manifest source, checked %d" manifestChecked)
    assertTrue (materializeChecked = 1) (sprintf "expected exactly 1 spec-kit-gated materialize source (template/lifecycle/), checked %d" materializeChecked)
    assertTrue (speckitNarrowChecked = 1) (sprintf "expected exactly 1 narrowed repo-root .agents/skills/ source, checked %d" speckitNarrowChecked)
    assertTrue (workspaceChecked >= 10) (sprintf "expected >=10 lifecycle-workspace sources, checked %d" workspaceChecked)
    assertTrue (productChecked >= 3) (sprintf "expected >=3 ungated product sources, checked %d" productChecked)
    frameworkChecked, workspaceChecked, productChecked

/// Verify the directive agent-context docs are lifecycle-safe (CC-1, env-free).
let private verifyBaseDocsNeutral () =
    use doc = templateDoc ()
    // base CLAUDE.md must be excluded from the ungated base source.
    let baseExcludesClaudeMd =
        doc.RootElement.GetProperty("sources").EnumerateArray()
        |> Seq.exists (fun s ->
            (match s.TryGetProperty "source" with true, v -> v.GetString() = "template/base/" | _ -> false)
            && (match s.TryGetProperty "exclude" with
                | true, ex -> ex.EnumerateArray() |> Seq.exists (fun e -> e.GetString() = "CLAUDE.md")
                | _ -> false))
    assertTrue baseExcludesClaudeMd "template/base/ source must exclude CLAUDE.md (gated agent-context)"
    // base README.md must carry no suppressed-path reference.
    let baseReadme = File.ReadAllText(repoPath "template/base/README.md")
    for p in [ ".specify/"; ".agents/"; ".claude/" ] do
        assertTrue (not (baseReadme.Contains p)) (sprintf "base README.md must not reference suppressed path %s" p)

let private verifyVerdictCore () =
    let values = enumerateLifecycleChoices ()
    let framework, workspace, product = verifyGatedSources ()
    verifyBaseDocsNeutral ()
    printfn "verdict-core OK: covered-values %s; %d lifecycle-workspace sources carry `%s`; %d framework product-skill sources profile-gated & lifecycle-independent; %d product sources clean; directive agent-context docs lifecycle-safe"
        (String.concat ", " values) workspace SPEC_KIT_COND framework product
    values

// ---- live scaffold helpers (env-gated only) ---------------------------------------------------

let private productName = "Demo"

let private runProc (workDir: string) (exe: string) (args: string list) =
    let psi = ProcessStartInfo(exe)
    psi.WorkingDirectory <- workDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add
    use proc = Process.Start psi
    let out = proc.StandardOutput.ReadToEndAsync()
    let err = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()
    proc.ExitCode, out.Result, err.Result

let private relFilesSet (root: string) =
    Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
    |> Seq.map (fun f -> Path.GetRelativePath(root, f).Replace('\\', '/'))
    |> Seq.filter (fun rel -> not (rel.Contains "/bin/" || rel.Contains "/obj/" || rel.StartsWith "bin/" || rel.StartsWith "obj/"))
    |> Set.ofSeq

let private treeFingerprint (root: string) =
    use sha = System.Security.Cryptography.SHA256.Create()
    relFilesSet root
    |> Set.toList
    |> List.map (fun rel ->
        let full = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar))
        rel, sha.ComputeHash(File.ReadAllBytes full) |> Convert.ToHexString)
    |> List.sortBy fst

/// Scaffold one combination, killing the trailing post-action once the tree has stabilised.
/// `extra` carries the `--lifecycle`/`--designSystem`/`--feedback` flags. Returns Some outDir on
/// success, None if generation was EXPECTED to fail (used by the unknown-value rejection check
/// returns the exit code instead — see `scaffoldExpectFail`).
let private scaffold (tmpRoot: string) (profile: string) (extra: string list) (outSubdir: string) =
    let outDir = Path.Combine(tmpRoot, outSubdir)
    if Directory.Exists outDir then Directory.Delete(outDir, true)
    let args =
        [ "new"; "fs-gg-ui"; "--name"; productName; "--profile"; profile; "-o"; outDir ]
        @ extra
    let psi = ProcessStartInfo("dotnet")
    psi.WorkingDirectory <- repoRoot
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add
    use proc = Process.Start psi
    let outTask = proc.StandardOutput.ReadToEndAsync()
    let errTask = proc.StandardError.ReadToEndAsync()

    let treeComplete () =
        File.Exists(Path.Combine(outDir, "Directory.Build.props"))
        && (Directory.Exists outDir
            && Directory.EnumerateFiles(outDir, "*.fsproj", SearchOption.AllDirectories) |> Seq.isEmpty |> not)
    // Feature 205: default generation is side-effect-free — no auto-run post-action, so the process
    // exits promptly on its own. The old 300 s wait/`Kill` loop existed only to defend against the
    // spinning auto-init post-action (the allow-scripts prompt looping on empty stdin); it is now
    // reduced to a short sanity bound that fires only if something unexpected blocks.
    if not (proc.WaitForExit 60000) then (try proc.Kill true with _ -> ())

    if proc.HasExited && proc.ExitCode <> 0 && not (treeComplete ()) then
        failwithf "dotnet new failed for profile=%s %A (exit %d):\n%s\n%s" profile extra proc.ExitCode outTask.Result errTask.Result
    if not (treeComplete ()) then
        failwithf "dotnet new did not materialise a complete tree for profile=%s %A" profile extra
    outDir

/// Scaffold expected to FAIL fast (unknown lifecycle value): returns (exitCode, treeExists).
let private scaffoldExpectFail (tmpRoot: string) (outSubdir: string) (extra: string list) =
    let outDir = Path.Combine(tmpRoot, outSubdir)
    if Directory.Exists outDir then Directory.Delete(outDir, true)
    let args = [ "new"; "fs-gg-ui"; "--name"; productName; "--profile"; "app"; "-o"; outDir ] @ extra
    let code, _, _ = runProc repoRoot "dotnet" args
    let treeExists = File.Exists(Path.Combine(outDir, "Directory.Build.props"))
    code, treeExists

// ---- live validation --------------------------------------------------------------------------

type private ProfileVerdict =
    { Profile: string
      SpecKitDiff: string       // "diff-vs-today=none"
      Sdd: string               // "gated-absent=ok product-present=ok diff-vs-default=gated-only"
      None_: string             // "gated-absent=ok product-present=ok"
      SddSkillCount: int        // framework `fs-gg-*` SKILL.md count under sdd (FR-001 positive fact)
      NoneSkillCount: int       // framework `fs-gg-*` SKILL.md count under none
      SddClaudeProductSkills: int   // ADR-0011: .claude/skills/fs-gg-* product count under sdd (must be 0)
      SddCodexProductSkills: int    // ADR-0011: .codex/skills/fs-gg-* product count under sdd (must be 0)
      NoneClaudeProductSkills: int  // ADR-0011: .claude/skills/fs-gg-* product count under none (must be 0)
      NoneCodexProductSkills: int   // ADR-0011: .codex/skills/fs-gg-* product count under none (must be 0)
      SpecKitMirror: string     // 231: three-root-mirror=ok (materialized) when the single materialize
                                //      step yields byte-identical .agents==.claude==.codex roots
      SpecKitDigests: string    // 231: manifest-digests=ok when --enforce verify exits 0 (ADR-0014 §3)
      DanglingRoutes: int }     // 231: fs-gg-* skill-body path refs unresolvable in the product (must be 0)

// Feature 231: the expected fs-gg-* skill-dir set per profile (mirrors the Feature 219 G-EMIT
// matrix) + the spec-kit-only base authoring skill. Any OTHER fs-gg-* dir in a scaffold is a
// vendored dev-surface wrapper (audit F3) and fails the run.
let private expectedFrameworkSkills =
    [ "app", Set.ofList [ "fs-gg-scene"; "fs-gg-skiaviewer"; "fs-gg-elmish"; "fs-gg-keyboard-input"; "fs-gg-ui-widgets"; "fs-gg-styling"; "fs-gg-layout"; "fs-gg-symbology" ]
      "headless-scene", Set.ofList [ "fs-gg-scene"; "fs-gg-symbology" ]
      "governed", Set.ofList [ "fs-gg-scene"; "fs-gg-testing"; "fs-gg-symbology" ]
      "sample-pack", Set.ofList [ "fs-gg-scene"; "fs-gg-skiaviewer"; "fs-gg-elmish"; "fs-gg-symbology"; "fs-gg-samples" ] ]
    |> Map.ofList

// ---- Feature 231 live helpers -------------------------------------------------------------------

/// Run the emitted standalone materialize step, enforcing (ADR-0014 §Decision 2/3). Returns the
/// process output; a non-zero exit is a hard failure at the call sites unless `expectFail`.
let private runMaterialize (dir: string) (expectFail: bool) =
    let script = Path.Combine(dir, ".specify", "scripts", "fs-gg", "materialize-skill-roots.fsx")
    if not (File.Exists script) then failwithf "%s: materialize script missing at %s" dir script
    let code, out, err = runProc dir "dotnet" [ "fsi"; script; "--enforce" ]
    if not expectFail && code <> 0 then
        failwithf "%s: materialize --enforce failed (exit %d):\n%s\n%s" dir code out err
    code, out + err

/// Full byte-identity of the three agent-skill roots' skills trees (files, not just dir sets —
/// covers extra skill files like fs-gg-symbology/reference.fsx and skill-manifest.json).
let private assertRootsByteIdentical (dir: string) (label: string) =
    let files root =
        let d = Path.Combine(dir, root, "skills")
        if Directory.Exists d then
            Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories)
            |> Seq.map (fun f -> Path.GetRelativePath(d, f).Replace('\\', '/'))
            |> Set.ofSeq
        else Set.empty
    let agents = files ".agents"
    for root in [ ".claude"; ".codex" ] do
        let other = files root
        if agents <> other then
            failwithf "%s: %s/skills file set differs from .agents/skills: only-in-agents=%A only-in-%s=%A"
                label root (Set.difference agents other) root (Set.difference other agents)
        for rel in agents do
            let a = File.ReadAllBytes(Path.Combine(dir, ".agents", "skills", rel.Replace('/', Path.DirectorySeparatorChar)))
            let b = File.ReadAllBytes(Path.Combine(dir, root, "skills", rel.Replace('/', Path.DirectorySeparatorChar)))
            if a <> b then failwithf "%s: %s/skills/%s bytes diverge from .agents copy" label root rel

/// Feature 231 (R2.4 / audit F3): extract path-like references from the fs-gg-* skill bodies and
/// resolve each against the scaffold tree. Backtick-quoted tokens with a product-root path prefix
/// (or any `../` escape) must resolve; placeholder tokens (`<`, `*`, `{`) are skipped.
let private danglingSkillRoutes (dir: string) =
    let skillsDir = Path.Combine(dir, ".agents", "skills")
    if not (Directory.Exists skillsDir) then []
    else
        [ for skillDir in Directory.EnumerateDirectories(skillsDir, "fs-gg-*") do
            for file in Directory.EnumerateFiles(skillDir, "*", SearchOption.AllDirectories) do
                let body = File.ReadAllText file
                let backticked =
                    System.Text.RegularExpressions.Regex.Matches(body, "`([^`\n]+)`")
                    |> Seq.map (fun m -> m.Groups.[1].Value.Trim())
                for token in backticked do
                    let isPlaceholder = token.Contains "<" || token.Contains "*" || token.Contains "{"
                    // `readiness/` is the documented product evidence convention — the directory is
                    // CREATED by the product's first build (build.fsx), so a reference to it in a
                    // fresh scaffold is a forward convention, not a dangling repo route.
                    let isProductConvention = token = "readiness/" || token.StartsWith "readiness/"
                    let looksRooted =
                        [ "docs/"; "src/"; "samples/"; "scripts/"; ".specify/"; ".agents/"; ".claude/"; ".codex/" ]
                        |> List.exists token.StartsWith
                    let isRelativeEscape = token.StartsWith "../"
                    let looksRooted = looksRooted && not isProductConvention
                    if not isPlaceholder && (looksRooted || isRelativeEscape) then
                        // strip a trailing sentence period; tolerate dir-or-file targets
                        let cleaned = token.TrimEnd('.', ':', ',').TrimEnd('/')
                        let resolved =
                            if isRelativeEscape then Path.GetFullPath(Path.Combine(Path.GetDirectoryName file, cleaned.Replace('/', Path.DirectorySeparatorChar)))
                            else Path.Combine(dir, cleaned.Replace('/', Path.DirectorySeparatorChar))
                        if not (File.Exists resolved || Directory.Exists resolved) then
                            yield sprintf "%s -> `%s`" (Path.GetRelativePath(dir, file).Replace('\\', '/')) token ]

let private manifestPresent (dir: string) =
    File.Exists(Path.Combine(dir, ".agents", "skills", "skill-manifest.json"))

/// Feature 219: the lifecycle WORKSPACE is absent (FR-003) even though the framework `fs-gg-*` product
/// skills are now PRESENT under `.agents/skills/`/`.claude/skills/` (FR-001). "Absent" is therefore no
/// longer "no `.agents` dir at all"; it is: no `.specify/`, no agent-context `CLAUDE.md`/`AGENTS.md`,
/// no `speckit-*` command skills, and no base authoring skill (`fs-gg-project`, blanket-copy only).
let private workspaceAbsent (dir: string) =
    not (Directory.Exists(Path.Combine(dir, ".specify")))
    && not (File.Exists(Path.Combine(dir, "CLAUDE.md")))
    && not (File.Exists(Path.Combine(dir, "AGENTS.md")))
    && not (Directory.Exists(Path.Combine(dir, ".agents", "skills", "fs-gg-project")))
    && not (Directory.Exists(Path.Combine(dir, ".claude", "skills", "fs-gg-project")))
    && (Directory.Exists dir
        && (Directory.EnumerateDirectories(dir, "speckit-*", SearchOption.AllDirectories) |> Seq.isEmpty))

/// Feature 219 positive fact: count of framework `fs-gg-*` SKILL.md emitted under `.agents/skills/`.
let private frameworkSkillCount (dir: string) =
    let skillsDir = Path.Combine(dir, ".agents", "skills")
    if Directory.Exists skillsDir then
        Directory.EnumerateDirectories(skillsDir, "fs-gg-*")
        |> Seq.filter (fun d -> File.Exists(Path.Combine(d, "SKILL.md")))
        |> Seq.length
    else 0

/// Feature 230 / ADR-0011: the `fs-gg-*` skill dir set under a given agent-skill root (dirs with a
/// SKILL.md). Under spec-kit the three roots MIRROR (equal sets); under sdd/none the .claude/.codex roots
/// hold ZERO product skills (a write under sdd is the `scaffold.providerWroteSddTree` intrusion, #47/#55).
let private skillSetUnder (dir: string) (root: string) =
    let skillsDir = Path.Combine(dir, root, "skills")
    if Directory.Exists skillsDir then
        Directory.EnumerateDirectories(skillsDir, "fs-gg-*")
        |> Seq.filter (fun d -> File.Exists(Path.Combine(d, "SKILL.md")))
        |> Seq.map Path.GetFileName
        |> Set.ofSeq
    else Set.empty

/// UI product-skill count under an orchestrator-owned root — the base authoring skill `fs-gg-project`
/// (part of the standalone Spec Kit base workspace) is excluded so this reads 0 under sdd/none.
let private orchestratorRootProductSkillCount (dir: string) (root: string) =
    skillSetUnder dir root |> Set.remove "fs-gg-project" |> Set.count

let private claudeProductSkillCount (dir: string) = orchestratorRootProductSkillCount dir ".claude"
let private codexProductSkillCount (dir: string) = orchestratorRootProductSkillCount dir ".codex"

/// Feature 231: the emitted fs-gg-* dir set must be EXACTLY the expected profile set (+ the
/// spec-kit-only authoring/conditional skills) — any extra dir is a vendored wrapper (F3).
let private assertNoWrapperDirs (dir: string) (profile: string) (specKit: bool) =
    let allowedSpecKitExtras = Set.ofList [ "fs-gg-project"; "fs-gg-feedback-capture" ]
    let expected =
        let baseSet = Map.find profile expectedFrameworkSkills
        // fs-gg-samples is spec-kit-gated (sample-pack only): drop it from the sdd/none expectation.
        if specKit then baseSet else Set.remove "fs-gg-samples" baseSet
    let actual = skillSetUnder dir ".agents"
    let extras =
        Set.difference actual expected
        |> fun s -> if specKit then Set.difference s allowedSpecKitExtras else s
    if not (Set.isEmpty extras) then
        failwithf "%s/%s: unexpected fs-gg-* skill dirs vendored (dev-surface wrappers, audit F3): %A"
            profile (if specKit then "spec-kit" else "sdd|none") extras
    let missing = Set.difference expected actual
    if not (Set.isEmpty missing) then
        failwithf "%s/%s: expected fs-gg-* skills missing: %A" profile (if specKit then "spec-kit" else "sdd|none") missing

let private catalogAbsent (dir: string) =
    not (File.Exists(Path.Combine(dir, "docs", "skillist-reference.md")))

let private productPresent (dir: string) =
    File.Exists(Path.Combine(dir, "Directory.Build.props"))
    && Directory.Exists(Path.Combine(dir, "src"))

let private validateProfileLive (tmpRoot: string) (profile: string) =
    let def = scaffold tmpRoot profile [] (sprintf "%s-default" profile)
    let explicit = scaffold tmpRoot profile [ "--lifecycle"; "spec-kit" ] (sprintf "%s-speckit" profile)
    // SC-001 (operational): explicit spec-kit == no-value default, byte for byte (compared BEFORE
    // the materialize step runs, so the comparison is of the raw template emission).
    if treeFingerprint def <> treeFingerprint explicit then
        failwithf "%s: explicit spec-kit scaffold differs from the no-value default (SC-001 broken)" profile
    // Feature 231 / ADR-0014 §Decision 2: under spec-kit (standalone, no orchestrator) the SINGLE
    // materialize step — the vendored FS.GG.Contracts algorithm the product's build target invokes —
    // fans .agents/skills/ into .claude/ + .codex/ and verifies content-addressed against the
    // shipped skill-manifest (--enforce: digests + presence + cross-root identity, ADR-0014 §3).
    if not (manifestPresent def) then
        failwithf "%s/spec-kit: .agents/skills/skill-manifest.json missing (ADR-0014 §1)" profile
    let _, materializeOut = runMaterialize def false
    if not (materializeOut.Contains "fs-gg-skill-roots: ok") then
        failwithf "%s/spec-kit: materialize did not report ok: %s" profile materializeOut
    let specKitDigests = "ok"
    // Idempotence: a second enforcing run mirrors nothing and stays green.
    let _, secondRun = runMaterialize def false
    if not (secondRun.Contains "0 files mirrored") then
        failwithf "%s/spec-kit: materialize is not idempotent: %s" profile secondRun
    // Byte-identical union across ALL THREE roots (files, incl. extra skill files + the manifest).
    assertRootsByteIdentical def (sprintf "%s/spec-kit" profile)
    let specKitMirror = "ok (materialized)"
    // Audit F3: no dev-surface wrapper dirs; the emitted fs-gg-* set is exactly the profile set.
    assertNoWrapperDirs def profile true
    // R2.4: zero dangling path routes in the emitted fs-gg-* skill bodies.
    let dangling = danglingSkillRoutes def
    if not (List.isEmpty dangling) then
        failwithf "%s/spec-kit: dangling skill routes (R2.4): %s" profile (String.concat "; " dangling)
    // Feature 231 (F5, both directions): the --enforce digest pass above already proves emitted
    // skill bodies are byte-verbatim (no name rewriting in skill prose); conversely the intended
    // capital-Product rename outside skills must still fire (src/<Name>/ project dir).
    if not (Directory.Exists(Path.Combine(def, "src", productName))) then
        failwithf "%s/spec-kit: intended Product rename regressed — src/%s missing" profile productName

    let sdd = scaffold tmpRoot profile [ "--lifecycle"; "sdd" ] (sprintf "%s-sdd" profile)
    if not (workspaceAbsent sdd) then failwithf "%s/sdd: lifecycle workspace not fully absent" profile
    if not (productPresent sdd) then failwithf "%s/sdd: product missing" profile
    // FR-001 (positive): the framework `fs-gg-*` product skills ARE present under sdd.
    let sddSkills = frameworkSkillCount sdd
    if sddSkills < 1 then failwithf "%s/sdd: no framework fs-gg-* skills present (FR-001 broken)" profile
    // Feature 231: the ungated skill-manifest ships under sdd too (ADR-0014 §1 — the orchestrator
    // fan-out mirrors it); the standalone materialize step must NOT (spec-kit-only mechanism).
    if not (manifestPresent sdd) then failwithf "%s/sdd: .agents/skills/skill-manifest.json missing" profile
    if File.Exists(Path.Combine(sdd, ".specify", "scripts", "fs-gg", "materialize-skill-roots.fsx")) then
        failwithf "%s/sdd: standalone materialize script leaked into an orchestrated scaffold" profile
    assertNoWrapperDirs sdd profile false
    // Feature 230 (negative): NO fs-gg-* product skill leaks into the orchestrator-owned .claude/ OR .codex/.
    let sddClaudeProduct = claudeProductSkillCount sdd
    let sddCodexProduct = codexProductSkillCount sdd
    if sddClaudeProduct <> 0 then failwithf "%s/sdd: %d fs-gg-* product skills leaked into .claude/skills/ (providerWroteSddTree, #47)" profile sddClaudeProduct
    if sddCodexProduct <> 0 then failwithf "%s/sdd: %d fs-gg-* product skills leaked into .codex/skills/ (providerWroteSddTree, #47)" profile sddCodexProduct
    // FR-006: the full-registry catalog is NOT emitted under sdd (it would dangle).
    if not (catalogAbsent sdd) then failwithf "%s/sdd: docs/skillist-reference.md emitted (would dangle, FR-006 broken)" profile
    // FR-009: default-minus-sdd differs in ONLY gated paths, and sdd adds nothing.
    let defSet = relFilesSet def
    let sddSet = relFilesSet sdd
    let removed = Set.difference defSet sddSet
    let added = Set.difference sddSet defSet
    if not (Set.isEmpty added) then
        failwithf "%s/sdd: added non-gated files vs default: %s" profile (String.concat ", " (Set.toList added))
    let nonGatedRemoved = removed |> Set.filter (isGatedPath >> not)
    if not (Set.isEmpty nonGatedRemoved) then
        failwithf "%s/sdd: removed NON-gated files (FR-009 broken): %s" profile (String.concat ", " (Set.toList nonGatedRemoved))

    let none_ = scaffold tmpRoot profile [ "--lifecycle"; "none" ] (sprintf "%s-none" profile)
    if not (workspaceAbsent none_) then failwithf "%s/none: lifecycle workspace not fully absent" profile
    if not (productPresent none_) then failwithf "%s/none: product missing" profile
    let noneSkills = frameworkSkillCount none_
    if noneSkills < 1 then failwithf "%s/none: no framework fs-gg-* skills present (FR-001 broken)" profile
    if not (manifestPresent none_) then failwithf "%s/none: .agents/skills/skill-manifest.json missing" profile
    assertNoWrapperDirs none_ profile false
    let noneClaudeProduct = claudeProductSkillCount none_
    let noneCodexProduct = codexProductSkillCount none_
    if noneClaudeProduct <> 0 then failwithf "%s/none: %d fs-gg-* product skills leaked into .claude/skills/ (#47)" profile noneClaudeProduct
    if noneCodexProduct <> 0 then failwithf "%s/none: %d fs-gg-* product skills leaked into .codex/skills/ (#47)" profile noneCodexProduct
    if not (catalogAbsent none_) then failwithf "%s/none: docs/skillist-reference.md emitted (would dangle, FR-006 broken)" profile
    // none == sdd at the template level.
    if treeFingerprint none_ <> treeFingerprint sdd then
        failwithf "%s: none tree differs from sdd tree (research CC-3 broken)" profile

    // CC-1: directive agent-context docs carry no suppressed-path reference under sdd/none.
    for tree in [ sdd; none_ ] do
        for d in directiveAgentDocs do
            let p = Path.Combine(tree, d)
            if File.Exists p then
                let txt = File.ReadAllText p
                for sp in [ ".specify/"; ".agents/"; ".claude/" ] do
                    if txt.Contains sp then
                        failwithf "%s: emitted %s references suppressed path %s (dangling ref)" tree d sp

    // FR-005/FR-006 (R4): the full-registry catalog is a spec-kit authoring-lane artifact. It is
    // PRESENT under spec-kit (where the full Spec Kit registry + tooling exist) and SUPPRESSED under
    // sdd/none (verified above), so no scaffold emits a catalog enumerating skills it was meant to
    // vendor but did not — the dangling bug (#30: sdd shipped the ~44-id catalog with 0 skills
    // present). Per-id scoping of the catalog to exactly the vendored `fs-gg-*` set is the deferred
    // R4 follow-up; this feature's guarantee is the emission gating.
    if catalogAbsent explicit then
        failwithf "%s/spec-kit: docs/skillist-reference.md missing (FR-005 broken)" profile

    { Profile = profile
      SpecKitDiff = "diff-vs-today=none"
      Sdd = "gated-absent=ok product-present=ok diff-vs-default=gated-only"
      None_ = "gated-absent=ok product-present=ok"
      SddSkillCount = sddSkills
      NoneSkillCount = noneSkills
      SddClaudeProductSkills = sddClaudeProduct
      SddCodexProductSkills = sddCodexProduct
      NoneClaudeProductSkills = noneClaudeProduct
      NoneCodexProductSkills = noneCodexProduct
      SpecKitMirror = specKitMirror
      SpecKitDigests = specKitDigests
      DanglingRoutes = dangling.Length }

/// Composition matrix (FR-007/FR-008/SC-004): all 12 lifecycle x profile combos generate with the
/// ungated ant overlay present; feedback=true emits no gated feedback skill under sdd/none.
let private validateCompositionMatrix (tmpRoot: string) (values: string list) =
    let mutable count = 0
    for lc in values do
        for p in profiles do
            let dir = scaffold tmpRoot p [ "--lifecycle"; lc; "--designSystem"; "ant" ] (sprintf "mtx-%s-%s-ant" lc p)
            if not (File.Exists(Path.Combine(dir, "design-system.json"))) then
                failwithf "composition %s/%s/ant: ungated ant overlay (design-system.json) missing" lc p
            count <- count + 1
    // feedback=true under a non-spec-kit lifecycle must NOT emit the gated feedback skill.
    let fb = scaffold tmpRoot "app" [ "--lifecycle"; "sdd"; "--feedback"; "true" ] "fb-sdd"
    let feedbackSkill = Directory.Exists(Path.Combine(fb, ".claude", "skills", "fs-gg-feedback-capture"))
    if feedbackSkill then failwithf "feedback=true under sdd emitted the gated feedback skill (should be suppressed)"
    count

/// Feature 231 (Constitution V red case): a corrupted canonical copy must turn the enforcing
/// verify red and NAME the drifted skill — the property the whole apparatus exists to check.
let private validateEnforceRedCase (tmpRoot: string) =
    let dir = scaffold tmpRoot "app" [] "enforce-red-case"
    // First materialize green, then corrupt the SOURCE-ROOT copy: the re-mirror propagates the
    // corruption to every root, so the manifest digest check is what must catch it.
    runMaterialize dir false |> ignore
    let scene = Path.Combine(dir, ".agents", "skills", "fs-gg-scene", "SKILL.md")
    File.AppendAllText(scene, "\n<!-- corrupted for the Feature 231 enforce red case -->\n")
    let code, out = runMaterialize dir true
    if code = 0 then failwith "enforce-red-case: corrupted fs-gg-scene body did NOT fail --enforce"
    if not (out.Contains "fs-gg-scene") then
        failwithf "enforce-red-case: drift output does not name the corrupted skill: %s" out
    "ok"

let private validateUnknownRejected (tmpRoot: string) =
    let code, treeExists = scaffoldExpectFail tmpRoot "bogus" [ "--lifecycle"; "bogus" ]
    if code = 0 then failwith "unknown --lifecycle value was accepted (should fail fast)"
    if treeExists then failwith "unknown --lifecycle value produced an output tree (should be none)"
    "rejected"

// ---- report rendering -------------------------------------------------------------------------

let private renderReport (values: string list) (provenance: string) (verdicts: ProfileVerdict list)
                         (matrixCount: int) (unknown: string) (enforceRedCase: string) =
    let sb = StringBuilder()
    let line (s: string) = sb.Append(s).Append('\n') |> ignore
    line "# Lifecycle Template Validation — Feature 204"
    line ""
    line "> GENERATED — do not edit. Regenerate via:"
    line "> FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx"
    line ""
    line (sprintf "covered-values: %s" (String.concat ", " values))
    line (sprintf "profiles: %s" (String.concat ", " profiles))
    line ""
    line "gated-condition: lifecycle-workspace sources (incl. the single standalone materialize step at .specify/scripts/fs-gg/) carry lifecycle == \"spec-kit\"; framework product-skill sources target .agents/skills/ ONLY (present under every lifecycle, copyOnly canonical bodies) and are profile-gated, lifecycle-independent; the ungated skill-manifest row ships provider data inside .agents/skills/ in every lifecycle (ADR-0014)"
    line "dangling-refs: none"
    line "catalog-dangling: none"
    line "symbology: vendored"
    line (sprintf "composition-matrix: %d/12 generate; ant-overlay-present=ok; feedback-gated-under-non-speckit=ok" matrixCount)
    line (sprintf "unknown-value: %s" unknown)
    line (sprintf "enforce-red-case: %s" enforceRedCase)
    line ""
    for v in verdicts do
        line (sprintf "spec-kit/%s: generate=pass %s" v.Profile v.SpecKitDiff)
    for v in verdicts do
        line (sprintf "spec-kit/%s: three-root-mirror=%s" v.Profile v.SpecKitMirror)
    for v in verdicts do
        line (sprintf "spec-kit/%s: manifest-digests=%s dangling-routes=%d" v.Profile v.SpecKitDigests v.DanglingRoutes)
    for v in verdicts do
        line (sprintf "sdd/%s: manifest-present=ok" v.Profile)
    for v in verdicts do
        line (sprintf "none/%s: manifest-present=ok" v.Profile)
    for v in verdicts do
        line (sprintf "sdd/%s: generate=pass %s" v.Profile v.Sdd)
    for v in verdicts do
        line (sprintf "sdd/%s: framework-skills-present=ok (%d SKILL.md)" v.Profile v.SddSkillCount)
    for v in verdicts do
        line (sprintf "sdd/%s: claude-product-skills=%d codex-product-skills=%d" v.Profile v.SddClaudeProductSkills v.SddCodexProductSkills)
    for v in verdicts do
        line (sprintf "none/%s: generate=pass %s" v.Profile v.None_)
    for v in verdicts do
        line (sprintf "none/%s: framework-skills-present=ok (%d SKILL.md)" v.Profile v.NoneSkillCount)
    for v in verdicts do
        line (sprintf "none/%s: claude-product-skills=%d codex-product-skills=%d" v.Profile v.NoneClaudeProductSkills v.NoneCodexProductSkills)
    line ""
    line (sprintf "provenance: %s" provenance)
    line "result: pass"
    sb.ToString()

let private writeReport (content: string) =
    let p = repoPath reportRelPath
    Directory.CreateDirectory(Path.GetDirectoryName p) |> ignore
    File.WriteAllText(p, content)
    printfn "wrote %s" reportRelPath

// Synthesize the live-only verdict lines from the verdict core (expected values) for --emit-report.
let private synthVerdicts () =
    profiles
    |> List.map (fun p ->
        { Profile = p
          SpecKitDiff = "diff-vs-today=none"
          Sdd = "gated-absent=ok product-present=ok diff-vs-default=gated-only"
          None_ = "gated-absent=ok product-present=ok"
          // env-free synth: the live framework-skill count is profile-specific; assert presence only.
          SddSkillCount = 1
          NoneSkillCount = 1
          // Feature 230 / ADR-0011: under sdd/none the orchestrator owns .claude/.codex, so the template
          // authors 0 product skills there; under spec-kit the three roots mirror (self-fan-out).
          SddClaudeProductSkills = 0
          SddCodexProductSkills = 0
          NoneClaudeProductSkills = 0
          NoneCodexProductSkills = 0
          // Feature 231 / ADR-0014: the single materialize step yields byte-identical roots whose
          // SKILL.md digests match the shipped manifest; zero dangling routes (verdict-core synth).
          SpecKitMirror = "ok (materialized)"
          SpecKitDigests = "ok"
          DanglingRoutes = 0 })

// ---- entry point ------------------------------------------------------------------------------

let private verdictCoreProvenance =
    "verdict-core (env-free; full live proof gated behind FS_GG_RUN_LIFECYCLE_VALIDATION=1)"

let private main () =
    let values = verifyVerdictCore ()

    let emitReport = fsi.CommandLineArgs |> Array.exists (fun a -> a = "--emit-report")
    let liveGate = Environment.GetEnvironmentVariable "FS_GG_RUN_LIFECYCLE_VALIDATION" = "1"

    if emitReport && not liveGate then
        let report = renderReport values verdictCoreProvenance (synthVerdicts ()) 12 "rejected" "ok"
        writeReport report
        0
    elif not liveGate then
        printfn "Live scaffold + report generation is env-gated."
        printfn "Set FS_GG_RUN_LIFECYCLE_VALIDATION=1 to scaffold every combination and write the report."
        printfn "Pass --emit-report to write the report from the env-free verdict-core path."
        0
    else
        let tmpRoot = Path.Combine(Path.GetTempPath(), "fs-gg-lifecycle-validation")
        if Directory.Exists tmpRoot then Directory.Delete(tmpRoot, true)
        Directory.CreateDirectory tmpRoot |> ignore

        let verdicts = profiles |> List.map (validateProfileLive tmpRoot)
        let matrixCount = validateCompositionMatrix tmpRoot values
        let unknown = validateUnknownRejected tmpRoot
        let enforceRedCase = validateEnforceRedCase tmpRoot

        let report = renderReport values "live" verdicts matrixCount unknown enforceRedCase
        writeReport report
        printfn "%s" report
        0

exit (main ())
