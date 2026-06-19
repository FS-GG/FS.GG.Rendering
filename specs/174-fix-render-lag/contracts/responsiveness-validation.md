# Contract: Responsiveness Validation

## Existing Commands

Feature 174 uses existing sample commands rather than adding a new CLI.

Focused render-lag probes:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- \
  render-lag-probe --scenario button-click --theme light

dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- \
  render-lag-probe --scenario page-change --theme light
```

Accepted live responsiveness path:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- \
  responsiveness --script representative --theme light --all-interactive --require-live --out specs/174-fix-render-lag/readiness/responsiveness --json
```

Focused page diagnostics remain allowed with `--page buttons` and `--page text-numeric-input`, but accepted feature evidence must still cover the named button activation and page-change scenarios.

## Required Behavior

- `render-lag-probe --scenario button-click` exercises the first-frame-visible plus key/button activation path and emits phase trace diagnostics when tracing is enabled.
- `render-lag-probe --scenario page-change` navigates to `text-numeric-input` and emits phase trace diagnostics.
- The responsiveness command preserves Feature 173 contract behavior for `records.jsonl`, `summary.json`, `summary.md`, `environment.md`, exit codes, budgets, and environment-limited reporting.
- Feature 174 may add scenario-specific readiness artifacts, but it must not weaken or replace the Feature 173 artifact contract.

## Exit-Code Expectations

The existing responsiveness command semantics remain:

- `0`: accepted measured live responsiveness evidence was written.
- `2`: invalid request.
- `3`: artifact write or diagnostic infrastructure failure.
- `4`: live evidence unavailable, blocked, incomplete, environment-limited, missing boundary, timeout, or substitute-only.
- `5`: live evidence ran but failed an acceptance budget or drag-continuity rule.

Focused `render-lag-probe` remains diagnostic:

- `0`: the probe ran and produced viewer outcome/metrics.
- non-zero: the probe failed before producing usable diagnostics.

## Validation Requirements

- Automated tests must verify that unsupported/headless live runs remain non-accepted.
- Automated tests must verify that contracted responsiveness artifacts keep required fields and stable tokens.
- Scenario validation must record whether phase-attributed measurements were measured, blocked, failed, or environment-limited.
- Final validation must not summarize blocked, skipped, substitute, timed-out, degraded, manual-review-pending, or environment-limited checks as green.

## Compatibility Rules

- Ordinary `interactive`, `coverage`, `evidence`, `visual-readiness`, `review-findings`, and existing `responsiveness` usage remain source-compatible.
- Existing Feature 173 tests remain valid.
- No new external runtime dependency is introduced.
- Public surface baselines remain unchanged.
