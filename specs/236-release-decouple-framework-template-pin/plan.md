# Plan: release framework@pin, template@tag; guard the release lane

**Issue**: FS-GG/FS.GG.Rendering#48 · **Spec**: [`spec.md`](./spec.md) · **Research**: [`research.md`](./research.md)

## Technology & constraints

- GitHub Actions (`.github/workflows/release.yml`), bash steps, `dotnet pack/restore/nuget push`.
- F# script guard `scripts/validate-version-coherence.fsx` (run via `dotnet fsi`) + its Expecto
  mirror `tests/Package.Tests/Feature209VersionCoherenceTests.fs`. No new dependencies; reuse the
  script's preview-aware `SemVer` comparator.
- **Behavior-preserving for a framework release** (pin == `$VER`): both packs collapse to the
  current coherent-set publish. Only template-only releases (pin ≠ `$VER`) change.
- No version bump, no tag, no registry flip (out of scope).

## Approach

1. **release.yml `template-product-tests`** (FR-001): replace the `$VER`-from-tag `ver` step with a
   step that reads `<FsGgUiVersion>` from `template/base/Directory.Packages.props`; pack the slnx at
   that pin into the runner-local feed. Update the surrounding comments to state the local feed
   carries **pin** bits (what the product restores), not `$VER`.
2. **release.yml `publish-packages`** (FR-002): keep the `$VER` resolution for the template package;
   add a pin read; pack `FS.GG.Rendering.slnx` at the pin and `.template.package/…fsproj` at `$VER`;
   push both. Update comments to name the two axes.
3. **guard script** (FR-003): add a release-lane input block (`pkg-version`, `v*`/`fs-gg-ui-template/v*`
   tag sets) and three rules to `structuralFailures`; extend the verdict report with the release-lane
   facts. Reuse `SemVer`, `readFile`, `run`, `Failure`.
4. **mirror test** (FR-004): add the three release-lane assertions to `Feature209VersionCoherenceTests.fs`
   (it re-derives structurally, env-free), matching the script.
5. **Verify**: `dotnet fsi scripts/validate-version-coherence.fsx` exits 0 (green baseline); flip each
   drift shape in a scratch copy → red naming the location; `dotnet test tests/Package.Tests`.

## Files

- `.github/workflows/release.yml` — two jobs (FR-001, FR-002).
- `scripts/validate-version-coherence.fsx` — release-lane rules + report (FR-003).
- `tests/Package.Tests/Feature209VersionCoherenceTests.fs` — mirror assertions (FR-004).
- `specs/236-release-decouple-framework-template-pin/*` — these artifacts.

## Risks

- **Guard red on the current tree** — mitigated by design: the release-lane rules are all satisfied
  now (`0.1.61` ∈ both release-tag sets and latest; `0.1.58 ≤ 0.1.61`). Verified before merge (SC-001).
- **`dotnet pack` at pin re-resolves inter-package deps** — framework members use ProjectReference, so
  `-p:Version=<pin>` sets both their own version and the cross-refs uniformly (as `dev-repack.fsx`
  already does at a single version). No manual dep pinning needed.
