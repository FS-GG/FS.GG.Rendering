# Second Ant Showcase

This is the second Ant showcase sample, not a replacement for `samples/AntShowcase`.
It keeps the original sample intact and adds a separate package-consuming review surface
under `samples/SecondAntShowcase`.

The sample renders the current `FS.GG.UI.Controls.Catalog.supportedControls` set across
13 catalog pages, adds six enterprise Ant-style template pages, and exercises behavior
through a pure Core `Model`/`Msg`/`update` boundary. Ant is used as a design language only:
the sample composes the one semantic control set and resolves visuals through the shipped
`FS.GG.UI.Themes.AntDesign.AntTheme.antLight` and `AntTheme.antDark` themes.

Local Ant guidance comes from `docs/product/ant-design/reference/ant-llms-sources.md`,
`docs/product/ant-design/README.md`, and the family pages under
`docs/product/ant-design/patterns/`. Do not use React, DOM, HTML/CSS, Ant-specific control
forks, or new product controls for this sample.

## Local Feed

The sample consumes packed `FS.GG.UI.*` packages from `~/.local/share/nuget-local`; it does
not reference `src/` projects directly. Refresh the feed before validating the sample:

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx
dotnet nuget locals global-packages --clear
```

The sample package pins currently match the product projects at `0.1.33-preview.1`.

## Commands

Run from `samples/SecondAntShowcase`:

```sh
dotnet build SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release
dotnet test SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release
dotnet run --project SecondAntShowcase.App -c Release -- coverage
dotnet run --project SecondAntShowcase.App -c Release -- list
dotnet run --project SecondAntShowcase.App -c Release -- evidence --seed 1 --out ../../specs/171-second-antshowcase-sample/readiness
dotnet run --project SecondAntShowcase.App -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out ../../specs/171-second-antshowcase-sample/readiness/preferred
dotnet run --project SecondAntShowcase.App -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out ../../specs/171-second-antshowcase-sample/readiness/minimum
dotnet run --project SecondAntShowcase.App -c Release -- review-findings --out ../../specs/171-second-antshowcase-sample/readiness --fail-on-unresolved
dotnet run --project SecondAntShowcase.App -c Release -- interactive display-typography --theme light
```

`coverage` fails on missing, duplicated, or unknown catalog ids. `visual-readiness` produces
real screenshots when the host supports it and records `environment-limited` or degraded
evidence when it does not. Environment-limited output is useful automation evidence, but it
is not accepted live visual fidelity.

## Layout

```text
samples/SecondAntShowcase/
|-- nuget.config
|-- Directory.Build.props
|-- Directory.Packages.props
|-- SecondAntShowcase.Core/
|-- SecondAntShowcase.App/
`-- SecondAntShowcase.Tests/
```

Core owns pure state, page registry, coverage, interaction contracts, review findings,
visual-readiness decisions, and evidence shaping. App owns CLI dispatch, interactive hosting,
filesystem writes, screenshot attempts, and diagnostics. Tests exercise the public Core `.fsi`
surface and CLI-facing behavior.

## 10-minute maintainer checklist

- Identify that this is the second Ant showcase and that `samples/AntShowcase` remains intact.
- Confirm local Ant guidance is cited through the repo hub and pattern docs.
- Confirm package consumption uses the local `FS.GG.UI.*` feed rather than `src/` references.
- Run `coverage`, `test`, `evidence`, `visual-readiness`, and `review-findings`.
- Treat missing live captures, degraded captures, pending reviewer classification, and
  unresolved visual findings as review-required or blocked, not accepted visual proof.
