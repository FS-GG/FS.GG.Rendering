module Issue1174RunTargetTimeoutTests

// #1174 repair round 1 — the critic's finding: nothing exercised `runInteractiveProcess`'s
// timeout-kill path, which is the ENTIRE behaviour the item adds (`template/base/build.fsx`'s `Run`
// target used to hang silently and unboundedly on a stalled `dotnet run --project src/<Product>`).
// The PR's own end-to-end verification proved incremental streaming, "alive means unbounded", and the
// exit-code diagnostic — but never put a genuinely SILENT, zero-output child against the bound, so the
// one thing #1174 converts (a silent hang -> a loud, bounded failure) went unverified.
//
// This test does exactly that, and needs no live display: `runInteractiveProcess` bounds SILENCE, not
// interactivity (see the comment above it in build.fsx), so a synthetic child that produces zero bytes
// of stdout/stderr is precisely the case the bound exists for.
//
// HERMETIC, PER #540. The fixture project below has no `<PackageReference>`, so its one `dotnet build`
// restores only implicit SDK-provided references (FSharp.Core, the netcore app-host) from the SDK's
// own local packs — no network call, matching the DEFAULT tier's rule for everything under `tests/`.

open System
open System.Diagnostics
open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private buildFsx = Path.Combine(repositoryRoot, "template", "base", "build.fsx")

/// A minimal, GENUINELY SILENT, long-running console app: no `Console` output at all, and a sleep far
/// past any timeout this file uses. Built ONCE, eagerly — `dotnet run` against an already-built,
/// up-to-date project prints nothing of its own (no restore/build banner), which is what makes the
/// later `dotnet run` invocation a true zero-byte child rather than one that merely looks quiet.
let private fixtureRoot =
    lazy
        (let dir =
            Path.Combine(Path.GetTempPath(), "issue1174-silent-" + Guid.NewGuid().ToString("N").Substring(0, 8))

         let srcDir = Path.Combine(dir, "src", "SilentApp")
         Directory.CreateDirectory srcDir |> ignore

         let fsprojPath = Path.Combine(srcDir, "SilentApp.fsproj")

         File.WriteAllText(
             fsprojPath,
             "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
             + "  <PropertyGroup>\n"
             + "    <OutputType>Exe</OutputType>\n"
             + "    <TargetFramework>net10.0</TargetFramework>\n"
             + "  </PropertyGroup>\n"
             + "  <ItemGroup>\n"
             + "    <Compile Include=\"Program.fs\" />\n"
             + "  </ItemGroup>\n"
             + "</Project>\n"
         )

         // Sleeps 60s and prints NOTHING — the "genuinely silent child" the repair asks for.
         File.WriteAllText(
             Path.Combine(srcDir, "Program.fs"),
             "open System.Threading\n[<EntryPoint>]\nlet main _ =\n    Thread.Sleep 60000\n    0\n"
         )

         let psi = ProcessStartInfo "dotnet"
         [ "build"; fsprojPath ] |> List.iter psi.ArgumentList.Add
         psi.WorkingDirectory <- dir
         psi.UseShellExecute <- false
         psi.RedirectStandardOutput <- true
         psi.RedirectStandardError <- true

         match Process.Start psi with
         | null -> failwith "Issue1174 fixture prebuild: could not start `dotnet build`"
         | started ->
             use proc = started
             let stdout = proc.StandardOutput.ReadToEndAsync()
             let stderr = proc.StandardError.ReadToEndAsync()
             proc.WaitForExit()

             if proc.ExitCode <> 0 then
                 failwithf "Issue1174 fixture prebuild failed (exit %d): %s%s" proc.ExitCode stdout.Result stderr.Result

         dir)

type private RunVerdict =
    { ExitCode: int
      Output: string
      ElapsedSeconds: float
      ReadinessLogBytes: int64 option }

