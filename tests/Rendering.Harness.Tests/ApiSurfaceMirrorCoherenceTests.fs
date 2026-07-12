module ApiSurfaceMirrorCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only api-surface-mirror gate.
//
// WHY THIS EXISTS. The bundled `template/base/docs/api-surface/**` tree is the contract surface a
// generated product actually reads. `ApiSurfaceMirrorTests` (tests/Package.Tests) is the gate that
// keeps that tree honest — every referenced FS.GG.* package has a mirrored surface (M-REF), every
// bundled .fsi points at a shipped product skill (M-PTR), every cross-repo mirror is stamped with the
// version its template pins (M-PROV), and the #259 completeness checks that keep the mirror table from
// silently going stale. But Package.Tests is RELEASE-ONLY: it is not in `FS.GG.Rendering.slnx` and runs
// only under `dotnet test … -c Release` in release.yml. So a PR that references a new FS.GG.* package
// without bundling its surface, drops a `// See skill:` pointer, or bumps a cross-repo pin without
// recopying the stamped mirror compiles green and only reds the release lane post-merge — the exact
// "PR-gated drift gates must be in the slnx" gap #350, #382 and the BOM slice already closed.
//
// WHAT IT LOCKS. `ApiSurfaceMirrorTests` is already fully static and self-contained: no pack, no
// restore, no consumer graph — it reads the bundled tree, `Product.fsproj`, the template's pinned
// versions, the pending-release ledger and the shipped product-skills, all as files on disk. So this
// hoist is a FAITHFUL mirror: it reads the SAME source-of-truth inputs and asserts the SAME invariants,
// one gate earlier —
//   M-REF   every FS.GG.* package Product.fsproj references has a mirrored api-surface directory.
//   M-PTR   every bundled .fsi names exactly one shipped product skill (with the recorded exemptions).
//   M-PROV  every cross-repo mirror records the source-repo version the template pins, and the mirror
//           table stays complete (a bundled dir with no src/ original must be registered cross-repo).
//   in-repo exact copies still match their src/ originals (the stronger, byte-level half of M-PROV).
//   M-MIR   the CONTENT check (#437) — every val a mirror teaches exists in src with an identical
//           signature (M-MIR/VAL), and every type it declares carries src's exact member set (M-MIR/TYPE).
//   P-PEND  the declared exceptions to M-MIR/TYPE (#594) — a member src has, the pin has not, and the
//           mirror therefore omits: it must be real (P-PEND/SRC), actually omitted (P-PEND/OMIT), and
//           stamped with the current pin (P-PEND/PIN).
//
// M-MIR AND P-PEND WERE NOT HERE UNTIL #594, AND L-INPUTS COULD NOT SEE THAT THEY WERE MISSING. #437 added
// M-MIR to the counterpart and nobody hoisted it, so this "faithful mirror" quietly asserted four rules to
// its counterpart's six. L-INPUTS is supposed to catch exactly that, and did not: it compares the paths each
// side names as a LITERAL argument to the path helper, and M-MIR's extra input is not one — it is
// INTERPOLATED, per directory (see `sourceSurface` below). The regex never matched it, the two input sets
// compared equal, and one side ran two rules the other did not. #594 needed a new LITERAL input (the
// ledger), which is the only reason the pairing was ever forced. That blind spot outlives this fix:
// FS.GG.Rendering#612.
//
// BE PRECISE ABOUT THE DAMAGE, THOUGH — IT WAS FIDELITY, NOT COVERAGE. The paragraph below says
// Package.Tests is release-only and absent from the slnx. THAT IS NO LONGER TRUE: #540 made it a slnx
// MEMBER, for this very reason ("its ~325 working-tree rules ran in ZERO gate cadences"). So M-MIR has been
// running on the PR gate all along — through Package.Tests itself, not through this twin — and its absence
// here left no rule unchecked on any PR. What it left was a twin that lied about being a mirror, and a
// lockstep guard that agreed with it. Which also means the WHY THIS EXISTS below is stale for every twin in
// this directory, not just this one: FS.GG.Rendering#613 asks whether the mechanism still earns its
// duplication now that its premise has been retired.
//
// Kept in deliberate lockstep with tests/Package.Tests/ApiSurfaceMirrorTests.fs: the two read the same
// repo inputs and assert the same invariants, so a real drift fails BOTH — the release check is
// mirrored earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing so the mirror
// cannot silently desync. This body is a verbatim hoist of the release-only rule (module name and
// header aside); keeping it byte-faithful is what makes the L-INPUTS lockstep check exact.

open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private apiSurfaceRoot = repositoryPath "template/base/docs/api-surface"
let private productProjPath = repositoryPath "template/base/src/Product/Product.fsproj"
let private templatePackagesPath = repositoryPath "template/base/Directory.Packages.props"
let private productSkillsRoot = repositoryPath "template/product-skills"

/// FS.GG.UI.Controls.Elmish -> Controls.Elmish; FS.GG.Audio.Core -> Audio.Core; FS.GG.Game.Core -> Game.Core.
/// The bundled directory is the package id with its org prefix stripped — longest prefix first.
let private surfaceDirectoryFor (packageId: string) =
    if packageId.StartsWith "FS.GG.UI." then packageId.Substring("FS.GG.UI.".Length)
    elif packageId.StartsWith "FS.GG." then packageId.Substring("FS.GG.".Length)
    else failwithf "not an FS.GG package id: %s" packageId

/// Mirrors copied verbatim from ANOTHER FS-GG repo. Each carries a provenance stamp, because a
/// cross-repo copy is the one that can outlive the package it claims (ADR-0024). Keyed by bundled
/// directory -> the source repo it was copied from, and the template property pinning that repo.
let private crossRepoMirrors =
    Map
        [ "Audio.Core", ("FS.GG.Audio", "FsGgAudioVersion")
          "Audio.Host", ("FS.GG.Audio", "FsGgAudioVersion")
          "Audio.Engine", ("FS.GG.Audio", "FsGgAudioVersion")
          "Audio.Elmish", ("FS.GG.Audio", "FsGgAudioVersion")
          "Game.Core", ("FS.GG.Game", "FsGgGameVersion") ]

