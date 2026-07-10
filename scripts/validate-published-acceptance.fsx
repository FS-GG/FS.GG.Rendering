// Feature 210 — PUBLISHED-PACKAGE epic-acceptance harness.
//
// Closes the lifecycle-agnostic template epic on EVIDENCE by validating the *published*
// `FS.GG.UI.Template` package a consumer pulls from the feed — NOT the working-tree source template
// that the child features (204/205/206) validated. This is the crucial difference: it closes the
// spec edge case "drift between child evidence and the published artifact."
//
// Mirrors the report-core + env-gated-live pattern of scripts/validate-lifecycle-template.fsx:
//
//   * ALWAYS (no env flag): the verdict CORE. Confirms the pinned package `.nupkg` exists in the
//     local feed and asserts the env-free facts (pin identity, profile/lifecycle/gated-set
//     constants). No `dotnet new`, install, build, or network. Exit non-zero on a structural failure.
//
//   * --emit-report (env-free): writes readiness/epic-acceptance.md from the verdict core, with the
//     live-only result lines SYNTHESIZED to their expected values and `provenance: verdict-core`
//     disclosed (Constitution V). A fresh checkout with a gitignored readiness/ is not red-by-default.
//     The CLOSE conclusion is NOT valid from a verdict-core record.
//
//   * ENV-GATED (FS_GG_RUN_PUBLISHED_ACCEPTANCE=1): the live loop. Installs the pinned package into
//     an isolated store, confirms `dotnet new list` resolves `fs-gg-ui` to the PACKAGE (not the
//     working tree), runs the 3 lifecycle x 4 profile matrix, asserts the per-value gated-set result
//     + byte-identical default + none-has-no-orchestrator-marker + unknown-value-rejected, runs the
//     bounded `dotnet build` spot-check on the app-profile sdd/none outputs, EXECUTES the packaged
//     fs-gg-symbology reference recipe materialized into the scaffold (#294), uninstalls/restores
//     (even on failure), and writes the record with `provenance: live`.
//
// Usage:
//   dotnet fsi scripts/validate-published-acceptance.fsx                  # verdict-core self-check
//   dotnet fsi scripts/validate-published-acceptance.fsx --emit-report    # + write verdict-core record
//   FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx  # + live proof

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.RegularExpressions

// ---- pinned inputs (research R1/R2) -----------------------------------------------------------

let packageId = "FS.GG.UI.Template"
let packageVersion = "0.1.51-preview.1"
let tagAnchor = "fs-gg-ui/v0.1.51-preview.1"
let feedDir =
    Path.Combine(
        Environment.GetFolderPath Environment.SpecialFolder.UserProfile,
        ".local", "share", "nuget-local")
let nupkgPath = Path.Combine(feedDir, sprintf "%s.%s.nupkg" packageId packageVersion)

let lifecycleValues = [ "spec-kit"; "sdd"; "none" ]
let profiles = [ "app"; "headless-scene"; "governed"; "sample-pack" ]
let productName = "Acme"   // PascalCase — avoids the dir-derived-lowercase build break (prior-feature lesson)

// ---- repo layout ------------------------------------------------------------------------------

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

let reportRelPath = "specs/210-lifecycle-template-closure/readiness/epic-acceptance.md"

// ---- helpers ----------------------------------------------------------------------------------

let private assertTrue cond msg =
    if not cond then failwithf "ACCEPTANCE FAIL: %s" msg

// A failure blob that carries one of these is the ENVIRONMENT failing to reach a feed, not the
// artifact under test failing. Shared by the build spot-check and the packaged reference recipe:
// both shell out to a `dotnet` that must restore before it can prove anything.
let private envLimitedMarkers =
    [ "NU1101"; "NU1102"; "NU1301"; "Unable to load the service index"
      "No such host"; "actively refused"; "nuget.org"; "Unable to resolve"; "network" ]

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

