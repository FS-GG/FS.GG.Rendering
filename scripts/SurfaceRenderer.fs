// The public-surface renderer: the ONE definition of how the built assemblies become the lines of
// `readiness/surface-baselines/**`.
//
// BOTH sides of the surface gate consume this file, so they cannot disagree:
//   - `scripts/refresh-surface-baselines.fsx` — WRITES the baselines (`#load`s this file)
//   - `tests/Package.Tests/SurfaceAreaTests.fs` — the GATE: re-derives the surface from the built
//     assemblies and compares it against the committed baselines (compiles this file in)
//
// Until #661 these were two hand-mirrored copies, and nothing enforced that they agreed. The comment
// that held them together claimed a divergence "cannot pass silently" — which was false in both
// directions:
//
//   - A bug present in BOTH copies is invisible to the comparison between them: two copies of one
//     mistake agree perfectly. All three emitter bugs #657 fixed were in both renderers, so each had
//     to be fixed twice, and until then the gate stayed green while recording something other than
//     the public surface.
//   - Fixing only ONE copy is worse: the writer emits a line the gate can never re-derive, so the
//     gate goes red and CANNOT be made green by regenerating — the standard remedy — because the
//     reader is the thing that is wrong.
//
// One definition removes both. There is nothing left to drift, and a change to the rendering now
// reaches the gate and the writer as the same change. Only the two baseline projections are public;
// every rendering step below is private, so no consumer can grow a second opinion about how a type
// or member is spelled.
module FsGg.Governance.SurfaceRenderer

open System
open System.Reflection
open System.Runtime.CompilerServices
open System.Text.RegularExpressions

// Compiler-generated / anonymous types and members are EXCLUDED: their names embed a
// non-deterministic hash (e.g. `<>f__AnonymousType…`) and would make the baseline unstable across
// builds. The same exclusion carries the member baseline: F# emits the structural
// `Equals`/`GetHashCode`/`CompareTo`/`ToString` overrides and every property accessor as
// [<CompilerGenerated>], so filtering them leaves the surface a reader actually authored.
let private isCompilerGenerated (m: MemberInfo) =
    m.GetCustomAttributes(typeof<CompilerGeneratedAttribute>, false).Length > 0
    || m.Name.StartsWith("<", StringComparison.Ordinal)

let private fullNameOf (ty: Type) =
    match ty.FullName with
    | null -> ty.Name
    | value -> value

// `fullName` always ends with `ty.Name`, so the suffix is exact at `Length - "Module".Length`. A
// `Replace` would strip the word wherever it occurs: `ModuleRegistryModule` renders as `…Registry`,
// naming a type that does not exist.
let private displayName (ty: Type) =
    let fullName = fullNameOf ty

    if ty.Name.EndsWith("Module", StringComparison.Ordinal) then
        fullName.Substring(0, fullName.Length - "Module".Length)
    else
        fullName

// Render a type reference the same way on every machine and every build: no assembly-qualified
// generic arguments (which `Type.FullName` embeds), no arity suffix, generic parameters by their own
// name. A signature line is only useful as a baseline if it is byte-identical across runs.
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
        // brackets. So: cut the arguments, then strip the arity from each segment. Truncating at the
        // FIRST backtick instead would erase a nested generic's own name — `Dictionary`2+Enumerator`
        // would render as plain `Dictionary` — and because `memberSignatures` ends in `Array.distinct`,
        // a member returning the enumerator and one returning the dictionary would collapse onto one
        // line. This renderer is also the gate, so that collapse is the gate going blind: a change
        // between those two members would compare equal to the baseline.
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

let private exportedTypes (assembly: Assembly) =
    assembly.GetExportedTypes() |> Array.filter (fun ty -> not (isCompilerGenerated ty))

/// One line per exported TYPE — the contents of `readiness/surface-baselines/<pkg>.txt`.
let typeNames (assembly: Assembly) =
    exportedTypes assembly |> Array.map displayName |> Array.distinct |> Array.sort

/// One line per exported MEMBER, with its signature — the contents of
/// `readiness/surface-baselines/members/<pkg>.txt`. Issue #200: type names alone cannot see a member
/// appear on or disappear from a type that already exists.
let memberSignatures (assembly: Assembly) =
    exportedTypes assembly
    |> Array.collect (fun ty ->
        let owner = displayName ty

        membersOf ty
        |> Array.filter (fun m -> not (isCompilerGenerated m))
        |> Array.choose (signature owner))
    |> Array.distinct
    |> Array.sort
