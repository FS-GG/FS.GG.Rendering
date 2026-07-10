module Feature204LifecycleTemplateTests

// Feature 204 — the always-on gate for the `lifecycle` template option (validation contract Layer 1).
//
// Deterministic, GL-free, NO `dotnet new`: it asserts the gitignored lifecycle validation report that
// the env-gated regenerator (scripts/validate-lifecycle-template.fsx) writes, AND independently
// re-derives the env-free verdict-core fact (every gated `source` entry in template.json carries
// `lifecycle == "spec-kit"`) so the gate proves the gating itself, not just a self-written line.
// The heavy live work — real `dotnet new` per lifecycle x profile + byte-diff + suppression checks —
// runs behind FS_GG_RUN_LIFECYCLE_VALIDATION=1, mirroring Feature128DesignSystemTemplateTests +
// validate-design-system-template.fsx.
//
// Authored failing-first (Principle V): the report is self-provisioned from the validator's env-free
// `--emit-report` verdict-core path (no `dotnet new`/build/GL/network) before the GV gates evaluate,
// so a fresh checkout (gitignored readiness/ absent) is green-by-construction — but only if the
// verdict core PASSES; if the gating is wrong the verdict core throws, no report is written, and the
// gate fails loudly. Coverage (TP-7) is checked against the template's own `lifecycle` choice set, so
// a new value left unvalidated fails the gate.
//
// Provisioning triggers when the report is absent OR older than either input it derives from
// (template.json, the validator script), and deletes the stale report before regenerating — so a local
// readiness/ surviving across runs and branches can never answer for a template.json it never saw.

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private validationReportPath =
    repositoryPath "specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md"

let private templateJsonPath = repositoryPath ".template.config/template.json"

let private profiles = [ "app"; "headless-scene"; "governed"; "sample-pack" ]

// ---- self-provisioning (mirrors Feature128) ---------------------------------------------------

let private validatorScriptRelPath = "scripts/validate-lifecycle-template.fsx"

// Every file the env-free verdict core reads: template.json (the gating it audits), the validator
// itself (the classification rules), and template/base/README.md (verifyBaseDocsNeutral). A report is
// only as trustworthy as its OLDEST input — miss one and a stale report answers for a file it never saw,
// which is the whole failure this guards. Keep in step with verifyVerdictCore.
let private verdictCoreInputs =
    [ templateJsonPath
      repositoryPath validatorScriptRelPath
      repositoryPath "template/base/README.md" ]

// The report is a DERIVED artifact of those inputs, and it lives under the gitignored readiness/, so it
// outlives runs and branch switches. Regenerating only when ABSENT lets a report written against a
// different template.json satisfy the GV gates below.
let private reportIsStale () =
    not (File.Exists validationReportPath)
    || (let writtenUtc = File.GetLastWriteTimeUtc validationReportPath

        // A vanished input counts as stale, never as "no evidence of change": regenerating then lets the
        // verdict core throw on it loudly, instead of silently dropping it from the freshness check.
        verdictCoreInputs
        |> List.exists (fun input ->
            not (File.Exists input) || File.GetLastWriteTimeUtc input > writtenUtc))

let private selfProvisionReport () =
    if reportIsStale () then
        // Drop the stale report FIRST. The verdict core throws (writing nothing) precisely when the
        // gating is wrong; leaving the old file in place would let it answer for the current
        // template.json, which is the failure this guards. Absent beats stale — GV-8 fails loudly.
        if File.Exists validationReportPath then
            File.Delete validationReportPath

        let psi = ProcessStartInfo("dotnet")
        psi.WorkingDirectory <- repositoryRoot
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        [ "fsi"; validatorScriptRelPath; "--emit-report" ]
        |> List.iter psi.ArgumentList.Add
        match Process.Start psi with
        | null -> ()
        | started ->
            use proc = started
            proc.StandardOutput.ReadToEnd() |> ignore
            proc.StandardError.ReadToEnd() |> ignore
            proc.WaitForExit()

let private reportProvisioned = selfProvisionReport ()

