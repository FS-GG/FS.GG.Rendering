module SecondAntShowcase.Core.VisualReadinessWorkflow

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

val init: seed: int -> size: Size -> themeIds: string list -> pageIds: string list -> outDir: string -> Model * Effect list
val update: msg: Msg -> model: Model -> Model * Effect list
val statusName: status: ReadinessStatus -> string