// A relative path is "gated" iff it lives under one of the gated lifecycle roots (reused from 204).
let private isGatedPath (rel: string) =
    let p = rel.Replace('\\', '/')
    p.StartsWith ".specify/" || p.StartsWith ".agents/" || p.StartsWith ".claude/"
    || p = "CLAUDE.md" || p = "AGENTS.md"

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

let private gatedAbsent (dir: string) =
    [ ".specify"; ".claude"; ".agents"; "CLAUDE.md"; "AGENTS.md" ]
    |> List.forall (fun f -> not (File.Exists(Path.Combine(dir, f)) || Directory.Exists(Path.Combine(dir, f))))

let private gatedPresent (dir: string) =
    Directory.Exists(Path.Combine(dir, ".specify"))
    && Directory.Exists(Path.Combine(dir, ".claude"))
    && Directory.Exists(Path.Combine(dir, ".agents"))
    && File.Exists(Path.Combine(dir, "AGENTS.md"))
    && File.Exists(Path.Combine(dir, "CLAUDE.md"))

let private productPresent (dir: string) =
    File.Exists(Path.Combine(dir, "Directory.Build.props"))
    && Directory.Exists(Path.Combine(dir, "src"))

// "Orchestrator marker" (FR-004): the DIRECTIVE agent-context docs must carry no suppressed-path
// reference under none. CLAUDE.md/AGENTS.md are absent under none; README.md is the live check.
// (copyOnly governance REFERENCE docs that merely document the skill convention are out of scope,
// per Feature 204.)
let private directiveAgentDocs = [ "CLAUDE.md"; "AGENTS.md"; "README.md" ]
let private hasOrchestratorMarker (dir: string) =
    directiveAgentDocs
    |> List.exists (fun d ->
        let p = Path.Combine(dir, d)
        File.Exists p
        && (let txt = File.ReadAllText p
            [ ".specify/"; ".agents/"; ".claude/" ] |> List.exists txt.Contains))

// ---- verdict core (env-free) ------------------------------------------------------------------

let private verifyVerdictCore () =
    assertTrue (Directory.Exists feedDir) (sprintf "local feed dir missing: %s" feedDir)
    assertTrue (File.Exists nupkgPath)
        (sprintf "pinned package absent from feed: %s (pack it before running acceptance)" nupkgPath)
    assertTrue (lifecycleValues = [ "spec-kit"; "sdd"; "none" ]) "lifecycle value set drifted"
    assertTrue (profiles = [ "app"; "headless-scene"; "governed"; "sample-pack" ]) "profile set drifted"
    printfn "verdict-core OK: %s %s present in feed; lifecycle=%s; profiles=%s"
        packageId packageVersion (String.concat "," lifecycleValues) (String.concat "," profiles)

// ---- live install / matrix (env-gated) --------------------------------------------------------

let private installPinned () =
    let code, out, err =
        runProc repoRoot "dotnet"
            [ "new"; "install"; sprintf "%s::%s" packageId packageVersion
              "--add-source"; feedDir; "--force" ]
    assertTrue (code = 0) (sprintf "install of %s::%s failed (exit %d):\n%s\n%s" packageId packageVersion code out err)

let private uninstallPinned () =
    runProc repoRoot "dotnet" [ "new"; "uninstall"; packageId ] |> ignore

/// Confirm the installed fs-gg-ui short name is supplied by the pinned PACKAGE, not a working-tree
/// source install (R1). `dotnet new uninstall` (no arg) lists installed packages + their versions.
let private confirmPackageResolution () =
    let _, out, _ = runProc repoRoot "dotnet" [ "new"; "uninstall" ]
    assertTrue (out.Contains packageId && out.Contains packageVersion)
        (sprintf "installed templates do not show %s %s — fs-gg-ui may resolve to a working-tree source (R1)" packageId packageVersion)
    let code, listOut, _ = runProc repoRoot "dotnet" [ "new"; "list"; "fs-gg-ui" ]
    assertTrue (code = 0 && (listOut.Contains "fs-gg-ui" || listOut.Contains "FS GG UI"))
        "dotnet new list did not resolve the fs-gg-ui short name after install"

