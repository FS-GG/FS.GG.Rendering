module Feature120ReplayCacheTests

// Feature 120 (US3, FR-007/FR-011/FR-013) — the backend `PictureReplayCache`: a valid hit replays the
// recorded picture (the direct walk is skipped), a changed fingerprint / eviction re-records, the LRU
// bound is never exceeded, native pictures are released on eviction/replacement, and the disabled oracle
// never records or replays (cache-on ≡ cache-off direct walk). Plus the pure idle-skip decision
// (`GlHost.shouldPresent`, US2). Reached over the internal surface via InternalsVisibleTo. Raster
// surfaces (no GL window required).

open Expecto
open SkiaSharp
open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.SkiaViewer.Host

let private blue: Color = { Red = 0uy; Green = 0uy; Blue = 255uy; Alpha = 255uy }

let private boundary (cacheId: uint64) (fp: uint64) : CacheBoundary =
    { CacheId = cacheId
      Fingerprint = fp
      Scene = { Nodes = [ Rectangle((0.0, 0.0, 8.0, 8.0), blue) ] } }

// A counting paint walk — records how many times the cache fell through to the direct walk (record path).
let private countingPaint (counter: int ref) : SKCanvas -> Scene -> unit =
    fun canvas scene ->
        counter.Value <- counter.Value + 1
        scene.Nodes |> List.iter (SceneRenderer.paintNode canvas)

let private withCanvas (f: SKCanvas -> unit) =
    use surface = SKSurface.Create(SKImageInfo(64, 64))
    f surface.Canvas

