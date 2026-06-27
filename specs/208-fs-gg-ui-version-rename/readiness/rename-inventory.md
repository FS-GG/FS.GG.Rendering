# T004 — Rename-site inventory (FsSkiaUiVersion → FsGgUiVersion)

Confirmed via `grep -c` 2026-06-27. Classification map every story depends on.

## (A) Property surface — ATOMIC, one commit (US1, FR-001/002/003)

| file | occ | breakdown |
|---|---:|---|
| template/base/Directory.Packages.props | 14 | 1 `<FsSkiaUiVersion>` literal + 11 `$(FsSkiaUiVersion)` pins + 2 comments |
| template/base/build.fsx | 6 | runtime resolver regex `<FsSkiaUiVersion>([^<]+)</FsSkiaUiVersion>` + 5 comment/usage lines |
| template/base/tests/Product.Tests/GovernanceTests.fs | 3 | 1 invariant assertion string + 2 comments |
| .template.config/generated/README.md | 1 | one `<FsSkiaUiVersion>` mention |
| .template.package/README.md | 1 | one `<FsSkiaUiVersion>` mention |

The 11 pins cover the FS.GG.UI.* members consumed by a generated product; the other coherent
members + BOM are not referenced by the generated tree.

## Version bump (US1, FR-006)

- `.template.package/FS.GG.UI.Template.fsproj` `<Version>` — the actual published/installed template
  package version (currently `0.1.50-preview.1`). **Deviation note:** tasks.md T011 names
  `template/base/Directory.Build.props`, but that file carries no `<Version>`; the effective template
  version that signals the breaking rename to `dotnet new --update` consumers lives in the package
  fsproj. Bumping there is what satisfies FR-006.

## (B) Shipped-doc / provenance sweep (US3, FR-007/008)

| file | occ |
|---|---:|
| PROVENANCE.md | 1 |
| template/base/README.md | 1 |
| template/base/docs/UPGRADING.md | 4 (+ add FR-008 migration note) |
| src/Build/README.md | 1 |
| src/Scene/README.md | 1 |
| src/SkiaViewer/README.md | 1 |
| src/Elmish/README.md | 1 |
| src/KeyboardInput/README.md | 1 |
| src/Layout/README.md | 1 |
| src/Controls/README.md | 1 |
| src/Controls.Elmish/README.md | 1 |
| src/Testing/README.md | 1 |

## (C) Tags (US2) — see pre-rename-tags.md
## (D) Cross-repo (FS-GG/.github) — registry ids + ADR-0003

## OUT OF SCOPE (do NOT edit)
specs/**, docs/product/decisions/0001-package-identity.md, docs/audit/mechanism-inventory.md,
docs/bridge/package-identity-migration.md, src/**/*.fs(i), fs-gg-ui-template/v* tag namespace.
