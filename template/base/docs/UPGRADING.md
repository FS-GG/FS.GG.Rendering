# Upgrading FS.Skia.UI

This project pins every `FS.Skia.UI.*` package **and** the in-process build engine to a
**single source of version truth**, so moving to a newer release is **one edit**.

## The one edit

Open `Directory.Packages.props` and change the single `<FsSkiaUiVersion>` value:

```xml
<PropertyGroup>
  <FsSkiaUiVersion>0.1.68-preview.1</FsSkiaUiVersion>   <!-- the ONLY FS.Skia.UI version literal -->
</PropertyGroup>
```

Every `FS.Skia.UI.*` library pin references `$(FsSkiaUiVersion)`, and `build.fsx` reads this
same property at runtime to bind the `FS.Skia.UI.Build` engine. There is no second place to
edit — the libraries and the build engine always move together.

## Then restore

```bash
dotnet restore
```

`dotnet restore` pulls the new library versions (via Central Package Management) **and** the
matching engine package. The next `./fake.sh build -t Dev` / `dotnet test` uses the new
version.

## Verify

```bash
dotnet build       # the libraries resolve at the new version
./fake.sh build -t Verify   # the build engine resolves at the new version too
```

If `dotnet restore` reports a missing package, the version you chose is not published on the
feed your `NuGet.config` references — pick a version that exists on the channel you want
(see below).

## Preview vs stable

The channel is **explicit in the value** — you never silently cross channels:

- `0.1.68-preview.1` (or any `-preview.N` / `-rc.N` suffix) ⇒ a **preview** release.
- `1.0.0` (a bare `MAJOR.MINOR.PATCH`, no suffix) ⇒ a **stable** release.

Pins stay **exact** — there are no floating ranges. Upgrading is always a deliberate single
edit, so a build is reproducible: the same `<FsSkiaUiVersion>` always restores the same
versions.

## Where the packages come from

The generated `NuGet.config` references the **public nuget.org feed only** — no machine-local
path — so this project restores on any machine without a repository checkout. To consume a
**private** or **pre-release staging** feed instead, add it to `NuGet.config`.
