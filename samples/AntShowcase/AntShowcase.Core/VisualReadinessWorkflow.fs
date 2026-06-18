module AntShowcase.Core.VisualReadinessWorkflow

open FS.GG.UI.Scene

type ScreenshotStatus =
    | CapturePending
    | CaptureCaptured
    | CaptureDegraded of reason: string

type ScreenshotTarget =
    { PageId: string
      ThemeId: string
      Size: Size
      RelativePath: string
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

let targetFor size pageId themeId =
    { PageId = pageId
      ThemeId = themeId
      Size = size
      RelativePath = themeFolder themeId + "/" + pageId + ".png"
      Status = CapturePending }

let init seed size themeIds pageIds outDir =
    let targets =
        [ for themeId in themeIds do
              for pageId in pageIds do
                  targetFor size pageId themeId ]
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

let updateTarget pageId themeId f targets =
    targets
    |> List.map (fun target ->
        if target.PageId = pageId && target.ThemeId = themeId then f target else target)

let evaluateStatus model =
    let anyPending = model.Targets |> List.exists (fun t -> t.Status = CapturePending)
    let anyDegraded = model.Targets |> List.exists (fun t -> match t.Status with CaptureDegraded _ -> true | _ -> false)
    if anyPending then Pending
    elif anyDegraded then EnvironmentLimited
    elif not model.ReviewerDefectsPresent || model.CriticalDefectsPresent then Blocked
    else Accepted

let update msg model =
    let model' =
        match msg with
        | ScreenshotCaptureSucceeded(pageId, themeId) ->
            { model with Targets = model.Targets |> updateTarget pageId themeId (fun t -> { t with Status = CaptureCaptured }) }
        | ScreenshotCaptureDegraded(pageId, themeId, reason) ->
            { model with Targets = model.Targets |> updateTarget pageId themeId (fun t -> { t with Status = CaptureDegraded reason }) }
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
