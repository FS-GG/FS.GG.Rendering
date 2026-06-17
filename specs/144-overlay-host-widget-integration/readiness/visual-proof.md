# Visual Proof

No real offscreen GL screenshot is claimed by this local validation record.

Feature 144 records the unsupported-host path explicitly through `Rendering.Harness.Live.overlayVisualLimitation`. The current test covers a no-display host and requires a clear limitation message:

- owner: Rendering.Harness live/offscreen visual proof
- cause: offscreen GL/display host unavailable
- next proof path: run the overlay corpus on a host with display and GL renderer support, then attach the generated visual artifact path
- trust rationale: deterministic coordinator, metadata, dispatch, replay, and parity evidence passed locally; pixel-level overlay proof remains environment-gated

Evidence:

- `tests/Rendering.Harness/Live.fs`
- `tests/Rendering.Harness.Tests/Feature144OverlayVisualProofTests.fs`
