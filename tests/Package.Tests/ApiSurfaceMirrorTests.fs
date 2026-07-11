module ApiSurfaceMirrorTests

// #247 (Breakout1 FEEDBACK.md §3.2(c), §5.3) — the bundled `docs/api-surface/**` tree is the contract
// surface a generated product actually reads. Four packages `Product.fsproj` references shipped no
// surface at all: FS.GG.Audio.Host / .Engine / .Elmish and FS.GG.UI.Controls.Elmish.
//
// There was no "mirror" that skipped them. The tree is a hand-maintained `copyOnly` snapshot
// (`.template.config/template.json`, Feature 060; rationale in
// `specs/201-refresh-template-scene-api/research.md`, "Decision 5"). Audio's surface was left out
// DELIBERATELY under ADR-0024 — verified in the FS.GG.Audio repo, and a bundled doc copy risks
// outliving the package it claims.
//
// This gate supersedes that omission and answers its objection rather than ignoring it:
//   M-REF   every FS.GG.* package `Product.fsproj` REFERENCES has a mirrored api-surface directory.
//           The converse is NOT asserted here: the bundled tree is the framework's WHOLE contract
//           surface, so a mirror legitimately outlives any one profile's reference set (an `app`
//           scaffold ships the Audio.Core/Game.Core mirrors while pinning neither package).
//           That latitude is exactly what hid #430 — Symbology was bundled, and named in this comment
//           as the example of a mirror with no reference, while the fs-gg-symbology SKILL told authors
//           to `open` it on every profile. A mirror may outlive a reference; a SKILL may not. The
//           skill => package direction is asserted by SkillPackageReachTests (R-PINNED/R-REF/R-REACH),
//           and Symbology is now referenced by Product.fsproj on the app/sample-pack/game gate.
//   M-PTR   every bundled .fsi names a SHIPPED product skill, so the .fsi and the SKILL.md stop being
//           an unlinked pair (§5.3 — the report's author read the .fsi and never found
//           `Scene.measureText`, which the fs-gg-scene skill documents under its own heading).
//           It checks the skill EXISTS, not that it teaches this file's members. Contrast
//           Feature240GameCoreSkillTests, which resolves every `Module.member` the skill names against
//           the .fsi. Tightening M-PTR that way is a follow-up: today `Controls.Elmish` would fail it,
//           because no shipped skill mentions `runInteractiveApp` / `InteractiveAppHost` at all.
//   M-PROV  every CROSS-REPO mirror records the version it was copied from, and that version must
//           equal the version property the template pins for that mirror's source repo. This is
//           ADR-0024's staleness concern in enforced form: the copy can no longer outlive its package
//           in silence. The in-repo copies get the stronger check — compared against their src/
//           original outright.
//
// #259 — M-PROV originally hard-coded FS.GG.Audio, so the Game.Core mirror was unstamped and
// therefore unchecked: it sat at the pre-0.2.0 surface (no `Resolution`, no collision layer on
// `Geometry`/`Primitives`) while `$(FsGgGameVersion)` already said 0.2.0, and a scaffolded product
// read that stale surface as its contract. The mirror table below is now keyed by source repo, so
// every cross-repo mirror is stamped and compared against its own pin.
//
// What M-PROV can and cannot see: it proves the stamp equals the pin, not that the bytes equal the
// package — CI cannot see the other repo. That is enough for the drift it exists to catch, because
// bumping a pin without recopying leaves the stamp behind and fails here.
//
// #437 — and the hole all of the above still left open. Every rule so far checks a mirror's
// PROVENANCE (a stamp, a copy) and none checks its CONTENT, so a mirror that is neither a
// cross-repo copy nor a listed exact copy was checked by NOTHING. `SkiaViewer/` was exactly that:
// it has a src/ original (so M-PROV skips it) but is a hand-MERGED surface, not a file copy (so it
// could not join `inRepoExactCopies`), and it had drifted — `classifyWindowObservation` taught a
// signature that no longer existed, and an author following the bundled contract wrote code that
// did not compile. `Scene/` was in the same position for the same reason, and had drifted too.
//
//   M-MIR   the content check the other rules imply but never make. It compares a mirror's
//           DECLARATIONS against its src/ original's, for EVERY bundled mirror that has one —
//           merged or not — so a surface can no longer be un-checked merely by being un-copyable.
//           Two rules, both COHERENCE (mirror => src), never completeness:
//
//             M-MIR/VAL   every `val` the mirror teaches exists in src with an IDENTICAL signature.
//             M-MIR/TYPE  every type the mirror declares matches a src type of the same name AND
//                         generic arity, member for member — same cases, same fields, same types.
//
//           Coherence, not completeness, because `docs/scaffold-map.md` sanctions curation in as
//           many words: the typed front door is deliberately absent from this tree, and "it's not in
//           docs/api-surface" does not mean it's unavailable. Measured, the mirrors omit 113 Controls
//           vals and 74 SkiaViewer vals — omission is the DESIGN. So M-MIR lets a mirror omit a val
//           or a whole type, and never lets it LIE about one it does teach.
//
//           A type is the exception that proves it: records and DUs are CLOSED. A record the mirror
//           teaches without `Mode` cannot be constructed; a DU taught without `GlyphRun` cannot be
//           matched exhaustively. So for a type the mirror declares, M-MIR/TYPE demands the WHOLE
//           member set — curating a type's fields is not curation, it is a lie about its shape.
//
// Two traps, both hit while writing this, both the reason it is a declaration comparison and not a
// text diff (a text diff over these files is 800+ lines of noise — the mirrors are merged and
// re-commented, so nearly every line differs "legitimately"):
//
//   - The source of a mirror is the WHOLE src/<dir> tree, recursively. `Controls/Display.fsi` is
//     merged from `src/Controls/Widgets/Display.fsi` — a SUBDIRECTORY. Globbing `src/<dir>/*.fsi`
//     reports `Badge.view` as a phantom that does not exist. It exists.
//   - A type's identity is its name AND its generic arity. `src/SkiaViewer` declares TWO types named
//     `ViewerEffect` — the mirrored `ViewerEffect` and an unrelated `ViewerEffect<'msg>` in
//     `Host/Diagnostics.fsi`. Keyed on the bare name they merge, and the gate invents five missing
//     cases. Hence `Types: Map<name * arity, _>`, and a src name may resolve to SEVERAL candidate
//     declarations — the mirror need match ANY ONE of them (same reason `Vals` maps a name to a SET
//     of signatures: two modules may each declare `create`, and the mirror teaches one of them).

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
/// Types.fsi), not a file copy, so it has no single original to compare against — M-MIR is what
/// checks the merged ones, by declaration rather than by byte.
let private inRepoExactCopies =
    [ "Controls.Elmish/ControlsElmish.fsi", "src/Controls.Elmish/ControlsElmish.fsi"
      "Controls.Elmish/Authoring.fsi", "src/Controls.Elmish/Authoring.fsi" ]

