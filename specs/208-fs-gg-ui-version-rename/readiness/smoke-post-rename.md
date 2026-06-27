# T012 — Live generate→restore→build of the BUMPED, RENAMED template (US1 MVP verification)

Template re-packed + reinstalled from the working tree: `FS.GG.UI.Template` **0.1.51-preview.1**
(bumped from 0.1.50-preview.1, FR-006). Generated with PascalCase `--name Acme` (FS0053 trap).

```
$ dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release   → 0.1.51-preview.1.nupkg
$ dotnet new uninstall FS.GG.UI.Template ; dotnet new install …0.1.51-preview.1.nupkg
$ dotnet new fs-gg-ui --name Acme -o Acme        → created

# inside generated Acme/:
grep -c '<FsGgUiVersion>' Directory.Packages.props   → 1     (single source, FR-002)
grep -rq 'FsSkiaUiVersion' .                          → no matches anywhere (SC-001) ✓
dotnet restore tests/Acme.Tests/Acme.Tests.fsproj     → EXIT 0
dotnet build   tests/Acme.Tests/Acme.Tests.fsproj     → EXIT 0 (SC-002) ✓
dotnet test    tests/Acme.Tests/Acme.Tests.fsproj     → Passed! 30/30, Failed 0 (FR-003 invariant green) ✓
```

**Result: GREEN.** Exactly one `FsGgUiVersion`, zero `FsSkiaUiVersion`, restore+build+invariant green —
the generated product is driven solely by `FsGgUiVersion`. US1 (MVP) independently done; breaking
change verified.

Notes:
- The single-source invariant test (`GovernanceTests`) now asserts `build.fsx` resolves the engine
  from `FsGgUiVersion`; it is part of the 30 green Acme.Tests.
- `sourceName=Product`→`Acme`: the migration note in `UPGRADING.md` is worded with the name-neutral
  word "project" (not "product") so the substitution leaves it reading correctly in the generated tree.
- FR-008 ∧ SC-001 reconciliation: the migration note instructs renaming the single version property to
  `FsGgUiVersion` WITHOUT reproducing the old `FsSkiaUi…` literal, so the generated tree stays at zero
  `FsSkiaUiVersion` (SC-001) while still telling pre-rename authors how to migrate (FR-008).