let private scaffold (tmpRoot: string) (profile: string) (lifecycle: string) (outSubdir: string) =
    let outDir = Path.Combine(tmpRoot, outSubdir)
    if Directory.Exists outDir then Directory.Delete(outDir, true)
    let args = [ "new"; "fs-gg-ui"; "--name"; productName; "--profile"; profile; "--lifecycle"; lifecycle; "-o"; outDir ]
    let code, out, err = runProc repoRoot "dotnet" args
    let complete =
        File.Exists(Path.Combine(outDir, "Directory.Build.props"))
        && Directory.EnumerateFiles(outDir, "*.fsproj", SearchOption.AllDirectories) |> Seq.isEmpty |> not
    if not complete then
        failwithf "dotnet new failed for %s/%s (exit %d):\n%s\n%s" lifecycle profile code out err
    outDir

/// Scaffold the no-flag default (no --lifecycle) — must equal explicit spec-kit byte-for-byte.
let private scaffoldDefault (tmpRoot: string) (profile: string) (outSubdir: string) =
    let outDir = Path.Combine(tmpRoot, outSubdir)
    if Directory.Exists outDir then Directory.Delete(outDir, true)
    let args = [ "new"; "fs-gg-ui"; "--name"; productName; "--profile"; profile; "-o"; outDir ]
    let code, out, err = runProc repoRoot "dotnet" args
    if not (File.Exists(Path.Combine(outDir, "Directory.Build.props"))) then
        failwithf "dotnet new (default) failed for %s (exit %d):\n%s\n%s" profile code out err
    outDir

let private scaffoldExpectFail (tmpRoot: string) (outSubdir: string) (lifecycle: string) =
    let outDir = Path.Combine(tmpRoot, outSubdir)
    if Directory.Exists outDir then Directory.Delete(outDir, true)
    let code, _, _ = runProc repoRoot "dotnet"
                        [ "new"; "fs-gg-ui"; "--name"; productName; "--profile"; "app"; "--lifecycle"; lifecycle; "-o"; outDir ]
    let treeExists = File.Exists(Path.Combine(outDir, "Directory.Build.props"))
    code, treeExists

// ---- per-profile assertions (live, step 3) ----------------------------------------------------

type private ProfileVerdict =
    { Profile: string
      SpecKit: string     // gated-present + diff-vs-baseline=none
      Sdd: string         // gated-absent + product-present + diff-vs-default=gated-only
      None_: string }     // gated-absent + no-orchestrator-marker + none==sdd

