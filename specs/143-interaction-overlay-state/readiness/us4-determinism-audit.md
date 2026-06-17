# US4 Determinism Audit

Date: 2026-06-17

Status: PARTIAL PASS.

Implemented:

- `InteractionReplayLog` data contract.
- Ordered input, transition, focus, dispatch, dismissal, diagnostic, and hit-decision evidence.
- Three-run byte-identical replay test.
- 100-scene deterministic fixture corpus.
- Elmish direct/retained/cache-mode parity projection over the same pure coordinator.

Validation:

- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --filter Feature143`
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build --filter Feature143`
- Result: all focused tests passed.

Remaining:

- Threading through retained/direct/cache host routing.
- Retained reuse preservation for unchanged overlay state.
- Pointer/Focus module evidence emission.
