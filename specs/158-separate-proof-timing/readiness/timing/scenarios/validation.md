# Feature 158 Scenario Timing Validation

Status: `accepted`

- Required scenario coverage: `timing/localized-update`, `timing/no-change`, `timing/movement-old-new`, `timing/overlap`, and `timing/edge-clipping`.
- Primary run `feature158-20260618152718` records `50` included samples, `0` excluded samples, warmup `3`, and measured repetitions `5` per path.
- Each scenario writes raw CSV/JSON evidence under `timing/raw/` and a reviewer report under `timing/scenarios/`.
- The shipped P7 performance claim remains `performance-not-accepted`.
