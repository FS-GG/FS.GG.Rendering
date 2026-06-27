# Product

This generated product references selected FS.GG.UI capabilities instead of
copying the framework repository.

The selected capabilities are controlled by `--profile`:

- `app`: Scene, SkiaViewer, Elmish, KeyboardInput, Layout, Controls, Controls.Elmish adapter
- `headless-scene`: Scene
- `governed`: Scene, Testing
- `sample-pack`: Scene, SkiaViewer, Elmish, Samples

## First-time setup (Git + executable scripts)

This product was generated **side-effect-free**: no Git repository was created and the generated
shell scripts may not carry the executable bit. Before the FAKE-backed commands below, either
re-generate with `--initGit true --allow-scripts yes`, or run these steps once by hand from the
project root (skip the `git` step if you are already inside a repository):

```bash
find . -type f \( -name "*.sh" -o -name "fake.sh" \) -exec chmod +x {} +
git init && git add . && git commit -m "[Spec Kit] Initial commit"
```

On Windows the executable-bit step is unnecessary (there is no executable mode); just run the
`git` step. If this product was produced by the SDD scaffold path, these steps were already done
for you.

## Quickstart

Run the generated product governance checks:

Generated FAKE-backed commands (`./fake.sh`, `fake.cmd`, or `dotnet fake`)
share `.fake` state and are not safe to run concurrently. Run multiple
FAKE-backed validation commands sequentially, and record that order in
readiness evidence. Non-FAKE checks may still run in parallel when they do not
invoke FAKE or depend on `.fake`.

1. `./fake.sh build -t Dev`
2. `./fake.sh build -t Test`
3. `./fake.sh build -t Verify`

> **`Dev` is a completion-marker / log-writer target, not a compiler.** It only
> records progress to `readiness/logs/Dev.txt` — it does **not** give you real
> compiler or test feedback. The authoritative compile/test path is
> `./fake.sh build -t Test` / `./fake.sh build -t Verify` (which run
> `dotnet test`); use those (or `dotnet build` / `dotnet test` directly) when you
> need actual compiler errors and test results. Do not infer "it compiles" from a
> green `Dev`.

> **`Verify` embeds the merge-gate audit; `-t Test` is the mid-implementation
> green-test path.** `./fake.sh build -t Verify` runs `EvidenceGraph` then
> `EvidenceAudit` **before** the tests, and that audit **hard-blocks until every
> task is `[X]`** — so `Verify` cannot produce a green test run while the feature is
> still in progress. Use `./fake.sh build -t Test` (the first real compile,
> audit-free) for a green test run mid-implementation, and `-t Verify` for the full
> merge-gate once the feature is complete.

Run Spec Kit evidence checks through the generated FAKE targets:

1. `./fake.sh build -t EvidenceGraph`
2. `./fake.sh build -t EvidenceAudit`

If a generated FAKE-backed command fails with race-like symptoms or unknown
concurrent context, rerun the affected FAKE-backed commands sequentially before
classifying the failure as a product regression.

The generated targets run the evidence graph and merge-gate audit in-process
through the packaged `FS.GG.UI.Build` engine (no Python or shell audit script
is copied into or executed by the generated project).
The workflow does not depend on executable file mode being preserved when the
checkout is copied. Redirected `Verify` output is written as plain text under
`readiness/logs/`; pass and fail diagnostics should remain readable and must not
contain embedded NUL byte blocks.

## Explore the app in FSI

To load the built app and all its transitive `FS.GG.UI.*` references into FSI
in a single step — with **zero manual reference edits** — build once, then run
the generated load script:

1. `./fake.sh build -t Dev`
2. `dotnet fsi load-product.fsx`

`load-product.fsx` is **generated** and stays in sync with the product's
assembly set: it is derived from `Directory.Packages.props` and the built
`Product` output, not a hand-maintained reference list, so do not edit it. It
only `#r`s the assemblies and `open`s `Product` — it launches nothing, so it
neither emits nor suppresses host warnings, and a missing assembly surfaces as a
normal load failure.

This product is generated without a bundled lifecycle workspace
(`--lifecycle sdd` / `--lifecycle none`). Governed feature work is driven by the
lifecycle owner you compose this product under, which supplies the workspace and
the governance skills. To generate the product with the Spec Kit lifecycle
workspace bundled in instead, scaffold with the default `--lifecycle spec-kit`.

