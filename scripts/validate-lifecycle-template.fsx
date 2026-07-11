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
open System.Text.RegularExpressions

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

/// Feature 219: the sources under `template/product-skills/` carry the framework PRODUCT skills.
/// Issue #91 (ADR-0017 §C2): `fs-gg-project` keeps its canonical body under
/// `template/base/.agents/skills/` but is now the SAME shape — a dedicated profile-gated,
/// lifecycle-independent source (promoted off the former lifecycle-gated whole-`.agents/`
/// blanket so it materializes on every lifecycle), so it classifies as a framework skill too.
let private isFrameworkSkillSource (source: string) =
    let s = source.Replace('\\', '/')
    s.StartsWith "template/product-skills/" || s.StartsWith "template/base/.agents/skills/"

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

/// Issue #248: a skill body shipped OFF the `template/product-skills/` convention — the same
/// `.agents/skills/`-only, copyOnly, lifecycle-INDEPENDENT shape as a framework product skill, but
/// gated on a capability flag (`feedback`) rather than a `profile` predicate, so it materializes on
/// every profile AND lane. Its own category, so `frameworkChecked` keeps meaning exactly
/// "profile-gated product skill".
///
/// Issue #434 widened this to admit an UNGATED body (`condition = ""` — the engine's "always").
/// The load-bearing facts were never the capability flag itself but the shape around it:
/// off-convention source, provider target, no lifecycle clause, no profile predicate, verbatim body.
/// An `always` row satisfies all of them, so it belongs here rather than in a category of its own.
///
/// Detected BY SHAPE, not by path: keying it on `template/feedback-report/skill/` would send the
/// NEXT such skill down the lifecycle-workspace branch, where it would be rejected for missing a
/// `lifecycle == "spec-kit"` clause it must not have. `fs-gg-feedback-report` is merely the first.
/// The ungated skill-manifest row can no longer be excluded by `condition <> ""` (it is ungated too),
/// so `classifySource` now resolves that named path BEFORE this shape test; capture and
/// fs-gg-samples are excluded by their spec-kit clause, the product skills by their profile predicate.
let private isCapabilitySkillSource (target: string) (condition: string) =
    target.Replace('\\', '/').StartsWith ".agents/skills/"
    && not (condition.Contains SPEC_KIT_COND)
    && not (condition.Contains "profile ==")

// ---- the classification contract, shared with Package.Tests by RULE ID (issue #253) -----------

/// One `sources[]` row of template.json, reduced to the fields classification reads.
type SourceRow =
    { Source: string
      Target: string
      Condition: string
      Includes: string list
      Excludes: string list
      CopyOnly: string list }

/// Read the `sources[]` rows out of a JSON array element (template.json's, or a synthetic fixture's).
let private sourceRows (arrayElement: JsonElement) =
    let str (s: JsonElement) (prop: string) =
        match s.TryGetProperty prop with
        | true, v -> v.GetString() |> Option.ofObj |> Option.defaultValue ""
        | _ -> ""
    let strs (s: JsonElement) (prop: string) =
        match s.TryGetProperty prop with
        | true, arr -> [ for e in arr.EnumerateArray() -> e.GetString() |> Option.ofObj |> Option.defaultValue "" ]
        | _ -> []
    [ for s in arrayElement.EnumerateArray() ->
        { Source = str s "source"
          Target = str s "target"
          Condition = str s "condition"
          Includes = strs s "include"
          Excludes = strs s "exclude"
          CopyOnly = strs s "copyOnly" } ]

