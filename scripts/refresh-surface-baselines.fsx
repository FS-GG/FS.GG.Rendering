// Regenerates the public-surface baselines in readiness/surface-baselines/ from the built assemblies.
// Run after an intended public-API change, then commit the updated *.txt. The CI gate runs this and
// fails on any uncommitted drift (it is the Principle II "visibility lives in .fsi" guard).
//
// TWO baselines are written per package, because type names alone cannot see a member appearing or
// disappearing on a type that already exists (issue #200):
//   readiness/surface-baselines/<pkg>.txt          — one line per exported TYPE
//   readiness/surface-baselines/members/<pkg>.txt  — one line per exported MEMBER, with its signature
// The type-level file keeps its exact format: `build/Governance/PackageSurface.fs`, `Tests.fs` and the
// sample surface tests all read those paths by name. The member file is additive, so nothing that
// reads the old format has to change.
//
// This is the SINGLE authoritative baseline location: the live gate `SurfaceAreaTests` and the
// `build/Governance/PackageSurface.fs` governance check both READ `readiness/surface-baselines/`, so
// the generator WRITES there too — writer and readers agree by construction (no second copy to sync).
//
// Loads each assembly by path (with a cross-assembly resolve handler) rather than `#r` + a hardcoded
// representative type, so adding a package is a one-line table entry. Compiler-generated / anonymous
// types are EXCLUDED: their names embed a non-deterministic hash (e.g. `<>f__AnonymousType…`) and
// would make the baseline unstable across builds. The same exclusion carries the member baseline:
// F# emits the structural `Equals`/`GetHashCode`/`CompareTo`/`ToString` overrides and every property
// accessor as [<CompilerGenerated>], so filtering them leaves the surface a reader actually authored.

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Text.Json

let scriptDir = __SOURCE_DIRECTORY__
let repoRoot = Path.GetFullPath(Path.Combine(scriptDir, ".."))

// Every package → its src project folder (assembly name == package name). One row per committed baseline.
let packages =
    [ "FS.GG.UI.Build", "Build"
      "FS.GG.UI.Layout", "Layout"
      "FS.GG.UI.KeyboardInput", "KeyboardInput"
      "FS.GG.UI.Canvas", "Canvas"
      "FS.GG.UI.Controls", "Controls"
      "FS.GG.UI.Controls.Elmish", "Controls.Elmish"
      "FS.GG.UI.DesignSystem", "DesignSystem"
      "FS.GG.UI.Diagnostics", "Diagnostics"
      "FS.GG.UI.Themes.AntDesign", "Themes.AntDesign"
      "FS.GG.UI.Themes.Default", "Themes.Default"
      "FS.GG.UI.Elmish", "Elmish"
      "FS.GG.UI.Scene", "Scene"
      "FS.GG.UI.SkiaViewer", "SkiaViewer"
      "FS.GG.UI.Symbology", "Symbology"
      "FS.GG.UI.Symbology.Render", "Symbology.Render"
      "FS.GG.UI.Testing", "Testing" ]

let binDir proj = Path.Combine(repoRoot, "src", proj, "bin", "Debug", "net10.0")
let binDirs = packages |> List.map (snd >> binDir)

