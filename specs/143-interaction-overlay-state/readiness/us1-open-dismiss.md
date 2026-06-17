# US1 Open And Dismiss Evidence

Date: 2026-06-17

Status: PARTIAL PASS.

Implemented:

- Pure overlay open/dismiss transitions for all eight supported surface kinds.
- Disabled-trigger safe failure.
- Missing-anchor safe failure.
- Topmost-only Escape/outside-pointer dismissal.
- Pointer outside-routing evidence through `OverlayState.PointerRouted`.

Validation:

- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --filter Feature143`
- Result: 20 passed, 0 failed.

Remaining:

- Widget metadata wiring in `Control`, `Buttons`, `DataEntry2`, and `Interactive2`.
- Pointer module host routing integration.
- Composition helper integration beyond existing Feature 140 layer evidence.
