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

let private selfProvisionReport () =
    if not (File.Exists validationReportPath) then
        let psi = ProcessStartInfo("dotnet")
        psi.WorkingDirectory <- repositoryRoot
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        [ "fsi"; "scripts/validate-lifecycle-template.fsx"; "--emit-report" ]
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

/// Re-derive the gating invariant directly from template.json (Feature 219 R3 / data-model
/// "Template source category"; reworked by Feature 231 / ADR-0014). Classification order is
/// significant: framework product-skill FIRST — a source under template/product-skills/, which MUST
/// target .agents/skills/ ONLY (the provider surface, present under EVERY lifecycle; a `.claude/`
/// or `.codex/` product-skill target is a resurrected Feature 230 twin and a violation). Then the
/// ungated skill-manifest row (template/skill-manifest/, the ADR-0014 named exception: provider
/// data inside .agents/skills/ in every lifecycle). Then lifecycle-workspace (target under
/// .specify/ — incl. the single materialize step at .specify/scripts/fs-gg/ — | .agents/ | .claude/
/// | .codex/ | CLAUDE.md | AGENTS.md, the generated tree, or the named docs/skillist-reference.md
/// catalog exception), then product. Framework product-skills carry a profile predicate, NO
/// spec-kit clause, and copyOnly (verbatim canonical bodies — the manifest sha256 contract);
/// lifecycle-workspace sources carry the spec-kit clause; product sources carry neither.
let private gatedSourceAudit () =
    use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
    let mutable framework = 0
    let mutable manifest = 0
    let mutable workspace = 0
    let mutable product = 0
    let mutable violations = []
    for s in doc.RootElement.GetProperty("sources").EnumerateArray() do
        let str (prop: string) : string =
            match s.TryGetProperty prop with
            | true, v -> elemStr v
            | _ -> ""
        let includes =
            match s.TryGetProperty "include" with
            | true, arr -> [ for e in arr.EnumerateArray() -> elemStr e ]
            | _ -> []
        let copyOnly =
            match s.TryGetProperty "copyOnly" with
            | true, arr -> [ for e in arr.EnumerateArray() -> elemStr e ]
            | _ -> []
        let source = (str "source").Replace('\\', '/')
        let target = (str "target").Replace('\\', '/')
        let condition = str "condition"
        let isProductSkillSource = source.StartsWith "template/product-skills/"
        let isManifestSource = source = "template/skill-manifest/"
        let isGeneratedTree = source = ".template.config/generated/"
        let isGatedTarget =
            target.StartsWith ".specify" || target.StartsWith ".agents" || target.StartsWith ".claude"
            || target.StartsWith ".codex"
            || target = "CLAUDE.md" || target = "AGENTS.md"
        let isSkillistException =
            target = "docs/skillist-reference.md" || List.contains "docs/skillist-reference.md" includes
        if isProductSkillSource then
            framework <- framework + 1
            if not (target.StartsWith ".agents/skills/") then
                violations <- (sprintf "product-skill %s -> %s must target .agents/skills/ only (Feature 230 twin resurrected?)" source target) :: violations
            if condition.Contains SPEC_KIT_COND then
                violations <- (sprintf "framework-skill %s -> %s wrongly lifecycle-gated" source target) :: violations
            if not (condition.Contains "profile ==") then
                violations <- (sprintf "framework-skill %s -> %s missing profile predicate" source target) :: violations
            if not (List.contains "**/*" copyOnly) then
                violations <- (sprintf "framework-skill %s -> %s missing copyOnly (verbatim canonical body, ADR-0014)" source target) :: violations
        elif isManifestSource then
            manifest <- manifest + 1
            if not (target.StartsWith ".agents/skills/") then
                violations <- (sprintf "skill-manifest %s -> %s must target .agents/skills/" source target) :: violations
            if condition <> "" then
                violations <- (sprintf "skill-manifest %s -> %s must be ungated (every lifecycle)" source target) :: violations
        elif isGatedTarget || isGeneratedTree || isSkillistException then
            workspace <- workspace + 1
            if not (condition.Contains SPEC_KIT_COND) then
                violations <- (sprintf "lifecycle-workspace %s -> %s missing condition" source target) :: violations
            // Feature 231 (F3): the repo-root blanket vendors ONLY the speckit-* process skills.
            if source = ".agents/skills/" && includes <> [ "speckit-*/**" ] then
                violations <- (sprintf ".agents/skills/ blanket must include only speckit-*/** (dev-surface vendoring, F3); found %A" includes) :: violations
        else
            product <- product + 1
            if condition.Contains SPEC_KIT_COND then
                violations <- (sprintf "product %s -> %s wrongly gated" source target) :: violations
    framework, manifest, workspace, product, List.rev violations

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
              let framework, manifest, workspace, product, violations = gatedSourceAudit ()
              Expect.isEmpty violations (sprintf "gating violations: %s" (String.concat "; " violations))
              // Feature 231 / ADR-0014: framework = EXACTLY the 9 .agents/skills/ provider sources
              // (present under every lifecycle; zero .claude/.codex twins — the single materialize
              // step owns the other roots). manifest = exactly the 1 ungated skill-manifest row.
              // workspace shrank from Feature 230's >=30 twin matrix to the ~10 genuine
              // lifecycle-workspace sources (incl. the materialize step). product unchanged.
              Expect.equal framework 9 (sprintf "expected exactly 9 framework product-skill sources (no twins), found %d" framework)
              Expect.equal manifest 1 (sprintf "expected exactly 1 ungated skill-manifest source, found %d" manifest)
              Expect.isTrue (workspace >= 10) (sprintf "expected >=10 lifecycle-workspace sources, found %d" workspace)
              Expect.isTrue (product >= 3) (sprintf "expected >=3 ungated product sources, found %d" product)
              let report = readValidationReport ()
              Expect.stringContains
                  report
                  "gated-condition: lifecycle-workspace sources (incl. the single standalone materialize step at .specify/scripts/fs-gg/) carry lifecycle == \"spec-kit\"; framework product-skill sources target .agents/skills/ ONLY (present under every lifecycle, copyOnly canonical bodies) and are profile-gated, lifecycle-independent; the ungated skill-manifest row ships provider data inside .agents/skills/ in every lifecycle (ADR-0014)"
                  "report records the ADR-0014 gated-condition fact"
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