let private readValidationReport () =
    Expect.isTrue
        (File.Exists validationReportPath)
        (sprintf
            "lifecycle validation report missing at %s — regenerate via FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx"
            validationReportPath)
    File.ReadAllText validationReportPath

// ---- env-free verdict-core facts re-derived in-test -------------------------------------------

let private elemStr (e: JsonElement) : string =
    e.GetString() |> Option.ofObj |> Option.defaultValue ""

/// The accepted lifecycle choice set, parsed from the template (single coverage source, TP-7).
let private enumeratedChoices () =
    use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
    [ for c in
          doc.RootElement.GetProperty("symbols").GetProperty("lifecycle").GetProperty("choices").EnumerateArray() ->
          elemStr (c.GetProperty("choice")) ]

/// The values listed on the report's `covered-values:` line.
let private coveredValues (report: string) =
    report.Split('\n')
    |> Array.tryFind (fun l -> l.StartsWith "covered-values:")
    |> Option.map (fun l ->
        l.Substring("covered-values:".Length).Split(',')
        |> Array.map (fun s -> s.Trim())
        |> Array.filter (fun s -> s <> "")
        |> Array.toList)
    |> Option.defaultValue []

let private SPEC_KIT_COND = "lifecycle == \"spec-kit\""

/// One `sources[]` row of template.json, reduced to the fields classification reads.
type private SourceRow =
    { Source: string
      Target: string
      Condition: string
      Includes: string list
      CopyOnly: string list }

let private sourceRows (arrayElement: JsonElement) =
    let str (s: JsonElement) (prop: string) =
        match s.TryGetProperty prop with
        | true, v -> elemStr v
        | _ -> ""
    let strs (s: JsonElement) (prop: string) =
        match s.TryGetProperty prop with
        | true, arr -> [ for e in arr.EnumerateArray() -> elemStr e ]
        | _ -> []
    [ for s in arrayElement.EnumerateArray() ->
        { Source = str s "source"
          Target = str s "target"
          Condition = str s "condition"
          Includes = strs s "include"
          CopyOnly = strs s "copyOnly" } ]

