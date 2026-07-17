namespace FS.GG.DocFences

open System
open System.IO
open System.Diagnostics
open System.Text

/// THE FENCE-COMPILE HARNESS (spec 255, US1 core — T015/T016/T017).
///
/// Turns extracted fences into compilation units, generates a project that `PackageReference`s the PUBLISHED
/// pin restored from nuget.org (cleared sources, isolated packages dir — the proven `runNameofProbe`
/// approach, so a locally-`pack`ed unreleased seam cannot fake a green), builds it, and maps each compiler
/// diagnostic back to the doc + line it came from. A fence naming a symbol the pin does not export then
/// fails to compile.
///
/// This is the minimum end-to-end mechanism the plan's "early live proof" (T005) exercises green + red
/// before any old machinery is deleted.
module Harness =

    /// A fence prepared for the compiler: a unique module (its name encodes the origin so a build error maps
    /// back), the `open` preamble that puts the pin's namespaces in scope (D2), and the fence body.
    type CompilationUnit =
        { ModuleName: string
          Origin: Corpus.FenceBlock
          Opens: string list
          Body: string list }

    /// A compiler diagnostic mapped back to the ORIGINAL document — so a failure is clickable (T017).
    type Diagnostic =
        { Doc: string
          Line: int
          Message: string }

    type Outcome =
        { Succeeded: bool
          RawOutput: string
          Diagnostics: Diagnostic list }

    /// Isolated packages folder OUTSIDE any work dir, so a cold restore of the published pin is paid once
    /// per machine and reused. Nothing this repo `pack`s locally can seed it — only nuget.org can — so reuse
    /// is safe (a published (id, version) is immutable). Same reasoning as the probe it descends from.
    let private packagesDir =
        Path.Combine(Path.GetTempPath(), "fsgg-docfences-probe-packages")

    /// A stalled restore/build must fail, not hang. Generous enough for a cold restore on a slow runner.
    let private timeoutMs = 6 * 60 * 1000

    let private nugetConfig =
        """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"""

    /// A generated unit's on-disk layout is DETERMINISTIC so a compiler line maps back to the doc line:
    ///   line 1        : `module <name>`
    ///   lines 2..1+N  : the N `open` lines
    ///   lines 2+N..   : the fence body, whose k-th line (0-based) is `Origin.StartLine + k`.
    let private unitSource (u: CompilationUnit) : string =
        let sb = StringBuilder()
        sb.AppendLine($"module {u.ModuleName}") |> ignore
        for o in u.Opens do
            sb.AppendLine($"open {o}") |> ignore
        for line in u.Body do
            sb.AppendLine(line) |> ignore
        sb.ToString()

    /// Recover the ORIGINAL doc line from a generated-file line: subtract the `module` line and the `open`
    /// preamble, then offset from the fence's start.
    let private originLine (u: CompilationUnit) (generatedLine: int) : int =
        let bodyIndex = generatedLine - 1 - List.length u.Opens - 1
        u.Origin.StartLine + max 0 bodyIndex

    // F# emits `path(line,col): error FSxxxx: message`. The path's file name is the unit module name.
    let private diagRegex =
        System.Text.RegularExpressions.Regex(
            @"^(?<file>.*?)\((?<line>\d+),\d+\):\s*(?<sev>error|warning)\s+(?<msg>FS\d+:.*)$",
            System.Text.RegularExpressions.RegexOptions.Compiled)

    /// Build all units in one generated project against the pin. ONE restore + build amortized over every
    /// fence. `packages` are the pinned `(id, version)` pairs to reference — read live from the props by
    /// `Pins.pinnedPackages`, so there is no second hardcoded oracle version.
    let compile (packages: (string * string) list) (units: CompilationUnit list) : Outcome =
        let workDir =
            Path.Combine(Path.GetTempPath(), "fsgg-docfences-" + Guid.NewGuid().ToString("N"))

        Directory.CreateDirectory workDir |> ignore

        try
            let references =
                packages
                |> List.map (fun (id, version) -> $"    <PackageReference Include=\"{id}\" Version=\"{version}\" />")
                |> String.concat "\n"

            let compiles =
                units
                |> List.map (fun u -> $"    <Compile Include=\"{u.ModuleName}.fs\" />")
                |> String.concat "\n"

            // Built OUTSIDE the repo tree so the repo's Directory.Build.props / central package management /
            // locked-restore rules do not apply. NU1603/NU1101/NU1102 are ERRORS so a nonexistent pin cannot
            // silently resolve UPWARD to a newer package that DOES contain the symbol (which would prove the
            // opposite of what the harness claims).
            let project =
                $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <RestorePackagesPath>{packagesDir}</RestorePackagesPath>
    <WarningsAsErrors>NU1603;NU1101;NU1102;NU1608</WarningsAsErrors>
    <DisableImplicitFSharpCoreReference>false</DisableImplicitFSharpCoreReference>
  </PropertyGroup>
  <ItemGroup>
{compiles}
{references}
  </ItemGroup>
