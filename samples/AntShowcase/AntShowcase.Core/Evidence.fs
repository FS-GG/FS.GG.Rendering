/// Deterministic per-page evidence record + serialization (contracts/evidence-record.md).
/// Pure and GL-free: the App edge supplies the `FrameMetrics` (from `Perf.runScript`) and
/// the screenshot outcome (from `captureScreenshotEvidence`); this module turns them into
/// the byte-stable `run.json` / `state.txt` / `summary.md` text. Ported from G1's
/// `ControlsGallery.Core.Evidence` so the showcase stays package-only.
module AntShowcase.Core.Evidence

open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer

/// The disclosed subset of a screenshot outcome carried in the record.
type ScreenshotSummary =
    { ProvesScreenshot: bool
      BlockedStage: string option
      UnsupportedHostReason: string option
      Fallback: string option
      Path: string option }

/// The per-page proof record. `Mode` is the resolved Ant variant (antLight/antDark);
/// `ControlIds` are the catalog ids the page shows (empty for template pages).
type PageEvidenceRecord =
    { PageId: string
      Seed: int
      Mode: string
      ControlIds: string list
      ProofLevel: string
      AuthoritativeFor: string list
      NotAuthoritativeFor: string list
      Screenshot: ScreenshotSummary }

/// One visual-readiness screenshot entry. A `CaptureSource` other than `real-screenshot`
/// is a disclosed limitation and can never accept visual readiness.
type VisualScreenshotRecord =
    { PageId: string
      ThemeId: string
      Width: int
      Height: int
      RelativePath: string
      CaptureSource: string
      Completeness: string
      DegradedReason: string option }

/// Matrix-level visual-readiness summary for the preferred or minimum-size run.
type VisualReadinessSummary =
    { Seed: int
      Size: string
      AcceptedSizeRole: string
      PageIds: string list
      ThemeIds: string list
      RequiredScreenshotCount: int
      PresentScreenshotCount: int
      CompletenessStatus: string
      CaptureAvailability: string
      ReviewerDefectStatus: string
      VisualReadinessStatus: string
      Screenshots: VisualScreenshotRecord list
      ContactSheets: string list
      Limitations: string list }

/// Feature 144 reference overlay evidence carried by tests/readiness for the live
/// date-picker flow. This is intentionally product-state oriented: the coordinator asks
/// for open/focus/value changes, while AntShowcase owns the applied state.
type DatePickerOverlayEvidence =
    { ScenarioId: string
      InputStep: string
      ExpectedOverlayState: string
      TopmostHitTarget: string
      FocusState: string
      DispatchSummary: string
      BehavioralEvidenceReference: string
      ReplayLog: string list
      FocusTransitions: (string option * string option) list
      ProductMessages: string list
      Diagnostics: string list
      NoStaleOverlay: bool }

/// Deterministic reference evidence for the Feature 144 date-picker flow.
let datePickerReferenceOverlayEvidence (): DatePickerOverlayEvidence =
    { ScenarioId = "feature144-antshowcase-date-picker-reference"
      InputStep = "open:date-picker-calendar"
      ExpectedOverlayState = "open"
      TopmostHitTarget = "date-picker-calendar"
      FocusState = "date-picker-trigger"
      DispatchSummary = "DatePickerOpenChanged:true; DatePickerChanged:2026-06-17; DatePickerOpenChanged:false"
      BehavioralEvidenceReference = "Feature144 AntShowcase date-picker reference flow"
      ReplayLog = [ "navigate:text-numeric-input"; "open:date-picker-calendar"; "focus:calendar"; "select:2026-06-17"; "close:date-picker-calendar"; "focus:trigger" ]
      FocusTransitions = [ None, Some "date-picker-calendar"; Some "date-picker-calendar", Some "date-picker-trigger" ]
      ProductMessages = [ "DatePickerOpenChanged:true"; "DatePickerChanged:2026-06-17"; "DatePickerOpenChanged:false" ]
      Diagnostics = []
      NoStaleOverlay = true }

// --- screenshot mapping ---------------------------------------------------------------

/// Map a framework `ScreenshotEvidenceResult` into the disclosed summary. `path` is the
/// relative screenshot path when a frame was actually written, otherwise None.
let ofScreenshotResult (result: ScreenshotEvidenceResult) (path: string option): ScreenshotSummary =
    { ProvesScreenshot = result.ProvesScreenshot
      BlockedStage = result.BlockedStage |> Option.map (sprintf "%A")
      UnsupportedHostReason = result.UnsupportedHostReason
      Fallback = result.Fallback
      Path = (if result.ProvesScreenshot then path else None) }

