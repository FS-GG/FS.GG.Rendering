# T009 + T010 — Cut & publish the coherent set at `V = 0.1.54-preview.1`

**Date**: 2026-06-30 · **Operator**: `EHotwagner` (release rights present — not deferred).

## T009 — Release cut from a `main` commit containing `b78e72a`

- Merged `222-republish-game-template` → `main` (ff): `eb93e89..afecc49`.
- `afecc49` contains `b78e72a` (Feature 220): `git merge-base --is-ancestor b78e72a afecc49` → true.
- Pushed the release tag-set (all on `afecc49`):

| Tag | Triggers | Purpose |
|---|---|---|
| `v0.1.54-preview.1` | `release.yml` (`tags: ['v*']`) | packs `FS.GG.UI.*` + template at `V`, pushes to org feed |
| `fs-gg-ui-template/v0.1.54-preview.1` | `template-dispatch.yml` (`tags: ['fs-gg-ui-template/v*']`) | derive + notify Templates |
| `fs-gg-ui/v0.1.54-preview.1` | — (sibling cadence tag, mirrors 0.1.53) | coherent-set marker |

## CI runs

| Workflow | Run ID | Link |
|---|---|---|
| release (publish-packages) | `28468936061` | https://github.com/FS-GG/FS.GG.Rendering/actions/runs/28468936061 |
| template-dispatch | `28468936457` | https://github.com/FS-GG/FS.GG.Rendering/actions/runs/28468936457 |

`release.yml` `publish-packages` is gated behind `package-tests` + `template-product-tests` (`needs:`),
then `dotnet pack` the whole set + template at `V` and `dotnet nuget push … --skip-duplicate` to
`nuget.pkg.github.com/FS-GG`. Outcome captured below once the run completes.

## T010 — Dispatch propagated `V` (FR-010, Templates re-pin half)

`template-dispatch.yml` (triggered by `fs-gg-ui-template/v0.1.54-preview.1`) runs
`scripts/derive-template-version.sh` → derives `version=0.1.54-preview.1` from the tag ref and the
Feature-216 reusable `dispatch-sender.yml` notifies Templates. Outcome captured below.

## Run outcomes (polled)

- **template-dispatch** (`28468936457`): ✅ **success** — `derive-template-version.sh` derived
  `version=0.1.54-preview.1` from `fs-gg-ui-template/v0.1.54-preview.1` and the Feature-216 reusable
  `dispatch-sender.yml` notified Templates (FR-010 / T010 satisfied).
- **release** (`28468936061`): `template-product-tests` ✅ success; `package-tests` running →
  `publish-packages` (gated `needs:` both) packs + pushes the coherent set at `V`. Feed-served
  confirmation captured in `feed-listing.md` once `publish-packages` completes.