/// Spawn `dotnet fsi build.fsx -t Run` rooted at the silent fixture, with a short launch timeout.
/// `waitMs` is an OUTER bound too: a regression that reintroduces the unbounded wait must fail this
/// test LOUDLY (a killed, reported process) rather than hang the gate meant to catch exactly that.
let private runAgainstSilentFixture (launchTimeoutSeconds: int) (waitMs: int) : RunVerdict =
    let fixture = fixtureRoot.Value
    let readinessLog = Path.Combine(fixture, "readiness", "logs", "Run.txt")

    if File.Exists readinessLog then
        File.Delete readinessLog

    let psi = ProcessStartInfo "dotnet"
    [ "fsi"; buildFsx; "-t"; "Run" ] |> List.iter psi.ArgumentList.Add
    psi.WorkingDirectory <- fixture
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.Environment.["FSGG_RUN_LAUNCH_TIMEOUT_SECONDS"] <- string launchTimeoutSeconds

    let stopwatch = Stopwatch.StartNew()

    match Process.Start psi with
    | null -> failwith "Issue1174: could not start `dotnet fsi build.fsx -t Run`"
    | started ->
        use proc = started
        let stdout = proc.StandardOutput.ReadToEndAsync()
        let stderr = proc.StandardError.ReadToEndAsync()

        let exited = proc.WaitForExit waitMs

        if not exited then
            // The regression case: the internal bound did not fire. Kill from OUTSIDE so the test can
            // still report a result instead of hanging the gate that exists to catch this.
            (try
                proc.Kill true
             with _ ->
                 ())

            proc.WaitForExit()

        stopwatch.Stop()

        { ExitCode = (if exited then proc.ExitCode else -1)
          Output = stdout.Result + stderr.Result
          ElapsedSeconds = stopwatch.Elapsed.TotalSeconds
          ReadinessLogBytes = if File.Exists readinessLog then Some(FileInfo(readinessLog).Length) else None }

[<Tests>]
let issue1174RunTargetTimeoutTests =
    testList
        "Issue1174 Run target launch-timeout (repair round 1)"
        [
          test "a genuinely silent child is killed within the bound, reaped, and reported with a diagnostic" {
              // Housekeeping only (not part of the assertion): this test builds a real scratch fixture
              // under the OS temp dir. Delete it on the way out, success or failure, rather than
              // leaving a guid-named directory behind on every run of a shared box.
              try
                  let verdict = runAgainstSilentFixture 2 30000

                  Expect.notEqual
                      verdict.ExitCode
                      0
                      $"a silent child past FSGG_RUN_LAUNCH_TIMEOUT_SECONDS must fail the Run target, not succeed silently.\n\n{verdict.Output}"

                  Expect.isLessThan
                      verdict.ElapsedSeconds
                      20.0
                      $"the kill must fire near the 2s bound, not near the fixture's 60s sleep — bounding this wait is #1174's entire point.\n\n{verdict.Output}"

                  Expect.stringContains
                      verdict.Output
                      "produced no output within"
                      $"the timeout diagnostic must name what happened.\n\n{verdict.Output}"

                  Expect.stringContains
                      verdict.Output
                      "killed after the timeout"
                      $"the timeout diagnostic must say the process was killed, not merely that it failed.\n\n{verdict.Output}"

                  match verdict.ReadinessLogBytes with
                  | Some 0L -> ()
                  | Some n ->
                      failwithf
                          "readiness/logs/Run.txt has %d byte(s); the fixture child is designed to produce NONE, so any bytes here mean this run was not the silent case this test claims to exercise.\n\n%s"
                          n
                          verdict.Output
                  | None ->
                      failwithf
                          "readiness/logs/Run.txt was never created — runInteractiveProcess must create it before the wrapped process launches.\n\n%s"
                          verdict.Output

                  // Reaping: the fixture's grandchild (`SilentApp`, the process `proc.Kill(true)` —
                  // kill the entire tree — exists to reach) must not survive the timeout kill.
                  Threading.Thread.Sleep 500

                  let lingering =
                      Process.GetProcesses()
                      |> Array.filter (fun p ->
                          try
                              p.ProcessName.Contains "SilentApp"
                          with _ ->
                              false)

                  for p in lingering do
                      try
                          p.Kill true
                      with _ ->
                          ()

                  Expect.isEmpty
                      lingering
                      $"the silent child (SilentApp) must not survive the timeout kill — a survivor is a reaping/zombie regression, not just a slow test.\n\n{verdict.Output}"
              finally
                  if fixtureRoot.IsValueCreated then
                      try
                          Directory.Delete(fixtureRoot.Value, true)
                      with _ ->
                          ()
          }
        ]
