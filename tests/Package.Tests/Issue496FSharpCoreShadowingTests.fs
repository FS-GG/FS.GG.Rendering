module Issue496FSharpCoreShadowingTests

// FS-GG/FS.GG.Rendering#496 (generalizes #459).
//
// A public DU case named `Error`, on a type with no `[<RequireQualifiedAccess>]`, shadows
// `FSharp.Core`'s `Result.Error` for every consumer that opens the namespace. An ordinary decode
// railway -- the most common shape in the language -- then fails to compile with:
//
//     error FS0003: This value is not a function and cannot be applied.
//                   It has type 'DiagnosticSeverity', which does not accept arguments.
//
// A message that names neither `Result`, nor the shadowing, nor the fix. #459's origin report lost a
// build cycle to it on `ControlDiagnosticSeverity`; #496 then swept every public signature and found
// the same defect live on six MORE types across five packages -- including `FS.GG.UI.Scene`, which
// every generated product's `Model.fs` opens.
//
// WHY A GATE AND NOT JUST SIX MORE FIXES. The `[<RequireQualifiedAccess>]` policy was already written
// down and already applied ~12 times, always for this exact stated reason. It was applied to
// `PointerButton` and `FrameCause` -- and missed on every single type whose case is literally `Error`,
// which is the one name in the language it is most dangerous to shadow. The policy was enforced by
// whoever remembered. Nothing checked it. Fixing the seven without adding this gate leaves the eighth
// a matter of time.
//
// WHAT IT CHECKS. Every public `.fsi` under `src/` -- the signature files ARE the public surface, so a
// type absent from them cannot shadow anything for a consumer. A DU that declares a case colliding
// with an `FSharp.Core` constructor (`Error`, `Ok`, `Some`, `None`, `Choice1Of2`, `Choice2Of2`) must
// carry `[<RequireQualifiedAccess>]`. That attribute makes the collision unreachable: the bare name
// stops resolving to the case, so `Result.Error` keeps its meaning.
//
// HONESTY CAVEAT (constitution Principle V): a line-shape reader over signature files, not an F#
// parser -- the same modelling choice `ApiSurfaceMirrorTests`' M-MIR reader makes, and for the same
// reason (`.fsi` is the most regular F# there is). It models attributes, type headers and DU cases.
// It does NOT see: a collision created downstream by an `AutoOpen` module rather than by the `.fsi`
// itself, or a case reached through a type abbreviation. It proves one bounded thing precisely -- no
// PUBLIC DU in `src/**/*.fsi` shadows an `FSharp.Core` constructor without qualified access -- which
// is exactly the class that produced #459 and #496.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

/// The COMPILE-TIME half of the guard, and the stronger half.
///
/// The scan below proves the attribute is PRESENT. This proves it WORKS: four namespaces that each
/// declare a colliding DU case are opened at once, and then an utterly ordinary `Result` railway --
/// the shape #459's reporter was writing when they lost a build cycle -- is compiled against them.
/// Drop `[<RequireQualifiedAccess>]` from any of the four and this file stops building, with the same
/// FS0003 a consumer would get.
///
/// That is strictly stronger than any runtime assertion, because the failure being guarded against IS
/// a compile failure in consumer code, and no runtime test can observe one. (Symbology declares
/// `Legibility.Severity` and is not referenced by this project; its railway guard lives beside its own
/// tests, in Symbology.Tests/LegibilityTests.fs.)
module private ConsumerRailwayGuard =
    open FS.GG.UI.Scene // `DiagnosticSeverity.Error` -- and every generated product's Model.fs opens this
    open FS.GG.UI.Diagnostics // `DiagnosticSeverity.Error`
    open FS.GG.UI.Layout // `DiagnosticSeverity.Error`
    open FS.GG.UI.Controls // `ControlDiagnosticSeverity.Error` (#459)

    let decodeRailway (input: string) : Result<int, string> =
        match Int32.TryParse input with
        | true, value -> Ok value
        | _ -> Error $"not a number: {input}"

/// The `FSharp.Core` constructors a bare DU case can shadow. `Error`/`Ok` are the dangerous ones in
/// practice -- `Result` is the error-handling vocabulary of the whole language -- but a DU case named
/// `Some`/`None` shadows `Option` just as silently, so the whole set is banned rather than only the
/// two that happen to have bitten us.
let private fsharpCoreConstructors =
    set [ "Error"; "Ok"; "Some"; "None"; "Choice1Of2"; "Choice2Of2" ]

/// Types that still shadow, and are NOT fixed by #496 because another worker holds their files under
/// a live claim (intra-repo-parallel-work: a declared touch-set is a boundary, and editing across it
/// is how two workers silently clobber each other).
///
/// This is a RATCHET, not an amnesty. The second test below fails if an entry stops shadowing, so an
/// exemption cannot outlive its defect -- which is the "enforced by whoever remembers" failure this
/// whole gate exists to end.
///
/// Both entries are owned by FS.GG.Rendering#522, which lands once #456 releases `src/SkiaViewer/`
/// and `tests/SkiaViewer.Tests`. Fixing them REQUIRES deleting them from here, or the ratchet fails.
let private knownViolations =
    Set.ofList
        [ "src/SkiaViewer/Viewer.Types.fsi", "ViewerDiagnosticLevel"
          "src/SkiaViewer/Host/Diagnostics.fsi", "DiagnosticSeverity" ]

let private repositoryRoot = RepositoryRoot.value

let private relativePath (path: string) =
    Path.GetRelativePath(repositoryRoot, path).Replace(Path.DirectorySeparatorChar, '/')

