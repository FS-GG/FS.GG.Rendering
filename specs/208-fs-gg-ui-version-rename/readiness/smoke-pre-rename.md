# T005 — Early live smoke run (CURRENT, un-renamed template)

Purpose: prove the generate→restore→build harness is sound BEFORE it judges the rename, so a
post-rename failure is attributable to the rename, not a pre-broken pipeline (plan Standing assumption).

Installed template: `FS.GG.UI.Template` **0.1.50-preview.1** (packed; `dotnet new fs-gg-ui`).

```
$ dotnet new fs-gg-ui --name Acme -o Acme      # PascalCase --name dodges the FS0053 lowercase trap
The template "FS GG UI Governed Project" was created successfully.   GEN_EXIT=0

generated Directory.Packages.props → <FsSkiaUiVersion>0.1.50-preview.1</FsSkiaUiVersion> (pre-rename, expected)

$ dotnet restore src/Acme/Acme.fsproj            EXIT=0
$ dotnet restore tests/Acme.Tests/Acme.Tests.fsproj  EXIT=0
$ dotnet build tests/Acme.Tests/Acme.Tests.fsproj --no-restore -c Debug
  Acme -> .../src/Acme/bin/Debug/net10.0/Acme.dll
  Acme.Tests -> .../tests/Acme.Tests/bin/Debug/net10.0/Acme.Tests.dll
  Build succeeded.  0 Warning(s)  0 Error(s)   EXIT=0
```

**Result: GREEN.** Harness sound. (Note: `sourceName=Product`→`Acme`, so the generated test project
is `tests/Acme.Tests/Acme.Tests.fsproj`, not the literal `Product.Tests` named in quickstart/T012.)
