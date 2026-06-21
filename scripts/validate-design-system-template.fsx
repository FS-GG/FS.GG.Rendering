// Feature 128 (Workstream F, F3) — generated-product design-system validation regenerator.
//
// Two responsibilities, mirroring the repo's report-gate + env-gated-live-run pattern
// (GeneratedConsumerValidationTests for the always-on assert; FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE
// for the heavy live op):
//
//   * ALWAYS (no env flag): the US2 policy-verdict CORE. For each accepted `designSystem` value it
//     resolves the recorded policy via the real F2 engine (`ColorPolicy.byName`), evaluates the
//     shared design-system pairing catalog, and compares the rendered verdicts to the committed
//     oracle `docs/reports/color-policy-<v>.md`. This needs NO `dotnet new` and proves the choice
//     selects POLICY, not a palette (≥1 pairing diverges; the certify-where-WCAG-fails verdict
//     carries Ant authority — no overclaim).
//
//   * ENV-GATED (FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1): the heavy live loop. For each accepted value
//     it scaffolds a product (`dotnet new fs-gg-ui --designSystem <v>`), proves `wcag` is
//     byte-identical to a no-value scaffold (`diff-vs-today=none`) / `ant` records `policy:"ant"`,
//     runs a real `dotnet build`, then writes the committed validation report asserted by the
//     always-on gate (Feature128DesignSystemTemplateTests).
//
// The F2 engine is `module internal ColorPolicy` (no `.fsi`); an `fsx` cannot borrow the
// `Controls.Tests` InternalsVisibleTo grant, so the engine source closure is `#load`-ed here to
// compile into THIS script's own assembly — same-assembly access, no IVT needed. The pairing
// catalog is reproduced from Feature127ColorPolicyTests.fs verbatim (same tokens) so the rendered
// report byte-matches the committed F2 oracle, keeping a single source of truth.
//
// Usage:
//   dotnet fsi scripts/validate-design-system-template.fsx                       # verdict-core self-check only
//   FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1 dotnet fsi scripts/validate-design-system-template.fsx   # + live scaffold/build + write report

#load "../src/Scene/Scene.fs"
#load "../src/ColorPolicy/Contrast.fs"
#load "../src/ColorPolicy/ColorPolicy.fs"
#load "../src/DesignSystem/DesignTokensExt.fs"

open System
open System.Diagnostics
open System.IO
open System.Text
open FS.GG.UI.Scene
open FS.GG.UI.Color
open FS.GG.UI.DesignSystem

// ---- repo layout -----------------------------------------------------------------------------

let repoRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then
            dir
        else
            match Directory.GetParent dir |> Option.ofObj with
            | Some p -> find p.FullName
            | None -> failwith "Could not locate repository root (FS.GG.Rendering.slnx)."

    find __SOURCE_DIRECTORY__

let repoPath (rel: string) =
    Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let reportRelPath =
    "specs/128-design-system-template-param/readiness/design-system-template-validation.md"

// ---- the shared design-system pairing catalog (reproduced from Feature127ColorPolicyTests) ----
// Same tokens / order as the F2 test catalog, so `renderReport` byte-matches the committed oracle.
// `open FS.GG.UI.Color` (after Scene) brings Role's cases into scope, so bare Text/GraphicOrUi/
// Decorative bind to Role, not SceneNode.Text.

let private pairing name fg bg role : ColorPolicy.Pairing =
    { Name = name
      Foreground = fg
      Background = bg
      Role = role }