/// A bundled directory is cross-repo exactly when this repo has no `src/<dir>` to have copied it from.
/// Derived, not trusted: it lets the suite CHECK the table above is complete rather than take its word,
/// so a newly-bundled cross-repo mirror cannot slip in unstamped the way Game.Core did (#259).
let private bundledSurfaceDirectories () =
    Directory.GetDirectories apiSurfaceRoot
    |> Array.map (fun d -> DirectoryInfo(d).Name)
    |> Array.toList

let private hasInRepoSource (directory: string) =
    Directory.Exists(repositoryPath $"src/{directory}")

/// Mirrors copied verbatim from THIS repo's src/, so their freshness is checkable directly rather
/// than by a stamp. `Scene/` is deliberately absent: it is a hand-MERGED surface (it inlines
/// Types.fsi), not a file copy, so it has no single original to compare against.
let private inRepoExactCopies =
    [ "Controls.Elmish/ControlsElmish.fsi", "src/Controls.Elmish/ControlsElmish.fsi"
      "Controls.Elmish/Authoring.fsi", "src/Controls.Elmish/Authoring.fsi" ]

let private referencedFsGgPackages () =
    Regex.Matches(File.ReadAllText productProjPath, @"<PackageReference\s+Include=""(FS\.GG\.[^""]+)""")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Seq.distinct
    |> Seq.toList

let private bundledFsiFiles () =
    Directory.GetFiles(apiSurfaceRoot, "*.fsi", SearchOption.AllDirectories)
    |> Array.toList

let private relativeToSurfaceRoot (file: string) =
    Path.GetRelativePath(apiSurfaceRoot, file).Replace('\\', '/')

/// The bundled package directory a given .fsi sits in (`.../api-surface/Audio.Host/Host.fsi` -> `Audio.Host`).
let private owningPackageDirectory (file: string) =
    (relativeToSurfaceRoot file).Split('/').[0]

/// Everything above the `namespace` declaration. Both header stamps are read from here, so a stray
/// occurrence deeper in the file cannot satisfy either gate.
let private headerLines (file: string) =
    File.ReadAllLines file
    |> Array.takeWhile (fun line -> not (line.StartsWith "namespace"))

let private skillPointers (file: string) =
    headerLines file
    |> Array.choose (fun line ->
        let m = Regex.Match(line, @"^//\s*See skill:\s*(\S+)\s*$")
        if m.Success then Some m.Groups.[1].Value else None)
    |> Array.toList

/// The version stamped on a mirror copied from `repo`. A stamp naming a DIFFERENT repo does not
/// count — otherwise a mis-copied file could satisfy the gate with someone else's provenance.
let private provenanceVersion (repo: string) (file: string) =
    headerLines file
    |> Array.tryPick (fun line ->
        let m = Regex.Match(line, $@"^// Mirrored from FS-GG/{Regex.Escape repo} @ (\S+)")
        if m.Success then Some m.Groups.[1].Value else None)

/// A mirrored file minus its `//` header stamps, for comparison against a src/ original that has none.
let private bodyOf (path: string) =
    File.ReadAllLines path
    |> Array.skipWhile (fun line -> line.StartsWith "//")
    |> String.concat "\n"

/// The mirror ships the PRODUCT-VISIBLE surface only (S-INT, #585), so an exact-copy mirror is its
/// src original MINUS the internal declarations. The transform is defined once, in TestSupport, and
/// shared with the Symbology mirror gate — a copy of it in each gate would rot.
let private stripInternalDeclarations (text: string) =
    text.Replace("\r\n", "\n").Split('\n')
    |> SurfaceSignature.stripInternalDeclarations
    |> String.concat "\n"

/// No shipped product skill covers FS.GG.UI.Canvas's immediate-mode drawing surface. Recorded
/// here rather than papered over with a pointer to a skill that does not teach these members.
/// (`Canvas/Loop.fsi` removed with the surface at the 0.6.0 framework major — ADR-0104 decision 5,
/// #319/#355; mirrors the release-only Package.Tests/ApiSurfaceMirrorTests pointerExempt.)
let private pointerExempt = set [ "Canvas/Elements.fsi" ]

let private shippedProductSkills () =
    Directory.GetDirectories productSkillsRoot
    |> Array.map (fun d -> DirectoryInfo(d).Name)
    |> Set.ofArray

let private pinnedVersion (property: string) =
    let m = Regex.Match(File.ReadAllText templatePackagesPath, $"<{Regex.Escape property}>([^<]+)</{Regex.Escape property}>")

    if m.Success then
        m.Groups.[1].Value
    else
        failwithf "template/base/Directory.Packages.props pins no %s" property

/// Each distinct version property resolved once, rather than re-reading the props file per .fsi.
let private pinnedVersions () =
    crossRepoMirrors
    |> Map.toList
    |> List.map (fun (_, (_, property)) -> property)
    |> List.distinct
    |> List.map (fun property -> property, pinnedVersion property)
    |> Map.ofList