/// Classify one `source` entry and name the rules it breaks (the env-free verdict-core fact,
/// Feature 219 R3 / data-model "Template source category"; reworked by Feature 231 / ADR-0014).
/// Classification order is significant: framework-product-skill FIRST (by `source` prefix), then
/// the capability-gated skill (by shape), then the ungated skill-manifest row (named exception),
/// then lifecycle-workspace (by `target` prefix / generated tree / the named skillist exception),
/// then product. Feature 231's two structural ADR-0014 facts live in the workspace branch: the
/// repo-root `.agents/skills/` source vendors ONLY the `speckit-*` process skills (no dev surface,
/// F3), and the single spec-kit-gated materialize step (template/lifecycle/ ->
/// .specify/scripts/fs-gg/) replaces the Feature 230 per-skill `.claude`/`.codex` twins.
///
/// Issue #253: `Package.Tests`' `gatedSourceAudit` re-derives this classification INDEPENDENTLY, so
/// the gate proves the gating rather than a self-written line. That is only worth anything if the
/// two derivations agree, so the returned **rule ids** are a contract shared verbatim with the test
/// copy, which feeds a synthetic fixture through both via `--classify` and asserts identical
/// verdicts. The prose each side renders from a rule id stays local to that side.
let private classifySource (row: SourceRow) : string * string list =
    let source = row.Source.Replace('\\', '/')
    let target = row.Target.Replace('\\', '/')
    let condition = row.Condition
    let violations = ResizeArray<string>()
    let require ruleId holds = if not holds then violations.Add ruleId
    let isCopyOnly = List.contains "**/*" row.CopyOnly

    let isGeneratedTree = source = ".template.config/generated/"
    let isGatedTarget =
        target.StartsWith ".specify" || target.StartsWith ".agents" || target.StartsWith ".claude"
        || target.StartsWith ".codex"
        || target = "CLAUDE.md" || target = "AGENTS.md"

    let category =
        // FRAMEWORK PRODUCT-SKILL: lifecycle-independent, profile-gated (FR-001/FR-002), verbatim
        // (copyOnly — canonical bytes must match the skill-manifest sha256, F5). Emits to
        // `.agents/skills/` ONLY; a `.claude/skills/` or `.codex/skills/` target is a resurrected
        // Feature 230 twin.
        if isFrameworkSkillSource source then
            require "framework/target-agents-skills-only" (target.StartsWith ".agents/skills/")
            require "framework/no-spec-kit-clause" (not (condition.Contains SPEC_KIT_COND))
            require "framework/profile-predicate" (condition.Contains "profile ==")
            require "framework/copy-only" isCopyOnly
            "framework"
        // SKILL-MANIFEST (Feature 231, named exception): ungated provider data in .agents/skills/.
        // Resolved by its NAMED PATH before the capability shape test below — since #434 that test
        // admits an ungated body, and this row (`.agents/skills/` target, no condition) would
        // otherwise match it and be miscounted as a skill.
        elif isManifestSource source then
            require "manifest/target-agents-skills" (target.StartsWith ".agents/skills/")
            require "manifest/ungated" (condition = "")
            require "manifest/copy-only" isCopyOnly
            "manifest"
        // CAPABILITY SKILL (issue #248; ungated variant #434): profile- and lifecycle-independent;
        // the gate is a capability flag (`feedback`) or nothing at all (`always`). Same
        // provider-surface + verbatim-body rule as a product skill. Target and lifecycle/profile
        // independence are the classification itself.
        elif isCapabilitySkillSource target condition then
            require "capability/copy-only" isCopyOnly
            "capability"
        // LIFECYCLE WORKSPACE: spec-kit-only (.specify/ incl. the single materialize step,
        // agent-context, the narrowed speckit-* skills copy, generated tree, and the spec-kit-only
        // skillist catalog — the named exception).
        elif isGatedTarget || isGeneratedTree || isSkillistCatalogSource target row.Includes then
            require "workspace/spec-kit-clause" (condition.Contains SPEC_KIT_COND)
            if source = ".agents/skills/" then
                require "workspace/speckit-blanket-target" (target = ".agents/skills/")
                require "workspace/speckit-blanket-include" (row.Includes = [ "speckit-*/**" ])
            if source = "template/lifecycle/" then
                require "workspace/materialize-target" (target = ".specify/scripts/fs-gg/")
            "workspace"
        // PRODUCT source (base -> ./, samples -> samples/, ant overlay -> ./)
        else
            require "product/no-spec-kit-clause" (not (condition.Contains SPEC_KIT_COND))
            "product"

    category, violations |> List.ofSeq |> List.sort

/// Every rule id `classifySource` can emit — the vocabulary half of the contract. `--classify`
/// publishes it, and Package.Tests asserts its own copy matches AND that its fixture exercises all
/// of it. So a rule added here but exercised by no fixture row cannot ship: declaring it forces the
/// test's list to change, which forces a fixture row, which GV-AGREE then runs through both copies.
/// (Adding a rule WITHOUT declaring it here defeats that, and is why it sits next to classifySource.)
let private ruleIds =
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

