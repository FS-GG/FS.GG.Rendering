# T006 / T007 — Preconditions & machinery confirmation

**Captured**: 2026-06-29, branch `218-publish-readable-template`.

## T006 — In-repo version pins (pre-bump) and tag-set families

Both pins are at the pre-217 published version, as expected:
```
template/base/Directory.Packages.props:9:    <FsGgUiVersion>0.1.52-preview.1</FsGgUiVersion>
.template.package/FS.GG.UI.Template.fsproj:9:    <Version>0.1.52-preview.1</Version>
```

The three release tag-set families all exist at prior versions (research R2), so the shape is
established:
```
$ git tag --list 'v0.1.5*'
v0.1.52-preview.1
$ git tag --list 'fs-gg-ui-template/v0.1.5*'
fs-gg-ui-template/v0.1.50-preview.1
fs-gg-ui-template/v0.1.52-preview.1
$ git tag --list 'fs-gg-ui/v0.1.5*'
fs-gg-ui/v0.1.50-preview.1
fs-gg-ui/v0.1.51-preview.1
fs-gg-ui/v0.1.52-preview.1
```
→ `V = 0.1.53-preview.1` is the next monotonic preview (`> 0.1.52-preview.1`, INV-1), and the
three tags to push are `v0.1.53-preview.1`, `fs-gg-ui-template/v0.1.53-preview.1`,
`fs-gg-ui/v0.1.53-preview.1`.

## T007 — Publish machinery is already authored (READ-ONLY; no logic change — FR scope §5)

**`.github/workflows/release.yml`** — triggers `on: push: tags: ['v*']` (and `release: published`,
`workflow_dispatch`). The publish does **not** fire on merge to `main`; only an explicit `v*`
tag-push triggers it. Key facts:
- `publish-packages` job is gated `if: github.repository == 'FS-GG/FS.GG.Rendering'` (canonical-repo
  guard; forks never publish — FR-013).
- Version is **derived from the pushed tag**: `ver="${raw#v}"` (`v0.1.53-preview.1` → `0.1.53-preview.1`).
  The in-repo pins are *not* the source of the published version — `-p:Version=$VER` overrides.
- Packs the coherent set at one version:
  `dotnet pack FS.GG.Rendering.slnx -c Release -p:Version="$VER"` **and**
  `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -p:Version="$VER"`.
- Pushes with `permissions: packages: write` →
  `dotnet nuget push "artifacts/packages/*.nupkg" --source https://nuget.pkg.github.com/FS-GG/index.json --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate`.
  No PAT, no manual push.

**`.github/workflows/template-dispatch.yml`** — triggers `on: push: tags: ['fs-gg-ui-template/v*']`
only (the `/` makes its glob disjoint from `release.yml`'s `v*`, so they never collide). It derives
the version via `scripts/derive-template-version.sh`, then `uses:` the org reusable App-token
dispatch-sender to notify FS-GG/FS.GG.Templates (Feature 216). Canonical-repo guarded; fails loud on
a non-tag ref.

**`scripts/derive-template-version.sh`** — present; single responsibility = derive + validate the
released template version from the triggering tag ref (refuses to send on a bad ref).

**Verdict**: All publish/dispatch machinery is authored and unchanged by this feature. The release is
cut purely by (1) bumping the two pins to `V` so main stays coherent and (2) pushing the three-tag
set. No workflow logic change (FR scope fence §5).

## Why the pins must still move to `V` (even though the pack reads the tag)

The published package versions come from the tag, but the in-repo `<FsGgUiVersion>` /template
`<Version>` pins are what the `template-product-tests` local-feed restore and downstream consumers
resolve against. Leaving them at `0.1.52` while the feed serves `0.1.53` makes `main` incoherent
(restore would pull the stale set). So both pins move to `V` in the same release commit (INV-2,
FR-006).
