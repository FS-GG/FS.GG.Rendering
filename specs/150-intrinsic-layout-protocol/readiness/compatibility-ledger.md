# Feature150 Compatibility Ledger

## Public Surface Changes

- `FS.GG.UI.Layout` adds Feature150 records for normalized constraints, intrinsic queries/results, child placements, cache entries, and layout content extent.
- `FS.GG.UI.Layout.Layout` adds `constraints`, `constraintsFromAvailable`, `layoutInputKey`, `intrinsicQuery`, `evaluateIntrinsic`, `measureProtocol`, `cacheEntry`, and `contentExtent`.
- `FS.GG.UI.Controls.ScrollViewport` adds content width, horizontal/vertical offset ranges, extent source, and diagnostics while retaining `ContentHeight`, `Offset`, and `MaxOffset`.
- `FS.GG.UI.Controls.Elmish` adds `LayoutWorkMetrics` plus `ControlsElmish.layoutMetrics`.
- `FS.GG.UI.Testing` adds `LayoutReadiness` report/status/evidence helper contracts.

## Behavior Changes

- `Control.scrollViewport` derives extent from `Layout.contentExtent` and Layout intrinsic queries instead of inspecting rendered descendant bounds.
- Smaller-than-viewport scroll content normalizes content extent to the viewport and reports zero unnecessary overflow.
- Overflowing scroll content reports a positive vertical max offset from the intrinsic content extent.

## Diagnostic Changes

- Layout diagnostics add intrinsic/cache-specific codes: unsupported intrinsic query, rejected intrinsic result, stale cache entry, duplicate measurement, insufficient dependency evidence, and contradictory intrinsic extent.
- Controls diagnostics add scroll-specific classifications for unavailable intrinsic data and extent fallback.

## Compatibility Verdict

Accepted for the bounded first slice. Existing `Layout.evaluate`, `Layout.evaluateIncremental`, `Layout.renderComputed`, hit testing, and snap helpers retain their signatures. Public surface baselines were refreshed with additive Feature150 deltas for Layout, Controls, Controls.Elmish, and Testing.

## Release Notes Draft

Feature150 introduces the first public intrinsic layout protocol for `FS.GG.UI.Layout` and updates `ScrollViewer` extent readback to use layout-provided intrinsic evidence.

## Migration Guidance

Existing callers that only evaluate layout or read vertical scroll metrics can continue using the old members. New callers should prefer `ContentWidth`, `MaxHorizontalOffset`, `MaxVerticalOffset`, `ExtentSource`, and `Diagnostics` when inspecting `ScrollViewport`.

## Limitations

- The implementation keeps Yoga as the default evaluator.
- The intrinsic query implementation is conservative and deterministic; it is not a general constraint solver.
- Full solution test and broad retained/default layout regression validation remain open follow-up work for final P8 acceptance.