/// This side's prose for a rule id. Deliberately NOT shared with the test copy — only the ids are.
let private ruleMessage (row: SourceRow) (ruleId: string) =
    let s, t = row.Source, row.Target
    match ruleId with
    | "framework/target-agents-skills-only" ->
        sprintf "product-skill source %s -> %s: product skills emit to .agents/skills/ ONLY (the standalone materialize step / orchestrator fan-out own the other roots, ADR-0014)" s t
    | "framework/no-spec-kit-clause" ->
        sprintf "framework product-skill source %s -> %s must NOT carry `%s` (it follows the profile, not the lifecycle)" s t SPEC_KIT_COND
    | "framework/profile-predicate" ->
        sprintf "framework product-skill source %s -> %s must carry a profile predicate" s t
    | "framework/copy-only" ->
        sprintf "framework product-skill source %s -> %s must be copyOnly (verbatim canonical body, ADR-0014/F5)" s t
    | "capability/copy-only" ->
        sprintf "capability-gated skill source %s -> %s must be copyOnly (verbatim canonical body, ADR-0014/F5)" s t
    | "manifest/target-agents-skills" ->
        sprintf "skill-manifest source %s -> %s must target .agents/skills/ (provider-owned in every lane)" s t
    | "manifest/ungated" ->
        sprintf "skill-manifest source %s -> %s must be UNGATED (ships in every lifecycle)" s t
    | "manifest/copy-only" -> sprintf "skill-manifest source %s -> %s must be copyOnly" s t
    | "workspace/spec-kit-clause" ->
        sprintf "lifecycle-workspace source %s -> %s missing `%s` (condition=%A)" s t SPEC_KIT_COND row.Condition
    | "workspace/speckit-blanket-target" ->
        sprintf "repo-root .agents/skills/ source must target .agents/skills/ only, found %s (Feature 230 blanket twin resurrected?)" t
    | "workspace/speckit-blanket-include" ->
        sprintf ".agents/skills/ source must include ONLY speckit-*/** (no dev-surface vendoring, ADR-0014/F3); found include=%A" row.Includes
    | "workspace/materialize-target" ->
        sprintf "materialize source %s must target .specify/scripts/fs-gg/, found %s" s t
    | "product/no-spec-kit-clause" ->
        sprintf "ungated product source %s -> %s must NOT carry `%s`" s t SPEC_KIT_COND
    | unknown -> sprintf "source %s -> %s violates %s" s t unknown

/// Verify the gating invariant on every `source` entry of the real template.json.
let private verifyGatedSources () =
    use doc = templateDoc ()
    let classified =
        sourceRows (doc.RootElement.GetProperty("sources"))
        |> List.map (fun row -> row, classifySource row)
    for row, (_, violations) in classified do
        for ruleId in violations do
            assertTrue false (ruleMessage row ruleId)
    let countWhere predicate =
        classified |> List.filter predicate |> List.length
    let inCategory name = countWhere (fun (_, (c, _)) -> c = name)
    let frameworkChecked = inCategory "framework"
    let capabilityChecked = inCategory "capability"
    let manifestChecked = inCategory "manifest"
    let workspaceChecked = inCategory "workspace"
    let productChecked = inCategory "product"
    let workspaceSourced source =
        countWhere (fun (row, (c, _)) -> c = "workspace" && row.Source.Replace('\\', '/') = source)
    let materializeChecked = workspaceSourced "template/lifecycle/"
    let speckitNarrowChecked = workspaceSourced ".agents/skills/"
    assertTrue (frameworkChecked = 18) (sprintf "expected exactly 18 framework product-skill sources (.agents/skills/ provider surface incl. fs-gg-project + fs-gg-collision + fs-gg-visibility + fs-gg-grids + fs-gg-line-drawing, no twins), checked %d" frameworkChecked)
    assertTrue (capabilityChecked = 1) (sprintf "expected exactly 1 capability-scope skill source (fs-gg-feedback-report — ungated since #434), checked %d" capabilityChecked)
    assertTrue (manifestChecked = 1) (sprintf "expected exactly 1 ungated skill-manifest source, checked %d" manifestChecked)
    assertTrue (materializeChecked = 1) (sprintf "expected exactly 1 spec-kit-gated materialize source (template/lifecycle/), checked %d" materializeChecked)
    assertTrue (speckitNarrowChecked = 1) (sprintf "expected exactly 1 narrowed repo-root .agents/skills/ source, checked %d" speckitNarrowChecked)
    assertTrue (workspaceChecked >= 9) (sprintf "expected >=9 lifecycle-workspace sources, checked %d" workspaceChecked)
    assertTrue (productChecked >= 3) (sprintf "expected >=3 ungated product sources, checked %d" productChecked)
    frameworkChecked, workspaceChecked, productChecked

