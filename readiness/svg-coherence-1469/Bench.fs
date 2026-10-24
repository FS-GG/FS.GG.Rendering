module Bench

open System
open System.Diagnostics
open System.Text
open System.Text.Json
open FS.GG.UI.Scene

// Same source, same method for baseline and candidate: only the referenced Scene.dll differs.
let box id x y =
    { Id = id
      Bounds = { X = x; Y = y; Width = 8.0; Height = 8.0 }
      Value = id }

let filler i = box $"filler-{i}" (1.0e6 + float i * 100.0) 1.0e6

/// Population of n entries; `visible` ordinals hold entries inside viewport [0,0,400,400]; extra ordinals hold pinned-offscreen entries.
let populate n (visible: int list) (pinned: int list) =
    let slots = Array.init n filler
    visible |> List.iteri (fun k o -> slots[o] <- box $"visible-{k}" (float (k % 10) * 36.0) (float (k / 10) * 36.0))
    pinned |> List.iteri (fun k o -> slots[o] <- box $"pinned-{k}" (-5000.0 - float k * 100.0) -5000.0)
    List.ofArray slots

let viewportQuery pins =
    { Viewport = { X = 0.0; Y = 0.0; Width = 400.0; Height = 400.0 }
      Overscan = 0.0
      PinnedIds = Set.ofList pins
      MaxVisitedChunks = 64 }

let tail count n = [ n - count .. n - 1 ]
let head count = [ 0 .. count - 1 ]

type Cell =
    { Name: string
      N: int
      Visible: int list
      Pinned: int list
      Pins: string list }

let cells =
    [ for n in [ 1000; 10000; 100000; 300000 ] do
          let one = [ 1 ]
          // one visible entry
          yield { Name = "visible1-first"; N = n; Visible = [ 0 ]; Pinned = []; Pins = [] }
          yield { Name = "visible1-middle"; N = n; Visible = [ n / 2 ]; Pinned = []; Pins = [] }
          yield { Name = "visible1-last"; N = n; Visible = [ n - 1 ]; Pinned = []; Pins = [] }
          // 100 visible entries (the M9 working-set size): first block vs last block vs sparse tail
          yield { Name = "visible100-first"; N = n; Visible = head 100; Pinned = []; Pins = [] }
          yield { Name = "visible100-last"; N = n; Visible = tail 100 n; Pinned = []; Pins = [] }
          yield { Name = "visible100-sparse-late"; N = n; Visible = [ for k in 0..99 -> n - 1 - k * 7 ] |> List.sort; Pinned = []; Pins = [] }
          // pinned off-screen identity at a late ordinal, with and without visible entries
          yield { Name = "pinned1-last"; N = n; Visible = []; Pinned = [ n - 1 ]; Pins = [ "pinned-0" ] }
          yield { Name = "visible100-first-pinned1-last"; N = n; Visible = head 100; Pinned = [ n - 1 ]; Pins = [ "pinned-0" ] }
          yield { Name = "visible100-last-pinned1-last"; N = n; Visible = tail 100 (n - 1); Pinned = [ n - 1 ]; Pins = [ "pinned-0" ] } ]
    |> List.filter (fun c -> c.N >= 1000)

let idsHash (r: SpatialWorkingSetResult<string>) =
    use sha = Security.Cryptography.SHA256.Create()
    let text = String.Join("\n", r.Entries |> List.map _.Id)
    Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes text)).Substring(0, 16)

let oracle (c: Cell) (entries: SpatialEntry<string> list) =
    // independent linear reference (walks the whole population): selected ids in source order
    let exp = { X = 0.0; Y = 0.0; Width = 400.0; Height = 400.0 }
    let pins = Set.ofList c.Pins
    entries
    |> List.filter (fun e ->
        Set.contains e.Id pins
        || (e.Bounds.X <= exp.X + exp.Width && exp.X <= e.Bounds.X + e.Bounds.Width
            && e.Bounds.Y <= exp.Y + exp.Height && exp.Y <= e.Bounds.Y + e.Bounds.Height))
    |> List.map _.Id

