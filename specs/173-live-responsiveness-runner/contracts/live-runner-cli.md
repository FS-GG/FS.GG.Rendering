# Contract: Live Responsiveness Runner CLI

## Command

The sample continues to expose responsiveness review through the existing subcommand:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- \
  responsiveness --script representative --theme <light|dark> --all-interactive --require-live --out <dir> --json
```

Focused page runs remain available:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- \
  responsiveness --script representative --theme light --page buttons --require-live --out <dir> --json
```

## Options

| Option | Required | Meaning |
|--------|----------|---------|
| `responsiveness` | yes | Runs responsiveness evidence instead of ordinary interactive mode. |
| `--script representative` | default | Runs the representative action script. Unknown scripts are invalid requests. |
| `--theme <light|dark>` | default `light` | Selects the Ant theme for the run. |
| `--all-interactive` | one scope required | Exercises every family from `InteractionContracts.all`. |
| `--page <page-id>` | one scope required | Exercises representative actions for one page. Mutually exclusive with `--all-interactive`. |
| `--require-live` | required for accepted readiness | Requires measured visible desktop presentation. Without this, output remains diagnostic/substitute unless a live measurement mode is explicitly requested and measured. |
| `--out <dir>` | default allowed | Creates `<dir>/<run-id>/` and writes artifacts. |
| `--json` | optional | Prints a compact JSON pointer to `summary.json`, readiness, and run id. |

## Required Behavior

- `--all-interactive` enumerates all required interactions from `SecondAntShowcase.Core.InteractionContracts.all`.
- Display-only controls from `InteractionContracts.displayOnlyReasons` are written as exclusions and are not timed failures.
- The live runner opens or attaches to a visible, focusable desktop viewer window and performs real mouse/keyboard actions against the visible surface.
- Each representative action produces one measured record or one explicit non-accepted record with diagnostics.
- `--require-live` can exit `0` only when `overallReadiness = accepted`.
- Headless deterministic `ControlsElmish.Perf.runScript` output may still be written, but it must use non-accepted readiness.

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Accepted measured live responsiveness evidence was written. |
| `2` | Invalid request, unknown page/script/theme, or mutually exclusive options. |
| `3` | Artifact write failure or diagnostic infrastructure failure after the request starts. |
| `4` | Live evidence unavailable, blocked, incomplete, environment-limited, missing boundary, timeout, or substitute-only. |
| `5` | Live evidence ran but failed an acceptance budget or drag-continuity rule. |

## Console JSON

With `--json`, stdout contains one object:

```json
{
  "runId": "resp-20260619-193000-a1b2c3",
  "summaryJson": "specs/173-live-responsiveness-runner/readiness/responsiveness/resp-20260619-193000-a1b2c3/summary.json",
  "readiness": "accepted"
}
```

The `readiness` value uses the same tokens as `summary.json`.

## Compatibility Rules

- Existing ordinary `interactive`, `coverage`, `evidence`, `visual-readiness`, and `review-findings` commands remain source-compatible.
- Existing `responsiveness --page ...` substitute behavior may remain, but substitute output cannot return accepted readiness.
- Existing `--page` and parser validation tests remain valid.
- Any public framework hook required for live timing is additive and must preserve current call sites.
