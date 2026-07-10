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
//           The converse is NOT asserted: a directory may outlive a reference (e.g. Symbology, which
//           is bundled but referenced only by the sample-pack content, not Product.fsproj).
//   M-PTR   every bundled .fsi names the product skill that teaches it, so the .fsi and the SKILL.md
//           stop being an unlinked pair (§5.3 — the report's author read the .fsi and never found
//           `Scene.measureText`, which the fs-gg-scene skill documents under its own heading).
//   M-PROV  every CROSS-REPO mirror records the version it was copied from, and that version must
//           equal the `$(FsGgAudioVersion)` the template pins. This is ADR-0024's staleness concern
//           in enforced form: the copy can no longer outlive its package in silence.

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
    else packageId

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

/// The `// See skill:` pointer must sit in the header, above the `namespace` declaration.
let private skillPointers (file: string) =
    File.ReadAllLines file
    |> Array.takeWhile (fun line -> not (line.StartsWith "namespace"))
    |> Array.choose (fun line ->
        let m = Regex.Match(line, @"^//\s*See skill:\s*(\S+)\s*$")
        if m.Success then Some m.Groups.[1].Value else None)
    |> Array.toList

/// No shipped product skill covers FS.GG.UI.Canvas's immediate-mode drawing/loop surface. Recorded
/// here rather than papered over with a pointer to a skill that does not teach these members.
let private pointerExempt = set [ "Canvas/Elements.fsi"; "Canvas/Loop.fsi" ]

let private shippedProductSkills () =
    Directory.GetDirectories productSkillsRoot
    |> Array.map (fun d -> DirectoryInfo(d).Name)
    |> Set.ofArray

let private pinnedAudioVersion () =
    let m = Regex.Match(File.ReadAllText templatePackagesPath, @"<FsGgAudioVersion>([^<]+)</FsGgAudioVersion>")
    if m.Success then m.Groups.[1].Value else failwith "template/base/Directory.Packages.props pins no FsGgAudioVersion"

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
                      let dir = Path.Combine(apiSurfaceRoot, surfaceDirectoryFor pkg)

                      if Directory.Exists dir && Directory.GetFiles(dir, "*.fsi").Length > 0 then
                          None
                      else
                          Some(pkg, surfaceDirectoryFor pkg))

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
          test "every cross-repo Audio mirror records the FS.GG.Audio version the template pins" {
              let pinned = pinnedAudioVersion ()

              let offenders =
                  bundledFsiFiles ()
                  |> List.filter (fun f -> (owningPackageDirectory f).StartsWith "Audio.")
                  |> List.choose (fun file ->
                      let m =
                          Regex.Match(File.ReadAllText file, @"^// Mirrored from FS-GG/FS\.GG\.Audio @ (\S+)", RegexOptions.Multiline)

                      if not m.Success then
                          Some(relativeToSurfaceRoot file, "<no provenance line>")
                      elif m.Groups.[1].Value <> pinned then
                          Some(relativeToSurfaceRoot file, m.Groups.[1].Value)
                      else
                          None)

              Expect.isEmpty offenders $"each Audio.* mirror is stamped with the pinned FsGgAudioVersion ({pinned})"
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
