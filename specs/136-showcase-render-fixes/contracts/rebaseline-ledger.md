# Contract — Rebaseline & Disclosure Ledger

Covers FR-012, SC-007. A Tier-1 renderer change moves golden/drift baselines; each MUST be re-established as
an **intended correctness fix** and disclosed. This file is the authoritative record (filled during P-F).

## Baselines expected to change

| Baseline | Path (indicative) | Cause (defect/fix) | Theme(s) | Status |
|---|---|---|---|---|
| G1 Controls Gallery golden evidence | `samples/ControlsGallery/.../golden` | bundled-font text, measurement, clipping | Light/Dark | re-establish |
| G2 Sample Apps golden evidence | `samples/SampleApps/.../golden` | same | Light/Dark | re-establish |
| Rendered-output drift gate | `readiness/...` | renderer output change | all | re-baseline |
| Surface-area baseline — `SkiaViewer` | per-module baseline | new font-registry + measurement-seam surface | n/a | update |
| Surface-area baseline — `Controls` | per-module baseline | new overlay-pass entry | n/a | update |
| Surface-area baseline — `Scene` | per-module baseline | measurement-seam surface (if surfaced) | n/a | update |
| Surface-area baseline — `Layout` | per-module baseline | ScrollViewer viewport surface (if surfaced) | n/a | update |

## Disclosure rule

For each changed baseline record: baseline id, the FR/defect that justifies the change, a one-line
before/after description, and confirmation the change is intended (not incidental). G1/G2 evidence that does
**not** need to change MUST render byte-identical (SC-007).

## Latent drift-gate holes to close if touched

Per repo memory `surface-baseline-gaps` (2026-06-16 audit): `FS.GG.UI.Color` is currently unguarded by the
drift gate and `readiness/surface-baselines/` is missing. If this feature's surface changes touch these,
close the holes in the same change; otherwise note them as out-of-scope follow-ups.

## SC-006 split record

Record the final count of defects fixed at framework vs sample level (see data-model §8): framework owns the
seven defect classes' causes except the chrome-region *sizing*, nav-rail width, and content scroll wiring,
which are sample-level in `Shell.fs`.