</Project>
"""

            File.WriteAllText(Path.Combine(workDir, "NuGet.config"), nugetConfig)
            File.WriteAllText(Path.Combine(workDir, "DocFences.Probe.fsproj"), project)

            for u in units do
                File.WriteAllText(Path.Combine(workDir, $"{u.ModuleName}.fs"), unitSource u)

            let psi = ProcessStartInfo("dotnet", "build DocFences.Probe.fsproj -c Release -m:1 --nologo")
            psi.WorkingDirectory <- workDir
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            psi.UseShellExecute <- false

            match Process.Start psi with
            | null -> failwith "could not start 'dotnet' to build the doc-fence harness"
            | started ->
                use proc = started
                // Drain both pipes CONCURRENTLY — reading one to end first deadlocks when the child fills
                // the other. `dotnet build` emits on both.
                let outT = proc.StandardOutput.ReadToEndAsync()
                let errT = proc.StandardError.ReadToEndAsync()

                if not (proc.WaitForExit timeoutMs) then
                    (try proc.Kill true with _ -> ())
                    failwithf "doc-fence harness build timed out after %d ms" timeoutMs

                let output = outT.Result + errT.Result

                let byModule =
                    units |> List.map (fun u -> u.ModuleName, u) |> Map.ofList

                let diagnostics =
                    output.Replace("\r\n", "\n").Split('\n')
                    |> Array.choose (fun line ->
                        let m = diagRegex.Match line
                        if m.Success && m.Groups.["sev"].Value = "error" then
                            let file = Path.GetFileNameWithoutExtension(m.Groups.["file"].Value.Trim())
                            match Map.tryFind file byModule with
                            | Some u ->
                                Some
                                    { Doc = u.Origin.Doc
                                      Line = originLine u (int m.Groups.["line"].Value)
                                      Message = m.Groups.["msg"].Value.Trim() }
                            | None -> None
                        else
                            None)
                    |> List.ofArray

                { Succeeded = proc.ExitCode = 0
                  RawOutput = output
                  Diagnostics = diagnostics }
        finally
            try Directory.Delete(workDir, true) with _ -> ()

    /// A stable, filesystem-safe module name for a fence, encoding its origin so a diagnostic maps back.
    let moduleNameFor (index: int) (fence: Corpus.FenceBlock) : string =
        let slug =
            fence.Doc
            |> Seq.map (fun c -> if System.Char.IsLetterOrDigit c then c else '_')
            |> Seq.toArray
            |> System.String
        $"Fence_{index}_{slug}_L{fence.StartLine}"

    /// Turn the compilable fences of a corpus into units: corpus preamble (T006) + the fence's own
    /// `docfences:open` additions, skipping any fence marked `docfences:skip` (T007). Returns the units and
    /// the skipped fences (so a caller can report what was excluded, and why — never a silent drop).
    let unitsFor (fences: Corpus.FenceBlock list) : CompilationUnit list * (Corpus.FenceBlock * string) list =
        let skipped =
            fences |> List.choose (fun f -> f.Skip |> Option.map (fun r -> f, r))

        let units =
            fences
            |> List.filter (fun f -> Option.isNone f.Skip)
            |> List.mapi (fun i f ->
                { ModuleName = moduleNameFor i f
                  Origin = f
                  Opens = Preamble.forKind f.Kind @ f.ExtraOpens
                  Body = f.Body })

        units, skipped

    /// Was a build failure caused by the PIN being unpublished (release window: NU1101/NU1102), rather than
    /// by a fence? Then the harness must skip, not fail — the `PinPending` waiver at the restore boundary
    /// (FR-012).
    let pinUnpublished (outcome: Outcome) =
        not outcome.Succeeded
        && (outcome.RawOutput.Contains "NU1101" || outcome.RawOutput.Contains "NU1102")
