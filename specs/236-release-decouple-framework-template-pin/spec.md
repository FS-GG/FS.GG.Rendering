# Feature Specification: release the framework libs at the pin and the template at its own tag (stop conflating the two version axes)

**Feature Branch**: `236-release-decouple-framework-template-pin`

**Created**: 2026-07-02

**Status**: Draft

**Input**: Finding P5 / B2 of the [2026-07-02 repo review](../../docs/reports/2026-07-02-14-07-repo-code-quality-and-architecture-review.md). Resolves **FS-GG/FS.GG.Rendering#48**.

## Context (non-normative)

The repo versions **two decoupled things** on one release:

- the **framework** set `FS.GG.UI.*` — its version-of-truth is the template pin
  `<FsGgUiVersion>` in `template/base/Directory.Packages.props` (`0.1.58-preview.1`), snapshotted by
  the `fs-gg-ui/v<V>` git tags; this is what a *generated product* restores.
- the **template package** `FS.GG.UI.Template` — its version-of-truth is `<Version>` in
  `.template.package/FS.GG.UI.Template.fsproj` (`0.1.61-preview.1`), snapshotted by the `v<V>` +
  `fs-gg-ui-template/v<V>` tags; this is what a *composer* installs.

These axes are **intentionally decoupled** (registry `fs-gg-ui-template`: `version` = framework pin,
`package-version` = template package; the framework pin may lag the template package): Features
230/231 shipped template **content** (skill-manifest materialize) at `0.1.60`/`0.1.61` with **no
`src/` change**, so the generated product still pins framework `0.1.58`.

`.github/workflows/release.yml` ignored the decoupling and drove **both** off the release tag `$VER`:

1. **`template-product-tests`** packed the whole `FS.GG.Rendering.slnx` at `$VER` into a runner-local
   feed, but the instantiated product restores `FS.GG.UI.*` at `$(FsGgUiVersion)` = `0.1.58` — which
   resolves from nuget.org, never the local feed. Whenever pin ≠ `$VER` (the normal case for a
   template-only release) the local feed is **dead weight** and the gate never exercises the bits it
   packed.
2. **`publish-packages`** packed *and pushed* every `FS.GG.UI.*` member at `$VER`. The `v0.1.60` /
   `v0.1.61` template-only releases therefore published **orphaned framework packages** `FS.GG.UI.*
   0.1.60`/`0.1.61` that no product pins (`fs-gg-ui/v0.1.60`/`61` snapshot tags deliberately do not
   exist), while the coherent framework set on the feed is still `0.1.58`.
3. `scripts/validate-version-coherence.fsx` (the merge-blocking Feature-209 guard) only knows the
   `fs-gg-ui/v*` framework lane — it cannot see the `v*`/`fs-gg-ui-template/v*` **release** lane, so
   template-package drift (a `<Version>` bump with no matching release tag, or a framework pin that
   *leads* the template package) is invisible to it.

The issue's literal fix ("fail the release when `$VER ≠ FsGgUiVersion`") is **rejected**: it would
block every legitimate template-only release. We fix the actual defect — the workflow must version
each axis on its own source of truth. See `research.md` R1.

## Clarifications

None required — the fix is fully specified by the review finding plus the registry-documented
two-axis model.

## Requirements

- **FR-001** — `template-product-tests` MUST pack the runner-local feed at the **framework pin**
  (`<FsGgUiVersion>` from `template/base/Directory.Packages.props`), not the release tag `$VER`, so
  the instantiated product (which restores `FS.GG.UI.*` @ pin) resolves those exact source-built bits
  from the local feed. The `$VER` resolution is removed from this job (wrong axis).
- **FR-002** — `publish-packages` MUST pack + push the `FS.GG.UI.*` framework members at the
  **framework pin** and the `FS.GG.UI.Template` package at the **release tag `$VER`**, so a
  template-only release republishes no framework member at the template version. Framework push stays
  idempotent (`--skip-duplicate`): when pin == `$VER` (a framework release) it is the same set.
- **FR-003** — `scripts/validate-version-coherence.fsx` MUST additionally validate the **release
  lane**, env-free and fail-closed, from the repo + pushed tags:
  - the template-package version-of-truth (`.template.package/FS.GG.UI.Template.fsproj` `<Version>`)
    is well-formed and present exactly once;
  - it has a matching `v<V>` tag **and** a matching `fs-gg-ui-template/v<V>` tag, and does **not lag**
    the latest of either (preview-aware SemVer);
  - the framework pin does **not lead** the template-package version (`pin ≤ package-version`) — a
    framework bump requires a template release at ≥ that version.
- **FR-004** — `tests/Package.Tests/Feature209VersionCoherenceTests.fs` (the release-lane mirror of
  the structural verdict) MUST mirror the FR-003 rules so the coherent baseline passing and the new
  drift shapes going red are enforced in the release lane and locally.

## Success Criteria

- **SC-001** — With the current tree (pin `0.1.58`, template package `0.1.61`, tags `v0.1.61` +
  `fs-gg-ui-template/v0.1.61` present, `fs-gg-ui/v0.1.58` latest) the guard is **green**: framework
  lane and release lane both coherent.
- **SC-002** — Forcing each new drift shape makes the guard go **red** naming the location
  expected-vs-actual: template `<Version>` bumped with no `v*`/template tag; framework pin set above
  the template package version.
- **SC-003** — On a simulated template-only release (`$VER` = `0.1.61`, pin `0.1.58`) the
  `template-product-tests` local feed carries `FS.GG.UI.* 0.1.58` and the instantiated product
  restores them from that feed (no dead-weight `$VER` feed); `publish-packages` would push framework
  `0.1.58` (skip-duplicate no-op) + template `0.1.61` only.
- **SC-004** — `Package.Tests` (incl. the extended Feature-209 mirror) passes; the guard script exits
  0 on the coherent tree.

## Out of scope

- Un-publishing the already-orphaned `FS.GG.UI.* 0.1.60`/`0.1.61` packages (immutable on the feeds;
  harmless — nothing pins them). The fix prevents recurrence.
- Any framework or template version **bump**, or cutting a new release / registry flip (this is a
  workflow + guard correctness fix, not a release).
- Changing the intentional framework/template decoupling itself (registry `fs-gg-ui-template`).