module private Fsi =

    /// A field or a DU case. `Name` identifies it; `Text` is the whole normalized declaration, so a
    /// member whose TYPE changed fails as loudly as one that was removed.
    type Member = { Name: string; Text: string }

    type Surface =
        {
            /// val name -> every signature declared under that name (two modules may each declare `create`).
            Vals: Map<string, Set<string>>
            /// (type name, generic arity) -> one member set per declaration of it (see the ViewerEffect trap).
            Types: Map<string * int, Set<Member> list>
        }

    let private normalize (text: string) = Regex.Replace(text, @"\s+", " ").Trim()

    let private indentOf (line: string) = line.Length - line.TrimStart().Length

    /// A line that opens a new declaration, and therefore ENDS whatever we were accumulating. The
    /// word boundary is load-bearing: without it a wrapped signature's own parameter truncates it —
    /// `typeName: string ->` starts with `type`, `andThen: …` with `and`, `openPath: …` with `open`.
    let private opensDeclaration (line: string) =
        Regex.IsMatch(line, @"^\s*(?:(?:val|type|and|module|namespace|open)\b|\[<)")

    /// A line that declares something M-MIR does not model (a class member). It is a member BOUNDARY,
    /// not filler: appending it to the pending field would corrupt that field's text with a
    /// declaration that is not part of it.
    let private opensUnmodelled (line: string) =
        Regex.IsMatch(line, @"^\s*(?:member|abstract|static|override|new|inherit|interface)\b")

    let private caseName (line: string) =
        let m = Regex.Match(line, @"^\s*\|\s*([A-Z][\w']*)")
        if m.Success then Some m.Groups.[1].Value else None

    /// `{ Id: ControlId option`, `  Mode: ThemeMode`, `; Text: string`. A DU case cannot match: it
    /// opens with `|`, which is not in the optional `[{;]` prefix.
    let private fieldName (line: string) =
        if opensUnmodelled line then
            None
        else
            let m = Regex.Match(line, @"^\s*[{;]?\s*(?:mutable\s+)?([A-Z][\w']*)\s*:")
            if m.Success then Some m.Groups.[1].Value else None

    /// `<'msg>` -> 1; `<'a, 'b>` -> 2; `<'msg when 'msg: equality>` -> 1. Counting DISTINCT type
    /// variables rather than commas keeps a constraint clause from inflating the arity.
    let private arityOf (generics: string) =
        Regex.Matches(generics, @"'[A-Za-z_][\w']*")
        |> Seq.map (fun m -> m.Value)
        |> Seq.distinct
        |> Seq.length

    /// Strip `//` comments and trailing space. Doc comments carry the mirror's teaching prose — the
    /// whole point of hand-merging — so they must not count as drift.
    let private significantLines (file: string) =
        File.ReadAllLines file
        |> Array.map (fun line ->
            let stripped =
                match line.IndexOf "//" with
                | -1 -> line
                | i -> line.Substring(0, i)

            stripped.TrimEnd())

    let read (files: string seq) : Surface =
        let vals = Dictionary<string, Set<string>>()
        let types = Dictionary<string * int, ResizeArray<Set<Member>>>()

        let addVal name signature =
            vals.[name] <-
                match vals.TryGetValue name with
                | true, existing -> Set.add signature existing
                | _ -> Set.singleton signature

        let addType key members =
            match types.TryGetValue key with
            | true, bucket -> bucket.Add members
            | _ ->
                let bucket = ResizeArray()
                bucket.Add members
                types.[key] <- bucket

        for file in files do
            let lines = significantLines file
            let mutable i = 0

            while i < lines.Length do
                let line = lines.[i]

                let valMatch =
                    Regex.Match(line, @"^(\s*)val\s+(?:mutable\s+)?([A-Za-z_][\w']*)\s*:\s*(.*)$")

                let typeMatch =
                    Regex.Match(line, @"^(\s*)(?:type|and)\s+(?:\[<[^\]]*>\]\s*)?([A-Za-z_][\w']*)\s*(<[^=]*>)?\s*=(.*)$")

                if valMatch.Success then
                    // A signature wraps across lines as often as not; join it so that two files which
                    // merely WRAP a type differently do not read as drift.
                    let indent = valMatch.Groups.[1].Value.Length
                    let signature = StringBuilder(valMatch.Groups.[3].Value)
                    let mutable j = i + 1

                    let wraps (index: int) =
                        index < lines.Length
                        && lines.[index].Trim() <> ""
                        && indentOf lines.[index] > indent
                        && not (opensDeclaration lines.[index])

                    while wraps j do
                        signature.Append(' ').Append(lines.[j].Trim()) |> ignore
                        j <- j + 1

                    addVal valMatch.Groups.[2].Value (normalize (signature.ToString()))
                    i <- j
                elif typeMatch.Success then
                    let indent = typeMatch.Groups.[1].Value.Length
                    let name = typeMatch.Groups.[2].Value
                    let arity = arityOf typeMatch.Groups.[3].Value

                    // The body is every line indented past the header — plus whatever trails the `=`,
                    // for a one-line `type Foo = { A: int }`.
                    let body = ResizeArray<string>()
                    let trailing = typeMatch.Groups.[4].Value

                    if trailing.Trim() <> "" then
                        body.Add("    " + trailing.Trim())

                    let mutable j = i + 1

                    while j < lines.Length && (lines.[j].Trim() = "" || indentOf lines.[j] > indent) do
                        if lines.[j].Trim() <> "" then
                            body.Add lines.[j]

                        j <- j + 1

                    // A record may put several fields on ONE line — `{ Operation: PathOperation; Message:
                    // string }` — and members are detected per line, so split on `;` or the second field
                    // is swallowed into the first one's text. Then the SAME record written across lines
                    // (the prevailing style here) would not compare equal to it, and two files that agree
                    // would report as drift. `;` cannot occur inside an .fsi field TYPE, so this is safe.
                    let fragments =
                        body
                        |> Seq.collect (fun bodyLine ->
                            if bodyLine.Contains ";" && not (bodyLine.TrimStart().StartsWith "|") then
                                bodyLine.Split ';' |> Seq.filter (fun fragment -> fragment.Trim() <> "")
                            else
                                Seq.singleton bodyLine)

                    // Fold the fragments into members, joining each member's continuation lines onto it.
                    let members = ResizeArray<Member>()
                    let pending = StringBuilder()
                    let mutable pendingName = None

                    let flush () =
                        match pendingName with
                        | Some memberName ->
                            let text =
                                (normalize (pending.ToString())).TrimStart('{').TrimEnd('}', ';') |> normalize

                            members.Add { Name = memberName; Text = text }
                        | None -> ()

                        pending.Clear() |> ignore
                        pendingName <- None

                    for bodyLine in fragments do
                        match caseName bodyLine, fieldName bodyLine with
                        | Some case, _ ->
                            flush ()
                            pendingName <- Some case
                            pending.Append bodyLine |> ignore
                        | None, Some field ->
                            flush ()
                            pendingName <- Some field
                            pending.Append bodyLine |> ignore
                        // A `member`/`abstract` line ends the field before it and starts nothing M-MIR
                        // models. Appending it would corrupt that field's text with a declaration that
                        // is not part of it.
                        | None, None when opensUnmodelled bodyLine -> flush ()
                        | None, None when pendingName.IsSome -> pending.Append(' ').Append(bodyLine.Trim()) |> ignore
                        | None, None -> ()

                    flush ()
                    addType (name, arity) (Set.ofSeq members)
                    i <- j
                else
                    i <- i + 1

        { Vals = vals |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq
          Types =
            types
            |> Seq.map (fun kv -> kv.Key, List.ofSeq kv.Value)
            |> Map.ofSeq }

