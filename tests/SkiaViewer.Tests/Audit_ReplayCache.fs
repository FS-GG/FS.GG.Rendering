module Audit_ReplayCache

// Feature 006 (Verify Imported Mechanisms) — AUDIT of the real `PictureReplayCache` backend seam.
// These tests are NOT a re-statement of Feature 120's contract tests; they independently audit, with
// proven discriminating power, that (a) the CPU-only `create`/`stats` surface is reachable (T008),
// (b) the enabled cache is byte-identical to the disabled parity oracle AND the comparison can catch a
// real divergence (US2/T027, FR-004/FR-011), and (c) repeated identical boundary paints actually reach
// a Hit steady-state via the live counters (US3/T036).
//
// Degrade-and-disclose (Constitution Principle VI): the paint seam needs a real `SKCanvas`. The audit
// drives it over an OFFSCREEN raster `SKSurface` (CPU — no GL window required), so it runs headless on a
// live SkiaSharp native stack. If even the offscreen raster surface cannot be created (SkiaSharp native
// absent / fully headless host), the GL/pixel-dependent assertions are recorded SKIPPED via `skiptest`
// with the required tier named in the reason — they exit cleanly as Ignored, never faked as a pass and
// never failed. The pure scaffold-sanity assertion has no surface dependency and always runs.

open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// --- fixtures (mirroring Feature120ReplayCacheTests) -------------------------------------------------

let private blue: Color = { Red = 0uy; Green = 0uy; Blue = 255uy; Alpha = 255uy }

let private boundary (cacheId: uint64) (fp: uint64) : CacheBoundary =
    { CacheId = cacheId
      Fingerprint = fp
      Scene = { Nodes = [ Rectangle((0.0, 0.0, 8.0, 8.0), blue) ] } }

/// A counting paint walk — counts how often the cache fell through to the direct (record) walk.
let private countingPaint (counter: int ref) : SKCanvas -> Scene -> unit =
    fun canvas scene ->
        counter.Value <- counter.Value + 1
        scene.Nodes |> List.iter (SceneRenderer.paintNode canvas)

/// Tier-T1 capability probe: can we stand up an offscreen raster surface at all? `SKSurface.Create`
/// returns null (it does not throw) when the native raster backend is unavailable.
let private rasterAvailable: bool =
    try
        use s = SKSurface.Create(SKImageInfo(8, 8))
        not (isNull s)
    with _ -> false

let private tierSkip (what: string) : unit =
    skiptest (
        sprintf
            "SKIPPED(tier=T1 raster/pixel GL): offscreen SKSurface unavailable on this host (SkiaSharp native/headless) — %s requires the raster/pixel render tier; recorded skipped-with-tier, not a pass (Constitution VI)."
            what
    )

let private withCanvas (f: SKCanvas -> unit) =
    use surface = SKSurface.Create(SKImageInfo(64, 64))
    f surface.Canvas

/// Render `scene` over an offscreen raster surface for `frames` frames with the given replay-cache
/// state installed on the active renderer, returning the final PNG snapshot bytes.
let private renderPng (cache: PictureReplayCache.Cache option) (scene: Scene) (frames: int) : byte[] =
    use surface = SKSurface.Create(SKImageInfo(64, 64))
    SceneRenderer.activeReplayCache <- cache
    try
        for _ in 1..frames do
            surface.Canvas.Clear(SKColors.Black)
            scene.Nodes |> List.iter (SceneRenderer.paintNode surface.Canvas)
            surface.Canvas.Flush()
    finally
        SceneRenderer.activeReplayCache <- None
    use img = surface.Snapshot()
    use data = img.Encode(SKEncodedImageFormat.Png, 100)
    data.ToArray()

/// A scene of three CachedSubtree boundaries over distinct content (the record/replay subject).
let private boundaryScene (cacheId: uint64) (yOff: float) : SceneNode =
    CachedSubtree
        { CacheId = cacheId
          Fingerprint = cacheId * 7UL + 1UL
          Scene =
            { Nodes =
                [ Rectangle((4.0, yOff, 40.0, 14.0), { Red = 30uy; Green = 144uy; Blue = 255uy; Alpha = 255uy })
                  SceneNode.Text((6.0, yOff + 11.0), "row", { Red = 240uy; Green = 240uy; Blue = 245uy; Alpha = 255uy }) ] } }

