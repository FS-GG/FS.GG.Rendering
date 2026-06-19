# Feature 167 Synthetic Evidence Disclosure

The deterministic/headless responsiveness evidence is intentionally synthetic substitute evidence.

- Synthetic tests and AntShowcase responsiveness output use `ControlsElmish.Perf.runScript` because the current automated environment may not provide a visible GL presentation surface.
- Synthetic records include `SYNTHETIC` diagnostics and report `environment-limited` or `headless-substitute`; they do not claim accepted live input-to-present readiness.
- Real live evidence path: run the AntShowcase `responsiveness` command on a visible GL-capable desktop session and archive the resulting `summary.json`, `summary.md`, `records.jsonl`, and `environment.md`.
- Current run: `responsiveness/resp-20260619-120611-0fcd49/summary.json` reports `overallReadiness = "environment-limited"` and `headless-substitute:no-live-presentation-boundary`.