/// A degraded (no-GL / capture-unavailable) summary with a stated reason. Never claims a
/// screenshot; never carries a frame path (FR-013/SC-005).
let degraded (reason: string): ScreenshotSummary =
    { ProvesScreenshot = false
      BlockedStage = Some "capture"
      UnsupportedHostReason = Some reason
      Fallback = Some "deterministic-state-only"
      Path = None }

// --- record construction --------------------------------------------------------------

/// What every Ant Showcase run does NOT prove (FR-012) — always non-empty.
let notAuthoritativeFor: string list =
    [ "pixel-level-ant-fidelity-vs-upstream-antd"
      "live-pointer-hit-testing-beyond-seeded-script"
      "chart-graph-controls-rendered-with-seeded-sample-data-only" ]

/// Build the record. `notAuthoritativeFor` is always non-empty (FR-012); a proven
/// screenshot adds "non-blank-offscreen-png" to what the run is authoritative for.
let build
    (pageId: string)
    (seed: int)
    (mode: string)
    (controlIds: string list)
    (metrics: FrameMetrics list)
    (shot: ScreenshotSummary)
    : PageEvidenceRecord =
    let _ = metrics
    let authoritative =
        if shot.ProvesScreenshot then
            [ "determinism"; "tree-equality"; "non-blank-offscreen-png" ]
        else
            [ "determinism"; "tree-equality" ]
    { PageId = pageId
      Seed = seed
      Mode = mode
      ControlIds = controlIds
      ProofLevel = "deterministic"
      AuthoritativeFor = authoritative
      NotAuthoritativeFor = notAuthoritativeFor
      Screenshot = shot }

// --- state.txt (golden FrameMetrics: count/bool fields only, no *Duration) ------------

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

/// The golden state outcome: deterministic count/bool metrics only (timing excluded).
let goldenState (metrics: FrameMetrics list): string =
    let lines = metrics |> List.mapi metricLine
    String.concat "\n" (metricsHeader :: lines) + "\n"

// --- run.json (hand-rolled, fixed field order ⇒ byte-stable) --------------------------

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

/// Serialize to the contract `run.json` shape. Deterministic: fixed key order, invariant
/// formatting, no wall-clock fields.
let toRunJson (r: PageEvidenceRecord): string =
    let s = r.Screenshot
    let sb = System.Text.StringBuilder()
    let line (t: string) = sb.AppendLine(t) |> ignore
    line "{"
    line (sprintf "  \"pageId\": %s," (q r.PageId))
    line (sprintf "  \"seed\": %d," r.Seed)
    line (sprintf "  \"mode\": %s," (q r.Mode))
    line (sprintf "  \"controlIds\": %s," (strList r.ControlIds))
    line (sprintf "  \"proofLevel\": %s," (q r.ProofLevel))
    line (sprintf "  \"authoritativeFor\": %s," (strList r.AuthoritativeFor))
    line (sprintf "  \"notAuthoritativeFor\": %s," (strList r.NotAuthoritativeFor))
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
let toSummaryMd (r: PageEvidenceRecord): string =
    let s = r.Screenshot
    let sb = System.Text.StringBuilder()
    let line (t: string) = sb.AppendLine(t) |> ignore
    line (sprintf "# Ant Showcase evidence — page `%s` (seed %d, %s)" r.PageId r.Seed r.Mode)
    line ""
    line (sprintf "- proof level: **%s**" r.ProofLevel)
    line (sprintf "- authoritative for: %s" (String.concat ", " r.AuthoritativeFor))
    line (sprintf "- **NOT** authoritative for: %s" (String.concat ", " r.NotAuthoritativeFor))
    line (sprintf "- screenshot proven: **%b**" s.ProvesScreenshot)
    match s.UnsupportedHostReason with
    | Some reason -> line (sprintf "- screenshot disclosure: %s" reason)
    | None -> ()
    match s.Fallback with
    | Some fb -> line (sprintf "- fallback: %s" fb)
    | None -> ()
    sb.ToString()

let visualScreenshotToJson (s: VisualScreenshotRecord): string =
    String.concat
        "\n"
        [ "    {"
          sprintf "      \"pageId\": %s," (q s.PageId)
          sprintf "      \"themeId\": %s," (q s.ThemeId)
          sprintf "      \"width\": %d," s.Width
          sprintf "      \"height\": %d," s.Height
          sprintf "      \"relativePath\": %s," (q s.RelativePath)
          sprintf "      \"captureSource\": %s," (q s.CaptureSource)
          sprintf "      \"completeness\": %s," (q s.Completeness)
          sprintf "      \"degradedReason\": %s" (optStr s.DegradedReason)
          "    }" ]