let nowNs () = float (Stopwatch.GetTimestamp()) * 1.0e9 / float Stopwatch.Frequency

[<EntryPoint>]
let main argv =
    let label = argv[0]
    let outPath = argv[1]
    let rounds = if argv.Length > 2 then int argv[2] else 30
    let createReps = 7
    let ordered = cells
    // Build all indexes first (so queries are not interleaved with construction garbage).
    let prepared =
        ordered
        |> List.map (fun c ->
            let entries = populate c.N c.Visible c.Pinned
            let index = SpatialWorkingSet.create 64.0 entries |> Result.defaultWith (fun e -> failwithf "%A" e)
            c, entries, index)

    // semantic + counter join
    let semantic =
        prepared
        |> List.map (fun (c, entries, index) ->
            let r = SpatialWorkingSet.query (viewportQuery c.Pins) index |> Result.defaultWith (fun e -> failwithf "%A" e)
            let expected = oracle c entries
            let ids = r.Entries |> List.map _.Id
            {| cell = c.Name
               n = c.N
               selected = ids.Length
               idsHash = idsHash r
               matchesLinearReference = (ids = expected)
               visited = r.VisitedChunkCount
               candidates = r.CandidateCount
               total = r.TotalEntryCount |})

    // Iterations per sample: scale so one sample is ~>=1ms for the cheap cells
    let iterationsFor (c: Cell) = if c.N >= 100000 then 20 else 200
    // warm up every cell
    for (c, _, index) in prepared do
        let q = viewportQuery c.Pins
        for _ in 1 .. 200 do
            SpatialWorkingSet.query q index |> ignore

    // Interleaved rounds: every round visits every cell once, in a rotating order, so drift and order are shared.
    let samples = Collections.Generic.Dictionary<string, Collections.Generic.List<float>>()
    let key (c: Cell) = $"{c.Name}|{c.N}"
    for (c, _, _) in prepared do samples[key c] <- Collections.Generic.List<float>()
    let arr = Array.ofList prepared
    for round in 0 .. rounds - 1 do
        for k in 0 .. arr.Length - 1 do
            let (c, _, index) = arr[(k + round * 5) % arr.Length]
            let q = viewportQuery c.Pins
            let iters = iterationsFor c
            let t0 = nowNs ()
            for _ in 1..iters do
                SpatialWorkingSet.query q index |> ignore
            let t1 = nowNs ()
            samples[key c].Add((t1 - t0) / float iters)

    // Construction cost and allocation (adverse side of the change).
    let creation =
        [ for n in [ 1000; 10000; 100000; 300000 ] do
              let entries = populate n [ 0 ] []
              SpatialWorkingSet.create 64.0 entries |> ignore
              let times = [ for _ in 1..createReps ->
                              GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect()
                              let a0 = GC.GetAllocatedBytesForCurrentThread()
                              let t0 = nowNs ()
                              let r = SpatialWorkingSet.create 64.0 entries
                              let t1 = nowNs ()
                              let a1 = GC.GetAllocatedBytesForCurrentThread()
                              GC.KeepAlive r
                              (t1 - t0) / 1.0e6, a1 - a0 ]
              yield {| n = n; createMs = times |> List.map fst; allocatedBytes = times |> List.map snd |} ]

    let result =
        {| label = label
           runtime = Environment.Version.ToString()
           framework = Runtime.InteropServices.RuntimeInformation.FrameworkDescription
           processorCount = Environment.ProcessorCount
           rounds = rounds
           semantic = semantic
           queryNsPerCall =
            [ for (c, _, _) in prepared ->
                  {| cell = c.Name
                     n = c.N
                     iterationsPerSample = iterationsFor c
                     samplesNs = samples[key c] |> Seq.toList |} ]
           creation = creation |}
    IO.File.WriteAllText(outPath, JsonSerializer.Serialize(result, JsonSerializerOptions(WriteIndented = true)))
    0
