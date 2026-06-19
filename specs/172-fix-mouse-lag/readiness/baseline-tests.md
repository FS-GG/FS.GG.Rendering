# Baseline Tests

- Command set: baseline run for `Controls.Tests`, `Elmish.Tests`, `SkiaViewer.Tests`, and `SecondAntShowcase.Tests`.
- Result: canceled/partial baseline.
- `Controls.Tests` reached test execution but did not complete before manual cancellation.
- `Elmish.Tests` and `SkiaViewer.Tests` were interrupted by the cancellation loop and are not baseline pass evidence.
- Caveat: this baseline is retained only to show the pre-change stall; it is not a green baseline.

Log: `specs/172-fix-mouse-lag/readiness/logs/baseline-tests.log`
