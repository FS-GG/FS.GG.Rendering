# Implementation Plan: Render-Anywhere Scene Protocol

**Branch**: `146-render-anywhere-protocol` | **Date**: 2026-06-17 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/146-render-anywhere-protocol/spec.md`

## Summary

Define a deterministic, versioned portable scene package for the public `FS.GG.UI.Scene`
contract, add safe package inspection and round-trip diagnostics, produce a real Skia-backed
reference PNG oracle from portable packages, and evaluate a browser-capable rendering candidate
against that oracle. The first slice uses a dependency-light custom TLV-style codec in `src/Scene`,
keeps Skia and filesystem work at the `src/SkiaViewer` and harness edges, records resource and
capability requirements explicitly, and treats browser support as feasibility evidence rather than
a production backend commitment.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`, warnings as errors.

**Primary Dependencies**: Existing `FS.GG.UI.Scene`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Testing`, and
`Rendering.Harness` packages; existing SkiaSharp, SkiaSharp.HarfBuzz, HarfBuzz native assets, and
Silk.NET dependencies at the rendering edge. The Scene codec must remain dependency-light and use
only BCL APIs plus existing Scene types unless implementation research justifies an explicit pinned
package.

**Storage**: Portable scene packages are deterministic binary files/byte arrays using a durable
TLV-style format with a magic header, protocol version, capability profile, scene payload, and
resource manifest. Reference and feasibility evidence are persisted as readiness artifacts under
`specs/146-render-anywhere-protocol/readiness/` and transient run artifacts under `artifacts/`.

**Testing**: Expecto/FsCheck test projects via `dotnet test`; semantic FSI-style tests against
public signatures; rendering evidence through `tests/Rendering.Harness`; public surface checks and
`scripts/refresh-surface-baselines.fsx`.

**Target Platform**: F# library packages on .NET `net10.0`; Skia-backed reference rendering in
capable desktop/headless environments; browser-capable candidate evaluated in a modern browser or
WASM-hosted proof path when available.

**Project Type**: Multi-package F# rendering/UI library plus validation harness.

**Performance Goals**: 50 exports of the same representative scene are byte-identical; accepted
reference rendering produces a non-placeholder PNG and metadata in one harness run; browser
candidate comparison records explicit tolerance and verdict for at least three showcase scenes.

**Constraints**: Public visibility is declared in `.fsi`; no top-level `private`/`internal` in
paired `.fs` files; Scene package cannot depend on SkiaSharp or browser runtimes; local file paths
must not be package resource identities; unsupported versions, capabilities, resources, and
environments must fail safely with actionable diagnostics; I/O-bearing workflows must expose or
wrap an Elmish/MVU-style `Model`/`Msg`/`Effect` boundary.

**Scale/Scope**: Representative corpus covers core drawing, layers/portals, shaped text evidence,
image/font resources, and negative compatibility/resource cases. Browser feasibility covers at
least three representative showcase scenes and ends with either an accepted candidate path or a
documented fallback path.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 because it introduces a durable public scene exchange contract and observable rendering evidence. |
| Spec -> FSI -> semantic tests -> implementation | PASS | New public surfaces are planned first in `.fsi` (`SceneCodec`, reference rendering contracts, harness-facing evidence records), then exercised through semantic tests before `.fs` bodies. |
| Visibility lives in `.fsi` | PASS | Every new public module has a corresponding `.fsi`; implementation files must not add top-level access modifiers. |
| Idiomatic simplicity | PASS | Custom TLV codec is chosen for deterministic skip-ability; no reflection, SRTP, type providers, or non-trivial computation expressions are planned. Any hot-path mutable writer/reader state must carry a one-line reason at the use site. |
| Elmish/MVU boundary for stateful/I/O workflows | PASS | Package inspection is pure; reference rendering, resource resolution, artifact writing, and browser feasibility use explicit model/message/effect records with pure `update` and edge interpreters. |
| Test evidence is mandatory | PASS | Plan requires round-trip, deterministic export, compatibility/resource negative tests, real PNG reference evidence where capable, environment-limited disclosure where not, browser comparison evidence, public surface checks, and package tests. |
| Observability and safe failure | PASS | Diagnostics are part of package inspection, reference evidence, and feasibility reports; unsupported environments are classified separately from product defects. |
| Tier 1 obligations | PASS | `.fsi`, surface baseline, compatibility ledger, migration notes, docs/readiness evidence, and package validation are required. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/146-render-anywhere-protocol/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── browser-feasibility.md
│   ├── portable-scene-package.md
│   └── reference-rendering-evidence.md
├── checklists/
│   └── requirements.md
└── readiness/
    ├── compatibility-ledger.md
    ├── reference/
    ├── roundtrip/
    └── browser/
```

