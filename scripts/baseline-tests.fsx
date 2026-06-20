// Comprehensive test-baseline runner (Feature 175 lesson T3).
//
// Runs EVERY test project in the repo and records the full red/green set, so pre-existing
// failures are known up front and are not mistaken for regressions at merge time. The solution
// (`dotnet test FS.GG.Rendering.slnx`) deliberately omits `tests/Package.Tests` (release-only) and
// the `samples/**/*.Tests` projects (package-feed consumers) — those are exactly where the
// surprises hid: the surface-baseline gate, stale sample pins, and missing-report failures. This
// runner discovers projects by globbing `*.Tests.fsproj`, so nothing can silently drop out of the
// baseline.
//
// Usage:
//   dotnet fsi scripts/baseline-tests.fsx                 # Debug, summary to stdout
//   dotnet fsi scripts/baseline-tests.fsx --config Release
//   dotnet fsi scripts/baseline-tests.fsx --out readiness/<feature>/baseline.md
//
// Exit code is non-zero if ANY project failed (so it can gate), but every project is still run and
// reported — it never stops at the first red.

open System
open System.Diagnostics
open System.IO

let repoRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName

// --- args -------------------------------------------------------------------
let args = fsi.CommandLineArgs |> Array.skip 1

let argValue flag =
    args
    |> Array.tryFindIndex ((=) flag)
    |> Option.bind (fun i -> if i + 1 < args.Length then Some args.[i + 1] else None)

let config = argValue "--config" |> Option.defaultValue "Debug"
let outPath = argValue "--out"

// --- discovery --------------------------------------------------------------
// Every `*.Tests.fsproj` under tests/ or samples/, excluding build output. Sorted so the report is
// stable; in-solution projects first (tests/), then the package-feed-consuming samples.
let isUnderBuildOutput (path: string) =
    let sep = Path.DirectorySeparatorChar
    path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}")

let discover (relRoot: string) =
    let root = Path.Combine(repoRoot, relRoot)
    if Directory.Exists root then
        Directory.EnumerateFiles(root, "*.Tests.fsproj", SearchOption.AllDirectories)
        |> Seq.filter (not << isUnderBuildOutput)
        |> Seq.sort
        |> Seq.toList
    else []

let projects = discover "tests" @ discover "samples"

if projects.IsEmpty then
    eprintfn "no *.Tests.fsproj projects discovered under tests/ or samples/ — wrong working dir?"
    exit 2

let rel (full: string) = Path.GetRelativePath(repoRoot, full)

// --- run --------------------------------------------------------------------
type Outcome =
    { Project: string
      Passed: bool
      Summary: string
      ExitCode: int }

let run (proj: string) =
    let psi = ProcessStartInfo("dotnet")
    psi.WorkingDirectory <- repoRoot
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    [ "test"; proj; "-c"; config ] |> List.iter psi.ArgumentList.Add

    let proc = Process.Start psi
    let stdout = proc.StandardOutput.ReadToEnd()
    let stderr = proc.StandardError.ReadToEnd()
    proc.WaitForExit()

    // Prefer the Expecto/VSTest summary line ("Passed!  - Failed: 0, Passed: 32, ...").
    let summaryLine =
        (stdout + "\n" + stderr).Split('\n')
        |> Array.map (fun l -> l.Trim())
        |> Array.filter (fun l ->
            l.Contains("Passed!") || l.Contains("Failed!")
            || l.StartsWith("error ") || l.Contains("Build FAILED"))
        |> Array.tryLast
        |> Option.defaultValue (if proc.ExitCode = 0 then "(no summary line; exit 0)" else "(no summary line; build/restore failure)")

    { Project = rel proj
      Passed = proc.ExitCode = 0
      Summary = summaryLine
      ExitCode = proc.ExitCode }

printfn "Running %d test projects (config %s)…\n" projects.Length config

let results =
    projects
    |> List.map (fun proj ->
        printf "  %-58s " (rel proj)
        let r = run proj
        printfn "%s" (if r.Passed then "PASS" else "FAIL")
        r)

// --- report -----------------------------------------------------------------
let green = results |> List.filter (fun r -> r.Passed)
let red = results |> List.filter (fun r -> not r.Passed)

let report =
    let sb = System.Text.StringBuilder()
    let line (s: string) = sb.AppendLine s |> ignore
    line $"# Test baseline — full red/green set"
    line ""
    line $"- Config: `{config}`"
    line $"- Projects: {results.Length}  ·  Green: {green.Length}  ·  Red: {red.Length}"
    line ""
    line "| Project | Result | Summary |"
    line "|---|---|---|"
    for r in results do
        let status = if r.Passed then "🟢 PASS" else "🔴 FAIL"
        line $"| `{r.Project}` | {status} | {r.Summary} |"
    if not red.IsEmpty then
        line ""
        line "## Red projects (known pre-existing failures unless this is a regression)"
        for r in red do
            line $"- `{r.Project}` (exit {r.ExitCode}): {r.Summary}"
    sb.ToString()

printfn "\n%s" report

match outPath with
| Some p ->
    let full = if Path.IsPathRooted p then p else Path.Combine(repoRoot, p)
    Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore
    File.WriteAllText(full, report)
    printfn "wrote baseline summary to %s" (rel full)
| None -> ()

exit (if red.IsEmpty then 0 else 1)
