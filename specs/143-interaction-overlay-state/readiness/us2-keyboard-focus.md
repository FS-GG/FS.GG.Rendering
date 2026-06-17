# US2 Keyboard And Focus Evidence

Date: 2026-06-17

Status: PARTIAL PASS.

Implemented:

- Overlay-local Tab and Shift+Tab traversal in the pure coordinator.
- Initial focus and focus recovery effects.
- Stale focus target diagnostics.
- Exactly-once product dispatch suppression by dispatch key.
- KeyboardInput handoff evidence for Escape and Shift+Tab modifier preservation.

Validation:

- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --filter Feature143`
- `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --no-build --filter Feature143`
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build --filter Feature143`
- Result: all focused tests passed.

Remaining:

- `Focus.fs` overlay helper integration.
- Controls.Elmish interpreter wiring for overlay focus and product-dispatch effects.
- Auto-complete and calendar widget keyboard metadata.
