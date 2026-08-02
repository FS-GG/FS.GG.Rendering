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

    /// The chosen whole-corpus model (Rendering#1050). A fence is either a positive member of the
    /// published-pin compilation corpus, or a contextual teaching fragment whose reader/product-owned
    /// inputs make standalone compilation dishonest. Contextual is an explicit classification, not a
    /// compiler waiver: the inventory digest below forces every corpus edit back through this decision.
    type FenceDisposition =
        | SelfContained
        | Contextual of reason: string

    /// Isolated packages folder OUTSIDE any work dir, so a cold restore of the published pin is paid once
    /// per machine and reused. Nothing this repo `pack`s locally can seed it — only nuget.org can — so reuse
    /// is safe (a published (id, version) is immutable). Same reasoning as the probe it descends from.
    let private packagesDir =
        Path.Combine(Path.GetTempPath(), "fsgg-docfences-probe-packages")

    /// A stalled restore/build must fail, not hang. Generous enough for a cold restore on a slow runner.
    let private timeoutMs = 6 * 60 * 1000

    let private nugetConfig () =
        let localSource, localMapping =
            match Environment.GetEnvironmentVariable "FS_GG_PRODUCT_LOCAL_FEED" with
            | null
            | "" -> "", ""
            | feed ->
                let escapedFeed = System.Security.SecurityElement.Escape feed

                $"    <add key=\"product-local-feed\" value=\"{escapedFeed}\" />\n",
                """    <packageSource key="product-local-feed">
      <package pattern="FS.GG.UI" />
      <package pattern="FS.GG.UI.*" />
    </packageSource>
"""

        $"""<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
{localSource}    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
{localMapping}    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
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

            File.WriteAllText(Path.Combine(workDir, "NuGet.config"), nugetConfig ())
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

    /// Frozen identity of the product-skill inventory reviewed for Rendering#1050, re-audited for
    /// Rendering#1165 (3 new fsharp fences: 2 in fs-gg-testing/SKILL.md, 1 in fs-gg-ui-widgets/SKILL.md), and
    /// again after round 2 reworded two of those fences' Expecto assertion-message strings ("present"/"regions"
    /// were bare-word homonyms of real, unrelated shipped API vals — S-DOC finding 2). Re-audited again for
    /// Rendering#1166 (1 new fsharp fence in fs-gg-game-shell/SKILL.md, the pause-safe rebind + exact-persistence
    /// production-host journey; no new fences in fs-gg-keyboard-input/SKILL.md or fs-gg-testing/SKILL.md, whose
    /// prose-only additions shift the three fs-gg-testing self-contained keys below), and once more after
    /// review round 1 (independent critic `smew-a5d2`, finding 1) replaced that fs-gg-game-shell fence's body:
    /// the original never drove `Screen` to `Playing`, so its final assertion passed vacuously. The corrected
    /// fence drives real `GameShell.Start`/`OpenSettings`/`ArmRebind`/`LeaveSettings`/`ResumeGame` navigation to
    /// actually reach `Playing`, and adds explicit non-vacuity assertions (`Screen = Playing`, the held command
    /// is genuinely `Some` before the pause) alongside the original zero-intent and persistence-count checks.
    /// Verified against the real `template/base/src/Product/GameShell.fs` (not a stub) via a scratch
    /// `dotnet fsi` script, both with the fix present (passes) and with the fix removed (reddens) — see the PR
    /// discussion for the script. Re-audited once more after review round 3 (finding 3): the acceptance text
    /// was corrected to require asserting "unchanged preferences emit none" on the `GameShell.Effect` list
    /// itself (this template wires no `ViewerEffect.Persist` sink), so the fs-gg-game-shell fence now threads
    /// every step's effects instead of discarding most as `_`, and fs-gg-testing's two cross-references were
    /// reworded to describe that same seam instead of a sink that does not exist here — a prose-only edit that
    /// shifts the three fs-gg-testing self-contained keys below by the same +6 lines. It covers origin, body,
    /// skip reason, and extra opens. Any added, removed, moved, or edited fence fails before
    /// classification/compilation and must be deliberately re-audited.
    /// Re-audited for Rendering#1171: the fs-gg-game-shell/SKILL.md Public Contract line-38 field list
    /// and the `## Usage` fenced example both still described the pre-#1018 `Config.DefaultKeymap :
    /// Keymap` shape (an incomplete migration in c12a7606, which replaced it with
    /// `Config.Actions : KeyRebindAction list`); the example did not compile against the shipped
    /// `GameShell.Config`. Corrected both spots to construct `Actions: KeyRebindAction list` (matching
    /// the real `KeyRebindAction` catalog shape from `src/Controls/KeyRebind.fs`, `Binding = None` with
    /// `DefaultBinding = Some key` as the shipped `Product.Tests/ShellBehaviorTests.fs` fixture does —
    /// the live `Keymap` is derived inside `init` via `KeyRebind.restoreDefaults`, never supplied
    /// directly). This fence remains Contextual (unchanged classification, only its body moved), so it
    /// is still not fed to the compiler here; verified separately against the real shipped
    /// `AppRoot.GameShell`/`FS.GG.UI.Controls` types via a scratch `dotnet fsi` probe (compiles with the
    /// fix, fails FS0001 on `Config` having no field `DefaultKeymap` with it reverted). Re-audited for
    /// Rendering#1169: the two new fs-gg-testing examples teach a numbered scenario index with an exact
    /// 1..N guard, and a canonical whole-model encoding for pause invariants. Both are contextual because
    /// their `Scenario` assertion and `Model`/`Determinism`/`update` seams are product-owned; their full
    /// worked examples are retained in the corpus inventory and covered by the document fence parser. The
    /// road-map guard fence formerly marked self-contained has also been classified honestly as contextual:
    /// it intentionally composes the preceding `sectionsByHeading` example and the product test runner's
    /// Expecto dependency, neither of which the published-pin probe supplies in isolation.
    /// Re-audited for Rendering#1175: the new restaging fence is contextual because
    /// its model, fixture, guarded action, and room staging helpers are product-owned.
    /// Re-audited for Rendering#1176: the new fs-gg-scene empty-group and handle probes are `text`
    /// fences because their assertions are product-test examples; `Expect`, `Render`, the model, and
    /// the handle are not part of the published Scene package. The surrounding framework prose is the
    /// shipped contract, while the product-specific sketches remain contextual.
    let private expectedProductSkillInventory =
        "b47ece9cfd8b377e1f31063b605ba9ed5c0fb859ca6349291fa35516671a4922"

    /// Positive corpus members proven individually self-contained against the published pins. Everything
    /// else remains taught/guarded by the retained symbol oracle, but is not padded with invented product
    /// types merely to make a compiler probe green.
    let private selfContainedProductSkillFences =
        Set.ofList
            [ "template/product-skills/fs-gg-symbology/SKILL.md", 214
              "template/product-skills/fs-gg-symbology/SKILL.md", 226
              "template/product-skills/fs-gg-testing/SKILL.md", 337
              "template/product-skills/fs-gg-testing/SKILL.md", 362
              "template/product-skills/fs-gg-grids/SKILL.md", 61
              "template/product-skills/fs-gg-collision/SKILL.md", 104
              "template/product-skills/fs-gg-layout/SKILL.md", 44
              "template/product-skills/fs-gg-ui-widgets/SKILL.md", 97
              "template/product-skills/fs-gg-scene/SKILL.md", 25
              "template/product-skills/fs-gg-scene/SKILL.md", 78
              "template/product-skills/fs-gg-scene/SKILL.md", 139
              "template/product-skills/fs-gg-scene/SKILL.md", 213
              "template/product-skills/fs-gg-styling/SKILL.md", 83
              "template/product-skills/fs-gg-symbology/reference/labels.md", 90 ]

    /// Why the remaining fences in each document are contextual. Reasons describe the document's actual
    /// teaching shape; they are never used to reinterpret compiler errors.
    let private contextualReasonByDoc =
        Map.ofList
            [ "template/product-skills/fs-gg-line-drawing/SKILL.md",
              "uses product-owned LineDrawing helpers and values introduced by earlier examples"
              "template/product-skills/fs-gg-symbology/SKILL.md",
              "mixes signature fragments with reader-owned unit/roster values"
              "template/product-skills/fs-gg-elmish/SKILL.md",
              "composes the reader's Model, Msg, host, update, view, and earlier bindings"
              "template/product-skills/fs-gg-testing/SKILL.md",
              "uses product-owned domain types, fixtures, and helpers introduced by earlier examples"
              "template/product-skills/fs-gg-game-shell/SKILL.md",
              "shows product configuration fragments and a product-owned GameShell wrapper"
              "template/product-skills/fs-gg-grids/SKILL.md",
              "continues with a cell value introduced by the preceding example"
              "template/product-skills/fs-gg-collision/SKILL.md",
              "uses product-owned body wrappers, targets, and Collision composition"
              "template/product-skills/fs-gg-layout/SKILL.md",
              "uses product-owned responsive-layout helper functions"
              "template/product-skills/fs-gg-ui-widgets/SKILL.md",
              "continues from a reader-owned control value"
              "template/product-skills/fs-gg-scene/SKILL.md",
              "uses reader-owned scenes/animation state or intentionally ambient record labels"
              "template/product-skills/fs-gg-skiaviewer/SKILL.md",
              "assembles the reader's Model, Msg, host functions, effects, and launch options"
              "template/product-skills/fs-gg-visibility/SKILL.md",
              "uses product-owned world inputs and intentionally ambient geometry record labels"
              "template/product-skills/fs-gg-styling/SKILL.md",
              "uses product theme/model values and surrounding control-host context"
              "template/product-skills/fs-gg-symbol-design/SKILL.md",
              "contains walkthrough fragments, product frame state, and an FSI script"
              "template/product-skills/fs-gg-keyboard-input/SKILL.md",
              "contains expression fragments and the reader's Msg/keymap host"
              "template/product-skills/fs-gg-symbology/reference/labels.md",
              "projects labels from the reader's unit value" ]

    let private productSkillInventoryDigest (fences: Corpus.FenceBlock list) =
        let payload =
            fences
            |> List.sortBy (fun fence -> fence.Doc, fence.StartLine)
            |> List.map (fun fence ->
                String.Join(
                    "\u001f",
                    [| fence.Doc
                       string fence.StartLine
                       String.Join("\n", fence.Body)
                       (fence.Skip |> Option.defaultValue "")
                       String.Join("\n", fence.ExtraOpens) |]))
            |> fun entries -> String.Join("\u001e", entries)

        payload
        |> Encoding.UTF8.GetBytes
        |> Security.Cryptography.SHA256.HashData
        |> Convert.ToHexString
        |> fun value -> value.ToLowerInvariant()

    /// Classify the complete live product-skill inventory. This is deliberately fail-closed: corpus drift,
    /// a stale positive key, or an unknown document has no default classification.
    let productSkillCompilationPlan () : (Corpus.FenceBlock * FenceDisposition) list =
        let fences = Corpus.productSkillFences ()
        let actualInventory = productSkillInventoryDigest fences

        if actualInventory <> expectedProductSkillInventory then
            failwithf
                "product-skill fence inventory changed (expected %s, actual %s); re-audit every changed fence for Rendering#1050 classification"
                expectedProductSkillInventory
                actualInventory

        let liveKeys = fences |> List.map (fun fence -> fence.Doc, fence.StartLine) |> Set.ofList
        let stalePositiveKeys = Set.difference selfContainedProductSkillFences liveKeys

        if not stalePositiveKeys.IsEmpty then
            failwithf "self-contained fence keys no longer exist: %A" (Set.toList stalePositiveKeys)

        let skippedPositiveFences =
            fences
            |> List.choose (fun fence ->
                if
                    Set.contains (fence.Doc, fence.StartLine) selfContainedProductSkillFences
                    && fence.Skip.IsSome
                then
                    Some(fence.Doc, fence.StartLine, fence.Skip.Value)
                else
                    None)

        if not skippedPositiveFences.IsEmpty then
            failwithf
                "self-contained fences cannot carry docfences:skip (they must compile actively): %A"
                skippedPositiveFences

        fences
        |> List.map (fun fence ->
            if Set.contains (fence.Doc, fence.StartLine) selfContainedProductSkillFences then
                fence, SelfContained
            else
                match Map.tryFind fence.Doc contextualReasonByDoc with
                | Some reason -> fence, Contextual reason
                | None -> failwithf "no contextual classification for product-skill document %s" fence.Doc)

    /// Was a build failure caused by the PIN being unpublished (release window: NU1101/NU1102), rather than
    /// by a fence? Then the harness must skip, not fail — the `PinPending` waiver at the restore boundary
    /// (FR-012).
    let pinUnpublished (outcome: Outcome) =
        not outcome.Succeeded
        && (outcome.RawOutput.Contains "NU1101" || outcome.RawOutput.Contains "NU1102")