/// Re-derive the gating invariant (Feature 219 R3 / data-model "Template source category"; reworked
/// by Feature 231 / ADR-0014). Classification order is significant: framework product-skill FIRST —
/// a source under template/product-skills/, which MUST target .agents/skills/ ONLY (the provider
/// surface, present under EVERY lifecycle; a `.claude/` or `.codex/` product-skill target is a
/// resurrected Feature 230 twin and a violation). Then the capability-gated skill (issue #248,
/// template/feedback-report/: same .agents/skills/-only, copyOnly, lifecycle-independent shape as a
/// product skill, but gated on the `feedback` capability flag rather than a `profile` predicate) —
/// detected BY SHAPE, not by path, so the next capability-gated skill needs no edit here (it would
/// otherwise fall through and be rejected for missing a spec-kit clause it must not have). Then the
/// ungated skill-manifest row (template/skill-manifest/, the ADR-0014 named exception: provider
/// data inside .agents/skills/ in every lifecycle). Then lifecycle-workspace (target under
/// .specify/ — incl. the single materialize step at .specify/scripts/fs-gg/ — | .agents/ | .claude/
/// | .codex/ | CLAUDE.md | AGENTS.md, the generated tree, or the named docs/skillist-reference.md
/// catalog exception), then product. Framework product-skills carry a profile predicate, NO
/// spec-kit clause, and copyOnly (verbatim canonical bodies — the manifest sha256 contract);
/// lifecycle-workspace sources carry the spec-kit clause; product sources carry neither.
///
/// This is written INDEPENDENTLY of the validator's `classifySource`, deliberately: the gate must
/// prove the gating, not a self-written line. Issue #253: that is only worth something if the two
/// stay in agreement, so the **rule ids** returned here are a contract shared verbatim with the
/// validator, and GV-AGREE feeds a synthetic fixture through both and compares. Only the ids are
/// shared — the prose `ruleMessage` renders from them is this side's own.
let private classifySource (row: SourceRow) : string * string list =
    let source = row.Source.Replace('\\', '/')
    let target = row.Target.Replace('\\', '/')
    let condition = row.Condition
    let violations = ResizeArray<string>()
    let breaks ruleId holds = if not holds then violations.Add ruleId
    let verbatimBody = List.contains "**/*" row.CopyOnly

    let isProductSkillSource =
        source.StartsWith "template/product-skills/" || source.StartsWith "template/base/.agents/skills/"
    let isCapabilitySkillSource =
        target.StartsWith ".agents/skills/"
        && condition <> ""
        && not (condition.Contains SPEC_KIT_COND)
        && not (condition.Contains "profile ==")
    let isManifestSource = source = "template/skill-manifest/"
    let isGeneratedTree = source = ".template.config/generated/"
    let isGatedTarget =
        target.StartsWith ".specify" || target.StartsWith ".agents" || target.StartsWith ".claude"
        || target.StartsWith ".codex"
        || target = "CLAUDE.md" || target = "AGENTS.md"
    let isSkillistException =
        target = "docs/skillist-reference.md" || List.contains "docs/skillist-reference.md" row.Includes

    let category =
        if isProductSkillSource then
            breaks "framework/target-agents-skills-only" (target.StartsWith ".agents/skills/")
            breaks "framework/no-spec-kit-clause" (not (condition.Contains SPEC_KIT_COND))
            breaks "framework/profile-predicate" (condition.Contains "profile ==")
            breaks "framework/copy-only" verbatimBody
            "framework"
        elif isCapabilitySkillSource then
            breaks "capability/copy-only" verbatimBody
            "capability"
        elif isManifestSource then
            breaks "manifest/target-agents-skills" (target.StartsWith ".agents/skills/")
            breaks "manifest/ungated" (condition = "")
            breaks "manifest/copy-only" verbatimBody
            "manifest"
        elif isGatedTarget || isGeneratedTree || isSkillistException then
            breaks "workspace/spec-kit-clause" (condition.Contains SPEC_KIT_COND)
            // Feature 231 (F3): the repo-root blanket vendors ONLY the speckit-* process skills.
            if source = ".agents/skills/" then
                breaks "workspace/speckit-blanket-target" (target = ".agents/skills/")
                breaks "workspace/speckit-blanket-include" (row.Includes = [ "speckit-*/**" ])
            // ADR-0014 §Decision 2/3: exactly one spec-kit-gated materialize step.
            if source = "template/lifecycle/" then
                breaks "workspace/materialize-target" (target = ".specify/scripts/fs-gg/")
            "workspace"
        else
            breaks "product/no-spec-kit-clause" (not (condition.Contains SPEC_KIT_COND))
            "product"

    category, violations |> List.ofSeq |> List.sort

/// This side's prose for a rule id. Deliberately NOT shared with the validator — only the ids are.
let private ruleMessage (row: SourceRow) (ruleId: string) =
    let s, t = row.Source, row.Target
    match ruleId with
    | "framework/target-agents-skills-only" ->
        sprintf "product-skill %s -> %s must target .agents/skills/ only (Feature 230 twin resurrected?)" s t
    | "framework/no-spec-kit-clause" -> sprintf "framework-skill %s -> %s wrongly lifecycle-gated" s t
    | "framework/profile-predicate" -> sprintf "framework-skill %s -> %s missing profile predicate" s t
    | "framework/copy-only" ->
        sprintf "framework-skill %s -> %s missing copyOnly (verbatim canonical body, ADR-0014)" s t
    | "capability/copy-only" ->
        sprintf "capability-skill %s -> %s missing copyOnly (verbatim canonical body, ADR-0014)" s t
    | "manifest/target-agents-skills" -> sprintf "skill-manifest %s -> %s must target .agents/skills/" s t
    | "manifest/ungated" -> sprintf "skill-manifest %s -> %s must be ungated (every lifecycle)" s t
    | "manifest/copy-only" ->
        sprintf "skill-manifest %s -> %s missing copyOnly (verbatim provider data, ADR-0014)" s t
    | "workspace/spec-kit-clause" -> sprintf "lifecycle-workspace %s -> %s missing condition" s t
    | "workspace/speckit-blanket-target" ->
        sprintf ".agents/skills/ blanket must target .agents/skills/ only; found %s (F3)" t
    | "workspace/speckit-blanket-include" ->
        sprintf ".agents/skills/ blanket must include only speckit-*/** (dev-surface vendoring, F3); found %A" row.Includes
    | "workspace/materialize-target" ->
        sprintf "materialize step %s must target .specify/scripts/fs-gg/; found %s (ADR-0014)" s t
    | "product/no-spec-kit-clause" -> sprintf "product %s -> %s wrongly gated" s t
    | unknown -> sprintf "source %s -> %s violates %s" s t unknown

