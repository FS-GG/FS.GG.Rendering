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
    let capture mode fixture result =
        { FixtureId = fixture.Id
          Mode = mode
          Metrics = Scene.measureShapedText result
          Fingerprint = result.Fingerprint
          Diagnostics = result.Diagnostics }

    let equivalent left right =
        left.Metrics = right.Metrics
        && left.Fingerprint = right.Fingerprint
        && left.Diagnostics = right.Diagnostics
