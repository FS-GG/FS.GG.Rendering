# FS.Skia.UI.Template

The `dotnet new` project template for **FS.Skia.UI** — an F# / Elmish UI and 2D scene-graph
framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install & scaffold

```bash
dotnet new install FS.Skia.UI.Template
dotnet new fs-skia-ui -o MyApp        # profiles: app, headless-scene, governed, sample-pack
cd MyApp
dotnet restore                        # resolves FS.Skia.UI.* from nuget.org only
dotnet build
dotnet test
```

The generated project restores entirely from the **public nuget.org feed** — no machine-local
path — so it works on any machine without a repository checkout.

## Usage

Install the template once, then scaffold a project with `dotnet new fs-skia-ui`. The
generated project name is derived from `--name` (`-o` sets the output directory):

```bash
dotnet new install FS.Skia.UI.Template
dotnet new fs-skia-ui --name MyApp                              # default profile: app
dotnet new fs-skia-ui --name MyScene --profile headless-scene  # scene/widget authoring
cd MyApp
dotnet restore
```

Pass `--profile <p>` to pick what to scaffold, and add `--feedback true` or
`--skipGitInit true` as needed (see **Options**). Every profile carries the Spec Kit
install and `speckit-*` skills, so you drive features through a governed agent loop.

## Options

| Option | Default | Effect |
|--------|---------|--------|
| `--profile <p>` | `app` | Which product to scaffold (see profile table below). |
| `--feedback true` | `false` | Capture per-phase Spec Kit feedback into `specs/<feature>/feedback/` — adds the `after_*` feedback hooks and the `fs-skia-feedback-capture` skill. Default `false` induces no diff. |
| `--skipGitInit true` | `false` | Don't create the initial Git commit (use when generating inside an existing repo). |

| Profile | Scaffolds |
|---------|-----------|
| `app` | Default product — Scene, SkiaViewer, Elmish, KeyboardInput, Layout, Controls, product tests + governance. |
| `headless-scene` | Headless Scene-only product for scene/widget authoring (no live window). |
| `governed` | Scene plus Testing helpers, governance-focused. |
| `sample-pack` | Scene, SkiaViewer, Elmish + sample-pack gallery content. |

## Single-source versioning

Every generated project pins all `FS.Skia.UI.*` packages **and** the in-process build engine to
one `<FsSkiaUiVersion>` value in `Directory.Packages.props`. Upgrading is a single edit + `dotnet
restore`; see the generated `docs/UPGRADING.md`. Preview vs stable is explicit in the value
(`-preview.N` ⇒ preview channel).

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT
