# 252 — Retire `FS.GG.UI.Canvas.Audio` (complete the audio extraction)

**Issue:** FS-GG/FS.GG.Rendering#158 · **Decision:** ADR-0024 (FS-GG/.github#237), Option (a)
**Type:** contract-change (breaking public-surface removal) → coherent-set release
**Lifecycle:** lean (on `item/158-retire-canvas-audio`; no full speckit ceremony — the ADR fixes the decision & scope).

**Status**: Shipped

## Why

`FS.GG.Audio.Core` was extracted *verbatim* from `FS.GG.UI.Canvas.Audio` but the Canvas copy
was never removed, so two non-interoperating `AudioEffect` vocabularies shipped and had already
diverged (`Core` grew `PlaySfx3D`/`SetBusVolume`/`Duck` and re-defined `SoundId`/`TrackId`/
`AudioEvidence`/`Bus`). ADR-0024 chose **(a) complete the extraction**: `FS.GG.Audio.Core` becomes
the platform's single audio request vocabulary; Canvas retires its copy. This is the Rendering half
(ordering step 2) — it must land before the template pins the FS.GG.Audio packages (#156).

## Scope (Rendering only)

- **Removed** `src/Canvas/Audio.fs` + `Audio.fsi` and their `Compile` items in `Canvas.Lib.fsproj`;
  refreshed the package `Description`/`PackageTags` (Persistence stays — it is a separate surface).
- **Removed** `tests/Canvas.Tests/AudioTests.fs` and its `Compile` item.
- **Updated** `readiness/surface-baselines/FS.GG.UI.Canvas.txt` — dropped the 9 audio public types
  (`Audio`, `AudioEffect`(+cases/Tags), `AudioEvidence`, `SoundId`, `TrackId`). Verified against the
  built assembly via `scripts/refresh-surface-baselines.fsx` (generator == hand-edit).
- **Bumped** the coherent set `0.2.0-preview.1` → `0.3.0-preview.1` (breaking): `<FsGgUiVersion>`
  (framework pin), `.template.package` `<Version>`, all `src/*` `<Version>`, and every `samples/*`
  FS.GG.UI.* pin (Feature163 coherence).

## Deliberately NOT in this pass (ADR ordering, cross-repo follow-ups)

- **`fs-gg-audio` skill re-point.** `template/product-skills/fs-gg-audio/SKILL.md` + the shipped
  `template/base/docs/api-surface/Canvas/Audio.fsi` still teach `Canvas.Audio`. ADR-0024 assigns the
  canonical re-point (to `FS.GG.Audio.Core`) to **FS.GG.Game** (ordering step 4); Rendering's frozen
  copy must byte-match Game's, so it is mirrored *there*, not authored here. No Rendering test gates
  it, and no template product source consumes `AudioEffect`, so nothing breaks — this is the
  ADR-accepted transient state between steps 2 and 4.
- **Template pins for FS.GG.Audio** (#156, step 3) and the **registry consumer-edge flip**
  (.github#238, step 5).

## Verification

- Full solution builds clean (Debug); `Canvas.Tests` 84/84 pass.
- `Package.Tests` 227/229 pass. The 2 reds are `Feature209` `pin-no-tag` / `pkg-no-release-tag` —
  **expected until the release tag triple is cut** (same accepted red as release PR #155's
  Deterministic gate). The API-compat gate flags the removed surface, forcing the major bump (met).

## Release choreography (publish-before-flip)

> **Superseded (2026-07-09, [ADR-0100](../../docs/product/decisions/0100-gate-is-a-required-check.md), #190).**
> Step 1's "red *by design*" is **no longer true and must not be copied into a new release spec.**
> It records what this shipped release did, not what the next one should do. The version-coherence
> guard now carries a RELEASE-PENDING waiver: a change that bumps the pin/package waives its own
> not-yet-cut tags, so a release PR's `Deterministic gate` is **green**. `gate` is a required check
> as of ADR-0100, so a release PR that is red is red for a real reason — do not merge past it.

1. Merge this release PR to `main` (its version-coherence gate is red *by design*, as on #155).
2. Push the tag triple at the merge commit: `fs-gg-ui/v0.3.0-preview.1`,
   `fs-gg-ui-template/v0.3.0-preview.1`, then `v0.3.0-preview.1` (the `v*` tag triggers `release.yml`).
3. `release.yml` packs FS.GG.UI.* @pin + the template @tag and publishes to `nuget.pkg.github.com/FS-GG`.
4. Follow-ups (separate PRs/repos): FS.GG.Game skill re-point → #156 template pins → .github registry flip.