let private catalog =
    [ pairing "text-on-canvas" DesignTokensExt.Alias.Light.textDefault DesignTokensExt.Alias.Light.surfaceCanvas Text
      pairing "text-on-surface" DesignTokensExt.Alias.Light.textDefault DesignTokensExt.Map.Light.colorBgContainer Text
      pairing
          "muted-text-on-surface"
          DesignTokensExt.Alias.Light.textSecondary
          DesignTokensExt.Map.Light.colorBgContainer
          Text
      pairing "primary-fg-on-surface" DesignTokensExt.Seed.colorPrimary DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing "success-fg-on-surface" DesignTokensExt.Seed.colorSuccess DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing "warning-fg-on-surface" DesignTokensExt.Seed.colorWarning DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing "error-fg-on-surface" DesignTokensExt.Seed.colorError DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing "info-fg-on-surface" DesignTokensExt.Seed.colorInfo DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing
          "primary-hover-fg-on-surface"
          DesignTokensExt.Map.Light.colorPrimaryHover
          DesignTokensExt.Map.Light.colorBgContainer
          GraphicOrUi
      pairing
          "decorative-hairline-on-surface"
          DesignTokensExt.Map.Light.colorBorder
          DesignTokensExt.Map.Light.colorBgContainer
          Decorative ]

// ---- engine-facing helpers -------------------------------------------------------------------

/// The divergent pairing the spec calls out (SC-004): Fail under wcag, Aa under ant.
let private divergentPairing = "primary-hover-fg-on-surface"

let private resolvePolicy (recorded: string) =
    match ColorPolicy.byName recorded with
    | Ok p -> p
    | Result.Error e -> failwithf "recorded policy %A did not resolve: %s" recorded e

let private authorityToken (a: ColorPolicy.Authority) =
    match a with
    | ColorPolicy.WcagCertified -> "WcagCertified"
    | ColorPolicy.AntExpectation -> "AntExpectation"

/// The disclosure column for one pairing (mirrors ColorPolicy.renderReport's private disclosure:
/// out-of-scope / indeterminate take precedence over the verdict).
let private disclosure (r: ColorPolicy.PairingResult) =
    match r.Outcome with
    | ColorPolicy.OutOfScope -> "out-of-scope"
    | ColorPolicy.Indeterminate -> "indeterminate"
    | _ ->
        match r.Verdict with
        | Aaa -> "Aaa"
        | Aa -> "Aa"
        | AaLarge -> "AaLarge"
        | Fail -> "Fail"
        | Exempt -> "Exempt"
        | Verdict.Indeterminate -> "indeterminate"

let private resultFor policy name =
    ColorPolicy.evaluate policy catalog |> List.find (fun r -> r.Pairing = name)

/// `overall` rendered as the report token.
let private overallToken policy =
    if ColorPolicy.overall (ColorPolicy.evaluate policy catalog) then "PASS" else "FAIL"

// ---- coverage source: the template's own designSystem choice set (FR-009/SC-006/TP-7) ---------
// Parsed straight out of .template.config/template.json so a new accepted value cannot ship
// unvalidated. Deliberately a tiny hand-parse (no JSON dep in fsx); fails loudly if the block
// cannot be located.

let private enumerateDesignSystemChoices () =
    let json = File.ReadAllText(repoPath ".template.config/template.json")
    let marker = "\"designSystem\""
    let mi = json.IndexOf(marker, StringComparison.Ordinal)
    if mi < 0 then failwith "designSystem symbol not found in template.json"
    // bound the search to this symbol's object (up to the next top-level symbol or the symbols end)
    let choicesIdx = json.IndexOf("\"choices\"", mi, StringComparison.Ordinal)
    if choicesIdx < 0 then failwith "designSystem.choices not found in template.json"
    let arrStart = json.IndexOf('[', choicesIdx)
    let arrEnd = json.IndexOf(']', arrStart)
    if arrStart < 0 || arrEnd < 0 then failwith "designSystem.choices array malformed"
    let body = json.Substring(arrStart, arrEnd - arrStart)
    // collect each "choice": "<value>" in declaration order
    let token = "\"choice\""
    let rec loop i acc =
        let ci = body.IndexOf(token, i, StringComparison.Ordinal)
        if ci < 0 then
            List.rev acc
        else
            let colon = body.IndexOf(':', ci)
            let q1 = body.IndexOf('"', colon + 1)
            let q2 = body.IndexOf('"', q1 + 1)
            let value = body.Substring(q1 + 1, q2 - q1 - 1)
            loop (q2 + 1) (value :: acc)
    let choices = loop 0 []
    if List.isEmpty choices then failwith "designSystem has no choices"
    choices