/// The mirrors M-MIR checks: every bundled directory with a src/ original, derived from the tree
/// rather than listed. #259's lesson was not "Game.Core was missing from a table" but "the table was
/// taken on trust" — so the next merged mirror is covered on the day it is added, by nobody's effort.
let private mirroredSubjects () =
    bundledSurfaceDirectories () |> List.filter hasInRepoSource

let private fsiFilesUnder (root: string) =
    Directory.GetFiles(root, "*.fsi", SearchOption.AllDirectories) |> Array.toList

// #594 — the contradiction M-MIR/TYPE could not survive, and the ledger that resolves it.
//
// M-MIR/TYPE is an EQUALITY rule (above: a closed type curated is a type lied about). #550's rule is that
// a shipped doc must not name a symbol absent from the package a product PINS. Between a feature landing on
// `main` and the release that publishes it, `src/` is AHEAD of the pin — so for any mirrored type that
// GROWS, those two rules cannot both hold: the gate compelled the mirror to declare a member the pinned
// package does not have, manufacturing the very bug #550 exists to fix. #535's `ViewerEffect.Persist` is
// the instance that proved it.
//
// The asymmetry is the whole reason this needed a mechanism rather than a relaxation: M-MIR/VAL is a SUBSET
// rule, so a val can wait for its release by simply being absent (that is how #550 was fixed at all). A
// UNION CASE cannot — omitting it was drift. There was no way to say "this member arrives next release".
//
// Now there is, and it is a DECLARATION rather than a silence: `mirror-pending-release-ledger.txt`. An entry
// claims src has the member, the pin does not, and the mirror therefore omits it. M-MIR/TYPE honours exactly
// that omission and nothing else — a member the mirror TEACHES that src lacks is still drift, and a member
// missing with no entry is still drift. The three rules below keep the ledger from becoming a licence.
//
// P-PEND/PIN is the load-bearing one, and it is the M-PROV idiom (a stamp that must equal the pin). A pin
// bump IS a release, and a release is the only thing that can retire an entry — so a bump reds every line and
// forces each to be judged afresh. Without it the file rots silently in the one direction the offline gate
// cannot see: the release lands, the member becomes bindable, and the mirror under-teaches a closed type
// forever with M-MIR/TYPE green.
//
// What is NOT asserted here: that the pinned package really lacks the member. This repo has no snapshot of
// the published surface (the mirrors are frozen against `src/`, which is the side that is ahead), so that
// claim is held by construction — the stamp records which pin was judged. The probe that could prove it is
// `tests/Build.Tests/TemplateConsumesPinnedApiTests.fs` (#589), whose extractor reads `Module.member` and so
// cannot see a DU case at all. #611 tracks closing that.

/// A DECLARED omission: a member `src/` has, the PINNED package does not, and the mirror therefore omits.
type private PendingMember =
    { Directory: string
      TypeName: string
      /// GENERIC ARITY — part of the type's identity, not decoration. `src/SkiaViewer` really does declare
      /// two types named `ViewerEffect`: the mirrored one, and an unrelated `ViewerEffect<'msg>` in
      /// Host/Diagnostics.fsi. Keyed on the bare name they MERGE (the trap this file's header opens with),
      /// and an entry written for one would silently excuse an omission from the other. So an entry names
      /// the arity it means — `Type` is arity 0, `Type<1>` is one type parameter.
      Arity: int
      MemberName: string
      /// The `$(FsGgUiVersion)` the omission was judged against. P-PEND/PIN holds it to the current pin, so
      /// a release cannot quietly leave the entry behind.
      Pin: string
      /// The issue that retires the entry — the release that publishes the member.
      Creditor: string
      Line: int }

/// Every M-MIR subject is a mirror with an in-repo `src/` original, i.e. an `FS.GG.UI.*` package, which a
/// scaffolded product binds at this pin. (The cross-repo mirrors are M-PROV's, not M-MIR's.)
let private uiPinProperty = "FsGgUiVersion"

let private pendingLedgerPath = repositoryPath "tests/Package.Tests/mirror-pending-release-ledger.txt"

/// `SkiaViewer::ViewerEffect.Persist @ 0.9.0 #587`, or `Foo::Bar<1>.Baz @ … #…` for a generic type.
let private pendingEntryPattern =
    Regex(@"^(?<dir>[\w.]+)::(?<type>[\w']+)(?:<(?<arity>\d+)>)?\.(?<member>[\w']+)\s+@\s+(?<pin>\S+)\s+#(?<issue>\d+)$")

/// Read once — the rules below all need it, and the parse is pure. A line that does not parse is a HARD
/// FAIL, never a silent skip: a ledger that quietly ignores a typo'd entry excuses an omission nobody
/// declared, which is the fails-open shape (FS-GG/.github#266) this suite refuses everywhere else.
let private pendingLedger =
    lazy
        (File.ReadAllLines pendingLedgerPath
         |> Array.mapi (fun index line -> index + 1, line.Trim())
         |> Array.filter (fun (_, line) -> line <> "" && not (line.StartsWith "#"))
         |> Array.map (fun (number, line) ->
             let m = pendingEntryPattern.Match line

             if not m.Success then
                 failwithf
                     "mirror-pending-release-ledger.txt:%d — cannot parse '%s'. Expected `<mirror dir>::<Type>.<Member> @ <pin> #<issue>`."
                     number
                     line

             { Directory = m.Groups.["dir"].Value
               TypeName = m.Groups.["type"].Value
               Arity = (if m.Groups.["arity"].Success then int m.Groups.["arity"].Value else 0)
               MemberName = m.Groups.["member"].Value
               Pin = m.Groups.["pin"].Value
               Creditor = m.Groups.["issue"].Value
               Line = number })
         |> Array.toList)

