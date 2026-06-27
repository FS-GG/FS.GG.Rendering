# Contract: API-Surface Conformance

The "interface" this feature must honor is the FS.GG.UI public surface the template targets, plus the
single-version invariant. These are the checkable contracts a reviewer (or `/speckit-tasks`) can turn
into assertions.

## C1 — Seed code conforms to the current surface (FR-001/FR-002, SC-001/SC-002)

- **Target**: the current FS.GG.UI public surface as restored from the local feed (Scene is the named
  centerpiece; Controls/Controls.Elmish/SkiaViewer/Elmish/Layout/KeyboardInput/DesignSystem/
  Themes.Default for the interactive branch).
- **Holds when**: for every profile, the generated product compiles with **zero** errors and zero
  warnings attributable to API drift.
- **Check**: `dotnet build` of each generated profile (see `generated-product-evidence.md`).
- **Cross-check**: every `Scene.`/`SceneNode.`/Scene-type construct referenced by the seed exists in
  the current public Scene surface with the assumed arity/shape (static scan + the build).

## C2 — Single FS.GG.UI version literal (FR-004, SC-003)

- **Holds when**: `template/base/Directory.Packages.props` contains exactly one FS.GG.UI version
  literal — `<FsSkiaUiVersion>` — and every `FS.GG.UI.*` `PackageVersion` resolves to
  `$(FsSkiaUiVersion)`.
- **Check**:
  - `grep -c 'FS.GG.UI[^"]*" Version="[0-9]' template/base/Directory.Packages.props` → `0`
    (no literal versions on FS.GG.UI pins; all use the property).
  - `grep -c '<FsSkiaUiVersion>' template/base/Directory.Packages.props` → `1`.
  - GovernanceTests assertion that `build.fsx` resolves the engine from `FsSkiaUiVersion` still passes.

## C3 — Pin equals the produced feed version (FR-003)

- **Holds when**: `<FsSkiaUiVersion>` equals the version `V` the local-feed pack produced
  (`~/.local/share/nuget-local/`), and a clean `dotnet restore` of each profile resolves all
  `FS.GG.UI.*` to `V` with no missing-package/version-conflict errors.
- **Check**: extract `V` from the packed `.nupkg` filenames; assert string-equal to `<FsSkiaUiVersion>`;
  `dotnet restore` exit 0.

## C4 — No stale version literal remains (FR-006, SC-003)

- **Holds when**: the superseded `FsSkiaUiVersion` value (the pre-refresh literal, if it changed)
  appears nowhere in template package pins, seed code, or documentation that states the *current* pin.
- **Check**: `grep -rn '<superseded-version>' template/base/` returns only intentionally
  illustrative/historical occurrences (e.g. a non-live example in `docs/UPGRADING.md`), and none that
  represent the live pin. If the re-pin was a no-op (`V` unchanged), this contract is vacuously true.

## C5 — Bundled Scene reference matches the live surface (FR-005, SC-004)

- **Holds when**: `template/base/docs/api-surface/Scene` presents no type/member as current that is
  absent from the live public Scene surface, and framework identifiers are byte-preserved (no
  `sourceName` substitution from being `copyOnly`).
- **Check**: every type/member named in the bundled Scene reference resolves in `src/Scene/*.fsi`;
  spot-confirm the file was not subjected to `Product` substitution.
