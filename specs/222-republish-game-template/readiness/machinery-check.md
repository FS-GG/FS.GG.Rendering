# T006 + T007 — Producer machinery intact (READ-ONLY, FR-010)

No edits to any producer artifact. Confirmed on `HEAD` (== `main` `eb93e89`).

## T006 — `release.yml` `publish-packages` (unchanged)

`.github/workflows/release.yml`:
- Trigger: `on: push: tags: ['v*']` + `workflow_dispatch` (manual `version` input). ✅
- Canonical-repo guard: every job `if: github.repository == 'FS-GG/FS.GG.Rendering'`. ✅
- `publish-packages` job: `permissions: packages: write`; resolves the coherent-set version from the
  release tag; packs `dotnet pack FS.GG.Rendering.slnx` **and** `.template.package/FS.GG.UI.Template.fsproj`
  at one `Version`; pushes `artifacts/packages/*.nupkg` to `https://nuget.pkg.github.com/FS-GG/index.json`
  with `${{ secrets.GITHUB_TOKEN }}` and `--skip-duplicate` (append-only safe). ✅
- Gated behind `package-tests` + `template-product-tests` (`needs:`) — pre-publish gates not weakened. ✅

## `scripts/derive-template-version.sh` + Feature-216 dispatch-sender (unchanged)

- Strips `refs/tags/fs-gg-ui-template/v`, validates `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$`,
  emits `version=<v>` to stdout + `$GITHUB_OUTPUT`; fails loud on a non-tag/malformed ref (FR-005).
- The cross-repo POST lives in the org reusable `FS-GG/.github/.github/workflows/dispatch-sender.yml`
  (`uses:` from the `dispatch` job) — notifies Templates. No change here.

→ **No edits required.** ✅

## T007 — `game` profile packs & is selectable (READ-ONLY)

`.template.config/template.json` `symbols.profile` (`datatype: choice`, default `app`) carries:

```json
{ "choice": "game",
  "description": "Game/rendering default starter: a minimal, replaceable Pong-style interactive
                  scene with Scene, SkiaViewer, Elmish, KeyboardInput, Layout, and Controls." }
```

The `game` choice is on `HEAD` and is included in the content-copy conditions (e.g. the
`fs-gg-scene` / `fs-gg-skiaviewer` skill copies list `profile == "game"`). It will pack into `V` and
be selectable via `--profile game`. ✅
