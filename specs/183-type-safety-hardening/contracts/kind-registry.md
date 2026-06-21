# Contract: Control `Kind` Registry (US1 / FR-001) — Tier 2, internal, no bump

## Invariant

One **internal** registry table (`src/Controls/ControlKindRegistry.fs`, keyed by `Control.Kind: string`)
is the single source of truth for all per-kind dispatch. The ~13 parallel `Kind`-keyed sites become
lookups. `Control.Kind` stays a public `string`; **no public surface changes; `FS.GG.UI.Controls` is
not bumped.**

## Must hold

1. **Behavioral equivalence for all ~98 kinds.** For every catalog kind, each migrated site produces the
   **exact** value it produced via the old switch: painter scene, required-attribute diagnostics, chart
   series routing, rich/chart family membership, layout direction, scroll affordance + offset stamping,
   virtualization counts, inspection node-kind / surface-role / clip status, and a11y role. Verified by
   the control scene-hash / fingerprint / inspection / accessibility byte-diff (behavior-invariance §A).
2. **Defaults preserved exactly.** A `tryEntry kind = None` (or a per-field absence) yields the current
   fallthrough: painter → `emptyState`; direction → `Column`; a11y/inspection-kind → `Custom`;
   surface-role → `Content`; scroll/virtualization → no-op. Unknown kinds behave **identically** to today.
3. **Catalog ↔ registry completeness.** A test asserts the registry's keys equal the `Catalog.fs` kind
   set (both directions) — a kind in one but not the other fails the build's tests (SC-001). This is the
   restored "exhaustiveness."
4. **No per-frame allocation regression.** The registry is built once at module load and read by `Map`
   lookup; the hot paths (`paintLeaf`, `RetainedRender.countVirtual`) MUST NOT rebuild it per node.
5. **Compile order.** `ControlKindRegistry.fs` precedes `Control.fs`/`Inspection.fs`/`Accessibility.fs`/
   `Catalog.fs`/`ControlRuntime.fs`/`RetainedRender.fs` in `Controls.fsproj`; no back-edge (FR-011).
6. **Surface untouched.** `FS.GG.UI.Controls.txt` and `Control.fsi`/`Inspection.fsi`/etc. unchanged
   (behavior-invariance §B).

## Explicitly NOT in this story

- Converting `Control.Kind` from `string` to a closed DU (surface-breaking; spec Out of Scope).
- Exposing the registry publicly (internal dispatch only).
- The `Inspection.fs:161` `Kind.Contains("transform")` substring test (not a kind-key lookup — stays inline).
- `popoverGeom`'s `withActions` (handled by US3).

## Retain-per-FR-010 triggers

If folding a particular site into the registry would change its output (e.g. a default that subtly
differs from the table value) or force a back-edge, leave that site as-is and record why; the registry
still subsumes the rest.
