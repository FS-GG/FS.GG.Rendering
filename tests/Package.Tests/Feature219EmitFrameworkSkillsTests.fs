module Feature219EmitFrameworkSkillsTests

// Feature 219 — the POSITIVE gate for "framework skills follow the product, not the lifecycle".
//
// Where Feature204LifecycleTemplateTests proves the *gating invariant* (the lifecycle workspace is
// spec-kit-only), this gate proves the *emission* facts the workspace gate cannot: that the framework
// `fs-gg-*` product skills ARE present under `sdd`/`none` per profile (G-EMIT / FR-001/FR-002), that
// no scaffold emits a catalog listing absent skills (G-CATALOG / FR-005/FR-006), and that every
// `fs-gg-symbology` directory is wired — vendored as of Feature 223 (G-NODANGLE-SYMB / FR-007).
//
// Deterministic, GL-free, NO `dotnet new`: the positive emission set is re-derived directly from
// template.json (a framework product-skill source emits for profile P under every lifecycle iff its
// `source` is under template/product-skills/, it carries no `lifecycle == "spec-kit"` clause, and its
// profile predicate names P), and the live-only counts are asserted against the gitignored validation
// report the env-gated regenerator (scripts/validate-lifecycle-template.fsx) writes — the same report
// Feature204 asserts. The report is self-provisioned from the validator's env-free `--emit-report`
// verdict-core path if absent, so a fresh checkout is green-by-construction only when the gating holds.

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

let private SPEC_KIT_COND = "lifecycle == \"spec-kit\""

// The profile -> framework `fs-gg-*` skill-set contract (data-model.md matrix). `fs-gg-symbology` is
// now VENDORED (Feature 223): it follows the `fs-gg-scene` profile set (app, headless-scene, governed,
// sample-pack, game) with no `lifecycle` clause, so it appears in every one of those rows. The earlier
// Feature 219 "not-vendored / would red GV-3" rationale was a misread of GV-3 (research R1): GV-3
// compares explicit `--lifecycle spec-kit` against the no-flag default of the SAME template, which an
// ungated source leaves byte-identical — so wiring symbology is GV-3-neutral.
let private expectedFrameworkSkills =
    [ "app", set [ "fs-gg-scene"; "fs-gg-skiaviewer"; "fs-gg-elmish"; "fs-gg-keyboard-input"; "fs-gg-ui-widgets"; "fs-gg-symbology" ]
      "headless-scene", set [ "fs-gg-scene"; "fs-gg-symbology" ]
      "governed", set [ "fs-gg-scene"; "fs-gg-testing"; "fs-gg-symbology" ]
      "sample-pack", set [ "fs-gg-scene"; "fs-gg-skiaviewer"; "fs-gg-elmish"; "fs-gg-symbology" ]
      "game", set [ "fs-gg-scene"; "fs-gg-skiaviewer"; "fs-gg-elmish"; "fs-gg-keyboard-input"; "fs-gg-ui-widgets"; "fs-gg-symbology" ] ]

// The env-free G-EMIT matrix above covers all five scene-bearing profiles (game's symbology emit is
// proven directly from template.json). The live lifecycle-validation REPORT, however, only scaffolds
// the four profiles the validator's loop covers (app, headless-scene, governed, sample-pack) — `game`
// is not in that loop — so the report-backed assertions iterate that covered set, not every matrix key.
let private profiles = [ "app"; "headless-scene"; "governed"; "sample-pack" ]

// ---- self-provisioning (mirrors Feature204) ---------------------------------------------------

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

// ---- env-free emission facts re-derived from template.json -------------------------------------

let private elemStr (e: JsonElement) : string =
    e.GetString() |> Option.ofObj |> Option.defaultValue ""

type private SkillSource =
    { Id: string
      Target: string
      Condition: string }

/// All framework product-skill sources (source under template/product-skills/), with skill id, the
/// destination target, and the condition.
let private frameworkSkillSources () =
    use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
    [ for s in doc.RootElement.GetProperty("sources").EnumerateArray() do
        let str (prop: string) =
            match s.TryGetProperty prop with
            | true, v -> elemStr v
            | _ -> ""
        let source = (str "source").Replace('\\', '/')
        if source.StartsWith "template/product-skills/" then
            let id = source.TrimEnd('/').Split('/') |> Array.last
            yield { Id = id; Target = (str "target").Replace('\\', '/'); Condition = str "condition" } ]

/// The set of framework skill ids that emit for `profile` under a NON-spec-kit lifecycle = sources
/// with a matching profile predicate and no spec-kit clause.
let private emittedFor (profile: string) (sources: SkillSource list) =
    sources
    |> List.filter (fun s ->
        not (s.Condition.Contains SPEC_KIT_COND)
        && s.Condition.Contains(sprintf "profile == \"%s\"" profile))
    |> List.map (fun s -> s.Id)
    |> Set.ofList

