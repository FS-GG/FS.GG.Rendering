# FR-007 non-regression — existing evidence surfaces unchanged (T025)

FR-007: existing scene-evidence consumers and formats (structural **hash**, **metadata**,
**evidence-file** writing) MUST continue to work unchanged.

## What changed vs what did not

The only behavioural change is the **pixel source of `renderPng`**. The `render` function's `Hash`,
`Metadata`, and `EvidencePath`-write branches, and `renderHash`, are **untouched** (`src/Scene/Evidence.fs`):

| Surface | Status | Evidence |
|---|---|---|
| `SceneEvidence.render` `Format = Hash` | unchanged | returns `readback.DeterministicHash`; branch not edited. |
| `SceneEvidence.render` `Format = Metadata` | unchanged | returns `size=…;capabilities=…;hash=…`; branch not edited. Asserted green by `Scene.Tests` "metadata records output size" / "metadata evidence is written to disk". |
| `SceneEvidence.renderHash` | unchanged | thin wrapper over `render`; asserted green by the new `Scene.Tests` "renderHash bytes are deterministic and do not require viewer startup". |
| `EvidencePath` writing (`writeEvidence`) | unchanged | not edited. |
| `Scene.renderReadbackEvidence` (capability hash) | unchanged | not edited; still the viewer-free determinism guarantee. |
| `renderPng` `Format = Png` | **changed (intended, Tier 1)** | pixel source flipped from hash-stub bytes → injected CPU rasterizer; returns typed failure when unproducible. |

## Diff-level confirmation

`git diff src/Scene/Evidence.fs` shows edits confined to: the `realPngRasterizer` seam + setter, and the
`renderPng` body. The `render`/`renderHash`/`writeEvidence`/`LayoutEvidence` functions are not in the diff
(beyond the removed unused `open System.Text`).

## Result: ✅ FR-007 holds

`Scene.Tests` (78 passed) and `Controls.Tests` (949 passed) — every `Hash`/metadata/evidence-file and
retained-render consumer — pass unchanged.