/// The members the ledger excuses this mirror from declaring on this type. Keyed by (name, ARITY) — the
/// same identity `Fsi.Surface.Types` uses, because `ViewerEffect` and `ViewerEffect<'msg>` are different
/// types and an entry for one must not excuse the other. Within that, the member is matched by NAME: a
/// pending member the mirror teaches under a CHANGED signature still shows up as a phantom below, so
/// excusing by name cannot let a mistaught member ride along.
let private pendingMembersOf (directory: string) (typeName: string) (arity: int) =
    pendingLedger.Value
    |> List.filter (fun p -> p.Directory = directory && p.TypeName = typeName && p.Arity = arity)
    |> List.map (fun p -> p.MemberName)
    |> Set.ofList

/// Does this src candidate satisfy the mirror's declaration? The mirror may omit exactly the members the
/// ledger declares pending, and nothing else. It may never teach a member src does not have.
let private candidateSatisfies (pending: Set<string>) (mirrored: Set<Fsi.Member>) (candidate: Set<Fsi.Member>) =
    let omitted = Set.difference candidate mirrored
    let taughtButAbsent = Set.difference mirrored candidate

    Set.isEmpty taughtButAbsent
    && omitted |> Set.forall (fun m -> pending.Contains m.Name)

/// Drift M-MIR has found that this repo has not yet fixed, keyed `<mirror dir>.<type or val>`. Both
/// rules honour it, so drift in a mirror another worker holds can be RECORDED rather than forcing
/// someone to disable a rule to get CI green — the fails-open pressure that produced the original
/// hole. It is EMPTY, and the aim is to keep it that way: an entry is a receipt for a bug, not a
/// licence. The guard below fails if an entry stops drifting, so an exemption cannot outlive the bug
/// it was written for and quietly re-open the hole this gate exists to close.
///
/// It was briefly non-empty: the Controls mirror taught a `ControlEvent.Payload` field src had
/// RETIRED and omitted Feature 175's whole scroll layer, but `docs/api-surface/Controls` was inside
/// #459's live touch-set and could not be edited from here. #459 merged, so the drift is fixed
/// outright instead (#499) and the exemption is gone with it.
let private knownDrift: Set<string> = Set.empty

/// The parse is pure and every rule below needs the same two surfaces per subject, so read each tree
/// once rather than once per rule. CONCURRENT, because Expecto runs the three M-MIR tests in
/// parallel and they share this cache — a plain Dictionary corrupts under that and the tests error
/// out (they pass in isolation, which is the tell).
let private surfaceCache = ConcurrentDictionary<string, Fsi.Surface>()

let private surfaceOf (root: string) =
    surfaceCache.GetOrAdd(root, fun path -> Fsi.read (fsiFilesUnder path))

let private mirrorSurface (directory: string) =
    surfaceOf (Path.Combine(apiSurfaceRoot, directory))

let private sourceSurface (directory: string) =
    surfaceOf (repositoryPath $"src/{directory}")

/// Every type DECLARATION the mirror makes, paired with the src candidates of that name and arity.
/// Per declaration, not unioned across them: a merged mirror could declare one name twice, and
/// union-merging the two would let a stale declaration hide inside a fresh one's member set.
let private typeComparison (directory: string) =
    let source = sourceSurface directory

    (mirrorSurface directory).Types
    |> Map.toList
    |> List.collect (fun ((name, arity), declarations) ->
        let candidates = source.Types |> Map.tryFind (name, arity) |> Option.defaultValue []
        declarations |> List.map (fun mirrored -> directory, name, arity, mirrored, candidates))

