open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Text.RegularExpressions

// Feature 043 (FR-013): generated projects run the EvidenceGraph / EvidenceAudit gates
// IN-PROCESS through the published FS.GG.UI.Build engine. No Python or shell audit scripts
// are copied into or executed by a generated scaffold; the only retained external process is
// `dotnet test`.
//
// Feature 064 (FR-004 / research R1): there is NO versioned engine reference directive here.
// F# script reference arguments must be string literals, so the engine version cannot be
// interpolated. Instead this script reads the SINGLE source of version truth —
// `<FsGgUiVersion>` in Directory.Packages.props — at runtime, loads the matching, already
// `dotnet restore`-d engine assembly from the NuGet global-packages folder, and invokes the
// generated-evidence façade by reflection (so no typed `open` pins a version). The result:
// exactly ONE literal FS.GG.UI version value in the whole generated project, and a consumer
// upgrade is a single edit to <FsGgUiVersion> + `dotnet restore` — libraries AND the build
// engine move together. See docs/UPGRADING.md.

let path parts = Path.Combine(Array.ofList parts)

let targetFromArgs args =
    let rec loop values =
        match values with
        | "-t" :: target :: _
        | "--target" :: target :: _
        | "target" :: target :: _ -> target
        | _ :: rest -> loop rest
        | [] -> "Dev"

    loop args

let writeLog target =
    Directory.CreateDirectory("readiness/logs") |> ignore
    File.WriteAllText(Path.Combine("readiness", "logs", target + ".txt"), $"{target} completed for generated product.{Environment.NewLine}")
    printfn "%s completed for generated product" target

// Fail-closed half of the SDD-lane guard. `sdd` (the default) and explicit `typed-sdd`
// emit the product only and expect an external SDD lifecycle owner (fsgg-sdd) to supply the
// lifecycle; the one file that distinguishes the byte-identical sdd/none trees —
// the product-root lifecycle-scaffolding-pending.md — is present when either SDD lane was
// chosen, with distinct content preserving typed intent. (It formerly lived under `readiness/`, but that is an SDD-owned tree the provider may not
// write under the orchestrated fsgg-sdd flow — see #954.) While it is present, the readiness/doctor
// gate stays RED (this raises, which fails Verify): a lifecycle-less product cannot pass the
// merge-gate audit. `none` (no sentinel) and `spec-kit` (no sentinel) never trip it. The stock
// `dotnet build`/Directory.Build.props path only WARNS ("sdd warns"); the fail-closed verdict lives
// here so it does not break the smoke build/test lane.
let private lifecycleGuardSentinel = "lifecycle-scaffolding-pending.md"

let private assertLifecycleSupplied () =
    // The message avoids the literal `product`/`fs-gg-ui` tokens (this file is not copyOnly, so the
    // template symbols rewrite them to the scaffolded name); `tree` keeps it name-stable.
    if File.Exists lifecycleGuardSentinel then
        failwithf
            "readiness/doctor: lifecycle scaffolding not yet supplied (selected sdd or typed-sdd lane) — failing closed. Run `fsgg-sdd` to supply it (clears %s), or re-scaffold with `--lifecycle none` if a lifecycle-less tree is deliberate."
            lifecycleGuardSentinel

let tryWriteTextLog (filePath: string) (content: string) =
    try
        let directory = Path.GetDirectoryName filePath

        if not (String.IsNullOrWhiteSpace directory) then
            Directory.CreateDirectory directory |> ignore

        File.WriteAllText(filePath, content)
        None
    with ex ->
        Some $"unreadable readiness log: {filePath}; diagnostics={ex.Message}"

// ----- engine binding: resolve <FsGgUiVersion> at runtime (FR-004, R1) -----

let private fsSkiaUiVersion () =
    let propsPath = path [ Directory.GetCurrentDirectory(); "Directory.Packages.props" ]

    if not (File.Exists propsPath) then
        failwithf "Cannot resolve the FS.GG.UI engine version: %s is missing." propsPath

    let m = Regex.Match(File.ReadAllText propsPath, "<FsGgUiVersion>([^<]+)</FsGgUiVersion>")

    if m.Success then
        m.Groups.[1].Value.Trim()
    else
        failwithf "Cannot resolve <FsGgUiVersion> from %s; it is the single source of FS.GG.UI version truth." propsPath

