# FS.GG.Rendering

An F# desktop UI framework that renders Model-View-Update (MVU) applications with
[SkiaSharp](https://github.com/mono/SkiaSharp) over **OpenGL**. You describe a scene
of primitives or a tree of semantic controls; the framework measures, lays it out,
and paints it — with an interactive render loop, theming, input routing, and a
deterministic offscreen path for tests.

The render core is **Elmish-free**; idiomatic [Elmish](https://elmish.github.io/elmish/)
(`Cmd`, subscriptions) is an **optional** adapter layer.

## Quick taste

```fsharp
open FS.GG.UI.SkiaViewer

let options : ViewerOptions =
    { Title = "Hello"; InitialSize = size
      PresentMode = ViewerPresentMode.DirectToSwapchain; FrameRateCap = None }

// Render a static scene…
Viewer.run options scene |> ignore

// …or drive an interactive MVU window with your own model/msg:
Viewer.runInteractiveViewer options host |> ignore
```

For semantic controls (Button, TextBox, DataGrid…) with Elmish, use
`Controls.Elmish.runInteractiveApp`. **Full walkthrough → [`docs/usage.md`](docs/usage.md).**

## Consume it

Published as `FS.GG.UI.*` packages on `net10.0` (`0.1.0-preview.1`). Not on a public
feed yet — reference the projects directly, `dotnet pack` to a local feed, or scaffold
from the template (`dotnet new install . && dotnet new fs-gg-ui`). See
[`docs/usage.md`](docs/usage.md#getting-the-packages) for all three paths and the
package map.

## Build & test

```sh
dotnet build FS.GG.Rendering.slnx -c Release         # all runtime libs + local tests
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release   # default local tier (GL via X11)
```

The offscreen/deterministic tiers run headless; live windowed rendering needs a GL/X11
session. The tiered evidence CLI under `tools/Rendering.Harness/` declares what each run
proves and what it does not.

## Status

Active preview. This repository is the canonical home of the FS-GG rendering product,
split out of the archived [`EHotwagner/FS-Skia-UI`](https://github.com/EHotwagner/FS-Skia-UI)
(see [`PROVENANCE.md`](PROVENANCE.md)). Only Light/Dark themes ship today; named themes,
design kits, and the remaining harness tiers are on the roadmap in
[`docs/reports/`](docs/reports/).

## Learn more

- [`docs/usage.md`](docs/usage.md) — how to consume and render, in detail.
- [`docs/product/layering.md`](docs/product/layering.md) — the four-layer UI model.
- [`docs/product/module-map.md`](docs/product/module-map.md) — what each module owns.
- [`SKIPPED-TESTS.md`](SKIPPED-TESTS.md) — documented out-of-scope test skips.

## License

[MIT](LICENSE) © 2026 EHotwagner