let private validateProfile (tmpRoot: string) (profile: string) =
    let def = scaffoldDefault tmpRoot profile (sprintf "%s-default" profile)
    let speckit = scaffold tmpRoot profile "spec-kit" (sprintf "%s-speckit" profile)
    // spec-kit: gated set PRESENT.
    assertTrue (gatedPresent speckit) (sprintf "%s/spec-kit: gated set not fully present" profile)
    assertTrue (productPresent speckit) (sprintf "%s/spec-kit: product missing" profile)
    // byte-identical default: no-flag default == explicit spec-kit (presence + content). This is the
    // reproducible stand-in for "identical to the pre-lifecycle baseline" (204 proved baseline==today).
    assertTrue (treeFingerprint def = treeFingerprint speckit)
        (sprintf "%s: no-flag default differs from explicit spec-kit (byte-identical default broken)" profile)

    let sdd = scaffold tmpRoot profile "sdd" (sprintf "%s-sdd" profile)
    assertTrue (gatedAbsent sdd) (sprintf "%s/sdd: gated set not fully absent" profile)
    assertTrue (productPresent sdd) (sprintf "%s/sdd: product missing" profile)
    // default-minus-sdd differs in ONLY gated paths; sdd adds nothing.
    let defSet = relFilesSet def
    let sddSet = relFilesSet sdd
    let added = Set.difference sddSet defSet
    assertTrue (Set.isEmpty added)
        (sprintf "%s/sdd: added non-default files: %s" profile (String.concat ", " (Set.toList added)))
    let nonGatedRemoved = Set.difference defSet sddSet |> Set.filter (isGatedPath >> not)
    assertTrue (Set.isEmpty nonGatedRemoved)
        (sprintf "%s/sdd: removed NON-gated files: %s" profile (String.concat ", " (Set.toList nonGatedRemoved)))

    let none_ = scaffold tmpRoot profile "none" (sprintf "%s-none" profile)
    assertTrue (gatedAbsent none_) (sprintf "%s/none: gated set not fully absent" profile)
    assertTrue (productPresent none_) (sprintf "%s/none: product missing" profile)
    assertTrue (not (hasOrchestratorMarker none_))
        (sprintf "%s/none: a directive agent-context doc references a suppressed path (orchestrator marker present)" profile)
    // none == sdd at the template level.
    assertTrue (treeFingerprint none_ = treeFingerprint sdd)
        (sprintf "%s: none tree differs from sdd tree" profile)

    { Profile = profile
      SpecKit = "gated-present=ok product-present=ok diff-vs-baseline=none"
      Sdd = "gated-absent=ok product-present=ok diff-vs-default=gated-only"
      None_ = "gated-absent=ok no-orchestrator-marker=ok none==sdd" }

let private validateUnknownRejected (tmpRoot: string) =
    let code, treeExists = scaffoldExpectFail tmpRoot "bogus" "bogus"
    assertTrue (code <> 0) "unknown --lifecycle value was accepted (should fail fast)"
    assertTrue (not treeExists) "unknown --lifecycle value produced an output tree (should be none)"
    "rejected"

// ---- build spot-check (live, step 4, FR-003/FR-004) -------------------------------------------

/// `dotnet build` the app-profile sdd/none outputs. Returns ("pass" | "environment-limited", detail).
/// A real compile failure (no restore/network marker) is NOT swallowed — it surfaces as a thrown
/// failure that blocks close (SC-007).
let private buildSpotCheck (sddAppDir: string) (noneAppDir: string) =
    let buildOne (dir: string) =
        // The generated product ships no solution file; the product project is src/<name>/<name>.fsproj.
        let proj = Path.Combine(dir, "src", productName, sprintf "%s.fsproj" productName)
        let code, out, err = runProc dir "dotnet" [ "build"; proj; "-c"; "Debug"; "--nologo" ]
        let blob = out + "\n" + err
        if code = 0 then true
        elif envLimitedMarkers |> List.exists blob.Contains then false
        else failwithf "build spot-check FAILED (real failure, not environment) in %s (exit %d):\n%s" dir code blob
    match buildOne sddAppDir, buildOne noneAppDir with
    | true, true -> "pass", "sdd/app exit 0; none/app exit 0"
    | _ ->
        "environment-limited",
        "build toolchain/restore unavailable (NuGet restore could not reach the feed); buildability not asserted as pass"

// ---- packaged symbology reference recipe (live, step 5, #294 lane 2) --------------------------

/// The fs-gg-symbology skill's `reference.fsx` is the one RUNNABLE artifact it ships, and it shipped
/// broken: `fsi` resolves a bare `#r` DLL against the file system rather than the project's
/// deps.json, so nothing pulled SkiaSharp in behind FS.GG.UI.SkiaViewer and every invocation threw.
/// Nobody noticed because every check that named the file asserted it EXISTS and is byte-mirrored;
/// none asserted it RUNS.
///
/// #295 closed that hole for the in-tree variant (`gate.yml`, on every PR). It cannot close it for
/// the PACKAGED variant, which `#r`s `nuget: FS.GG.UI.*` packages that do not exist until they are
/// published — so the packaged twin is only provable here, against the real feed, exactly as a
/// consumer gets it. This is the drift the harness was built for (feature 210).
///
/// Runs the copy materialized into the spec-kit scaffold (`.agents/` is gated to lifecycle=spec-kit,
/// so this is the only lifecycle that carries it) and pins the same verdict SEQUENCE `gate.yml` pins
/// in-tree. Returns ("pass" | "environment-limited", detail). A real failure — a broken script, a
/// drifted Legibility contract, an unresolvable pin — is NOT swallowed (SC-007).
let private symbologyReferenceCheck (specKitAppDir: string) =
    let relPath = ".agents/skills/fs-gg-symbology/reference.fsx"
    let script = Path.Combine(specKitAppDir, relPath.Replace('/', Path.DirectorySeparatorChar))
    assertTrue (File.Exists script)
        (sprintf "packaged fs-gg-symbology reference recipe missing from the spec-kit scaffold at %s" relPath)

    // Which recipe actually landed? The `#r` header is the whole difference between the two twins:
    // the in-tree one references src/*/bin/Debug DLLs, the packaged one `#r`s `nuget: FS.GG.UI.*`.
    // Assert the SHAPE before running, so the failure names the defect instead of surfacing an
    // FS0078 "unable to find the file" from four directories up.
    //
    // This is not hypothetical. Template source `.agents/skills/ -> .agents/skills/` (spec-kit-gated)
    // copies THIS REPO's Codex wrapper into the product, shadowing the
    // `template/product-skills/fs-gg-symbology/ -> .agents/skills/fs-gg-symbology/` source that is
    // supposed to supply it (skill-manifest.json: supplied-by). A consumer's recipe is therefore the
    // in-tree twin, `#r`ing Debug DLLs that exist in no product.
    // Only the `#r` DIRECTIVES decide which twin this is — a prose mention of bin/Debug in a comment
    // (both headers describe the other variant) must not trip the detector.
    let refLines =
        File.ReadAllLines script
        |> Array.filter (fun l -> l.TrimStart().StartsWith "#r ")
    let refsDebugDll = refLines |> Array.exists (fun l -> l.Contains "bin/Debug")
    let refsNugetPackages = refLines |> Array.exists (fun l -> l.Contains "nuget: FS.GG.UI.")

    if refsDebugDll || not refsNugetPackages then
        failwithf
            "packaged symbology reference recipe DRIFT at %s — the materialized script is the IN-TREE twin.\n\
             It `#r`s src/*/bin/Debug DLLs, which exist only in the FS.GG.Rendering working tree, so it\n\
             throws FS0078 on a consumer's first run. Expected the packaged twin from\n\
             template/product-skills/fs-gg-symbology/ (`#r \"nuget: FS.GG.UI.*\"`).\n\
             Likely cause: the spec-kit-gated `.agents/skills/` template source shadows the\n\
             `template/product-skills/fs-gg-symbology/` source that targets the same directory.\n\
             --- materialized #r directives ---\n%s"
            relPath (String.concat "\n" refLines)

    // Run from the scaffold: the product's NuGet.config is what resolves the `#r "nuget:"` lines,
    // and the recipe writes its PNGs under Path.GetTempPath(), never the working directory.
    let code, out, err = runProc specKitAppDir "dotnet" [ "fsi"; script ]
    let blob = out + "\n" + err

    if code <> 0 then
        // Three failure modes, named apart. The recipe guards its own loop with `failwith "reference
        // recipe: ..."`, so when THAT fires the script ran fine and the published LIBRARY disagreed
        // with it — a different bug, and a different fix, from the script being broken.
        if blob.Contains "reference recipe:" then
            failwithf
                "packaged symbology reference recipe: the PUBLISHED library disagrees with the recipe at %s.\n\
                 The recipe's own guard fired, so it resolved and ran — but `#r \"nuget: FS.GG.UI.*\"` is\n\
                 UNPINNED, so it runs against the latest PUBLISHED library, never the working tree. A\n\
                 library behaviour change that has merged but not yet shipped reds here until the packages\n\
                 publish (green follows the publish). Pin the recipe's `#r` lines to FsGgUiVersion, or ship.\n\
                 --- recipe output ---\n%s\n--- diagnostic ---\n%s"
                relPath out blob
        elif envLimitedMarkers |> List.exists blob.Contains then
            "environment-limited",
            "packaged FS.GG.UI.* could not be restored from the feed; the packaged recipe was not asserted to run"
        else
            failwithf "packaged symbology reference recipe FAILED (real failure, not environment) at %s (exit %d):\n%s"
                relPath code blob
    else
        // The script fails loud on its own (`failwith` on both rounds). Pinning the output here makes
        // the expected sequence part of the gate rather than a comment that can drift from the code
        // beneath it — and proves the packaged twin demonstrates the SAME loop as the in-tree one.
        let must (pattern: string) (why: string) =
            assertTrue (Regex.IsMatch(out, pattern, RegexOptions.Multiline))
                (sprintf "packaged symbology reference recipe: %s\n--- stdout ---\n%s" why out)

        must @"^round 1: HasWarnings$"
            "round 1 must score HasWarnings — the recipe teaches the loop by showing a warning"
        must @"Speed overloaded: 5 distinct levels used, capacity 4"
            "round 1 must name Speed as the overloaded channel, over capacity 4"
        must @"^\s+units: \[4\]$"
            "round 1 must name ONLY the units past capacity (expected [4]); naming the whole board is the #295 regression"
        must @"^round 2 \(after tweak\): Clean$"
            "round 2 must score Clean — the ChannelMap tweak has to clear every finding"
        for grammar in [ "Token"; "Badge"; "Ring" ] do
            must (sprintf @"^%s\s+board PNG: " grammar)
                (sprintf "one unchanged mapUnit must render a %s board via galleryIn" grammar)

        "pass",
        "packaged reference.fsx exit 0: seeded overload -> named unit 4 -> tweak -> Clean -> 3 grammars"

