module Feature248LineDrawingSkillTests

// Feature 248 — the fs-gg-line-drawing skill + import-and-adapt helper wiring guard.
//
// The generic manifest/materialize gates (Feature 231/238) already prove fs-gg-line-drawing is
// catalogued coherently. This gate asserts the line-drawing-specific wiring the generic tests do not: the
// adaptable source fragment exists with its intended surface and is delete-safe-wired, the skill is gated
// to game/sample-pack and reuses the shared primitives, and the scaffold swap-guidance surfaces list the
// helper as consumer-owned replaceable source (US1/US2/US3).

open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private skillBody = File.ReadAllText(repositoryPath "template/product-skills/fs-gg-line-drawing/SKILL.md")
let private helperSource = repositoryPath "template/fragments/line-drawing/src/Product/LineDrawing.fs"
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
let feature248LineDrawingSkillTests =
    testList
        "Feature248 line-drawing skill + import-and-adapt helper (US1/US2/US3)"
        [
          // ---- US2: the skill exists, is named correctly, reuses the primitives, cites the ref -----
          test "the fs-gg-line-drawing skill declares its name, reuses Cell, cites Red Blob Games" {
              Expect.stringContains skillBody "name: fs-gg-line-drawing" "frontmatter name matches the id"
              Expect.stringContains skillBody "Cell" "grid vocabulary reuses the shared Cell"
              Expect.stringContains skillBody "Pathfinding" "reuses the Pathfinding predicate convention"
              Expect.stringContains skillBody "LineDrawing.fs" "points at the adaptable helper source"
              Expect.stringContains skillBody "redblobgames.com/grids/line-drawing" "cites the Red Blob Games reference"
          }

          test "the skill materializes for game/sample-pack in the manifest" {
              use doc = JsonDocument.Parse manifest
              let entry =
                  doc.RootElement.GetProperty("skills").EnumerateArray()
                  |> Seq.tryFind (fun e -> gs (e.GetProperty("id")) = "fs-gg-line-drawing")
              Expect.isSome entry "fs-gg-line-drawing is in the manifest"
              Expect.equal (gs (entry.Value.GetProperty("materializes-when"))) "profile in [game, sample-pack]" "gated to the sim profiles"
          }

          // ---- US1: the adaptable helper fragment exists and is delete-safe-wired -----------------
          test "the helper fragment source exists with the intended surface" {
              Expect.isTrue (File.Exists helperSource) "LineDrawing.fs fragment exists"
              let src = File.ReadAllText helperSource
              Expect.stringContains src "namespace Product" "literal Product namespace (default sourceName)"
              Expect.stringContains src "module LineDrawing" "the LineDrawing module"
              Expect.stringContains src "open FS.GG.UI.Canvas" "reuses the shared Cell from FS.GG.UI.Canvas"
              for fn in [ "line"; "supercover"; "lineOfSight" ] do
                  Expect.stringContains src (sprintf "let %s" fn) (sprintf "exposes %s" fn)
          }

          test "Product.fsproj compiles LineDrawing.fs profile-gated and Exists-guarded (delete-safe)" {
              Expect.stringContains
                  productFsproj
                  "<Compile Include=\"LineDrawing.fs\" Condition=\"Exists('LineDrawing.fs')\" />"
                  "the compile item is Exists-guarded so deleting the file keeps the build green"
              // it sits inside the game/sample-pack #if region and before Model.fs
              let idx (s: string) = productFsproj.IndexOf(s, System.StringComparison.Ordinal)
              Expect.isLessThan (idx "LineDrawing.fs") (idx "Compile Include=\"Model.fs\"") "LineDrawing.fs compiles before Model.fs (usable from update/view)"
              Expect.isGreaterThan (idx "LineDrawing.fs") (idx "profile == \"game\" || profile == \"sample-pack\"") "LineDrawing.fs is inside the sim-profile gate"
          }

          test "both template.json sources (skill + fragment) are gated to game/sample-pack" {
              Expect.equal (sourceCondition "template/product-skills/fs-gg-line-drawing/") (Some simGate) "skill source gated to sim profiles"
              Expect.equal (sourceCondition "template/fragments/line-drawing/src/") (Some simGate) "fragment source gated to sim profiles"
          }

          // ---- US3: swap-guidance surfaces list the helper as consumer-owned replaceable source ----
          test "model-swap and scaffold-map classify LineDrawing.fs as replaceable/adaptable" {
              Expect.stringContains modelSwapBody "LineDrawing.fs" "model-swap lists the line-drawing helper"
              Expect.stringContains modelSwapBody "[[fs-gg-line-drawing]]" "model-swap links the line-drawing skill"
              Expect.stringContains scaffoldMap "LineDrawing.fs" "scaffold-map classifies the line-drawing helper"
          }
        ]
