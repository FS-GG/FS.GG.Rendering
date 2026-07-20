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
| `--lifecycle <l>` | `sdd` | Which lifecycle scaffolding to emit alongside the product: `sdd` (default — product only, to be composed by an external SDD orchestrator, with a fail-closed guard until the lifecycle is re-supplied), `spec-kit` (full Spec Kit workspace — **legacy**, frozen and scheduled for removal per ADR-0056), or `none` (product only, nothing attached, no guard). See **Lifecycle** below. |
| `--feedback true` | `false` | Emit the per-phase feedback **capture** machinery: under `--lifecycle spec-kit`, capture into `specs/<feature>/feedback/` via the `after_*` hooks and the `fs-gg-feedback-capture` skill. Default `false` induces no diff. It does **not** gate the retrospective `fs-gg-feedback-report` skill, which ships in every scaffold on every profile and lifecycle: the report is agent-invoked at cycle end and reads only whatever guarded evidence exists, so it degrades cleanly when nothing was captured. |
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
├─ An app-only product to be composed by the SDD scaffold (external orchestrator supplies governance)
│     → --lifecycle sdd   (default; omit the flag for the same result)
├─ A governed product with the legacy Spec Kit lifecycle (specify/plan/tasks, constitution, agent context)
│     → --lifecycle spec-kit   (frozen, scheduled for removal — see ADR-0056)
└─ A bare standalone product with nothing attached
      → --lifecycle none
```

### Per-value include / exclude

| value | includes | excludes |
|-------|----------|----------|
| `sdd` (default) | the generated product (app-only), plus a fail-closed guard sentinel (`readiness/lifecycle-scaffolding-pending.md`) | the entire gated lifecycle surface — an **external orchestrator** (the SDD scaffold) re-supplies lifecycle/governance, which clears the guard |
| `spec-kit` | the full Spec Kit lifecycle surface: `.specify/`, the generated constitution, the `.agents/` + `.claude/` skill/context trees, and the generated `AGENTS.md`/`CLAUDE.md` agent-context tree (**legacy** — frozen, scheduled for removal per ADR-0056) | — (full surface) |
| `none` | the generated product only, no guard | the gated lifecycle surface **and** any orchestrator expectation |

### The default (`sdd`) guard

The `sdd` lane emits the product only and expects an external SDD orchestrator to re-supply the
lifecycle. So the default ships **with** a guard: a post-scaffold notice, a build **warning**, and a
fail-closed `Verify` readiness/doctor gate — all keyed on the one file that distinguishes the otherwise
byte-identical `sdd`/`none` trees (`readiness/lifecycle-scaffolding-pending.md`, emitted only under
`sdd`). Run `fsgg-sdd` to re-supply the lifecycle (which clears the guard), or pass `--lifecycle none`
for a deliberately lifecycle-less product with no guard. `none` is silent; only `sdd` warns.

### Standalone `none`

With `--lifecycle none`, **no governance and no orchestrator are attached, and none is expected** —
nothing will be added later. Pick `none` when you want only the generated product with no lifecycle
surface; do not expect a scaffold or governance layer to fill it in afterward.

### Migrating from the pre-lifecycle template

ADR-0056 makes `sdd` the default and reclassifies `spec-kit` as legacy (frozen, scheduled for removal):

- **To reproduce the pre-lifecycle output byte-for-byte, pass `--lifecycle spec-kit` explicitly** — the
  legacy lane still emits exactly what the pre-lifecycle template did. Omitting `--lifecycle` now yields
  the `sdd` product-only tree (with the guard), **not** the full Spec Kit workspace.
- The default (`sdd`) expects an external SDD orchestrator to re-supply the lifecycle; until it does, the
  build warns and the `Verify` readiness/doctor gate fails closed. Run `fsgg-sdd`, or pass
  `--lifecycle none` for a deliberately lifecycle-less product with no guard.

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
