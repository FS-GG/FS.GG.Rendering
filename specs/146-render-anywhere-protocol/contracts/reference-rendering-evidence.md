# Contract: Reference Rendering Evidence

## Scope

This contract defines the trusted Skia-backed rendering oracle for portable scene packages. The
public rendering edge is owned by `FS.GG.UI.SkiaViewer`; pure package inspection remains in
`FS.GG.UI.Scene`.

## Public Surface

The implementation must add a `ReferenceRendering` surface in `src/SkiaViewer` with a companion
`.fsi`. The surface may wrap lower-level model/message/effect types, but it must expose enough pure
state transition behavior for semantic tests and enough edge behavior for harness execution.

Required contract capabilities:

- Accept portable package bytes or an imported package identity.
- Run package inspection before rendering.
- Resolve required resources through explicit resolver effects.
- Render via the same exhaustive Scene painter used by current Skia evidence paths.
- Persist or return a PNG artifact and metadata.
- Return failed or environment-limited evidence without claiming pass status.

## Evidence Record

A passed evidence record contains:

- Package identity.
- Protocol version.
- Capability profile/verdict summary.
- Resource manifest status.
- Output width and height.
- PNG artifact path or byte payload.
- PNG checksum or equivalent output identity.
- Renderer identity and version bucket where available.
- Verdict `passed`.
- Diagnostics collected during inspection and rendering.

A non-passed evidence record contains:

- Verdict `failed` or `environment-limited`.
- Blocking stage.
- Classification: product defect, unsupported environment, package/resource incompatibility, or
  verification-depth limitation.
- Actionable diagnostics.
- No accepted image artifact.

## Rendering Rules

- Passed evidence requires a decodable, non-blank PNG.
- The renderer must clear and report text fallback disclosure per capture.
- Missing required resources, unsupported required capabilities, and rejected protocol versions
  fail before image acceptance.
- Environment limitations are recorded but excluded from accepted reference evidence counts.

## MVU Boundary

The workflow must be testable as:

- `Model`: package identity, inspection state, resource status, render request, artifact state, and
  diagnostics.
- `Msg`: package parsed, inspection completed, resource resolved, render completed, artifact
  validated, failure classified.
- `Effect`: read package/resource, render image, write artifact, validate PNG.
- `update`: pure transition from message and model to next model plus effects.
- Interpreter: executes effects at the SkiaViewer/harness edge.

## Acceptance Tests

- Valid package produces passed evidence with a real non-blank PNG and all required metadata.
- Missing required resource returns failed evidence and no accepted artifact.
- Unsupported environment returns environment-limited evidence and no accepted artifact.
- The same package rendered twice in the same capable environment reports matching package identity,
  dimensions, protocol version, resource status, and deterministic output identity when the painter
  output is stable.
