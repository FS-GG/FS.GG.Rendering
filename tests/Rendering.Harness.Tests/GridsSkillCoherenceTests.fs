module GridsSkillCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only fs-gg-grids skill-wiring gate.
//
// WHY THIS EXISTS. The shipped `template/product-skills/fs-gg-grids/SKILL.md` promises a grid parts
// layer (faces/edges/vertices, their adjacency conversions and pixel mapping) delivered as an adaptable
// `Grids.fs` fragment, gated to the game/sample-pack profiles, reusing the shared `Cell` and `Point`,
// citing Red Blob Games. `Feature249GridsSkillTests` (tests/Package.Tests) enforces that wiring, but
// Package.Tests is RELEASE-ONLY: it is not in `FS.GG.Rendering.slnx` and runs only under
// `dotnet test … -c Release` in release.yml. So a SKILL.md edit that renames the helper, a fragment that
// drops a promised member or stops reusing the shared `Cell`/`Point`, an un-gated template.json source,
// or swap-guidance that stops listing the helper all compile green on a PR and only red the release lane.
// That is the "PR-gated tests must be in the slnx" gap #350/#382 already closed for the launch host and
// the audio wiring, and #388/#397/#401/#402/#405/#408/#410/#417 have been closing rule-by-rule for the
// other twins.
//
// WHAT IT LOCKS. Feature249GridsSkillTests is already fully static and self-contained (no pack, no
// restore, no assembly load): it re-derives everything from the shipped skill body, the fragment source,
// Product.fsproj, the model-swap skill body, the scaffold-map, template.json and the skill-manifest. So
// this hoist is a faithful mirror — it reads the SAME seven source-of-truth inputs STATICALLY and asserts
// the SAME invariants, one gate earlier:
//   US2  the skill declares its id, reuses Cell + Point, cites both Red Blob Games references, and
//        materializes only for the sim profiles in the manifest.
//   US1  the adaptable `Grids.fs` fragment exists with its intended parts surface (cellCorners/cellEdges/
//        edgeCells/…/cellAt, EdgeOrientation/Edge/Vertex/GridSpec) and is compiled profile-gated +
//        Exists-guarded (delete-safe) before Model.fs; both template.json sources gated.
//   US3  the model-swap and scaffold-map surfaces classify `Grids.fs` as consumer-owned replaceable
//        source.
//
// Kept in deliberate lockstep with tests/Package.Tests/Feature249GridsSkillTests.fs: the two read
// the same repo inputs and assert the same parity, so a real drift fails BOTH — the release check is
// mirrored earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing (L-INPUTS) so the
// mirror cannot silently desync.

open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private skillBody = File.ReadAllText(repositoryPath "template/product-skills/fs-gg-grids/SKILL.md")
let private helperSource = repositoryPath "template/fragments/grids/src/Product/Grids.fs"
let private productFsproj = File.ReadAllText(repositoryPath "template/base/src/Product/Product.fsproj")
let private modelSwapBody = File.ReadAllText(repositoryPath "template/product-skills/fs-gg-model-swap/SKILL.md")
let private scaffoldMap = File.ReadAllText(repositoryPath "template/base/docs/scaffold-map.md")
let private templateJson = File.ReadAllText(repositoryPath ".template.config/template.json")
let private manifest = File.ReadAllText(repositoryPath "template/skill-manifest/skill-manifest.json")

let private simGate = "(profile == \"game\" || profile == \"sample-pack\")"

/// Read a JSON string element, coercing null to "" (nullable-reference-types is on here).
let private gs (el: JsonElement) = el.GetString() |> Option.ofObj |> Option.defaultValue ""

/// The condition string on the template.json `sources[]` row whose `source` equals `srcPath`, if any.
let private sourceCondition (srcPath: string) : string option =
    use doc = JsonDocument.Parse templateJson

    doc.RootElement.GetProperty("sources").EnumerateArray()
    |> Seq.tryPick (fun s ->
        match s.TryGetProperty "source" with
        | true, v when gs v = srcPath ->
            match s.TryGetProperty "condition" with
            | true, c -> Some(gs c)
            | _ -> Some ""
        | _ -> None)

