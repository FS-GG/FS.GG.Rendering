# Quickstart: Verify the fs-skia-ui → fs-gg-ui Version Rename

Runnable validation that the rename is coherent end-to-end. Ordered so the verifiable breaking change
(the property) is proven first, then the tags, then the doc sweep, then the cross-repo record. All
product validation runs against a **generated** product, never `template/base` in place.

## Prerequisites

- Local feed at `~/.local/share/nuget-local/` carrying a published coherent `FS.GG.UI.*` set.
- The template version bumped (FR-006) and the property renamed atomically (see
  [contracts/version-property-rename.md](./contracts/version-property-rename.md)).

## Step 1 — Property rename: generate → restore → build (US1, SC-001/002)

```bash
# Generate a product from the bumped template (use the repo's normal generate path / dotnet new fs-gg-ui)
# Then, inside the generated product:
grep -c "<FsGgUiVersion>" Directory.Packages.props     # → 1  (single source, FR-002)
! grep -rq "FsSkiaUiVersion" .                          # → no matches anywhere in the generated tree (SC-001)
dotnet restore                                          # → resolves every FS.GG.UI.* via $(FsGgUiVersion)
dotnet build                                            # → green (SC-002)
dotnet test tests/Product.Tests/Product.Tests.fsproj    # → single-source invariant green on FsGgUiVersion (FR-003)
```

Expected: exactly one `FsGgUiVersion` literal, zero `FsSkiaUiVersion`, green restore+build+invariant.
A half-renamed tree fails restore fast on an undefined property — that is the catch, not a grep.

## Step 2 — Snapshot tags re-pointed (US2, SC-003)

See [contracts/snapshot-tag-namespace.md](./contracts/snapshot-tag-namespace.md).

```bash
git tag -l 'fs-gg-ui/v*'                       # → fs-gg-ui/v0.1.50-preview.1, fs-gg-ui/v0.1.51-preview.1
git tag -l 'fs-skia-ui/v*'                     # → (empty) — clean break (FR-005)
git rev-list -n1 fs-gg-ui/v0.1.50-preview.1    # → 57be86c…  (same commit, FR-004)
git rev-list -n1 fs-gg-ui/v0.1.51-preview.1    # → d9f4c81…  (same commit, FR-004)
```

## Step 3 — Shipped-doc / provenance sweep (US3, SC-004)

```bash
# Current guidance must carry zero legacy version-property references:
grep -rn "FsSkiaUiVersion" \
  PROVENANCE.md template/base/README.md template/base/docs/UPGRADING.md \
  .template.config/generated/README.md .template.package/README.md src/*/README.md   # → no matches

# History is preserved (matches remain ONLY here):
grep -rl "FsSkiaUiVersion" specs/ | head        # → historical records, untouched (FR-009)
```

Also confirm `template/base/docs/UPGRADING.md` instructs editing `FsGgUiVersion` and includes the
pre-rename migration note (rename the property when adopting the bumped template) (FR-008).

## Step 4 — Cross-repo record (SC-005, gated on Steps 1–2)

See [contracts/registry-contract-ids.md](./contracts/registry-contract-ids.md). Via `gh` +
`cross-repo-coordination`: rename `fs-skia-ui-version`/`fs-skia-ui-bom` → `fs-gg-ui-version`/`fs-gg-ui-bom`
in `FS-GG/.github`, flip ADR-0003 to Accepted, and confirm Templates/SDD carry no
`FsSkiaUiVersion`/`fs-skia-ui/*` reference (cross-repo request if one is found, FR-011).

## Done when

- SC-001/002: generated product has one `FsGgUiVersion`, zero `FsSkiaUiVersion`, restores+builds green.
- SC-003: `fs-gg-ui/v<V>` resolves to the same commits; `fs-skia-ui/v*` is empty.
- SC-004: zero `fs-skia-ui`/`FsSkiaUiVersion` in current docs (only `specs/**` history remains).
- SC-005: all three surfaces use the `fs-gg-ui` root; registry updated; ADR-0003 Accepted.
