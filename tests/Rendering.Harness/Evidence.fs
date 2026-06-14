namespace Rendering.Harness

open System
open System.IO
open System.Globalization

module Evidence =

    type Evidence =
        { RunId: string
          Tier: Tier
          Subcommand: string
          Status: RunStatus
          SkipReason: string option
          ProofLevel: ProofLevel
          AuthoritativeFor: string list
          NotAuthoritativeFor: string list
          Facts: ProbeFacts
          Frames: int
          P50Ms: float option
          P95Ms: float option
          P99Ms: float option
          Artifacts: string list }

    let tierToken tier =
        match tier with
        | T0 -> "T0"
        | T1 -> "T1"
        | T2 -> "T2"
        | T3 -> "T3"
        | TUinput -> "T-uinput"

    let proofToken proof =
        match proof with
        | Deterministic -> "deterministic"
        | OffscreenPixels -> "offscreen-pixels"
        | LiveHost -> "live-host"
        | Timing -> "timing"
        | KernelInput -> "kernel-input"

    let statusToken status =
        match status with
        | Passed -> "passed"
        | Failed -> "failed"
        | Skipped -> "skipped"

    let backendToken backend =
        match backend with
        | X11 -> "x11"
        | Wayland -> "wayland"
        | NoDisplay -> "none"

    // minimal JSON string escape for the controlled values used here
    let esc (s: string) =
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")

    let q (s: string) = "\"" + esc s + "\""
    let strList (xs: string list) = "[" + String.Join(", ", xs |> List.map q) + "]"
    let optStr (o: string option) = match o with Some s -> q s | None -> "null"
    let optNum (o: float option) = match o with Some v -> v.ToString("0.###", CultureInfo.InvariantCulture) | None -> "null"
    let optInt (o: int option) = match o with Some v -> string v | None -> "null"
    let boolStr (b: bool) = if b then "true" else "false"

    let percentiles (frameMs: float list) =
        match frameMs with
        | [] -> (None, None, None)
        | _ ->
            let sorted = frameMs |> List.sort |> List.toArray
            let pick p =
                // nearest-rank percentile
                let idx = int (ceil (p * float sorted.Length)) - 1
                let i = max 0 (min (sorted.Length - 1) idx)
                Some sorted.[i]
            (pick 0.50, pick 0.95, pick 0.99)

    let toJson (evidence: Evidence) =
        let e = evidence
        let f = e.Facts
        let sb = Text.StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        line "{"
        line (sprintf "  \"runId\": %s," (q e.RunId))
        line (sprintf "  \"tier\": %s," (q (tierToken e.Tier)))
        line (sprintf "  \"subcommand\": %s," (q e.Subcommand))
        line (sprintf "  \"status\": %s," (q (statusToken e.Status)))
        line (sprintf "  \"skipReason\": %s," (optStr e.SkipReason))
        line (sprintf "  \"proofLevel\": %s," (q (proofToken e.ProofLevel)))
        line (sprintf "  \"authoritativeFor\": %s," (strList e.AuthoritativeFor))
        line (sprintf "  \"notAuthoritativeFor\": %s," (strList e.NotAuthoritativeFor))
        line "  \"env\": {"
        line (sprintf "    \"effectiveBackend\": %s," (q (backendToken f.EffectiveBackend)))
        line (sprintf "    \"display\": %s," (optStr f.Display))
        line (sprintf "    \"gl\": { \"renderer\": %s, \"version\": %s, \"direct\": %s }," (optStr f.GlRenderer) (optStr f.GlVersion) (boolStr f.GlDirect))
        line (sprintf "    \"refreshHz\": %s," (optNum f.RefreshHz))
        line (sprintf "    \"extensions\": %s" (strList f.Extensions))
        line "  },"
        line (sprintf "  \"present\": { \"swapControl\": %s, \"vblankSource\": %s }," (optInt f.SwapControl) (optStr f.VblankSource))
        line (sprintf "  \"metrics\": { \"frames\": %d, \"p50Ms\": %s, \"p95Ms\": %s, \"p99Ms\": %s }," e.Frames (optNum e.P50Ms) (optNum e.P95Ms) (optNum e.P99Ms))
        line (sprintf "  \"artifacts\": %s" (strList e.Artifacts))
        line "}"
        sb.ToString()

    let metricsCsv (frameMs: float list) =
        let sb = Text.StringBuilder()
        sb.AppendLine("frame,ms") |> ignore
        frameMs |> List.iteri (fun i ms -> sb.AppendLine(sprintf "%d,%s" i (ms.ToString("0.###", CultureInfo.InvariantCulture))) |> ignore)
        sb.ToString()

    let toSummary (evidence: Evidence) =
        let e = evidence
        let sb = Text.StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        line (sprintf "# Harness run %s — tier %s (%s)" e.RunId (tierToken e.Tier) (statusToken e.Status))
        line ""
        line (sprintf "- proof level: **%s**" (proofToken e.ProofLevel))
        line (sprintf "- authoritative for: %s" (String.Join(", ", e.AuthoritativeFor)))
        line (sprintf "- **NOT** authoritative for: %s" (String.Join(", ", e.NotAuthoritativeFor)))
        line (sprintf "- effective backend: %s" (backendToken e.Facts.EffectiveBackend))
        match e.SkipReason with
        | Some r -> line (sprintf "- skipped: %s" r)
        | None -> ()
        sb.ToString()

    let write (dir: string) (evidence: Evidence) (frameMs: float list) =
        let e = evidence
        Directory.CreateDirectory(dir) |> ignore
        let runJson = Path.Combine(dir, "run.json")
        File.WriteAllText(runJson, toJson e)
        File.WriteAllText(Path.Combine(dir, "metrics.csv"), metricsCsv frameMs)
        File.WriteAllText(Path.Combine(dir, "summary.md"), toSummary e)
        runJson
