module Audit_PictureCache

// AUDIT (feature 006, T004 sanity + T020 + T031) — the picture cache (`PictureCacheEnabled` oracle +
// `WorkReductionRecord.PictureCacheHits/PictureCacheMisses`) over the wired `RetainedRender.step` path.
//   * PARITY (FR-004): cache-on ≡ cache-off byte-identical (paint-order, transparent to the feature-120
//     replay boundary), with a DISCRIMINATING proof that the byte-identity oracle catches a real
//     divergence (a genuinely different scene is NOT equal).
//   * PRESENT-BUT-DEAD (FR-010, D5): drive a representative repeated scene and assert
//     `PictureCacheHits` PROVABLY MOVES (>0). If it never moves on any representative scene, that is a
//     FINDING (reported, never faked).
//   * EFFECTIVENESS (FR-008, T031): `PictureCacheHits` reaches a steady-state ≫0 while misses→0 across
//     repeated frames; the margin is recorded.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private rinit (t: Theme) (s: Size) (c: Control<'msg>) : RetainedRender<'msg> = (RetainedRender.init t s c).Retained

let private row (key: string) (content: string) (width: float) (state: VisualState) : Control<int> =
    { Kind = "data-grid-row"
      Key = Some key
      Attributes =
        [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue width }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 }
          Attr.visualState state ]
      Children = []
      Content = Some content
      Accessibility = None }

let private plainRow (key: string) (content: string) = row key content 200.0 Normal
let private grid (rows: Control<int> list) : Control<int> =
    { Kind = "stack"; Key = None; Attributes = []; Children = rows; Content = None; Accessibility = None }

let private threeRows = grid [ plainRow "r0" "zero"; plainRow "r1" "one"; plainRow "r2" "two" ]

// Flatten the paint-order leaf stream, normalizing transparent grouping (Group / CachedSubtree).
let rec private flattenScene (s: Scene) : SceneNode list = s.Nodes |> List.collect flattenNode
and private flattenNode (n: SceneNode) : SceneNode list =
    match n with
    | CachedSubtree b -> flattenScene b.Scene
    | Group scenes -> scenes |> List.collect flattenScene
    | ClipNode(c, s) -> [ ClipNode(c, { Nodes = flattenScene s }) ]
    | ColorSpaceNode(c, s) -> [ ColorSpaceNode(c, { Nodes = flattenScene s }) ]
    | PerspectiveNode(t, s) -> [ PerspectiveNode(t, { Nodes = flattenScene s }) ]
    | Translate(o, s) -> [ Translate(o, { Nodes = flattenScene s }) ]
    | PictureNode p -> [ PictureNode { p with Scene = { Nodes = flattenScene p.Scene } } ]
    | other -> [ other ]

let private flat (r: ControlRenderResult<int>) = flattenScene r.Scene

[<Tests>]
let tests =
    testList "Audit: Picture cache parity + present-but-dead + effectiveness (FR-004/008/010, D5)" [

        // ---- T004 scaffold sanity ----
        test "Audit: PictureCache scaffold reachability — PictureCacheEnabled + counters (T004)" {
            let disabled = { rinit theme size threeRows with PictureCacheEnabled = false }
            Expect.isFalse disabled.PictureCacheEnabled "PictureCacheEnabled oracle reachable + settable"
            let s = RetainedRender.step theme size (rinit theme size threeRows) threeRows
            Expect.isTrue (s.WorkReduction.PictureCacheHits >= 0 && s.WorkReduction.PictureCacheMisses >= 0 && s.WorkReduction.PictureCacheEntryCount >= 0) "picture-cache counters reachable"
        }

        // ---- PRESENT-BUT-DEAD: the hit counter must provably move ----
        test "Audit: PRESENT-BUT-DEAD — PictureCacheHits provably MOVES on a representative repeated scene (FR-010, D5)" {
            let r0 = rinit theme size threeRows
            let s = RetainedRender.step theme size r0 threeRows
            Expect.isTrue (s.WorkReduction.PictureCacheHits > 0)
                "FINDING-GATE: the picture cache is NOT dead — a stable 3-row frame produces >0 hits"
            Expect.equal s.WorkReduction.PictureCacheHits 3 "all three stable rows hit"
            Expect.equal s.WorkReduction.PictureCacheMisses 0 "a stable frame recomputes no picture"
        }

        // ---- T020 PARITY with DISCRIMINATING proof ----
        test "Audit: cache-on ≡ cache-off byte-identical, with a discriminating divergence check (FR-004)" {
            let enabled = rinit theme size threeRows
            let disabled = { rinit theme size threeRows with PictureCacheEnabled = false }
            let on = RetainedRender.step theme size enabled threeRows
            let off = RetainedRender.step theme size disabled threeRows

            Expect.equal off.WorkReduction.PictureCacheHits 0 "the disabled oracle reports zero hits"
            Expect.isTrue (off.WorkReduction.PictureCacheMisses > 0) "the disabled oracle re-misses every picture"
            Expect.equal (flat off.Render) (flat on.Render) "cache-off scene is byte-identical (paint-order) to cache-on"

            // DISCRIMINATING: the byte-identity oracle is NOT vacuous — a genuinely different scene
            // (one changed row's content) is NOT equal to the cache-on scene. So the parity assertion
            // above would go RED on a real divergence.
            let differentScene = RetainedRender.step theme size (rinit theme size threeRows) (grid [ plainRow "r0" "zero"; plainRow "r1" "CHANGED"; plainRow "r2" "two" ])
            Expect.notEqual (flat differentScene.Render) (flat on.Render) "a genuinely different scene is caught by the byte-identity oracle (discriminating)"
        }

        // ---- T031 EFFECTIVENESS: steady-state hits ≫ 0, misses → 0 ----
        test "Audit: EFFECTIVENESS — PictureCacheHits reach steady-state ≫0 while misses→0 across repeated frames (T031)" {
            let frameCount = 30
            let r0 = rinit theme size threeRows
            let _, totalHits, totalMisses, lastHits, lastMisses =
                [ 1 .. frameCount ]
                |> List.fold (fun (prev, h, m, _, _) _ ->
                    let s = RetainedRender.step theme size prev threeRows
                    s.Retained, h + s.WorkReduction.PictureCacheHits, m + s.WorkReduction.PictureCacheMisses, s.WorkReduction.PictureCacheHits, s.WorkReduction.PictureCacheMisses)
                    (r0, 0, 0, 0, 0)

            Expect.equal lastMisses 0 "steady-state: a repeated stable frame misses no picture"
            Expect.equal lastHits 3 "steady-state: all 3 rows hit every frame"
            Expect.isTrue (totalHits > 0) "the picture cache provably accumulates hits across frames"

            // disabled baseline: zero hits across the same drive.
            let _, offHits =
                [ 1 .. frameCount ]
                |> List.fold (fun (prev, h) _ -> let s = RetainedRender.step theme size prev threeRows in s.Retained, h + s.WorkReduction.PictureCacheHits) ({ r0 with PictureCacheEnabled = false }, 0)
            Expect.equal offHits 0 "the disabled baseline accumulates zero hits"
            printfn "AUDIT-MARGIN PictureCache: enabled hits=%d/%d frames (steady %d/3 per frame) misses_total=%d | disabled hits=%d" totalHits frameCount lastHits totalMisses offHits
        }
    ]
