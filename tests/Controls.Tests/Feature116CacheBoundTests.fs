module Feature116CacheBoundTests

// Feature 116 (US3, FR-009/FR-010, SC-004) — the picture cache is bounded by entry count with
// deterministic LRU eviction, reached through `[<assembly: InternalsVisibleTo("Controls.Tests")>]`
// over the REAL wired `RetainedRender.step` path. Driving more distinct cacheable rows than the cap
// keeps `PictureCacheEntryCount <= PictureCacheCap` at all times; eviction is deterministic (same
// input → same surviving entries); an evicted entry recomputes as a MISS with fresh, correct paint
// (never a stale hit). Render-only / deterministic — no live Vulkan window ([[fs-skia-evidence-mode]]).

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private rinit (t: Theme) (s: Size) (c: Control<'msg>) : RetainedRender<'msg> =
    (RetainedRender.init t s c).Retained

let private row (key: string) (content: string) : Control<int> =
    { Kind = "data-grid-row"
      Key = Some key
      Attributes =
        [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 200.0 }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 } ]
      Children = []
      Content = Some content
      Accessibility = None }

// A grid of `n` DISTINCT cacheable rows (distinct key + distinct content → distinct correctness key).
let private gridOf (n: int) : Control<int> =
    { Kind = "stack"
      Key = None
      Attributes = []
      Children = [ for i in 0 .. n - 1 -> row (sprintf "r%d" i) (sprintf "row-%d" i) ]
      Content = None
      Accessibility = None }

// The eviction-pressure scenario: 320 distinct rows = 1.25 × the 256 cap (forces >= 64 evictions).
let private overCapCount = 320

[<Tests>]
let tests =
    testList "Feature 116 bounded picture cache (US3, FR-009/010, SC-004)" [

        test "under the cap: a small grid never evicts and reuses every row (FR-009)" {
            let small = gridOf 10
            let r0 = rinit theme size small
            let s = RetainedRender.step theme size r0 small

            Expect.equal s.WorkReduction.PictureCacheEntryCount 10 "10 distinct rows → 10 live entries, no eviction"
            Expect.equal s.WorkReduction.PictureCacheHits 10 "every stable row hits below the cap"
        }

        test "over the cap: PictureCacheEntryCount <= cap at all times (FR-009, SC-004)" {
            let big = gridOf overCapCount
            let r0 = rinit theme size big
            let s = RetainedRender.step theme size r0 big

            Expect.isTrue (overCapCount > RetainedRender.PictureCacheCap) "scenario drives more rows than the cap"
            Expect.isTrue
                (s.WorkReduction.PictureCacheEntryCount <= RetainedRender.PictureCacheCap)
                (sprintf "entry count %d is bounded by the cap %d" s.WorkReduction.PictureCacheEntryCount RetainedRender.PictureCacheCap)
            Expect.equal s.WorkReduction.PictureCacheEntryCount RetainedRender.PictureCacheCap "the cache is full at the cap under pressure"
        }

        test "eviction is deterministic: the same input yields the same surviving entries (FR-010, SC-004)" {
            let big = gridOf overCapCount

            let surviving () =
                let r0 = rinit theme size big
                let s = RetainedRender.step theme size r0 big
                s.Retained.PictureCache.Entries |> Map.toList |> List.map fst |> List.sort

            Expect.equal (surviving ()) (surviving ()) "the surviving-entry identity set is identical across runs"
        }

        test "an evicted entry re-misses with fresh correct paint, never a stale hit (FR-010, SC-004)" {
            let big = gridOf overCapCount
            let r0 = rinit theme size big
            let s = RetainedRender.step theme size r0 big
            let full = Control.renderTree theme size big

            // some rows were evicted under pressure, so they re-miss (recompute) rather than hit a stale
            // picture; the re-missed (recomputed) scene stays byte-identical to a fresh full rebuild.
            Expect.isTrue (s.WorkReduction.PictureCacheMisses > 0) "eviction pressure forces re-misses"
            Expect.equal s.Render.Scene full.Scene "the recomputed scene is byte-identical to a fresh paint (never stale)"
        }
    ]
