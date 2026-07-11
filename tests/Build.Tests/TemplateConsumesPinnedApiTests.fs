module TemplateConsumesPinnedApiTests

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.RegularExpressions
open Expecto

// Issue #504 — the template-vs-PIN assertion.
//
// THE GAP. `src/` compiles against ITSELF, and the bundled api-surface mirror
// (`template/base/docs/api-surface/**`) is frozen against `src/`. But a SCAFFOLDED product compiles
// against the PUBLISHED package at $(FsGgUiVersion). So the framework and its mirror can agree with
// each other while BOTH disagree with the only surface the template can actually call.
//
// That is what #429's audio seam did: `Viewer.runAppWithAudio` lived in `src/`, the mirror duly
// advertised it, the template pinned 0.8.0 — which did not contain it — and NOTHING went red for the
// entire life of 0.8.0. #436 found it by trying to build; #492 had to cut a release.
//
// WHY THE MIRROR CANNOT BE THE ORACLE. #437's M-MIR diffs the mirror against `src/`. That is a real
// gate, but it is blind to this class BY CONSTRUCTION, because `src/` is the side that is ahead. A
// PERFECT mirror-vs-src gate still reports green while the template cannot compile. The missing
// assertion is not mirror-vs-src. It is template-vs-pin.
//
// TWO LAYERS, mirroring scripts/validate-template-payload-pins.fsx:
//
//   * Structural core (always, offline). Extract the framework entry points the template's
//     `Program.fs` ACTUALLY calls, and resolve each against the bundled mirror. This catches a call
//     site that names nothing at all. It is deliberately NOT the pin proof — the mirror tracks
//     `src/`, so this layer is green in exactly the #429 case. Its real job is to keep the EXTRACTOR
//     honest: zero call sites is an ERROR, never a pass (the fails-open class of FS-GG/.github#266 —
//     "nothing to check" and "checked, and it's fine" must not share an exit code).
//
//   * Pin-grounded proof (FS_GG_RUN_TEMPLATE_PINNED_API=1, network). Restore the pinned packages at
//     the template's real axes and COMPILE the extracted call sites against them. This is the layer
//     that turns red the moment the framework grows API the template cannot consume — i.e. at
//     #433's merge, not at #436's build three days and one emergency release later.
//
// It is a compile probe rather than reflection on purpose: `Assembly.LoadFrom` on a bare package
// DLL trips over unresolved dependencies, and `MetadataLoadContext` would add a PackageReference to
// this locked-restore project. `nameof` resolves the symbol at COMPILE time and needs neither, so a
// member the pinned package does not export is a compiler error — which is precisely the question
// being asked ("can the template consume the pin?").
//
// This test lives in Build.Tests, NOT Package.Tests, DELIBERATELY. Package.Tests is release-only and
// absent from FS.GG.Rendering.slnx, so a rule placed there "runs after the merge that breaks it,
// never on the PR" (gate.yml) — that is how Renovate PR #233 reached 4/4 green on a pin no local
// pack could produce. A check that fires after the merge cannot deliver #504, whose entire point is
// to fire ON it. Build.Tests is in the slnx, so this runs on every gate.

// ---------------------------------------------------------------------------------------------
// Repo layout
// ---------------------------------------------------------------------------------------------

let private repoRoot =
    let rec up (dir: DirectoryInfo | null) =
        match dir with
        | null -> failwith "could not locate repo root (FS.GG.Rendering.slnx) walking up from test base dir"
        | d ->
            if File.Exists(Path.Combine(d.FullName, "FS.GG.Rendering.slnx")) then d.FullName
            else up d.Parent

    up (DirectoryInfo(AppContext.BaseDirectory))

let private repoPath (rel: string) =
    Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private mirrorRoot = repoPath "template/base/docs/api-surface"
let private programPath = repoPath "template/base/src/Product/Program.fs"
let private packagesPropsPath = repoPath "template/base/Directory.Packages.props"