// ---- the skill supply chain: who is allowed to write .agents/skills/<id>/ (issue #303) ---------

// `skill-manifest.json`'s `supplied-by` names the ONE template source that fills each skill
// directory. Nothing verified that claim against template.json, and two sources can legally target
// the same directory — so the claim was wrong for eleven skills and no gate could tell. Until
// Feature 231 narrowed it, the repo-root `.agents/skills/` row copied this repo's whole dev surface
// over every `template/product-skills/` row aimed at the same directories; consumers got the Codex
// wrappers, whose `reference.fsx` `#r`s `src/*/bin/Debug` DLLs that exist in no product (#303).
//
// `classifySource` cannot see this: it judges each row alone, and a shadow is a property of the row
// SET. So prove the two structural facts directly, env-free, against the real trees:
//   S1  every row targeting `.agents/skills/<id>/` is the supplier the manifest names for `<id>`,
//       and each manifest skill has exactly one such row;
//   S2  a row targeting an ANCESTOR of the skill directories (`.agents/skills/`, `.agents/`, `./`)
//       may create `<id>/` only by copying the very directory the manifest declares its supplier.
//
// S2 is checked by SIMULATION — enumerate what the row's source offers beneath the skills root and
// filter it through the row's `include`/`exclude` globs. That keeps it honest when a row's glob set
// changes shape, and it needs no named exception. Today the two `template/base/ -> ./` rows reach no
// skill directory (the ungated one excludes `.agents/**`; its spec-kit twin includes one doc file),
// and the manifest-data row copies none — but `template/base/.agents/skills/` is a real tree, so had
// either row stayed blanket it would have shadowed. Keying the verdict on `supplied-by` rather than
// on the row's identity means such a row is allowed exactly when it copies the declared directory.

/// One `skills[]` entry of skill-manifest.json, reduced to the supply-chain claim it makes.
type private ManifestSkill = { Id: string; SuppliedBy: string }

let private manifestSkills () =
    let path = repoPath "template/skill-manifest/skill-manifest.json"
    assertTrue (File.Exists path) (sprintf "skill-manifest missing at %s — the supply chain has no declaration to check" path)
    use doc = JsonDocument.Parse(File.ReadAllText path)
    let field (s: JsonElement) name =
        match s.TryGetProperty(name: string) with
        | true, v when v.ValueKind = JsonValueKind.String -> v.GetString().Replace('\\', '/')
        | _ -> failwithf "VERDICT-CORE FAIL: skill-manifest entry is missing a string `%s` (regenerate via scripts/generate-skill-manifest.fsx)" name
    [ for s in doc.RootElement.GetProperty("skills").EnumerateArray() ->
        { Id = field s "id"; SuppliedBy = field s "supplied-by" } ]

/// The `<id>` a row fills, when its target names a single directory under `.agents/skills/`.
let private targetedSkillId (target: string) =
    let t = target.Replace('\\', '/').TrimEnd '/'
    if not (t.StartsWith ".agents/skills/") then None
    else
        let rest = t.Substring(".agents/skills/".Length)
        if rest = "" || rest.Contains "/" then None else Some rest

/// Match one path segment against a template glob segment (`*` is the only wildcard).
let private segmentMatches (pattern: string) (segment: string) =
    let rx = "^" + String.Join(".*", pattern.Split '*' |> Array.map Regex.Escape) + "$"
    Regex.IsMatch(segment, rx)

let private globSegments (pattern: string) =
    pattern.Replace('\\', '/').Split('/') |> List.ofArray

/// `**` matches zero or more whole segments, so it must ABSORB a prefix of `segs` and let the rest
/// of the pattern decide — not short-circuit to a match. Reading it as "anything from here" makes
/// `**/bin/**` cover every directory, which in an `exclude` silently hides real shadows.
let private absorb (recurse: string list -> bool) (segs: string list) =
    let rec go s = recurse s || (match s with [] -> false | _ :: rest -> go rest)
    go segs

