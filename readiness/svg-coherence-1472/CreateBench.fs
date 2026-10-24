module CreateBench

open System
open System.Diagnostics
open System.Text
open System.Text.Json
open FS.GG.UI.Scene

// Same source and method for baseline and candidate: only the referenced Scene.dll differs.
// Measures SpatialWorkingSet.create (time and allocation) for populations with different amounts of
// entries per chunk, and joins one query per cell to an independent linear reference.

let chunk = 64.0

let box id x y w h =
    { Id = id
      Bounds = { X = x; Y = y; Width = w; Height = h }
      Value = id }

type Cell =
    { Layout: string
      N: int
      Make: int -> SpatialEntry<string>
      Viewport: Rect
      Limit: int }

let r x y w h = { X = x; Y = y; Width = w; Height = h }

let mixedEntries n =
    let random = Random 20261025
    let sizes = [| for _ in 1..n -> random.NextDouble(), random.NextDouble(), random.NextDouble(), random.NextDouble() |]
    fun i ->
        let (a, b, c, d) = sizes[i]
        let w = if i % 17 = 0 then 40.0 + c * 140.0 else c * 12.0
        let h = if i % 23 = 0 then 40.0 + d * 140.0 else d * 12.0
        box $"mixed-{i}" (a * 2000.0) (b * 2000.0) w h

let cells =
    [ for n in [ 1000; 10000; 100000; 300000 ] ->
          { Layout = "spread-1-per-chunk"; N = n; Make = (fun i -> box $"e{i}" (float i * 100.0) 0.0 8.0 8.0); Viewport = r 0.0 0.0 2000.0 64.0; Limit = 1000 }
      for n in [ 1000; 10000; 100000 ] ->
          { Layout = "grid-4-per-chunk"; N = n; Make = (fun i -> box $"e{i}" (float (i % 316) * 32.0) (float (i / 316) * 32.0) 8.0 8.0); Viewport = r 0.0 0.0 640.0 640.0; Limit = 400 }
      for n in [ 250; 1000; 10000 ] ->
          { Layout = "soldiers-grid-36"; N = n; Make = (fun i -> box $"e{i}" (float (i % 40) * 36.0) (float (i / 40) * 36.0) 8.0 8.0); Viewport = r 0.0 0.0 720.0 720.0; Limit = 400 }
      for n in [ 1000; 10000; 100000 ] ->
          { Layout = "cluster-100-per-chunk"
            N = n
            Make = (fun i -> let c, j = i / 100, i % 100 in box $"e{i}" (float (c % 100) * 64.0 + float (j % 10) * 6.0) (float (c / 100) * 64.0 + float (j / 10) * 6.0) 4.0 4.0)
            Viewport = r 0.0 0.0 640.0 640.0
            Limit = 400 }
      for n in [ 1000; 5000; 20000; 40000 ] ->
          { Layout = "one-chunk"; N = n; Make = (fun i -> box $"e{i}" (float (i % 60)) (float ((i / 60) % 60)) 2.0 2.0); Viewport = r 10.0 10.0 6.0 6.0; Limit = 4 }
      for n in [ 20000 ] ->
          { Layout = "mixed-spanning"; N = n; Make = mixedEntries n; Viewport = r 500.0 500.0 300.0 300.0; Limit = 400 } ]

let idsHash (ids: string list) =
    use sha = Security.Cryptography.SHA256.Create()
    Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(String.Join("\n", ids)))).Substring(0, 16)

// Independent linear reference: selected ids in source order and the number of entries sharing a visited chunk.
let oracle (entries: SpatialEntry<string> list) (v: Rect) =
    let span start length = floor (start / chunk), floor ((start + length) / chunk)
    let qx0, qx1 = span v.X v.Width
    let qy0, qy1 = span v.Y v.Height
    let shares (e: SpatialEntry<string>) =
        let ex0, ex1 = span e.Bounds.X e.Bounds.Width
        let ey0, ey1 = span e.Bounds.Y e.Bounds.Height
        ex0 <= qx1 && qx0 <= ex1 && ey0 <= qy1 && qy0 <= ey1
    let touches (e: SpatialEntry<string>) =
        let b = e.Bounds
        b.X <= v.X + v.Width && v.X <= b.X + b.Width && b.Y <= v.Y + v.Height && v.Y <= b.Y + b.Height
    entries |> List.filter (fun e -> shares e && touches e) |> List.map _.Id,
    int ((qx1 - qx0 + 1.0) * (qy1 - qy0 + 1.0)),
    entries |> List.filter shares |> List.length

