# Contract: Version Property Rename (`FsSkiaUiVersion` → `FsGgUiVersion`)

Covers User Story 1 (P1) and FR-001/002/003/006. This is the consumer-visible breaking change and
the definition of done for the feature.

## Surfaces (template base — renamed atomically in one commit)

| Surface | Obligation |
|---------|-----------|
| `template/base/Directory.Packages.props` | The single literal is `<FsGgUiVersion>…</FsGgUiVersion>`; every `FS.GG.UI.*` `PackageVersion` reads `Version="$(FsGgUiVersion)"` (13 pins); the two explanatory comments name `$(FsGgUiVersion)`. |
| `template/base/build.fsx` | The runtime resolver regex is `<FsGgUiVersion>([^<]+)</FsGgUiVersion>`; the `failwithf` and header comments name `<FsGgUiVersion>`. |
| `template/base/tests/Product.Tests/GovernanceTests.fs` | The single-source invariant asserts the generated `build.fsx` contains `"FsGgUiVersion"` (FR-003); comments updated. |
| `.template.config/generated/README.md`, `.template.package/README.md` | The `<FsGgUiVersion>` mention is renamed. |
| `template/base/Directory.Build.props` | `<Version>` bumped one preview increment (FR-006; exact number a release detail). |

## Invariants

- **Single source (FR-002)**: exactly **one** FS.GG.UI version literal per generated product, named
  `FsGgUiVersion`. The rename introduces no second literal.
- **Atomicity**: literal + all 13 pins + resolver regex + invariant assertion change together. A pin
  left as `$(FsSkiaUiVersion)` resolves to an undefined (empty) property and restore fails fast.
- **Clean break**: no `FsSkiaUiVersion` alias is defined anywhere.

## Acceptance (US1)

1. A freshly generated product's `Directory.Packages.props` has the single literal `FsGgUiVersion`
   and **zero** `FsSkiaUiVersion` tokens anywhere in the generated tree. *(AS1 / SC-001)*
2. Changing `FsGgUiVersion` to a valid published version + `dotnet restore` resolves every
   `FS.GG.UI.*` to that version and the product **builds green**. *(AS2 / SC-002)*
3. The single-source invariant test runs against a generated product, asserts against `FsGgUiVersion`,
   and **passes**. *(AS3 / FR-003)*

## Verification commands (against a generated product, not `template/base` in place)

```bash
grep -c "<FsGgUiVersion>" Directory.Packages.props        # → 1
! grep -rq "FsSkiaUiVersion" .                            # → no matches in the generated tree
dotnet restore && dotnet build                            # → green
dotnet test tests/Product.Tests/Product.Tests.fsproj      # → invariant green on FsGgUiVersion
```