// Third-party dependencies are NOT copied into a library project's bin/ (only executables and test
// projects get the full closure), yet a public signature may name one — `SkiaViewer` exposes
// Silk.NET windowing types. Read the restore graph instead: `obj/project.assets.json` pins the exact
// version of every package the project resolved, so this probe needs nothing built but the packages
// themselves, and cannot drift from what the build used.
let private restoredAssemblies () =
    let probe = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)

    let tryProperty (element: JsonElement) (name: string) =
        match element.TryGetProperty name with
        | true, value -> Some value
        | _ -> None

    // Present, and actually a string. Anything else means this assets file cannot say where the
    // package lives, so skip it rather than compose a path out of null.
    let stringProperty element name =
        tryProperty element name
        |> Option.filter (fun value -> value.ValueKind = JsonValueKind.String)
        |> Option.bind (fun value -> Option.ofObj (value.GetString()))

    let harvest (assetsPath: string) =
        use doc = JsonDocument.Parse(File.ReadAllText assetsPath)
        let root = doc.RootElement

        let packagesPath =
            tryProperty root "project"
            |> Option.bind (fun project -> tryProperty project "restore")
            |> Option.bind (fun restore -> stringProperty restore "packagesPath")

        match packagesPath, tryProperty root "libraries", tryProperty root "targets" with
        | Some packagesPath, Some libraries, Some targets ->
            for target in targets.EnumerateObject() do
                for entry in target.Value.EnumerateObject() do
                    // Project-to-project references have no `path`; those resolve from the bin dirs above.
                    match libraries.TryGetProperty entry.Name with
                    | true, library ->
                        match stringProperty library "path" with
                        | Some relativeRoot ->
                            for section in [ "runtime"; "compile" ] do
                                match entry.Value.TryGetProperty section with
                                | true, files ->
                                    for file in files.EnumerateObject() do
                                        // `_._` is NuGet's "framework supported, but empty" placeholder.
                                        if not (file.Name.EndsWith("_._", StringComparison.Ordinal)) then
                                            let full =
                                                Path.Combine(
                                                    packagesPath,
                                                    relativeRoot,
                                                    file.Name.Replace('/', Path.DirectorySeparatorChar)
                                                )

                                            let simpleName = Path.GetFileNameWithoutExtension file.Name

                                            // `runtime` is probed before `compile`, so a real
                                            // implementation assembly always beats a reference one.
                                            if not (probe.ContainsKey simpleName) && File.Exists full then
                                                probe[simpleName] <- full
                                | _ -> ()
                        | None -> ()
                    | _ -> ()
        | _ -> ()
                            | _ -> ()
                    | _ -> ()
                | _ -> ()

    for (_, proj) in packages do
        let assets = Path.Combine(repoRoot, "src", proj, "obj", "project.assets.json")
        if File.Exists assets then harvest assets

    probe

let private restored = restoredAssemblies ()

// Resolve cross-assembly dependencies from a package bin dir first (so FS.GG.UI.* bind to the copies
// just built), then from the restore graph, so reflection can walk a full public signature.
AppDomain.CurrentDomain.add_AssemblyResolve(
    ResolveEventHandler(fun _ args ->
        let name = AssemblyName(args.Name).Name

        binDirs
        |> List.tryPick (fun d ->
            let f = Path.Combine(d, name + ".dll")
            if File.Exists f then Some f else None)
        |> Option.orElseWith (fun () ->
            match restored.TryGetValue name with
            | true, path -> Some path
            | _ -> None)
        |> Option.map Assembly.LoadFrom
        |> Option.toObj))

let isCompilerGenerated (m: MemberInfo) =
    m.GetCustomAttributes(typeof<CompilerGeneratedAttribute>, false).Length > 0
    || m.Name.StartsWith("<", StringComparison.Ordinal)

let fullNameOf (ty: Type) =
    match ty.FullName with
    | null -> ty.Name
    | value -> value

let displayName (ty: Type) =
    let fullName = fullNameOf ty

    if ty.Name.EndsWith("Module", StringComparison.Ordinal) then
        fullName.Replace("Module", "")
    else
        fullName

let exportedTypes (assembly: Assembly) =
    assembly.GetExportedTypes() |> Array.filter (fun ty -> not (isCompilerGenerated ty))

let names (assembly: Assembly) =
    exportedTypes assembly |> Array.map displayName |> Array.distinct |> Array.sort

// Render a type reference the same way on every machine and every build: no assembly-qualified
// generic arguments (which `Type.FullName` embeds), no `\`1` arity suffix, generic parameters by
// their own name. A signature line is only useful as a baseline if it is byte-identical across runs.
let rec typeRef (ty: Type) : string =
    if ty.IsGenericParameter then
        ty.Name
    elif ty.IsArray || ty.IsByRef || ty.IsPointer then
        let suffix =
            if ty.IsArray then "[]"
            elif ty.IsByRef then "&"
            else "*"

        match ty.GetElementType() with
        | null -> ty.Name
        | element -> typeRef element + suffix
    elif ty.IsGenericType then
        let stem =
            let raw = fullNameOf ty

            match raw.IndexOf('`') with
            | -1 -> raw
            | tick -> raw.Substring(0, tick)

        let args = ty.GetGenericArguments() |> Array.map typeRef |> String.concat ", "
        $"{stem}<{args}>"
    else
        fullNameOf ty

