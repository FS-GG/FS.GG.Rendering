# Contract: Harness CLI

`dotnet run --project tests/Rendering.Harness -- <subcommand> [flags]`

## Subcommands

| Subcommand | Tier(s) | Purpose | Display dep |
|---|---|---|---|
| `probe` | — | Record env facts (display/GL/refresh/extensions, effective backend); write `run.json` | none |
| `offscreen` | T0, T1 | Deterministic render/routing + offscreen readback (non-blank PNG) | none |
| `live-x11` | T2 | Launch viewer on X11, discover window, screenshot, inject input, confirm visible change | X11 + WM |
| `perf` | T3 | Bounded frame set in a perf mode; per-frame + percentile metrics with display/swap facts | Xorg/KMS |
| `input` | T0/T2/T-uinput | Run a declarative input script on a chosen backend | per backend |

## Common flags

- `--out <dir>` — run directory (default `artifacts/harness/<run-id>/`; gitignored).
- `--scene <name>` — built-in scene to render (default: a deterministic demo scene).
- `--json` — print the `run.json` path on stdout.

## Subcommand flags

- `perf --mode <throughput|paced-60|paced-native|stress-resize|input-latency>` `--frames <n>`
- `input --backend <pure|x11-xtest|uinput>` `--script <path>`
- `live-x11 --frames <n>` (bounded)

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Tier ran and its assertions passed (or cleanly skipped — see `run.json.status`) |
| 1 | Tier ran and an assertion failed (e.g. blank PNG, no visible change) |
| 2 | Bad usage (unknown subcommand/flag) |

A **clean skip** (no desktop, no `/dev/uinput`, Wayland-classified) is exit 0 with
`run.json.status = "skipped"` and the reason — never a crash, never a false pass.

## Acceptance (maps to spec)

- [ ] Five subcommands exist and dispatch via `argv`. *(FR-001)*
- [ ] `probe` records display/GL/refresh/backend. *(FR-002, SC-007)*
- [ ] Every subcommand writes the evidence artifact. *(FR-003)*
- [ ] Clean degradation is exit 0 + `status:"skipped"`, crashes are absent. *(FR-008, SC-005)*