let private gatedSourceAudit () =
    use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
    let classified =
        sourceRows (doc.RootElement.GetProperty("sources"))
        |> List.map (fun row -> row, classifySource row)
    let inCategory name =
        classified |> List.filter (fun (_, (c, _)) -> c = name) |> List.length
    let violations =
        classified
        |> List.collect (fun (row, (_, ruleIds)) -> ruleIds |> List.map (ruleMessage row))
    inCategory "framework",
    inCategory "capability",
    inCategory "manifest",
    inCategory "workspace",
    inCategory "product",
    violations

// ---- GV-AGREE: the two independent classifiers must agree (issue #253) ------------------------

/// The shared vocabulary, asserted against the one the validator publishes via `--classify`. That
/// mirror is what gives the fixture teeth: a rule declared in the validator must appear here (or
/// GV-AGREE's vocabulary check fails), and every id here must be violated by some fixture row (or
/// the fixture check fails) — so a validator-side rule cannot ship compared-against-nothing, which
/// is the direction #248 actually drifted.
///
/// The mirror cannot police a rule added to `classifySource` below and declared nowhere: it fires
/// on no fixture row and both set checks stay empty. Keep this list beside that function.
let private allRuleIds =
    set
        [ "framework/target-agents-skills-only"
          "framework/no-spec-kit-clause"
          "framework/profile-predicate"
          "framework/copy-only"
          "capability/copy-only"
          "manifest/target-agents-skills"
          "manifest/ungated"
          "manifest/copy-only"
          "workspace/spec-kit-clause"
          "workspace/speckit-blanket-target"
          "workspace/speckit-blanket-include"
          "workspace/materialize-target"
          "product/no-spec-kit-clause" ]

let private row source target condition includes copyOnly =
    { Source = source
      Target = target
      Condition = condition
      Includes = includes
      CopyOnly = copyOnly }

