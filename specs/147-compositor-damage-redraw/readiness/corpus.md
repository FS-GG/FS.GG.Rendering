# Feature 147 Compositor Corpus

## Target Host Profiles

- `x11-opengl-direct`: X11, OpenGL, direct swapchain, framebuffer `640x480`, scale `1.0`.
- `headless-offscreen`: no display or headless GL, offscreen/readback observation, framebuffer `640x480`, scale `1.0`.
- `unsupported-display`: missing or unsupported display, no accepted present-preservation claim.

## Stable Scenario Identifiers

- `proof/sentinel-damage-v1`
- `damage/idle`
- `damage/localized-update`
- `damage/overlap`
- `damage/frame-edge`
- `damage/full-frame-invalidation`
- `promotion/stable-boundary`
- `promotion/placement-only-move`
- `promotion/content-change`
- `promotion/churn`
- `snapshot/expensive-stable`
- `snapshot/simple-overhead`
- `snapshot/over-budget`

## Thresholds

- Damage parity: 100% full-redraw oracle parity.
- Promotion benefit: repeated visual work reduction at least 30%.
- Non-beneficial overhead: simple and churning scenes within 5% of lower tier or demoted.
- Snapshot benefit: at least 20% frame-cost improvement on expensive stable scenes.

## Snapshot Resource Budget

- Entries: 64 retained snapshot candidates.
- Bytes: 32 MiB deterministic estimate.
- Invalid or over-budget resources refresh, evict, demote, or fall back before reuse.

## Artifact Paths

- Present proof: `specs/147-compositor-damage-redraw/readiness/present-proof/`
- Parity: `specs/147-compositor-damage-redraw/readiness/parity/`
- Performance: `specs/147-compositor-damage-redraw/readiness/perf/`
- Package summary: `specs/147-compositor-damage-redraw/readiness/validation-summary.md`
- Compatibility ledger: `specs/147-compositor-damage-redraw/readiness/compatibility-ledger.md`

## Skipped Environment Categories

- Missing display server.
- GL context unavailable.
- Readback unsupported or blocked.
- Host profile changed since proof.
- Synthetic-only observation.