// ---- US2 verdict core (always runs; needs no dotnet new) --------------------------------------
// T013/T014/T015: resolve each recorded policy, compare the rendered verdicts to the committed
// oracle, and assert the divergence + no-overclaim disclosure.

let private assertTrue cond msg =
    if not cond then failwithf "VERDICT-CORE FAIL: %s" msg

let private verifyVerdictCore (values: string list) =
    for v in values do
        let policy = resolvePolicy v
        // (T014) the rendered report must byte-match the committed F2 oracle — single source of truth.
        let oraclePath = repoPath (sprintf "docs/reports/color-policy-%s.md" v)
        assertTrue (File.Exists oraclePath) (sprintf "oracle %s missing" oraclePath)
        let live = ColorPolicy.renderReport policy catalog
        let committed = File.ReadAllText oraclePath
        assertTrue (live = committed) (sprintf "%s: rendered verdicts drifted from committed oracle" v)

    // (T014) per-value overall + authority match today's verdicts.
    let wcag = resolvePolicy "wcag"
    let ant = resolvePolicy "ant"
    assertTrue (overallToken wcag = "FAIL") "wcag overall must be FAIL"
    assertTrue (authorityToken wcag.Authority = "WcagCertified") "wcag authority must be WcagCertified"
    assertTrue (overallToken ant = "PASS") "ant overall must be PASS"
    assertTrue (authorityToken ant.Authority = "AntExpectation") "ant authority must be AntExpectation"

    // (T015) divergence + no-overclaim on the called-out pairing.
    let wHover = resultFor wcag divergentPairing
    let aHover = resultFor ant divergentPairing
    assertTrue (disclosure wHover = "Fail") (sprintf "%s must be Fail under wcag" divergentPairing)
    assertTrue (disclosure aHover = "Aa") (sprintf "%s must be Aa under ant" divergentPairing)
    assertTrue (aHover.AuthorityNote = Some "ant: not WCAG-certified") "ant must carry the no-overclaim note on the divergent pairing"

    printfn "verdict-core OK: %s; divergent %s wcag=%s ant=%s; no-overclaim note present"
        (String.concat ", " values) divergentPairing (disclosure wHover) (disclosure aHover)

// ---- live scaffold + build helpers (env-gated only) -------------------------------------------

let private runProc (workDir: string) (exe: string) (args: string list) =
    let psi = ProcessStartInfo(exe)
    psi.WorkingDirectory <- workDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add
    use proc = Process.Start psi
    let out = proc.StandardOutput.ReadToEnd()
    let err = proc.StandardError.ReadToEnd()
    proc.WaitForExit()
    proc.ExitCode, out, err

/// Recursively list a scaffold tree as (relativePath, sha256) for a byte-identical comparison.
let private treeFingerprint (root: string) =
    use sha = System.Security.Cryptography.SHA256.Create()
    Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
    |> Seq.map (fun f -> Path.GetRelativePath(root, f).Replace('\\', '/'), f)
    // ignore build artifacts so a post-scaffold build cannot perturb the byte comparison.
    |> Seq.filter (fun (rel, _) -> not (rel.Contains "/bin/" || rel.Contains "/obj/" || rel.StartsWith "bin/" || rel.StartsWith "obj/"))
    |> Seq.map (fun (rel, f) -> rel, sha.ComputeHash(File.ReadAllBytes f) |> Convert.ToHexString)
    |> Seq.sortBy fst
    |> Seq.toList

// Every scaffold uses the SAME product name so the wcag-vs-no-value byte comparison reflects
// POLICY, not a differing project name; only the output directory differs per case.
let private productName = "Demo"

