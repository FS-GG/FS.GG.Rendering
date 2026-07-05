module Feature246CollisionSkillTests

// Feature 246 (#…) — the fs-gg-collision skill + import-and-adapt helper wiring guard.
//
// The generic manifest/materialize gates (Feature 231/238) already prove fs-gg-collision is catalogued
// coherently. This gate asserts the collision-specific wiring the generic tests do not: the adaptable
// source fragment exists and is delete-safe-wired, the skill is gated to game/sample-pack, and the
// fs-gg-game-core Collision section was trimmed to a single-source-of-truth pointer (US2/US3).

open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private skillBody = File.ReadAllText(repositoryPath "template/product-skills/fs-gg-collision/SKILL.md")
let private helperSource = repositoryPath "template/fragments/collision/src/Product/Collision.fs"
let private productFsproj = File.ReadAllText(repositoryPath "template/base/src/Product/Product.fsproj")
let private gameCoreBody = File.ReadAllText(repositoryPath "template/product-skills/fs-gg-game-core/SKILL.md")
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
let feature246CollisionSkillTests =
    testList
        "Feature246 collision skill + import-and-adapt helper (US1/US2/US3)"
        [
          // ---- US2: the skill exists, is named correctly, reuses the primitives -----------------
          test "the fs-gg-collision skill declares its name and reuses Geometry + SpatialGrid" {
              Expect.stringContains skillBody "name: fs-gg-collision" "frontmatter name matches the id"
              Expect.stringContains skillBody "Geometry" "narrow-phase reuses Geometry"
              Expect.stringContains skillBody "SpatialGrid" "broad-phase reuses SpatialGrid"
              Expect.stringContains skillBody "Collision.fs" "points at the adaptable helper source"
          }

          test "the skill materializes for game/sample-pack in the manifest" {
              use doc = JsonDocument.Parse manifest
              let entry =
                  doc.RootElement.GetProperty("skills").EnumerateArray()
                  |> Seq.tryFind (fun e -> gs (e.GetProperty("id")) = "fs-gg-collision")
              Expect.isSome entry "fs-gg-collision is in the manifest"
              Expect.equal (gs (entry.Value.GetProperty("materializes-when"))) "profile in [game, sample-pack]" "gated to the sim profiles"
          }

          // ---- US1: the adaptable helper fragment exists and is delete-safe-wired ----------------
          test "the helper fragment source exists with the intended surface" {
              Expect.isTrue (File.Exists helperSource) "Collision.fs fragment exists"
              let src = File.ReadAllText helperSource
              Expect.stringContains src "namespace Product" "literal Product namespace (default sourceName)"
              Expect.stringContains src "module Collision" "the Collision module"
              for fn in [ "contact"; "collide"; "resolve"; "step" ] do
                  Expect.stringContains src (sprintf "let %s" fn) (sprintf "exposes %s" fn)
              for ty in [ "Body"; "Contact"; "Resolution"; "ResponseRule" ] do
                  Expect.stringContains src (sprintf "type %s" ty) (sprintf "exposes %s" ty)
          }

          test "Product.fsproj compiles Collision.fs profile-gated and Exists-guarded (delete-safe)" {
              Expect.stringContains
                  productFsproj
                  "<Compile Include=\"Collision.fs\" Condition=\"Exists('Collision.fs')\" />"
                  "the compile item is Exists-guarded so deleting the file keeps the build green"
              // it sits inside the game/sample-pack #if region and before Model.fs
              let idx (s: string) = productFsproj.IndexOf(s, System.StringComparison.Ordinal)
              Expect.isLessThan (idx "Collision.fs") (idx "Compile Include=\"Model.fs\"") "Collision.fs compiles before Model.fs (usable from update)"
              Expect.isGreaterThan (idx "Collision.fs") (idx "profile == \"game\" || profile == \"sample-pack\"") "Collision.fs is inside the sim-profile gate"
          }

          test "both template.json sources (skill + fragment) are gated to game/sample-pack" {
              Expect.equal (sourceCondition "template/product-skills/fs-gg-collision/") (Some simGate) "skill source gated to sim profiles"
              Expect.equal (sourceCondition "template/fragments/collision/src/Product/") (Some simGate) "fragment source gated to sim profiles"
          }

          // ---- US3: game-core points at the new skill (single source of truth) -------------------
          test "fs-gg-game-core's Collision section is a pointer, not a duplicated write-up" {
              Expect.stringContains gameCoreBody "[[fs-gg-collision]]" "game-core points at the dedicated skill"
              // the old detailed narrow-phase list must no longer live here
              Expect.isFalse
                  (gameCoreBody.Contains("`Geometry.intersects a b` — box-vs-box overlap on positive area", System.StringComparison.Ordinal))
                  "the detailed detection write-up moved to fs-gg-collision"
          }
        ]
