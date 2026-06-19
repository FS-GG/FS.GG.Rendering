# Contract: AntShowcase Responsiveness

## Purpose

AntShowcase is the representative first sample for input/render responsiveness because the retrospective measured the visible lag there. The sample must expose a documented responsiveness run without requiring maintainers to edit source files.

## Entry Point

Exact CLI flags may be refined during implementation, but the app edge must support a command with this shape:

```sh
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- \
  responsiveness \
  --page buttons \
  --theme light \
  --script representative \
  --out specs/167-input-render-responsiveness/readiness/responsiveness
```

## Required Options

| Option | Meaning |
|--------|---------|
| `responsiveness` | Run the responsiveness diagnostic workflow instead of launching ordinary interactive mode. |
| `--page <id>` | Page/screen to check. Initial required page is `buttons`. |
| `--theme <light|dark>` | Theme mode. Initial accepted run may use light; dark may be optional unless tasks broaden scope. |
| `--script <id>` | Interaction script id. `representative` includes pointer activation, keyboard activation, continuous movement burst, no-state-change input, and long-frame fixture where available. |
| `--out <dir>` | Output root. The command creates a run-id child directory. |
| `--require-live` | Optional flag requiring a visible presentation surface; returns environment-limited/non-zero readiness if unavailable. |
| `--json` | Print structured summary path and readiness token. |

## Representative Script

The initial representative script includes:

- pointer move burst over the active page
- primary pointer activation on a state-changing button/control
- key-down `Enter` activation
- key-down `Space` activation
- key-up for Enter/Space proving non-activation unless focused routing consumes it
- one input that produces no visible state change
- one fixture or diagnostic path that records long-frame behavior when available

## Output

The command writes:

```text
<out>/<run-id>/
|-- records.jsonl
|-- summary.json
|-- summary.md
`-- environment.md
```

If the live host cannot provide a visible presentation surface, output still includes `summary.json` and `environment.md` with `overallReadiness = environment-limited` or `blocked`.

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Accepted readiness for the requested live scope. |
| `1` | Budgets failed, long-frame policy failed, or required record is blocked/incomplete. |
| `2` | Invalid request, unknown page/script/theme, or unwritable output root. |
| `3` | Diagnostic infrastructure failure after the request starts. |
| `4` | Environment-limited live presentation; substitute evidence may exist but accepted live readiness was not produced. |

## Compatibility Rules

- Ordinary `interactive` launch behavior remains unchanged when diagnostics are not requested.
- `Host.defaultHost` keeps `MapKey` behavior: key-down `Enter` and `Space` activate the representative command; key-up does not activate unless focused-control routing handles it.
- Pointer activation still uses authored bindings before `MapPointer` fallback.
- AntShowcase continues to consume `FS.GG.UI.*` packages; diagnostics do not require project references in the app edge.

## Readiness Rules

- Accepted readiness requires at least one pointer activation and one keyboard activation record with measured visible response.
- A passing deterministic `Perf.runScript` result is useful substitute evidence but does not by itself satisfy live input-to-present readiness.
- Environment-limited runs must be saved and disclosed; they cannot be represented as accepted live responsiveness.
