# Contract: Damage-Scissored Render Path

## Runtime Surface

The live implementation is owned by `src/SkiaViewer/Host/OpenGl.fs` in the
`DirectToSwapchain` path. Public or package-visible diagnostics, if needed, must be declared in
`src/SkiaViewer/Host/OpenGl.fsi`, `src/SkiaViewer/CompositorProof.fsi`, or `src/SkiaViewer/SkiaViewer.fsi`
before implementation.

## Eligibility Inputs

- Active host profile.
- Feature 155-compatible proof readiness.
- Current run identity.
- Previous frame identity.
- Trusted retained frame state.
- Framebuffer size.
- Damage region.
- Resource availability.
- Full-redraw parity oracle availability.

## Decision Outputs

- `damage-scoped-accepted`
- `full-redraw`
- `skip-no-change`
- `rejected`
- `environment-limited`

Every non-accepted decision includes a primary fallback reason.

## Damage-Scoped Behavior

When eligible, the host must:

1. Avoid clearing the whole frame.
2. Ensure previous content outside damage is trusted for the buffer being presented.
3. Clip repaint to the validated damage union or equivalent native scissor region.
4. Repaint damaged pixels from the new scene.
5. Flush and present normally.
6. Refresh retained backing identity for later frames.
7. Emit diagnostics naming the decision, damage area, retained backing, and proof gate.

## Full-Redraw Behavior

When ineligible, the host must:

1. Use the existing full clear plus full scene paint path.
2. Record the fallback reason.
3. Avoid publishing accepted damage-scoped artifacts.
4. Keep Feature 155 proof and Feature 156 timing evidence intact.

## Safety Invariants

- Damage-scoped repaint never runs without accepted same-profile proof.
- Damage-scoped repaint never runs without trusted retained previous content.
- Resize and full-frame invalidation use full redraw.
- Resource failures use full redraw.
- Parity mismatch rejects the attempt and future attempts until fresh proof and backing are present.
- Unsupported hosts remain fail-closed.

## Compatibility

If this feature adds public result or diagnostic types, package compatibility notes and
`readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt` must be updated. If the implementation stays
internal and only readiness output changes, the compatibility ledger must state that no public API
surface changed.
