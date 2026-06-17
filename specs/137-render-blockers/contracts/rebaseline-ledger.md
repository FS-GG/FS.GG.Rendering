# Contract — Rebaseline & Disclosure Ledger

Covers FR-010, FR-011 (continuing 136 FR-012/SC-007). A Tier-1 renderer change moves golden/drift baselines;
each MUST be re-established as an intended correctness fix and disclosed. Filled during P-D.

## Baselines actually changed (disclosed, P-D)

| Baseline | Path | Cause (FR / defect→fix) | Before → After | Theme(s) | Intended? |
|---|---|---|---|---|---|
| Surface-area baseline — `Controls` | `tests/surface-baselines/FS.GG.UI.Controls.txt` | FR-006/FR-008 — the US3 scroll-viewport read-back type `Control.ScrollViewport` is new public surface (the overlay-pass entry `Control.isOverlaySurface` and `Control.scrollViewport` are **module members**, so they add no new *type* line — the baseline lists public types). | +1 line: `FS.GG.UI.Controls.ScrollViewport` (inserted in sorted order after `ScatterPlot`). No removals, no other additions. | n/a | ✅ Yes — deliberate, spec-mandated (US2/US3). Regenerated via `scripts/refresh-surface-baselines.fsx`; verified the diff is exactly this one line. |

### Baselines that did NOT change (verified)

- **Surface-area baseline — `Layout`** (`tests/surface-baselines/FS.GG.UI.Layout.txt`): the scroll-viewport
  metric is surfaced from `Control` (computed from the render result), **not** `Layout`, so `Layout`'s public
  surface is unchanged (T021's "only if a metric must be read back from layout" did not trigger). Verified: no
  diff after regeneration.
- **Rendered-output drift gate** (`readiness/parity/screenshots/us1-render-readback.txt`): this artifact is
  regenerated each run by `Lib.Tests` from a **synthetic** capability gallery (paints/shapes/text-runs), not
  from the showcase controls, so the container-clip/overlay/scroll changes do not move its hash. Verified: no
  diff; the full solution suite stays green.
- **G1/G2 golden + per-page evidence**: the showcase evidence under `artifacts/ant-showcase/` is **gitignored**
  (transient verification output, feature-135 harness), so there is no committed golden tree in this repo to
  re-establish. The 19-page re-capture (below) is verification, not a committed baseline.

### Regression oracle (the hard gate) — unchanged and green WITH clipping

- `tests/Controls.Tests/Audit_PictureCache.fs` (3 tests): `cache-on ≡ cache-off` byte-identical, hits=3,
  misses=0, effectiveness margin preserved — **green with clipping enabled** (the feature-136 blocker removed).
  New `Feature137ClippingTests.fs` (9 tests: container clip, full≡retained, scroll viewport, overlay
  z-order/escape-clip/hit-test/parity) all green. Full `Controls.Tests`: 748 passed / 1 skipped, zero
  regressions.

## Disclosure rule

For each changed baseline record: baseline id, the FR/defect that justifies it, a one-line before/after, and
confirmation the change is intended. Evidence that should NOT change MUST render byte-identical (SC-005/006).

## 19-page re-capture (FR-011) — performed (GL available, `DISPLAY=:1`, direct rendering)

Re-captured all 19 pages via `dotnet run --project AntShowcase.App -c Release -- evidence --seed 1` (real GL,
`provesScreenshot=true` for every page). **Determinism (SC-006):** two same-seed runs are byte-for-byte
identical (`diff -r` clean). Artifacts are gitignored (`artifacts/ant-showcase/`), so this is verification,
not a committed baseline.

**Framework-defect classes — confirmed fixed (visible in the GL captures):**
- Container clipping / no-spill: content is confined to its container box (verified structurally + visually).
- Overlay z-order / escape-clip: e.g. the `buttons` page split-button's `SAVE AS / EXPORT` surface floats
  above the flow.
- Scroll viewport: the `layout-containers` scroll-viewer clips its rows to the viewport with an affordance.

**Disclosed remaining item (sample-only, T027 — not a framework defect):** with framework clipping now
active, the AntShowcase **shell chrome bands** (`Shell.fs` app-bar / feedback / status) are revealed to rely on
overflow — the outer vertical `Stack` flex-shrinks them below their content height, so the app-bar's theme
toggle (whose button lays out ~132px tall in a ~32px band) is clipped. This is **correct framework behavior
exposing under-sized sample chrome**, not a renderer defect (clipping never moves a box; the region boxes do
not overlap — verified). A clean fix needs a flex-shrink/grow authoring control (not currently exposed by the
`Attr` API) or a shell-layout redesign; tracked as the optional T027 follow-up and intentionally **not**
bundled here to keep the framework change isolated and the sample's feedback-feature commit clean.
