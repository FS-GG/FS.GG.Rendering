module VisibilitySkillCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only fs-gg-visibility skill-wiring gate.
//
// WHY THIS EXISTS. The shipped `template/product-skills/fs-gg-visibility/SKILL.md` promises an
// angular-sweep visibility layer (line-of-sight / field-of-view / fog-of-war / 2D lighting) delivered
// as an adaptable `Visibility.fs` fragment, gated to the game/sample-pack profiles, reusing the shared
// `Point`, citing Red Blob Games, and teaching an EXACT segment-vs-box cull (#261) rather than the
// endpoint-bucketing that silently drops a spanning wall. `Feature247VisibilitySkillTests`
// (tests/Package.Tests) enforces that wiring, but Package.Tests is RELEASE-ONLY: it is not in
// `FS.GG.Rendering.slnx` and runs only under `dotnet test … -c Release` in release.yml. So a SKILL.md
// edit that renames the helper, a fragment that drops a promised member or reaches back into the
// point-keyed grid it warns against, an un-gated template.json source, or swap-guidance that stops
// listing the helper all compile green on a PR and only red the release lane. That is the "PR-gated
// tests must be in the slnx" gap #350/#382 already closed for the launch host and the audio wiring,
// and #388/#397/#401/#402/#405/#408 have been closing rule-by-rule for the other twins.
//
// WHAT IT LOCKS. Feature247VisibilitySkillTests is already fully static and self-contained (no pack, no
// restore, no assembly load): it re-derives everything from the shipped skill body, the fragment
// source, Product.fsproj, the model-swap skill body, the scaffold-map, template.json and the
// skill-manifest. So this hoist is a faithful mirror — it reads the SAME seven source-of-truth inputs
// STATICALLY and asserts the SAME invariants, one gate earlier:
//   US2  the skill declares its id, reuses Point, cites the reference, teaches the exact segment-vs-box
//        cull (#261), and materializes only for the sim profiles in the manifest.
//   US1  the adaptable `Visibility.fs` fragment exists with its intended surface, does not bucket
//        endpoints, and is compiled profile-gated + Exists-guarded (delete-safe) before Model.fs; both
//        template.json sources gated.
//   US3  the model-swap and scaffold-map surfaces classify `Visibility.fs` as consumer-owned
//        replaceable source.
//
// Kept in deliberate lockstep with tests/Package.Tests/Feature247VisibilitySkillTests.fs: the two read
// the same repo inputs and assert the same parity, so a real drift fails BOTH — the release check is
// mirrored earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing (L-INPUTS) so the
// mirror cannot silently desync.

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
let visibilitySkillCoherenceTests =
    testList
        "#366 twin — fs-gg-visibility skill surface"
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
