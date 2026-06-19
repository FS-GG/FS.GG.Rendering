module AntShowcase.Core.VisualReadinessWorkflow

open FS.GG.UI.Scene
open FS.GG.UI.Testing

type ScreenshotStatus =
    | CapturePending
    | CaptureCaptured
    | CaptureDegraded of reason: string

type ScreenshotTarget =
    { PageId: string
      ThemeId: string
      Size: Size
      RelativePath: string
      SharedTarget: VisualCaptureTarget
      Status: ScreenshotStatus }

type ReadinessStatus =
    | Pending
    | EnvironmentLimited
    | Blocked
    | Accepted

type Model =
    { Seed: int
      Size: Size
      ThemeIds: string list
      PageIds: string list
      OutputDirectory: string
      Targets: ScreenshotTarget list
      Status: ReadinessStatus
      ReviewerDefectsPresent: bool
      CriticalDefectsPresent: bool }

type Msg =
    | ScreenshotCaptureSucceeded of pageId: string * themeId: string
    | ScreenshotCaptureDegraded of pageId: string * themeId: string * reason: string
    | ReviewerDefectsLoaded of hasClassification: bool * hasCritical: bool
    | CompletenessEvaluated

type Effect =
    | CaptureScreenshot of ScreenshotTarget
    | WriteSummary
    | WriteReviewerRubric

let themeFolder themeId =
    match themeId with
    | "antLight" -> "light"
    | "antDark" -> "dark"
    | other -> other

let visualSize (size: Size): VisualSize =
    { Role = VisualConfig.roleName (VisualConfig.classifySize size)
      Width = size.Width
      Height = size.Height
      Order = 0 }

let visualPage order pageId: VisualPage =
    { PageId = pageId
      Title = pageId
      Order = order
      Required = true }

let visualTheme order themeId: VisualTheme =
    { ThemeId = themeId
      Title = themeId
      Order = order }

let pathFor (page: VisualPage) (theme: VisualTheme) (_size: VisualSize) =
    themeFolder theme.ThemeId + "/" + page.PageId + ".png"

let sharedTargets (size: Size) (themeIds: string list) (pageIds: string list) =
    let pages = pageIds |> List.mapi visualPage
    let themes = themeIds |> List.mapi visualTheme

    match VisualCaptureMatrix.expand pages themes [ visualSize size ] pathFor with
    | Ok targets -> targets
    | Result.Error diagnostics -> failwith (String.concat "; " diagnostics)

let targetFor (size: Size) (sharedTarget: VisualCaptureTarget) =
    { PageId = sharedTarget.Page.PageId
      ThemeId = sharedTarget.Theme.ThemeId
      Size = size
      RelativePath = sharedTarget.RelativePath
      SharedTarget = sharedTarget
      Status = CapturePending }

let init seed size themeIds pageIds outDir =
    let targets =
        sharedTargets size themeIds pageIds
        |> List.map (targetFor size)
    let model =
        { Seed = seed
          Size = size
          ThemeIds = themeIds
          PageIds = pageIds
          OutputDirectory = outDir
          Targets = targets
          Status = Pending
          ReviewerDefectsPresent = false
          CriticalDefectsPresent = false }
    model, (targets |> List.map CaptureScreenshot)

let updateTarget pageId themeId f (targets: ScreenshotTarget list) =
    targets
    |> List.map (fun target ->
        if target.PageId = pageId && target.ThemeId = themeId then f target else target)

let evaluateStatus model =
    let anyPending = model.Targets |> List.exists (fun t -> t.Status = CapturePending)
    if anyPending then Pending
    else
        let captureRecord (target: ScreenshotTarget) =
            match target.Status with
            | CaptureCaptured ->
                { Target = target.SharedTarget
                  Status = VisualCaptureComplete
                  Artifact = None
                  ExpectedWidth = target.Size.Width
                  ExpectedHeight = target.Size.Height
                  ObservedWidth = Some target.Size.Width
                  ObservedHeight = Some target.Size.Height
                  Reason = None
                  Diagnostics = [] }
            | CaptureDegraded reason -> VisualCompleteness.degraded target.SharedTarget reason
            | CapturePending ->
                { Target = target.SharedTarget
                  Status = VisualCaptureMissing
                  Artifact = None
                  ExpectedWidth = target.Size.Width
                  ExpectedHeight = target.Size.Height
                  ObservedWidth = None
                  ObservedHeight = None
                  Reason = Some "pending capture"
                  Diagnostics = [ "pending capture" ] }

        let reviewerClassifications =
            if not model.ReviewerDefectsPresent then
                []
            else
                model.Targets
                |> List.map (fun target ->
                    ({ TargetId = target.SharedTarget.TargetId
                       Severity = (if model.CriticalDefectsPresent then VisualReviewerBlocking else VisualReviewerNone)
                       DefectClass = "none"
                       ReadinessImpact = (if model.CriticalDefectsPresent then "blocking" else "no-blocker")
                       Reviewer = "ant-showcase"
                       ReviewedAt = "recorded"
                       Notes = "summary-level reviewer gate" }
                     : VisualReviewerClassification))

        let report =
            VisualReadiness.evaluate
                "ant-showcase"
                model.OutputDirectory
                (model.Targets |> List.map _.SharedTarget)
                (model.Targets |> List.map captureRecord)
                reviewerClassifications
                []
                []
                []

        match report.ReadinessStatus with
        | VisualReadinessAccepted -> Accepted
        | VisualReadinessEnvironmentLimited -> EnvironmentLimited
        | VisualReadinessIncomplete -> Pending
        | VisualReadinessPendingReview
        | VisualReadinessBlocked -> Blocked

let update (msg: Msg) (model: Model) =
    let model' =
        match msg with
        | ScreenshotCaptureSucceeded(pageId, themeId) ->
            { model with Targets = model.Targets |> updateTarget pageId themeId (fun (t: ScreenshotTarget) -> { t with Status = CaptureCaptured }) }
        | ScreenshotCaptureDegraded(pageId, themeId, reason) ->
            { model with Targets = model.Targets |> updateTarget pageId themeId (fun (t: ScreenshotTarget) -> { t with Status = CaptureDegraded reason }) }
        | ReviewerDefectsLoaded(hasClassification, hasCritical) ->
            { model with ReviewerDefectsPresent = hasClassification; CriticalDefectsPresent = hasCritical }
        | CompletenessEvaluated -> model
    let evaluated = { model' with Status = evaluateStatus model' }
    evaluated, [ WriteSummary; WriteReviewerRubric ]

let statusName status =
    match status with
    | Pending -> "pending"
    | EnvironmentLimited -> VisualConfig.visualReadinessStatusEnvironmentLimited
    | Blocked -> VisualConfig.visualReadinessStatusBlocked
    | Accepted -> VisualConfig.visualReadinessStatusAccepted