let private signatureFiles () =
    Directory.EnumerateFiles(Path.Combine(repositoryRoot, "src"), "*.fsi", SearchOption.AllDirectories)
    |> Seq.sort
    |> List.ofSeq

let private attributeLine = Regex(@"^\s*\[<.*>\]\s*$", RegexOptions.Compiled)
let private typeHeader = Regex(@"^\s*type\s+(?<name>[A-Za-z_][A-Za-z0-9_']*)", RegexOptions.Compiled)
let private duCase = Regex(@"^\s*\|\s*(?<case>[A-Z][A-Za-z0-9_']*)", RegexOptions.Compiled)

/// One public DU read out of a signature file.
type private DuDecl =
    { File: string
      TypeName: string
      Qualified: bool
      Collisions: string list }

/// Reads the DU declarations out of one `.fsi`. An attribute block may sit on its own line(s) above
/// the `type` header or inline on it; a DU's cases are the `|`-led lines that follow, up to whatever
/// declaration comes next.
let private readDus (file: string) : DuDecl list =
    let results = ResizeArray<DuDecl>()
    let mutable pendingAttributes: string list = []
    let mutable current: (string * bool * string list) option = None

    let flush () =
        match current with
        | Some(name, qualified, cases) when not (List.isEmpty cases) ->
            let collisions = cases |> List.filter (fun c -> Set.contains c fsharpCoreConstructors)

            if not (List.isEmpty collisions) then
                results.Add
                    { File = relativePath file
                      TypeName = name
                      Qualified = qualified
                      Collisions = List.rev collisions }
        | _ -> ()

        current <- None

    for raw in File.ReadAllLines file do
        let line = raw.TrimEnd()
        let trimmed = line.TrimStart()

        if String.IsNullOrWhiteSpace line || trimmed.StartsWith "//" then
            () // Blank lines and comments sit inside DU bodies; they end nothing.
        elif attributeLine.IsMatch line then
            // An attribute block introduces the declaration BELOW it, so it closes the DU above it.
            flush ()
            pendingAttributes <- line :: pendingAttributes
        elif typeHeader.IsMatch line then
            flush ()
            let name = typeHeader.Match(line).Groups.["name"].Value
            let attributeText = String.Join(" ", line :: pendingAttributes)
            current <- Some(name, attributeText.Contains "RequireQualifiedAccess", [])
            pendingAttributes <- []
        elif duCase.IsMatch line then
            match current with
            | Some(name, qualified, cases) ->
                let case = duCase.Match(line).Groups.["case"].Value
                current <- Some(name, qualified, case :: cases)
            | None -> ()
        else
            // A `val`, a record field, a nested module -- anything that is not a case ends the DU.
            flush ()
            pendingAttributes <- []

    flush ()
    List.ofSeq results

/// Every public DU in `src/**/*.fsi` that declares an FSharp.Core-colliding case, fixed or not.
let private shadowingDus () =
    signatureFiles () |> List.collect readDus

let private describe (dus: DuDecl list) =
    dus
    |> List.map (fun du ->
        let cases = du.Collisions |> List.map (sprintf "`%s`") |> String.concat ", "
        sprintf "  %s: `%s` declares %s" du.File du.TypeName cases)
    |> String.concat "\n"

[<Tests>]
let fsharpCoreShadowingTests =
    testList "public DU cases never shadow FSharp.Core constructors" [

        test "no public DU shadows an FSharp.Core constructor without RequireQualifiedAccess" {
            let offenders =
                shadowingDus ()
                |> List.filter (fun du -> not du.Qualified)
                |> List.filter (fun du -> not (Set.contains (du.File, du.TypeName) knownViolations))

            Expect.isEmpty
                offenders
                (sprintf
                    "a public DU case colliding with an FSharp.Core constructor shadows it for every consumer who opens the namespace, and the compile error names neither Result nor the fix (#459, #496). Add [<RequireQualifiedAccess>] to:\n%s"
                    (describe offenders))
        }

        test "no known violation has been fixed without being removed from the list" {
            let stillShadowing =
                shadowingDus ()
                |> List.filter (fun du -> not du.Qualified)
                |> List.map (fun du -> du.File, du.TypeName)
                |> Set.ofList

            let stale = Set.difference knownViolations stillShadowing |> Set.toList

            let report =
                stale
                |> List.map (fun (file, typeName) -> sprintf "  %s: `%s`" file typeName)
                |> String.concat "\n"

            Expect.isEmpty
                stale
                (sprintf
                    "these types no longer shadow an FSharp.Core constructor, so they must be deleted from `knownViolations` -- an exemption that outlives its defect is how the next one hides (#496):\n%s"
                    report)
        }

        // Proves the reader actually SEES the surface it claims to check. A reader that silently
        // matched nothing would make both tests above pass vacuously, forever.
        test "the reader sees the types #459 and #496 fixed" {
            let qualified =
                shadowingDus ()
                |> List.filter (fun du -> du.Qualified)
                |> List.map (fun du -> du.File, du.TypeName)
                |> Set.ofList

            [ "src/Scene/Types.fsi", "DiagnosticSeverity"
              "src/Diagnostics/Diagnostics.fsi", "DiagnosticSeverity"
              "src/Layout/Types.fsi", "DiagnosticSeverity"
              "src/Symbology/Legibility.fsi", "Severity"
              "src/Controls/Types.fsi", "ControlDiagnosticSeverity" ]
            |> List.iter (fun (file, typeName) ->
                Expect.isTrue
                    (Set.contains (file, typeName) qualified)
                    (sprintf
                        "%s: `%s` declares an FSharp.Core-colliding case, so this reader must see it carrying [<RequireQualifiedAccess>]"
                        file
                        typeName))
        }
    ]
