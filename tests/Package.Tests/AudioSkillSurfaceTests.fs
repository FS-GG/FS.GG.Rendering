module AudioSkillSurfaceTests

// FS.GG.Rendering#160 / FS.GG.Game#20 (ADR-0024 step 4) — the fs-gg-audio product-skill surface guard.
//
// WHY THIS EXISTS. Canvas 0.3.0 (#158) retired the Audio request surface from FS.GG.UI.Canvas; the
// vocabulary moved to the standalone FS.GG.Audio.Core. Nothing went red. The skill kept telling
// authors to `open FS.GG.UI.Canvas` for `AudioEffect`, and the product kept bundling a
// `docs/api-surface/Canvas/Audio.fsi` declaring `namespace FS.GG.UI.Canvas` — both dead. Generated
// products still BUILT, because no product source consumes `AudioEffect`: the skill is guidance and
// the bundled `.fsi` is a doc copy. A breaking removal in a library silently invalidated a shipped
// skill, and only a human reading the two files could tell.
//
// This gate closes that class. It mirrors Feature240GameCoreSkillTests (the same guard for
// FS.GG.Game.Core) and adds the generalization the incident argued for:
//   A-MEMBERS — every `Audio.<member>` the shipped SKILL.md names resolves to a `val`/`type` in the
//               bundled surface (a renamed/hallucinated/relocated member fails).
//   A-BUNDLE  — the surface the skill CITES is the surface the product actually ships.
//   A-RETIRED — the retired Canvas audio doc-copy is gone, and the body no longer opens Canvas for
//               audio (naming Canvas in the migration pitfall is fine — that is the point of it).
//   A-NS      — EVERY bundled `docs/api-surface/<Pkg>/*.fsi` declares a namespace carrying `<Pkg>` as
//               a dotted component. This is the drift check that would have caught #158 at the source:
//               a doc copy can no longer outlive the package whose name it claims. The rule is
//               deliberately "dotted component", not "equals": `Controls/*.fsi` legitimately declare
//               `FS.GG.UI.Controls.Typed`, and `Themes.Default/Theming.fsi` declares
//               `FS.GG.UI.Themes.Default.Theming`. Verified against all 55 bundled surfaces.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private skillBodyPath = repositoryPath "template/product-skills/fs-gg-audio/SKILL.md"
let private apiSurfaceRoot = repositoryPath "template/base/docs/api-surface"

/// The product-relative path the skill cites, and where the template really bundles it.
let private citedSurfaceRelative = "docs/api-surface/Audio.Core/Audio.fsi"
let private audioCoreFsiPath = repositoryPath ("template/base/" + citedSurfaceRelative)

/// The doc copy retired with Canvas 0.3.0 (ADR-0024, #158) — it must not come back.
let private retiredCanvasAudioFsi = repositoryPath "template/base/docs/api-surface/Canvas/Audio.fsi"

/// A packed .fsi declares a member `m` when it carries `val m:` or `type m`.
let private declaresMember (fsiText: string) (m: string) =
    Regex.IsMatch(fsiText, sprintf @"(^|\s)val\s+%s\s*:" (Regex.Escape m), RegexOptions.Multiline)
    || Regex.IsMatch(fsiText, sprintf @"(^|\s)type\s+%s\b" (Regex.Escape m))

/// The first `namespace` declaration in a signature file, if any.
let private declaredNamespace (fsiText: string) =
    let m = Regex.Match(fsiText, @"^namespace\s+(\S+)", RegexOptions.Multiline)
    if m.Success then Some(m.Groups.[1].Value) else None

[<Tests>]
let audioSkillSurfaceTests =
    testList
        "fs-gg-audio skill surface (ADR-0024 step 4)"
        [
          // A-MEMBERS — a member the body names but the surface does not declare is a lie the build
          // must not ship. `Audio.playSfx` etc. resolve; `Audio.play` (renamed) would not.
          test "every Audio.<member> cited in SKILL.md resolves in the bundled FS.GG.Audio.Core surface" {
              let body = File.ReadAllText skillBodyPath
              let fsiText = File.ReadAllText audioCoreFsiPath

              // Code position only: members are lowercase-initial F# values; the lookbehind rejects
              // file-path / qualified contexts like `Audio.Core/Audio.fsi` or `X.Audio.y`.
              let cited =
                  Regex.Matches(body, @"(?<![\w./])Audio\.([a-z][A-Za-z0-9']*)")
                  |> Seq.map (fun m -> m.Groups.[1].Value)
                  |> Seq.distinct
                  |> Seq.toList

              Expect.isNonEmpty cited "the skill body cites at least one Audio member"

              let unresolved = cited |> List.filter (declaresMember fsiText >> not)
              Expect.isEmpty unresolved $"every cited Audio.<member> resolves in {citedSurfaceRelative}"
          }

          // A-BUNDLE — the surface the skill points a reader at is the one the product ships, and it
          // is the FS.GG.Audio.Core surface (not a Canvas-era copy sitting at a new path).
          test "the cited FS.GG.Audio.Core surface is bundled and declares its own namespace" {
              Expect.isTrue (File.Exists audioCoreFsiPath) $"bundled surface missing: {citedSurfaceRelative}"

              let body = File.ReadAllText skillBodyPath
              Expect.stringContains body citedSurfaceRelative "SKILL.md cites the bundled surface path"

              let ns = declaredNamespace (File.ReadAllText audioCoreFsiPath)
              Expect.equal ns (Some "FS.GG.Audio.Core") "the bundled audio surface declares namespace FS.GG.Audio.Core"
          }

          // A-RETIRED — the Canvas-era doc copy is gone, and the body opens the new namespace. Naming
          // FS.GG.UI.Canvas in the migration pitfall is intentional, so assert on the `open`, not the token.
          test "the retired Canvas audio surface is gone and the skill opens FS.GG.Audio.Core" {
              Expect.isFalse
                  (File.Exists retiredCanvasAudioFsi)
                  "template/base/docs/api-surface/Canvas/Audio.fsi was retired with Canvas 0.3.0 (#158)"

              let body = File.ReadAllText skillBodyPath
              Expect.stringContains body "open FS.GG.Audio.Core" "the skill opens the standalone audio namespace"

              Expect.isFalse
                  (Regex.IsMatch(body, @"^\s*open\s+FS\.GG\.UI\.Canvas\s*$", RegexOptions.Multiline))
                  "the skill no longer opens FS.GG.UI.Canvas for the audio vocabulary"
          }

          // A-NS — the general anti-drift rule. A bundled doc copy cannot outlive the package it claims:
          // its declared namespace must carry the directory name as a dotted component.
          test "every bundled api-surface .fsi declares a namespace carrying its package directory" {
              let offenders =
                  Directory.GetDirectories apiSurfaceRoot
                  |> Seq.collect (fun dir ->
                      let pkg = DirectoryInfo(dir).Name

                      Directory.GetFiles(dir, "*.fsi")
                      |> Seq.choose (fun file ->
                          let text = File.ReadAllText file

                          match declaredNamespace text with
                          | None -> Some(file, "<no namespace declaration>")
                          | Some ns ->
                              let pattern = sprintf @"(^|\.)%s(\.|$)" (Regex.Escape pkg)
                              if Regex.IsMatch(ns, pattern) then None else Some(file, ns)))
                  |> Seq.toList

              Expect.isEmpty offenders "each bundled <Pkg>/*.fsi declares a namespace containing <Pkg>"
          }
        ]
