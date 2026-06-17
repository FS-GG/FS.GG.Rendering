# US5 Reference Date Picker Evidence

Date: 2026-06-17

Status: PARTIAL PASS.

Implemented:

- Controls-level reference date-picker overlay flow test.
- AntShowcase test-project reference-flow test using the local `OverlayState` coordinator.
- Local source references for `AntShowcase.Tests` so the Feature 143 Controls contract is visible.

Validation:

- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --filter Feature143`
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter Feature143`
- Result: all focused reference-flow tests passed.

Remaining:

- Showcase core scripted interactions.
- Showcase page/demo-state integration for live date-picker open/navigation/selection.
- App-level evidence emission and README command update.
