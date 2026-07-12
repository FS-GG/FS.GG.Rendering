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
// reason (`.fsi` is the most regular F# there is). It models attributes (above the header or inline
// on it), `type` AND `and` headers, accessibility, and DU cases; a record field or a wrapped case
// payload is a continuation, not a boundary.
//
// It does NOT see: a collision created downstream by an `AutoOpen` module rather than by the `.fsi`
// itself, or a case reached through a type abbreviation. Nor does it check the `.fsx` skill
// references, which are not compiled by the solution -- #496's own review caught four of them still
// teaching the bare `Legibility.Warning` AFTER the type was fixed, which is precisely the defect
// class this gate exists to end, and the gap is worth closing next.
//
// It proves one bounded thing precisely: no PUBLIC DU in `src/**/*.fsi` shadows an `FSharp.Core`
// constructor without qualified access -- which is exactly the class that produced #459 and #496.

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

/// Types that still shadow, and are NOT fixed because another worker holds their files under a live
/// claim (intra-repo-parallel-work: a declared touch-set is a boundary, and editing across it is how
/// two workers silently clobber each other).
///
/// This is a RATCHET, not an amnesty. The second test below fails if an entry stops shadowing, so an
/// exemption cannot outlive its defect -- which is the "enforced by whoever remembers" failure this
/// whole gate exists to end.
///
/// EMPTY, as of #522. It carried the two `src/SkiaViewer/` types #496 could not reach — `ViewerDiagnosticLevel`
/// and Host `DiagnosticSeverity` — and it said, in as many words, that fixing them REQUIRED deleting them from
/// here or the ratchet would fail. #522 fixed them, so they are gone, and the ratchet did exactly what it was
/// built to do: it made the exemption impossible to forget, because leaving it would have turned the fix RED.
///
/// Keep it empty. A new entry is a promise to come back, and it is only worth the paper if the second test
/// below is the one collecting on it.
let private knownViolations : Set<string * string> = Set.empty

let private repositoryRoot = RepositoryRoot.value

let private relativePath (path: string) =
    Path.GetRelativePath(repositoryRoot, path).Replace(Path.DirectorySeparatorChar, '/')

let private signatureFiles () =
    Directory.EnumerateFiles(Path.Combine(repositoryRoot, "src"), "*.fsi", SearchOption.AllDirectories)
    |> Seq.sort
    |> List.ofSeq

/// A leading attribute block, e.g. `[<RequireQualifiedAccess>]` or `[<NoEquality; NoComparison>]`,
/// possibly several in a row. Stripped from the front of a line so an attribute written INLINE with
/// its declaration (`[<Struct>] type Foo =`) is not mistaken for a bare attribute line.
let private leadingAttributes = Regex(@"^\s*(?:\[<[^\]]*>\]\s*)+", RegexOptions.Compiled)

/// A DU header. `and` is load-bearing: F# declares mutually recursive types with it, and this repo's
/// public surface really does (`and ScreenshotCaptureMode`, `and ChildOp<'msg>`, `and AttrValue<'msg>`).
/// A reader that only matched `type` would be blind to an entire declaration form -- and the eighth
/// shadowing case could then land inside one and keep this gate green, which is the one outcome it
/// exists to prevent. `Accessibility` is captured so non-public types can be skipped: they are not
/// visible to a consumer and so cannot shadow anything for one.
let private typeHeader =
    Regex(
        @"^(?:type|and)\s+(?:(?<access>private|internal|public)\s+)?(?:rec\s+)?(?<name>[A-Za-z_][A-Za-z0-9_']*)",
        RegexOptions.Compiled
    )

/// Any line that OPENS a new declaration, and therefore ends whatever DU we were reading. Everything
/// else -- a record field, a wrapped case payload (`| Bar of` / `    x: int`), a constraint clause --
/// is a CONTINUATION and must not end it. The earlier version of this reader flushed on any
/// unrecognised line, so a case whose payload wrapped silently truncated its DU and dropped every
/// case after it. (Same shape as `ApiSurfaceMirrorTests`' M-MIR reader, which learned this the hard
/// way; the two cannot share code because M-MIR models signatures and this models attributes.)
let private opensDeclaration =
    Regex(@"^(?:val|type|and|module|namespace|open)\b", RegexOptions.Compiled)

let private duCase = Regex(@"^\s*\|\s*(?<case>[A-Z][A-Za-z0-9_']*)", RegexOptions.Compiled)

/// One public DU read out of a signature file.
type private DuDecl =
    { File: string
      TypeName: string
      Qualified: bool
      Collisions: string list }

/// Reads the public DU declarations out of one `.fsi`. An attribute block may sit on its own line(s)
/// above the header or inline on it; a DU's cases are the `|`-led lines that follow, up to the next
/// line that OPENS a declaration.
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
        // Split a line into its leading attribute block and whatever declaration follows it, so
        // `[<RequireQualifiedAccess>]` reads the same whether it sits above the header or on it.
        let attributes = leadingAttributes.Match(line).Value
        let declaration = leadingAttributes.Replace(line, "").TrimStart()

        if String.IsNullOrWhiteSpace line || trimmed.StartsWith "//" then
            () // Blank lines and doc comments sit inside DU bodies; they end nothing.
        elif not (String.IsNullOrWhiteSpace attributes) && String.IsNullOrWhiteSpace declaration then
            // A bare attribute line: it introduces the declaration BELOW, so it closes the DU above.
            flush ()
            pendingAttributes <- attributes :: pendingAttributes
        elif typeHeader.IsMatch declaration then
            flush ()
            let m = typeHeader.Match declaration
            let qualified =
                String.Join(" ", attributes :: pendingAttributes).Contains "RequireQualifiedAccess"

            // `type internal Foo` is not part of the public surface, so it cannot shadow anything for
            // a consumer. Skip it rather than report it under the name "internal".
            let isPublic =
                let access = m.Groups.["access"].Value
                access = "" || access = "public"

            current <- if isPublic then Some(m.Groups.["name"].Value, qualified, []) else None
            pendingAttributes <- []
        elif opensDeclaration.IsMatch declaration then
            // `val`, `module`, `namespace`, `open` -- ends the DU, and its attributes are not ours.
            flush ()
            pendingAttributes <- []
        elif duCase.IsMatch line then
            match current with
            | Some(name, qualified, cases) ->
                current <- Some(name, qualified, duCase.Match(line).Groups.["case"].Value :: cases)
            | None -> ()
        else
            () // A record field or a wrapped case payload: a continuation, not a boundary.

    flush ()
    List.ofSeq results

/// Every public DU in `src/**/*.fsi` that declares an FSharp.Core-colliding case, fixed or not.
/// Read once: it is a pure function of the tree, and all three tests below ask for it.
let private shadowingDus =
    lazy (signatureFiles () |> List.collect readDus)

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
                shadowingDus.Value
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
                shadowingDus.Value
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
                shadowingDus.Value
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
