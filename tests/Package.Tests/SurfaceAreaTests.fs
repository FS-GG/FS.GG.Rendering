module SurfaceAreaTests

open System
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let repositoryRoot = RepositoryRoot.value

let private readBaseline (path: string) =
    path |> File.ReadAllLines |> Array.filter (fun line -> line.Trim() <> "") |> Set.ofArray

let baseline packageName =
    Path.Combine(repositoryRoot, "readiness", "surface-baselines", packageName + ".txt")
    |> readBaseline

/// Issue #200: the type-name baseline above cannot see a member added to or removed from a type that
/// already exists. `members/<package>.txt` closes that: one line per exported member, with signature.
let memberBaseline packageName =
    Path.Combine(repositoryRoot, "readiness", "surface-baselines", "members", packageName + ".txt")
    |> readBaseline

let private fullNameOf (ty: Type) =
    match ty.FullName with
    | null -> ty.Name
    | value -> value

// `fullName` always ends with `ty.Name`, so the suffix is exact at `Length - "Module".Length`. A
// `Replace` would strip the word wherever it occurs: `ModuleRegistryModule` renders as `…Registry`,
// naming a type that does not exist. Must match `scripts/refresh-surface-baselines.fsx`.
let private displayName (ty: Type) =
    let fullName = fullNameOf ty

    if ty.Name.EndsWith("Module", StringComparison.Ordinal) then
        fullName.Substring(0, fullName.Length - "Module".Length)
    else
        fullName

let exportedNames (assembly: Assembly) =
    assembly.GetExportedTypes() |> Array.map displayName |> Set.ofArray

let assertBaseline packageName (assembly: Assembly) =
    let expected = baseline packageName
    let actual = exportedNames assembly
    let missing = Set.difference expected actual
    let unexpected = Set.difference actual expected
    Expect.isEmpty missing $"expected public surface for {packageName} is exported"
    Expect.isEmpty unexpected $"no unapproved public exports were added to {packageName}"

// ---------------------------------------------------------------------------------------------
// Member-level surface. This mirrors the renderer in `scripts/refresh-surface-baselines.fsx`, which
// WRITES `members/<package>.txt`; here we re-derive the signatures and READ that file back. The two
// renderers must agree character-for-character — and if they ever drift apart, this comparison is
// what fails, so the divergence cannot pass silently.
// ---------------------------------------------------------------------------------------------

let private isCompilerGenerated (m: MemberInfo) =
    m.GetCustomAttributes(typeof<CompilerGeneratedAttribute>, false).Length > 0
    || m.Name.StartsWith("<", StringComparison.Ordinal)

/// Stable rendering of a type reference: no assembly-qualified generic arguments, no arity suffix.
let rec private typeRef (ty: Type) : string =
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
        // A constructed generic's `FullName` carries the arity on EVERY generic segment, not just the
        // last (`Dictionary`2+Enumerator[[…],[…]]`), and appends its arguments assembly-qualified in
        // brackets. Cut the arguments, then strip the arity per segment. Truncating at the FIRST
        // backtick instead would erase a nested generic's own name — and because THIS renderer is the
        // gate (it re-derives the signatures and compares them to the baseline), that erasure is the
        // gate going blind: a member returning `Dictionary<K,V>.Enumerator` and one returning
        // `Dictionary<K,V>` would render to the same line, and a change between them would compare
        // equal. Must match `scripts/refresh-surface-baselines.fsx`.
        let stem =
            let raw = fullNameOf ty

            let withoutArguments =
                match raw.IndexOf('[') with
                | -1 -> raw
                | bracket -> raw.Substring(0, bracket)

            Regex.Replace(withoutArguments, @"`\d+", "")

        let args = ty.GetGenericArguments() |> Array.map typeRef |> String.concat ", "
        $"{stem}<{args}>"
    else
        fullNameOf ty

let private parameters (ps: ParameterInfo array) =
    ps |> Array.map (fun p -> typeRef p.ParameterType) |> String.concat ", "

let private isAccessor (m: MethodInfo) =
    m.IsSpecialName
    && [ "get_"; "set_"; "add_"; "remove_" ]
       |> List.exists (fun prefix -> m.Name.StartsWith(prefix, StringComparison.Ordinal))

let private memberFlags =
    BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.DeclaredOnly

/// Deliberately not `GetMembers`, which also populates nested types and so forces the runtime to
/// resolve assemblies a public signature never names. See the generator for the full rationale.
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

