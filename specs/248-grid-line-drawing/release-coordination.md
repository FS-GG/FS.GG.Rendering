# Release coordination note — Feature 248 (fs-gg-line-drawing)

**Status: STAGED (T027). Do NOT flip the registry now — this is the release-time checklist.**
The actual publish-before-flip happens when the next `fs-gg-ui` coherent-set is cut (FR-014).

## What changed (the contract delta)

Feature 248 changes the **`fs-gg-ui-template` emitted-file contract** — the set of files a generated
`game`/`sample-pack` product receives:

1. **New skill** `fs-gg-line-drawing` (materializes for `profile in [game, sample-pack]`).
2. **New adaptable source fragment** `LineDrawing.fs` (materialized into `src/<ProductDir>/`).
3. **New `Product.fsproj` compile item** (`Exists`-guarded, profile-gated, next to `Visibility.fs`).

**No F# package public surface changed** — no `src/` library, no new `.fsi`. The fragment reuses the
existing `Cell` and `Pathfinding` surfaces already shipped by `FS.GG.UI.Canvas` (feature 245). So this is
a **template-package-content** change, not a framework-library change — identical in shape to 246
(collision) and 247 (visibility).

## Change classification

**Tier 1 template-contract change** (per the plan Constitution Check). It joins the queue of
**unreleased** template/skill features already merged but not yet flipped. Feature 248 **batches with
243/244/246/247** into the next coherent-set release rather than being flipped on its own.

## Registry impact (current state, for reference)

`FS-GG/.github` → `registry/dependencies.yml`, entry `fs-gg-ui-template`:

| Field | Release-time action |
|---|---|
| `version` (FsGgUiVersion framework pin) | **unchanged** — no FS.GG.UI.* library changed |
| `package-version` (template package on the feed) | **advance** to the next preview (carries 243/244/246/247/248) |
| `package-tag` (coherent-set tag) | **advance** to the new `fs-gg-ui-template/v<V>` |

## Release-time checklist (publish-before-flip, FR-014) — DO NOT run until the release is cut

1. **Bump the template version-of-truth** (Rendering repo): `.template.package/FS.GG.UI.Template.fsproj`
   `<Version>`. For a template-only content bump the `FsGgUiVersion` framework pin stays put unless a
   library change rides along.
2. **Cut the release** via the repo's `release.yml` tag flow; push the coherent-set `fs-gg-ui-template/v<V>`
   tag. The release-only gates (package-consumption + generated-product tests) must be green. **Confirm the
   template package is LIVE on `nuget.pkg.github.com/FS-GG` before step 3.**
3. **Flip the registry** (`FS-GG/.github`, only after the package is live): update the `fs-gg-ui-template`
   `package-version` + `package-tag`, refresh the consuming edge and top-level `updated:`, prepend one
   dated newest-first entry to `registry/CHANGELOG.md` naming 243/244/246/247/248, and update the
   `docs/registry/compatibility.md` projection (dependency-graph + versioned-contracts row + coherence
   row). Validate with `fsgg-sdd registry validate registry/dependencies.yml` → `"valid": true`.
4. **Re-pin downstream**: `FS.GG.Templates` → `providers/rendering.providers.yml` `source: <PkgId>::<V>`;
   its `composition` CI must pass.
5. **Track + done-stamp**: one `contract-change` issue on the board (Contract = `fs-gg-ui-template`),
   linked to the registry PR; close out with `scripts/fsgg-coord done <issue> --flip`.

## Why no issue is filed now

T027 stages this note; it does not open a cross-repo request or touch the registry. The producing repo
(this one) owns the release cut, and 248 rides the next batched coherent-set flip with 243/244/246/247 —
filing a premature `contract-change` issue would sit idle and the registry must not advertise a version
the feed cannot yet serve. When the release is cut, open the single batched `contract-change` issue then.
