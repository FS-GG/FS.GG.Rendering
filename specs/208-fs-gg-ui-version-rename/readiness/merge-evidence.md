# Merge evidence — feature 208 (squash-merge to main)

- Squash-merged `208-fs-gg-ui-version-rename` → `main` (commit recorded post-merge).
- `git check-ignore` proof: `specs/208-fs-gg-ui-version-rename/readiness/` is allowlisted in
  `.gitignore` (plain `git check-ignore` returns nothing for files under it → trackable), verified
  before staging.

## Package bump evidence
- **FS.GG.UI.Template** `0.1.50-preview.1` → **`0.1.51-preview.1`** (`.template.package/FS.GG.UI.Template.fsproj`),
  signalling the breaking `FsSkiaUiVersion`→`FsGgUiVersion` property rename (FR-006).
- Packed to the local feed: `~/.local/share/nuget-local/FS.GG.UI.Template.0.1.51-preview.1.nupkg` ✓.
- **No packable `FS.GG.UI.*` library project changed** — only per-library `README.md` docs and the
  template base. So **no library package bump and no sample pin re-alignment** are required; the
  package-feed sample pins are unaffected by this rename.

## Verification carried from US1 (smoke-post-rename.md)
- `dotnet new fs-gg-ui --name Acme` from the bumped+reinstalled template → 1 `FsGgUiVersion`, 0
  `FsSkiaUiVersion`, `dotnet restore`+`dotnet build` green, invariant `dotnet test` 30/30.

## No degraded/synthetic checks
All evidence is from real `dotnet pack`/`restore`/`build`/`test` and real `git tag`/`gh` operations.
No canceled, skipped, substitute, or environment-limited checks reported as green.
