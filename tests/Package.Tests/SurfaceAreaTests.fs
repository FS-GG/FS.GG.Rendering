module SurfaceAreaTests

open System
open System.IO
open System.Reflection
open Expecto

let rec findRepositoryRoot (directory: string) =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || File.Exists(Path.Combine(directory, "build.fsx")) then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | Some parent -> findRepositoryRoot parent.FullName
        | None -> failwithf "Could not locate repository root from %s" directory

let repositoryRoot = findRepositoryRoot AppContext.BaseDirectory

let baseline packageName =
    Path.Combine(repositoryRoot, "readiness", "surface-baselines", packageName + ".txt")
    |> File.ReadAllLines
    |> Array.filter (fun line -> line.Trim() <> "")
    |> Set.ofArray

let exportedNames (assembly: Assembly) =
    assembly.GetExportedTypes()
    |> Array.map (fun ty ->
        let fullName =
            match ty.FullName with
            | null -> ty.Name
            | value -> value

        if ty.Name.EndsWith("Module", StringComparison.Ordinal) then
            fullName.Replace("Module", "")
        else
            fullName)
    |> Set.ofArray

let assertBaseline packageName (assembly: Assembly) =
    let expected = baseline packageName
    let actual = exportedNames assembly
    let missing = Set.difference expected actual
    let unexpected = Set.difference actual expected
    Expect.isEmpty missing $"expected public surface for {packageName} is exported"
    Expect.isEmpty unexpected $"no unapproved public exports were added to {packageName}"

