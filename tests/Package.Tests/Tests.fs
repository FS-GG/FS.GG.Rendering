module PackageTests

open System
open System.Diagnostics
open System.IO
open Expecto
open FS.GG.TestSupport

let repositoryRoot = RepositoryRoot.value

let repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

// Feature 045: build.fsx was relocated into compiled build/Governance modules; the PackLocal
// package list and build wiring now live there. Aggregate those sources for the contract
// assertions that historically scanned build.fsx text (behaviour/intent preserved).
let buildFrontEnd () =
    let dir = Path.Combine(repositoryRoot, "build", "Governance")

    if Directory.Exists dir then
        Directory.GetFiles(dir, "*.fs", SearchOption.AllDirectories)
        |> Array.filter (fun p ->
            let n = p.Replace('\\', '/')
            not (n.Contains "/bin/" || n.Contains "/obj/"))
        |> Array.sort
        |> Array.map File.ReadAllText
        |> String.concat Environment.NewLine
    else
        ""

let runDotnetWithin (timeoutMilliseconds: int) (workingDirectory: string) (arguments: string) =
    let startInfo: ProcessStartInfo = ProcessStartInfo("dotnet", arguments)
    startInfo.WorkingDirectory <- workingDirectory
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.UseShellExecute <- false

    match Process.Start(startInfo) |> Option.ofObj with
    | None -> failwithf "Could not start dotnet %s" arguments
    | Some proc ->
        use proc = proc
        let stdoutTask = proc.StandardOutput.ReadToEndAsync()
        let stderrTask = proc.StandardError.ReadToEndAsync()

        if proc.WaitForExit(timeoutMilliseconds) then
            proc.ExitCode, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult()
        else
            proc.Kill(true)
            -1, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult()

/// Version the consumer smoke packs the coherent set at. It is not the repo's shipped version; it
/// only has to agree with the `PackageReference` the generated consumer restores, so pack and
/// reference both read it from here. Packing at an implicit default (1.0.0) while referencing a
/// literal here is what previously made the smoke resolve nothing and pass vacuously.
let packageVersion = "0.1.9-preview.1"

/// Packages the consumer smoke references directly.
let consumerSmokePackages =
    [ "FS.GG.UI.Scene"; "FS.GG.UI.Layout"; "FS.GG.UI.Controls"; "FS.GG.UI.Themes.Default" ]

/// Transitive project closure of `consumerSmokePackages`, in dependency order. The consumer restores
/// them all at `packageVersion`, a version on no public feed, so a partial feed cannot restore: every
/// FS.GG.UI.* package the four pull in has to be packed locally too. Ordered and packed one project
/// at a time rather than `pack FS.GG.Rendering.slnx`, which builds every package in parallel and
/// exhausts memory on smaller machines.
let consumerSmokeProjects =
    [ "src/Scene/Scene.fsproj"
      "src/Diagnostics/Diagnostics.fsproj"
      "src/Layout/Layout.fsproj"
      "src/KeyboardInput/KeyboardInput.fsproj"
      "src/DesignSystem/DesignSystem.fsproj"
      "src/Themes.Default/Themes.Default.fsproj"
      "src/Controls/Controls.fsproj" ]

/// A consumer that CALLS the packages rather than merely restoring them: it exports a scene through
/// SceneCodec, computes a real Yoga layout, and paints a Button through the default theme. Each call
/// is load-bearing — a package that restores but ships no native asset or no public entry point
/// fails here at build or at run, which a bare `dotnet restore` cannot detect.
let consumerSmokeProgram =
    """module PackageConsumerSmoke

open FS.GG.UI.Scene
open FS.GG.UI.Layout
open FS.GG.UI.Controls

type Msg = Clicked

[<EntryPoint>]
let main _ =
    let package = SceneCodec.export (Scene.rectangle (0.0, 0.0, 8.0, 8.0) Colors.white)

    if not (package.PackageIdentity.StartsWith "sha256:") then
        failwith "FS.GG.UI.Scene: SceneCodec.export produced no sha256 package identity"

    let layout = Layout.evaluate (Defaults.availableSpace 100.0 50.0) (Defaults.layoutNode "root")

    if List.isEmpty layout.Bounds then
        failwith "FS.GG.UI.Layout: Layout.evaluate computed no bounds"

    let theme = FS.GG.UI.Themes.Default.Theme.light
    let rendered = Control.render theme (Button.create [ Button.text "ok"; Button.onClick Clicked ])
    let renderedIdentity = (SceneCodec.export rendered.Scene).PackageIdentity

    if not (renderedIdentity.StartsWith "sha256:") then
        failwith "FS.GG.UI.Controls: Control.render produced no exportable scene"

    printfn "package consumer smoke: scene=%s layout=%d controls=%s" package.PackageIdentity layout.Bounds.Length renderedIdentity
    0
"""

