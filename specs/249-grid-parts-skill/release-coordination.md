# Release coordination note — Feature 249 (fs-gg-grids)

**Status: STAGED (T027). Do NOT flip the registry now — this is the release-time checklist.**
The actual publish-before-flip happens when the next `fs-gg-ui` coherent-set is cut (FR-013).

## What changed (the contract delta)

Feature 249 changes the **`fs-gg-ui-template` emitted-file contract** — the set of files a generated
`game`/`sample-pack` product receives:

1. **New skill** `fs-gg-grids` (materializes for `profile in [game, sample-pack]`).
2. **New adaptable source fragment** `Grids.fs` (materialized into `src/<ProductDir>/`).
3. **New `Product.fsproj` compile item** (`<Compile Include="Grids.fs" Condition="Exists('Grids.fs')" />`,
   `Exists`-guarded, profile-gated, next to `Collision.fs`/`Visibility.fs`).

Unlike 247, there is **no fragment-materialization fix** here — the grids fragment source shipped with
the already-correct form (`source: "template/fragments/grids/src/"`, `target: "src/"`) from the start, so
its `Product/` path segment is source-relative and gets `fileRename`'d into `src/<ProductDir>/Grids.fs`.
This was verified by a real `dotnet new fs-gg-ui --profile game --productName Grids249` scaffold + build.

**No F# package public surface changed** — no `src/` library, no new `.fsi`. The PR's **API compatibility
gate passed**, confirming this. So this is a **template-package-content** change, not a framework-library
change. Contrast Rendering #110 / Feature 245 (which advanced the framework `version` because they added
`FS.GG.UI.Canvas.SpatialGrid`/`Pathfinding` — the very primitives `Grids.fs` reuses as the *face*).

## Change classification

**Tier 1 template-contract change** (per the plan Constitution Check). It joins the queue of
**unreleased** template/skill features already merged but not yet flipped — the registry tag list
(`fs-gg-ui-template/v0.1.64-preview.1`) does **not** yet carry 243 (audio), 244 (persistence),
246 (collision), or 247 (visibility). Feature 249 **batches with 243/244/246/247** into the next
coherent-set release rather than being flipped on its own. (Sibling template features building in
parallel — `248-grid-line-drawing`, `250-polygonal-map-generation` — will, once merged, join the same
batch; each independently bumps the framework product-skill count, which must be reconciled at merge.)

## Registry impact (current state, for reference)

`FS-GG/.github` → `registry/dependencies.yml`, entry `fs-gg-ui-template`:

| Field | Current | Release-time action |
|---|---|---|
| `version` (FsGgUiVersion framework pin) | `0.1.64-preview.1` | **unchanged** — no FS.GG.UI.* library changed |
| `package-version` (template package on the feed) | `0.1.64-preview.1` | **advance** to the next preview (carries 243/244/246/247/249) |
| `package-tag` (coherent-set tag) | `fs-gg-ui-template/v0.1.64-preview.1` | **advance** to the new `fs-gg-ui-template/v<V>` |

## Release-time checklist (publish-before-flip) — DO NOT run until the release is cut

1. **Bump the template version-of-truth** (Rendering repo): `.template.package/FS.GG.UI.Template.fsproj`
   `<Version>` and — only if a combined framework bump is also being cut — `template/base/Directory.Packages.props`
   `<FsGgUiVersion>`. For a template-only content bump, advance the **template package version**; the
   `FsGgUiVersion` framework pin stays put unless a library change rides along.
2. **Cut the release** via the repo's `release.yml` tag flow; push the coherent-set `fs-gg-ui-template/v<V>`
   tag. The release-only gates (package-consumption + generated-product tests) must be green — they
   fail-closed and SKIP the push. **Confirm the template package is LIVE on `nuget.pkg.github.com/FS-GG`
   before step 3.**
3. **Flip the registry** (`FS-GG/.github`, only after the package is live): update the `fs-gg-ui-template`
   `package-version` + `package-tag` (and `version` iff a framework bump rode along), refresh the
   consuming edge and top-level `updated:`, prepend one dated newest-first entry to
   `registry/CHANGELOG.md` naming 243/244/246/247/249, and update the `docs/registry/compatibility.md`
   projection (dependency-graph + versioned-contracts row + coherence row). Validate with
   `fsgg-sdd registry validate registry/dependencies.yml` → `"valid": true`; the `contract-coherence`
   check must pass.
4. **Re-pin downstream**: `FS.GG.Templates` → `providers/rendering.providers.yml`
   `source: <PkgId>::<V>`; its `composition` CI must pass.
5. **Track + done-stamp**: one `contract-change` issue on the board (Contract = `fs-gg-ui-template`),
   linked to the registry PR; close out with `scripts/fsgg-coord done <issue> --flip`.

## Why no issue is filed now

T027 stages this note; it does not open a cross-repo request or touch the registry. The producing repo
(this one) owns the release cut, and 249 rides the next batched coherent-set flip with 243/244/246/247 —
filing a premature `contract-change` issue would sit idle and the registry must not advertise a version
the feed cannot yet serve. When the release is cut, open the single batched `contract-change` issue then.
