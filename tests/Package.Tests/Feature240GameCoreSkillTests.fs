module Feature240GameCoreSkillTests

// Feature 240 (#73) — the fs-gg-game-core product skill surface guard.
//
// The skill body advises the Feature-239 simulation primitives (Geometry / Rng / FixedStep). It is only
// honest if every FS.GG.UI member it names actually exists in the surface a generated product bundles —
// the verbatim-copied api-surface .fsi (Feature 060). This gate scans the shipped SKILL.md for every
// `Geometry.<m>` / `Rng.<m>` / `FixedStep.<m>` token and fails the build if any does NOT resolve to a
// `val`/member in the matching packed .fsi (SC-004: a renamed/hallucinated reference fails). It also
// asserts the packaging that makes those APIs consumable exists (FR-011/FR-012): the bundled Canvas
// surface, the refreshed Geometry module, and the Canvas package pin/reference on the sim profiles.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private skillBodyPath = repositoryPath "template/product-skills/fs-gg-game-core/SKILL.md"
let private sceneFsiPath = repositoryPath "template/base/docs/api-surface/Scene/Scene.fsi"
let private canvasRngFsiPath = repositoryPath "template/base/docs/api-surface/Canvas/Rng.fsi"
let private canvasFixedStepFsiPath = repositoryPath "template/base/docs/api-surface/Canvas/FixedStep.fsi"

/// The cited module -> the packed .fsi its members must resolve in.
let private moduleSurface =
    [ "Geometry", sceneFsiPath
      "Rng", canvasRngFsiPath
      "FixedStep", canvasFixedStepFsiPath ]
    |> Map.ofList

/// A packed .fsi declares a member `m` when it carries `val m:` or the type `type m` (for `Rng`).
let private declaresMember (fsiText: string) (m: string) =
    Regex.IsMatch(fsiText, sprintf @"(^|\s)val\s+%s\s*:" (Regex.Escape m), RegexOptions.Multiline)
    || Regex.IsMatch(fsiText, sprintf @"(^|\s)type\s+%s\b" (Regex.Escape m))

[<Tests>]
let feature240GameCoreSkillTests =
    testList
        "Feature240 fs-gg-game-core skill surface"
        [
          // SC-004 — every `Module.member` the body names resolves in the packed api-surface .fsi.
          // A deliberately-renamed member (e.g. `Geometry.overlaps`) has no `val`/`type` and fails here.
          test "every Geometry/Rng/FixedStep member cited in SKILL.md resolves in the packed surface" {
              let body = File.ReadAllText skillBodyPath
              let fsiText = moduleSurface |> Map.map (fun _ path -> File.ReadAllText path)

              // Match `Module.member` in code position only: members are lowercase-initial F# values, and
              // the lookbehind rejects file-path / qualified contexts like `Canvas/Rng.fsi` or `X.Rng.y`.
              let cited =
                  Regex.Matches(body, @"(?<![\w./])(Geometry|Rng|FixedStep)\.([a-z][A-Za-z0-9']*)")
                  |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
                  |> Seq.filter (fun (_, member') -> member' <> "fsi")
                  |> Seq.distinct
                  |> Seq.toList

              Expect.isNonEmpty cited "the body must cite at least one Geometry/Rng/FixedStep member"

              for moduleName, member' in cited do
                  let fsi = Map.find moduleName fsiText
                  Expect.isTrue
                      (declaresMember fsi member')
                      (sprintf
                          "SKILL.md cites %s.%s but the packed %s surface does not declare it — a dangling/renamed reference"
                          moduleName
                          member'
                          moduleName)
          }

          // Completeness — the four patterns each name their key entry point, so the body cannot silently
          // drop a primitive and still pass the resolve check above.
          test "SKILL.md names the key entry point of each of the four patterns" {
              let body = File.ReadAllText skillBodyPath
              for token in [ "FixedStep.drain"; "Rng.ofSeed"; "Rng.split"; "Geometry.intersects"; "Geometry.sweptIntersects" ] do
                  Expect.stringContains body token (sprintf "SKILL.md must demonstrate %s" token)
          }

          // FR-012 — the bundled surface that makes the citations resolvable exists in the product tree.
          test "the packed Canvas surface is bundled and Scene carries the Geometry module (FR-012)" {
              for path in [ canvasRngFsiPath; canvasFixedStepFsiPath ] do
                  Expect.isTrue (File.Exists path) (sprintf "packed Canvas surface missing: %s" path)
              let scene = File.ReadAllText sceneFsiPath
              Expect.stringContains scene "module Geometry =" "packed Scene.fsi must carry the refreshed Geometry module"
          }

          // FR-011 — Canvas is pinned and referenced so a game/sample-pack product can compile Rng/FixedStep.
          test "Canvas is pinned in Directory.Packages.props and referenced by the product (FR-011)" {
              let props = File.ReadAllText (repositoryPath "template/base/Directory.Packages.props")
              let proj = File.ReadAllText (repositoryPath "template/base/src/Product/Product.fsproj")
              Expect.stringContains props "Include=\"FS.GG.UI.Canvas\"" "Directory.Packages.props must pin FS.GG.UI.Canvas"
              Expect.stringContains proj "Include=\"FS.GG.UI.Canvas\"" "Product.fsproj must reference FS.GG.UI.Canvas"
              // gated to the simulation profiles only (matches the skill's materializes-when).
              Expect.stringContains proj "profile == \"game\" || profile == \"sample-pack\"" "Canvas reference is sim-profile gated"
          }
        ]