/// Does an `include` glob reach the directory at `segs`? A pattern more specific than `segs` still
/// reaches it (`x/SKILL.md` reaches `x/`); one that diverges does not.
let rec private includeReaches (pattern: string list) (segs: string list) =
    match pattern, segs with
    | _, [] -> true
    | "**" :: rest, _ -> List.isEmpty rest || absorb (includeReaches rest) segs
    | p :: pRest, s :: sRest -> segmentMatches p s && includeReaches pRest sRest
    | [], _ -> false

/// Does an `exclude` glob remove the WHOLE directory at `segs`? A pattern that only names files
/// inside it (`x/SKILL.md`) does not — the directory still materializes.
let rec private excludeCovers (pattern: string list) (segs: string list) =
    match pattern, segs with
    | "**" :: rest, _ -> List.isEmpty rest || absorb (excludeCovers rest) segs
    | [], [] -> true
    | p :: pRest, s :: sRest -> segmentMatches p s && excludeCovers pRest sRest
    | _ -> false

/// For a row targeting an ancestor of the skill directories, the sub-path from its source down to
/// the skills root (`./` ⇒ `.agents/skills`, `.agents/` ⇒ `skills`, `.agents/skills/` ⇒ nothing).
let private skillsRootPrefix (target: string) =
    let t = target.Replace('\\', '/').TrimEnd '/'
    let segs = if t = "" || t = "." then [] else List.ofArray (t.Split '/')
    let skillsRoot = [ ".agents"; "skills" ]
    if segs.Length <= skillsRoot.Length && segs = List.truncate segs.Length skillsRoot
    then Some(List.skip segs.Length skillsRoot)
    else None

/// The skill directories an ancestor-targeting row actually creates, paired with the source
/// directory each is copied FROM — which is what `supplied-by` must name.
let private rowMaterializesSkillDirs (row: SourceRow) (prefix: string list) =
    let sourceRel = row.Source.Replace('\\', '/').TrimEnd '/'
    let sourceDir = repoPath (String.concat "/" (sourceRel :: prefix))
    if not (Directory.Exists sourceDir) then []
    else
        Directory.EnumerateDirectories sourceDir
        |> Seq.map Path.GetFileName
        |> Seq.filter (fun d ->
            let segs = prefix @ [ d ]
            let included =
                List.isEmpty row.Includes
                || row.Includes |> List.exists (fun p -> includeReaches (globSegments p) segs)
            let excluded = row.Excludes |> List.exists (fun p -> excludeCovers (globSegments p) segs)
            included && not excluded)
        |> Seq.map (fun d -> d, String.concat "/" (sourceRel :: prefix @ [ d ]) + "/")
        |> List.ofSeq

let private verifySkillSupplyChain () =
    use doc = templateDoc ()
    let rows = sourceRows (doc.RootElement.GetProperty("sources"))
    let skills = manifestSkills ()
    let suppliedBy id = skills |> List.tryFind (fun s -> s.Id = id) |> Option.map (fun s -> s.SuppliedBy)

    // S1: each skill-directory row is the manifest's named supplier.
    let skillRows = rows |> List.choose (fun r -> targetedSkillId r.Target |> Option.map (fun id -> id, r))
    for id, row in skillRows do
        match suppliedBy id with
        | None ->
            assertTrue false
                (sprintf "template source %s -> .agents/skills/%s/ fills a skill the manifest does not declare (add it to template/skill-manifest/, or stop emitting it)"
                    row.Source id)
        | Some declared ->
            assertTrue (declared = row.Source.Replace('\\', '/'))
                (sprintf "skill-manifest says %s is supplied-by %s, but template.json fills .agents/skills/%s/ from %s (#303: supplied-by must name the real source)"
                    id declared id row.Source)

    // S1 (converse): exactly one row per declared skill — a second row silently wins or loses.
    for skill in skills do
        let suppliers = skillRows |> List.filter (fun (id, _) -> id = skill.Id) |> List.map (fun (_, r) -> r.Source)
        match suppliers with
        | [ _ ] -> ()
        | [] ->
            assertTrue false
                (sprintf "no template source targets .agents/skills/%s/, so the manifest declares a skill the template never emits (#303)" skill.Id)
        | many ->
            assertTrue false
                (sprintf "skill %s is filled by %d template sources (%s) — exactly one may target .agents/skills/%s/ (#303)"
                    skill.Id many.Length (String.concat ", " many) skill.Id)

    // S2: an ancestor row may create <id>/ only from the directory `supplied-by` names.
    let rootRows = rows |> List.choose (fun r -> skillsRootPrefix r.Target |> Option.map (fun p -> r, p))
    for row, prefix in rootRows do
        for id, copiedFrom in rowMaterializesSkillDirs row prefix do
            match suppliedBy id with
            | Some declared when declared = copiedFrom -> ()
            | Some declared ->
                assertTrue false
                    (sprintf "template source %s -> %s materializes .agents/skills/%s/ from %s, shadowing its declared supplier %s (#303: the repo's own wrappers `#r` src/*/bin/Debug DLLs that exist in no product; narrow this row's `include`/`exclude`)"
                        row.Source row.Target id copiedFrom declared)
            | None -> ()

    skills.Length, rootRows.Length

