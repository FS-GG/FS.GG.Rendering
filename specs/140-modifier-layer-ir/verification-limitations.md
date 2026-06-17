# Verification Limitations: Modifier Layer IR Foundation

## Deferred Non-Goals

Feature 140 intentionally does not verify or implement:

- R1b retained-renderer unification.
- Full HarfBuzz shaping, bidi, line breaking, expanded font fallback, or complex-script text layout.
- Overlay interaction state, open/close/dismiss, focus-trap, or keyboard behavior.
- Portable scene serialization or wire protocol.
- Compositor promotion, damage-scissored presentation, texture tiers, or present-path optimization.
- Intrinsic layout protocol.
- Public modifier/layer/portal Scene nodes or public layout containers inside `SceneNode`.

## Known Verification Limits

| Area | Limitation | Attribution |
|---|---|---|
| Fake build wrappers | `./fake.sh build -t PackageSurfaceCheck`, `ControlsRenderingCheck`, and `VerifyPreflight` cannot be run because `./fake.sh` is absent. | Repository/tooling state, not Feature 140 implementation. |
| Package surface tests | `tests/Package.Tests` surface filter expects stale paths (`readiness/surface-baselines/*`, `scripts/controls-prelude.fsx`). | Pre-existing stale package gate. |
| Surface baseline coverage | Current type-name baselines record new Scene types and DU cases but do not expose module function additions such as `Fonts.buildGlyphRunData`. | Baseline granularity limitation; recorded in compatibility plan. |
| Offscreen pixel evidence | Offscreen harness is authoritative for renderer pixels, not desktop visibility, focus, or live input. | Harness scope. |
| Glyph-run proof | Proof data uses deterministic per-character advances and bundled-font fallback. It is not full shaping. | Intentional Feature 140 boundary. |

## Environment Facts

The offscreen rendering harness passed on X11 with Mesa GL:

- Backend: `x11`
- Display: `:1`
- GL renderer: `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)`
- GL version: `4.6 (Compatibility Profile) Mesa 26.1.2-arch1.1`

If a future environment lacks GL/window-system support, record that as an environment limitation and keep deterministic non-GL tests as the primary structural proof.