### Source Code (repository root)

```text
src/
├── Scene/
│   ├── Scene.fsi
│   ├── Scene.fs
│   ├── SceneCodec.fsi
│   ├── SceneCodec.fs
│   └── Scene.fsproj
├── SkiaViewer/
│   ├── ReferenceRendering.fsi
│   ├── ReferenceRendering.fs
│   ├── SceneRenderer.fs
│   ├── SkiaViewer.fsi
│   ├── SkiaViewer.fs
│   └── SkiaViewer.fsproj
└── Testing/
    ├── Testing.fsi
    ├── Testing.fs
    └── Testing.fsproj

tests/
├── Scene.Tests/
│   ├── Feature146PortableSceneRoundTripTests.fs
│   ├── Feature146PortableSceneCompatibilityTests.fs
│   ├── Feature146PortableSceneResourceTests.fs
│   ├── Feature146PackageCapabilityInspectionTests.fs
│   ├── Feature146PackageResourceInspectionTests.fs
│   └── Scene.Tests.fsproj
├── SkiaViewer.Tests/
│   ├── Feature146ReferenceRenderingTests.fs
│   └── SkiaViewer.Tests.fsproj
├── Rendering.Harness/
│   ├── RenderAnywhere.fsi
│   ├── RenderAnywhere.fs
│   ├── Evidence.fsi
│   ├── Evidence.fs
│   ├── Cli.fs
│   └── Rendering.Harness.fsproj
├── Rendering.Harness.Tests/
│   ├── Feature146RenderAnywhereEvidenceTests.fs
│   ├── Feature146BrowserFeasibilityTests.fs
│   ├── Feature146BrowserEvidenceFormatterTests.fs
│   └── Rendering.Harness.Tests.fsproj
└── Package.Tests/
    ├── FsiTranscriptCoverageTests.fs
    ├── Feature146CompatibilityLedgerTests.fs
    ├── SurfaceAreaTests.fs
    └── Package.Tests.fsproj
```

**Structure Decision**: The portable protocol belongs in `src/Scene` because it is the
dependency-light public Scene contract. Skia-backed reference rendering belongs in `src/SkiaViewer`
because it already owns the exhaustive `SceneNode -> SKCanvas` painter and native dependencies.
Corpus orchestration, browser feasibility, real artifact validation, and readiness output belong in
`tests/Rendering.Harness`, with package/surface evidence in the existing test projects.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Durable package format: custom deterministic TLV, not F# DU auto-serialization, JSON, or SkPicture.
- Resource identity: content-addressed manifest entries, no local machine paths.
- Capability profile: Scene element vocabulary plus protocol feature tags, with explicit required and optional requirements.
- Text portability: existing shaped text and `GlyphRunData` payloads are preserved in the package.
- Reference oracle: Skia-backed PNG/evidence through `SkiaViewer`, not a Scene package placeholder.
- Browser feasibility: CanvasKit-compatible command stream/proof first, with documented fallback when capability or packaging fails.
- I/O workflow modeling: explicit model/message/effect boundary for reference and feasibility interpreters.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Public interface contracts:

- [Portable Scene Package Contract](contracts/portable-scene-package.md)
- [Reference Rendering Evidence Contract](contracts/reference-rendering-evidence.md)
- [Browser Feasibility Contract](contracts/browser-feasibility.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Tier 1 artifact chain | PASS | Contracts specify `.fsi` surfaces, semantic tests, surface baseline refresh, compatibility ledger, and docs/readiness output. |
| Dependency boundaries | PASS | `SceneCodec` remains Skia-free; Skia/browser/native concerns stay in `SkiaViewer` and harness projects. |
| Determinism and safe failure | PASS | Contracts require canonical ordering/encoding, explicit package statuses, resource diagnostics before render acceptance, and no accepted misleading artifacts. |
| Real evidence and synthetic disclosure | PASS | Quickstart requires real reference PNG artifacts in capable environments and environment-limited records otherwise; browser feasibility records pass/fail/fallback instead of overclaiming. |
| MVU/I/O boundary | PASS | Contracts define pure decisions plus effect/interpreter responsibilities for package inspection, reference rendering, artifact writing, and browser feasibility. |

No constitution violations are introduced by the design.

## Complexity Tracking

No constitution violations require justification.