let private nugetPackagesRoot () =
    match Environment.GetEnvironmentVariable "NUGET_PACKAGES" with
    | null -> path [ Environment.GetFolderPath Environment.SpecialFolder.UserProfile; ".nuget"; "packages" ]
    | "" -> path [ Environment.GetFolderPath Environment.SpecialFolder.UserProfile; ".nuget"; "packages" ]
    | dir -> dir

// Probe the NuGet global-packages cache for an assembly by simple name, preferring net10.0.
// The engine's transitive dependency closure (Fake.Core, YamlDotNet, FSharp.SystemTextJson,
// DiffPlex, FS.GG.UI.SkillSupport, …) is restored into this cache; Assembly.LoadFrom of the
// engine alone does not bring them, so we resolve each on demand at invoke time.
let private probeCachedAssembly (nugetPackages: string) (simpleName: string) : string option =
    let packageDir = path [ nugetPackages; simpleName.ToLowerInvariant() ]

    if not (Directory.Exists packageDir) then
        None
    else
        Directory.GetDirectories packageDir
        |> Array.collect (fun versionDir ->
            Directory.GetFiles(versionDir, simpleName + ".dll", SearchOption.AllDirectories)
            |> Array.filter (fun f -> f.Replace('\\', '/').Contains "/lib/"))
        |> Array.sortByDescending (fun f -> if f.Replace('\\', '/').Contains "/net10.0/" then 1 else 0)
        |> Array.tryHead

// Restore the pinned engine (+ its dependency closure) into the global cache when absent, using
// a throwaway project under TEMP so default/user NuGet config resolution applies — that has the
// local feed for in-repo framework development and nuget.org for a published consumer. The exact
// <FsGgUiVersion> is restored (not "latest"), so the engine and libraries stay in lock-step.
let private restoreEngine (version: string) =
    let tmp = path [ Path.GetTempPath(); "fsskia-engine-restore-" + version ]
    Directory.CreateDirectory tmp |> ignore
    let proj = path [ tmp; "engine-restore.fsproj" ]

    File.WriteAllText(
        proj,
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
        + "  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n"
        + sprintf "  <ItemGroup>\n    <PackageReference Include=\"FS.GG.UI.Build\" Version=\"%s\" />\n  </ItemGroup>\n" version
        + "</Project>\n")

    let psi = ProcessStartInfo("dotnet", sprintf "restore \"%s\"" proj)
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.UseShellExecute <- false
    psi.WorkingDirectory <- tmp

    match (try Process.Start psi |> Option.ofObj with _ -> None) with
    | None -> ()
    | Some p ->
        let outTask = p.StandardOutput.ReadToEndAsync()
        let errTask = p.StandardError.ReadToEndAsync()
        p.WaitForExit()
        outTask.Result |> ignore
        errTask.Result |> ignore

let private engineAssembly =
    lazy
        (let version = fsSkiaUiVersion ()
         let nugetPackages = nugetPackagesRoot ()
         // NuGet lowercases package-id folders in the global-packages cache.
         let dll = path [ nugetPackages; "fs.gg.ui.build"; version; "lib"; "net10.0"; "FS.GG.UI.Build.dll" ]

         if not (File.Exists dll) then
             restoreEngine version

         if not (File.Exists dll) then
             failwithf
                 "FS.GG.UI.Build %s could not be restored to %s. Ensure the version exists on a configured feed (`dotnet restore`)."
                 version
                 dll

         // R1: idiomatic simplicity yields to the #r-literal constraint here — bind the
         // property-resolved engine assembly at runtime so the engine moves with the single
         // version value, and resolve its dependency closure from the same global cache.
         AppDomain.CurrentDomain.add_AssemblyResolve (
             ResolveEventHandler(fun _ args ->
                 let simple = System.Reflection.AssemblyName(args.Name).Name

                 match probeCachedAssembly nugetPackages simple with
                 | Some path -> Assembly.LoadFrom path
                 | None -> null))

         Assembly.LoadFrom dll)