[<Tests>]
let feature219EmitFrameworkSkillsTests =
    testList
        "Feature219 emit framework skills on every lifecycle"
        [
          // G-EMIT (FR-001/FR-002, env-free): for each profile the framework `fs-gg-*` skills emitted
          // under a non-spec-kit lifecycle equal the data-model matrix (symbology vendored, Feature 223).
          test "G-EMIT framework skill set per profile is lifecycle-independent and matches the matrix" {
              let sources = frameworkSkillSources ()
              for profile, expected in expectedFrameworkSkills do
                  let actual = emittedFor profile sources
                  Expect.equal actual expected (sprintf "profile %s framework skill set" profile)
          }

          // G-EMIT (FR-001): every framework skill source is lifecycle-independent AND emitted to BOTH
          // .agents/skills/ and .claude/skills/ destinations (so agents on either runtime find it).
          test "G-EMIT every framework skill source is lifecycle-independent with both agent destinations" {
              let sources = frameworkSkillSources ()
              // 7 product skills x 2 surfaces = 14 since Feature 223 wired symbology (was 6 x 2 = 12).
              Expect.isTrue (sources.Length >= 14) (sprintf "expected >=14 framework skill sources, found %d" sources.Length)
              for s in sources do
                  Expect.isFalse (s.Condition.Contains SPEC_KIT_COND) (sprintf "%s -> %s must not be lifecycle-gated" s.Id s.Target)
                  Expect.stringContains s.Condition "profile ==" (sprintf "%s -> %s must carry a profile predicate" s.Id s.Target)
              for id in sources |> List.map (fun s -> s.Id) |> List.distinct do
                  let targets = sources |> List.filter (fun s -> s.Id = id) |> List.map (fun s -> s.Target)
                  Expect.isTrue
                      (targets |> List.exists (fun t -> t.StartsWith ".agents/skills/"))
                      (sprintf "%s must emit under .agents/skills/" id)
                  Expect.isTrue
                      (targets |> List.exists (fun t -> t.StartsWith ".claude/skills/"))
                      (sprintf "%s must emit under .claude/skills/" id)
          }

          // G-EMIT (FR-001 positive, report-backed): sdd and none carry the framework skills.
          test "G-EMIT report records framework-skills-present under sdd and none for every profile" {
              let report = readValidationReport ()
              for p in profiles do
                  Expect.stringContains report (sprintf "sdd/%s: framework-skills-present=ok" p) (sprintf "sdd/%s skills present" p)
                  Expect.stringContains report (sprintf "none/%s: framework-skills-present=ok" p) (sprintf "none/%s skills present" p)
          }

          // G-CATALOG (FR-005/FR-006, env-free): the docs/skillist-reference.md catalog is emitted from
          // a spec-kit-gated source (the named lifecycle-workspace exception), NOT the ungated base
          // copyOnly list — so it is suppressed under sdd/none and never dangles.
          test "G-CATALOG skillist-reference.md is spec-kit-gated, not ungated on the base source" {
              use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
              let sources = doc.RootElement.GetProperty("sources")
              let str (s: JsonElement) (prop: string) =
                  match s.TryGetProperty prop with
                  | true, v -> elemStr v
                  | _ -> ""
              let arr (s: JsonElement) (prop: string) =
                  match s.TryGetProperty prop with
                  | true, a -> [ for e in a.EnumerateArray() -> elemStr e ]
                  | _ -> []
              // the base source (target ./) must NOT emit the catalog (it is excluded there).
              let baseSource =
                  sources.EnumerateArray() |> Seq.find (fun s -> str s "source" = "template/base/" && str s "target" = "./" && (str s "condition" = ""))
              Expect.isTrue
                  (List.contains "docs/skillist-reference.md" (arr baseSource "exclude"))
                  "base source must exclude docs/skillist-reference.md"
              Expect.isFalse
                  (List.contains "docs/skillist-reference.md" (arr baseSource "copyOnly"))
                  "base source must NOT list docs/skillist-reference.md in copyOnly"
              // exactly one spec-kit-gated source re-emits the catalog (copyOnly, no sourceName rewrite).
              let catalogSources =
                  sources.EnumerateArray()
                  |> Seq.filter (fun s ->
                      List.contains "docs/skillist-reference.md" (arr s "include")
                      || str s "target" = "docs/skillist-reference.md")
                  |> Seq.toList
              Expect.equal catalogSources.Length 1 "exactly one source re-emits the catalog"
              let catalog = catalogSources.[0]
              Expect.stringContains (str catalog "condition") SPEC_KIT_COND "the catalog source is spec-kit-gated"
              Expect.isTrue
                  (List.contains "docs/skillist-reference.md" (arr catalog "copyOnly"))
                  "the catalog is copyOnly (governance tokens preserved verbatim)"
          }

          // G-CATALOG (report-backed): no emitted catalog lists absent skills.
          test "G-CATALOG report records catalog-dangling: none" {
              let report = readValidationReport ()
              Expect.stringContains report "catalog-dangling: none" "no scaffold emits a dangling catalog"
          }

          // G-NODANGLE-SYMB (FR-007): every template/product-skills/<id> directory is wired by a source.
          // Feature 223 wired fs-gg-symbology, so there is no longer any intentionally-unwired directory.
          test "G-NODANGLE-SYMB no product-skill directory is silently unwired; symbology is vendored" {
              let productSkillsDir = repositoryPath "template/product-skills"
              let onDisk =
                  Directory.EnumerateDirectories productSkillsDir
                  |> Seq.map Path.GetFileName
                  |> Set.ofSeq
              let wired = frameworkSkillSources () |> List.map (fun s -> s.Id) |> Set.ofList
              let unwired = Set.difference onDisk wired
              // every product-skill directory is now wired — the unwired set is empty.
              Expect.equal unwired Set.empty "no product-skill directory is left unwired (symbology vendored in Feature 223)"
              let report = readValidationReport ()
              Expect.stringContains report "symbology: vendored" "symbology status is explicitly resolved as vendored"
          }
        ]
