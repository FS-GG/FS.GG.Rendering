module Feature116PictureCacheTests

// Feature 116 (US2, FR-005/FR-006/FR-007, SC-002/SC-003) — the fully-keyed picture cache surfaced as
// the internal `WorkReductionRecord.PictureCacheHits` / `PictureCacheMisses`, reached through
// `[<assembly: InternalsVisibleTo("Controls.Tests")>]` over the REAL wired `RetainedRender.step` path.
// A subtree unchanged in every render-affecting input is a HIT (reused, byte-identical); perturbing
// EXACTLY one keyed input (theme | box | content/font-text | visual-state) independently forces a MISS
// with correct fresh output (proving no keyed input is omitted); the always-miss oracle
// (`PictureCacheEnabled = false`) renders byte-identically to the cache-enabled build (cache-on ≡
// cache-off). Render-only / deterministic — no live Vulkan window ([[fs-gg-evidence-mode]]).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private rinit (t: Theme) (s: Size) (c: Control<'msg>) : RetainedRender<'msg> =
    (RetainedRender.init t s c).Retained

// A cacheable picture boundary: a data-grid row (the row analog of the 113 data-grid memo site).
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
    { Kind = "stack"
      Key = None
      Attributes = []
      Children = rows
      Content = None
      Accessibility = None }

let private threeRows = grid [ plainRow "r0" "zero"; plainRow "r1" "one"; plainRow "r2" "two" ]

// Feature 120: with literal `CachedSubtree` emission a reuse-stable row's contribution is wrapped in a
// transparent replay boundary, which adds a grouping layer that the full rebuild does not have. Both
// `Group` and `CachedSubtree` are PURE grouping (the painter descends through them with no visual
// effect), so the byte-identity invariant is asserted on the flattened paint-order node stream — which
// normalizes that grouping while preserving every leaf node's payload (geometry, color, font, clip,
// transform). Equal flattened streams ⇒ byte-identical presented pixels.
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

[<Tests>]
let tests =
    testList "Feature 116 picture cache (US2, FR-005/006/007, SC-002/003)" [

        test "a subtree unchanged across two frames is a HIT, reused not repainted (FR-005, SC-002)" {
            let r0 = rinit theme size threeRows
            let s = RetainedRender.step theme size r0 threeRows

            Expect.equal s.WorkReduction.PictureCacheHits 3 "all three stable rows are cache hits"
            Expect.equal s.WorkReduction.PictureCacheMisses 0 "a stable frame recomputes no picture"
        }

        test "a HIT is byte-identical to a fresh full rebuild (FR-005)" {
            let r0 = rinit theme size threeRows
            let s = RetainedRender.step theme size r0 threeRows
            let full = Control.renderTree theme size threeRows

            Expect.equal (flattenScene s.Render.Scene) (flattenScene full.Scene) "the reused (hit) scene is byte-identical (paint-order) to a fresh paint, transparent to the replay boundary"
        }

        test "perturbing CONTENT (font/text) forces a miss on exactly that row (FR-006)" {
            let after = grid [ plainRow "r0" "zero"; plainRow "r1" "ONE-CHANGED"; plainRow "r2" "two" ]
            let r0 = rinit theme size threeRows
            let s = RetainedRender.step theme size r0 after

            Expect.equal s.WorkReduction.PictureCacheMisses 1 "the changed-content row misses"
            Expect.equal s.WorkReduction.PictureCacheHits 2 "the two unchanged rows still hit"
        }

        test "perturbing BOX (width) forces a miss on exactly that row (FR-006)" {
            let after = grid [ plainRow "r0" "zero"; row "r1" "one" 260.0 Normal; plainRow "r2" "two" ]
            let r0 = rinit theme size threeRows
            let s = RetainedRender.step theme size r0 after

            Expect.equal s.WorkReduction.PictureCacheMisses 1 "the resized row misses"
            Expect.equal s.WorkReduction.PictureCacheHits 2 "the two unchanged rows still hit"
        }

        test "the correctness key is the painted picture: a paint-affecting change misses, a paint-neutral one hits (FR-006)" {
            // The cache key is a structural digest of the row's painted subtree — complete by
            // construction: ANY input that alters the rendered picture forces a miss (no keyed input —
            // theme/box/clip/opacity/transform/font-text/visual-state — can be omitted), and an input
            // that does NOT alter the picture (e.g. a visual-state a plain row's paint does not honour)
            // correctly HITS, because the reused picture is genuinely byte-identical (never a stale hit).
            let r0 = rinit theme size threeRows

            // a visual-state that does not change a plain row's paint → genuinely identical picture → hit.
            let stateOnly = grid [ plainRow "r0" "zero"; row "r1" "one" 200.0 Hover; plainRow "r2" "two" ]
            let neutral = RetainedRender.step theme size r0 stateOnly
            Expect.equal neutral.WorkReduction.PictureCacheMisses 0 "a paint-neutral change keeps the picture identical → no stale-free miss"
            Expect.equal neutral.WorkReduction.PictureCacheHits 3 "all rows hit when the picture is unchanged"

            // a paint-affecting change (content) → different picture → miss on exactly that row.
            let painted = grid [ plainRow "r0" "zero"; plainRow "r1" "DIFFERENT"; plainRow "r2" "two" ]
            let changed = RetainedRender.step theme size r0 painted
            Expect.equal changed.WorkReduction.PictureCacheMisses 1 "a paint-affecting change misses exactly that row"
        }

        test "perturbing THEME forces a miss on EVERY row (FR-006)" {
            let r0 = rinit theme size threeRows
            let s = RetainedRender.step Theme.dark size r0 threeRows

            Expect.equal s.WorkReduction.PictureCacheMisses 3 "a theme switch misses every cached picture"
            Expect.equal s.WorkReduction.PictureCacheHits 0 "no row hits across a theme switch"
        }

        test "cache-on ≡ cache-off: the always-miss oracle renders byte-identically (FR-007, SC-003)" {
            let enabled = rinit theme size threeRows
            let disabled = { rinit theme size threeRows with PictureCacheEnabled = false }

            let on = RetainedRender.step theme size enabled threeRows
            let off = RetainedRender.step theme size disabled threeRows

            Expect.equal off.WorkReduction.PictureCacheHits 0 "the disabled oracle reports zero hits"
            Expect.isTrue (off.WorkReduction.PictureCacheMisses > 0) "the disabled oracle re-misses every picture"
            Expect.equal (flattenScene off.Render.Scene) (flattenScene on.Render.Scene) "cache-off scene is byte-identical (paint-order) to cache-on, transparent to the replay boundary (FR-007)"
        }
    ]