let visualSummaryToJson (summary: VisualReadinessSummary): string =
    let screenshots =
        if List.isEmpty summary.Screenshots then
            ""
        else
            summary.Screenshots |> List.map visualScreenshotToJson |> String.concat ",\n"
    String.concat
        "\n"
        [ "{"
          sprintf "  \"seed\": %d," summary.Seed
          sprintf "  \"size\": %s," (q summary.Size)
          sprintf "  \"acceptedSizeRole\": %s," (q summary.AcceptedSizeRole)
          sprintf "  \"pageIds\": %s," (strList summary.PageIds)
          sprintf "  \"themeIds\": %s," (strList summary.ThemeIds)
          sprintf "  \"requiredScreenshotCount\": %d," summary.RequiredScreenshotCount
          sprintf "  \"presentScreenshotCount\": %d," summary.PresentScreenshotCount
          sprintf "  \"completenessStatus\": %s," (q summary.CompletenessStatus)
          sprintf "  \"captureAvailability\": %s," (q summary.CaptureAvailability)
          sprintf "  \"reviewerDefectStatus\": %s," (q summary.ReviewerDefectStatus)
          sprintf "  \"visualReadinessStatus\": %s," (q summary.VisualReadinessStatus)
          sprintf "  \"contactSheets\": %s," (strList summary.ContactSheets)
          sprintf "  \"limitations\": %s," (strList summary.Limitations)
          "  \"screenshots\": ["
          screenshots
          "  ]"
          "}" ]
    + "\n"

let visualSummaryToMarkdown (summary: VisualReadinessSummary): string =
    let sb = System.Text.StringBuilder()
    let line (t: string) = sb.AppendLine(t) |> ignore
    line "# AntShowcase visual readiness"
    line ""
    line (sprintf "- seed: `%d`" summary.Seed)
    line (sprintf "- size: `%s` (%s)" summary.Size summary.AcceptedSizeRole)
    line (sprintf "- pages: `%d`" (List.length summary.PageIds))
    line (sprintf "- themes: `%s`" (String.concat "," summary.ThemeIds))
    line (sprintf "- required screenshots: `%d`" summary.RequiredScreenshotCount)
    line (sprintf "- present screenshots: `%d`" summary.PresentScreenshotCount)
    line (sprintf "- completeness: **%s**" summary.CompletenessStatus)
    line (sprintf "- capture availability: **%s**" summary.CaptureAvailability)
    line (sprintf "- reviewer defects: **%s**" summary.ReviewerDefectStatus)
    line (sprintf "- visual readiness: **%s**" summary.VisualReadinessStatus)
    if not (List.isEmpty summary.ContactSheets) then
        line ""
        line "## Contact Sheets"
        summary.ContactSheets |> List.iter (fun path -> line (sprintf "- `%s`" path))
    if not (List.isEmpty summary.Limitations) then
        line ""
        line "## Limitations"
        summary.Limitations |> List.iter (fun limitation -> line (sprintf "- %s" limitation))
    line ""
    line "## Screenshots"
    summary.Screenshots
    |> List.iter (fun s ->
        let degraded = s.DegradedReason |> Option.defaultValue "none"
        line (sprintf "- `%s` `%s` `%s` `%s` degraded=`%s`" s.PageId s.ThemeId s.RelativePath s.Completeness degraded))
    sb.ToString()

let reviewerDefectTemplate (pageIds: string list) (themeIds: string list): string =
    let classes =
        [ "shell overlap"
          "navigation label spill"
          "top-bar displacement"
          "content-footer collision"
          "unplanned background exposure"
          "section overpaint"
          "clipped primary label"
          "unreadable primary content"
          "transient-surface overprint"
          "template hierarchy unclear"
          "lower-level limitation" ]
    let header =
        [ "# Reviewer Defect Classification"
          ""
          sprintf "Defect classes: %s" (String.concat ", " classes)
          ""
          "| pageId | themeId | severity | class | readiness impact | reviewer | timestamp | notes |"
          "|---|---|---|---|---|---|---|---|" ]
    let rows =
        [ for pageId in pageIds do
              for themeId in themeIds do
                  sprintf "| %s | %s | none | none | no-blocker | pending | pending | pending review |" pageId themeId ]
    String.concat System.Environment.NewLine (header @ rows) + System.Environment.NewLine