/// Synthetic rows — one per category, each category's violations, and the traps that make the
/// classification ORDER load-bearing. These describe rows template.json does not (and mostly must
/// not) contain; both classifiers are pure over a row, so neither reads repo state to judge them.
let private agreementFixture: (string * SourceRow) list =
    let verbatim = [ "**/*" ]
    let profileGate = "(profile == \"app\")"
    [ "framework: product-skill, clean",
      row "template/product-skills/fs-gg-scene/" ".agents/skills/fs-gg-scene/" profileGate [] verbatim
      "framework: fs-gg-project's canonical body under template/base/.agents/skills/ (issue #91)",
      row "template/base/.agents/skills/fs-gg-project/" ".agents/skills/fs-gg-project/" "(profile == \"game\")" [] verbatim
      "framework: a resurrected Feature 230 .claude/ twin",
      row "template/product-skills/fs-gg-twin/" ".claude/skills/fs-gg-twin/" profileGate [] verbatim
      "framework: wrongly lifecycle-gated",
      row "template/product-skills/fs-gg-gated/" ".agents/skills/fs-gg-gated/" "(profile == \"app\") && lifecycle == \"spec-kit\"" [] verbatim
      "framework: no profile predicate and not copyOnly",
      row "template/product-skills/fs-gg-bare/" ".agents/skills/fs-gg-bare/" "" [] []
      "framework: `.agents/skillsets/` is not the `.agents/skills/` provider root",
      row "template/product-skills/fs-gg-prefix/" ".agents/skillsets/fs-gg-prefix/" profileGate [] verbatim
      "capability: fs-gg-feedback-report, the first one (issue #248)",
      row "template/feedback-report/skill/" ".agents/skills/fs-gg-feedback-report/" "(feedback == true)" [] verbatim
      "capability: a hypothetical SECOND one, off a path neither classifier names",
      row "template/telemetry/skill/" ".agents/skills/fs-gg-telemetry/" "(telemetry == true)" [] verbatim
      "capability: not copyOnly",
      row "template/telemetry/skill/" ".agents/skills/fs-gg-telemetry/" "(telemetry == true)" [] []
      "manifest: ungated provider data, clean",
      row "template/skill-manifest/" ".agents/skills/" "" [] verbatim
      "manifest: gated AND not copyOnly",
      row "template/skill-manifest/" ".agents/skills/" "(lifecycle == \"spec-kit\")" [] []
      "manifest: wrong target root",
      row "template/skill-manifest/" ".agents/skillsets/" "" [] verbatim
      "workspace: the narrowed repo-root speckit-* blanket, clean",
      row ".agents/skills/" ".agents/skills/" "(lifecycle == \"spec-kit\")" [ "speckit-*/**" ] verbatim
      "workspace: the blanket widened to the dev surface and re-pointed at .claude/ (F3)",
      row ".agents/skills/" ".claude/skills/" "(lifecycle == \"spec-kit\")" [ "**/*" ] verbatim
      "workspace: the single materialize step, clean",
      row "template/lifecycle/" ".specify/scripts/fs-gg/" "(lifecycle == \"spec-kit\")" [] verbatim
      "workspace: materialize step re-pointed off .specify/scripts/fs-gg/",
      row "template/lifecycle/" ".specify/scripts/other/" "(lifecycle == \"spec-kit\")" [] verbatim
      "workspace: fs-gg-feedback-capture — capability-gated AND spec-kit-gated, so NOT a capability skill",
      row "template/feedback/skill/" ".agents/skills/fs-gg-feedback-capture/" "(feedback == true) && lifecycle == \"spec-kit\"" [] verbatim
      "workspace: fs-gg-samples — profile-gated AND spec-kit-gated, so NOT a framework skill",
      row "template/fragments/samples/skill/" ".agents/skills/fs-gg-samples/" "(profile == \"sample-pack\") && lifecycle == \"spec-kit\"" [] verbatim
      "workspace: a profile-gated skill body outside the framework source prefix, missing its clause",
      row "template/other/skill/" ".agents/skills/fs-gg-other/" "(profile == \"game\")" [] verbatim
      "workspace: the generated agent-context tree, ungated",
      row ".template.config/generated/" "./" "" [] []
      "workspace: the docs/skillist-reference.md catalog exception (targets the product root)",
      row "template/base/" "./" "(lifecycle == \"spec-kit\")" [ "docs/skillist-reference.md" ] [ "docs/skillist-reference.md" ]
      "workspace: the .claude/ root",
      row "template/base/.claude/" ".claude/" "(lifecycle == \"spec-kit\")" [] verbatim
      "workspace: a directive agent-context doc",
      row "template/agent-context/" "CLAUDE.md" "(lifecycle == \"spec-kit\")" [] []
      "product: the ungated base source",
      row "template/base/" "./" "" [] [ "docs/api-surface/**" ]
      "product: a profile-gated source fragment",
      row "template/fragments/vec2/src/" "src/" "(profile == \"game\")" [] []
      "product: wrongly lifecycle-gated",
      row "template/fragments/bad/" "src/" "(lifecycle == \"spec-kit\")" [] []
      "product: the ant design-system overlay",
      row "template/design-system/ant/" "./" "(designSystem == \"ant\")" [] [] ]