let private runGeneratedEvidence (target: string) : int =
    let assembly = engineAssembly.Value
    let runnerType = assembly.GetType("FS.GG.UI.Build.Evidence.GeneratedRunner")

    if isNull runnerType then
        failwith "FS.GG.UI.Build.Evidence.GeneratedRunner not found in the resolved engine assembly."

    let runMethod = runnerType.GetMethod("run")

    if isNull runMethod then
        failwith "FS.GG.UI.Build.Evidence.GeneratedRunner.run not found in the resolved engine assembly."

    runMethod.Invoke(null, [| box target; box (Directory.GetCurrentDirectory()) |]) :?> int

let runProcess (target: string) (fileName: string) (arguments: string) =
    Directory.CreateDirectory("readiness/logs") |> ignore
    let logPath = Path.Combine("readiness", "logs", target + ".txt")
    let startInfo = ProcessStartInfo(fileName, arguments)
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.UseShellExecute <- false
    startInfo.WorkingDirectory <- Directory.GetCurrentDirectory()

    let proc =
        try
            Process.Start(startInfo) |> Option.ofObj
        with ex ->
            failwithf "%s failed command launch: %s %s; diagnostics=%s" target fileName arguments ex.Message

    use proc =
        match proc with
        | Some proc -> proc
        | None -> failwithf "%s failed command launch: %s %s" target fileName arguments

    // Drain stdout and stderr concurrently before waiting: reading one stream to
    // end before the other deadlocks when the child fills the other pipe.
    let stdoutTask = proc.StandardOutput.ReadToEndAsync()
    let stderrTask = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()
    let stdout = stdoutTask.Result
    let stderr = stderrTask.Result

    let output = stdout + stderr

    match tryWriteTextLog logPath output with
    | Some diagnostic -> failwithf "%s failed readiness log write; %s" target diagnostic
    | None -> ()

    printf "%s" output

    if output.IndexOf("NU1603", StringComparison.OrdinalIgnoreCase) >= 0 then
        failwithf "%s failed package-resolution: NU1603 fallback is not authoritative generated-product evidence" target

    if proc.ExitCode <> 0 then
        failwithf "%s failed with exit code %d; see %s" target proc.ExitCode logPath