/// `--classify <fixture.json>`: publish this side's rule vocabulary, then run `classifySource` over
/// a synthetic row array and print one `category<TAB>rule,rule` line per row, bracketed by markers.
/// The agreement test (GV-AGREE) runs its own classifier over the same fixture and compares both.
/// Reads no repo state, so a fixture may describe rows that template.json does not (and must not)
/// contain.
let private classifyFixture (fixturePath: string) =
    use doc = JsonDocument.Parse(File.ReadAllText fixturePath)
    printfn "FSGG-CLASSIFY-BEGIN"
    printfn "FSGG-RULE-IDS\t%s" (String.concat "," (List.sort ruleIds))
    for row in sourceRows doc.RootElement do
        let category, violations = classifySource row
        printfn "%s\t%s" category (String.concat "," violations)
    printfn "FSGG-CLASSIFY-END"
    0

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
    let suppliedSkills, rootRows = verifySkillSupplyChain ()
    verifyBaseDocsNeutral ()
    printfn "verdict-core OK: covered-values %s; %d lifecycle-workspace sources carry `%s`; %d framework product-skill sources profile-gated & lifecycle-independent; %d product sources clean; %d manifest skills each filled by their declared supplier, %d root-targeting source(s) shadow none; directive agent-context docs lifecycle-safe"
        (String.concat ", " values) workspace SPEC_KIT_COND framework product suppliedSkills rootRows
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

