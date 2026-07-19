# FS.GG.Rendering

An F# desktop UI framework that renders Model-View-Update (MVU) applications with
[SkiaSharp](https://github.com/mono/SkiaSharp) over **OpenGL**. You describe a scene
of primitives or a tree of semantic controls; the framework measures, lays it out,
and paints it — with an interactive render loop, theming, input routing, and a
deterministic offscreen path for tests.

The render core is **Elmish-free**; idiomatic [Elmish](https://elmish.github.io/elmish/)
(`Cmd`, subscriptions) is an **optional** adapter layer.

> **Platform vs. workspace.** FS-GG is a **platform** — five repositories (the UI
> framework is one **component** of it). What you scaffold *with* the platform is a
> **workspace**: a generated repo with a runnable app, the `.fsgg/` lifecycle, skills,
> and optional governance. See the
> [vocabulary](https://github.com/FS-GG/.github/blob/main/docs/adr/0020-platform-workspace-component-vocabulary.md).

## Quick taste

```fsharp
open FS.GG.UI.SkiaViewer

let options : ViewerOptions =
    { Title = "Hello"; InitialSize = size
      PresentMode = ViewerPresentMode.DirectToSwapchain; FrameRateCap = None
      LogicalSize = None }   // Some { Width = 1280; Height = 720 } to letterbox a fixed canvas

// Render a static scene…
Viewer.run options scene |> ignore

// …or drive an interactive MVU window with your own model/msg:
Viewer.runInteractiveViewer options host |> ignore
```

For semantic controls (Button, TextBox, DataGrid…) with Elmish, use
`Controls.Elmish.runInteractiveApp`. **Full walkthrough → [`docs/usage.md`](docs/usage.md).**

## Consume it

Published as `FS.GG.UI.*` packages on `net10.0` — 16 libraries plus the `FS.GG.UI` BOM
metapackage (current framework version `0.14.0`). Each release **dual-publishes**
the byte-identical set to public [nuget.org](https://www.nuget.org/packages?q=FS.GG.UI)
(GitHub OIDC Trusted Publishing) and the org GitHub Packages feed. You can also reference
the `src/*/*.fsproj` directly, `dotnet pack` to a local feed, or scaffold a ready-wired app
from the template (`dotnet new install . && dotnet new fs-gg-ui`). See
[`docs/usage.md`](docs/usage.md#getting-the-packages) for every path and the full package map.

## Build & test

```sh
dotnet build FS.GG.Rendering.slnx -c Release         # all runtime libs + local tests
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release   # default local tier (GL via X11)
```

The offscreen/deterministic tiers run headless; live windowed rendering needs a GL/X11
session. The tiered evidence CLI under `tools/Rendering.Harness/` declares what each run
proves and what it does not.

## Status

Active preview. This repository is the canonical home of the FS-GG rendering component,
split out of the archived [`EHotwagner/FS-Skia-UI`](https://github.com/EHotwagner/FS-Skia-UI)
(see [`PROVENANCE.md`](PROVENANCE.md)). Three themes ship today: **Light** and **Dark**
(`FS.GG.UI.Themes.Default`) plus an opt-in **Ant Design** theme (`FS.GG.UI.Themes.AntDesign`,
`AntTheme.antLight`/`antDark`; [ADR-0006](docs/product/decisions/0006-antdesign-theme-and-new-controls.md)).
Further design languages (Fluent, Material), design kits, and the remaining harness tiers are
on the roadmap in [`docs/reports/`](docs/reports/).

## Learn more

- [`docs/usage.md`](docs/usage.md) — how to consume and render, in detail.
- [`docs/product/layering.md`](docs/product/layering.md) — the four-layer UI model.
- [`docs/product/module-map.md`](docs/product/module-map.md) — what each module owns.
- [`SKIPPED-TESTS.md`](SKIPPED-TESTS.md) — documented out-of-scope test skips.

## License

[MIT](LICENSE) © 2026 EHotwagner
