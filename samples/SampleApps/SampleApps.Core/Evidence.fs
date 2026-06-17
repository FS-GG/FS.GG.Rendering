/// Deterministic per-sample evidence record + serialization (contracts/evidence-record.md).
/// Pure and GL-free: the App edge supplies the `FrameMetrics` (from `Perf.runScript`) and
/// the screenshot outcome (from `captureScreenshotEvidence`); this module turns them into
/// byte-stable `run.json` / `state.txt` / `summary.md` text. Ports G1's package-only schema
/// (research R5) and EXTENDS it with the authored-acceptance `Outcome` block, so the record
/// both discloses and checks the sample's source-spec acceptance criteria (FR-009/SC-001).
module SampleApps.Core.Evidence

open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer

/// The authored acceptance outcome (research R6). `Kind` is `"game"`/`"productivity"`;
/// `Values` holds the pinned facts the run is checked against — e.g.
/// `[ "terminal","game-over"; "clearedRows","4"; "score","1200" ]` for Tetris, or
/// `[ "committed","3"; "rejected","1"; "completed","2" ]` for Todo. Equality on this value
/// is the build-outcome gate (R-E2 / FR-009).
type ExpectedOutcome = { Kind: string; Values: (string * string) list }

/// The disclosed subset of a screenshot outcome carried in the record.
type ScreenshotSummary =
    { ProvesScreenshot: bool
      BlockedStage: string option
      UnsupportedHostReason: string option
      Fallback: string option
      Path: string option }

/// The per-sample proof record.
type SampleEvidenceRecord =
    { SampleId: string
      Seed: int
      ProofLevel: string
      AuthoritativeFor: string list
      NotAuthoritativeFor: string list
      Outcome: ExpectedOutcome
      Screenshot: ScreenshotSummary }

// --- screenshot mapping -----------------------------------------------------------

/// Map a framework `ScreenshotEvidenceResult` into the disclosed summary. `path` is the
/// relative screenshot path when a frame was actually written, otherwise None.
let ofScreenshotResult (result: ScreenshotEvidenceResult) (path: string option): ScreenshotSummary =
    { ProvesScreenshot = result.ProvesScreenshot
      BlockedStage = result.BlockedStage |> Option.map (sprintf "%A")
      UnsupportedHostReason = result.UnsupportedHostReason
      Fallback = result.Fallback
      Path = (if result.ProvesScreenshot then path else None) }

/// A degraded (no-GL / capture-unavailable) summary with a stated reason. Never claims a
/// screenshot; never carries a frame path (FR-008/SC-003).
let degraded (reason: string): ScreenshotSummary =
    { ProvesScreenshot = false
      BlockedStage = Some "capture"
      UnsupportedHostReason = Some reason
      Fallback = Some "deterministic-state-only"
      Path = None }

// --- record construction ----------------------------------------------------------

/// Build the record. `notAuthoritativeFor` is always non-empty (FR-007/R-E1); `outcome`
/// is always authoritative (it is checked headlessly, R-E3); a proven screenshot adds
/// "non-blank-offscreen-png".
let build (sampleId: string) (seed: int) (outcome: ExpectedOutcome) (_metrics: FrameMetrics list) (shot: ScreenshotSummary): SampleEvidenceRecord =
    let authoritative =
        if shot.ProvesScreenshot then
            [ "determinism"; "tree-equality"; "outcome"; "non-blank-offscreen-png" ]
        else
            [ "determinism"; "tree-equality"; "outcome" ]
    { SampleId = sampleId
      Seed = seed
      ProofLevel = "deterministic"
      AuthoritativeFor = authoritative
      NotAuthoritativeFor = [ "renderer-vs-desktop-pixels"; "live-host"; "timing" ]
      Outcome = outcome
      Screenshot = shot }

// --- state.txt (golden FrameMetrics: count/bool fields only, no *Duration) ---------

let private metricsHeader =
    "frame,productModelChanged,viewCalled,fullRenderCount,remeasuredNodeCount,memoHit,memoMiss,"
    + "virtualMaterialized,virtualTotal,repaintedNodes,dirtyRects,dirtyArea,pictureCacheHit,"
    + "pictureCacheMiss,pictureCacheEntries,textMeasureHit,textMeasureMiss,layoutInvalidated,"
    + "pointerSamples,pointerMoves,fullRenderFallback,frameCause,diffRan,layoutRan,paintRan,"
    + "replayHit,replayMiss,replayRecords,replaySkipped,replayCacheBytes"