[<Tests>]
let apiSurfaceMirrorCoherenceTests =
    testList
        "#366 — api-surface mirror coherence (PR-time twin of the release-only ApiSurfaceMirrorTests gate)"
        [
          // M-REF — the fails-open gap this issue is really about. Today a package can be referenced
          // with no bundled surface and nothing complains.
          test "every FS.GG.* package Product.fsproj references has a bundled api-surface directory" {
              let missing =
                  referencedFsGgPackages ()
                  |> List.choose (fun pkg ->
                      let name = surfaceDirectoryFor pkg
                      let dir = Path.Combine(apiSurfaceRoot, name)

                      if Directory.Exists dir && Directory.GetFiles(dir, "*.fsi").Length > 0 then
                          None
                      else
                          Some(pkg, name))

              Expect.isEmpty missing "each referenced FS.GG.* package bundles at least one api-surface .fsi"
          }

          test "the four packages #247 reported are bundled" {
              [ "FS.GG.Audio.Host", "Audio.Host"
                "FS.GG.Audio.Engine", "Audio.Engine"
                "FS.GG.Audio.Elmish", "Audio.Elmish"
                "FS.GG.UI.Controls.Elmish", "Controls.Elmish" ]
              |> List.iter (fun (pkg, dir) ->
                  Expect.isTrue
                      (Directory.Exists(Path.Combine(apiSurfaceRoot, dir)))
                      $"{pkg} bundles docs/api-surface/{dir}")
          }

          // M-PTR — §5.3: the .fsi and the SKILL.md were "two artifacts with no link in either direction".
          test "every bundled .fsi carries exactly one See skill pointer naming a shipped product skill" {
              let skills = shippedProductSkills ()

              let offenders =
                  bundledFsiFiles ()
                  |> List.choose (fun file ->
                      let rel = relativeToSurfaceRoot file

                      match skillPointers file with
                      | _ when pointerExempt.Contains rel -> None
                      | [ skill ] when skills.Contains skill -> None
                      | [ skill ] -> Some(rel, $"names unknown skill '{skill}'")
                      | [] -> Some(rel, "no '// See skill:' pointer above its namespace")
                      | many -> Some(rel, $"carries {many.Length} pointers"))

              Expect.isEmpty offenders "each bundled .fsi points at exactly one shipped product skill"
          }

          test "the pointer exemptions still exist and still lack a pointer" {
              // If someone writes a skill for Canvas's immediate-mode surface, delete the exemption.
              pointerExempt
              |> Set.iter (fun rel ->
                  let file = Path.Combine(apiSurfaceRoot, rel.Replace('/', Path.DirectorySeparatorChar))
                  Expect.isTrue (File.Exists file) $"exempt {rel} still exists"
                  Expect.isEmpty (skillPointers file) $"exempt {rel} has no owning skill to point at")
          }

          // M-PROV — ADR-0024's objection, enforced. A cross-repo doc copy cannot outlive its package
          // without failing here.
          test "every cross-repo mirror records the source-repo version the template pins" {
              let pins = pinnedVersions ()

              let offenders =
                  bundledFsiFiles ()
                  |> List.choose (fun file ->
                      crossRepoMirrors
                      |> Map.tryFind (owningPackageDirectory file)
                      |> Option.bind (fun (repo, property) ->
                          let pinned = pins.[property]

                          match provenanceVersion repo file with
                          | None -> Some(relativeToSurfaceRoot file, $"<no 'Mirrored from FS-GG/{repo}' line>")
                          | Some stamped when stamped <> pinned ->
                              Some(relativeToSurfaceRoot file, $"stamped {stamped}, but {property} pins {pinned}")
                          | Some _ -> None))

              Expect.isEmpty offenders "each cross-repo mirror is stamped with the version its template property pins"
          }

          test "every cross-repo mirror directory declared here actually exists and is non-empty" {
              // Keeps `crossRepoMirrors` from silently going stale into a no-op lookup, which would make
              // M-PROV vacuously green — the exact way #259's Game.Core mirror went unchecked.
              crossRepoMirrors
              |> Map.iter (fun dir _ ->
                  let path = Path.Combine(apiSurfaceRoot, dir)
                  Expect.isTrue (Directory.Exists path) $"declared cross-repo mirror {dir} exists"

                  Expect.isGreaterThan
                      (Directory.GetFiles(path, "*.fsi").Length)
                      0
                      $"declared cross-repo mirror {dir} bundles at least one .fsi to stamp")
          }

          // #259, generalized. The bug was not "Game.Core was missing from the table" but "the table was
          // taken on trust". A bundled directory with no `src/` original can only have come from another
          // repo, so membership is derivable — check it instead of asserting Game.Core by name, and the
          // NEXT unstamped cross-repo mirror fails here on the day it is added.
          test "every bundled mirror with no src/ original is registered as cross-repo" {
              let unregistered =
                  bundledSurfaceDirectories ()
                  |> List.filter (fun dir -> not (hasInRepoSource dir) && not (crossRepoMirrors.ContainsKey dir))

              Expect.isEmpty
                  unregistered
                  "a bundled surface with no src/ original is a cross-repo copy and must be registered in crossRepoMirrors so M-PROV stamps it"
          }

          test "no registered cross-repo mirror actually has a src/ original" {
              // The converse. A mirror wrongly registered as cross-repo would be stamped by hand forever
              // instead of getting the stronger `inRepoExactCopies` byte comparison.
              let misregistered =
                  crossRepoMirrors |> Map.toList |> List.map fst |> List.filter hasInRepoSource

              Expect.isEmpty misregistered "each registered cross-repo mirror has no in-repo src/ original to compare against"
          }

          // The surface #259 was actually missing. Anchored on the DECLARATION, not a bare substring:
          // `slide` occurs inside `collide`, `Contact` inside `aabbContact`, so a substring probe could
          // stay green on a surface that had dropped the member.
          test "the bundled Game.Core surface declares the 0.2.0 collision layer" {
              let declares file (declaration: string) =
                  let text = File.ReadAllText(Path.Combine(apiSurfaceRoot, "Game.Core", file))
                  Expect.stringContains text declaration $"bundled Game.Core/{file} declares '{declaration}'"

              [ "Resolution.fsi", [ "val pushOut:"; "val slide:"; "val knockback:" ]
                "Primitives.fsi", [ "type Contact ="; "type Circle ="; "type RayHit ="; "type ConvexPolygon =" ]
                "Geometry.fsi",
                [ "val aabbContact:"
                  "val circleContact:"
                  "val circleAabbContact:"
                  "val segmentAabbHit:"
                  "val segmentCircleHit:"
                  "val obbPolygon:"
                  "val polygonContact:" ] ]
              |> List.iter (fun (file, declarations) -> declarations |> List.iter (declares file))
          }

          // The in-repo half of the same staleness problem. A cross-repo copy gets a stamp because we
          // cannot see the other repo from here; an in-repo copy has its original one directory away,
          // so compare them outright — modulo the internals the mirror deliberately does not ship
          // (S-INT, #585: see `stripInternalDeclarations`).
          test "every in-repo exact-copy mirror still matches its src original" {
              let offenders =
                  inRepoExactCopies
                  |> List.choose (fun (rel, srcRelative) ->
                      let mirror = Path.Combine(apiSurfaceRoot, rel.Replace('/', Path.DirectorySeparatorChar))
                      let original = repositoryPath srcRelative

                      if bodyOf mirror = stripInternalDeclarations (bodyOf original) then
                          None
                      else
                          Some(rel, srcRelative))

              Expect.isEmpty
                  offenders
                  "each in-repo exact-copy mirror is its src original minus the internal declarations (modulo the // header)"
          }

          // §5.3 — the report's author hand-rolled `len * size * 0.6` centring while `measureText` sat
          // in the very file they were reading, and slipped on the positional `Rectangle`/`Text` cases
          // whose safe siblings carry an explicit warning.
          test "the bundled Scene surface warns about the positional arity slip on its DU cases" {
              let scenePath = Path.Combine(apiSurfaceRoot, "Scene", "Scene.fsi")
              let lines = File.ReadAllLines scenePath

              // Only the doc-comment block DIRECTLY above the case counts — a 'nearby' warning on a
              // neighbouring case must not satisfy this.
              let docBlockAbove index =
                  lines.[.. index - 1]
                  |> Array.rev
                  |> Array.takeWhile (fun line -> line.TrimStart().StartsWith "///")
                  |> String.concat " "

              [ "Rectangle"; "Text"; "SizedText" ]
              |> List.iter (fun case ->
                  match lines |> Array.tryFindIndex (fun l -> l.TrimStart().StartsWith $"| {case} of ") with
                  | None -> failtestf "the bundled Scene surface no longer declares the %s case" case
                  | Some index ->
                      Expect.stringContains
                          (docBlockAbove index)
                          "arity slip"
                          $"the {case} case carries an arity-slip warning directly above it")

              Expect.stringContains
                  (File.ReadAllText scenePath)
                  "Scene.measureText"
                  "the Scene surface points at measureText for text layout"
          }
          // M-MIR/VAL — the drift #437 was filed for. `classifyWindowObservation` sat in the bundled
          // SkiaViewer surface with a signature src had replaced; `Scene.combine` promised a bare
          // `PathSpec` where src returns `Result<PathSpec, PathCombineError>`. Both compile-break an
          // author who trusts the contract the scaffold calls authoritative.
          test "no bundled mirror teaches a val its src original does not declare with that exact signature" {
              let offenders =
                  mirroredSubjects ()
                  |> List.collect (fun directory ->
                      let source = sourceSurface directory

                      (mirrorSurface directory).Vals
                      |> Map.toList
                      |> List.filter (fun (name, _) -> not (knownDrift.Contains $"{directory}.{name}"))
                      |> List.collect (fun (name, mirrored) ->
                          match source.Vals |> Map.tryFind name with
                          | None -> [ $"{directory}: val {name} — teaches a val no src/{directory} .fsi declares" ]
                          | Some declared ->
                              // EVERY signature the mirror teaches under this name must be one src declares
                              // — not merely one of them. A merged mirror carries a name from several
                              // modules, and an intersection test would let a stale overload ride along
                              // beside a fresh one, which is precisely the lie the rule exists to catch.
                              Set.difference mirrored declared
                              |> Set.toList
                              |> List.map (fun taught ->
                                  let actual = declared |> Set.toList |> List.head

                                  $"{directory}: val {name} — mirror teaches `{taught}`, src declares `{actual}`")))

              let report = offenders |> List.map (fun o -> "\n  " + o) |> String.concat ""

              Expect.isEmpty
                  offenders
                  $"each val a mirror teaches is declared in its src original with an identical signature (M-MIR/VAL).{report}"
          }

          // M-MIR/TYPE — a record or a DU is a CLOSED declaration, so a mirror that teaches one at all
          // must teach the whole of it: a `RolePalette` without `Mode` cannot be constructed, and a
          // `SceneNode` without `GlyphRun` cannot be matched exhaustively. Omitting a val is curation
          // (scaffold-map sanctions it); omitting a case is a lie about the type's shape.
          test "every type a bundled mirror declares matches a src type of that name and arity, member for member" {
              let offenders =
                  mirroredSubjects ()
                  |> List.collect typeComparison
                  |> List.filter (fun (directory, name, _, _, _) -> not (knownDrift.Contains $"{directory}.{name}"))
                  |> List.choose (fun (directory, name, arity, mirrored, candidates) ->
                      let describe (members: Set<Fsi.Member>) =
                          members |> Set.map (fun m -> m.Name) |> Set.toList |> String.concat ", "

                      // #594 — the members this mirror is DECLARED to omit until the next release. Keyed on
                      // (name, arity), the same identity the comparison itself uses.
                      let pending = pendingMembersOf directory name arity

                      match candidates with
                      | [] -> Some $"{directory}: type {name} (arity {arity}) — no src/{directory} type of that name and arity"
                      | _ when candidates |> List.exists (candidateSatisfies pending mirrored) -> None
                      | _ ->
                          // Report against the closest candidate — with several, the nearest is the one
                          // the mirror was plainly copied from, and diffing against the others is noise.
                          let closest =
                              candidates
                              |> List.minBy (fun candidate ->
                                  Set.count (Set.difference candidate mirrored)
                                  + Set.count (Set.difference mirrored candidate))

                          // A pending member is not missing — it is DECLARED absent (#594). Subtract it here
                          // rather than at the match above, so the report names only the drift someone must
                          // actually fix.
                          let missing =
                              Set.difference closest mirrored
                              |> Set.filter (fun m -> not (pending.Contains m.Name))

                          let phantom = Set.difference mirrored closest

                          // A member present on both sides under one name but with different text has not
                          // been added or removed — its DECLARATION changed, which is the drift that most
                          // needs saying plainly (it is how `classifyWindowObservation` went wrong).
                          let changed =
                              Set.intersect (missing |> Set.map (fun m -> m.Name)) (phantom |> Set.map (fun m -> m.Name))

                          let unchangedBy (members: Set<Fsi.Member>) =
                              members |> Set.filter (fun m -> not (changed.Contains m.Name))

                          let complaints =
                              [ if not (Set.isEmpty changed) then
                                    let names = changed |> Set.toList |> String.concat ", "
                                    $"members whose declaration CHANGED: {names}"

                                let trulyMissing = unchangedBy missing

                                if not (Set.isEmpty trulyMissing) then
                                    $"mirror OMITS {describe trulyMissing}"

                                let trulyPhantom = unchangedBy phantom

                                if not (Set.isEmpty trulyPhantom) then
                                    $"mirror teaches members src does not have: {describe trulyPhantom}" ]

                          let detail = complaints |> String.concat "; "
                          Some $"{directory}: type {name} — {detail}")

              let report = offenders |> List.map (fun o -> "\n  " + o) |> String.concat ""

              // The pointer matters as much as the failure. An author who GREW a mirrored type is looking at
              // "mirror OMITS X" with no way past it that does not break #550 — which is exactly how the
              // unbindable `Persist` case got shipped. Tell them the third option exists (#594).
              Expect.isEmpty
                  offenders
                  $"each type a mirror declares carries its src original's exact member set (M-MIR/TYPE).\n\
                    If the member is NEWER than the package a product pins — it is on `main`, not on the \
                    release — do NOT add it to the mirror: that ships a doc teaching a member nobody can \
                    bind (#550). Declare the omission in tests/Package.Tests/mirror-pending-release-ledger.txt \
                    instead (#594).{report}"
          }

          // P-PEND/SRC — an entry claims src HAS the member. If it does not, the feature was reverted or
          // renamed and the entry is a PHANTOM: it now excuses an omission that is plain drift, which is the
          // one way this ledger could re-open the hole it closes.
          test "every pending-release entry names a member its src original actually declares" {
              let subjects = mirroredSubjects () |> Set.ofList

              let offenders =
                  pendingLedger.Value
                  |> List.choose (fun pending ->
                      let where = $"mirror-pending-release-ledger.txt:{pending.Line}"

                      if not (subjects.Contains pending.Directory) then
                          // Not an M-MIR subject, so M-MIR/TYPE never reads the entry — it excuses nothing
                          // and silently rots. (A cross-repo mirror is M-PROV's; it has no src/ to be ahead.)
                          Some
                              $"{where}: '{pending.Directory}' is not a bundled mirror with a src/ original, so M-MIR/TYPE never consults this entry"
                      else
                          let declarations =
                              (sourceSurface pending.Directory).Types
                              |> Map.tryFind (pending.TypeName, pending.Arity)
                              |> Option.defaultValue []

                          match declarations with
                          | [] ->
                              Some
                                  $"{where}: src/{pending.Directory} declares no type {pending.TypeName} of arity {pending.Arity} — PHANTOM entry, delete it (if the type is generic, name its arity: {pending.TypeName}<n>.{pending.MemberName})"
                          | _ when
                              declarations
                              |> List.exists (Set.exists (fun m -> m.Name = pending.MemberName))
                              ->
                              None
                          | _ ->
                              Some
                                  $"{where}: src/{pending.Directory}'s {pending.TypeName} (arity {pending.Arity}) has no member {pending.MemberName} — PHANTOM entry (reverted or renamed?), delete it")

              let report = offenders |> List.map (fun o -> "\n  " + o) |> String.concat ""

              Expect.isEmpty
                  offenders
                  $"every pending-release entry names a member src actually declares — a phantom entry excuses real drift (P-PEND/SRC).{report}"
          }

          // P-PEND/OMIT — the converse, and the rule that makes an entry MEAN something. An entry says the
          // mirror omits the member. If the mirror declares it anyway, the entry excuses nothing and the
          // shipped doc is back to teaching a member a product on the pin cannot bind — #550's bug, with a
          // ledger entry next to it saying we knew.
          test "no bundled mirror declares a member the ledger says is pending release" {
              // Enumerated once, not once per entry — and a directory that is not an M-MIR subject is left to
              // P-PEND/SRC, which is the rule that reports it.
              let subjects = mirroredSubjects () |> Set.ofList

              let offenders =
                  pendingLedger.Value
                  |> List.filter (fun pending -> subjects.Contains pending.Directory)
                  |> List.choose (fun pending ->
                      let declared =
                          (mirrorSurface pending.Directory).Types
                          |> Map.tryFind (pending.TypeName, pending.Arity)
                          |> Option.defaultValue []
                          |> List.exists (Set.exists (fun m -> m.Name = pending.MemberName))

                      if declared then
                          Some
                              $"mirror-pending-release-ledger.txt:{pending.Line}: the {pending.Directory} mirror DECLARES {pending.TypeName}.{pending.MemberName}, which this entry says it omits until #{pending.Creditor} — either the mirror is teaching a member the pin cannot bind (#550), or the entry is stale and should be deleted"
                      else
                          None)

              let report = offenders |> List.map (fun o -> "\n  " + o) |> String.concat ""

              Expect.isEmpty
                  offenders
                  $"a ledgered member is one the mirror OMITS — an entry beside a declaration excuses nothing (P-PEND/OMIT).{report}"
          }

          // P-PEND/PIN — the ratchet. A pin bump IS a release, and a release is the only thing that can
          // retire an entry, so a bump must red EVERY line and force it to be judged afresh: delete the ones
          // the release published (and let M-MIR/TYPE demand the case back), re-stamp any that genuinely
          // still are not there. Without this the file rots silently in the direction the offline gate cannot
          // see — the member becomes bindable, and the mirror under-teaches a closed type forever, green.
          //
          // This is M-PROV's idiom, and it is here for M-PROV's reason: a stamp that must equal the pin is
          // the only staleness check available to a gate that cannot see the published package.
          test "every pending-release entry is stamped with the version the template pins" {
              let pinned = pinnedVersion uiPinProperty

              let offenders =
                  pendingLedger.Value
                  |> List.filter (fun pending -> pending.Pin <> pinned)
                  |> List.map (fun pending ->
                      $"mirror-pending-release-ledger.txt:{pending.Line}: {pending.TypeName}.{pending.MemberName} was judged against {uiPinProperty} {pending.Pin}, but the template now pins {pinned} — re-judge it: if {pinned} exports the member, DELETE the line and restore the member to the mirror; if it still does not, re-stamp to {pinned}")

              let report = offenders |> List.map (fun o -> "\n  " + o) |> String.concat ""

              Expect.isEmpty
                  offenders
                  $"every pending-release omission was judged against the CURRENT pin — a bump retires or re-stamps each one (P-PEND/PIN).{report}"
          }

          test "every knownDrift exemption still drifts" {
              // The exemption is a receipt for a bug someone else owns, not a permanent hole. When
              // an exempted mirror is fixed, this fails until the entry is deleted — so the gate cannot
              // be left half-blind by inattention, which is the failure #437 is about in the first place.
              let stillDrifting =
                  mirroredSubjects ()
                  |> List.collect typeComparison
                  |> List.filter (fun (directory, name, arity, mirrored, candidates) ->
                      // The SAME satisfaction test M-MIR/TYPE uses, or the two disagree about what "drifts"
                      // means: a type whose only gap is a DECLARED pending omission (#594) is not drifting,
                      // and counting it here would keep a knownDrift entry alive that no longer has a bug.
                      let pending = pendingMembersOf directory name arity
                      not (candidates |> List.exists (candidateSatisfies pending mirrored)))
                  |> List.map (fun (directory, name, _, _, _) -> $"{directory}.{name}")
                  |> Set.ofList

              let stale = Set.difference knownDrift stillDrifting

              let report = stale |> Set.toList |> List.map (fun o -> "\n  " + o) |> String.concat ""

              Expect.isEmpty
                  stale
                  $"every knownDrift entry names a mirror type that STILL drifts — a fixed one must be deleted from the set.{report}"
          }
        ]
