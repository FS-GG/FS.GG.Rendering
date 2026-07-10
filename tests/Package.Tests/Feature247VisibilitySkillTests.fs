module Feature247VisibilitySkillTests

// Feature 247 — the fs-gg-visibility skill + import-and-adapt helper wiring guard.
//
// The generic manifest/materialize gates (Feature 231/238) already prove fs-gg-visibility is catalogued
// coherently. This gate asserts the visibility-specific wiring the generic tests do not: the adaptable
// source fragment exists with its intended surface and is delete-safe-wired, the skill is gated to
// game/sample-pack and reuses the shared primitives, and the scaffold swap-guidance surfaces list the
// helper as consumer-owned replaceable source (US1/US2/US3).

open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private skillBody = File.ReadAllText(repositoryPath "template/product-skills/fs-gg-visibility/SKILL.md")
let private helperSource = repositoryPath "template/fragments/visibility/src/Product/Visibility.fs"
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
let feature247VisibilitySkillTests =
    testList
        "Feature247 visibility skill + import-and-adapt helper (US1/US2/US3)"
        [
          // ---- US2: the skill exists, is named correctly, reuses the primitives, cites the ref -----
          test "the fs-gg-visibility skill declares its name, reuses Point, cites Red Blob Games" {
              Expect.stringContains skillBody "name: fs-gg-visibility" "frontmatter name matches the id"
              Expect.stringContains skillBody "Point" "geometry vocabulary reuses the shared Point"
              Expect.stringContains skillBody "Visibility.fs" "points at the adaptable helper source"
              Expect.stringContains skillBody "redblobgames.com/articles/visibility" "cites the Red Blob Games reference"
          }

          // #261 — the skill used to teach endpoint-bucketing as the right answer, so an agent following
          // it hand-rolled the very cull that drops a spanning wall. It must now teach the exact test,
          // and the helper must not reach for the point-keyed grid it warns against.
          test "the skill teaches an exact segment-vs-box cull, and the helper does not bucket endpoints" {
              Expect.stringContains skillBody "segment-vs-box" "the cull is described as an exact segment-vs-box test"
              Expect.stringContains
                  skillBody
                  "both ends outside"
                  "the skill names the spanning-wall case the endpoint cull dropped"

              // The word survives in a comment explaining why the grid is the wrong tool here; what must
              // not survive is a CALL back into the point-keyed cull.
              let src = File.ReadAllText helperSource
              for call in [ "SpatialGrid.build"; "SpatialGrid.query" ] do
                  Expect.isFalse
                      (src.Contains(call, System.StringComparison.Ordinal))
                      $"polygon culls occluders without calling {call} (#261)"
          }

          test "the skill materializes for game/sample-pack in the manifest" {
              use doc = JsonDocument.Parse manifest
              let entry =
                  doc.RootElement.GetProperty("skills").EnumerateArray()
                  |> Seq.tryFind (fun e -> gs (e.GetProperty("id")) = "fs-gg-visibility")
              Expect.isSome entry "fs-gg-visibility is in the manifest"
              Expect.equal (gs (entry.Value.GetProperty("materializes-when"))) "profile in [game, sample-pack]" "gated to the sim profiles"
          }

          // ---- US1: the adaptable helper fragment exists and is delete-safe-wired -----------------
          test "the helper fragment source exists with the intended surface" {
              Expect.isTrue (File.Exists helperSource) "Visibility.fs fragment exists"
              let src = File.ReadAllText helperSource
              Expect.stringContains src "namespace AppRoot" "literal AppRoot identifier namespace (the product-name identifier token, derived to the product namespace on scaffold)"
              Expect.stringContains src "module Visibility" "the Visibility module"
              for fn in [ "raySegment"; "isVisible"; "polygon" ] do
                  Expect.stringContains src (sprintf "let %s" fn) (sprintf "exposes %s" fn)
              for ty in [ "Segment"; "Settings"; "VisibilityPolygon" ] do
                  Expect.stringContains src (sprintf "type %s" ty) (sprintf "exposes %s" ty)
          }

          test "Product.fsproj compiles Visibility.fs profile-gated and Exists-guarded (delete-safe)" {
              Expect.stringContains
                  productFsproj
                  "<Compile Include=\"Visibility.fs\" Condition=\"Exists('Visibility.fs')\" />"
                  "the compile item is Exists-guarded so deleting the file keeps the build green"
              // it sits inside the game/sample-pack #if region and before Model.fs
              let idx (s: string) = productFsproj.IndexOf(s, System.StringComparison.Ordinal)
              Expect.isLessThan (idx "Visibility.fs") (idx "Compile Include=\"Model.fs\"") "Visibility.fs compiles before Model.fs (usable from update/view)"
              Expect.isGreaterThan (idx "Visibility.fs") (idx "profile == \"game\" || profile == \"sample-pack\"") "Visibility.fs is inside the sim-profile gate"
          }

          test "both template.json sources (skill + fragment) are gated to game/sample-pack" {
              Expect.equal (sourceCondition "template/product-skills/fs-gg-visibility/") (Some simGate) "skill source gated to sim profiles"
              Expect.equal (sourceCondition "template/fragments/visibility/src/") (Some simGate) "fragment source gated to sim profiles"
          }

          // ---- US3: swap-guidance surfaces list the helper as consumer-owned replaceable source ----
          test "model-swap and scaffold-map classify Visibility.fs as replaceable/adaptable" {
              Expect.stringContains modelSwapBody "Visibility.fs" "model-swap lists the visibility helper"
              Expect.stringContains modelSwapBody "[[fs-gg-visibility]]" "model-swap links the visibility skill"
              Expect.stringContains scaffoldMap "Visibility.fs" "scaffold-map classifies the visibility helper"
          }
        ]