let nowMs () = float (Stopwatch.GetTimestamp()) * 1000.0 / float Stopwatch.Frequency

[<EntryPoint>]
let main argv =
    let label = argv[0]
    let outPath = argv[1]
    let rounds = if argv.Length > 2 then int argv[2] else 9
    let key (c: Cell) = $"{c.Layout}|{c.N}"
    let prepared = cells |> List.map (fun c -> c, List.init c.N c.Make) |> Array.ofList

    let semantic =
        [ for (c, entries) in prepared ->
              let index = SpatialWorkingSet.create chunk entries |> Result.defaultWith (fun e -> failwithf "%A" e)
              let q = { Viewport = c.Viewport; Overscan = 0.0; PinnedIds = Set.empty; MaxVisitedChunks = c.Limit }
              let res = SpatialWorkingSet.query q index |> Result.defaultWith (fun e -> failwithf "%A" e)
              let ids = res.Entries |> List.map _.Id
              let expIds, expVisited, expCandidates = oracle entries c.Viewport
              {| layout = c.Layout
                 n = c.N
                 selected = ids.Length
                 idsHash = idsHash ids
                 matchesLinearReference = (ids = expIds && res.VisitedChunkCount = expVisited && res.CandidateCount = expCandidates && res.TotalEntryCount = entries.Length)
                 visited = res.VisitedChunkCount
                 candidates = res.CandidateCount
                 total = res.TotalEntryCount |} ]

    // Warm up every cell once (JIT, tiered compilation) before measuring.
    for (c, entries) in prepared do
        for _ in 1..3 do
            SpatialWorkingSet.create chunk (List.truncate (min c.N 2000) entries) |> ignore
        SpatialWorkingSet.create chunk entries |> ignore

    let iterationsFor (c: Cell) = max 1 (min 200 (200000 / c.N))
    let times = Collections.Generic.Dictionary<string, Collections.Generic.List<float>>()
    let allocs = Collections.Generic.Dictionary<string, Collections.Generic.List<int64>>()
    for (c, _) in prepared do
        times[key c] <- Collections.Generic.List<float>()
        allocs[key c] <- Collections.Generic.List<int64>()

    // Interleaved rounds: every round visits every cell once in a rotating order, so drift is shared.
    for round in 0 .. rounds - 1 do
        for k in 0 .. prepared.Length - 1 do
            let (c, entries) = prepared[(k + round * 7) % prepared.Length]
            let iters = iterationsFor c
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()
            let a0 = GC.GetAllocatedBytesForCurrentThread()
            let t0 = nowMs ()
            let mutable last = Unchecked.defaultof<_>
            for _ in 1..iters do
                last <- SpatialWorkingSet.create chunk entries
            let t1 = nowMs ()
            let a1 = GC.GetAllocatedBytesForCurrentThread()
            GC.KeepAlive last
            times[key c].Add((t1 - t0) / float iters)
            allocs[key c].Add((a1 - a0) / int64 iters)

    let result =
        {| label = label
           runtime = Environment.Version.ToString()
           framework = Runtime.InteropServices.RuntimeInformation.FrameworkDescription
           processorCount = Environment.ProcessorCount
           rounds = rounds
           semantic = semantic
           create =
            [ for (c, _) in prepared ->
                  {| layout = c.Layout
                     n = c.N
                     iterationsPerSample = iterationsFor c
                     createMs = times[key c] |> Seq.toList
                     allocatedBytes = allocs[key c] |> Seq.toList |} ] |}
    IO.File.WriteAllText(outPath, JsonSerializer.Serialize(result, JsonSerializerOptions(WriteIndented = true)))
    0