let private parameters (ps: ParameterInfo array) =
    ps |> Array.map (fun p -> typeRef p.ParameterType) |> String.concat ", "

// Property/event accessors are also emitted as `get_X`/`add_X` methods. The Property and Event lines
// already carry them, so dropping the accessor methods keeps one member = one line.
let private isAccessor (m: MethodInfo) =
    m.IsSpecialName
    && [ "get_"; "set_"; "add_"; "remove_" ]
       |> List.exists (fun prefix -> m.Name.StartsWith(prefix, StringComparison.Ordinal))

let private memberFlags =
    BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.DeclaredOnly

// Deliberately NOT `GetMembers`: that also populates nested types, which forces the runtime to
// resolve every assembly a nested type mentions. A library project's bin/ holds no third-party
// dependencies (only executables and test projects get them copied), so `GetMembers` on
// FS.GG.UI.Layout throws on Yoga.Net. Asking for each member kind resolves only what a public
// signature actually names — and nested types are exported types in their own right anyway, so
// they already get their own lines.
let private membersOf (ty: Type) : MemberInfo array =
    [| yield! ty.GetConstructors(memberFlags) |> Array.map (fun m -> m :> MemberInfo)
       yield! ty.GetMethods(memberFlags) |> Array.map (fun m -> m :> MemberInfo)
       yield! ty.GetProperties(memberFlags) |> Array.map (fun m -> m :> MemberInfo)
       yield! ty.GetFields(memberFlags) |> Array.map (fun m -> m :> MemberInfo)
       yield! ty.GetEvents(memberFlags) |> Array.map (fun m -> m :> MemberInfo) |]

let private signature (owner: string) (m: MemberInfo) =
    match m with
    | :? ConstructorInfo as ctor -> Some $"{owner}.new({parameters (ctor.GetParameters())})"
    | :? MethodInfo as method' when isAccessor method' -> None
    | :? MethodInfo as method' ->
        let generics =
            if method'.IsGenericMethodDefinition then
                let args = method'.GetGenericArguments() |> Array.map _.Name |> String.concat ", "
                $"<{args}>"
            else
                ""

        Some $"{owner}.{method'.Name}{generics}({parameters (method'.GetParameters())}) : {typeRef method'.ReturnType}"
    | :? PropertyInfo as property ->
        let accessors =
            [ if property.CanRead then "get"
              if property.CanWrite then "set" ]
            |> String.concat ", "

        Some $"{owner}.{property.Name} : {typeRef property.PropertyType} [{accessors}]"
    | :? FieldInfo as field -> Some $"{owner}.{field.Name} : {typeRef field.FieldType}"
    | :? EventInfo as event' ->
        match event'.EventHandlerType with
        | null -> Some $"{owner}.{event'.Name} : event"
        | handler -> Some $"{owner}.{event'.Name} : event {typeRef handler}"
    | _ -> None

let memberSignatures (assembly: Assembly) =
    exportedTypes assembly
    |> Array.collect (fun ty ->
        let owner = displayName ty

        membersOf ty
        |> Array.filter (fun m -> not (isCompilerGenerated m))
        |> Array.choose (signature owner))
    |> Array.distinct
    |> Array.sort

let private writeLines path (values: string array) noun =
    Directory.CreateDirectory(Path.GetDirectoryName(path: string)) |> ignore
    File.WriteAllLines(path, values)
    printfn "wrote %s (%d public %s)" path (Array.length values) noun

let write packageName (assembly: Assembly) =
    let root = Path.Combine(repoRoot, "readiness", "surface-baselines")
    writeLines (Path.Combine(root, packageName + ".txt")) (names assembly) "types"
    writeLines (Path.Combine(root, "members", packageName + ".txt")) (memberSignatures assembly) "members"

for (packageName, proj) in packages do
    let dll = Path.Combine(binDir proj, packageName + ".dll")
    if not (File.Exists dll) then
        failwithf "missing %s — build the solution (Debug) before refreshing baselines" dll
    write packageName (Assembly.LoadFrom dll)