/// Run the validator's `classifySource` over the fixture, out-of-process, and collect both the rule
/// vocabulary it publishes and its per-row verdicts. `--classify` is env-free and reads no repo
/// state, so this stays as deterministic as the in-test copy.
let private validatorVerdicts (rows: SourceRow list) =
    let fixturePath =
        Path.Combine(Path.GetTempPath(), sprintf "fs-gg-classify-%s.json" (Guid.NewGuid().ToString "N"))
    let payload =
        rows
        |> List.map (fun r ->
            {| source = r.Source
               target = r.Target
               condition = r.Condition
               ``include`` = r.Includes
               copyOnly = r.CopyOnly |})
    File.WriteAllText(fixturePath, JsonSerializer.Serialize payload)
    try
        let psi = ProcessStartInfo("dotnet")
        psi.WorkingDirectory <- repositoryRoot
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        [ "fsi"; "scripts/validate-lifecycle-template.fsx"; "--classify"; fixturePath ]
        |> List.iter psi.ArgumentList.Add
        match Process.Start psi with
        | null -> failwith "could not start dotnet fsi for --classify"
        | started ->
            use proc = started
            let out = proc.StandardOutput.ReadToEnd()
            let err = proc.StandardError.ReadToEnd()
            proc.WaitForExit()
            if proc.ExitCode <> 0 then
                failwithf "validate-lifecycle-template.fsx --classify failed (exit %d):\n%s\n%s" proc.ExitCode out err
            let lines = out.Replace("\r\n", "\n").Split('\n')
            let marker name =
                lines |> Array.tryFindIndex (fun l -> l.Trim() = name)
            let split (line: string) =
                let parts = line.Split('\t')
                let rules =
                    if parts.Length < 2 || parts[1] = "" then []
                    else parts[1].Split(',') |> Array.toList
                parts[0], rules
            match marker "FSGG-CLASSIFY-BEGIN", marker "FSGG-CLASSIFY-END" with
            | Some b, Some e when e > b ->
                let block = lines[b + 1 .. e - 1] |> Array.map split |> Array.toList
                match block with
                | ("FSGG-RULE-IDS", vocabulary) :: verdicts -> Set.ofList vocabulary, verdicts
                | _ -> failwithf "--classify did not publish its rule vocabulary first:\n%s" out
            | _ -> failwithf "--classify emitted no delimited block:\n%s\n%s" out err
    finally
        if File.Exists fixturePath then File.Delete fixturePath