// ---------------------------------------------------------------------------------------------
// The framework surface, as the TEMPLATE bundles it.
//
// Each mirrored `.fsi` is one package: its `namespace` IS the package id (`namespace
// FS.GG.UI.SkiaViewer` -> package `FS.GG.UI.SkiaViewer`), which is what lets the probe below derive
// its PackageReferences instead of hardcoding a list that would rot.
// ---------------------------------------------------------------------------------------------

type FrameworkModule =
    { /// The package id AND the namespace to `open` — they are the same string.
      Namespace: string
      /// Top-level module name, as a call site spells it (`Viewer`, `ControlsElmish`, `Audio`).
      Name: string
      /// `val` members exported by that module.
      Members: Set<string> }

let private namespaceRegex = Regex(@"^namespace\s+([\w.]+)", RegexOptions.Compiled)
let private moduleRegex = Regex(@"^module\s+(\w+)", RegexOptions.Compiled)
let private valRegex = Regex(@"^\s+val\s+(?:inline\s+)?([a-z]\w*)\s*:", RegexOptions.Compiled)

/// Parse one mirrored `.fsi` into its top-level modules. Only column-0 `module` declarations are
/// entry points a call site can name; nested modules are reached through their parent and are not
/// what `Program.fs` writes.
let private parseMirrorFile (path: string) =
    let mutable ns = ""
    let mutable current = ""
    // Keyed by (namespace, module): the namespace is bound when the MODULE is declared, not read off
    // a mutable after the loop, so a file that ever declares two namespaces attributes each module to
    // the one it was actually written under instead of to whichever came last.
    let members = System.Collections.Generic.Dictionary<string * string, ResizeArray<string>>()

    for line in File.ReadAllLines path do
        let nsMatch = namespaceRegex.Match line
        let moduleMatch = moduleRegex.Match line
        let valMatch = valRegex.Match line

        if nsMatch.Success then ns <- nsMatch.Groups.[1].Value
        elif moduleMatch.Success then
            current <- moduleMatch.Groups.[1].Value
            if ns <> "" && not (members.ContainsKey((ns, current))) then
                members.[(ns, current)] <- ResizeArray()
        elif valMatch.Success && current <> "" && ns <> "" then
            match members.TryGetValue((ns, current)) with
            | true, vals -> vals.Add(valMatch.Groups.[1].Value)
            | _ -> ()

    members
    |> Seq.map (fun kvp ->
        { Namespace = fst kvp.Key
          Name = snd kvp.Key
          Members = Set.ofSeq kvp.Value })
    |> List.ofSeq

let private frameworkModules =
    Directory.EnumerateFiles(mirrorRoot, "*.fsi", SearchOption.AllDirectories)
    |> Seq.collect parseMirrorFile
    |> Seq.filter (fun m -> not m.Members.IsEmpty)
    |> List.ofSeq

/// A module name is only a framework entry point if the mirror declares it. This is what keeps the
/// extractor from mistaking the product's OWN modules (`AppRoot.WindowOptions.parseWindowBehavior`)
/// or FSharp.Core's (`List.ofArray`, `Option.defaultValue`) for framework calls.
let private frameworkModulesByName =
    frameworkModules
    |> List.groupBy (fun m -> m.Name)
    |> Map.ofList

// ---------------------------------------------------------------------------------------------
// The call sites, as the TEMPLATE'S Program.fs actually writes them.
//
// The `//#if` profile directives are COMMENTS, so reading the file as raw text sees every profile's
// code at once — app, game, sample-pack, governed, headless-scene — which is what we want: the pin
// must satisfy every profile the template can scaffold.
// ---------------------------------------------------------------------------------------------

type CallSite =
    { Module: string
      Member: string
      Line: int }

let private stringLiteral = Regex("\"(\\\\.|[^\"\\\\])*\"", RegexOptions.Compiled)

