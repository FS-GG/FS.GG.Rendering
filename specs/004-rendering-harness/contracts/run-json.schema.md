# Contract: `run.json` evidence artifact

The machine-readable evidence per run. The non-overclaim rule lives here: a run may **only**
claim what `authoritativeFor` lists, and MUST list what it cannot prove in `notAuthoritativeFor`.

## Shape

```json
{
  "runId": "<id>",
  "tier": "T0|T1|T2|T3|T-uinput",
  "subcommand": "probe|offscreen|live-x11|perf|input",
  "status": "passed|failed|skipped",
  "skipReason": "<string, when status=skipped>",
  "proofLevel": "deterministic|offscreen-pixels|live-host|timing|kernel-input",
  "authoritativeFor": ["<claim>", "..."],
  "notAuthoritativeFor": ["<claim>", "..."],
  "env": {
    "effectiveBackend": "x11|wayland|none",
    "display": "<:N or null>",
    "gl": { "renderer": "...", "version": "...", "direct": true },
    "refreshHz": 119.93,
    "extensions": ["XTEST", "Present", "RANDR", "..."]
  },
  "present": { "swapControl": "<n|null>", "vblankSource": "<HDMI-A-1|null>" },
  "metrics": { "frames": 0, "p50Ms": null, "p95Ms": null, "p99Ms": null },
  "artifacts": ["summary.md", "metrics.csv", "window.png"]
}
```

## Rules

- `proofLevel` MUST match the tier (T1 → `offscreen-pixels`; T2 → `live-host`; T3 → `timing`).
- `notAuthoritativeFor` MUST be non-empty for every non-`probe` run (e.g. T1 lists
  `"desktop-visibility"`; T0 lists `"renderer-pixels"`/`"live-host"`).
- **`vsync-faithful` may appear in `authoritativeFor` ONLY if** `present.swapControl` and
  `present.vblankSource` are both non-null (FR-007/SC-003). Otherwise it is forbidden.
- `status:"skipped"` requires a `skipReason` and MUST NOT appear as `passed`.
- Companion files: `metrics.csv` (per-frame rows) and `summary.md` (human, restating proof scope).

## Acceptance (maps to spec)

- [ ] Every artifact carries `proofLevel` + non-empty `notAuthoritativeFor`. *(FR-003, SC-004)*
- [ ] `vsync-faithful` gated on present facts. *(FR-007, SC-003)*
- [ ] `metrics.csv` + `summary.md` accompany `run.json`. *(FR-003)*
- [ ] Wayland-effective run is `status:"failed"`/classified, not silently passed. *(FR-014, SC-007)*
