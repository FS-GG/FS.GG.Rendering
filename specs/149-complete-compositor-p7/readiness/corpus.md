# Feature 149 Compositor Corpus

## Target Host Profiles

- `x11-opengl-direct`: X11, OpenGL, direct swapchain, framebuffer `640x480`, scale `1.0`, package version recorded with proof.
- `headless-offscreen`: headless/offscreen OpenGL readback, framebuffer `640x480`, scale `1.0`, package version recorded with proof.
- `unsupported-display`: missing or unsupported display, no accepted live-preservation claim.
- `synthetic-non-preserving`: simulated host used only by Synthetic-named tests; never accepted as real proof.
- `feature149-capable-host-candidate`: capable-host candidate profile used to name the required final three-run acceptance path.

## Live Proof Scenarios

- `proof/live-sentinel-damage-v1`
- `proof/capable-host-three-run`
- `proof/non-preserving-host`
- `proof/stale`
- `proof/host-mismatch`
- `proof/algorithm-mismatch`
- `proof/missing-artifact`
- `proof/blank-artifact`
- `proof/synthetic-only`
- `proof/missing-display`
- `proof/unsupported-readback`
- `proof/timeout`
- `proof/permission`
- `proof/host-error`

## Damage Parity Scenarios

- `damage/idle`
- `damage/localized-update`
- `damage/overlap`
- `damage/frame-edge`
- `damage/movement-old-new`
- `damage/resize`
- `damage/theme-global`
- `damage/zero-damage`
- `damage/stale-proof`
- `damage/disabled`
- `damage/unsupported`
- `damage/resource-failure`
- `damage/internal-error`
- `damage/parity-failure`

## Reuse Scenarios

- `reuse/stable-boundary`
- `reuse/placement-only`
- `reuse/mixed-change`
- `reuse/no-change`
- `reuse/content-changing`
- `reuse/churning`
- `reuse/no-benefit`
- `reuse/failed-parity`
- `reuse/same-seed`

## Snapshot Scenarios

- `snapshot/expensive-stable`
- `snapshot/create-reuse-refresh`
- `snapshot/replacement-eviction-disposal`
- `snapshot/simple-scene`
- `snapshot/churning`
- `snapshot/over-budget`
- `snapshot/stale-resource`
- `snapshot/invalid-resource`
- `snapshot/unsupported-host`
- `snapshot/parity-failure`

## Timing Tiers

- `timing/damage`
- `timing/placement`
- `timing/replay`
- `timing/snapshot`

## Thresholds

- Damage parity: 100% full-frame oracle parity before accepting a tier.
- Placement/reuse: at least 30% repeated-work reduction on moving/placement corpus.
- Simple/churning overhead: no more than 5% overhead before demotion.
- Snapshot: at least 20% frame-cost improvement over replay/lower-tier baseline.
- Timing probes separate warmup frames from measured frames.

## Snapshot Resource Budget

- Entries: 64 retained snapshot candidates.
- Bytes: 32 MiB deterministic estimate.
- Invalid, stale, host-mismatched, or over-budget snapshots refresh, evict, demote, dispose, or fall back before reuse.

## Artifact Paths

- Live proof: `specs/149-complete-compositor-p7/readiness/live-proof/`
- Parity: `specs/149-complete-compositor-p7/readiness/parity/`
- Reuse: `specs/149-complete-compositor-p7/readiness/reuse/`
- Snapshots: `specs/149-complete-compositor-p7/readiness/snapshots/`
- Timing: `specs/149-complete-compositor-p7/readiness/timing/`
- Package summary: `specs/149-complete-compositor-p7/readiness/validation-summary.md`
- Compatibility ledger: `specs/149-complete-compositor-p7/readiness/compatibility-ledger.md`
