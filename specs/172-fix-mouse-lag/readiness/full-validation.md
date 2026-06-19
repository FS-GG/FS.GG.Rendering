# Full Validation

- `tests/Controls.Tests/Controls.Tests.fsproj`: timed out with exit code `124` after 240 seconds. Caveat: the same project stalled in the pre-change baseline capture, so this is not reported as green.
- `tests/Elmish.Tests/Elmish.Tests.fsproj`: passed, 194 passed / 17 existing skipped.
- `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`: passed, 198 passed.
- `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`: passed, 122 passed.
- No validation caveat is summarized as a full pass.

Log: `specs/172-fix-mouse-lag/readiness/logs/final-validation-tests.log`
