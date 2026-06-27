open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Text.RegularExpressions

// Feature 043 (FR-013): generated projects run the EvidenceGraph / EvidenceAudit gates
// IN-PROCESS through the published FS.GG.UI.Build engine. No Python or shell audit scripts
// are copied into or executed by generated products; the only retained external process is
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

let runGeneratedTests () =
    runProcess "Test" "dotnet" "test tests/Product.Tests/Product.Tests.fsproj -m:1 --disable-build-servers"
    printfn "Test completed for generated product"

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
    | "Test" -> runGeneratedTests ()
    | "Verify" ->
        [ "Dev"; "GeneratedGuidanceCheck"; "TemplateDrift" ]
        |> List.iter writeLog
        let graphExitCode = runGeneratedEvidence "EvidenceGraph"
        if graphExitCode <> 0 then
            failwithf "EvidenceGraph failed with exit code %d; see readiness/evidence-graph.md" graphExitCode
        let auditExitCode = runGeneratedEvidence "EvidenceAudit"
        if auditExitCode <> 0 then
            failwithf "EvidenceAudit failed with exit code %d; see readiness/evidence-audit.md" auditExitCode
        runGeneratedTests ()
        writeLog "Verify"
        printfn "Verify completed for generated product"
    | other ->
        failwithf "Unknown generated product target: %s" other

Environment.GetCommandLineArgs()
|> Array.skip 1
|> Array.toList
|> targetFromArgs
|> run
