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
// versions, and the shipped product-skills, all as files on disk. So this hoist is a FAITHFUL mirror:
// it reads the SAME four source-of-truth inputs and asserts the SAME invariants, one gate earlier —
//   M-REF   every FS.GG.* package Product.fsproj references has a mirrored api-surface directory.
//   M-PTR   every bundled .fsi names exactly one shipped product skill (with the recorded exemptions).
//   M-PROV  every cross-repo mirror records the source-repo version the template pins, and the mirror
//           table stays complete (a bundled dir with no src/ original must be registered cross-repo).
//   in-repo exact copies still match their src/ originals (the stronger, byte-level half of M-PROV).
//
// Kept in deliberate lockstep with tests/Package.Tests/ApiSurfaceMirrorTests.fs: the two read the same
// repo inputs and assert the same invariants, so a real drift fails BOTH — the release check is
// mirrored earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing so the mirror
// cannot silently desync. This body is a verbatim hoist of the release-only rule (module name and
// header aside); keeping it byte-faithful is what makes the L-INPUTS lockstep check exact.

open System.IO
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

/// No shipped product skill covers FS.GG.UI.Canvas's immediate-mode drawing/loop surface. Recorded
/// here rather than papered over with a pointer to a skill that does not teach these members.
let private pointerExempt = set [ "Canvas/Elements.fsi"; "Canvas/Loop.fsi" ]

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
        ]
