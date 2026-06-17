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

---

## Realized outcome (filled during implementation, 2026-06-17)

### Implemented (committed, fully tested, zero regressions across Scene/SkiaViewer/Controls/Layout suites)

| Defect class | Fix (framework) | Tasks | Disclosure / before→after |
|---|---|---|---|
| **wrong-glyph** (`@`→`7`, all-caps) | Bundled standard font set embedded in `FS.GG.UI.SkiaViewer`; `Fonts` registry resolves real typefaces via `SKTypeface.FromStream` with a per-character fallback chain; `SceneRenderer.drawText` draws per-character, mixed case preserved; tofu = unambiguous box, never a plausible-wrong glyph | T001–T004, T011–T014, T017 | before: host `SKTypeface.Default` (empty in sandbox) → whole string via 5×7 bitmap, uppercased, `@`→`7`-wildcard. after: real Noto/Inter/JetBrains/DejaVu glyphs, host-independent. **Intended.** |
| **truncated-text** (`Stable`→`STABL`) | `Scene` measurement seam (`setRealTextMeasurer`/`measureTextResolved`); `Fonts.realMeasure` sums the same per-char advances the renderer draws → size == draw; pure heuristic kept conservative for pure callers | T015, T016 | before: box sized at 0.58·size, drawn at 0.857·size → clip. after: box sized from real advances → no clip. **Intended.** |
| **composite-structure** (data-grid/menu/descriptions/QR/charts) | `directionOf` data-grid→Row; `rowsGeom` min row height + clip; `descriptionsGeom` box-scaled + clip; `qrCodeGeom` min module grid + clip; chart bodies clipped + `chartValues` finite-guard | T031–T035 | before: stacked grid, collapsed rows, overflowing descriptions, blank QR, overrunning/NaN charts. after: table, distinct rows, in-box descriptions, populated QR, clipped degenerate-safe charts. **Intended.** |
| **text overflow** (residual) | `ControlInternals.ellipsize` — explicit `…` instead of silent clip | T010A, T016A | before: over-long labels hard-clipped. after: ellipsized. **Intended.** |

### Surface-area baselines updated (T040)

- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` — **+9 public types**: `Fonts` (+`FallbackResolution`/`Authored`/`Substituted`/`Tofu`/`FallbackReport`/`ResolvedChar`) and `Text`. Regenerated via `scripts/refresh-surface-baselines.fsx`. **Intended (new font-registry + text-seam surface).**
- `Scene` baseline **unchanged**: the seam additions (`setRealTextMeasurer`, `measureTextResolved`) are module functions, not new types (the type-level baseline does not track them; the `.fsi` is the binding contract).
- `Controls` baseline **unchanged**: `ellipsize`, `chartValues`, etc. live in `module internal ControlInternals` (not public surface).

### Deferred (NOT shipped — blocked by a real cross-cutting constraint; recorded honestly)

| Item | Tasks | Why deferred |
|---|---|---|
| **Container/child clipping** | T025 | Wrapping a container's children in `Scene.clipped` (in `renderTree` + the retained path's `SubtreeScene` assembly) breaks the **feature-116/120 picture-cache parity** invariants (`cache-on ≡ cache-off`, present-but-dead/effectiveness) because cached data-grid-row picture boundaries become nested inside a clip group. Verified: 3 picture-cache parity tests fail; reverted rather than ship breakage. Needs coordinated work in `RetainedRender`'s cache fingerprint/effectiveness logic. |
| **Real overlay pass** (z-top deferral, hit-test) | T022–T024 | Requires a *global* overlay-deferral during paint that must be mirrored into the **bottom-up, incremental** retained build to keep full≡retained parity; not expressible in the per-node fragment model without the same picture-cache coordination. Note: in this schematic renderer transient surfaces (combo/menu/date-picker) already render as self-contained leaf schematics, so the in-flow overprint defect does not reproduce on the showcase pages. |
| **ScrollViewer real viewport** | T036–T038 | Depends on container/child clipping (above). |
| **Flex explicit-basis split** | T026 | The flex engine already honours explicit `Size`/`FlexBasis` (`preferredMainSize`) and `FlexGrow`; the uniform split is only an all-unspecified fallback. The region-overlap fix is therefore predominantly the sample `Shell.fs` sizing (T039), not a framework change. |
| **Sample `Shell.fs` region sizing** | T039 | Sample-level; the showcase `Shell.fs` already carries uncommitted in-progress edits outside this change's scope. |
| **Region/overlay/container semantic tests** | T018–T020 | Track the deferred framework pieces above. |
| **G1/G2 golden + drift re-baseline; 19-page re-capture; docs** | T041, T042, T044–T047 | See verification note below. |

### SC-006 split (realized)

Framework fixes shipped: **wrong-glyph, all-caps, truncated-text, composite-structure (5 of 7 classes)** — all at the renderer/scene/control layer, benefiting every consumer. Deferred (framework): overlay-overprint, control/region-overlap, unbounded-content/scroll. Sample-level (`Shell.fs`): chrome-region sizing — deferred/out of this change's scope.

### Verification note (T045)

The G1/G2 golden + drift re-baseline and the 19-page re-capture were **not** run here: the re-capture is the verification vehicle for the deferred layout/overlay/scroll classes, and re-baselining is only honest once those land. The shipped text + composite fixes are verified by the new framework semantic suites (glyph correctness, determinism, disclosure, measure/draw agreement, overflow, table/rows/descriptions/QR/chart) — all green — rather than a fabricated visual pass. A no-fabrication degrade, per Principle V.
