// Feature 207 — FS.GG.UI BOM / metapackage consumer-validation regenerator.
//
// Two layers, mirroring validate-lifecycle-template.fsx / validate-design-system-template.fsx:
//
//   * Env-free verdict core (always): re-derives the membership-parity facts directly from the
//     repo (nuspec dependency-id set == discovered packable FS.GG.UI.* set; single [$version$]
//     token; exact-bracket form) and writes the report with `provenance: verdict-core`. If parity
//     is broken the core throws — no report, loud failure.
//
//   * Live proof (FS_GG_RUN_BOM_CONSUMER_SMOKE=1): additionally packs the whole solution to a
//     throwaway feed at a chosen V, restores a CLEAN consumer whose only FS.GG.UI declaration is
//     `FS.GG.UI@V` (asserts every resolved FS.GG.UI.* is at V and it builds), restores TWICE from a
//     cleared cache (asserts an identical resolved set), then forces a member to Y<V and Y>V and
//     asserts the exact `[V]` bracket makes the deviation loud. Writes `provenance: live`.
//
// MECHANISM NOTE (observed, corrects research R1): the exact `[V]` bracket makes deviation
// DETECTED in BOTH directions — NU1605 (downgrade, Y<V) and NU1608 (outside-constraint, Y>V) — but
// these are NuGet WARNINGS by default; nearest-wins then builds a mixed graph silently. They become
// a BLOCKING restore/build failure only when the consumer treats those codes as errors
// (`WarningsAsErrors=NU1605;NU1608` / `TreatWarningsAsErrors` — the FS.GG repo's and the governed
// fs-gg-ui template's default posture). So "loud deviation" is the exact-bracket DETECTION plus the
// recommended warnings-as-errors policy, not an unconditional NU1107 — R1 named NU1605/NU1107; the
// observed codes are NU1605/NU1608 and loudness is consumer-policy-gated.

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions

let repoRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName
let repo (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let live = Environment.GetEnvironmentVariable "FS_GG_RUN_BOM_CONSUMER_SMOKE" = "1"

// ---- shell helper -----------------------------------------------------------------------------
let run (workDir: string) (exe: string) (args: string list) =
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
    proc.ExitCode, out + err

// ---- env-free verdict core: membership parity -------------------------------------------------
let nuspecPath = repo "src/Meta/FS.GG.UI.nuspec"

/// Discovered packable FS.GG.UI.* members from src/** (template / IsPackable=false excluded).
let discoveredMembers () =
    Directory.GetFiles(repo "src", "*.fsproj", SearchOption.AllDirectories)
    |> Array.choose (fun proj ->
        let t = File.ReadAllText proj
        let m name = Regex.Match(t, sprintf "<%s>([^<]*)</%s>" name name)
        let pid = let g = (m "PackageId") in if g.Success then g.Groups.[1].Value.Trim() else ""
        let packable = let g = (m "IsPackable") in g.Success && g.Groups.[1].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
        if packable && pid.StartsWith("FS.GG.UI.", StringComparison.Ordinal) then Some pid else None)
    |> Set.ofArray

/// (dependency ids, every-version-is-the-single-token?, every-version-is-exact-bracket?)
let nuspecDeps () =
    let text = File.ReadAllText nuspecPath
    let deps =
        Regex.Matches(text, "<dependency\\s+id=\"([^\"]+)\"\\s+version=\"([^\"]+)\"")
        |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
        |> Seq.toList
    let ids = deps |> List.map fst |> Set.ofList
    let allToken = deps |> List.forall (fun (_, v) -> v = "[$version$]")
    let allExact = deps |> List.forall (fun (_, v) -> v.StartsWith "[" && v.EndsWith "]" && not (v.Contains ","))
    ids, allToken, allExact, deps.Length

let members = discoveredMembers ()
let depIds, allToken, allExact, depCount = nuspecDeps ()

let parityOk = depIds = members && allToken && allExact
if not parityOk then
    eprintfn "PARITY FAILED: nuspec deps %A vs members %A; allToken=%b allExact=%b" depIds members allToken allExact
    exit 1

let noLib = (File.ReadAllText nuspecPath).Contains "<files />"  // explicit deps-only payload

// ---- live proof -------------------------------------------------------------------------------
type LiveResult =
    { V: string
      Resolved: (string * string) list
      CleanBuild: bool
      ReproIdentical: bool
      Channel: string
      Downgrade: int * string   // exitcode-under-warnaserror, code
      Upgrade: int * string }

let liveProof () : LiveResult =
    let tmp = Path.Combine(Path.GetTempPath(), "bom207-" + Guid.NewGuid().ToString("N").Substring(0, 8))
    let feed = Path.Combine(tmp, "feed")
    let gpf = Path.Combine(tmp, "gpf")
    Directory.CreateDirectory feed |> ignore
    let v = "0.1.51-preview.1"      // next preview after the current published 0.1.50-preview.1
    let yLow = "0.1.50-preview.1"   // current published — staged from the real feed
    let yHigh = "0.1.52-preview.1"  // staged by packing one member ahead

    // pack the coherent snapshot (members + BOM) to the throwaway feed
    let pc, po = run repoRoot "dotnet" [ "pack"; "FS.GG.Rendering.slnx"; "-c"; "Release"; sprintf "-p:Version=%s" v; "-o"; feed ]
    if pc <> 0 then failwithf "coherent pack failed:\n%s" po

    // clean consumer: ONLY FS.GG.UI@V
    let cdir = Path.Combine(tmp, "consumer")
    Directory.CreateDirectory cdir |> ignore
    let nugetConfig =
        sprintf
            "<configuration><config><add key=\"globalPackagesFolder\" value=\"%s\" /></config><packageSources><clear /><add key=\"local\" value=\"%s\" /><add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" /></packageSources><packageSourceMapping><packageSource key=\"local\"><package pattern=\"FS.GG.UI*\" /></packageSource><packageSource key=\"nuget.org\"><package pattern=\"*\" /></packageSource></packageSourceMapping></configuration>"
            gpf feed
    File.WriteAllText(Path.Combine(cdir, "nuget.config"), nugetConfig)
    File.WriteAllText(Path.Combine(cdir, "Library.fs"), "module Consumer.Library")
    let consumerProj single =
        sprintf
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Library</OutputType></PropertyGroup><ItemGroup>%s</ItemGroup><ItemGroup><Compile Include=\"Library.fs\" /></ItemGroup></Project>"
            single
    let projPath = Path.Combine(cdir, "Consumer.fsproj")
    File.WriteAllText(projPath, consumerProj (sprintf "<PackageReference Include=\"FS.GG.UI\" Version=\"%s\" />" v))

    let rc, _ = run cdir "dotnet" [ "restore"; "Consumer.fsproj" ]
    if rc <> 0 then failwith "clean restore failed"
    let _, listOut = run cdir "dotnet" [ "list"; "Consumer.fsproj"; "package"; "--include-transitive" ]
    let resolved =
        Regex.Matches(listOut, "(FS\\.GG\\.UI[A-Za-z.]*)\\s+(?:[0-9][^\\s]*\\s+)?([0-9][0-9A-Za-z.\\-]*)")
        |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
        |> Seq.distinct
        |> Seq.toList
    let bc, _ = run cdir "dotnet" [ "build"; "Consumer.fsproj"; "-c"; "Release"; "--no-restore" ]

    // reproducibility: clear cache + restore twice, compare resolved member sets
    let restoreSet () =
        run repoRoot "dotnet" [ "nuget"; "locals"; "global-packages"; "--clear" ] |> ignore
        if Directory.Exists gpf then Directory.Delete(gpf, true)
        run cdir "dotnet" [ "restore"; "Consumer.fsproj" ] |> ignore
        let _, o = run cdir "dotnet" [ "list"; "Consumer.fsproj"; "package"; "--include-transitive" ]
        Regex.Matches(o, "(FS\\.GG\\.UI[A-Za-z.]*)\\s+(?:[0-9][^\\s]*\\s+)?([0-9][0-9A-Za-z.\\-]*)")
        |> Seq.map (fun m -> m.Groups.[1].Value + " " + m.Groups.[2].Value)
        |> Set.ofSeq
    let r1 = restoreSet ()
    let r2 = restoreSet ()

    // forced mismatch staging
    let scenLow = repo (sprintf "../../../.local/share/nuget-local/FS.GG.UI.Scene.%s.nupkg" yLow)
    let realLow = Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, ".local/share/nuget-local", sprintf "FS.GG.UI.Scene.%s.nupkg" yLow)
    if File.Exists realLow then File.Copy(realLow, Path.Combine(feed, Path.GetFileName realLow), true)
    run repoRoot "dotnet" [ "pack"; "src/Scene/Scene.fsproj"; "-c"; "Release"; sprintf "-p:Version=%s" yHigh; "-o"; feed ] |> ignore
    ignore scenLow

    let mismatch (y: string) =
        let mdir = Path.Combine(tmp, "mismatch-" + y)
        Directory.CreateDirectory mdir |> ignore
        File.Copy(Path.Combine(cdir, "nuget.config"), Path.Combine(mdir, "nuget.config"), true)
        File.WriteAllText(Path.Combine(mdir, "Library.fs"), "module C")
        File.WriteAllText(
            Path.Combine(mdir, "Consumer.fsproj"),
            consumerProj (sprintf "<PackageReference Include=\"FS.GG.UI\" Version=\"%s\" /><PackageReference Include=\"FS.GG.UI.Scene\" Version=\"%s\" />" v y))
        if Directory.Exists gpf then Directory.Delete(gpf, true)
        // loud posture: treat the two deviation codes as errors
        let ec, out = run mdir "dotnet" [ "restore"; "Consumer.fsproj"; "-p:WarningsAsErrors=NU1605%3BNU1608" ]
        let code =
            if Regex.IsMatch(out, "NU1605") then "NU1605"
            elif Regex.IsMatch(out, "NU1608") then "NU1608"
            else "none"
        ec, code

    { V = v
      Resolved = resolved
      CleanBuild = (bc = 0)
      ReproIdentical = (r1 = r2 && not r1.IsEmpty)
      Channel = (if v.Contains "-preview" then "preview" else "stable")
      Downgrade = mismatch yLow
      Upgrade = mismatch yHigh }

