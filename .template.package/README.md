# FS.GG.UI.Template

The `dotnet new` project template for **FS.GG.UI** — an F# / Elmish UI and 2D scene-graph
framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install & scaffold

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp        # profiles: app, headless-scene, governed, sample-pack
cd MyApp
dotnet restore                        # resolves FS.GG.UI.* from nuget.org only
dotnet build
dotnet test
```

The generated project restores entirely from the **public nuget.org feed** — no machine-local
path — so it works on any machine without a repository checkout.

## Usage

Install the template once, then scaffold a project with `dotnet new fs-gg-ui`. The
generated project name is derived from `--name` (`-o` sets the output directory):

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui --name MyApp                              # default profile: app
dotnet new fs-gg-ui --name MyScene --profile headless-scene  # scene/widget authoring
cd MyApp
dotnet restore
```

Pass `--profile <p>` to pick what to scaffold, and add `--feedback true` or
`--initGit true` as needed (see **Options**). Every profile carries the Spec Kit
install and `speckit-*` skills, so you drive features through a governed agent loop.

Generation is **side-effect-free by default**: it emits files only — it starts no process,
creates no Git repository, and never hangs in CI or IDE "new project" hosts. To initialize a
repository and mark the generated shell scripts executable, either pass `--initGit true` (and
`--allow-scripts yes` for a non-interactive run) or run the steps by hand (see **Manual setup**).
Under the SDD scaffold path (`fsgg-sdd scaffold --provider rendering`) these steps are performed
for you, so `--initGit` is unnecessary there.

## Options

| Option | Default | Effect |
|--------|---------|--------|
| `--profile <p>` | `app` | Which product to scaffold (see profile table below). |
| `--lifecycle <l>` | `spec-kit` | Which lifecycle scaffolding to emit alongside the product: `spec-kit` (full Spec Kit workspace), `sdd` (product only, external orchestrator), or `none` (product only, nothing attached). See **Lifecycle** below. The default is byte-identical to the pre-lifecycle template. |
| `--feedback true` | `false` | Capture per-phase Spec Kit feedback into `specs/<feature>/feedback/` — adds the `after_*` feedback hooks and the `fs-gg-feedback-capture` skill. Default `false` induces no diff. |
| `--initGit true` | `false` | Opt in to initialize a Git repository with a `[Spec Kit] Initial commit` **and** mark generated shell scripts executable. Skipped when already inside a repository; non-fatal if `git` is absent. Pair with `--allow-scripts yes` for non-interactive runs. Default `false` is side-effect-free. Unnecessary under the SDD scaffold path. |

| Profile | Scaffolds |
|---------|-----------|
| `app` | Default product — Scene, SkiaViewer, Elmish, KeyboardInput, Layout, Controls, product tests + governance. |
| `headless-scene` | Headless Scene-only product for scene/widget authoring (no live window). |
| `governed` | Scene plus Testing helpers, governance-focused. |
| `sample-pack` | Scene, SkiaViewer, Elmish + sample-pack gallery content. |

## Lifecycle

`--lifecycle` selects whether the generated product ships with a governance/orchestration surface
attached. It is **orthogonal to `--profile`**: `--profile` picks the product *shape*; `--lifecycle`
picks whether the Spec Kit lifecycle *workspace* is emitted alongside it. Every combination is valid.

### Which value do I want?

```text
What do you want to scaffold?
├─ A governed product with the Spec Kit lifecycle (specify/plan/tasks, constitution, agent context)
│     → --lifecycle spec-kit   (default; omit the flag for the same result)
├─ An app-only product to be composed by the SDD scaffold (external orchestrator supplies governance)
│     → --lifecycle sdd
└─ A bare standalone product with nothing attached
      → --lifecycle none
```

### Per-value include / exclude

| value | includes | excludes |
|-------|----------|----------|
| `spec-kit` (default) | the full Spec Kit lifecycle surface: `.specify/`, the generated constitution, the `.agents/` + `.claude/` skill/context trees, and the generated `AGENTS.md`/`CLAUDE.md` agent-context tree | — (full surface) |
| `sdd` | the generated product (app-only) | the entire gated lifecycle surface — an **external orchestrator** (the SDD scaffold) re-supplies lifecycle/governance |
| `none` | the generated product only | the gated lifecycle surface **and** any orchestrator expectation |

### Standalone `none`

With `--lifecycle none`, **no governance and no orchestrator are attached, and none is expected** —
nothing will be added later. Pick `none` when you want only the generated product with no lifecycle
surface; do not expect a scaffold or governance layer to fill it in afterward.

### Migrating from the pre-lifecycle template

The lifecycle-aware template is a **drop-in upgrade** from the pre-lifecycle template:

- **Select the default (`spec-kit`, or omit `--lifecycle`) to reproduce prior output byte-for-byte** —
  the default emits exactly what the pre-lifecycle template did.
- Choose `sdd` or `none` only when you want to **opt out** of the emitted lifecycle surface (e.g. the
  SDD scaffold owns governance, or you want a bare standalone product).

## Manual setup (standalone use)

Because generation is side-effect-free, a standalone (non-scaffold) caller who wants a Git
repository and executable scripts can either pass `--initGit true --allow-scripts yes`, or perform
the steps by hand from the generated project root:

```bash
find . -type f \( -name "*.sh" -o -name "fake.sh" \) -exec chmod +x {} +
git init && git add . && git commit -m "[Spec Kit] Initial commit"   # skip if already in a repo
```

These same instructions ship in the generated product's `README.md`. Under the SDD scaffold path
they are performed for you.

## Single-source versioning

Every generated project pins all `FS.GG.UI.*` packages **and** the in-process build engine to
one `<FsGgUiVersion>` value in `Directory.Packages.props`. Upgrading is a single edit + `dotnet
restore`; see the generated `docs/UPGRADING.md`. Preview vs stable is explicit in the value
(`-preview.N` ⇒ preview channel).

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT
