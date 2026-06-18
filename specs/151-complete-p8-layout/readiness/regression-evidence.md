# Feature151 Broad Regression Evidence

Status: `accepted`

| Area | Command or Filter | Expected Outcome | Actual Outcome | Classification | Evidence |
|---|---|---|---|---|---|
| retained rendering parity | `Feature151RetainedRenderingRegression` | accepted or limited | accepted | accepted | `tests/SkiaViewer.Tests/Feature151RetainedRenderingRegressionTests.fs` |
| default layout compatibility | `Feature151LayoutCompatibility` | accepted | accepted | accepted | `tests/Controls.Tests/Feature151LayoutCompatibilityTests.fs` |
| disabled-cache parity | `Feature151DisabledCacheParity` | accepted | accepted | accepted | `tests/Controls.Tests/Feature151DisabledCacheParityTests.fs` |
| overlay behavior | `Feature151LayoutCompatibility` | accepted | accepted | accepted | `tests/Controls.Tests/Feature151LayoutCompatibilityTests.fs` |
| render-anywhere | `Feature151RenderAnywhereRegression` | accepted | accepted | accepted | `tests/Rendering.Harness.Tests/Feature151RenderAnywhereRegressionTests.fs` |
| text-shaping | `Feature151TextShapingRegression` | accepted or synthetic-only classified | accepted | accepted, no new text behavior claimed | `tests/Rendering.Harness.Tests/Feature151TextShapingRegressionTests.fs` |
| compositor readiness | `Feature151CompositorReadinessRegression` | environment-limited or accepted without overclaim | environment-limited | non-blocking limitation | `tests/Rendering.Harness.Tests/Feature151CompositorReadinessRegressionTests.fs` |
| public surface compatibility | `Feature151CompatibilityLedger` | accepted | accepted | accepted | `tests/Package.Tests/Feature151CompatibilityLedgerTests.fs` |
| package validation | `Feature151PackageValidation` | accepted | accepted | accepted | `tests/Package.Tests/Feature151PackageValidationTests.fs` |
| full solution | `dotnet test FS.GG.Rendering.slnx` | accepted or classified | accepted | accepted | [package-validation.md](package-validation.md) |

The compositor row remains a non-claim: Feature151 does not accept live
partial-redraw performance behavior.
