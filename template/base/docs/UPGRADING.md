# Upgrading FS.GG.UI

This project pins every `FS.GG.UI.*` package **and** the in-process build engine to a
**single source of version truth**, so moving to a newer release is **one edit**.

## The one edit

Open `Directory.Packages.props` and change the single `<FsGgUiVersion>` value:

```xml
<PropertyGroup>
  <FsGgUiVersion>X.Y.Z-preview.N</FsGgUiVersion>   <!-- the ONLY FS.GG.UI version literal (illustrative placeholder) -->
</PropertyGroup>
```

Every `FS.GG.UI.*` library pin references `$(FsGgUiVersion)`, and `build.fsx` reads this
same property at runtime to bind the `FS.GG.UI.Build` engine. There is no second place to
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

- `X.Y.Z-preview.N` (or any `-preview.N` / `-rc.N` suffix) ⇒ a **preview** release.
- `1.0.0` (a bare `MAJOR.MINOR.PATCH`, no suffix) ⇒ a **stable** release.

Pins stay **exact** — there are no floating ranges. Upgrading is always a deliberate single
edit, so a build is reproducible: the same `<FsGgUiVersion>` always restores the same
versions.

## Migrating a pre-rename project (the version property was renamed)

The single version property is now named `FsGgUiVersion`. Projects generated before template
`0.1.51-preview.1` used the **older, pre-rebrand property name** — a **clean break, with no
backward-compatibility alias**. If you generated your project from one of those earlier templates and
are adopting the bumped template version, do the one-time rename in `Directory.Packages.props`:

- rename your single version literal so the element is `<FsGgUiVersion>…</FsGgUiVersion>`, and
- update **every** `FS.GG.UI.*` pin so each reads `Version="$(FsGgUiVersion)"`.

`build.fsx` already reads `FsGgUiVersion`. A half-renamed file fails `dotnet restore` fast on an
undefined property, so there is no silent-drift failure mode — rename the literal and all pins
together, then restore.

## Where the packages come from

The generated `NuGet.config` references the **public nuget.org feed only** — no machine-local
path — so this project restores on any machine without a repository checkout. To consume a
**private** or **pre-release staging** feed instead, add it to `NuGet.config`.
