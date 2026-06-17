# Feature 145 Scope Review

Decision: Tier 2 scope preserved.

Feature 145 changed harness/readiness evidence and AntShowcase reference evidence only. It did not change product-facing overlay behavior, public control APIs, portable scene serialization, browser rendering, compositor behavior, layout, text shaping, text editing, selection editing, or widget catalog behavior.

Touched source areas:

- `tests/Rendering.Harness/Evidence.fsi`
- `tests/Rendering.Harness/Evidence.fs`
- `tests/Rendering.Harness/Live.fsi`
- `tests/Rendering.Harness/Live.fs`
- `tests/Rendering.Harness/Cli.fs`
- `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`
- `samples/AntShowcase/AntShowcase.Core/Evidence.fs`
- `samples/AntShowcase/AntShowcase.Tests/Feature145OverlayVisualProofTests.fs`

Explicitly untouched Tier 1 surfaces:

- `src/Testing`
- `src/SkiaViewer`
- public package `.fsi` files under `src/`
- surface baselines

No Tier 1 reclassification is required.
