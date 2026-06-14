namespace FS.Skia.UI.SkiaViewer

#nowarn "3261"

open System.Collections.Generic
open SkiaSharp
open FS.Skia.UI.Scene

// Feature 120 (US3, FR-007/FR-011/FR-013/FR-014) — the load-bearing backend replay cache. On a hit it
// replays a recorded `SKPicture` (skipping the per-primitive draw-call walk); on a miss it records,
// disposing any replaced/evicted picture. Native lifetime demands a mutable dictionary of native handles
// + explicit `Dispose` (constitution III measured-hot-path allowance, disclosed here). A disabled cache
// is the always-direct parity oracle (FR-011).
module internal PictureReplayCache =

    let cap = 256

    // bytes-per-node model estimate mirrors `RetainedRender` (deterministic native-byte proxy).
    [<Literal>]
    let private bytesPerNode = 64

    type internal Entry =
        { mutable Picture: SKPicture
          mutable Fingerprint: uint64
          mutable Stamp: int64
          mutable NodeBytes: int }

    [<Sealed>]
    type internal Cache(enabled: bool) =
        let entries = Dictionary<uint64, Entry>()
        member val Enabled = enabled
        member _.Entries = entries
        member val Clock = 0L with get, set
        member val Hits = 0 with get, set
        member val Misses = 0 with get, set
        member val Records = 0 with get, set
        member val SkippedNodes = 0 with get, set

    let create (enabled: bool) = Cache(enabled)

    // Painted-node count of a scene — `Scene.describe` sees through wrappers, matching the RetainedRender
    // skipped-node model.
    let private nodeCount (scene: Scene) = List.length (Scene.describe scene)

    let private evictOverCap (cache: Cache) =
        while cache.Entries.Count > cap do
            let mutable lruKey = 0UL
            let mutable lruStamp = System.Int64.MaxValue
            let mutable seen = false

            for kv in cache.Entries do
                if not seen || kv.Value.Stamp < lruStamp then
                    lruKey <- kv.Key
                    lruStamp <- kv.Value.Stamp
                    seen <- true

            if seen then
                let victim = cache.Entries.[lruKey]
                if not (isNull victim.Picture) then victim.Picture.Dispose()
                cache.Entries.Remove lruKey |> ignore

    let paintBoundary
        (cache: Cache)
        (canvas: SKCanvas)
        (paintScene: SKCanvas -> Scene -> unit)
        (boundary: CacheBoundary)
        =
        if not cache.Enabled then
            // Parity oracle (FR-011): recurse straight into the wrapped scene; no record, no replay.
            paintScene canvas boundary.Scene
        else
            cache.Clock <- cache.Clock + 1L

            match cache.Entries.TryGetValue boundary.CacheId with
            | true, entry when entry.Fingerprint = boundary.Fingerprint ->
                // HIT: replay the recorded draw commands — the per-primitive walk is skipped.
                canvas.DrawPicture entry.Picture
                entry.Stamp <- cache.Clock
                cache.Hits <- cache.Hits + 1
                cache.SkippedNodes <- cache.SkippedNodes + nodeCount boundary.Scene
            | found, entry ->
                // MISS (cold, changed fingerprint, or evicted): record, disposing any replaced picture.
                if found && not (isNull entry.Picture) then
                    entry.Picture.Dispose()

                let bounds = canvas.DeviceClipBounds
                let cull = SKRect(float32 bounds.Left, float32 bounds.Top, float32 bounds.Right, float32 bounds.Bottom)
                use recorder = new SKPictureRecorder()
                let recCanvas = recorder.BeginRecording cull
                paintScene recCanvas boundary.Scene
                let picture = recorder.EndRecording()
                canvas.DrawPicture picture

                let nb = nodeCount boundary.Scene * bytesPerNode

                if found then
                    entry.Picture <- picture
                    entry.Fingerprint <- boundary.Fingerprint
                    entry.Stamp <- cache.Clock
                    entry.NodeBytes <- nb
                else
                    cache.Entries.[boundary.CacheId] <-
                        { Picture = picture
                          Fingerprint = boundary.Fingerprint
                          Stamp = cache.Clock
                          NodeBytes = nb }

                cache.Misses <- cache.Misses + 1
                cache.Records <- cache.Records + 1
                evictOverCap cache

    let stats (cache: Cache) =
        let mutable nb = 0
        for kv in cache.Entries do
            nb <- nb + kv.Value.NodeBytes

        {| Entries = cache.Entries.Count
           NativeBytes = nb
           Hits = cache.Hits
           Misses = cache.Misses
           Records = cache.Records
           SkippedNodes = cache.SkippedNodes |}

    let resetCounters (cache: Cache) =
        cache.Hits <- 0
        cache.Misses <- 0
        cache.Records <- 0
        cache.SkippedNodes <- 0

    let dispose (cache: Cache) =
        for kv in cache.Entries do
            if not (isNull kv.Value.Picture) then kv.Value.Picture.Dispose()

        cache.Entries.Clear()
