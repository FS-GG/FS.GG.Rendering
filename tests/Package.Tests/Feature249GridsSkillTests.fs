module Feature249GridsSkillTests

// Feature 249 — the fs-gg-grids skill + import-and-adapt helper wiring guard.
//
// The generic manifest/materialize gates (Feature 231/238) already prove fs-gg-grids is catalogued
// coherently. This gate asserts the grid-parts-specific wiring the generic tests do not: the adaptable
// source fragment exists with its intended surface and is delete-safe-wired, the skill is gated to
// game/sample-pack and reuses the shared primitives (Cell/Point), and the scaffold swap-guidance surfaces
// list the helper as consumer-owned replaceable source (US1/US2/US3).

open System.IO
open System.Text.RegularExpressions
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private skillBody = File.ReadAllText(repositoryPath "template/product-skills/fs-gg-grids/SKILL.md")
let private helperSource = repositoryPath "template/fragments/grids/src/Product/Grids.fs"
let private productFsproj = File.ReadAllText(repositoryPath "template/base/src/Product/Product.fsproj")
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
let feature249GridsSkillTests =
    testList
        "Feature249 grids skill + import-and-adapt helper (US1/US2/US3)"
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
          // The fs-gg-model-swap half was removed with ADR-0063 (FS.GG.Rendering#965): model-swap is no
          // longer shipped by this provider (owner-sourced from FS.GG.Game.Skills), so its body is not
          // present here to assert against. scaffold-map is Rendering's and still classifies the helper.
          test "scaffold-map classifies Grids.fs as replaceable/adaptable" {
              Expect.stringContains scaffoldMap "Grids.fs" "scaffold-map classifies the grid-parts helper"
          } ]