[<Tests>]
let packageContractTests =
    let v1PackageTests = [
        test "active packages are declared for PackLocal" {
            let build = buildFrontEnd ()

            // V3 Stage 5: the monolith is retired; PackLocal packs the nine split packages only.
            [ "src/Scene/Scene.fsproj", "FS.GG.UI.Scene"
              "src/SkiaViewer/SkiaViewer.fsproj", "FS.GG.UI.SkiaViewer"
              "src/Layout/Layout.fsproj", "FS.GG.UI.Layout"
              "src/Controls.Elmish/Controls.Elmish.fsproj", "FS.GG.UI.Controls.Elmish"
              "src/Controls/Controls.fsproj", "FS.GG.UI.Controls" ]
            |> List.iter (fun (project, packageId) ->
                Expect.stringContains build project $"{project} is packed by PackLocal"
                Expect.stringContains build packageId $"{packageId} is packed by PackLocal")

            Expect.isFalse (build.Contains("\"src/Charts/Charts.fsproj\", \"FS.GG.UI.Charts\"")) "Charts is not an active PackLocal package"
        }

        test "controls boundary has no active Charts package capability or monolithic viewer coupling" {
            let build = buildFrontEnd ()
            let capabilities = File.ReadAllText(Path.Combine(repositoryRoot, "template", "capabilities.yml"))
            let controlsProject = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Controls", "Controls.fsproj"))

            // V3 Stage 5: the monolith project is retired; name it via parts so this guard
            // stays meaningful without re-introducing a literal monolith path reference.
            let monolithDir = "Lib"
            let monolithRef = $@"..\{monolithDir}\{monolithDir}.fsproj"

            Expect.isFalse (File.Exists(Path.Combine(repositoryRoot, "src", "Charts", "Charts.fsproj"))) "legacy Charts project is removed or deactivated from source ownership"
            Expect.isFalse (build.Contains("FS.GG.UI.Charts", StringComparison.Ordinal)) "build wiring has no active Charts package reference"
            Expect.isFalse (capabilities.Contains("id: charts", StringComparison.OrdinalIgnoreCase)) "generated capability catalog has no active charts capability"
            Expect.isFalse (controlsProject.Contains(monolithRef, StringComparison.Ordinal)) "Controls package does not depend on the retired monolithic viewer/runtime project"
            Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, "src", "Controls", "DataGrid.fsi"))) "DataGrid public contract is owned by Controls"
        }

        test "generated products and surface checks do not keep Charts as an active package" {
            let build = buildFrontEnd ()

            let generatedProductInputs =
                [ "template/capabilities.yml"
                  "template/profiles/app.yml"
                  "template/profiles/governed.yml"
                  "template/profiles/headless-scene.yml"
                  "template/profiles/sample-pack.yml"
                  "template/base/Directory.Packages.props"
                  "template/base/src/Product/Product.fsproj"
                  "template/base/.agents/skills/fs-gg-project/SKILL.md"
                  "scripts/refresh-surface-baselines.fsx" ]

            let forbiddenTokens =
                [ "PackageReference Include=\"FS.GG.UI.Charts\""
                  "src/Charts/Charts.fsproj"
                  "id: charts"
                  "template/fragments/charts"
                  ".agents/skills/fs-gg-charts/SKILL.md" ]

            let activeHits =
                generatedProductInputs
                |> List.filter (repositoryPath >> File.Exists)
                |> List.collect (fun relative ->
                    let content = File.ReadAllText(repositoryPath relative)

                    forbiddenTokens
                    |> List.choose (fun token ->
                        if content.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 then
                            Some $"{relative}: {token}"
                        else
                            None))

            Expect.isEmpty activeHits "active generated product inputs do not select Charts package, capability, project, or chart-specific generated skill"
            Expect.isFalse (build.Contains("\"FS.GG.UI.Charts\"", StringComparison.Ordinal)) "generated product package validation does not enumerate Charts as an available capability package"
            Expect.isFalse (File.Exists(repositoryPath "readiness/surface-baselines/FS.GG.UI.Charts.txt")) "legacy Charts package has no active surface baseline"
            Expect.isFalse (File.Exists(repositoryPath "template/fragments/charts/skill/SKILL.md")) "template has no chart-specific generated skill fragment"
            Expect.isFalse (File.Exists(repositoryPath "template/base/.agents/skills/fs-gg-charts/SKILL.md")) "generated product base has no chart-specific generated skill"
            Expect.stringContains build "readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt" "package surface report includes the Controls.Elmish adapter baseline"
            Expect.stringContains build "readiness/surface-baselines/FS.GG.UI.Controls.txt" "package surface report includes the Controls baseline"
            Expect.stringContains build "readiness/surface-baselines/FS.GG.UI.KeyboardInput.txt" "package surface report includes the KeyboardInput baseline"
        }

        // The smoke is too slow for the push gate, so it stays opt-in for Dev/Verify/Ci. "Opt-in"
        // only means something if something opts in: assert the release lane sets the flag, or the
        // pack -> consume path is tested nowhere and its green is worth nothing.
        test "the release lane opts the package consumer smoke in" {
            let release = File.ReadAllText(repositoryPath ".github/workflows/release.yml")

            Expect.stringContains
                release
                "FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE: \"1\""
                "release.yml must enable the package consumer smoke; never-by-default is not a cadence"
        }
    ]

    let deferredPackageSmokeTests =
        if Environment.GetEnvironmentVariable("FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE") = "1" then
            [ test "explicit package consumer smoke builds and runs a consumer against the packed feed" {
                  let feed = Path.Combine(Path.GetTempPath(), "fs-gg-ui-package-feed-" + Guid.NewGuid().ToString("N"))
                  Directory.CreateDirectory feed |> ignore

                  consumerSmokeProjects
                  |> List.iter (fun project ->
                      let exitCode, stdout, stderr =
                          runDotnetWithin 600000 repositoryRoot $"pack {project} -c Release -m:1 -p:Version={packageVersion} --output {feed}"

                      Expect.equal exitCode 0 $"packing {project} to the local feed:{Environment.NewLine}{stdout}{stderr}")

                  let missing =
                      consumerSmokePackages
                      |> List.filter (fun packageId -> not (File.Exists(Path.Combine(feed, $"{packageId}.{packageVersion}.nupkg"))))

                  Expect.isEmpty missing $"every package the consumer references was packed to the local feed (feed: {feed})"

                  let consumerRoot = Path.Combine(Path.GetTempPath(), "fs-gg-ui-package-consumer-" + Guid.NewGuid().ToString("N"))
                  Directory.CreateDirectory consumerRoot |> ignore

                  File.WriteAllText(
                      Path.Combine(consumerRoot, "NuGet.config"),
                      $"""<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="{feed}" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"""
                  )

                  let references =
                      consumerSmokePackages
                      |> List.map (fun packageId -> $"""    <PackageReference Include="{packageId}" Version="{packageVersion}" />""")
                      |> String.concat Environment.NewLine

                  // Central package management is on repo-wide; the consumer lives outside the repo's
                  // Directory.Packages.props, so it pins versions on the PackageReference itself.
                  File.WriteAllText(
                      Path.Combine(consumerRoot, "PackageConsumerSmoke.fsproj"),
                      $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.fs" />
  </ItemGroup>
  <ItemGroup>
{references}
  </ItemGroup>
</Project>
"""
                  )

                  File.WriteAllText(Path.Combine(consumerRoot, "Program.fs"), consumerSmokeProgram)

                  let buildExit, buildStdout, buildStderr = runDotnetWithin 600000 consumerRoot "build -c Release"
                  Expect.equal buildExit 0 (buildStdout + buildStderr)

                  // Building proves the public API compiles; running proves the packages' native and
                  // managed assets actually load. Restore alone proved neither.
                  let runExit, runStdout, runStderr = runDotnetWithin 300000 consumerRoot "run -c Release --no-build"
                  Expect.equal runExit 0 (runStdout + runStderr)
                  Expect.stringContains runStdout "package consumer smoke:" "the consumer executed its FS.GG.UI calls"
              } ]
        else
            []

    testList "Package contract" (v1PackageTests @ deferredPackageSmokeTests)