let exportedMembers (assembly: Assembly) =
    assembly.GetExportedTypes()
    |> Array.filter (fun ty -> not (isCompilerGenerated ty))
    |> Array.collect (fun ty ->
        let owner = displayName ty

        membersOf ty
        |> Array.filter (fun m -> not (isCompilerGenerated m))
        |> Array.choose (signature owner))
    |> Set.ofArray

let assertMemberBaseline packageName (assembly: Assembly) =
    let expected = memberBaseline packageName
    let actual = exportedMembers assembly
    let removed = Set.difference expected actual
    let added = Set.difference actual expected
    Expect.isEmpty removed $"no public member was removed from {packageName} without updating its baseline"
    Expect.isEmpty added $"no unapproved public member was added to {packageName}"

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

        // Issue #200: a member added to or removed from an already-exported type moves none of the
        // type-name baselines above. These three assert the member-level baseline for the packages
        // this project already reflects; the gate.yml drift step regenerates and diffs the baselines
        // of all sixteen packages, so no package is left without a member-level guard.
        test "FS.GG.UI.Layout member baseline pins every exported member" {
            assertMemberBaseline "FS.GG.UI.Layout" typeof<FS.GG.UI.Layout.GraphDefinition>.Assembly
        }

        test "FS.GG.UI.Controls member baseline pins every exported member" {
            assertMemberBaseline "FS.GG.UI.Controls" typeof<FS.GG.UI.Controls.Control<int>>.Assembly
        }

        test "FS.GG.UI.Build member baseline pins every exported member" {
            assertMemberBaseline "FS.GG.UI.Build" typeof<FS.GG.UI.Build.Evidence.EvidenceNode>.Assembly
        }

        test "SkiaViewer package exposes selected generated persistent viewer entry point" {
            let assembly = typeof<FS.GG.UI.SkiaViewer.ViewerOptions>.Assembly

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

        test "FS.GG.UI.Build engine baseline exports expected contract names" {
            // Feature 202: the in-repo governance engine is a public package → its curated .fsi is
            // governed by the same surface-drift baseline as every other FS.GG.UI.* package.
            assertBaseline "FS.GG.UI.Build" typeof<FS.GG.UI.Build.Evidence.EvidenceNode>.Assembly
        }

        test "V3 capability packages declare package-specific contracts and baselines" {
            [ "Build", "src/Build/FS.GG.UI.Build.fsproj", "src/Build/Evidence.fsi", "readiness/surface-baselines/FS.GG.UI.Build.txt"
              "Scene", "src/Scene/Scene.fsproj", "src/Scene/Scene.fsi", "readiness/surface-baselines/FS.GG.UI.Scene.txt"
              "SkiaViewer", "src/SkiaViewer/SkiaViewer.fsproj", "src/SkiaViewer/SkiaViewer.fsi", "readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt"
              "Elmish", "src/Elmish/Elmish.fsproj", "src/Elmish/Elmish.fsi", "readiness/surface-baselines/FS.GG.UI.Elmish.txt"
              "KeyboardInput", "src/KeyboardInput/KeyboardInput.fsproj", "src/KeyboardInput/KeyboardInput.fsi", "readiness/surface-baselines/FS.GG.UI.KeyboardInput.txt"
              "Layout", "src/Layout/Layout.fsproj", "src/Layout/Layout.fsi", "readiness/surface-baselines/FS.GG.UI.Layout.txt"
              "Controls", "src/Controls/Controls.fsproj", "src/Controls/Types.fsi", "readiness/surface-baselines/FS.GG.UI.Controls.txt"
              "Controls.Elmish", "src/Controls.Elmish/Controls.Elmish.fsproj", "src/Controls.Elmish/ControlsElmish.fsi", "readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt"
              "Diagnostics", "src/Diagnostics/Diagnostics.fsproj", "src/Diagnostics/Diagnostics.fsi", "readiness/surface-baselines/FS.GG.UI.Diagnostics.txt"
              "Symbology", "src/Symbology/Symbology.fsproj", "src/Symbology/Symbology.fsi", "readiness/surface-baselines/FS.GG.UI.Symbology.txt"
              "Symbology.Render", "src/Symbology.Render/Symbology.Render.fsproj", "src/Symbology.Render/Render.fsi", "readiness/surface-baselines/FS.GG.UI.Symbology.Render.txt"
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
