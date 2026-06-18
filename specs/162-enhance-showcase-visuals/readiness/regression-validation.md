# Regression Validation Ledger

Validated on 2026-06-18 after AntShowcase was repinned to the current local package version `0.1.24-preview.1`.

## Existing AntShowcase Coverage

- Status: passed
- Evidence: `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- coverage` reported `96/96 controls mapped, 19 pages (13 catalog + 6 template), 0 unreferenced, 0 duplicated`. `Coverage` tests also passed in the focused 70-test run.

## Determinism

- Status: passed
- Evidence: full AntShowcase suite passed 78/78 tests, including `Determinism` tests for same-seed golden state and serialized evidence stability.

## Interaction

- Status: passed
- Evidence: focused AntShowcase filter passed 70/70 tests, including `Interaction` tests for keyboard activation, button activation, family-specific state transitions, host key mapping, and display-only exemptions.

## Feedback

- Status: passed
- Evidence: focused AntShowcase filter passed 70/70 tests, including `Feedback` tests for non-blank submit, blank no-op, per-page ordering, persistence round-trip, and malformed-line rejection.

## Templates

- Status: passed
- Evidence: focused AntShowcase filter passed 70/70 tests, including `Template` and `VisualTemplate` coverage for catalog-only composition, form validation, primary actions, and result/exception balance.

## Theme Invariance

- Status: passed
- Evidence: focused AntShowcase filter passed 70/70 tests, including `ThemeInvariance` and `VisualReadiness` checks for antLight/antDark shape invariance, alias resolution, accepted sizes, and page-state preservation.

## Feature 143/144/145 Overlay Regressions

- Status: passed for AntShowcase sample regressions
- Evidence: full AntShowcase suite passed 78/78 tests, including `Feature143 AntShowcase date-picker reference flow`, `Feature144 AntShowcase date-picker flow`, `Feature144 AntShowcase date-picker stale-overlay proof`, and `Feature145 AntShowcase overlay visual correlation`.
- Full-solution caveat: `dotnet test FS.GG.Rendering.slnx -c Release --no-restore --no-build` was canceled after the `Controls.Tests` child produced no additional output for several minutes. Before cancellation, the already reported projects had 809 passed tests and 20 skipped tests with no failures. `Controls.Tests` did not complete in that run.
