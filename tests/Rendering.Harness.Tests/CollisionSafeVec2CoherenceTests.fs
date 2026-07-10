module CollisionSafeVec2CoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only collision-safe Vec2 wiring gate.
//
// WHY THIS EXISTS. Feature 250 (#138) ships the collision-safe `Vec2` fragment for the game /
// sample-pack profiles. Its load-bearing invariant is a NAMING one: `Vec2`'s record labels must reuse
// none of `Scene.Point` (X,Y) / `Scene.Rect` (X,Y,Width,Height), and the game branch of the base
// `Model.fs` must declare none of those labels either — otherwise a shadowed `X`/`Y` silently makes a
// collision test compare the wrong coordinate. `Feature250CollisionSafeVec2Tests` (tests/Package.Tests)
// is the gate that keeps that invariant + the fragment's delivery wiring (Exists-guarded, gated in
// Product.fsproj before Model.fs, sourced in template.json) honest. But Package.Tests is RELEASE-ONLY:
// it is not in `FS.GG.Rendering.slnx` and runs only under `dotnet test … -c Release` in release.yml. So
// a PR that renames a Vec2 label into `X`/`Y`, drops the Exists-guard, or reorders the fragment after
// Model.fs compiles green and only reds the release lane post-merge — the exact "PR-gated drift gates
// must be in the slnx" gap #350, #382, the BOM slice, the api-surface-mirror slice and the
// materializes-when slice already closed.
//
// WHAT IT LOCKS. `Feature250CollisionSafeVec2Tests` is already fully static and self-contained: no pack,
// no restore, no `dotnet new`, no GL — it reads the fragment source, the base Model.fs, Product.fsproj
// and template.json as files on disk and scans them. So this hoist is a FAITHFUL mirror: it reads the
// SAME four source-of-truth inputs and asserts the SAME invariants, one gate earlier —
//   the load-bearing invariant — Vec2's record labels reuse none of Scene.Point / Scene.Rect labels,
//                                and the game branch of Model.fs declares none of them either.
//   fragment surface           — Vec2.fs exposes the intended module surface and opens FS.GG.UI.Scene.
//   delivery wiring            — Product.fsproj compiles Vec2.fs Exists-guarded before Model.fs, and
//                                template.json sources the vec2 fragment.
//
// The body below is a BYTE-FAITHFUL hoist of the release-only counterpart (module name, `[<Tests>]`
// binding name and testList label aside); keeping it byte-faithful is what makes the L-INPUTS lockstep
// check (ReleaseOnlyTwinLockstepTests) exact — both sides read the same four inputs via the path helper.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private vec2Source = File.ReadAllText(repositoryPath "template/fragments/vec2/src/Product/Vec2.fs")
let private modelSource = File.ReadAllText(repositoryPath "template/base/src/Product/Model.fs")
let private fsproj = File.ReadAllText(repositoryPath "template/base/src/Product/Product.fsproj")
let private templateJson = File.ReadAllText(repositoryPath ".template.config/template.json")

let private sceneLabels = set [ "X"; "Y"; "Width"; "Height" ]

// Extract the field labels of every `{ ... }` record type declared in a source string (labels are
// `Name:` at a brace-record position). Conservative: matches `Ident :` inside record-type braces.
let private recordLabelsIn (source: string) =
    Regex.Matches(source, @"\{[^{}]*\}")
    |> Seq.collect (fun m -> Regex.Matches(m.Value, @"([A-Za-z_][A-Za-z0-9_]*)\s*:"))
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Set.ofSeq

// The game branch of Model.fs lives between `//#if (profile == "game")` and the next `//#else`.
let private gameModelBranch =
    let s = modelSource.IndexOf("//#if (profile == \"game\")")
    let e = if s >= 0 then modelSource.IndexOf("//#else", s) else -1
    if s >= 0 && e > s then modelSource.Substring(s, e - s) else ""

[<Tests>]
let collisionSafeVec2CoherenceTests =
    testList
        "#366 — collision-safe Vec2 coherence (PR-time twin of the release-only Feature250CollisionSafeVec2Tests gate)"
        [

          // --- the load-bearing invariant (SC-002 / FR-001) --------------------------------------
          test "Vec2's record labels reuse none of Scene.Point / Scene.Rect (X/Y/Width/Height)" {
              let labels = recordLabelsIn vec2Source
              Expect.isNonEmpty (Set.toList labels) "Vec2.fs declares a record type"
              Expect.isTrue (labels.Contains "Vx" && labels.Contains "Vy") "Vec2 uses Vx/Vy"
              Expect.isEmpty (Set.intersect labels sceneLabels |> Set.toList) "zero overlap with Point/Rect labels"
          }

          test "the game branch of the base Model.fs declares no X/Y/Width/Height record labels" {
              Expect.isTrue (gameModelBranch.Length > 0) "found the game branch of Model.fs"
              let labels = recordLabelsIn gameModelBranch
              Expect.isEmpty (Set.intersect labels sceneLabels |> Set.toList) "game model never reuses Scene labels"
          }

          // --- fragment surface (C1) --------------------------------------------------------------
          test "Vec2.fs exposes the intended module surface" {
              for token in [ "module Geometry"; "type Vec2 ="; "Vx: float"; "Vy: float"
                             "let vec2"; "let zero"; "let add"; "let sub"; "let scale"
                             "let clamp"; "let toPoint"; "let toRect" ] do
                  Expect.stringContains vec2Source token $"Vec2.fs contains '{token}'"
          }

          test "Vec2.fs opens FS.GG.UI.Scene (Point/Rect) and no other package" {
              Expect.stringContains vec2Source "open FS.GG.UI.Scene" "reuses the shared scene vocabulary"
          }

          // --- delivery wiring (delete-safe, gated, before Model.fs) ------------------------------
          test "Product.fsproj compiles Vec2.fs Exists-guarded, before Model.fs" {
              Expect.stringContains fsproj "<Compile Include=\"Vec2.fs\" Condition=\"Exists('Vec2.fs')\" />" "delete-safe gated item"
              let vec2Idx = fsproj.IndexOf("Include=\"Vec2.fs\"")
              let modelIdx = fsproj.IndexOf("Include=\"Model.fs\"")
              Expect.isTrue (vec2Idx >= 0 && modelIdx > vec2Idx) "Vec2.fs compiles before Model.fs"
          }

          test "template.json sources the vec2 fragment gated to game/sample-pack" {
              Expect.stringContains templateJson "template/fragments/vec2/src/" "vec2 fragment is a template source"
          } ]
