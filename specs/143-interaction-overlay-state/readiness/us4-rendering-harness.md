# US4 Rendering Harness Evidence

Date: 2026-06-17

Status: LIMITATION RECORDED.

Validation:

- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build --filter Feature143`
- Result: 1 passed, 0 failed.

Evidence:

- The current headless harness test records that visual overlay-order proof needs an offscreen GL host.
- Semantic paint/hit order for the implemented coordinator slice is covered by Controls tests and existing Feature 140 layer tests.

Remaining:

- Add real offscreen visual and hit-order harness proof once host support is available in the validation environment.