// #1174: `runProcess` above buffers the ENTIRE child output and writes the readiness log only after
// `WaitForExit()` returns — correct for every OTHER caller (Restore, Build, Test, Pack, the Performance
// targets), because each of those wraps a command that is expected to run to completion and exit on its
// own. `Run` (below) is the one target that wraps `dotnet run --project src/<Product>` — an interactive,
// window-owning process that, on a real display, keeps running until a human closes it (docs/product.md
// documents that contract, and the separate `setsid … &` idiom for keeping it open past this target's own
// lifetime). Reusing `runProcess` for it gives a caller ZERO signal for as long as it runs: a live but
// slow launch and a genuinely deadlocked one both read as silence, and there is no bound on either.
//
// This wrapper keeps `runProcess` and every one of its other callers byte-identical (Test/Verify's bodies
// are FROZEN, see the comment above `run`) and instead:
//   - streams stdout/stderr to the console AND appends each line to the readiness log AS IT ARRIVES, so a
//     stall is visible in readiness/logs/Run.txt while it is still stalled, not only after (acceptance #2);
//   - bounds only the SILENCE before the first byte of output: if the wrapped process has produced nothing
//     at all within `launchTimeoutSeconds` (default 120s — generous next to a cold `dotnet run` compile,
//     tiny next to the 6+ minute stall this defect reports; overridable via
//     `FSGG_RUN_LAUNCH_TIMEOUT_SECONDS` for a slower host), it is killed and the target fails with a
//     diagnostic naming the timeout and the log to inspect (acceptance #1). The underlying stall's exact
//     mechanism is unestablished (#1174 root cause: bounded unknown) — this mitigates the observable
//     symptom (unbounded, undiagnosed silence) rather than a mechanism nobody has pinned down;
//   - once ANY output has been observed, the process is confirmed alive, so no further timeout applies —
//     `Run` wraps a persistent, window-owning process by contract, and a live session that keeps running
//     until a human (or the process itself) closes the window is the documented, correct outcome, not a
//     stall. This target then waits it out exactly as `runProcess` does, and applies the same NU1603 /
//     exit-code checks on the way out.
let runInteractiveProcess (target: string) (fileName: string) (arguments: string) =
    Directory.CreateDirectory("readiness/logs") |> ignore
    let logPath = Path.Combine("readiness", "logs", target + ".txt")

    // Truncate up front: the log is now written incrementally as the child produces output, so a
    // previous invocation's tail must not survive to be misread as part of this one.
    (match tryWriteTextLog logPath "" with
     | Some diagnostic -> failwithf "%s failed readiness log write; %s" target diagnostic
     | None -> ())

    let launchTimeoutSeconds =
        match Environment.GetEnvironmentVariable "FSGG_RUN_LAUNCH_TIMEOUT_SECONDS" with
        | null
        | "" -> 120.0
        | raw ->
            match Double.TryParse raw with
            | true, seconds when seconds > 0.0 -> seconds
            | _ -> 120.0

    let startInfo = ProcessStartInfo(fileName, arguments)
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.UseShellExecute <- false
    startInfo.WorkingDirectory <- Directory.GetCurrentDirectory()

    let outputLock = obj ()
    let mutable sawOutput = false

    let onDataReceived (args: DataReceivedEventArgs) =
        if not (isNull args.Data) then
            lock outputLock (fun () ->
                sawOutput <- true
                File.AppendAllText(logPath, args.Data + Environment.NewLine))

            printfn "%s" args.Data

    let proc =
        try
            Process.Start(startInfo) |> Option.ofObj
        with ex ->
            failwithf "%s failed command launch: %s %s; diagnostics=%s" target fileName arguments ex.Message

    use proc =
        match proc with
        | Some proc -> proc
        | None -> failwithf "%s failed command launch: %s %s" target fileName arguments

    proc.OutputDataReceived.Add onDataReceived
    proc.ErrorDataReceived.Add onDataReceived
    proc.BeginOutputReadLine()
    proc.BeginErrorReadLine()

    let started = DateTime.UtcNow

    // Poll rather than block outright: a bare `proc.WaitForExit()` would give us back exactly the
    // unbounded, unobservable wait this function exists to replace. `WaitForExit(200)` returns `true`
    // the moment the process exits early (a fast failure still reports promptly), so this never adds
    // more than ~200ms of latency to a quick command.
    let rec waitForFirstSignOfLife () =
        if proc.WaitForExit(200) then
            () // exited before producing a byte, or before the timeout — either way, fall through
        elif lock outputLock (fun () -> sawOutput) then
            () // confirmed alive: stop bounding it, exactly like a direct terminal launch
        elif (DateTime.UtcNow - started).TotalSeconds >= launchTimeoutSeconds then
            let killDiagnostic =
                try
                    proc.Kill(true)
                    None
                with ex ->
                    Some ex.Message

            let killNote =
                match killDiagnostic with
                | Some message -> sprintf " (the stalled process could also not be killed: %s)" message
                | None -> ""

            failwithf
                "%s produced no output within %.0fs of launching %s %s and was killed after the timeout.%s See %s — this is #1174's mitigation for the target's undiagnosed stall, not a fix for its root cause."
                target
                launchTimeoutSeconds
                fileName
                arguments
                killNote
                logPath
        else
            waitForFirstSignOfLife ()

    waitForFirstSignOfLife ()

    // Confirmed alive (or already exited): wait out the rest unbounded, matching what a human gets
    // running the same command directly in a terminal.
    proc.WaitForExit()

    let output = lock outputLock (fun () -> sawOutput)

    if output && File.Exists logPath && (File.ReadAllText logPath).IndexOf("NU1603", StringComparison.OrdinalIgnoreCase) >= 0 then
        failwithf "%s failed package-resolution: NU1603 fallback is not authoritative generated-product evidence" target

    if proc.ExitCode <> 0 then
        failwithf "%s failed with exit code %d; see %s" target proc.ExitCode logPath

