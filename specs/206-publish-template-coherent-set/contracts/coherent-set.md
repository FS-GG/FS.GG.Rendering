# Contract: Coherent Set — Template ↔ Framework Snapshot

**Contract name (cross-repo registry):** `fs-gg-ui-template`
**Owner repo:** FS.GG.Rendering (template package + manifest)
**Tag:** `fs-gg-ui-template/v0.1.50-preview.1` (annotated, immutable)
**Status:** Proposed (Feature 206) — becomes Accepted when published + tagged + reconciled.

## What the coherent set binds

A reproducible snapshot pairing the published template package with the framework set it scaffolds
products against:

| Role | Package id | Version | Anchor |
|---|---|---|---|
| Template | `FS.GG.UI.Template` | `0.1.50-preview.1` | `fs-gg-ui-template/v0.1.50-preview.1` (this feature) |
| Framework (pinned by template) | `FS.GG.UI.*` set | `0.1.50-preview.1` | `fs-skia-ui/v0.1.50-preview.1` (feature 204) |

The framework `FS.GG.UI.*` set is the 16-package coherent snapshot recorded by feature 204's
`contracts/snapshot-manifest.md`; this contract does not re-enumerate it — it references it and
declares template coherence **on top of** that already-coherent base (invariant I2).

## Template surfaces the published package MUST carry

- `lifecycle` choice symbol: `spec-kit` (default) | `sdd` | `none`; unknown values rejected (204).
- `initGit` bool opt-in, default `false`; **no** `skipGitInit`; no auto-running post-actions (205).
- Profiles emitted: `app`, `headless-scene`, `governed`, `sample-pack`.
- Generated products pin `FsSkiaUiVersion=0.1.50-preview.1`.

## Reproducibility obligation (FR-009)

From a clean checkout of `fs-gg-ui-template/v0.1.50-preview.1`:

```sh
git checkout fs-gg-ui-template/v0.1.50-preview.1
# bump already present at the tag:
grep '<Version>' .template.package/FS.GG.UI.Template.fsproj   # -> 0.1.50-preview.1
dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local
# -> reproduces FS.GG.UI.Template.0.1.50-preview.1.nupkg byte-equivalently
```

A from-tag repack MUST reproduce the published template package, and the framework packages it pins
MUST already be resolvable at `0.1.50-preview.1` on the feed.

## Collision rules (edge cases)

- **Package exists**: if `FS.GG.UI.Template.0.1.50-preview.1.nupkg` already exists on the feed, the
  publish MUST fail loudly and a next unused version chosen — never silent overwrite (FR-002).
- **Tag exists**: if `fs-gg-ui-template/v0.1.50-preview.1` already exists, surface the collision and
  pick a distinct name; never move an existing tag (FR-002).
- **Incoherent base**: if the pinned `FS.GG.UI.*` set is not the coherent published snapshot, record
  the dependency and do **not** declare template coherence (FR-009).
