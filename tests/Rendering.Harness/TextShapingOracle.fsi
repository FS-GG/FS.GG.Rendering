namespace Rendering.Harness

open FS.GG.UI.Scene

module TextShapingOracle =
    val assertFingerprintStable: result: ShapedTextResult -> bool
    val measureDrawAdvanceDelta: result: ShapedTextResult -> float
    val diagnosticsCoverFixture: fixture: TextShapingFixture -> result: ShapedTextResult -> bool