[<Tests>]
let surfaceAreaTests =
    testList "Surface baselines" [
        test "surface baselines use stable root readiness path" {
            // V3 Stage 5: the retired FS.GG.UI monolith no longer owns a surface baseline;
            // the stable-root-path invariant is asserted against a live split package.
            let baselinePath =
                Path.Combine(repositoryRoot, "readiness", "surface-baselines", "FS.GG.UI.Scene.txt")

            Expect.isTrue (File.Exists baselinePath) "stable FS.GG.UI.Scene package surface baseline exists"
            Expect.isFalse (baselinePath.Contains("specs/002-skia-feature-parity", StringComparison.Ordinal)) "baseline path is not historical feature readiness"
        }

        test "FS.GG.UI.Layout baseline exports expected contract names" {
            assertBaseline "FS.GG.UI.Layout" typeof<FS.GG.UI.Layout.GraphDefinition>.Assembly
        }

        test "SkiaViewer package exposes selected generated persistent viewer entry point" {
            let assemblyPath =
                Path.Combine(repositoryRoot, "src", "SkiaViewer", "bin", "Debug", "net10.0", "FS.GG.UI.SkiaViewer.dll")

            Expect.isTrue (File.Exists assemblyPath) "SkiaViewer assembly has been built"

            let assembly = Assembly.LoadFrom assemblyPath

            let viewerModule =
                match assembly.GetType("FS.GG.UI.SkiaViewer.Viewer") |> Option.ofObj with
                | Some value -> value
                | None ->
                    failtest "SkiaViewer package exports Viewer module"
                    typeof<obj>

            let methodNames =
                viewerModule.GetMethods(BindingFlags.Public ||| BindingFlags.Static)
                |> Array.map _.Name
                |> Set.ofArray

            Expect.contains methodNames "runApp" "selected generated persistent launch contract is packaged"
            Expect.contains methodNames "runAppWithWindowBehavior" "window-behavior overload remains packaged but is not the generated default"
        }

        test "FS.GG.UI.Controls baseline exports expected contract names" {
            assertBaseline "FS.GG.UI.Controls" typeof<FS.GG.UI.Controls.Control<int>>.Assembly
        }

        test "V3 capability packages declare package-specific contracts and baselines" {
            [ "Scene", "src/Scene/Scene.fsproj", "src/Scene/Scene.fsi", "readiness/surface-baselines/FS.GG.UI.Scene.txt"
              "SkiaViewer", "src/SkiaViewer/SkiaViewer.fsproj", "src/SkiaViewer/SkiaViewer.fsi", "readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt"
              "Elmish", "src/Elmish/Elmish.fsproj", "src/Elmish/Elmish.fsi", "readiness/surface-baselines/FS.GG.UI.Elmish.txt"
              "KeyboardInput", "src/KeyboardInput/KeyboardInput.fsproj", "src/KeyboardInput/KeyboardInput.fsi", "readiness/surface-baselines/FS.GG.UI.KeyboardInput.txt"
              "Layout", "src/Layout/Layout.fsproj", "src/Layout/Layout.fsi", "readiness/surface-baselines/FS.GG.UI.Layout.txt"
              "Controls", "src/Controls/Controls.fsproj", "src/Controls/Types.fsi", "readiness/surface-baselines/FS.GG.UI.Controls.txt"
              "Controls.Elmish", "src/Controls.Elmish/Controls.Elmish.fsproj", "src/Controls.Elmish/ControlsElmish.fsi", "readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt"
              "Testing", "src/Testing/Testing.fsproj", "src/Testing/Testing.fsi", "readiness/surface-baselines/FS.GG.UI.Testing.txt" ]
            |> List.iter (fun (name, project, contract, baseline) ->
                Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, project))) $"{name} project exists"
                Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, contract))) $"{name} public .fsi contract exists"
                Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, baseline))) $"{name} package surface baseline exists")
        }

        test "controls boundary public contracts include runtime rich rendering and FSI transcript coverage" {
            [ "src/Controls/ControlRuntime.fsi"
              "src/Controls/RichText.fsi"
              "src/Controls/DataGrid.fsi"
              "src/Controls/Charts.fsi"
              "src/Controls/CustomControl.fsi"
              "src/KeyboardInput/KeyboardInput.fsi"
              "src/Controls.Elmish/ControlsElmish.fsi" ]
            |> List.iter (fun contract ->
                Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, contract))) $"{contract} is a curated public contract")

            [ "scripts/controls-prelude.fsx"
              "scripts/input-prelude.fsx"
              "scripts/controls-elmish-prelude.fsx" ]
            |> List.iter (fun transcriptScript ->
                Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, transcriptScript))) $"{transcriptScript} produces public FSI evidence")

            [ "readiness/surface-baselines/FS.GG.UI.Controls.txt"
              "readiness/surface-baselines/FS.GG.UI.KeyboardInput.txt"
              "readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt" ]
            |> List.iter (fun baselinePath ->
                Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, baselinePath))) $"{baselinePath} exists for package surface review")
        }

        test "removed Charts package has no active surface baseline participation" {
            let chartsBaseline =
                Path.Combine(repositoryRoot, "readiness", "surface-baselines", "FS.GG.UI.Charts.txt")

            Expect.isFalse (File.Exists chartsBaseline) "legacy Charts package surface baseline is removed from active package review"
        }

        test "Controls FSI transcript authors chart graph and DataGrid without Charts package" {
            let transcript = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "controls-prelude.fsx"))

            [ "open FS.GG.UI.Controls"
              "LineChart.create"
              "GraphView.create"
              "DataGrid.create"
              "DataGrid.columns"
              "DataGrid.rows" ]
            |> List.iter (fun required ->
                Expect.stringContains transcript required $"Controls FSI transcript includes {required}")

            [ "FS.GG.UI.Charts"
              "open FS.GG.UI.Charts"
              "#r \"../src/Charts" ]
            |> List.iter (fun forbidden ->
                Expect.isFalse (transcript.Contains(forbidden, StringComparison.Ordinal)) $"Controls FSI transcript does not use {forbidden}")
        }

        test "Scene package stays dependency-light" {
            let sceneProject = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Scene", "Scene.fsproj"))

            [ "Fable.Elmish"
              "Silk.NET"
              "SkiaSharp"
              "Yoga.Net"
              "YamlDotNet" ]
            |> List.iter (fun forbidden -> Expect.isFalse (sceneProject.Contains forbidden) $"Scene does not reference {forbidden}")
        }

        test "top-level F# visibility modifiers do not replace signature ownership" {
            let sourceFiles =
                Directory.EnumerateFiles(Path.Combine(repositoryRoot, "src"), "*.fs", SearchOption.AllDirectories)
                |> Seq.filter (fun path -> not (path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) && not (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))

            let offending =
                sourceFiles
                |> Seq.collect (fun file ->
                    File.ReadAllLines(file)
                    |> Seq.mapi (fun index line -> file, index + 1, line.TrimStart())
                    |> Seq.choose (fun (file, lineNumber, line) ->
                        if line.StartsWith("private ", StringComparison.Ordinal)
                           || line.StartsWith("internal ", StringComparison.Ordinal)
                           || line.StartsWith("public ", StringComparison.Ordinal) then
                            Some($"{file}:{lineNumber}: {line}")
                        else
                            None))
                |> Seq.toList

            Expect.isEmpty offending "top-level visibility stays in .fsi files"
        }

        test "US4 maintained package surfaces are governed by paired signatures and active baselines" {
            let implementationRoots =
                [ "src/Controls"
                  "src/KeyboardInput"
                  "src/Controls.Elmish" ]

            implementationRoots
            |> List.collect (fun relative ->
                Directory.EnumerateFiles(Path.Combine(repositoryRoot, relative), "*.fs", SearchOption.TopDirectoryOnly)
                |> Seq.map (fun implementation -> implementation, Path.ChangeExtension(implementation, ".fsi"))
                |> Seq.toList)
            |> List.iter (fun (implementation, signature) ->
                Expect.isTrue (File.Exists signature) $"{implementation} has a paired .fsi signature")

            [ "readiness/surface-baselines/FS.GG.UI.Controls.txt", "FS.GG.UI.Controls.DataGrid"
              "readiness/surface-baselines/FS.GG.UI.KeyboardInput.txt", "FS.GG.UI.KeyboardInput.KeyboardModel"
              "readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt", "FS.GG.UI.Controls.Elmish.ControlsElmish" ]
            |> List.iter (fun (baselinePath, expectedExport) ->
                let content = File.ReadAllText(Path.Combine(repositoryRoot, baselinePath))
                Expect.stringContains content expectedExport $"{baselinePath} contains {expectedExport}")

            Expect.isFalse (File.Exists(Path.Combine(repositoryRoot, "readiness", "surface-baselines", "FS.GG.UI.Charts.txt"))) "removed Charts baseline is not active"
        }
    ]
