module Feature142SurfaceAndDependencyTests

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let rec private repoRoot (dir: DirectoryInfo) =
    if File.Exists(Path.Combine(dir.FullName, "Directory.Packages.props")) then
        dir.FullName
    else
        match dir.Parent with
        | null -> Directory.GetCurrentDirectory()
        | parent -> repoRoot parent

let private root = repoRoot (DirectoryInfo(AppContext.BaseDirectory))

[<Tests>]
let tests =
    testList "Feature142 surface and dependency guards" [
        test "SkiaViewer references SkiaSharp.HarfBuzz through central package management" {
            let central = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"))
            let fsproj = File.ReadAllText(Path.Combine(root, "src", "SkiaViewer", "SkiaViewer.fsproj"))

            Expect.stringContains central "SkiaSharp.HarfBuzz" "central package pin is present"
            Expect.stringContains fsproj "SkiaSharp.HarfBuzz" "SkiaViewer owns the package reference"
        }

        test "public text edge exposes provider status" {
            let status = Text.shapingProviderStatus ()
            Expect.isNonEmpty status.Evidence.ProviderId "provider id is readable"
            Expect.isNonEmpty status.Evidence.VersionBucket "provider bucket is readable"
        }

        test "Scene package remains free of Skia/HarfBuzz dependencies" {
            let fsproj = File.ReadAllText(Path.Combine(root, "src", "Scene", "Scene.fsproj"))
            for forbidden in [ "SkiaSharp"; "HarfBuzz"; "Silk.NET"; "Yoga" ] do
                Expect.isFalse (fsproj.Contains forbidden) $"Scene.fsproj must not reference {forbidden}"
        }
    ]
