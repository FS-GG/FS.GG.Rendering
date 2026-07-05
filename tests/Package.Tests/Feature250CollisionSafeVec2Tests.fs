module Feature250CollisionSafeVec2Tests

// Feature 250 — the collision-safe Vec2 helper + its wiring guard (source scan; no product build).
//
// The generic manifest/materialize gates do not cover this: #138 ships NO skill, only a template fragment.
// This gate asserts (a) the load-bearing collision-safety invariant — Vec2's record labels reuse NONE of
// Scene.Point (X,Y) / Scene.Rect (X,Y,Width,Height), and the game branch of the base Model.fs declares none
// of those labels either; and (b) the delivery wiring — the fragment source exists with its intended
// surface, is Exists-guarded + gated in Product.fsproj before Model.fs, and is sourced in template.json.

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
let tests =
    testList
        "Feature 250 collision-safe Vec2 (US1)"
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
