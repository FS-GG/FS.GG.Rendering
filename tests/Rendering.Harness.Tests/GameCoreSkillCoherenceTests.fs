module GameCoreSkillCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only fs-gg-game-core skill-surface gate.
//
// WHY THIS EXISTS. The shipped `template/product-skills/fs-gg-game-core/SKILL.md` advises the
// simulation primitives (Geometry / Rng / FixedStep / Pathfinding / SpatialGrid). It is only honest
// if every `FS.GG.Game.Core` member it names actually exists in the surface a generated product
// bundles — the verbatim-copied api-surface `.fsi` under `template/base/docs/api-surface/Game.Core/`.
// `Feature240GameCoreSkillTests` (tests/Package.Tests) enforces that, but Package.Tests is
// RELEASE-ONLY: it is not in `FS.GG.Rendering.slnx` and runs only under `dotnet test … -c Release` in
// release.yml. So a SKILL.md edit that cites a renamed/hallucinated member — or a mirror `.fsi` that
// dropped it, or a lost `FS.GG.Game.Core` pin — compiles green on a PR and only reds the release lane.
// That is the "PR-gated tests must be in the slnx" gap #350/#382 already closed for the launch host
// and the audio wiring, and #388/#397/#401/#402 have been closing rule-by-rule for the other twins.
//
// WHAT IT LOCKS. Feature240GameCoreSkillTests is already fully static and self-contained (no pack, no
// restore, no report): it re-derives everything from the shipped SKILL.md and the bundled `.fsi` set.
// So this hoist is a faithful mirror — it reads the SAME eight source-of-truth inputs STATICALLY (the
// skill body, the five packed Game.Core `.fsi`, Directory.Packages.props and Product.fsproj) and
// asserts the SAME invariants, one gate earlier:
//   SC-004  every `Module.member` the body cites resolves to a `val`/`type` in the packed `.fsi`
//           (a renamed/dangling reference reds).
//   compl.  the body names the key entry point of each pattern, so it cannot silently drop a primitive.
//   FR-012  the packed FS.GG.Game.Core surface is bundled with the Geometry module.
//   FR-011  FS.GG.Game.Core is pinned in Directory.Packages.props and referenced by the (sim-gated) product.
//
// Kept in deliberate lockstep with tests/Package.Tests/Feature240GameCoreSkillTests.fs: the two read
// the same repo inputs and assert the same parity, so a real drift fails BOTH — the release check is
// mirrored earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing (L-INPUTS) so the
// mirror cannot silently desync.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private skillBodyPath = repositoryPath "template/product-skills/fs-gg-game-core/SKILL.md"
let private gameCoreGeometryFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/Geometry.fsi"
let private gameCoreRngFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/Rng.fsi"
let private gameCoreFixedStepFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/FixedStep.fsi"
let private gameCorePathfindingFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/Pathfinding.fsi"
let private gameCoreSpatialGridFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/SpatialGrid.fsi"

/// The cited module -> the packed .fsi its members must resolve in. Mirrors
/// Feature240GameCoreSkillTests.moduleSurface so the two classify citations identically.
let private moduleSurface =
    [ "Geometry", gameCoreGeometryFsiPath
      "Rng", gameCoreRngFsiPath
      "FixedStep", gameCoreFixedStepFsiPath
      "Pathfinding", gameCorePathfindingFsiPath
      "SpatialGrid", gameCoreSpatialGridFsiPath ]
    |> Map.ofList

/// A packed .fsi declares a member `m` when it carries `val m:` or the type `type m` (for `Rng`).
let private declaresMember (fsiText: string) (m: string) =
    Regex.IsMatch(fsiText, sprintf @"(^|\s)val\s+%s\s*:" (Regex.Escape m), RegexOptions.Multiline)
    || Regex.IsMatch(fsiText, sprintf @"(^|\s)type\s+%s\b" (Regex.Escape m))

[<Tests>]
let gameCoreSkillCoherenceTests =
    testList
        "#366 twin — fs-gg-game-core skill surface"
        [
          // SC-004 — every `Module.member` the body names resolves in the packed api-surface .fsi.
          // A deliberately-renamed member (e.g. `Geometry.overlaps`) has no `val`/`type` and fails here.
          test "every Geometry/Rng/FixedStep member cited in SKILL.md resolves in the packed surface" {
              let body = File.ReadAllText skillBodyPath
              let fsiText = moduleSurface |> Map.map (fun _ path -> File.ReadAllText path)

              // Match `Module.member` in code position only: members are lowercase-initial F# values, and
              // the lookbehind rejects file-path / qualified contexts like `Game.Core/Rng.fsi` or `X.Rng.y`.
              let cited =
                  Regex.Matches(body, @"(?<![\w./])(Geometry|Rng|FixedStep|Pathfinding|SpatialGrid)\.([a-z][A-Za-z0-9']*)")
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
              for token in [ "FixedStep.drain"; "Rng.ofSeed"; "Rng.split"; "Geometry.intersects"; "Geometry.sweptIntersects"; "Pathfinding.astar"; "Pathfinding.bfs"; "SpatialGrid.build"; "SpatialGrid.queryRadius" ] do
                  Expect.stringContains body token (sprintf "SKILL.md must demonstrate %s" token)
          }

          // FR-012 — the bundled surface that makes the citations resolvable exists in the product tree.
          test "the packed FS.GG.Game.Core surface is bundled with the Geometry module (FR-012)" {
              for path in [ gameCoreRngFsiPath; gameCoreFixedStepFsiPath; gameCorePathfindingFsiPath; gameCoreSpatialGridFsiPath; gameCoreGeometryFsiPath ] do
                  Expect.isTrue (File.Exists path) (sprintf "packed FS.GG.Game.Core surface missing: %s" path)
              let geometry = File.ReadAllText gameCoreGeometryFsiPath
              Expect.stringContains geometry "module Geometry =" "packed Game.Core/Geometry.fsi must carry the Geometry module"
          }

          // FR-011 — FS.GG.Game.Core is pinned and referenced so a game/sample-pack product can compile
          // Rng/FixedStep/Pathfinding/SpatialGrid/Geometry.
          test "FS.GG.Game.Core is pinned in Directory.Packages.props and referenced by the product (FR-011)" {
              let props = File.ReadAllText (repositoryPath "template/base/Directory.Packages.props")
              let proj = File.ReadAllText (repositoryPath "template/base/src/Product/Product.fsproj")
              Expect.stringContains props "Include=\"FS.GG.Game.Core\"" "Directory.Packages.props must pin FS.GG.Game.Core"
              Expect.stringContains proj "Include=\"FS.GG.Game.Core\"" "Product.fsproj must reference FS.GG.Game.Core"
              // gated to the simulation profiles only (matches the skill's materializes-when).
              Expect.stringContains proj "profile == \"game\" || profile == \"sample-pack\"" "Game.Core reference is sim-profile gated"
          }
        ]
