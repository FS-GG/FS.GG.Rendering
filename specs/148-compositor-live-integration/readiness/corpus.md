# Feature 148 Compositor Corpus

## Target Host Profiles

- `x11-opengl-direct`: X11, OpenGL, direct swapchain, framebuffer `640x480`, scale `1.0`, package version recorded with proof.
- `headless-offscreen`: headless/offscreen OpenGL readback, framebuffer `640x480`, scale `1.0`, package version recorded with proof.
- `unsupported-display`: missing or unsupported display, no accepted live-preservation claim.
- `synthetic-non-preserving`: simulated host used only by Synthetic-named tests; never accepted as real proof.

## Live Proof Scenarios

- `proof/live-sentinel-damage-v1`
- `proof/non-preserving-host`
- `proof/stale`
- `proof/host-mismatch`
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
- `damage/stale-proof`
- `damage/disabled`
- `damage/unsupported`
- `damage/parity-failure`

## Reuse Scenarios

- `reuse/stable-boundary`
- `reuse/moving-only`
- `reuse/scrolling`
- `reuse/content-changing`
- `reuse/theme-resource-change`
- `reuse/churning`
- `reuse/no-benefit`
- `reuse/failed-parity`
- `reuse/same-seed`

## Snapshot Scenarios

- `snapshot/expensive-stable`
- `snapshot/simple-scene`
- `snapshot/churning`
- `snapshot/over-budget`
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
- Placement/reuse: at least 30% repeated-work reduction on moving/scrolling corpus.
- Simple/churning overhead: no more than 5% overhead before demotion.
- Snapshot: at least 20% frame-cost improvement over replay/lower-tier baseline.
- Timing probes separate warmup frames from measured frames.

## Snapshot Resource Budget

- Entries: 64 retained snapshot candidates.
- Bytes: 32 MiB deterministic estimate.
- Invalid, stale, host-mismatched, or over-budget snapshots refresh, evict, demote, dispose, or fall back before reuse.

## Artifact Paths

- Live proof: `specs/148-compositor-live-integration/readiness/live-proof/`
- Parity: `specs/148-compositor-live-integration/readiness/parity/`
- Reuse: `specs/148-compositor-live-integration/readiness/reuse/`
- Snapshots: `specs/148-compositor-live-integration/readiness/snapshots/`
- Timing: `specs/148-compositor-live-integration/readiness/timing/`
- Package summary: `specs/148-compositor-live-integration/readiness/validation-summary.md`
- Compatibility ledger: `specs/148-compositor-live-integration/readiness/compatibility-ledger.md`