/// Multi-line forms have to go before the file is split into lines: a `(* ... *)` block or a `"""…"""`
/// string spans lines, so no per-line rule can see it. Each match is replaced by JUST its newlines,
/// which erases the content while keeping every later line at its true line number (the numbers are
/// reported to a human chasing a failure, so they have to be real).
let private blockForms =
    Regex(@"\(\*.*?\*\)|"""""".*?""""""", RegexOptions.Compiled ||| RegexOptions.Singleline)

let private eraseKeepingLines (text: string) =
    blockForms.Replace(
        text,
        fun m -> String(m.Value |> Seq.filter (fun c -> c = '\n') |> Seq.toArray))

/// Strings BEFORE comments: a `//` inside a string literal is not a comment. Both must go, because
/// `Program.fs` NAMES framework API in prose and in string literals without calling it —
/// `let desktopSessionDiagnosticApi = "Viewer.desktopSessionDiagnostic()"` is a label, not a call.
/// Counting it would make this test assert something the template does not actually do, and would
/// then demand the pinned package export API the template never touches.
let private stripCommentsAndStrings (line: string) =
    let withoutStrings = stringLiteral.Replace(line, "\"\"")

    match withoutStrings.IndexOf("//", StringComparison.Ordinal) with
    | -1 -> withoutStrings
    | i -> withoutStrings.Substring(0, i)

/// `[qualifier.]Module.member` — a capitalised module, a lowercase-initial member (the F# convention
/// for functions and values). The qualifier is captured rather than discarded because it is the only
/// thing that disambiguates a genuine name collision: the framework exports
/// `FS.GG.UI.Scene.LayoutEvidence`, and the TEMPLATE'S OWN `AppRoot.LayoutEvidence` shares its name.
/// Matching on the bare module name alone would read the product's calls to itself as framework
/// calls — and then demand the pinned package export them.
let private callRegex = Regex(@"(?<![\w.])((?:[A-Z]\w*\.)*)([A-Z]\w*)\.([a-z]\w*)", RegexOptions.Compiled)

/// The template's own modules (`module AppRoot.LayoutEvidence`), so an unqualified reference to one
/// of them is never mistaken for the framework module it happens to share a name with.
let private productModules =
    Directory.EnumerateFiles(Path.GetDirectoryName programPath |> Option.ofObj |> Option.defaultValue "", "*.fs")
    |> Seq.collect File.ReadAllLines
    |> Seq.choose (fun line ->
        let m = Regex.Match(line, @"^module\s+(?:[\w.]+\.)?(\w+)\s*$")
        if m.Success then Some m.Groups.[1].Value else None)
    |> Set.ofSeq

/// A match is a framework call only if the mirror declares the module AND the qualifier agrees:
/// either the call is unqualified (reached through an `open`) or it is spelled out in full with the
/// framework namespace (`FS.GG.Audio.Host.OpenAlBackend.create`). A qualifier that is anything else
/// — `AppRoot.` — is the product calling itself.
let private isFrameworkCall (qualifier: string) (moduleName: string) =
    match frameworkModulesByName.TryFind moduleName with
    | None -> false
    | Some candidates ->
        let qualified = qualifier.TrimEnd('.')

        if qualified = "" then not (productModules.Contains moduleName)
        else candidates |> List.exists (fun m -> m.Namespace = qualified)

let private callSites =
    (File.ReadAllText programPath |> eraseKeepingLines).Split('\n')
    |> Array.mapi (fun i line -> i + 1, stripCommentsAndStrings line)
    |> Array.collect (fun (lineNo, line) ->
        callRegex.Matches line
        |> Seq.map (fun m ->
            m.Groups.[1].Value,
            { Module = m.Groups.[2].Value
              Member = m.Groups.[3].Value
              Line = lineNo })
        |> Array.ofSeq)
    |> Array.filter (fun (qualifier, c) -> isFrameworkCall qualifier c.Module)
    |> Array.map snd
    |> Array.distinctBy (fun c -> c.Module, c.Member)
    |> List.ofArray

/// Resolve a call site to the mirrored module that EXPORTS THE MEMBER (a module name is unique
/// across the mirror in practice; if two packages ever export the same module name, any that
/// declares the member satisfies the call, which is what the F# resolver would do given the `open`s).
/// `None` means the mirror does not declare this member — which is what the mirror test asserts on.
let private owningModule (call: CallSite) =
    frameworkModulesByName
    |> Map.tryFind call.Module
    |> Option.bind (fun candidates -> candidates |> List.tryFind (fun m -> m.Members.Contains call.Member))

/// The namespace to `open` (and the package to reference) for a call site — resolved at MODULE level,
/// deliberately falling back to any module of that name when no mirrored module declares the member.
///
/// The probe emits a `nameof` for EVERY call site, so it must emit an `open` for every call site too.
/// Deriving the namespace from `owningModule` instead would drop exactly the call sites the mirror is
/// missing, and the probe would then fail FS0039 on an unopened namespace and blame the PIN — reporting
/// "the framework grew API a scaffolded product cannot reach" when the pin is fine and only the mirror
/// is stale. A wrong diagnosis on a real failure is worse than no diagnosis.
let private callNamespace (call: CallSite) =
    frameworkModulesByName
    |> Map.tryFind call.Module
    |> Option.bind (fun candidates ->
        candidates
        |> List.tryFind (fun m -> m.Members.Contains call.Member)
        |> Option.orElse (List.tryHead candidates))
    |> Option.map (fun m -> m.Namespace)

// ---------------------------------------------------------------------------------------------
// The pins the template hands a scaffolded product.
// ---------------------------------------------------------------------------------------------

let private readAxis (axis: string) =
    let props = File.ReadAllText packagesPropsPath
    let m = Regex.Match(props, $"<{axis}>([^<]+)</{axis}>")
    if m.Success then m.Groups.[1].Value else failwith $"<{axis}> not found in {packagesPropsPath}"

/// A package id derives its version from the axis its family is released on — the same three axes
/// `Directory.Packages.props` declares. Getting this wrong would restore a version the template
/// never pins, and the probe would then prove nothing about the real product.
let private pinFor (packageId: string) =
    if packageId.StartsWith("FS.GG.UI.", StringComparison.Ordinal) then readAxis "FsGgUiVersion"
    elif packageId.StartsWith("FS.GG.Audio.", StringComparison.Ordinal) then readAxis "FsGgAudioVersion"
    elif packageId.StartsWith("FS.GG.Game.", StringComparison.Ordinal) then readAxis "FsGgGameVersion"
    else failwith $"no version axis covers package '{packageId}'"

// ---------------------------------------------------------------------------------------------
// The pin-grounded proof: compile the call sites against the RESTORED pinned packages.
// ---------------------------------------------------------------------------------------------

/// A stalled restore must fail, not hang. Generous enough for a cold restore on a slow runner.
let private probeTimeoutMs = 6 * 60 * 1000

/// Probe-private packages folder, OUTSIDE the throwaway work dir so a cold restore is paid once per
/// machine rather than on every test run. It is still not the machine's global packages folder — the
/// point of the isolation is that nothing this repo `pack`s locally can ever be resolved from here;
/// only nuget.org can populate it, and a published (id, version) is immutable, so reuse is safe.
let private probePackagesDir =
    Path.Combine(Path.GetTempPath(), "fsgg-pinned-api-probe-packages")

let private runProbeBuild () =
    let workDir = Path.Combine(Path.GetTempPath(), "fsgg-pinned-api-probe-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory workDir |> ignore

    try
        // The namespaces actually called, each one a package to restore at its axis pin.
        let packages =
            callSites
            |> List.choose callNamespace
            |> List.distinct
            |> List.sort

        let references =
            packages
            |> List.map (fun id -> $"    <PackageReference Include=\"{id}\" Version=\"{pinFor id}\" />")
            |> String.concat "\n"

        // The probe must see what a REAL scaffolded product sees: the PUBLISHED package on nuget.org.
        //
        // Restoring from the ambient NuGet cache would defeat the whole test. This repo's own
        // `dotnet pack` writes locally-built FS.GG.* packages into the machine's global packages
        // folder, and a locally-packed 0.8.0 carries whatever was in `src/` at pack time — including
        // the very seam the published 0.8.0 might not have. Resolve against that and the probe goes
        // GREEN precisely when the published pin is missing the API, which is the failure it exists
        // to catch. So: `<clear />` the sources down to nuget.org, and restore into a probe-local
        // packages folder that no local pack can have seeded.
        let nugetConfig =
            """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"""

        // Built OUTSIDE the repo tree so the repo's Directory.Build.props / central package
        // management / locked-restore rules do not apply to the probe.
        //
        // NU1603 (the pin does not exist, so NuGet quietly resolved UPWARD to the nearest version
        // that does) and NU1101/NU1102 (no such package/version at all) are ERRORS here, exactly as
        // in scripts/validate-template-payload-pins.fsx. Without that, a nonexistent pin would
        // silently restore a NEWER package that does contain the API, and the probe would prove the
        // opposite of what it claims.
        let project =
            $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <RestorePackagesPath>{probePackagesDir}</RestorePackagesPath>
    <WarningsAsErrors>NU1603;NU1101;NU1102;NU1608</WarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Probe.fs" />
{references}
  </ItemGroup>
</Project>
"""

        // `nameof` is a COMPILE-TIME symbol resolution: it needs no value, no type instantiation and
        // no generic instantiation (so a generic entry point like `Viewer.runApp<'model,'msg>` does
        // not trip the value restriction), and it fails to compile if the pinned package does not
        // export the member. That is exactly the question, and nothing more.
        let probe = StringBuilder()
        probe.AppendLine("module Probe").AppendLine() |> ignore

        for ns in packages do
            probe.AppendLine($"open {ns}") |> ignore

        probe.AppendLine().AppendLine("let private entryPointsTheTemplateCalls : string list =").AppendLine("    [") |> ignore

        for call in callSites |> List.sortBy (fun c -> c.Module, c.Member) do
            probe.AppendLine($"      nameof {call.Module}.{call.Member}") |> ignore

        probe.AppendLine("    ]") |> ignore

        File.WriteAllText(Path.Combine(workDir, "NuGet.config"), nugetConfig)
        File.WriteAllText(Path.Combine(workDir, "Probe.fsproj"), project)
        File.WriteAllText(Path.Combine(workDir, "Probe.fs"), probe.ToString())

        let psi = ProcessStartInfo("dotnet", "build Probe.fsproj -c Release -m:1 --nologo")
        psi.WorkingDirectory <- workDir
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

        match Process.Start psi with
        | null -> failwith "could not start 'dotnet' to build the pinned-API probe"
        | started ->
            use proc = started

            // Both pipes are drained CONCURRENTLY. Reading one to the end before touching the other
            // deadlocks the moment the child fills the pipe it is NOT being read from — the child
            // blocks writing, the parent blocks reading, and neither moves. `dotnet build` emits
            // plenty on both, and this test is default-on, so that would hang the gate rather than
            // fail it.
            let output = StringBuilder()

            let append (data: string | null) =
                match data with
                | null -> ()
                | text -> lock output (fun () -> output.AppendLine text |> ignore)

            proc.OutputDataReceived.Add(fun e -> append e.Data)
            proc.ErrorDataReceived.Add(fun e -> append e.Data)
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()

            // And the wait is BOUNDED. The accepted cost of running this by default is that a feed
            // outage can turn the gate RED; an unbounded wait would instead let a stalled restore
            // HANG it until the job timeout, which is strictly worse and is not what was signed up
            // for. A timeout is reported as a failure with its own message, not as a missing API.
            if proc.WaitForExit probeTimeoutMs then
                // Let the async readers flush before the buffer is read.
                proc.WaitForExit()
                proc.ExitCode, lock output (fun () -> output.ToString())
            else
                try proc.Kill true with _ -> ()

                let minutes = probeTimeoutMs / 60_000

                -1,
                $"the pinned-API probe did not finish within {minutes} minute(s) — most likely the \
                  restore from nuget.org stalled. This is an infrastructure failure, NOT a missing \
                  API.\n\n{lock output (fun () -> output.ToString())}"
    finally
        try Directory.Delete(workDir, true) with _ -> ()

// ---------------------------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------------------------

[<Tests>]
let templateConsumesPinnedApiTests =
    testList "Template consumes the pinned framework API (#504)" [

        // The extractor is the load-bearing part of every assertion below it. If it silently matches
        // nothing, every other test in this list passes VACUOUSLY — green because it checked
        // nothing, which is the failure this whole item exists to stop.
        test "the template's Program.fs calls framework entry points (extractor is not vacuous)" {
            Expect.isNonEmpty frameworkModules "the bundled api-surface mirror declares framework modules"

            Expect.isNonEmpty
                callSites
                $"framework entry points were extracted from {programPath}. Zero call sites means the \
                  extractor has stopped seeing the template's framework usage — that is a defect in \
                  this test, not a passing template."
        }

        // The seam from #429 — the concrete API that existed in `src/`, shipped in no package the
        // template pinned, and was unreachable from every scaffolded product for the life of 0.8.0.
        // If a refactor ever stops the extractor from seeing the viewer launch calls, this is the
        // test that says so out loud rather than quietly reducing the check to nothing.
        test "the viewer launch seam is among the extracted call sites" {
            let extracted = callSites |> List.map (fun c -> $"{c.Module}.{c.Member}") |> Set.ofList
            let rendered = extracted |> Set.toList |> String.concat ", "

            [ "Viewer.runApp"; "Viewer.runAppWithAudio"; "ControlsElmish.runInteractiveApp" ]
            |> List.iter (fun entryPoint ->
                Expect.isTrue
                    (extracted.Contains entryPoint)
                    $"'{entryPoint}' is one of the framework entry points the template's Program.fs calls \
                      (extracted: {rendered})")
        }

        // Offline necessary-but-not-sufficient condition. It CANNOT catch #429 (the mirror tracks
        // `src/`, so it advertises the seam the pin lacks) — the pin-grounded test below is what
        // does. It catches the other direction: a call site that names nothing at all.
        test "every framework entry point the template calls exists in the bundled mirror" {
            let unresolved =
                callSites
                |> List.filter (fun c -> (owningModule c).IsNone)
                |> List.map (fun c -> $"{c.Module}.{c.Member} (Program.fs:{c.Line})")

            Expect.equal
                (String.concat "; " unresolved)
                ""
                "every framework entry point called by the template's Program.fs is declared in the \
                 bundled api-surface mirror"
        }

        // THE assertion #504 asks for, and it runs BY DEFAULT — including on the gate.
        //
        // It is deliberately NOT opt-in. The sibling restore proofs
        // (scripts/validate-template-payload-pins.fsx) are gated behind an opt-in env var that the
        // WORKFLOW sets; this test cannot rely on that, because switching it on would mean editing
        // gate.yml. An opt-in check that nothing opts into is a check that never runs — it would
        // report green having verified nothing, which is the exact fails-open shape (#266) that let
        // #429 sit unreachable for the life of 0.8.0. #504 exists to fire ON the PR, so it fires.
        //
        // The cost is honest and named: this restores from nuget.org, so the gate's test step now
        // depends on the feed being up. FS_GG_SKIP_TEMPLATE_PINNED_API=1 skips it for offline work —
        // an explicit, visible opt-OUT rather than a silent default-off.
        testCase "every framework entry point the template calls exists in the PINNED package" <| fun _ ->
            match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API" with
            | null | "" ->
                let exitCode, output = runProbeBuild ()
                let uiPin = readAxis "FsGgUiVersion"
                let audioPin = readAxis "FsGgAudioVersion"

                Expect.equal
                    exitCode
                    0
                    $"the template's framework call sites compile against the PINNED packages \
                      (FsGgUiVersion={uiPin}, FsGgAudioVersion={audioPin}). \
                      A failure here means the framework has grown public API that a scaffolded product \
                      CANNOT reach — the #429/#492 class. Either the seam is unreleased (cut the release, \
                      then bump the pin) or the template calls API that no longer exists.\n\n{output}"

            | _ ->
                skiptest
                    "FS_GG_SKIP_TEMPLATE_PINNED_API is set — the pinned-package proof did NOT run. This \
                     check is default-on; skipping it means the template-vs-pin question is unanswered."
    ]