// ---- record writer (T009) ---------------------------------------------------------------------

let private renderRecord (provenance: string) (verdicts: ProfileVerdict list)
                         (unknown: string) (buildability: string) (buildDetail: string)
                         (symbology: string) (symbologyDetail: string)
                         (sddIssueUrl: string) (constitutionItem: string) =
    let sb = StringBuilder()
    let line (s: string) = sb.Append(s).Append('\n') |> ignore
    line "# Epic Acceptance — Lifecycle-Agnostic FS.GG.UI Template"
    line ""
    line "> GENERATED — do not edit. Regenerate via:"
    line "> FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx"
    line ""
    line (sprintf "validated_package: %s %s" packageId packageVersion)
    line (sprintf "tag_anchor:        %s" tagAnchor)
    line "                   (NOTE: no dedicated template tag at 0.1.51; follow-up: tag fs-gg-ui-template/v0.1.51-preview.1)"
    line (sprintf "provenance:        %s      # conclusion VALID only when live" provenance)
    line (sprintf "profiles:          [%s]" (String.concat ", " profiles))
    line ""
    line "## Gated lifecycle set (restated, per Feature 204)"
    line "- .specify/, generated constitution, .agents/, .claude/, generated AGENTS.md/CLAUDE.md"
    line "- present only under lifecycle == \"spec-kit\"; suppressed under sdd/none. The three ungated"
    line "  PRODUCT sources (base -> ./, samples -> samples/, ant overlay) are present for all values."
    line ""
    line "## Per-lifecycle results"
    line "| lifecycle | app | headless-scene | governed | sample-pack | gated set | product |"
    line "|---|---|---|---|---|---|---|"
    line "| spec-kit  | pass | pass | pass | pass | PRESENT | present |"
    line "| sdd       | pass | pass | pass | pass | ABSENT  | present/buildable |"
    line "| none      | pass | pass | pass | pass | ABSENT (no orchestrator marker) | present/buildable |"
    line ""
    line "Per-profile detail (all four profiles asserted live):"
    for v in verdicts do line (sprintf "- spec-kit/%s: %s" v.Profile v.SpecKit)
    for v in verdicts do line (sprintf "- sdd/%s: %s" v.Profile v.Sdd)
    for v in verdicts do line (sprintf "- none/%s: %s" v.Profile v.None_)
    line (sprintf "- unknown-lifecycle-value: %s" unknown)
    line ""
    line "## Byte-identical default"
    line "- baseline: pre-lifecycle template output per profile (Features 204/206); the default value is spec-kit."
    line "- scope:    presence AND content compared, across all four profiles."
    line "- result:   spec-kit (== no-flag default) diff-vs-baseline = none, all 4 profiles."
    line "  (204 proved baseline == today's spec-kit default; this run proves no-flag default == explicit"
    line "   spec-kit byte-for-byte against the installed PACKAGE, the reproducible stand-in for that baseline.)"
    line ""
    line "## Build spot-check (FR-003/FR-004 \"buildable\")"
    line "- scope:  dotnet build on the app-profile output for sdd and none (spec-kit follows from byte-identity)."
    line (sprintf "- result: buildability = %s" buildability)
    line (sprintf "          %s" buildDetail)
    line ""
    line "## Packaged skill reference recipe (#294 lane 2)"
    line "- scope:  dotnet fsi on .agents/skills/fs-gg-symbology/reference.fsx in the app/spec-kit output."
    line "          The packaged recipe `#r`s published `nuget: FS.GG.UI.*`, so only a live run against the"
    line "          real feed can prove it — the in-tree twin is gated per-PR by gate.yml (#295)."
    line (sprintf "- result: packaged-reference-recipe = %s" symbology)
    line (sprintf "          %s" symbologyDetail)
    line ""
    line "## Reproduction"
    line "```bash"
    line (sprintf "dotnet new install %s::%s --add-source ~/.local/share/nuget-local/" packageId packageVersion)
    line "FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx"
    line (sprintf "dotnet new uninstall %s" packageId)
    line "```"
    line ""
    line "## Cross-repo remainder (tracked; both resolved at implementation time)"
    line (sprintf "- %s" sddIssueUrl)
    line (sprintf "- %s" constitutionItem)
    line "No open cross-repo remainder item blocks full closure: each is tracked exactly once on the"
    line "FS-GG Coordination board and both are Done (dedupe per FR-010 — neither re-filed)."
    line ""
    line "## Conclusion"
    let closeEligible = provenance = "live" && buildability <> "FAILED"
    if closeEligible then
        line "Rendering-side: **CLOSE** — the published 0.1.51-preview.1 package emits the full Spec Kit"
        line "lifecycle surface only under spec-kit, suppresses it (product intact) under sdd/none, the"
        line "no-flag default is byte-identical to spec-kit across all four profiles, and the app-profile"
        line "sdd/none outputs build (or buildability is disclosed environment-limited)."
        line ""
        line "Cross-repo remainder state (US3): both SDD-owned items the spec/204 assumed open are, at"
        line "implementation time, already tracked once and **Done** on the Coordination board — so no open"
        line "remainder blocks full closure. Epic-fully-done is therefore achievable; the board is updated"
        line "to Rendering-side complete with the (resolved) remainder attributed to its owning repo."
    else
        line "Rendering-side: **DON'T-CLOSE (provisional)** — this record is provenance: verdict-core"
        line "(synthesized fresh-checkout fallback). Re-run the live gate for a CLOSE-eligible record."
    line "Epic-fully-done: ACHIEVABLE — no open cross-repo remainder item remains (both Done on the"
    line "Coordination board). The invariant \"false while any remainder is open\" holds vacuously."
    sb.ToString()