/// M-MIR's reader: the declarations an .fsi teaches. Not an F# parser — a line-shape reader over
/// signature files, which are the most regular F# there is. It models `val`s, types, record fields
/// and DU cases. It does NOT model `member`/`abstract`: no in-repo src/ .fsi under a mirrored
/// directory declares one (the 17 in the tree are all in the Audio.* cross-repo mirrors, which M-MIR
/// does not cover), so they are treated as a member BOUNDARY and skipped rather than compared. If a
/// mirrored surface ever grows one, model it here — do not let it ride along inside a field's text.
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

/// Drift M-MIR has FOUND and this repo has not yet fixed, because the file is not ours to fix:
/// `template/base/docs/api-surface/Controls` is inside the live touch-set of #459, so #437 could not
/// edit it without two workers writing one file. Each entry is `<mirror dir>.<type or val>` — both
/// rules honour it, so an unowned mirror's drift can always be RECORDED rather than forcing someone
/// to disable a rule to get CI green. Each is a REAL defect, itemised in #499: the mirror teaches a
/// retired `ControlEvent.Payload` field and omits 10+ live DU cases. Fixing #499 means DELETING these
/// lines; the guard below fails if an entry stops drifting, so the exemption cannot outlive the bug
/// and quietly re-open the hole this gate exists to close.
let private knownDrift =
    set
        [ "Controls.AttrValue" // #499 — missing case SceneValue
          "Controls.ControlDiagnosticCode" // #499 — missing 10 overlay/scroll cases
          "Controls.ControlEvent" // #499 — teaches `Payload`, RETIRED from src
          "Controls.ControlRuntimeEffect" // #499 — missing case ScrollChanged
          "Controls.ControlRuntimeModel" // #499 — missing field ScrollOffsets
          "Controls.ControlRuntimeMsg" // #499 — missing cases ScrollControl / SetScrollExtent
          "Controls.NavPayload" ] // #499 — missing case EditedText

