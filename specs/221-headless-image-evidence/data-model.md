# Phase 1 Data Model: Headless Image Evidence Path

Entities are drawn from spec.md §Key Entities and grounded in the existing types in `src/Scene/Evidence.fs` / `Evidence.fsi`. This feature **reuses** the existing failure model and adds **no new persisted types** — the only new surface is the injection seam.

## E1 — Scene evidence request

The render input. Already modelled as `SceneEvidenceRequest` (`src/Scene/Evidence.fs`).

| Field | Type | Notes / validation |
|---|---|---|
| `Scene` | `Scene` | The pure scene description to rasterize (`src/Scene/Types.fsi`). |
| `Size` | `Size` (int W/H) | Output dimensions. **Validation**: W>0 ∧ H>0, else `ProductDefect` (existing rule). |
| `Format` | `SceneEvidenceFormat` (`Hash` \| `Png` \| metadata) | This feature changes only the `Png` branch's behavior. |

## E2 — Image evidence artifact

The unit of "pixel proof" — a deterministic PNG. Not a new type: it is the `byte[]` success payload of `renderPng`.

| Property | Constraint |
|---|---|
| Encoding | Valid, decodable PNG (`SKEncodedImageFormat.Png`). |
| Dimensions | Exactly the requested `Size.Width × Size.Height`. |
| Pixel content | Non-blank; depicts the scene's geometry, color, and text (FR-002). |
| Determinism | Byte-for-byte identical for the same `(Scene, Size)` across runs/machines (FR-003). |
| Minimum validity | Never an undersized hash/stub payload presented as success (FR-005/SC-005). |

## E3 — Evidence failure (existing — reused, not redefined)

`SceneEvidenceFailure` at `src/Scene/Evidence.fs:11-19`.

| Field | Type | Notes |
|---|---|---|
| `BlockedStage` | `string` (from `EvidenceStage = Scene \| Renderer`, `Evidence.fs:40-49`) | Names where evidence production stopped. |
| `Classification` | `UnsupportedEnvironment \| ProductDefect` | Environment limitation vs product bug (Principle VI). |
| `DiagnosticCategory` | `string` | Existing diagnostic grouping. |
| `Message` | `string` | Human-readable cause. |

**Classification rules on the PNG path** (FR-005):
- No rasterizer injected / no CPU raster available → `UnsupportedEnvironment`, stage `Renderer`.
- Zero/negative size → `ProductDefect` (existing rule, preserved).
- Very large size exceeding bounds → clear resource diagnostic (classification per cause), never a stub.

## New surface (the only addition)

### Scene rasterizer seam — `src/Scene/Scene.fsi`

```
val setRealPngRasterizer : (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) -> unit
```

- **Default** (no injection): returns `Error (UnsupportedEnvironment, stage Renderer, …)`.
- **Injected** (by `src/SkiaViewer/Fonts.fs`): the SkiaSharp CPU rasterizer (`renderScenePngResult`).
- **Thread-safety**: the injected function must be re-entrant — independent concurrent `renderPng` calls must not interfere and must each stay deterministic (spec Edge Cases — concurrency).

## State / lifecycle

`renderPng` is a single-shot pure-ish transition — no durable state machine, so no Elmish `Model`/`Msg` (Principle IV N/A):

```
renderPng size scene
  └─ Format = Png → call injected rasterizer
       ├─ Ok bytes      → return Ok bytes        (valid PNG; E2 invariants hold)
       └─ Error failure → return Error failure    (E3; write nothing)
  Hash / metadata formats → unchanged (FR-007)
```