let runGeneratedTests () =
    runProcess "Test" "dotnet" "test tests/Product.Tests/Product.Tests.fsproj -m:1 --disable-build-servers"
    printfn "Test completed for generated product"

// Feature 212 (R3): name-agnostic locators so the new pass-through targets (and the verb wrapper)
// need no literal <Name>. The product root holds exactly one root solution and exactly one src
// project; both are discovered at runtime so the same script works for any scaffolded name.
let private singleRootSolution () =
    match Directory.GetFiles(Directory.GetCurrentDirectory(), "*.slnx") with
    | [| f |] -> Path.GetFileName f
    | [||] -> failwith "No root *.slnx found in the product root (Feature 212 root solution missing)."
    | many -> failwithf "Expected exactly one root *.slnx; found %d." many.Length

let private singleSrcProject () =
    let srcRoot = path [ Directory.GetCurrentDirectory(); "src" ]

    match (if Directory.Exists srcRoot then Directory.GetDirectories srcRoot else [||]) with
    | [| d |] -> Path.GetFileName d
    | [||] -> failwith "No src/<project> directory found in the product root."
    | many -> failwithf "Expected exactly one src/<project>; found %d." many.Length

let runPerformanceEvidence () =
    let project = singleSrcProject ()
    runProcess "PerformanceEvidence" "dotnet" (sprintf "run -c Release --project src/%s -- --performance-evidence readiness/performance-evidence.json" project)

let runPerformanceCriticRequest () =
    let project = singleSrcProject ()
    runProcess "PerformanceCriticRequest" "dotnet" (sprintf "run -c Release --project src/%s -- --performance-critic-request readiness/performance-critic-request.json" project)

let runPerformanceIntent () =
    let project = singleSrcProject ()
    runProcess "PerformanceIntent" "dotnet" (sprintf "run -c Release --project src/%s -- --performance-intent readiness/performance-intent.yml" project)

let run target =
    match target with
    | "Dev"
    | "GeneratedGuidanceCheck"
    | "TemplateDrift" -> writeLog target
    | "EvidenceGraph" ->
        let exitCode = runGeneratedEvidence "EvidenceGraph"
        if exitCode <> 0 then
            failwithf "EvidenceGraph failed with exit code %d; see readiness/evidence-graph.md" exitCode
    | "EvidenceAudit" ->
        let exitCode = runGeneratedEvidence "EvidenceAudit"
        if exitCode <> 0 then
            failwithf "EvidenceAudit failed with exit code %d; see readiness/evidence-audit.md" exitCode
    // Feature 212 (R3 / FR-007): pass-through build-graph targets over the single root .slnx. These
    // shell to stock `dotnet` so the governed script path and stock root path build the SAME project set
    // (FR-010, no divergence). Test/Verify below are FROZEN — their bodies are unchanged.
    | "Restore" -> runProcess "Restore" "dotnet" (sprintf "restore \"%s\"" (singleRootSolution ()))
    | "Build" -> runProcess "Build" "dotnet" (sprintf "build \"%s\"" (singleRootSolution ()))
    | "Run" -> runInteractiveProcess "Run" "dotnet" (sprintf "run --project src/%s" (singleSrcProject ()))
    | "Pack" -> runProcess "Pack" "dotnet" (sprintf "pack \"%s\" -c Release" (singleRootSolution ()))
    | "Test" ->
        runGeneratedTests ()
        runPerformanceIntent ()
        runPerformanceEvidence ()
    | "PerformanceIntent" -> runPerformanceIntent ()
    | "PerformanceEvidence" -> runPerformanceEvidence ()
    | "PerformanceCriticRequest" -> runPerformanceCriticRequest ()
    | "Verify" ->
        // ADR-0056 §Decision.2: fail closed BEFORE any other audit work — a lifecycle-less sdd tree
        // is not a completable feature, so the merge-gate audit must not even begin.
        assertLifecycleSupplied ()
        [ "Dev"; "GeneratedGuidanceCheck"; "TemplateDrift" ]
        |> List.iter writeLog
        let graphExitCode = runGeneratedEvidence "EvidenceGraph"
        if graphExitCode <> 0 then
            failwithf "EvidenceGraph failed with exit code %d; see readiness/evidence-graph.md" graphExitCode
        let auditExitCode = runGeneratedEvidence "EvidenceAudit"
        if auditExitCode <> 0 then
            failwithf "EvidenceAudit failed with exit code %d; see readiness/evidence-audit.md" auditExitCode
        runGeneratedTests ()
        runPerformanceIntent ()
        runPerformanceEvidence ()
        writeLog "Verify"
        printfn "Verify completed for generated product"
    | other ->
        failwithf "Unknown generated product target: %s" other

