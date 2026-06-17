namespace Rendering.Harness

open FS.GG.UI.Scene

type TextShapingParityMode =
    | Direct
    | ColdRetained
    | WarmRetained
    | CacheEnabled
    | CacheDisabled
    | ShapingEnabled
    | PureFallback

type TextShapingParityCapture =
    { FixtureId: string
      Mode: TextShapingParityMode
      Metrics: TextMetrics
      Fingerprint: string
      Diagnostics: string list }

module TextShapingParity =
    val capture: mode: TextShapingParityMode -> fixture: TextShapingFixture -> result: ShapedTextResult -> TextShapingParityCapture
    val equivalent: left: TextShapingParityCapture -> right: TextShapingParityCapture -> bool