// Sequenced (feature 203, US4/T024): the byte-identical cache-vs-direct parity proof renders through the
// shared, single-threaded SceneRenderer; a concurrent render in another suite would tear its pixels and
// flip this red intermittently (the disclosed feature-202 "GL flakiness" — a shared-state race, not a
// missing window system). Running it in the non-parallel phase — together with every other renderer,
// all likewise sequenced — keeps the pass set deterministic. The canonical `rasterAvailable`/`tierSkip`
// idiom below is unchanged; only the list's scheduling is.
[<Tests>]
let tests =
    testSequenced
    <| testList "Audit ReplayCache (Feature 006: verify imported mechanisms)" [

        // ---- T008 scaffold sanity (CPU-only; no surface/GL) ----------------------------------------
        test "Audit: scaffold sanity — create/stats reachable, disabled cache reports zeroed counters (T008)" {
            // A pure CPU `create false |> stats` needs no canvas, no GL — it proves the seam is wired
            // and the disabled parity oracle starts fully zeroed before any paint.
            let cache = PictureReplayCache.create false
            let s = PictureReplayCache.stats cache
            Expect.equal s.Entries 0 "fresh disabled cache holds no entries"
            Expect.equal s.Hits 0 "fresh disabled cache has zero hits"
            Expect.equal s.Misses 0 "fresh disabled cache has zero misses"
            Expect.equal s.Records 0 "fresh disabled cache has zero records"
            Expect.equal s.NativeBytes 0 "fresh disabled cache owns zero native bytes"
            Expect.equal s.SkippedNodes 0 "fresh disabled cache has skipped no nodes"
            Expect.isTrue (PictureReplayCache.cap > 0) "the LRU cap is a positive bound"
        }

        // ---- T027 US2 parity, with discriminating proof (FR-004/FR-011) ---------------------------
        test "Audit: US2 parity — cache-on output is byte-identical to the disabled oracle, and the comparison is discriminating (T027, FR-004/FR-011)" {
            if not rasterAvailable then
                tierSkip "the cache-on vs cache-off pixel parity comparison"
            else
                let sceneA: Scene = { Nodes = [ boundaryScene 0UL 2.0; boundaryScene 1UL 20.0; boundaryScene 2UL 38.0 ] }
                // A genuinely different scene (shifted + extra boundary) — the discriminating control.
                let sceneB: Scene = { Nodes = [ boundaryScene 0UL 6.0; boundaryScene 1UL 24.0; boundaryScene 9UL 42.0 ] }

                let direct = renderPng None sceneA 1 // cache-off direct walk (reference)
                let oracleOff = renderPng (Some(PictureReplayCache.create false)) sceneA 1 // disabled oracle
                let replayWarm = renderPng (Some(PictureReplayCache.create true)) sceneA 3 // frame 1 records, 2/3 replay

                // Parity: enabled cache must paint exactly what the disabled oracle / direct walk paints.
                Expect.equal oracleOff direct "FR-011: the disabled oracle is byte-identical to the direct walk"
                Expect.equal replayWarm direct "FR-004/FR-009: a warmed replayed boundary is byte-identical to the direct walk"

                // Discriminating proof: the SAME comparison applied to a different scene must FAIL to be
                // equal — proving these asserts would catch a real divergence rather than always passing.
                let differentScene = renderPng None sceneB 1
                Expect.notEqual differentScene direct
                    "DISCRIMINATING: a different scene yields different pixels — the parity comparison can detect divergence"
        }

        // ---- T036 US3 effectiveness via live counters (FINDING if no hit) -------------------------
        test "Audit: US3 effectiveness — repeated identical boundary paints reach a Hit steady-state (T036)" {
            if not rasterAvailable then
                tierSkip "the live record/replay counter effectiveness measurement"
            else
                withCanvas (fun canvas ->
                    let cache = PictureReplayCache.create true
                    let walks = ref 0
                    let paint = countingPaint walks
                    let b = boundary 0UL 100UL

                    let frames = 10
                    for _ in 1..frames do
                        PictureReplayCache.paintBoundary cache canvas paint b

                    let s = PictureReplayCache.stats cache

                    // FINDING gate: an enabled cache that never hits a stable boundary is a real defect.
                    Expect.isGreaterThan s.Hits 0
                        "FINDING if false: enabled cache produced NO hit on a stable repeated boundary"

                    // First paint records (1 miss / 1 record / 1 direct walk); the rest replay.
                    Expect.equal s.Records 1 "exactly one cold record for the stable boundary"
                    Expect.equal s.Misses 1 "exactly one cold miss"
                    Expect.equal walks.Value 1 "the direct walk runs once (record); subsequent frames replay"
                    Expect.equal s.Hits (frames - 1) "every post-warmup frame is a replay hit (steady-state)"
                    Expect.equal s.Entries 1 "the stable boundary occupies exactly one resident entry"
                    Expect.isTrue (s.NativeBytes > 0) "the recorded picture owns observable native bytes"
                    // Measured margin: Hits (frames-1)/frames after a single warmup record.
                    )
        }
    ]