let private metricLine (i: int) (m: FrameMetrics): string =
    String.concat
        ","
        [ string i
          string m.ProductModelChanged
          string m.ViewCalled
          string m.FullRenderCount
          string m.RemeasuredNodeCount
          string m.MemoHitCount
          string m.MemoMissCount
          string m.VirtualItemsMaterialized
          string m.VirtualItemsTotal
          string m.RepaintedNodeCount
          string m.DirtyRectCount
          string m.DirtyArea
          string m.PictureCacheHitCount
          string m.PictureCacheMissCount
          string m.PictureCacheEntryCount
          string m.TextMeasureCacheHitCount
          string m.TextMeasureCacheMissCount
          string m.LayoutInvalidatedNodeCount
          string m.PointerSamplesReceived
          string m.PointerMovesProcessed
          string m.FullRenderFallbackCount
          sprintf "%A" m.FrameCause
          string m.DiffRan
          string m.LayoutRan
          string m.PaintRan
          string m.ReplayHitCount
          string m.ReplayMissCount
          string m.ReplayRecordCount
          string m.ReplaySkippedNodeCount
          string m.ReplayCacheNativeBytes ]

/// The golden state outcome: deterministic count/bool metrics only (timing excluded, R-E5).
let goldenState (metrics: FrameMetrics list): string =
    let lines = metrics |> List.mapi metricLine
    String.concat "\n" (metricsHeader :: lines) + "\n"

// --- run.json (hand-rolled, fixed field order ⇒ byte-stable) -----------------------

let private esc (s: string): string =
    s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")

let private q (s: string): string = "\"" + esc s + "\""

let private optStr (v: string option): string =
    match v with
    | Some s -> q s
    | None -> "null"

let private strList (xs: string list): string =
    "[" + String.concat ", " (xs |> List.map q) + "]"

let private boolJson (b: bool): string = if b then "true" else "false"

/// Render the `outcome` block's value pairs as `[["k", "v"], …]`.
let private outcomeValues (values: (string * string) list): string =
    let one (k, v) = "[" + q k + ", " + q v + "]"
    "[" + String.concat ", " (values |> List.map one) + "]"

/// Serialize to the contract `run.json` shape. Deterministic: fixed key order, invariant
/// formatting, no wall-clock fields (R-E3).
let toRunJson (r: SampleEvidenceRecord): string =
    let s = r.Screenshot
    let sb = System.Text.StringBuilder()
    let line (t: string) = sb.AppendLine(t) |> ignore
    line "{"
    line (sprintf "  \"sampleId\": %s," (q r.SampleId))
    line (sprintf "  \"seed\": %d," r.Seed)
    line (sprintf "  \"proofLevel\": %s," (q r.ProofLevel))
    line (sprintf "  \"authoritativeFor\": %s," (strList r.AuthoritativeFor))
    line (sprintf "  \"notAuthoritativeFor\": %s," (strList r.NotAuthoritativeFor))
    line "  \"outcome\": {"
    line (sprintf "    \"kind\": %s," (q r.Outcome.Kind))
    line (sprintf "    \"values\": %s" (outcomeValues r.Outcome.Values))
    line "  },"
    line "  \"screenshot\": {"
    line (sprintf "    \"provesScreenshot\": %s," (boolJson s.ProvesScreenshot))
    line (sprintf "    \"blockedStage\": %s," (optStr s.BlockedStage))
    line (sprintf "    \"unsupportedHostReason\": %s," (optStr s.UnsupportedHostReason))
    line (sprintf "    \"fallback\": %s," (optStr s.Fallback))
    line (sprintf "    \"path\": %s" (optStr s.Path))
    line "  }"
    line "}"
    sb.ToString()

/// Human-readable disclosure (`summary.md`).
let toSummaryMd (r: SampleEvidenceRecord): string =
    let s = r.Screenshot
    let sb = System.Text.StringBuilder()
    let line (t: string) = sb.AppendLine(t) |> ignore
    line (sprintf "# Sample Apps evidence — `%s` (seed %d)" r.SampleId r.Seed)
    line ""
    line (sprintf "- proof level: **%s**" r.ProofLevel)
    line (sprintf "- authoritative for: %s" (String.concat ", " r.AuthoritativeFor))
    line (sprintf "- **NOT** authoritative for: %s" (String.concat ", " r.NotAuthoritativeFor))
    line (sprintf "- outcome (`%s`):" r.Outcome.Kind)
    for k, v in r.Outcome.Values do
        line (sprintf "    - %s = %s" k v)
    line (sprintf "- screenshot proven: **%b**" s.ProvesScreenshot)
    match s.UnsupportedHostReason with
    | Some reason -> line (sprintf "- screenshot disclosure: %s" reason)
    | None -> ()
    match s.Fallback with
    | Some fb -> line (sprintf "- fallback: %s" fb)
    | None -> ()
    sb.ToString()
