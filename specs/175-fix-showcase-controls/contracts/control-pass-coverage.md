# Contract: Control-Pass Coverage

Governs the systematic pass: every control classified, every finding recorded and re-verified,
live behavior matching scripted coverage. Spans sample tests and the readiness finding log.

## Surface touched

- `samples/SecondAntShowcase/SecondAntShowcase.Tests` — per-control live-vs-scripted parity,
  coverage classification, visual parity.
- `specs/175-fix-showcase-controls/readiness/finding-log.md` and `coverage-classification.md` —
  evidence artifacts.
- Existing `CoverageTests` / `InteractionContracts` remain the classification source of truth.

## Behavior

1. **Classification** (FR-012, SC-007): every catalog control resolves to exactly one of
   `Interactive` or `DisplayOnly`; none unclassified. The check fails if an interactive control
   does not respond under the verification path.
2. **Live-vs-scripted parity** (FR-007, SC-003): no control passes scripted coverage while failing
   under real input; the live path and the scripted path produce the same state change.
3. **Finding log** (FR-010, SC-005): each defect is recorded as `Open → Fixed → ReVerified` with
   symptom, root cause, fix tier, and verification. The feature is accepted only at zero
   unresolved findings.
4. **No removal** (FR-014): no control, page, or existing passing behavior is removed; the 19-page
   catalog and templates remain intact.

## Acceptance evidence

- Coverage test reports every control classified with no failing interactive control.
- Per-family parity tests assert live evidence equals scripted evidence for each contract.
- `finding-log.md` shows all findings `ReVerified`; `coverage-classification.md` lists every
  control with its classification and reason.
- The Feature 174 responsiveness budgets are not regressed (re-run the existing runner).