// Feature 242 (spec 242-scaffold-discoverability, §2.3): surface the load-bearing build-target
// semantics at the entry point, so a developer never mistakes a green `Dev` for a passing compile.
// The banner phrasing is kept in sync with docs/product.md (a governance scan fails on drift).
// `dotnet fsi` reserves --help/-h for itself on the script path (they never reach this script), so
// the script-level trigger is the bare `help` token; ./build.sh handles --help/-h at the shell level.
// Printing help runs no target and writes no readiness/logs/*, then exits 0.
let helpBanner =
    "FS.GG.UI generated product — build targets\n"
    + "  Invoke: ./build.sh <verb> | dotnet fsi build.fsx -t <Target> | ./fake.sh -t <Target>\n\n"
    + "  Dev      A completion-marker / log-writer only — writes readiness/logs/Dev.txt. It does not compile\n"
    + "           your code; a green Dev is not evidence the build passes. Use Test for real feedback.\n"
    + "  Test     The first real compile: `dotnet test` + Release expected-workload performance evidence (audit-free).\n"
    + "           A fresh game scaffold fails until all five Placeholder workloads drive product-authored state/messages;\n"
    + "           run PerformanceEvidence, review each definitionDigest, then acknowledge it as Authored.\n"
    + "  PerformanceIntent emits the Contracts 7.x declaration for the SDD performanceIntent block.\n"
    + "  PerformanceCriticRequest emits the exact provenance, cost inventory, raw evidence, host facts,\n"
    + "           rubric version and digest a fresh-context representativeness critic must review.\n"
    + "  Verify   Runs the merge-gate audit (EvidenceGraph -> EvidenceAudit) first — the audit hard-blocks\n"
    + "           until every task is [X] — then runs the tests. Use only when the feature is complete.\n"
    + "           The first Verify on a fresh scaffold fails until you generate the headless evidence baseline\n"
    + "           (readiness/layout-evidence.txt + headless-scene-evidence.txt) and author performance workloads.\n"
    + "           A linked performance-debt issue permits baseline capture but never satisfies acceptance.\n\n"
    + "  Restore | Build | Pack   Pass-through to stock `dotnet` over the single root .slnx.\n"
    + "  Run      Pass-through to `dotnet run` over the single root .slnx; fails fast with a diagnostic\n"
    + "           if it produces no output at all within FSGG_RUN_LAUNCH_TIMEOUT_SECONDS (default 120s)\n"
    + "           of launching, instead of stalling silently (#1174). Streams to readiness/logs/Run.txt\n"
    + "           as it runs, and waits out a confirmed-alive interactive session unbounded, same as\n"
    + "           running the command directly.\n\n"
    + "  Help:  ./build.sh --help   |   dotnet fsi build.fsx help   (fsi reserves --help/-h on the script path)"

let private isHelpToken (token: string) =
    match token.ToLowerInvariant() with
    | "help"
    | "--help"
    | "-h"
    | "-help"
    | "/?" -> true
    | _ -> false

let args = Environment.GetCommandLineArgs() |> Array.skip 1 |> Array.toList

if args |> List.exists isHelpToken then
    printfn "%s" helpBanner
else
    args |> targetFromArgs |> run
