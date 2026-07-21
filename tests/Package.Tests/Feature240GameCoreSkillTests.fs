module Feature240GameCoreSkillTests

// Feature 240 (#73) — the FS.GG.Game.Core bundled-surface + package-wiring guard.
//
// ADR-0063 (2026-07-21 amendment, FS.GG.Rendering#965) RETIRED the fs-gg-game-core SKILL copy from this
// provider — it is now owner-sourced from FS.GG.Game.Skills — so the two SC-004 checks that scanned the
// shipped SKILL.md body (every `Geometry.<m>`/`Rng.<m>`/`FixedStep.<m>` token resolves; the body names
// each pattern's entry point) were removed with it: the body is no longer here to scan, and FS.GG.Game's
// own gate holds it against the canonical. What REMAINS is what the retire does NOT touch — the game
// PACKAGE and its bundled doc surface still ship (the product's starter simulation compiles against
// FS.GG.Game.Core): the packed FS.GG.Game.Core api-surface (Rng/FixedStep/Pathfinding/SpatialGrid/
// Geometry/Loop/Physics) and the FS.GG.Game.Core pin/reference on the sim profiles (FR-011/FR-012). This
// file must stay (PackageTestsGateMembershipTests, #613: it holds rules with no PR-gate twin).

open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private gameCoreGeometryFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/Geometry.fsi"
let private gameCoreRngFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/Rng.fsi"
let private gameCoreFixedStepFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/FixedStep.fsi"
let private gameCorePathfindingFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/Pathfinding.fsi"
let private gameCoreSpatialGridFsiPath = repositoryPath "template/base/docs/api-surface/Game.Core/SpatialGrid.fsi"

let private gameCoreSurfaceDir = repositoryPath "template/base/docs/api-surface/Game.Core"

/// The cited module -> the packed .fsi its members must resolve in.
///
/// DERIVED FROM THE SHIPPED SURFACE, not hand-listed (#767). This map used to enumerate five modules by
/// hand, and the regex below repeated the same five by hand again — so a module the mirror gained was
/// invisible to BOTH, and every `NewModule.member` the skill named went unresolved-against-nothing and
/// PASSED. That is the fails-open shape (FS-GG/.github#266): "no citations of that module to check" and
/// "its citations all check out" shared an exit code.
///
/// It was not hypothetical. `Loop` has never been in this map, and the skill has cited `Loop.advance` /
/// `Loop.alpha` / `Loop.init` the whole time — three dangling-reference risks the gate was structurally
/// unable to see. `Physics` (#767) would have been the fourth on the day it was mirrored.
///
/// A mirror file's stem IS its module name (`Geometry.fsi` -> `Geometry`), so the shipped surface can
/// answer this question itself and cannot fall behind what it ships.
/// A `null` stem cannot happen for a path `EnumerateFiles` just handed us — but it is THROWN rather than
/// dropped, because `Seq.choose`-ing it away would silently shrink the module set, which is the one failure
/// this derivation exists to prevent.
let private moduleSurface =
    Directory.EnumerateFiles(gameCoreSurfaceDir, "*.fsi")
    |> Seq.map (fun path ->
        match Path.GetFileNameWithoutExtension path with
        | null -> failwithf "cannot read the module name out of mirror file: %s" path
        | stem -> stem, path)
    |> Map.ofSeq

[<Tests>]
let feature240GameCoreSkillTests =
    testList
        "Feature240 fs-gg-game-core skill surface"
        [
          // THE INSTRUMENT, before the subject (#767 / FS-GG/.github#266). `moduleSurface` is DERIVED, so a
          // reader that comes back empty takes SC-004 with it — and it does not fail cleanly: an empty
          // alternation makes `citedMemberRegex` match the empty module name before every `.member` in the
          // file, and SC-004 then dies in `Map.find ""` with a KeyNotFoundException. That is red, so nothing
          // unsafe merges; it is simply an unreadable way to say "the mirror directory vanished".
          //
          // So the emptiness is named HERE, where the diagnosis is in the failure message, rather than left
          // to surface as a cryptic lookup error three tests down. An empty map is a broken reader, never a
          // clean bill of health.
          test "the module map is derived from the shipped Game.Core surface (the reader is not blind)" {
              Expect.isNonEmpty
                  (moduleSurface |> Map.toList)
                  "no .fsi was found under docs/api-surface/Game.Core — the surface reader has stopped \
                   seeing the mirror, and SC-004 below would check every citation against nothing"

              // The modules this skill is ABOUT. Derivation tracks what is shipped; this is the floor of what
              // must BE shipped, so the map cannot go quietly green by the mirror losing a file.
              for expected in [ "Geometry"; "Rng"; "FixedStep"; "Pathfinding"; "SpatialGrid"; "Loop"; "Physics" ] do
                  Expect.isTrue
                      (moduleSurface |> Map.containsKey expected)
                      (sprintf "the packed Game.Core surface no longer ships %s.fsi" expected)
          }

          // SC-004 (every Module.member the SKILL.md names resolves in the packed surface) and the
          // entry-point completeness check were REMOVED with ADR-0063 (FS.GG.Rendering#965): fs-gg-game-core
          // is no longer shipped by this provider, so there is no body here to scan. FS.GG.Game's own gate
          // holds its owner-sourced body against the canonical. The bundled surface and package pin below —
          // which the retire does NOT touch (the product's starter simulation still compiles against
          // FS.GG.Game.Core) — stay guarded here.

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
