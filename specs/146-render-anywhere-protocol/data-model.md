# Data Model: Render-Anywhere Scene Protocol

## Portable Scene Package

**Purpose**: Durable exchange unit that can be exported, imported, inspected, rendered by the
reference path, and evaluated by browser candidates.

**Fields**:

- `ProtocolVersion`: major/minor compatibility identity.
- `Profile`: durable protocol profile for cross-version/cross-backend exchange.
- `Capabilities`: required and optional `CapabilityRequirement` entries.
- `ScenePayload`: deterministic ordered scene graph payload.
- `ResourceManifest`: external image/font/resource table.
- `PackageIdentity`: deterministic checksum over canonical package bytes.

**Validation rules**:

- Must begin with the protocol magic header and supported major version.
- Must not contain timestamps, random ids, local paths as resource identities, or host-specific
  absolute paths.
- Equivalent scene content under the same protocol version must produce identical bytes.
- Scene node order is semantic and must be preserved.
- Unknown optional TLV tags may be skipped; unknown required tags reject inspection.

## Protocol Version

**Fields**:

- `Major`: breaking compatibility version.
- `Minor`: additive compatible version.

**Validation rules**:

- Newer major versions are rejected before rendering.
- Newer minor versions may be accepted only when all required tags and capabilities are supported.
- Diagnostics must name the producer version and the consumer support range.

## Capability Requirement

**Fields**:

- `CapabilityId`: stable capability identifier derived from Scene element vocabulary or protocol
  feature tags.
- `RequirementLevel`: required or optional.
- `DegradationPolicy`: reject, degrade, or ignore when unsupported.
- `AffectedScenePaths`: deterministic scene payload paths where the capability is used.

**Validation rules**:

- Required unsupported capabilities reject rendering.
- Optional unsupported capabilities may degrade only when the package states the policy and the
  consumer reports the degradation.
- Capability lists are encoded canonically, but scene payload paths preserve authored order.

## Resource Manifest

**Fields**:

- `Entries`: sorted list of `ResourceEntry`.
- `ManifestIdentity`: checksum over canonical entries.

**Validation rules**:

- Required resource ids must be unique.
- Entries are sorted by `ResourceId` for deterministic package bytes.
- Manifest identity changes when any entry content hash, kind, status, or required flag changes.

## Resource Entry

**Fields**:

- `ResourceId`: stable package-local identity.
- `Kind`: image, font, or future length-skippable resource kind.
- `ContentHash`: canonical hash of the resource bytes or declared external payload.
- `ByteLength`: expected byte length when bytes are available.
- `Required`: true when rendering must reject if unavailable.
- `MediaType`: stable descriptive type when known.
- `SourceLabel`: optional non-authoritative label for diagnostics, never a local path identity.

**Validation rules**:

- Missing, corrupted, duplicated, or hash-mismatched required entries reject accepted rendering.
- Missing optional entries can produce accepted-with-degradation only when the scene capability
  policy allows it.
- Consumers must report resource availability before accepting a rendering result.

## Portable Scene Payload

**Fields**:

- `Nodes`: ordered canonical scene node records.
- `NodePath`: deterministic path from the package root to each node.
- `NodeTag`: stable protocol tag for the Scene node kind.
- `NodePayload`: canonical primitive data for the node.
- `Children`: ordered child payloads.

**Validation rules**:

- Numeric values use the protocol canonical representation.
- Recursive depth and payload sizes are bounded by consumer limits with explicit diagnostics.
- `Image` payloads reference `ResourceId`, not producer machine paths.
- High-level nodes that cannot be represented by the target profile must be lowered or reported as
  unsupported before rendering.

## Shaped Text Payload

**Fields**:

- `GlyphRunData`: existing dependency-light glyph-run proof data.
- `ProviderEvidence`: provider id, version bucket, availability, and fallback mode.
- `TextDiagnostics`: fallback and shaping diagnostics.

**Validation rules**:

- Glyph order, cluster data, advances, positions, fallback mode, and fingerprint are preserved.
- A target that cannot draw glyph runs must reject or degrade according to capability policy.
- Raw text may be retained for diagnostics, but rendering acceptance is based on shaped payload
  consistency.

## Package Inspection Report

**Fields**:

- `Status`: accepted, accepted-with-degradation, or rejected.
- `ProtocolVerdict`: version compatibility details.
- `CapabilityVerdicts`: supported, degraded, or unsupported capability results.
- `ResourceVerdicts`: available, missing, corrupted, mismatched, or optional-unavailable results.
- `Diagnostics`: actionable messages with severity and stage.

**Validation rules**:

- Inspection runs before rendering.
- Rejected packages cannot produce accepted reference evidence.
- Accepted-with-degradation evidence must name every degraded capability or resource.

## Reference Rendering Evidence

**Fields**:

- `PackageIdentity`: checksum of canonical package bytes.
- `ProtocolVersion`: version used for inspection/rendering.
- `CapabilityProfile`: accepted/degraded capability summary.
- `ResourceStatus`: manifest availability summary.
- `OutputSize`: image dimensions.
- `ImagePath`: readiness or artifact path for PNG output.
- `ImageIdentity`: checksum over accepted PNG bytes.
- `Verdict`: passed, failed, or environment-limited.
- `Diagnostics`: rendering and environment messages.

**Validation rules**:

- Passed evidence must include a decodable, non-blank PNG artifact.
- Missing resources, unsupported required capabilities, and unsupported protocol versions fail
  safely without accepted artifacts.
- Environment-limited results are not counted as accepted reference evidence.

## Backend Feasibility Report

**Fields**:

- `CandidateBackend`: browser candidate identity, such as CanvasKit command stream.
- `Corpus`: list of package identities and scenario names.
- `Tolerance`: explicit image comparison tolerance.
- `Comparisons`: per-scene reference identity, candidate identity, diff metric, and verdict.
- `UnsupportedCapabilities`: missing browser capabilities by scene.
- `Decision`: accepted candidate path or documented fallback path.
- `Diagnostics`: browser execution, resource loading, and comparison details.

**Validation rules**:

- At least three representative showcase scenes are evaluated.
- Each comparison is traceable to a reference rendering evidence record.
- The report must end with exactly one decision: accepted candidate path or documented fallback path.

## Compatibility Ledger

**Fields**:

- `PublicSurfaceChanges`: changed modules/types/functions and baseline references.
- `MigrationGuidance`: downstream actions for existing `Scene.Image`, text, resource, or evidence
  consumers.
- `IntentionalLimitations`: bounded follow-ups and unsupported profiles.
- `EvidenceLinks`: round-trip, reference, browser, and package validation artifacts.

**Validation rules**:

- Required before implementation readiness is claimed.
- Must be updated when `.fsi`, public package behavior, or rendering behavior changes.

## State Transitions

```text
Export:
Scene + resource resolver
  -> PortableScenePackage
  -> deterministic package bytes
  -> package identity

Import/Inspect:
Package bytes
  -> parsed package or malformed diagnostic
  -> version/capability/resource inspection
  -> accepted | accepted-with-degradation | rejected

Reference Render:
Accepted package
  -> resource resolution
  -> Skia-backed render attempt
  -> passed PNG evidence | failed diagnostic | environment-limited record

Browser Feasibility:
Package + reference evidence
  -> browser candidate render
  -> image comparison
  -> per-scene verdict
  -> accepted candidate path | documented fallback path
```