[<Tests>]
let tests =
    testList "Feature 120 backend replay cache (US3, FR-007/011/013)" [

        test "a matching fingerprint is a HIT: the picture is replayed, the direct walk is skipped (FR-007)" {
            withCanvas (fun canvas ->
                let cache = PictureReplayCache.create true
                let walks = ref 0
                let paint = countingPaint walks
                let b = boundary 0UL 100UL

                PictureReplayCache.paintBoundary cache canvas paint b // miss → record (1 walk)
                PictureReplayCache.paintBoundary cache canvas paint b // hit → replay (no walk)
                PictureReplayCache.paintBoundary cache canvas paint b // hit → replay (no walk)

                let s = PictureReplayCache.stats cache
                Expect.equal walks.Value 1 "the subtree is walked once (recorded); the two hits replay"
                Expect.equal s.Hits 2 "two replay hits"
                Expect.equal s.Misses 1 "one cold miss"
                Expect.equal s.Records 1 "one record")
        }

        test "a changed fingerprint re-records, never a stale hit (FR-010/FR-013)" {
            withCanvas (fun canvas ->
                let cache = PictureReplayCache.create true
                let walks = ref 0
                let paint = countingPaint walks

                PictureReplayCache.paintBoundary cache canvas paint (boundary 0UL 100UL) // record
                PictureReplayCache.paintBoundary cache canvas paint (boundary 0UL 200UL) // changed fp → re-record

                let s = PictureReplayCache.stats cache
                Expect.equal walks.Value 2 "the changed fingerprint forces a re-record (second walk)"
                Expect.equal s.Hits 0 "no stale hit across the changed fingerprint"
                Expect.equal s.Records 2 "two records"
                Expect.equal s.Entries 1 "the replaced picture does not accumulate a second entry")
        }

        test "the LRU bound is never exceeded under eviction pressure (FR-013)" {
            withCanvas (fun canvas ->
                let cache = PictureReplayCache.create true
                let paint = countingPaint (ref 0)
                // drive 1.5x the cap distinct identities
                for i in 0 .. (PictureReplayCache.cap + PictureReplayCache.cap / 2) do
                    PictureReplayCache.paintBoundary cache canvas paint (boundary (uint64 i) (uint64 i))

                let s = PictureReplayCache.stats cache
                Expect.isTrue (s.Entries <= PictureReplayCache.cap) "entry count never exceeds the cap"
                Expect.isTrue (s.NativeBytes >= 0) "native bytes are observable and bounded")
        }

        test "the disabled oracle never records or replays — cache-on ≡ cache-off direct walk (FR-011)" {
            withCanvas (fun canvas ->
                let cache = PictureReplayCache.create false
                let walks = ref 0
                let paint = countingPaint walks
                let b = boundary 0UL 100UL

                PictureReplayCache.paintBoundary cache canvas paint b
                PictureReplayCache.paintBoundary cache canvas paint b

                let s = PictureReplayCache.stats cache
                Expect.equal walks.Value 2 "every paint recurses directly (no replay)"
                Expect.equal s.Hits 0 "the oracle never hits"
                Expect.equal s.Records 0 "the oracle never records"
                Expect.equal s.Entries 0 "the oracle holds no native pictures")
        }

        test "dispose releases all resident pictures (teardown, FR-013)" {
            withCanvas (fun canvas ->
                let cache = PictureReplayCache.create true
                let paint = countingPaint (ref 0)
                for i in 0 .. 4 do
                    PictureReplayCache.paintBoundary cache canvas paint (boundary (uint64 i) (uint64 i))

                PictureReplayCache.dispose cache
                Expect.equal (PictureReplayCache.stats cache).Entries 0 "dispose clears all entries")
        }

        test "cache-on ≡ cache-off pixel readback parity: a replayed boundary is byte-identical to the direct walk (SC-003/FR-009/FR-011)" {
            // A scene with three CachedSubtree boundaries over distinct content.
            let boundaryScene cacheId (yOff: float) : SceneNode =
                CachedSubtree
                    { CacheId = cacheId
                      Fingerprint = cacheId * 7UL + 1UL
                      Scene =
                        { Nodes =
                            [ Rectangle((4.0, yOff, 40.0, 14.0), { Red = 30uy; Green = 144uy; Blue = 255uy; Alpha = 255uy })
                              SceneNode.Text((6.0, yOff + 11.0), "row", { Red = 240uy; Green = 240uy; Blue = 245uy; Alpha = 255uy }) ] } }

            let scene: Scene = { Nodes = [ boundaryScene 0UL 2.0; boundaryScene 1UL 20.0; boundaryScene 2UL 38.0 ] }

            let renderWith (cache: PictureReplayCache.Cache option) (frames: int) : byte[] =
                use surface = SKSurface.Create(SKImageInfo(64, 64))
                SceneRenderer.activeReplayCache <- cache
                for _ in 1 .. frames do
                    surface.Canvas.Clear(SKColors.Black)
                    scene.Nodes |> List.iter (SceneRenderer.paintNode surface.Canvas)
                    surface.Canvas.Flush()
                SceneRenderer.activeReplayCache <- None
                use img = surface.Snapshot()
                use data = img.Encode(SKEncodedImageFormat.Png, 100)
                data.ToArray()

            let direct = renderWith None 1 // cache-off direct walk
            let oracleOff = renderWith (Some(PictureReplayCache.create false)) 1 // disabled oracle
            let replayWarm = renderWith (Some(PictureReplayCache.create true)) 3 // warm: frame 1 records, 2/3 replay

            Expect.equal oracleOff direct "the disabled oracle is byte-identical to the direct walk (FR-011)"
            Expect.equal replayWarm direct "a warmed replayed boundary is byte-identical to the direct walk (SC-003/FR-009)"
        }

        test "idle-skip decision: present iff first frame, scene changed, or size changed (US2, FR-004/005/006)" {
            let sceneA: Scene = { Nodes = [ Rectangle((0.0, 0.0, 8.0, 8.0), blue) ] }
            let sceneB: Scene = { Nodes = [ Rectangle((0.0, 0.0, 9.0, 8.0), blue) ] }

            Expect.isTrue (GlHost.shouldPresent None sceneA false) "first frame always presents"
            Expect.isFalse (GlHost.shouldPresent (Some sceneA) sceneA false) "unchanged scene + unchanged size ⇒ idle skip"
            Expect.isTrue (GlHost.shouldPresent (Some sceneA) sceneB false) "a changed scene presents"
            Expect.isTrue (GlHost.shouldPresent (Some sceneA) sceneA true) "a size change forces a present even if the scene is unchanged"
        }
    ]
