# Research: Render-Anywhere Scene Protocol

## Decision: Use a deterministic custom TLV durable package format

**Rationale**: The package must be byte-identical for equivalent content, length-skippable for
unknown future data, and independent of CLR/F# union layout. A custom binary TLV format can pin tag
numbers, numeric encodings, list order, and version handling while preserving forward-compatible
inspection.

**Alternatives considered**:

- F# DU auto-serialization: rejected because it pins implementation layout and does not provide
  stable unknown-field skip rules.
- JSON: rejected for durable evidence because canonical float formatting and large binary resource
  payloads add avoidable ambiguity and overhead.
- SkPicture serialization: rejected because it is a Skia implementation artifact, not the public
  cross-version Scene contract.

## Decision: Keep `SceneCodec` dependency-light in `src/Scene`

**Rationale**: `FS.GG.UI.Scene` is the public scene vocabulary and already carries shaped text and
glyph-run proof data. The codec can encode and inspect packages without SkiaSharp, native assets,
browser runtimes, or filesystem access, which keeps package exchange usable in pure tests and
downstream tools.

**Alternatives considered**:

- Put the codec in `SkiaViewer`: rejected because it would force consumers to take native rendering
  dependencies just to inspect a package.
- Put the codec only in the harness: rejected because the portable package is a public contract, not
  a test-only artifact.

## Decision: Represent resources through content-addressed manifest entries

**Rationale**: Local paths are not portable identities. Each external image or font resource needs a
stable `ResourceId`, content hash, kind, required/optional status, byte length, and diagnostics so a
consumer can reject or degrade before rendering. Manifest entries can be sorted by resource id for
deterministic encoding while scene node order remains semantically preserved.

**Alternatives considered**:

- Embed local paths in scene nodes: rejected because package consumers cannot resolve producer
  machine paths and diagnostics would be host-specific.
- Inline every resource in every scene node: rejected because duplicates become ambiguous and large
  packages cannot report resource status centrally.

## Decision: Use Scene capability vocabulary plus protocol feature tags

**Rationale**: `Scene.describe` and `SceneElementKind` already provide a stable starting vocabulary
for drawing capabilities. The protocol adds required/optional capability requirements and target
profile verdicts so inspection can return `accepted`, `accepted-with-degradation`, or `rejected`
before a renderer consumes pixels.

**Alternatives considered**:

- Free-form capability strings only: rejected because spelling drift would weaken compatibility
  checks.
- Renderer-specific capability lists only: rejected because the package needs producer and consumer
  semantics independent of one backend.

## Decision: Preserve shaped text and glyph-run evidence in the package

**Rationale**: Feature 142 already records dependency-light shaped text evidence and `GlyphRunData`.
Portable packages should carry that data so measurement and drawing remain reproducible without
requiring every backend to install the same shaping provider.

**Alternatives considered**:

- Serialize raw text only and shape on each target: rejected because target shaper availability and
  version buckets would change metrics and pixels.
- Convert all text to paths in the package: rejected for the first slice because it discards useful
  text diagnostics and makes resource/font inspection less meaningful.

## Decision: Reference rendering belongs in `SkiaViewer`

**Rationale**: The trusted visual oracle must use the real exhaustive Scene painter and produce a
decodable, non-blank PNG plus metadata. `SkiaViewer` already owns SkiaSharp, bundled fonts, fallback
diagnostics, screenshot/evidence helpers, and host/environment classifications. `Scene` should keep
only pure metadata/hash placeholders and portable package inspection.

**Alternatives considered**:

- Add SkiaSharp to `Scene`: rejected because it violates the dependency-light Scene boundary.
- Treat a metadata hash as reference rendering: rejected because the feature requires real image
  artifacts for accepted evidence.

## Decision: Browser feasibility evaluates a CanvasKit-compatible path first

**Rationale**: CanvasKit gives the closest browser target to the Skia reference oracle. The first
slice should prove package loading, resource/font loading, command mapping, and image comparison for
at least three showcase scenes. If direct reuse of the current .NET/SkiaSharp painter is not
practical, the feasibility path can generate or interpret a CanvasKit command stream from the
portable package and record that decision.

**Alternatives considered**:

- Commit immediately to a production browser backend: rejected because the specification only
  requires evidence and a go/fallback decision.
- Canvas2D first: rejected as the primary candidate because it is lower fidelity for the full Scene
  vocabulary; it remains a possible documented fallback for a core subset.

## Decision: Model I/O workflows with pure update/effect boundaries

**Rationale**: Package inspection is pure, but resource resolution, reference rendering, artifact
writing, browser execution, and diffing are stateful/I/O-bearing. A small MVU-style model, message,
effect, and interpreter boundary makes diagnostics and safe failure testable without hiding I/O in
private helper chains.

**Alternatives considered**:

- Direct imperative orchestration only: rejected because it weakens semantic tests and violates the
  constitution for stateful/I/O workflows.
- Full Elmish runtime dependency in `Scene`: rejected because the pure codec does not need runtime
  commands or subscriptions.
