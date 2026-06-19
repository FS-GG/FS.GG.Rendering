// Regenerates the public-surface baselines in tests/surface-baselines/ from the built assemblies.
// Run after an intended public-API change, then commit the updated *.txt. The CI gate runs this and
// fails on any uncommitted drift (it is the Principle II "visibility lives in .fsi" guard).
//
// Loads each assembly by path (with a cross-assembly resolve handler) rather than `#r` + a hardcoded
// representative type, so adding a package is a one-line table entry. Compiler-generated / anonymous
// types are EXCLUDED: their names embed a non-deterministic hash (e.g. `<>f__AnonymousType…`) and
// would make the baseline unstable across builds.

open System
open System.IO
open System.Reflection
open System.Runtime.CompilerServices

let scriptDir = __SOURCE_DIRECTORY__
let repoRoot = Path.GetFullPath(Path.Combine(scriptDir, ".."))

// Every package → its src project folder (assembly name == package name). One row per committed baseline.
let packages =
    [ "FS.GG.UI.Layout", "Layout"
      "FS.GG.UI.KeyboardInput", "KeyboardInput"
      "FS.GG.UI.Controls", "Controls"
      "FS.GG.UI.Controls.Elmish", "Controls.Elmish"
      "FS.GG.UI.DesignSystem", "DesignSystem"
      "FS.GG.UI.Diagnostics", "Diagnostics"
      "FS.GG.UI.Themes.AntDesign", "Themes.AntDesign"
      "FS.GG.UI.Themes.Default", "Themes.Default"
      "FS.GG.UI.Elmish", "Elmish"
      "FS.GG.UI.Input", "Input"
      "FS.GG.UI.Scene", "Scene"
      "FS.GG.UI.SkiaViewer", "SkiaViewer"
      "FS.GG.UI.Testing", "Testing" ]

let binDir proj = Path.Combine(repoRoot, "src", proj, "bin", "Debug", "net10.0")
let binDirs = packages |> List.map (snd >> binDir)

// Resolve cross-assembly dependencies (and native-adjacent managed deps) from any package bin dir,
// so GetExportedTypes can fully load type signatures without #r wiring.
AppDomain.CurrentDomain.add_AssemblyResolve(
    ResolveEventHandler(fun _ args ->
        let name = AssemblyName(args.Name).Name
        binDirs
        |> List.tryPick (fun d ->
            let f = Path.Combine(d, name + ".dll")
            if File.Exists f then Some(Assembly.LoadFrom f) else None)
        |> Option.toObj))

let isCompilerGenerated (ty: Type) =
    ty.GetCustomAttributes(typeof<CompilerGeneratedAttribute>, false).Length > 0
    || ty.Name.StartsWith("<", StringComparison.Ordinal)

let names (assembly: Assembly) =
    assembly.GetExportedTypes()
    |> Array.filter (fun ty -> not (isCompilerGenerated ty))
    |> Array.map (fun ty ->
        match ty.FullName with
        | null -> ty.Name
        | fullName when ty.Name.EndsWith("Module", StringComparison.Ordinal) -> fullName.Replace("Module", "")
        | fullName -> fullName)
    |> Array.distinct
    |> Array.sort

let write packageName values =
    let path = Path.Combine(repoRoot, "tests", "surface-baselines", packageName + ".txt")
    Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
    File.WriteAllLines(path, values)
    printfn "wrote %s (%d public types)" path (Array.length values)

for (packageName, proj) in packages do
    let dll = Path.Combine(binDir proj, packageName + ".dll")
    if not (File.Exists dll) then
        failwithf "missing %s — build the solution (Debug) before refreshing baselines" dll
    write packageName (names (Assembly.LoadFrom dll))