[<Tests>]
let feature204LifecycleTemplateTests =
    testList
        "Feature204 lifecycle template validation"
        [
          // GV-1 (TP-7 / SC-005): covered-values equals the enumerated lifecycle choice set.
          test "GV-1 coverage equals the template's lifecycle choice set" {
              let report = readValidationReport ()
              let covered = coveredValues report |> List.sort
              let expected = enumeratedChoices () |> List.sort
              Expect.equal covered expected "covered-values must equal the template's lifecycle choices"
              Expect.stringContains report "covered-values: spec-kit, sdd, none" "covered-values lists the 3 values in declaration order"
          }

          // GV-2 (FR-001/FR-002/FR-003, Feature 219 3-category verdict-core fact re-derived in-test):
          // framework product-skill sources are profile-gated & lifecycle-INDEPENDENT; lifecycle-
          // workspace sources carry the spec-kit condition; product sources carry neither.
          test "GV-2 sources partition into framework-skill / manifest / lifecycle-workspace / product (ADR-0014 gating)" {
              let framework, capability, manifest, workspace, product, violations = gatedSourceAudit ()
              Expect.isEmpty violations (sprintf "gating violations: %s" (String.concat "; " violations))
              // Feature 231 / ADR-0014: framework = EXACTLY the .agents/skills/ provider sources
              // (present under every lifecycle; zero .claude/.codex twins — the single materialize
              // step owns the other roots). Feature 240 (#73) added fs-gg-game-core; issue #91 added
              // the 11th, fs-gg-project — promoted from the former lifecycle-gated whole-.agents/
              // blanket to a dedicated profile-gated source so it materializes on every lifecycle.
              // Feature 243 (#92) added fs-gg-audio (12th); Feature 244 (#93) added fs-gg-persistence (13th).
              // Issue #113 added the docs-only fs-gg-model-swap (14th) on the (game, sample-pack) gate.
              // Feature 246 added fs-gg-collision (15th) on the same (game, sample-pack) gate.
              // Feature 247 added fs-gg-visibility (16th) on the same (game, sample-pack) gate.
              // Feature 249 added fs-gg-grids (17th) on the same (game, sample-pack) gate.
              // manifest = exactly the 1 ungated skill-manifest row.
              // workspace shrank from Feature 230's >=30 twin matrix to the genuine
              // lifecycle-workspace sources (incl. the materialize step). product unchanged.
              Expect.equal framework 18 (sprintf "expected exactly 18 framework product-skill sources (no twins), found %d" framework)
              Expect.equal capability 1 (sprintf "expected exactly 1 capability-gated skill source (fs-gg-feedback-report, issue #248), found %d" capability)
              Expect.equal manifest 1 (sprintf "expected exactly 1 ungated skill-manifest source, found %d" manifest)
              Expect.isTrue (workspace >= 9) (sprintf "expected >=9 lifecycle-workspace sources, found %d" workspace)
              Expect.isTrue (product >= 3) (sprintf "expected >=3 ungated product sources, found %d" product)
              let report = readValidationReport ()
              Expect.stringContains
                  report
                  "gated-condition: lifecycle-workspace sources (incl. the single standalone materialize step at .specify/scripts/fs-gg/) carry lifecycle == \"spec-kit\"; framework product-skill sources target .agents/skills/ ONLY (present under every lifecycle, copyOnly canonical bodies) and are profile-gated, lifecycle-independent; the ungated skill-manifest row ships provider data inside .agents/skills/ in every lifecycle (ADR-0014)"
                  "report records the ADR-0014 gated-condition fact"
          }

          // GV-AGREE (issue #253): the source taxonomy is implemented TWICE on purpose — here, and
          // in validate-lifecycle-template.fsx's verdict core — so the gate proves the gating rather
          // than a self-written line. An independent re-derivation is only worth keeping if it is
          // kept independent AND kept in sync on the rules it shares. #248 added the capability
          // category, updated this copy, left the script's behind, and the suite stayed green: the
          // disagreement only surfaced later, by hand, as a misleading `missing lifecycle ==
          // "spec-kit"` on a source that must not carry it. These two tests close that gap.
          test "GV-AGREE both classifiers return the same verdict for every fixture row" {
              let rows = agreementFixture |> List.map snd
              let mine = rows |> List.map classifySource
              let vocabulary, theirs = validatorVerdicts rows
              Expect.equal
                  vocabulary
                  allRuleIds
                  "validate-lifecycle-template.fsx's rule vocabulary matches this file's copy — a rule declared on one side only"
              Expect.equal
                  (List.length theirs)
                  (List.length rows)
                  "validate-lifecycle-template.fsx --classify returns one verdict per fixture row"
              for (label, _), expected, actual in List.zip3 agreementFixture mine theirs do
                  Expect.equal
                      actual
                      expected
                      (sprintf
                          "row '%s': validate-lifecycle-template.fsx classifySource and gatedSourceAudit disagree — the two copies of the source taxonomy have drifted"
                          label)
          }

          // The fixture is the agreement test's coverage. A rule id added to either classifier but
          // to no fixture row would be compared on nothing, and the drift would ship green again.
          test "GV-AGREE fixture exercises every category and every shared rule id" {
              let verdicts = agreementFixture |> List.map (snd >> classifySource)
              let categories = verdicts |> List.map fst |> Set.ofList
              Expect.equal
                  categories
                  (set [ "framework"; "capability"; "manifest"; "workspace"; "product" ])
                  "fixture covers every source category"
              let exercised = verdicts |> List.collect snd |> Set.ofList
              Expect.isEmpty
                  (Set.difference allRuleIds exercised)
                  "every shared rule id is violated by at least one fixture row"
              Expect.isEmpty
                  (Set.difference exercised allRuleIds)
                  "the fixture emits no rule id missing from the shared vocabulary"
          }

          // GV-3 (US1 / FR-002 / SC-001): spec-kit is byte-identical to today, every profile.
          test "GV-3 spec-kit is byte-identical (diff-vs-today=none) for every profile" {
              let report = readValidationReport ()
              for p in profiles do
                  Expect.stringContains report (sprintf "spec-kit/%s: generate=pass diff-vs-today=none" p) (sprintf "spec-kit/%s byte-identical" p)
          }

          // GV-4 (US2 / FR-004/FR-005/FR-009 / SC-003): sdd suppresses exactly the lifecycle WORKSPACE
          // (not the framework skills, which are now PRESENT under sdd — Feature 219 FR-001), product intact.
          test "GV-4 sdd suppresses the lifecycle workspace while the framework skills are present" {
              let report = readValidationReport ()
              for p in profiles do
                  Expect.stringContains
                      report
                      (sprintf "sdd/%s: generate=pass gated-absent=ok product-present=ok diff-vs-default=gated-only" p)
                      (sprintf "sdd/%s gated-only suppression" p)
                  Expect.stringContains
                      report
                      (sprintf "sdd/%s: framework-skills-present=ok" p)
                      (sprintf "sdd/%s framework fs-gg-* skills present (FR-001)" p)
                  // Feature 230 / ADR-0011 (G-204.4): under sdd the orchestrator owns .claude/.codex, so the
                  // template authors zero UI product skills into EITHER tree (#47/providerWroteSddTree).
                  Expect.stringContains
                      report
                      (sprintf "sdd/%s: claude-product-skills=0 codex-product-skills=0" p)
                      (sprintf "sdd/%s .claude+.codex hold zero product skills (ADR-0011)" p)
              Expect.stringContains report "dangling-refs: none" "no directive agent-context doc references a suppressed path (CC-1)"
              Expect.stringContains report "catalog-dangling: none" "no scaffold emits a catalog listing absent skills (FR-005/FR-006)"
          }

          // GV-5 (US3 / FR-004 / SC-003): none suppresses the workspace, framework skills present (same as sdd).
          test "GV-5 none suppresses the lifecycle workspace while the framework skills are present" {
              let report = readValidationReport ()
              for p in profiles do
                  Expect.stringContains
                      report
                      (sprintf "none/%s: generate=pass gated-absent=ok product-present=ok" p)
                      (sprintf "none/%s suppression" p)
                  Expect.stringContains
                      report
                      (sprintf "none/%s: framework-skills-present=ok" p)
                      (sprintf "none/%s framework fs-gg-* skills present (FR-001)" p)
                  // Feature 230 / ADR-0011 (G-204.4): zero UI product skills in .claude+.codex under none (none ≡ sdd).
                  Expect.stringContains
                      report
                      (sprintf "none/%s: claude-product-skills=0 codex-product-skills=0" p)
                      (sprintf "none/%s .claude+.codex hold zero product skills (ADR-0011)" p)
          }

          // GV-4b (Feature 230 / ADR-0011 §1 / SC-001): under spec-kit the standalone product's three
          // agent-skill roots MIRROR — the .claude/skills/ and .codex/skills/ fs-gg-* set equals .agents/skills/.
          test "GV-4b spec-kit mirrors the skill union across .agents/.claude/.codex" {
              let report = readValidationReport ()
              for p in profiles do
                  Expect.stringContains
                      report
                      (sprintf "spec-kit/%s: three-root-mirror=ok" p)
                      (sprintf "spec-kit/%s .agents==.claude==.codex skill set (ADR-0011 §1)" p)
          }

          // GV-6 (Polish / FR-006/FR-007/FR-008 / SC-004): composition matrix + fail-fast.
          test "GV-6 composition matrix generates and unknown value is rejected" {
              let report = readValidationReport ()
              Expect.stringContains report "composition-matrix: 12/12 generate; ant-overlay-present=ok; feedback-gated-under-non-speckit=ok" "12-combo composition matrix holds"
              Expect.stringContains report "unknown-value: rejected" "unknown lifecycle value fails fast"
          }

          // GV-7 (Constitution V): provenance is disclosed (verdict-core env-free OR live).
          test "GV-7 provenance is disclosed" {
              let report = readValidationReport ()
              Expect.isTrue
                  (report.Contains "provenance: live"
                   || report.Contains "provenance: verdict-core (env-free; full live proof gated behind FS_GG_RUN_LIFECYCLE_VALIDATION=1)")
                  "report discloses whether it was self-provisioned env-free or written from the live run"
          }

          // GV-8 (Principle VI): result: pass only when GV-1..GV-7 hold.
          test "GV-8 overall result is pass" {
              let report = readValidationReport ()
              Expect.stringContains report "result: pass" "validation result is pass"
          }
        ]
