# Contract — Package-surface changes (the single allowed delta)

The surface-drift gate (`SurfaceAreaTests` + `build/Governance/PackageSurface.fs`) reads
`readiness/surface-baselines/*.txt` and pairs each baseline with a package row in
`scripts/refresh-surface-baselines.fsx`. The invariant: **every baseline has a package and every
packed package has a baseline.** This feature changes exactly one package surface.

## Allowed change — remove `FS.GG.UI.Input` (Tier 1)

Both halves land in the **same** change so the gate is never transiently inconsistent:

1. Delete `readiness/surface-baselines/FS.GG.UI.Input.txt` (77 exports).
2. Remove the `"FS.GG.UI.Input", "Input"` row from `refresh-surface-baselines.fsx` (the manifest
   list, ~line 33).

Result: no baseline without a package; no package without a baseline (FR-006, SC-004). Migration
guidance already exists in `docs/bridge/package-deprecation-notice.md` /
`package-identity-migration.md`; `docs/usage.md` inventory drops the line.

## Disallowed changes (must stay byte-identical)

- **No other `*.txt` baseline** under `readiness/surface-baselines/` changes. The harness move and
  the Color/ColorPolicy relocation touch **no** shipped surface.
- **`FS.GG.UI.Color` has no surface-drift baseline** (verified — no `FS.GG.UI.Color.txt`) and **no
  manifest row**, so retiring it requires **zero** surface-gate change (FR-010, SC-004). **Correction
  (found at merge):** `FS.GG.UI.Color` *was* nevertheless a packable/shipped package (`Color.fsproj`
  had `IsPackable=true` / `PackageId`) consumed by 4 samples via inert `PackageReference`s — "no
  surface-drift baseline" ≠ "never packed." Those 4 dead pins were removed during the merge fix
  (samples verified green without them); no surviving package depends on Color. See plan.md "US3
  premise correction."
- `src/ColorPolicy` is `IsPackable=false` ⇒ it is never packed ⇒ it gets **no** baseline and **no**
  manifest row. Adding it must not introduce a new baseline.

## Acceptance

- After US2, the surface-drift gate passes with the `FS.GG.UI.Input` baseline+row gone.
- A diff of `readiness/surface-baselines/` shows **only** `FS.GG.UI.Input.txt` removed — every other
  baseline byte-identical (SC-004).
- The `refresh-surface-baselines.fsx` manifest lists 12 packages (was 13), `FS.GG.UI.Input` absent,
  no other row changed.
