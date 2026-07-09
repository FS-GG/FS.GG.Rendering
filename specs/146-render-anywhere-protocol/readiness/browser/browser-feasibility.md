# Feature 146 Browser Capability

No candidate image is rendered and no perceptual diff is computed. This report records which
corpus scenes have passing reference evidence, and why the browser candidate did not run. It
is NOT cross-backend fidelity evidence.

- candidate-backend: canvaskit-command-stream/proof
- comparison: not performed
- decision: fallback: Continue with a generated CanvasKit command-stream proof; do not claim a production browser backend yet.

## Scenarios
- basic-primitives: candidate-not-executed
  package: sha256:bfb3dcab66a4b44e4471cc1df041c946a2e86c7c8f6ed664e817c4dc85e28c7f
  reference: sha256:0d1c1f23a155622228e531f83eb9f9ea617c6f2a7ed1bb275abd4b55a285f3fe
- layered-portal: candidate-not-executed
  package: sha256:8f712520e8269655f6d78c2504ee8a197c7f67479a43a3bc1b25bd19f241f89a
  reference: sha256:6991f976ee155afaea60d83e0956a1dd15deebe3351f6e5f83e737906da15ec8
- shaped-text: candidate-not-executed
  package: sha256:499c3eb3bd5ba642f1d990dbaaabf6c77c66dd85545e65674983e3e5ff8f6796
  reference: sha256:c1ae09e80e13cb304698abe40553e8027e53c1474e2cef49b2cc72f62f76a5a5

## Unsupported Capabilities
- direct browser execution unavailable in current harness

## Diagnostics
- This is a capability report, not a perceptual diff: no candidate image is produced, so no image is compared.
- Cross-backend visual fidelity is UNPROVEN and this report is not evidence of it.
