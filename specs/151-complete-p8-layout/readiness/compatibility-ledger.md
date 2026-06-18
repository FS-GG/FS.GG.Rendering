# Feature151 Compatibility Ledger

Status: `accepted`

## Public Surface Changes

No new public `.fsi` surface is introduced by Feature151. The accepted public
contract remains the Feature150 layout, ScrollViewer, Controls.Elmish metrics, and
Testing readiness helper surface.

## Behavior Changes

| Surface | Change | Intentional | Migration | Baseline Delta | Evidence | Status |
|---|---|---|---|---|---|---|
| `FS.GG.UI.Layout` | Representative corpus accepts existing deterministic bounds, diagnostics, intrinsic extent, and dependency identities. | yes | None. Existing `Layout.evaluate`, `evaluateIncremental`, `contentExtent`, `layoutInputKey`, and cache-entry helpers remain source-compatible. | none | `Feature151RepresentativeCorpus`, `Feature151MeasuredReuse`, `Feature151IntrinsicReuse` | `accepted` |
| `FS.GG.UI.Controls` | ScrollViewer extent coverage is broadened without changing the public `ScrollViewport` shape. | yes | None. Prefer `ContentWidth`, `ContentHeight`, `MaxHorizontalOffset`, `MaxVerticalOffset`, `ExtentSource`, and `Diagnostics`. | none | `Feature151ScrollViewerCorpus` | `accepted` |
| `FS.GG.UI.Controls.Elmish` | Layout work metric projection is regression-checked for P8. | yes | None. | none | `Feature151LayoutRegressionMetrics` | `accepted` |
| `FS.GG.UI.Testing` | Existing `LayoutReadiness` aggregation is used for Feature151 readiness. | yes | None. | none | `Feature151Readiness` | `accepted` |

## Diagnostic Changes

Feature151 accepts the existing Feature150 diagnostic vocabulary:
`InvalidAvailableSpace`, `InvalidLayoutValue`, `DuplicateLayoutNodeId`,
`UnsatisfiedConstraint`, `UnsupportedIntrinsicQuery`, `RejectedIntrinsicResult`,
`StaleLayoutCacheEntry`, `DuplicateMeasurement`,
`InsufficientDependencyEvidence`, and `ContradictoryIntrinsicExtent`.

## Migration Guidance

No consumer migration is required for Feature151. Consumers can adopt the
Feature150 protocol helpers for more explicit evidence and continue using
existing control authoring APIs.

## Surface Baseline References

Surface refresh is recorded as no unexpected drift for Feature151. The durable
baseline review remains [docs/validation/surface-baseline-review.md](../../../docs/validation/surface-baseline-review.md).

## Limitations

Feature151 does not claim new compositor live partial-redraw acceptance, browser
rendering acceptance, or a replacement layout solver.
