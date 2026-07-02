# Tasks: release framework@pin, template@tag; guard the release lane

**Issue**: FS-GG/FS.GG.Rendering#48 · **Spec/Plan**: [`spec.md`](./spec.md) / [`plan.md`](./plan.md)

## Phase 1 — Investigation

- [X] T001 Establish the two-axis model from the registry + version-of-truth files; confirm the
  framework/template decoupling is intentional and the issue's literal `$VER≠pin` fail is wrong. →
  `research.md` R1.
- [X] T002 Capture tag/pin evidence; confirm `v0.1.60/61` published orphan `FS.GG.UI.*` and the
  `template-product-tests` local feed was dead weight. → `research.md` R2.
- [X] T003 Confirm `.template.package/…fsproj` is not a slnx member (packs never overlap) and its
  `<Version>` = `0.1.61` is the release-lane truth. → `research.md` R3.

## Phase 2 — release.yml

- [X] T004 `template-product-tests`: replace the `$VER`-from-tag `ver` step with a pin read from
  `template/base/Directory.Packages.props`; pack the slnx at the **pin** into the local feed; fix the
  comments (feed carries pin bits the product restores). (FR-001)
- [X] T005 `publish-packages`: pack `FS.GG.Rendering.slnx` at the **pin** and
  `.template.package/…fsproj` at **`$VER`**; push both `--skip-duplicate`; fix the comments to name
  the two axes. (FR-002)

## Phase 3 — guard + mirror

- [X] T006 `scripts/validate-version-coherence.fsx`: add the release-lane input block (`pkg-version`,
  `v*`/`fs-gg-ui-template/v*` tag sets) + three rules (`pkg-version` ∈ both tag sets & not lagging;
  `pin ≤ pkg-version`) into `structuralFailures`; add the release-lane facts to the verdict report.
  (FR-003)
- [X] T007 `tests/Package.Tests/Feature209VersionCoherenceTests.fs`: mirror the three release-lane
  assertions (structural, env-free), matching the script. (FR-004)

## Phase 4 — Verification & delivery

- [X] T008 `dotnet fsi scripts/validate-version-coherence.fsx` exits 0 (green baseline, SC-001); flip
  each drift shape in a scratch copy → red naming the location (SC-002); `dotnet test
  tests/Package.Tests` green (SC-004). (SC-001..004)
- [X] T009 Squash-merge to `main`, set the Coordination board item #48 to Done, push. Closes #48.