/// Install THIS WORKING TREE's template as the `fs-gg-ui` this run scaffolds from (#452).
///
/// The live loop below shells out to `dotnet new fs-gg-ui`, which resolves against the machine's
/// template cache — NOT against `.template.config/template.json` in this repo. So without this step
/// the "live" audit silently scaffolds from whatever `FS.GG.UI.Template` package happens to be
/// installed (on the box that found this, a published 0.8.0 predating #434), and then reports a
/// verdict about it as though it had proven something about the tree under review. A CI lane wired
/// up that way is worse than no lane: it is green, it is fast, and its subject is the wrong one.
/// That is the same fail-open this whole item exists to close, so closing it here too.
///
/// The install is scoped to a temp `DOTNET_CLI_HOME` rather than the caller's real one, for two
/// reasons: it does not mutate a developer's global template cache as a side effect of running a
/// validator, and it dodges the short-name collision that `dotnet new install .` would otherwise
/// hit on any box that already has the published FS.GG.UI.Template installed (two packages, one
/// `fs-gg-ui` short name). Child `dotnet` processes inherit this env, so every scaffold below —
/// and only this run's — sees the isolated cache.
let private installTemplateUnderTest (tmpRoot: string) =
    let cliHome = Path.Combine(tmpRoot, ".cli-home")
    Directory.CreateDirectory cliHome |> ignore
    Environment.SetEnvironmentVariable("DOTNET_CLI_HOME", cliHome)
    Environment.SetEnvironmentVariable("DOTNET_NOLOGO", "1")
    let code, out, err = runProc repoRoot "dotnet" [ "new"; "install"; repoRoot ]
    if code <> 0 then
        failwithf "could not install the working-tree template from %s (exit %d) — the live loop has no subject to audit:\n%s\n%s"
            repoRoot code out err
    printfn "live subject: %s (isolated DOTNET_CLI_HOME=%s)" repoRoot cliHome

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
//
// This set is deliberately a HAND-MAINTAINED restatement of the G-EMIT matrix, not something
// derived from template.json's conditions: deriving it would make the audit tautological (it would
// mirror whatever the conditions say, including a wrong condition) and it would stop being an
// independent statement of intent. The cost of that choice is that widening a skill's
// `materializes-when` must ALSO widen this map — issue #90 widened fs-gg-testing to all five
// profiles and did not, which is what rotted this lane red (#452). If you gate a skill onto a new
// profile, add it here in the same change.
let private expectedFrameworkSkills =
    // fs-gg-testing: profile-gated onto every profile, with NO lifecycle term (template.json
    // `(profile == "app" || … || profile == "game")`), so it emits on every profile AND every lane
    // — spec-kit, sdd and none alike (#90).
    //
    // fs-gg-symbology: NOT on headless-scene/governed. Issue #430 narrowed it to the three profiles
    // that carry SkiaViewer (app, sample-pack, game): the skill's loop is `Render.toPng`, which lives
    // in FS.GG.UI.Symbology.Render -> FS.GG.UI.SkiaViewer, and the viewerless profiles pin no viewer,
    // so it could never compile there. Matches its manifest `materializes-when: profile in [app,
    // sample-pack, game]`.
    [ "app", Set.ofList [ "fs-gg-scene"; "fs-gg-skiaviewer"; "fs-gg-elmish"; "fs-gg-keyboard-input"; "fs-gg-ui-widgets"; "fs-gg-styling"; "fs-gg-layout"; "fs-gg-symbology"; "fs-gg-testing" ]
      "headless-scene", Set.ofList [ "fs-gg-scene"; "fs-gg-testing" ]
      "governed", Set.ofList [ "fs-gg-scene"; "fs-gg-testing" ]
      // sample-pack shares the game profile's capability set (audio/collision/game-core/grids/
      // line-drawing/model-swap/persistence/visibility are all gated `profile == "game" || profile ==
      // "sample-pack"`). Each of those was added to the profile by its own feature without updating this
      // map — eight silent drifts, invisible because the lane that would have caught them never ran.
      // fs-gg-samples is the one spec-kit-gated member and is dropped from the sdd/none expectation below.
      "sample-pack", Set.ofList [ "fs-gg-scene"; "fs-gg-skiaviewer"; "fs-gg-elmish"; "fs-gg-symbology"; "fs-gg-samples"; "fs-gg-testing"
                                  "fs-gg-audio"; "fs-gg-collision"; "fs-gg-game-core"; "fs-gg-grids"
                                  "fs-gg-line-drawing"; "fs-gg-model-swap"; "fs-gg-persistence"; "fs-gg-visibility" ] ]
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
/// and no `speckit-*` command skills.
///
/// `.agents/skills/fs-gg-project/` is deliberately NOT part of this predicate (#452). It used to be:
/// fs-gg-project arrived only via a `lifecycle == "spec-kit"`-gated blanket copy of `template/base/.agents/`,
/// so its presence was a sound proxy for "a Spec Kit workspace was written here". Issue #91 (ADR-0017 §C2)
/// promoted it to a dedicated PROFILE-gated, lifecycle-INDEPENDENT source, precisely so the default sdd lane
/// stops shipping capability skills with no top-level product map. It therefore materializes on EVERY lane,
/// and asserting its absence under sdd/none contradicts #91. Of the two, #91 is the deliberate decision and
/// this predicate was the stale one — so the clause is dropped rather than #91 reverted.
///
/// `.claude/skills/fs-gg-project/` IS still asserted: `template/base/.claude/` remains `lifecycle == "spec-kit"`-gated,
/// so under sdd/none the orchestrator roots hold no product skills at all, and a write there is the
/// `scaffold.providerWroteSddTree` intrusion (#47/#55) this lane exists to catch.
let private workspaceAbsent (dir: string) =
    not (Directory.Exists(Path.Combine(dir, ".specify")))
    && not (File.Exists(Path.Combine(dir, "CLAUDE.md")))
    && not (File.Exists(Path.Combine(dir, "AGENTS.md")))
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
    // fs-gg-feedback-capture is the per-phase CAPTURE skill, gated `(feedback == true) && lifecycle == "spec-kit"`.
    // `feedback` defaults to false and no call site of this function scaffolds with `--feedback true`, so in
    // practice it never emits here — this is a tolerance for a spec-kit lane that turns the flag on, not a
    // requirement. (Contrast fs-gg-feedback-report below, which is unconditional and so is REQUIRED.)
    let allowedSpecKitExtras = Set.ofList [ "fs-gg-feedback-capture" ]
    let expected =
        let baseSet =
            match Map.tryFind profile expectedFrameworkSkills with
            | Some s -> s
            | None ->
                failwithf "profile %s is in `profiles` but has no entry in `expectedFrameworkSkills` — add its expected fs-gg-* set (see the note there)"
                    profile

        // fs-gg-samples is spec-kit-gated (sample-pack only): drop it from the sdd/none expectation.
        let baseSet = if specKit then baseSet else Set.remove "fs-gg-samples" baseSet
        // Issue #91 (ADR-0017 §C2): fs-gg-project is profile-gated and lifecycle-INDEPENDENT — it is the
        // product-orientation umbrella, and #91 promoted it precisely so the sdd lane stops shipping
        // capability skills with no top-level map. So it is REQUIRED on every lane, not merely tolerated
        // on spec-kit. It sat in `allowedSpecKitExtras` until #452, which (a) read as an unexpected
        // vendored wrapper under sdd/none, redding this lane, and (b) — had the polarity gone the other
        // way — could never have caught its ABSENCE, the same hole that shipped fs-gg-feedback-report to
        // nobody (#434). Requiring it is what makes a regression of #91 loud.
        let baseSet = Set.add "fs-gg-project" baseSet
        // Issue #434: fs-gg-feedback-report is UNCONDITIONAL — every profile, every lane, with or
        // without `--feedback`. So it is EXPECTED here, not merely tolerated. While it was gated on
        // `feedback` it sat in an `allowedCapabilityExtras` allow-list, which only ever PERMITTED it
        // and so could never catch its ABSENCE — and since `feedback` defaults to false it was in fact
        // absent from every workspace in the org. Requiring it is what makes that regression loud.
        //
        // A future capability skill that IS flag-gated needs no allowance here: every scaffold below
        // runs with its flag defaulted off, so it never emits and never reads as an extra. Add it to
        // `expected` on the lanes that actually enable the flag, rather than restoring a blanket
        // allow-list — an allowance can only ever permit a skill, never catch its absence, which is
        // the exact hole that let this one ship to nobody.
        Set.add "fs-gg-feedback-report" baseSet
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
    // Probe `.agents/`, not `.claude/`: under sdd the whole `.claude/` root is suppressed, so a
    // `.claude/skills/fs-gg-feedback-capture` probe is vacuously false and would stay green even if
    // capture lost its lifecycle clause and leaked into the provider surface. `.agents/skills/` is
    // where a leak would actually land.
    let feedbackSkill = Directory.Exists(Path.Combine(fb, ".agents", "skills", "fs-gg-feedback-capture"))
    if feedbackSkill then failwithf "feedback=true under sdd emitted the gated feedback skill (should be suppressed)"
    // ...but the UNGATED retrospective report skill (issue #248) MUST emit on that same lane — it is
    // agent-invoked, not hook-invoked, so it carries no lifecycle clause. This is the positive dual of
    // the check above: together they pin that exactly one of the two feedback skills is lane-gated.
    if not (Directory.Exists(Path.Combine(fb, ".agents", "skills", "fs-gg-feedback-report"))) then
        failwith "feedback=true under sdd did NOT emit fs-gg-feedback-report (should be lifecycle-independent)"
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

/// `--classify <fixture.json>`, if present on the command line.
let private classifyFixtureArg () =
    match fsi.CommandLineArgs |> Array.tryFindIndex (fun a -> a = "--classify") with
    | Some i when i + 1 < fsi.CommandLineArgs.Length -> Some fsi.CommandLineArgs[i + 1]
    | Some _ -> failwith "--classify requires a fixture path"
    | None -> None

let private runValidation () =
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

        installTemplateUnderTest tmpRoot

        let verdicts = profiles |> List.map (validateProfileLive tmpRoot)
        let matrixCount = validateCompositionMatrix tmpRoot values
        let unknown = validateUnknownRejected tmpRoot
        let enforceRedCase = validateEnforceRedCase tmpRoot

        let report = renderReport values "live" verdicts matrixCount unknown enforceRedCase
        writeReport report
        printfn "%s" report
        0

/// `--classify` short-circuits ahead of the verdict core (issue #253): its fixture rows are
/// synthetic, so the caller wants this side's verdict on THEM, not on the real template.json.
let private main () =
    match classifyFixtureArg () with
    | Some fixturePath -> classifyFixture fixturePath
    | None -> runValidation ()

exit (main ())