let private scaffold (tmpRoot: string) (designSystem: string option) (outSubdir: string) =
    let outDir = Path.Combine(tmpRoot, outSubdir)
    if Directory.Exists outDir then Directory.Delete(outDir, true)
    let args =
        // dotnet new derives the CLI option from the symbol name verbatim: the `designSystem`
        // symbol surfaces as `--designSystem` (not kebab `--design-system`). A hyphenated symbol
        // name would break the `(designSystem == "ant")` conditional sources, so the symbol stays
        // camelCase and the option is `--designSystem`.
        [ "new"; "fs-gg-ui"; "--name"; productName; "-o"; outDir ]
        @ (match designSystem with Some v -> [ "--designSystem"; v ] | None -> [])

    let psi = ProcessStartInfo("dotnet")
    psi.WorkingDirectory <- repoRoot
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add
    use proc = Process.Start psi
    // drain the pipes asynchronously so a full buffer can never deadlock the child.
    let outTask = proc.StandardOutput.ReadToEndAsync()
    let errTask = proc.StandardError.ReadToEndAsync()

    // `dotnet new` writes the complete product tree, then (in some environments) spins forever on a
    // trailing post-action and never exits. Guard with a bounded wait that finishes the instant the
    // tree has fully materialised (file count stabilises) and then force-kills the stuck post-action.
    // A genuine template failure exits non-zero before any tree is written and is surfaced loudly.
    let productProj = Path.Combine(outDir, "src", productName, productName + ".fsproj")
    let treeComplete () =
        File.Exists(Path.Combine(outDir, "Directory.Build.props"))
        && File.Exists productProj
        && (match designSystem with
            | Some "ant" -> File.Exists(Path.Combine(outDir, "design-system.json"))
            | _ -> true)
    let countFiles () =
        if Directory.Exists outDir then
            Directory.EnumerateFiles(outDir, "*", SearchOption.AllDirectories) |> Seq.length
        else
            0

    let sw = Stopwatch.StartNew()
    let mutable prev = -1
    let mutable stableTicks = 0
    while not proc.HasExited && sw.Elapsed.TotalSeconds < 900.0 && stableTicks < 2 do
        System.Threading.Thread.Sleep 3000
        let c = countFiles ()
        if treeComplete () && c = prev && c > 100 then
            stableTicks <- stableTicks + 1
        else
            stableTicks <- 0
        prev <- c

    if not proc.HasExited then
        // tree settled (or we hit the ceiling) — stop the spinning post-action.
        (try proc.Kill true with _ -> ())

    if proc.HasExited && proc.ExitCode <> 0 && not (treeComplete ()) then
        failwithf "dotnet new failed for %A (exit %d):\n%s\n%s" designSystem proc.ExitCode outTask.Result errTask.Result

    if not (treeComplete ()) then
        failwithf "dotnet new did not materialise a complete %A tree within the time budget" designSystem

    outDir

/// One row of the validation report.
type private ValueResult =
    { Value: string
      Build: string
      Marker: string // "diff-vs-today=none" for wcag, "record=ant" for ant, generic otherwise
      Overall: string
      Authority: string }

let private validateValueLive (tmpRoot: string) (noValueTree: (string * string) list) (v: string) =
    let dir = scaffold tmpRoot (Some v) (sprintf "value-%s" v)

    let marker =
        if v = "wcag" then
            // (T019) byte-identical to the no-value scaffold ⇒ diff-vs-today=none. Fingerprint BEFORE
            // building so build artifacts cannot perturb the comparison (bin/obj are excluded too).
            let tree = treeFingerprint dir
            if tree = noValueTree then "diff-vs-today=none"
            else failwithf "wcag scaffold differs from the no-value scaffold (SC-001 broken)"
        else
            // (T019) ant records its policy
            let recordPath = Path.Combine(dir, "design-system.json")
            if not (File.Exists recordPath) then failwithf "%s scaffold missing design-system.json" v
            let record = File.ReadAllText recordPath
            if record.Contains "\"policy\"" && record.Contains "\"ant\"" then sprintf "record=%s" v
            else failwithf "%s design-system.json does not record policy:\"ant\": %s" v record

    // (T020) real build — the scaffold ships no solution file, so build every project it emits.
    let projects =
        Directory.EnumerateFiles(dir, "*.fsproj", SearchOption.AllDirectories)
        |> Seq.filter (fun p ->
            let n = p.Replace('\\', '/')
            not (n.Contains "/bin/" || n.Contains "/obj/"))
        |> Seq.sort
        |> Seq.toList
    if List.isEmpty projects then failwithf "%s scaffold emitted no .fsproj to build" v
    for proj in projects do
        let code, out, err = runProc dir "dotnet" [ "build"; proj ]
        if code <> 0 then failwithf "dotnet build failed for %s (%s):\n%s\n%s" v proj out err
    let build = "build=pass"

    // (T020) wire in the recorded-policy verdicts
    let policy = resolvePolicy v
    { Value = v
      Build = build
      Marker = marker
      Overall = overallToken policy
      Authority = authorityToken policy.Authority }