[<Tests>]
let gridsSkillCoherenceTests =
    testList
        "#366 twin — fs-gg-grids skill surface"
        [
          // ---- US2: the skill exists, is named correctly, reuses the primitives, cites the refs ----
          test "the fs-gg-grids skill declares its name, reuses Cell + Point, cites both Red Blob Games refs" {
              Expect.stringContains skillBody "name: fs-gg-grids" "frontmatter name matches the id"
              Expect.stringContains skillBody "Cell" "the face vocabulary reuses the shared Cell"
              Expect.stringContains skillBody "Point" "the pixel vocabulary reuses the shared Point"
              Expect.stringContains skillBody "Grids.fs" "points at the adaptable helper source"
              Expect.stringContains skillBody "redblobgames.com/grids/parts" "cites the Parts-of-a-grid reference"
              Expect.stringContains skillBody "redblobgames.com/grids/edges" "cites the Grid-edges reference"
          }

          test "the skill materializes for game/sample-pack in the manifest" {
              use doc = JsonDocument.Parse manifest

              let entry =
                  doc.RootElement.GetProperty("skills").EnumerateArray()
                  |> Seq.tryFind (fun e -> gs (e.GetProperty("id")) = "fs-gg-grids")

              Expect.isSome entry "fs-gg-grids is in the manifest"

              Expect.equal
                  (gs (entry.Value.GetProperty("materializes-when")))
                  "profile in [game, sample-pack]"
                  "gated to the sim profiles"
          }

          // ---- US1: the adaptable helper fragment exists and is delete-safe-wired -----------------
          test "the helper fragment source exists with the intended parts surface" {
              Expect.isTrue (File.Exists helperSource) "Grids.fs fragment exists"
              let src = File.ReadAllText helperSource
              Expect.stringContains src "namespace AppRoot" "literal AppRoot identifier namespace (the product-name identifier token, derived to the product namespace on scaffold)"
              Expect.stringContains src "module Grids" "the Grids module"

              for fn in
                  [ "cellCorners"
                    "cellEdges"
                    "edgeCells"
                    "edgeVertices"
                    "vertexCells"
                    "vertexEdges"
                    "cellRect"
                    "cellCenter"
                    "vertexPoint"
                    "edgeSegment"
                    "edgeMidpoint"
                    "cellAt" ] do
                  Expect.stringContains src (sprintf "let %s" fn) (sprintf "exposes %s" fn)

              for ty in [ "EdgeOrientation"; "Edge"; "Vertex"; "GridSpec" ] do
                  Expect.stringContains src (sprintf "type %s" ty) (sprintf "exposes %s" ty)
          }

          test "Product.fsproj compiles Grids.fs profile-gated and Exists-guarded (delete-safe)" {
              Expect.stringContains
                  productFsproj
                  "<Compile Include=\"Grids.fs\" Condition=\"Exists('Grids.fs')\" />"
                  "the compile item is Exists-guarded so deleting the file keeps the build green"

              let idx (s: string) = productFsproj.IndexOf(s, System.StringComparison.Ordinal)

              Expect.isLessThan
                  (idx "Grids.fs")
                  (idx "Compile Include=\"Model.fs\"")
                  "Grids.fs compiles before Model.fs (usable from update/view)"

              Expect.isGreaterThan
                  (idx "Grids.fs")
                  (idx "profile == \"game\" || profile == \"sample-pack\"")
                  "Grids.fs is inside the sim-profile gate"
          }

          test "both template.json sources (skill + fragment) are gated to game/sample-pack" {
              Expect.equal
                  (sourceCondition "template/product-skills/fs-gg-grids/")
                  (Some simGate)
                  "skill source gated to sim profiles"

              Expect.equal
                  (sourceCondition "template/fragments/grids/src/")
                  (Some simGate)
                  "fragment source gated to sim profiles"
          }

          // ---- US3: swap-guidance surfaces list the helper as consumer-owned replaceable source ----
          test "model-swap and scaffold-map classify Grids.fs as replaceable/adaptable" {
              Expect.stringContains modelSwapBody "Grids.fs" "model-swap lists the grid-parts helper"
              Expect.stringContains modelSwapBody "[[fs-gg-grids]]" "model-swap links the grids skill"
              Expect.stringContains scaffoldMap "Grids.fs" "scaffold-map classifies the grid-parts helper"
          } ]
