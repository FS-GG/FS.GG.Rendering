# Testing API FSI Surface

The Feature 164 public surface is additive and lives in `src/Testing/Testing.fsi`.

New public records/unions:

- `VisualSize`
- `VisualTheme`
- `VisualPage`
- `VisualCaptureTarget`
- `VisualCaptureStatus`
- `VisualCaptureArtifact`
- `VisualCaptureRecord`
- `VisualReviewerSeverity`
- `VisualReviewerClassification`
- `VisualReviewerValidationResult`
- `VisualContactSheet`
- `VisualReadinessStatus`
- `VisualReadinessReport`
- `VisualSummarySectionUpdate`

New public modules:

- `VisualCaptureMatrix`
- `VisualCompleteness`
- `VisualReviewerClassifications`
- `VisualReadiness`
- `VisualReadinessMarkdown`

No new package dependency was added to `FS.GG.UI.Testing`; PNG validation uses the existing SkiaSharp reference.

Surface baselines updated:

- `readiness/surface-baselines/FS.GG.UI.Testing.txt`
- `tests/surface-baselines/FS.GG.UI.Testing.txt`
