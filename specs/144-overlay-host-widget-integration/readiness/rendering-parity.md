# Rendering Parity

Rendering.Harness overlay evidence is deterministic across the direct, retained, cache-enabled, and cache-disabled paths represented by the Feature 144 parity test.

Evidence:

- `tests/Rendering.Harness/Evidence.fs`
- `tests/Rendering.Harness/Input.fs`
- `tests/Rendering.Harness.Tests/Feature144OverlayRenderingParityTests.fs`

The representative corpus contains 100 deterministic overlay scripts with stable unique names.

Feature 140 layer and portal order evidence remains available through the existing internal `Composition` contracts: `paintOrder`, `hitOrder`, `composeLayers`, `LayerHost`, `Portal`, and `LayerComposition`.
