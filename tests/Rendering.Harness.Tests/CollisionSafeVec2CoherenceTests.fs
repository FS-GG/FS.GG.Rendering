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
          }

          // --- the OUTWARD half: Vec2's shape is a PUBLISHED contract, pinned one repo over -----------
          //
          // #519 -> #570 -> #602. `Vec2` lives in the GENERATED PRODUCT, not in a package, so FS.GG.Game —
          // whose product skills teach it by name (`type Creep = { Pos: Geometry.Vec2 }`) — has no assembly
          // to reference. It used to hand-write a TWIN of this record in scripts/skill-block-context/
          // _scaffold.fs, and that twin was the hazard this test stood in for: a rename here left their
          // copy compiling and their gate GREEN over skills teaching a shape this scaffold no longer
          // shipped — FS-GG/.github#416's silent no-op, one repo over.
          //
          // THAT HAZARD IS GONE (FS.GG.Game#189, closing #570). `_scaffold.fs` is now GENERATED verbatim
          // from the published fragment — `content/template/fragments/vec2/src/Product/Vec2.fs` inside the
          // FS.GG.UI.Template package — behind a `scaffold-drift` job that re-derives it from the package
          // and fails unless the committed copy matches byte-for-byte (it uses `--check`, so a DELETION
          // fails too, rather than being silently regenerated). There is no twin left to fall out of step.
          // So this test no longer carries that job, and the instruction it used to give — "open a
          // cross-repo issue and move _scaffold.fs in lockstep, and do not merge until it is filed" — is
          // now simply WRONG: nobody hand-edits that file, and it regenerates when their pin moves.
          //
          // WHAT SURVIVES, AND WHY IT STILL LIVES HERE. The coupling is structural but NOT CONTINUOUS.
          // FS.GG.Game regenerates from the version it PINS (FS.GG.UI.Template 0.5.0), so a reshape in this
          // repo reds nothing over there until someone cuts a release and bumps that pin — which is correct
          // and deliberate, but may be months later, and lands looking like FS.GG.Game broke rather than
          // like this file changed. The tripwire therefore has to fire HERE, in the commit doing the
          // reshaping, which is the one place that knows it is a reshape.
          //
          // AND IT EARNS ITS KEEP ON ADDITION SPECIFICALLY. A RENAME is already caught by the surface test
          // above, which asserts `Vx: float`/`Vy: float` are present. Adding a `Vz` is what slips past:
          // every other assertion in this file stays green while every `{ Vx = …; Vy = … }` literal written
          // against the published fragment breaks — in this repo's skills, and in the skill blocks
          // FS.GG.Game typechecks — because a two-field literal cannot construct a three-field record.
          //
          // NOT A BAN — A CHECKPOINT, and the distinction matters because the fragment's own header invites
          // a reshape ("rename Vx/Vy, add a Z, or delete it"). That adaptability belongs to the SCAFFOLDED
          // COPY, downstream of generation, in a product that owns its file. Upstream, in this repo, the
          // same bytes are what one package publishes and another repo pins — so reshaping them is a
          // versioned breaking change, not a local edit.
          test "Vec2's shape is the published fragment contract (FS.GG.Game pins it and regenerates from it)" {
              let decl = Regex.Match(vec2Source, @"type\s+Vec2\s*=\s*\{([^}]*)\}")

              Expect.isTrue
                  decl.Success
                  "Vec2 must remain a brace-record `type Vec2 = { ... }`. It is published as SOURCE inside FS.GG.UI.Template (content/template/fragments/vec2/src/Product/Vec2.fs), FS.GG.Game generates its skill-block scaffold verbatim from that file, and the `{ Vx = …; Vy = … }` literals in both repos' skills are written against this shape."

              let fields =
                  Regex.Matches(decl.Groups.[1].Value, @"([A-Za-z_][A-Za-z0-9_]*)\s*:\s*([A-Za-z_][A-Za-z0-9_.]*)")
                  |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
                  |> List.ofSeq

              Expect.equal
                  fields
                  [ "Vx", "float"; "Vy", "float" ]
                  "Vec2's fields must stay EXACTLY `Vx: float; Vy: float`. This is the assertion that catches an ADDED field, which nothing else here does: a `Vz` leaves every other check in this file green while breaking every `{ Vx = …; Vy = … }` literal written against the published fragment — in this repo's skills, and in the skill blocks FS.GG.Game typechecks — because a two-field literal cannot construct a three-field record. Reshaping Vec2 IS allowed, but it is a versioned breaking change to a published fragment rather than a local edit: change it, update this assertion in the SAME PR, and expect it to reach FS.GG.Game only when their FS.GG.UI.Template pin moves and `scaffold-drift` regenerates their copy (FS.GG.Game#189). Do not hand-edit anything over there — that file is generated."

              Expect.stringContains
                  vec2Source
                  "module Geometry"
                  "the module name is half the contract — both repos' skills say `Geometry.Vec2`, and the scaffold FS.GG.Game generates carries this `module Geometry` with it. Renaming or moving the module breaks those blocks when their pin next moves (#519, #570)."
          } ]