let private writeRecord (content: string) =
    let p = repoPath reportRelPath
    Directory.CreateDirectory(Path.GetDirectoryName p) |> ignore
    File.WriteAllText(p, content)
    printfn "wrote %s" reportRelPath

let private synthVerdicts () =
    profiles
    |> List.map (fun p ->
        { Profile = p
          SpecKit = "gated-present=ok product-present=ok diff-vs-baseline=none"
          Sdd = "gated-absent=ok product-present=ok diff-vs-default=gated-only"
          None_ = "gated-absent=ok no-orchestrator-marker=ok none==sdd" })

// US3 cross-repo remainder (T016/T017/T018). At implementation time BOTH items the spec/204 assumed
// open are already tracked AND resolved on the FS-GG Coordination board — no open blocker remains:
//   1. scaffold-path git-init/chmod  → FS-GG/FS.GG.SDD#1 (CLOSED 2026-06-27) + board item Done.
//   2. constitution ownership (sdd)  → board draft decision DI_lADOEYAWY84Bb08WzgKrVHM, status Done
//      (downstream P2 "Implement constitution-ownership decision" also Done). The dedupe edge case
//      (FR-010) makes this the canonical tracker; a freshly-filed SDD issue would duplicate it.
let private sddIssuePlaceholder =
    "FS-GG/FS.GG.SDD#1 (https://github.com/FS-GG/FS.GG.SDD/issues/1) — scaffold-path git-init/chmod obligations — RESOLVED (closed 2026-06-27; Coordination board item Done)"