/// The parse is pure and every rule below needs the same two surfaces per subject, so read each tree
/// once rather than once per rule.
let private surfaceCache = Dictionary<string, Fsi.Surface>()

let private surfaceOf (root: string) =
    match surfaceCache.TryGetValue root with
    | true, cached -> cached
    | _ ->
        let parsed = Fsi.read (fsiFilesUnder root)
        surfaceCache.[root] <- parsed
        parsed

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

/// No shipped product skill covers FS.GG.UI.Canvas's immediate-mode drawing surface. Recorded
/// here rather than papered over with a pointer to a skill that does not teach these members.
/// (`Canvas/Loop.fsi` removed with the surface; retires at the next framework major, 0.6.0 —
/// ADR-0104 decision 5, #319.)
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

[<Tests>]
let apiSurfaceMirrorTests =
    testList
        "api-surface-mirror"
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
          // so compare them outright.
          test "every in-repo exact-copy mirror still matches its src original" {
              let offenders =
                  inRepoExactCopies
                  |> List.choose (fun (rel, srcRelative) ->
                      let mirror = Path.Combine(apiSurfaceRoot, rel.Replace('/', Path.DirectorySeparatorChar))
                      let original = repositoryPath srcRelative

                      if bodyOf mirror = bodyOf original then
                          None
                      else
                          Some(rel, srcRelative))

              Expect.isEmpty offenders "each in-repo exact-copy mirror is identical to its src original (modulo the // header)"
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

                      match candidates with
                      | [] -> Some $"{directory}: type {name} (arity {arity}) — no src/{directory} type of that name and arity"
                      | _ when candidates |> List.exists (fun candidate -> candidate = mirrored) -> None
                      | _ ->
                          // Report against the closest candidate — with several, the nearest is the one
                          // the mirror was plainly copied from, and diffing against the others is noise.
                          let closest =
                              candidates
                              |> List.minBy (fun candidate ->
                                  Set.count (Set.difference candidate mirrored)
                                  + Set.count (Set.difference mirrored candidate))

                          let missing = Set.difference closest mirrored
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

              Expect.isEmpty
                  offenders
                  $"each type a mirror declares carries its src original's exact member set (M-MIR/TYPE).{report}"
          }

          test "every knownDrift exemption still drifts" {
              // The exemption is a receipt for a bug someone else owns (#499), not a permanent hole. When
              // the Controls mirror is fixed, this fails until the entry is deleted — so the gate cannot
              // be left half-blind by inattention, which is the failure #437 is about in the first place.
              let stillDrifting =
                  mirroredSubjects ()
                  |> List.collect typeComparison
                  |> List.filter (fun (_, _, _, mirrored, candidates) ->
                      not (candidates |> List.exists (fun candidate -> candidate = mirrored)))
                  |> List.map (fun (directory, name, _, _, _) -> $"{directory}.{name}")
                  |> Set.ofList

              let stale = Set.difference knownDrift stillDrifting

              let report = stale |> Set.toList |> List.map (fun o -> "\n  " + o) |> String.concat ""

              Expect.isEmpty
                  stale
                  $"every knownDrift entry names a mirror type that STILL drifts — a fixed one must be deleted from the set (see #499).{report}"
          }
        ]
