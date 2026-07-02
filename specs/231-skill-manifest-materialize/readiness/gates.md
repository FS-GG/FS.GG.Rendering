# Feature 231 — release-gate readiness ledger (T021/T022)

**Date**: 2026-07-02 · Template `0.1.61-preview.1` (bumped from `0.1.60-preview.1`;
`FsGgUiVersion` untouched at `0.1.58-preview.1` — no `src/**` change).

## Gate results

- **Package.Tests (release gate)**: `dotnet test tests/Package.Tests -c Release` →
  **177/177 passed** (includes the reworked Feature 204/219 gates and the 10 new Feature 231
  gates: G-MANIFEST ×3, G-PARITY ×4, G-NODANGLE ×2, G-TARGET ×1). Prerequisite: `dotnet build
  FS.GG.Rendering.slnx` first (Surface-baseline tests read Debug assemblies — same order
  release.yml uses).
- **Lifecycle validator, verdict core (env-free)**: OK — 9 framework skill rows (.agents-only,
  copyOnly), 1 ungated manifest row, 1 materialize row, narrowed speckit-* blanket, 10
  workspace rows, 3 product rows.
- **Lifecycle validator, live loop** (`FS_GG_RUN_LIFECYCLE_VALIDATION=1`, real `dotnet new`
  per lifecycle × profile, real `dotnet fsi … --enforce` per spec-kit scaffold): **result:
  pass, provenance: live** — per profile `three-root-mirror=ok (materialized)`,
  `manifest-digests=ok dangling-routes=0`; sdd/none `claude-product-skills=0
  codex-product-skills=0`, `manifest-present=ok`, materialize script absent;
  `enforce-red-case: ok` (corrupted body → non-zero exit naming the skill);
  composition matrix 12/12; unknown value rejected. Report (gitignored, transient):
  `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md`.
- **Template pack**: `dotnet pack .template.package -c Release` succeeds; the nupkg carries
  `content/template/skill-manifest/skill-manifest.json`,
  `content/template/lifecycle/{skill-mirror-vendored.fs, materialize-skill-roots.fsx}`.
- **End-to-end MSBuild proof**: readiness/live-materialize.md (real product build, real NuGet
  restore, incremental second build).

## Environment caveats (disclosed per Feature 168 rules)

- **Sandbox NuGet proxy re-hashes packages**: local `dotnet restore` of the committed
  lockfile graph fails `NU1403` on `FSharp.Core 10.1.301` (cache `contentHash` differs from
  the committed lockfiles' — the sandbox feed serves a re-packed copy). Verified pre-existing
  on the unmodified tree (`git stash` → same failure). Local workaround for the test runs
  only: `dotnet restore --force-evaluate`, then `git checkout` of all 38 rewritten
  `packages.lock.json` (committed lockfiles unchanged; CI restores against the real feed).
  A `.template.package/packages.lock.json` generated during the local pack was deleted for
  the same reason (proxy hashes would fail CI locked restore).
- **Readiness allowlist proof**: `.gitignore` gained
  `!specs/231-skill-manifest-materialize/readiness/**`; `git check-ignore` on these files
  exits 1 (not ignored). The regenerated Feature 204 report stays gitignored by design.

## Deferred (bounded follow-ups, per plan)

- F5 outside skill bodies (e.g. `load-product.fsx` self-reference, `build.fsx` prose
  rewrites) — pre-existing; candidate: delimited substitution token.
- `docs/skillist-reference.md` per-id/profile scoping beyond the Feature 231 text update
  (Feature 219 research R4 deferral stands; the catalog no longer advertises wrapper aliases).