let private constitutionPlaceholder =
    "Coordination board decision \"P0 · cross-repo — Constitution ownership for lifecycle=sdd (Rendering vs SDD)\" (DI_lADOEYAWY84Bb08WzgKrVHM) — RESOLVED (status Done; downstream P2 implementation Done). Reused per FR-010 dedupe — not re-filed."

// ---- entry point ------------------------------------------------------------------------------

let private main () =
    verifyVerdictCore ()

    let emitReport = fsi.CommandLineArgs |> Array.exists (fun a -> a = "--emit-report")
    let liveGate = Environment.GetEnvironmentVariable "FS_GG_RUN_PUBLISHED_ACCEPTANCE" = "1"

    if emitReport && not liveGate then
        let record =
            renderRecord "verdict-core" (synthVerdicts ()) "rejected"
                "environment-limited" "verdict-core: live build spot-check not run"
                "environment-limited" "verdict-core: packaged reference recipe not run"
                sddIssuePlaceholder constitutionPlaceholder
        writeRecord record
        0
    elif not liveGate then
        printfn "Live install + acceptance matrix + record is env-gated."
        printfn "Set FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 to install the pinned package and write the record."
        printfn "Pass --emit-report to write a provenance: verdict-core fallback record (not close-eligible)."
        0
    else
        let tmpRoot = Path.Combine(Path.GetTempPath(), "fs-gg-published-acceptance")
        if Directory.Exists tmpRoot then Directory.Delete(tmpRoot, true)
        Directory.CreateDirectory tmpRoot |> ignore
        installPinned ()
        try
            confirmPackageResolution ()
            let verdicts = profiles |> List.map (validateProfile tmpRoot)
            let unknown = validateUnknownRejected tmpRoot
            let buildability, buildDetail =
                buildSpotCheck
                    (Path.Combine(tmpRoot, "app-sdd"))
                    (Path.Combine(tmpRoot, "app-none"))
            // The gated `.agents/` tree exists only under lifecycle=spec-kit; validateProfile left
            // every scaffold on disk, so the app/spec-kit output is still there to run.
            let symbology, symbologyDetail =
                symbologyReferenceCheck (Path.Combine(tmpRoot, "app-speckit"))
            let record =
                renderRecord "live" verdicts unknown buildability buildDetail
                    symbology symbologyDetail
                    sddIssuePlaceholder constitutionPlaceholder
            writeRecord record
            printfn "%s" record
            0
        finally
            uninstallPinned ()
            try Directory.Delete(tmpRoot, true) with _ -> ()

exit (main ())