// ---- report rendering (T021) ------------------------------------------------------------------
// Deterministic: declaration-order values, invariant culture, no clock/random. Emits the exact
// contract tokens asserted by Feature128DesignSystemTemplateTests.

let private renderReport (values: string list) (results: ValueResult list) =
    let wcag = resolvePolicy "wcag"
    let ant = resolvePolicy "ant"
    let wHover = resultFor wcag divergentPairing
    let aHover = resultFor ant divergentPairing
    let note = defaultArg aHover.AuthorityNote "ant: not WCAG-certified"
    let byValue v = results |> List.find (fun r -> r.Value = v)
    let w = byValue "wcag"
    let a = byValue "ant"

    let sb = StringBuilder()
    let line (s: string) = sb.Append(s).Append('\n') |> ignore
    line "# Design-System Template Validation — Feature 128 (F3)"
    line ""
    line "> GENERATED — do not edit. Regenerate via:"
    line "> FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1 dotnet fsi scripts/validate-design-system-template.fsx"
    line ""
    line (sprintf "covered-values: %s" (String.concat ", " values))
    line (sprintf "wcag: %s %s overall=%s authority=%s" w.Build w.Marker w.Overall w.Authority)
    line (sprintf "ant: %s %s overall=%s authority=%s" a.Build a.Marker a.Overall a.Authority)
    line (sprintf "divergent-pairing: %s wcag=%s ant=%s" divergentPairing (disclosure wHover) (disclosure aHover))
    line (sprintf "no-overclaim-note: %s" note)
    line "result: pass"
    sb.ToString()

// ---- entry point ------------------------------------------------------------------------------

let private main () =
    let values = enumerateDesignSystemChoices ()
    printfn "covered designSystem values (from template.json): %s" (String.concat ", " values)

    // US2 verdict core runs unconditionally (proves policy-not-palette; needs no dotnet new).
    verifyVerdictCore values

    let liveGate = Environment.GetEnvironmentVariable "FS_GG_RUN_DESIGN_SYSTEM_VALIDATION" = "1"
    if not liveGate then
        printfn "Live scaffold/build + report generation is env-gated."
        printfn "Set FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1 to scaffold each value, build it, and write the report."
        0
    else
        let tmpRoot = Path.Combine(Path.GetTempPath(), "fs-gg-design-system-validation")
        if Directory.Exists tmpRoot then Directory.Delete(tmpRoot, true)
        Directory.CreateDirectory tmpRoot |> ignore

        // baseline: a no-value scaffold (same product name) is the byte-identical reference for the
        // wcag comparison — fingerprinted before any build so artifacts cannot perturb it.
        let baselineDir = scaffold tmpRoot None "baseline"
        let noValueTree = treeFingerprint baselineDir

        // process EVERY enumerated value; coverage guard (FR-009).
        let results = values |> List.map (validateValueLive tmpRoot noValueTree)
        let processed = results |> List.map (fun r -> r.Value) |> Set.ofList
        let missing = values |> List.filter (fun v -> not (processed.Contains v))
        if not (List.isEmpty missing) then
            failwithf "coverage guard: accepted values not processed: %s" (String.concat ", " missing)

        let report = renderReport values results
        let reportPath = repoPath reportRelPath
        Directory.CreateDirectory(Path.GetDirectoryName reportPath) |> ignore
        File.WriteAllText(reportPath, report)
        printfn "wrote %s" reportRelPath
        printfn "%s" report
        0

exit (main ())
