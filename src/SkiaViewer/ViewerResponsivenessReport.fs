namespace FS.GG.UI.SkiaViewer

open System
open System.IO
open System.Text.Json
open FS.GG.UI.SkiaViewer

module internal ViewerResponsivenessReport =
    let defaultResponsivenessBudget =
        { InputReceiptP95 = TimeSpan.FromMilliseconds 4.0
          InputReceiptMax = TimeSpan.FromMilliseconds 16.0
          InputToVisibleP95 = TimeSpan.FromMilliseconds 50.0
          InputToVisibleMax = TimeSpan.FromMilliseconds 150.0
          LongFrameThreshold = TimeSpan.FromMilliseconds 50.0 }

    let defaultResponsivenessOptions =
        { Enabled = false
          RunId = None
          OutputRoot = None
          Budget = defaultResponsivenessBudget
          Sink = None }

    let responsivenessInputKindToken kind = ViewerResponsiveness.responsivenessInputKindToken kind

    let responsivenessVisibleResponseToken response =
        ViewerResponsiveness.responsivenessVisibleResponseToken response

    let responsivenessEnvironmentStatusToken status =
        ViewerResponsiveness.responsivenessEnvironmentStatusToken status

    let responsivenessReadinessToken readiness = ViewerResponsiveness.responsivenessReadinessToken readiness

    let createResponsivenessRunId () = ViewerResponsiveness.createResponsivenessRunId ()

    let latencyRecordToJsonLine (latency: ViewerLatencyRecord) =
        ViewerResponsiveness.latencyRecordToJsonLine latency

    let summarizeResponsivenessRecords
        (runId: string)
        (scope: string)
        (recordsPath: string)
        (startedUtc: DateTimeOffset)
        (completedUtc: DateTimeOffset)
        (budget: ViewerResponsivenessBudget)
        (records: ViewerLatencyRecord list)
        : ViewerResponsivenessSummary
        =
        ViewerResponsiveness.summarizeResponsivenessRecords runId scope recordsPath startedUtc completedUtc budget records

    let responsivenessSummaryToJson (summary: ViewerResponsivenessSummary) =
        ViewerResponsiveness.responsivenessSummaryToJson summary

    let responsivenessSummaryToMarkdown (summary: ViewerResponsivenessSummary) =
        ViewerResponsiveness.responsivenessSummaryToMarkdown summary

    let writeResponsivenessRun (outputRoot: string) (summary: ViewerResponsivenessSummary) (records: ViewerLatencyRecord list) =
        ViewerResponsiveness.writeResponsivenessRun outputRoot summary records
