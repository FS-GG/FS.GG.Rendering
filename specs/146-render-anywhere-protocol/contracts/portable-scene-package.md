# Contract: Portable Scene Package

## Scope

This contract defines the public package exchange surface owned by `FS.GG.UI.Scene`. The contract is
pure and dependency-light: it must not depend on SkiaSharp, browser runtimes, windowing packages, or
filesystem-specific resource paths.

## Public Surface

The implementation must add a public `SceneCodec` module in `src/Scene/SceneCodec.fsi` and include
it in `src/Scene/Scene.fsproj` before `SceneCodec.fs`.

Required contract capabilities:

- Export a `Scene` plus a resource manifest/resolver description to deterministic package bytes.
- Import package bytes into a portable package model or a malformed-package diagnostic.
- Inspect package bytes without rendering and return accepted, accepted-with-degradation, or
  rejected status.
- Compare an imported/restored scene with the original through semantic diagnostics.
- Compute a package identity from canonical bytes.

## Wire Contract

- Durable profile packages use a binary TLV-style encoding.
- Header includes magic token `FSGGSCENE`, protocol major/minor, and profile id.
- Tags are stable numeric identifiers documented in source comments and tests.
- Unknown optional tags are skipped by length; unknown required tags reject the package.
- Scene node order is encoded exactly as authored.
- Manifest entries are encoded in canonical `ResourceId` order.
- Numeric encodings, enum values, string encoding, and list ordering are canonical and tested.

## Versioning Rules

- Consumer supports protocol `1.x` for the first implementation slice.
- Newer major versions reject before rendering.
- Newer minor versions are accepted only if every required tag and required capability is supported.
- Rejection diagnostics name producer version, consumer support range, and the first blocking field.

## Resource Rules

- `Image` and font-like resources are represented by manifest entries and scene references to
  `ResourceId`.
- A `ResourceId` is stable inside the package and independent of producer local paths.
- Required resources must be available, hash-matched, and kind-matched before accepted rendering.
- Optional resources may degrade only when package capability policy permits it.

## Text Rules

- Shaped text evidence and `GlyphRunData` must round-trip through the package.
- Raw text may remain diagnostic data; portable rendering relies on shaped/glyph-run evidence when
  present.
- Provider evidence, fallback mode, glyph order, cluster data, advances, and fingerprints must be
  preserved.

## Diagnostics

Inspection diagnostics must include:

- Stage: parse, version, capability, resource, scene, or text.
- Severity: info, warning, error, or fatal.
- Actionable message.
- Optional scene path, capability id, or resource id.

## Acceptance Tests

- Representative corpus round-trips with no semantic mismatches.
- 50 repeated exports of the same representative scene are byte-identical.
- Newer major version, unknown required tag, missing required resource, resource hash mismatch, and
  unsupported required capability all reject before rendering.
- Unknown optional tags are skipped and reported without corrupting known content.
