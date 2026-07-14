module Feature240GameCoreSkillTests

// Feature 240 (#73) — the fs-gg-game-core product skill surface guard.
//
// The skill body advises the Feature-239 simulation primitives (Geometry / Rng / FixedStep). It is only
// honest if every FS.GG.Game.Core member it names actually exists in the surface a generated product
// bundles — the verbatim-copied api-surface .fsi (Feature 060). This gate scans the shipped SKILL.md for
// every `Geometry.<m>` / `Rng.<m>` / `FixedStep.<m>` token and fails the build if any does NOT resolve to
// a `val`/member in the matching packed .fsi (SC-004: a renamed/hallucinated reference fails). It also
// asserts the packaging that makes those APIs consumable exists (FR-011/FR-012): the bundled
// FS.GG.Game.Core surface (Rng/FixedStep/Pathfinding/SpatialGrid/Geometry) and the FS.GG.Game.Core
// package pin/reference on the sim profiles.

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

/// `Module.member` in code position. The alternation is the DERIVED module set, so it tracks the surface.
/// The lookbehind rejects file-path / qualified contexts like `Game.Core/Rng.fsi` or `X.Rng.y`.
let private citedMemberRegex =
    let alternation =
        moduleSurface |> Map.toList |> List.map (fst >> Regex.Escape) |> String.concat "|"

    Regex(sprintf @"(?<![\w./])(%s)\.([a-z][A-Za-z0-9']*)" alternation)

/// A packed .fsi declares a member `m` when it carries `val m:` or the type `type m` (for `Rng`).
let private declaresMember (fsiText: string) (m: string) =
    Regex.IsMatch(fsiText, sprintf @"(^|\s)val\s+%s\s*:" (Regex.Escape m), RegexOptions.Multiline)
    || Regex.IsMatch(fsiText, sprintf @"(^|\s)type\s+%s\b" (Regex.Escape m))

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

          // SC-004 — every `Module.member` the body names resolves in the packed api-surface .fsi.
          // A deliberately-renamed member (e.g. `Geometry.overlaps`) has no `val`/`type` and fails here.
          test "every Game.Core member cited in SKILL.md resolves in the packed surface" {
              let body = File.ReadAllText skillBodyPath
              let fsiText = moduleSurface |> Map.map (fun _ path -> File.ReadAllText path)

              let cited =
                  citedMemberRegex.Matches body
                  |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
                  |> Seq.filter (fun (_, member') -> member' <> "fsi")
                  |> Seq.distinct
                  |> Seq.toList

              Expect.isNonEmpty cited "the body must cite at least one Game.Core member"

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

          // Completeness — each pattern names its key entry point, so the body cannot silently drop a
          // primitive and still pass the resolve check above (which only judges what IS cited).
          //
          // `Physics` is NOT in this list, and that is not an oversight (#767). This SKILL.md is a FROZEN
          // MIRROR of a body FS.GG.Game owns (ADR-0022 §6, and `scripts/check-frozen-mirrors.fsx` reds the
          // gate on any edit to it), so THIS repo cannot make it name `Physics.step` — the canonical body
          // has to, and this copy is re-frozen from it afterwards. Demanding the token here would wedge the
          // gate on a change no one in this repo is allowed to make. Until then the eight `Physics` vals are
          // carried in `tests/Package.Tests/surface-doc-ledger.txt` as a DECLARED gap with the cross-repo
          // issue on it, which is the honest place for a decision somebody made.
          test "SKILL.md names the key entry point of each pattern" {
              let body = File.ReadAllText skillBodyPath

              for token in
                  [ "FixedStep.drain"
                    "Rng.ofSeed"
                    "Rng.split"
                    "Geometry.intersects"
                    "Geometry.sweptIntersects"
                    "Pathfinding.astar"
                    "Pathfinding.bfs"
                    "SpatialGrid.build"
                    "SpatialGrid.queryRadius" ] do
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
