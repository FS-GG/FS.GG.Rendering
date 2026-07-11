module CollisionSkillCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only fs-gg-collision skill-wiring gate.
//
// WHY THIS EXISTS. The shipped `template/product-skills/fs-gg-collision/SKILL.md` promises a
// broad-phase-over-SpatialGrid / narrow-phase-over-Geometry collision layer delivered as an adaptable
// `Collision.fs` fragment, gated to the game/sample-pack profiles, with fs-gg-game-core trimmed to a
// single-source-of-truth pointer at it. `Feature246CollisionSkillTests` (tests/Package.Tests) enforces
// that wiring, but Package.Tests is RELEASE-ONLY: it is not in `FS.GG.Rendering.slnx` and runs only
// under `dotnet test … -c Release` in release.yml. So a SKILL.md edit that renames the helper, a
// fragment that drops a promised member, an un-gated template.json source, or a game-core section that
// re-grows the duplicated write-up all compile green on a PR and only red the release lane. That is the
// "PR-gated tests must be in the slnx" gap #350/#382 already closed for the launch host and the audio
// wiring, and #388/#397/#401/#402/#405 have been closing rule-by-rule for the other twins.
//
// WHAT IT LOCKS. Feature246CollisionSkillTests is already fully static and self-contained (no pack, no
// restore, no assembly load): it re-derives everything from the shipped skill body, the fragment
// source, Product.fsproj, the game-core skill body, template.json and the skill-manifest. So this hoist
// is a faithful mirror — it reads the SAME six source-of-truth inputs STATICALLY and asserts the SAME
// invariants, one gate earlier:
//   US2  the skill declares its id and reuses Geometry + SpatialGrid, and materializes only for the
//        sim profiles in the manifest.
//   US1  the adaptable `Collision.fs` fragment exists with its intended surface and is compiled
//        profile-gated + Exists-guarded (delete-safe) before Model.fs; both template.json sources gated.
//   US3  fs-gg-game-core points at `[[fs-gg-collision]]` rather than duplicating the detection write-up.
//
// Kept in deliberate lockstep with tests/Package.Tests/Feature246CollisionSkillTests.fs: the two read
// the same repo inputs and assert the same parity, so a real drift fails BOTH — the release check is
// mirrored earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing (L-INPUTS) so the
// mirror cannot silently desync.

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
let collisionSkillCoherenceTests =
    testList
        "#366 twin — fs-gg-collision skill surface"
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
              Expect.stringContains src "namespace AppRoot" "literal AppRoot identifier namespace (the product-name identifier token, derived to the product namespace on scaffold)"
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
              Expect.equal (sourceCondition "template/fragments/collision/src/") (Some simGate) "fragment source gated to sim profiles"
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