The product references FS.GG.UI packages from **public nuget.org** — the generated
`NuGet.config` references that feed only, with no machine-local path, so `dotnet restore`
works on any machine. All `FS.GG.UI.*` packages and the build engine are pinned by a single
`<FsGgUiVersion>` in `Directory.Packages.props`; upgrading is one edit + `dotnet restore`
(see `docs/UPGRADING.md`). _Framework developers_ working from a clone of the FS.GG.UI
repository instead pack the source with `./fake.sh build -t PackLocal` and add
`~/.local/share/nuget-local` as a NuGet source before restoring.
Use the generated source-shaped package API reference. Do not use assembly reflection or
repository source inspection as an authoring substitute. When Scene and Controls are used in
the same file, qualify collision-prone names such as
`FS.GG.UI.Scene.Rect`, `FS.GG.UI.Scene.Paint`,
`FS.GG.UI.Scene.TextRun`, `FS.GG.UI.Controls.TextBlock.create`,
`FS.GG.UI.Controls.TextBox.onChanged`, and
`FS.GG.UI.Controls.Stack.children`. Do not rely on namespace open order.

The product owns its application code, tests, documentation, readiness evidence,
and selected local skills.

Visual demo task lists assign scene rendering -> fs-gg-scene, screenshot
capture -> fs-gg-skiaviewer, layout readability -> fs-gg-layout-readability,
persistent viewer launch -> fs-gg-skiaviewer, deterministic evidence mode ->
fs-gg-evidence-mode, generated-package validation ->
fs-gg-template-update, graph validation -> speckit-evidence-graph, and audit
validation -> speckit-evidence-audit. Ordered multi-skill examples preserve
implementation-before-evidence, graph-before-audit, debug-before-broad-rerun,
and visible mirrors such as `[skillist: speckit-tasks, fs-gg-layout-readability]`.

Generated readiness scaffolds include `readiness/visual-evidence-honesty.md`,
`readiness/window-visibility.md`, `readiness/governance-risk-levels.md`,
`readiness/aggregate-hang-diagnostics.md`, `readiness/runtime-limitations.md`,
`readiness/generated-guidance-validation.md`, and
`readiness/real-image-evidence.md`. Each scaffold records the authoritative
command, artifact path, failure class, and next action.

For generated app profiles, `FS.GG.UI.Controls` is the authoring path for
ordinary controls, rich text, chart controls, graph controls, and DataGrid.
When Elmish integration is selected, `FS.GG.UI.Controls.Elmish` provides the
adapter for commands, subscriptions, and program wiring. Users moving from the
legacy Charts package should use Controls chart and DataGrid declarations
directly; there is no compatibility shim.

## Authoring controls — discover the API, never reflect

Everything you need to author controls is discoverable from this generated project.
**Do not reflect over the assembly or read framework source** — each discovery path below
resolves to a concrete, populated reference you can open:

- **Typed Props front door (recommended, compiler-guided).** `src/Product/View.fs` is the
  worked starter: every control is `{ Module.defaults with Field = ... } |> Module.view`
  through `FS.GG.UI.Controls.Typed`. To add a control kind not shown, type
  `SomeControl.defaults` and let IntelliSense enumerate its `Props` fields — no
  attribute-name guessing.
- **Source-shaped API reference on disk.** `docs/api-surface/Controls/*.fsi` carries the
  typed `Props`/`view` and legacy builder signatures, each with substantive `///`
  documentation (the same text IntelliSense shows).
- **Per-control catalog facts.** `docs/controls-catalog.md` answers "which attributes/events
  does control *X* support?" for the demonstrated controls and explains how to enumerate the
  rest.
- **Programmatic discovery API.** `FS.GG.UI.Controls.Catalog` —
  `knownControlKinds`, `requiredAttributes`, `supportedAttributes`, `supportedEvents`, and
  `markdownSummary` — reports any control's complete contract as data, from IntelliSense or at
  runtime. A control authored through the legacy builder rather than the typed front door is
  still fully supported; the catalog reports its contract either way.
- **Interactive host seam.** `FS.GG.UI.Controls.Elmish.ControlsElmish.runInteractiveApp`
  (wired in `src/Product/Program.fs`) runs an interactive controls app; `programOfWidget` /
  `widgetView` wire a typed `Widget<'msg>` view directly.

## Archive And API Reference Guidance

For generated product governance, current feature readiness paths are authoritative for current gates. historical feature readiness is audit context only unless a current evidence map explicitly marks it as supporting evidence.
Archived material must not be cited as current package, template, generated-product, or audit pass/fail evidence.

The source-shaped `.fsi` package API reference remains authoritative for agent
authoring. FSharp.Formatting/fsdocs output is secondary or hybrid unless the
active generator decision record marks it authoritative. Package consumers must not use assembly reflection or repository source inspection as an authoring substitute.
