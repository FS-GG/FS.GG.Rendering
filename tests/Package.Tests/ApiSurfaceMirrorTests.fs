module ApiSurfaceMirrorTests

// #247 (Breakout1 FEEDBACK.md §3.2(c), §5.3) — the bundled `docs/api-surface/**` tree is the contract
// surface a generated product actually reads. Four packages `Product.fsproj` references shipped no
// surface at all: FS.GG.Audio.Host / .Engine / .Elmish and FS.GG.UI.Controls.Elmish.
//
// There was no "mirror" that skipped them. Audio's surface was left out DELIBERATELY under ADR-0024 —
// verified in the FS.GG.Audio repo, and a bundled doc copy risks outliving the package it claims.
//
// ---------------------------------------------------------------------------------------------------
// THE TREE IS A BUILD OUTPUT NOW (#752), AND THAT IS WHY MOST OF THIS FILE IS GONE (#753)
// ---------------------------------------------------------------------------------------------------
//
// This file used to open by calling the tree "a hand-maintained `copyOnly` snapshot". It is not one any
// more: `scripts/refresh-api-surface-mirror.fsx` EMITS it from the PINNED package's packed `.fsi` plus
// the curation in `scripts/api-surface-manifest.txt`, and `gate.yml` fails on any diff. So M-MIR
// (/VAL, /TYPE), the in-repo exact-copy byte gate, and the whole `mirror-pending-release-ledger.txt`
// mechanism were deleted: they compared the emitted tree against `src/`, which is one link up its own
// generator's chain (`src/` -> nupkg -> mirror), and #694's test is that a gate comparing a generated
// artifact against its generator's input is subsumed by the generator. Worse than redundant, they were
// wrong-by-construction after #752: the mirror tracks the PIN, so for the whole window where `src/` runs
// ahead of the release they RED on a correct mirror — which is the entire reason the ledger existed.
// Generating from the pin closes that window, so the ledger closed with it.
//
// WHAT SURVIVES, AND WHY — each of these compares two INDEPENDENTLY-DERIVED facts, so the generator is
// not their proof. The generator's inputs are the pin and the manifest; a rule that checks something
// neither of those decides is still load-bearing:
//   M-REF   every FS.GG.* package `Product.fsproj` REFERENCES has a mirrored api-surface directory.
//           The generator never reads `Product.fsproj`.
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
//           The generator emits the manifest's `skill` pointer with NO validation that the skill
//           exists, so this is the only thing resolving it against `template/product-skills/`.
//           It checks the skill EXISTS, not that it teaches this file's members. Contrast
//           Feature240GameCoreSkillTests, which resolves every `Module.member` the skill names against
//           the .fsi. Tightening M-PTR that way is a follow-up: today `Controls.Elmish` would fail it,
//           because no shipped skill mentions `runInteractiveApp` / `InteractiveAppHost` at all.
//   M-PROV  every CROSS-REPO mirror records the version it was copied from, and that version must
//           equal the version property the template pins for that mirror's source repo.
//
// M-PROV is the one #752 made MORE necessary, not less, and it is worth being precise about why.
// The generator resolves the pin to restore the package, but it copies each stanza's `header` through
// VERBATIM (`for h in st.Header do sb.AppendLine h`) — and the manifest hardcodes the version as
// literal text (`// Mirrored from FS-GG/FS.GG.Audio @ 0.3.0 …`). Nothing reconciles the two. So bump
// `$(FsGgAudioVersion)` to 0.4.0 and the generator restores 0.4.0, regenerates every member from the
// 0.4.0 nupkg, and still stamps `@ 0.3.0` — exiting 0, with `gate.yml --check` green on a document
// whose own provenance line names a version it did not come from. M-PROV is the only rule in the tree
// that catches that. It is a stamp-vs-pin comparison, and the generator authors neither side.
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
          "Contracts", ("FS.GG.SDD", "FsGgContractsVersion")
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

/// No shipped product skill covers FS.GG.UI.Canvas's immediate-mode drawing surface. Recorded
/// here rather than papered over with a pointer to a skill that does not teach these members.
/// (`Canvas/Loop.fsi` removed with the surface; retires at the next framework major, 0.6.0 —
/// ADR-0104 decision 5, #319.)
let private pointerExempt = set [ "Canvas/Elements.fsi" ]

/// Product skills this repo no longer SHIPS but whose bodies still reach the scaffolded product —
/// they are OWNER-SOURCED from FS.GG.Game.Skills (ADR-0063, FS.GG.Rendering#965). Their api-surface
/// `// See skill:` pointers stay valid — a reader in the product DOES find the skill (materialized from
/// the package) — even though the body is no longer in `template/product-skills/` here. The pointer is
/// verified against the owner's body by FS.GG.Game's own gate, not this one.
let private ownerSourcedSkills =
    set [ "fs-gg-game-core"; "fs-gg-audio"; "fs-gg-persistence"; "fs-gg-model-swap" ]

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
          // A pointer may name a skill this repo SHIPS, or one it OWNER-SOURCES (ADR-0063): both reach the
          // scaffolded product, so both are a live link a reader can follow. Only a pointer to a skill that
          // is neither is an unknown-skill offence.
          test "every bundled .fsi carries exactly one See skill pointer naming a shipped or owner-sourced product skill" {
              let skills = shippedProductSkills ()
              let resolves skill = skills.Contains skill || ownerSourcedSkills.Contains skill

              let offenders =
                  bundledFsiFiles ()
                  |> List.choose (fun file ->
                      let rel = relativeToSurfaceRoot file

                      match skillPointers file with
                      | _ when pointerExempt.Contains rel -> None
                      | [ skill ] when resolves skill -> None
                      | [ skill ] -> Some(rel, $"names unknown skill '{skill}'")
                      | [] -> Some(rel, "no '// See skill:' pointer above its namespace")
                      | many -> Some(rel, $"carries {many.Length} pointers"))

              Expect.isEmpty offenders "each bundled .fsi points at exactly one shipped or owner-sourced product skill"
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
              // The converse. A mirror wrongly registered as cross-repo would be carrying a hand-written
              // stamp for a package this repo builds itself — a provenance claim about somewhere else.
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