// ---- report -----------------------------------------------------------------------------------
let reportPath = repo "specs/207-ui-bom-metapackage/readiness/bom-consumer-validation.md"
Directory.CreateDirectory(Path.GetDirectoryName reportPath) |> ignore

let sb = System.Text.StringBuilder()
let line (s: string) = sb.AppendLine s |> ignore

line "# FS.GG.UI BOM / metapackage — consumer validation"
line ""
line "Regenerated by `scripts/validate-bom-consumer.fsx`. The always-on gate"
line "(`Feature207BomConsumerTests`) asserts the tokens below; `Feature207BomMembershipTests`"
line "independently re-derives the parity facts env-free."
line ""
line "- feature: 207-ui-bom-metapackage"
line "- bom-package-id: FS.GG.UI"
line (sprintf "- members-expected: %d" members.Count)
line (sprintf "- parity: %s (nuspec dependency-id set == discovered packable FS.GG.UI.* set; single [$version$] token; exact-bracket form)" (if parityOk then "pass" else "fail"))
line (sprintf "- single-version-token: %b" allToken)
line (sprintf "- exact-bracket-form: %b" allExact)
line (sprintf "- nuspec-dependency-count: %d" depCount)
line (sprintf "- ships-no-lib: %b (explicit empty <files />; dependencies only)" noLib)
line "- mechanism-note: the exact [V] bracket makes deviation DETECTED in both directions — NU1605 (downgrade Y<V) and NU1608 (outside-constraint Y>V). These are NuGet WARNINGS by default (nearest-wins then builds a mixed graph); they become a BLOCKING restore/build failure only when the consumer treats those codes as errors (WarningsAsErrors=NU1605;NU1608 / TreatWarningsAsErrors — the repo + governed fs-gg-ui template default). This corrects research R1's \"NU1605/NU1107 fails unconditionally\" to the observed NU1605/NU1608 + consumer-policy condition."

if live then
    let r = liveProof ()
    let membersResolved =
        r.Resolved |> List.filter (fun (id, _) -> id.StartsWith "FS.GG.UI." && id <> "FS.GG.UI")
    let atV = membersResolved |> List.filter (fun (_, v) -> v = r.V)
    let dgEc, dgCode = r.Downgrade
    let upEc, upCode = r.Upgrade
    let forcedPass = dgEc <> 0 && upEc <> 0 && dgCode = "NU1605" && upCode = "NU1608"
    line "- provenance: live"
    line (sprintf "- bom-version: %s" r.V)
    line "- single-reference: true"
    line (sprintf "- members-resolved: %d" membersResolved.Length)
    line (sprintf "- resolved-members-at-version: %d/%d at %s" atV.Length membersResolved.Length r.V)
    line (sprintf "- clean-consumer-build: %s" (if r.CleanBuild then "pass" else "fail"))
    line (sprintf "- channel: %s (bom %s matches members; -preview => preview)" r.Channel r.V)
    line (sprintf "- reproducibility: %s (two cache-cleared clean restores produced %s resolved sets)" (if r.ReproIdentical then "identical" else "DIVERGED") (if r.ReproIdentical then "identical" else "different"))
    line (sprintf "- forced-mismatch: %s" (if forcedPass then "pass" else "fail"))
    line (sprintf "  - downgrade Y<V (FS.GG.UI.Scene 0.1.50-preview.1): code=%s loud-under-warnaserror-exit=%d (nonzero => blocked); default=warning(mixed-graph)" dgCode dgEc)
    line (sprintf "  - upgrade   Y>V (FS.GG.UI.Scene 0.1.52-preview.1): code=%s loud-under-warnaserror-exit=%d (nonzero => blocked); default=warning(mixed-graph)" upCode upEc)
    let overall = parityOk && atV.Length = membersResolved.Length && membersResolved.Length = members.Count && r.CleanBuild && r.ReproIdentical && forcedPass
    line (sprintf "- result: %s" (if overall then "pass" else "fail"))
else
    line "- provenance: verdict-core (env-free; full live proof gated behind FS_GG_RUN_BOM_CONSUMER_SMOKE=1)"
    line "- bom-version: pending-live (run FS_GG_RUN_BOM_CONSUMER_SMOKE=1)"
    line "- single-reference: true"
    line "- resolved-members-at-version: pending-live"
    line "- forced-mismatch: pending-live"
    line (sprintf "- result: %s" (if parityOk then "pass" else "fail"))

File.WriteAllText(reportPath, sb.ToString())
printfn "wrote %s (live=%b, parity=%b)" reportPath live parityOk
