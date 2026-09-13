# SVG scale measurement contract

The M9 contract extends the accepted Preview-A harness without changing its measured budgets. The exact
reference host, Playwright 1.62.1 browser family scope, three-run/200-sample nearest-rank p95 method, workloads,
thresholds and unavailable-stage rule are machine-readable in
`readiness/svg-scale-01-1/measurement-contract.json`.

Chromium owns detailed trace stages. Firefox and WebKit own functional, callback, next-frame and updated-surface
coverage; their absent Chromium-only stages remain unavailable with reasons. The 10x-extent comparison keeps the
visible set fixed and requires zero live-node growth plus at most a 1.20 steady-state cost ratio. The sustained
continuous workload allows at most a 0.05 missed-frame ratio. Idle and post-disposal owned-resource counts remain
exact zeroes.

`run-scale-contract.sh` proves the gate is discriminating: controlled mutations independently violate idle
rebuilds, extent node growth, extent cost, both latency ceilings, missed frames and retained resources, and each
must fail at its named predicate. This milestone fixes the measurement semantics and controls; later M9 units
produce the new raw spatial, complexity, continuous and cross-browser samples.
