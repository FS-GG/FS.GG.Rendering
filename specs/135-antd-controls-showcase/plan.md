# Implementation Plan: Ant Design Controls Showcase — Ant restyle + enterprise templates (G3)

**Branch**: `135-antd-controls-showcase` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/135-antd-controls-showcase/spec.md`

## Summary

Build a runnable **Ant Design Controls Showcase** — a navigable multi-page sample application that renders
**every control in the widened catalog (96 controls, incl. the net-new Ant primitives from feature 132)**
under the shipped **`FS.GG.UI.Themes.AntDesign`** theme in both **Ant light and Ant dark**, plus the six
canonical Ant **enterprise template pages** (workbench, list, detail, form, result, exception) composed
**only from catalog controls**. It is a pure **`FS.GG.UI.*`-package consumer** (no `src/` references),
reusing the deterministic seeded-evidence harness the Controls Gallery (G1, feature 123) and Sample Apps
(G2, feature 134) established: a pure MVU `Core` (`Model`/`Msg`/`update` + per-page seeded `FrameInput`
scripts + a coverage map + a deterministic evidence record), a thin `App` edge that turns the Core into a
live window (`interactive`, GL-gated) or a headless evidence run (`evidence --seed N`, the CI path) via the
public `runInteractiveApp` / `Perf.runScript` / `Viewer.captureScreenshotEvidence` surfaces, and an Expecto
test project (outside the default test tier). The visible payoff of the whole Workstream-F design-system
arc: the *same* semantic control set, restyled to the Ant visual language, with no control forks.

The new capability over G1 is twofold: (1) the **concrete Ant theme** is the renderer (`antLight`/`antDark`
from `FS.GG.UI.Themes.AntDesign`) instead of the built-in Light/Dark + accent seam; and (2) **enterprise
template pages** — compositions of catalog controls realizing the `docs/product/ant-design/templates/*.md`
recipes — which are exempt from the one-control-one-page coverage bijection and instead validated by a
"composed only of catalog control types" check plus, for the form page, real validation behavior.

A hard **precondition** distinguishes this feature from G1/G2: the local NuGet feed is **stale relative to
feature 132** (dated 2026-06-15, before 132's 2026-06-16 control additions) and **does not contain the
`FS.GG.UI.Themes.AntDesign` package at all**. The feed MUST be refreshed (repack `FS.GG.Rendering.slnx`)
so the showcase can consume the Ant theme and the 96-control surface as packages — see Research R1.

This is **Workstream G3 only** (Ant restyle + enterprise templates). G4 (wiring sample runs into the
perf/CI corpus) and dedicated Ant **Charts** dashboards (feature 133) are out of scope.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`LangVersion=latest`, `Nullable=enable`, `FS0078`-as-error) —
same consumer toolchain as G1/G2 (`Directory.Build.props` shadows the repo root).

**Primary Dependencies** (consumed as packed NuGet packages from `~/.local/share/nuget-local/`, **never** as
project references into `src/`):

- `FS.GG.UI.Themes.AntDesign` — **the new dependency** for this feature: `AntTheme.antLight`,
  `AntTheme.antDark`, `AntTheme.resolve` (the concrete Ant theme + Ant intent policy, feature 132).
- `FS.GG.UI.Controls` — control builders, `Catalog` (now **96** controls incl. net-new Ant primitives),
  `Control.renderTree`, the `Typed` widget surface.
- `FS.GG.UI.Controls.Elmish` — `InteractiveAppHost<'M,'Msg>`, `runInteractiveApp`, `Perf.runScript`,
  `FrameInput<'Msg>` (`Tick`/`Key`/`Pointer`/`Idle`), `FrameMetrics`.
- `FS.GG.UI.SkiaViewer` — `Viewer.captureScreenshotEvidence`, `ViewerOptions`, `ScreenshotEvidenceRequest`/
  `Result`, `Viewer.defaultDiagnostics`.
- `FS.GG.UI.DesignSystem` — `Theme` (the flat record `antLight`/`antDark` are instances of), token/resolver
  surface promoted in Workstream F.
- `FS.GG.UI.Color`, `FS.GG.UI.Scene`, `FS.GG.UI.KeyboardInput` — `Color`, scene/`Control`, `ViewerKey`.
- `FS.GG.UI.Testing` — `ScreenshotEvidenceResult` disclosure fields.
- Test-only: `Expecto`, `Microsoft.NET.Test.Sdk`, `YoloDev.Expecto.TestSdk`.

`FS.GG.UI.Themes.Default` is **not** referenced — this showcase renders only under the Ant theme (contrast
with G1, which is Default-only). All other packages are already on the feed at `0.1.0-preview.1`; the Ant
theme package and the widened `Controls` package become available after the R1 feed refresh.

**Storage**: Filesystem only — per-page evidence under `artifacts/ant-showcase/<seed>/<page-id>/
{run.json,summary.md,frame.png,state.txt}` (gitignored). The committed coverage report lives under the
sample tree. No database.

**Testing**: Expecto via `dotnet test`, **outside the default test tier** (not added to
`FS.GG.Rendering.slnx`). Suites: coverage (FR-003/SC-001), page-render (FR-001/FR-004), enterprise-template
composition + form validation (FR-005/FR-006/SC-002/SC-009), theme-invariance antLight↔antDark
(FR-008/SC-003), determinism (FR-011/SC-004), degrade-and-disclose (FR-013/SC-005), interaction
(FR-014), and public-surface-only consumption (FR-015/SC-006). Precedent:
`samples/ControlsGallery/ControlsGallery.Tests/*`.

**Target Platform**: Linux (X11/GL) for interactive mode; headless evidence + coverage run anywhere
including no-display/no-GL CI hosts (degrade-and-disclose path), mirroring G1/G2.

**Project Type**: Sample **desktop application** (consumer of the framework) — one shared pure Core library
+ one thin executable + one co-located Expecto test project, in a standalone `samples/AntShowcase/` tree
decoupled from the main solution.

**Performance Goals**: Not a hot-path feature. The quantitative gates are **byte-identical evidence across
two same-seed headless runs (SC-004)** and **coverage bijection over all 96 catalog controls (SC-001)**.

**Constraints**:

- Public package surface only — **no project references into `src/`, no internal access** (FR-015/SC-006).
- **No public product surface / token / rendered-output change** (FR-016/SC-007) — both drift gates
  untouched; the showcase is a pure consumer. (Feature 132 already added the controls/theme; this feature
  only consumes them.)
- Ant theme only — `antLight`/`antDark`; **no Default/Fluent/Material**, no new controls/themes/kits.
- Deterministic headless mode — **no wall-clock, no `System.Random`/`Math.random`**; seeded `FrameInput`
  scripts only (FR-011). (No game loop / PRNG — simpler than G2.)
- Headless evidence is the CI-facing path; **the required gate never depends on a display or GL** (FR-018).
- One semantic control set — controls **MUST NOT branch on theme identity**; antLight↔antDark differ only
  by resolved visuals (FR-008/SC-003).
- Imported identifiers rebranded `FS.Skia.UI.*` → `FS.GG.UI.*`, recorded in provenance (FR-017).

**Scale/Scope**: 96 catalog controls across **≈13 family pages** + **6 enterprise template pages**, 1
shared harness, 1 committed coverage report, **0 new packages, 0 public-surface/token-baseline changes**.
The one real novelty over G1 is the enterprise-template page kind (compositions exempt from the coverage
bijection) and consuming the concrete Ant theme. **Medium.**

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

**Change Classification**: **Tier 2 (additive consumer)** — identical posture to G1/G2. The feature adds a
new sample tree + its tests; it makes **no change to the product public API surface**, adds **no new
external runtime dependency** to the shipped packages, and touches **no `.fsi` or surface-area baseline and
no design-token**. The new package dependency (`FS.GG.UI.Themes.AntDesign`) is *consumed*, not authored
here — it shipped in feature 132. Tier 2 requires spec + tests, which this plan provides. (The R1 feed
refresh re-packs already-built `src/` packages to the documented feed location; it changes no source and no
surface.)

| Principle | Gate | Status |
|-----------|------|--------|
| **I. Spec → FSI → Semantic Tests → Implementation** | Public surface sketched + validated by use before `.fs` bodies | **PASS (adapted, as G1/G2).** The samples are *consumers*; their "FSI surface" is the framework's already-published `.fsi` (incl. the Ant theme's `AntTheme.fsi` from feature 132). The plan exercises that surface through the headless CLI and Expecto tests as a downstream app would. No new public product surface is sketched. |
| **II. Visibility lives in `.fsi`, not `.fs`** | Every **public** module has a curated `.fsi`; no access modifiers in `.fs` | **PASS (not in scope).** The sample exposes **no public package surface** (`IsPackable=false`, FR-015) — its modules are application-internal, so Principle II's `.fsi` requirement does not apply. No `private`/`internal`/`public` modifiers on top-level bindings (keeps `FS0078`-as-error clean; `private` *helpers inside modules* remain allowed, as in G1's `Pages.fs`). |
| **III. Idiomatic simplicity** | Plainest F#; exotic features justified | **PASS.** Records, unions, lists, pattern matching, pure functions, MVU. The `PageKind = Catalog \| Template` tag and the template compositions are plain F# — no SRTP, reflection, type providers, or custom operators. |
| **IV. Elmish/MVU is the boundary for stateful/I-O work** | Stateful/interactive features modeled as `Model`/`Msg`/pure `update` + edge interpreter | **PASS.** The whole showcase is one MVU app; `update` is pure (page nav, theme-mode toggle, per-control demo-state edits incl. form field/validation transitions); I/O (window, screenshot, file write) happens only at the App edge; headless mode drives the same `update` via pure `Perf.runScript`. |
| **V. Test evidence is mandatory** | Tests fail before / pass after; real evidence preferred; synthetic disclosed | **PASS.** Coverage, page-render, template-composition, form-validation, theme-invariance, determinism, and degrade suites are behavior gates. Real GL screenshot evidence is captured where available; a no-GL host records a **disclosed degrade** (`ProvesScreenshot=false` + reason), never a fabricated pass. No mocks needed — assertions run against the real public packages. |
| **VI. Observability and safe failure** | Structured diagnostics; fail-fast or degrade explicitly; GL smoke distinguishes defect vs missing window-system | **PASS.** Every evidence record carries a non-empty `NotAuthoritativeFor` (FR-012); GL/display unavailability is reported via `ScreenshotEvidenceResult.{BlockedStage,UnsupportedHostReason}` and yields a clean, non-hanging exit (FR-013); a coverage drift or a missing Ant-theme package fails loudly with a disclosed reason. |

**Engineering Constraints check**: `net10.0` ✓ · SkiaSharp-over-GL inherited via packages, no Vulkan ✓ ·
consumes packed packages from `~/.local/share/nuget-local/` ✓ · **one semantic control set, no per-theme
forks** — explicitly demonstrated by rendering the same tree under antLight/antDark ✓ · package identity
untouched (consumer only) ✓ · sample kept outside the default test tier ✓.

**Result: PASS — no violations. Complexity Tracking is empty.**

## Project Structure

### Documentation (this feature)

```text
specs/135-antd-controls-showcase/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — resolved decisions (R1–R8)
├── data-model.md        # Phase 1 — entities: Page (+PageKind), AntShowcaseModel/Msg, DemoState, scripts, records, coverage
├── quickstart.md        # Phase 1 — feed-refresh precondition + build/run/verify guide (V0–V8)
├── contracts/           # Phase 1
│   ├── cli.md                    #   list | interactive <id> | evidence --seed N | coverage subcommands + exit codes
│   ├── page-registry.md          #   the 96→family-page bijection + the 6 enterprise template pages, PageKind rule
│   ├── enterprise-templates.md   #   the 6 template recipes → catalog-control compositions + the form-validation contract
│   └── evidence-record.md        #   per-page evidence-record schema + disclosure rule (reused from G1, Ant-themed)
├── checklists/
│   └── requirements.md  # (created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

The sample is a standalone consumer tree, **not** added to `FS.GG.Rendering.slnx`, built/tested on its own
via a local `nuget.config` pointing at `~/.local/share/nuget-local/` — exactly as G1/G2.

```text
samples/
└── AntShowcase/
    ├── nuget.config                      # local feed → ~/.local/share/nuget-local/ (copied from G1)
    ├── Directory.Build.props             # shadows repo root; net10.0, FS0078-as-error, IsPackable=false (from G1)
    ├── Directory.Packages.props          # disables central package management for the sample (from G1)
    ├── PROVENANCE.md                     # FS.Skia.UI.* → FS.GG.UI.* rebrand + template-recipe source disclosure (FR-017)
    ├── README.md                         # what's built, the Ant-restyle rationale, the feed-refresh precondition, how to run
    ├── coverage-report.md                # committed coverage: 96 controls → family pages, + the 6 template pages (FR-003)
    ├── AntShowcase.Core/                 # pure, testable core + harness (no GL, no I/O)
    │   ├── AntShowcase.Core.fsproj       #   PackageReference FS.GG.UI.{Themes.AntDesign,Controls,Color,Scene,Controls.Elmish,SkiaViewer,DesignSystem,KeyboardInput}
    │   ├── AntTheme.fs                    #   thin wrapper over FS.GG.UI.Themes.AntDesign: mode→Theme (antLight/antDark) — R3
    │   ├── Model.fs                       #   AntShowcaseModel/Msg, PageKind, CoverageResult — R2/R4
    │   ├── DemoState.fs                   #   seeded representative content for all control families incl. form state — R5
    │   ├── Pages.fs                       #   family pages (all 96 controls) — R2
    │   ├── Templates.fs                   #   the 6 enterprise template pages (compositions) — R6
    │   ├── PageRegistry.fs               #   Pages.all = family pages ++ template pages, tagged by PageKind — R2
    │   ├── CoverageMap.fs                 #   bijection over Catalog pages only; template pages exempt — R4
    │   ├── Scripts.fs                     #   per-page seeded FrameInput scripts (deterministic) — R7
    │   ├── Shell.fs                       #   app-bar (Ant light/dark toggle) + nav rail + content + status, Ant-themed
    │   ├── Host.fs                        #   InteractiveAppHost bridge (Theme = AntTheme.resolve mode) — R3
    │   └── Evidence.fs                    #   package-only evidence record + run.json/state.txt/summary.md (from G1)
    ├── AntShowcase.App/                   # thin executable (the edge / interpreter)
    │   ├── AntShowcase.App.fsproj         #   ProjectReference Core; PackageReference Controls.Elmish/SkiaViewer/Testing
    │   ├── Interactive.fs                 #   runInteractiveApp wiring (GL-gated)
    │   ├── Evidence.fs                    #   headless: Perf.runScript + captureScreenshotEvidence + record writer
    │   └── Program.fs                     #   CLI dispatch: list | interactive <id> | evidence --seed N | coverage
    └── AntShowcase.Tests/                 # Expecto (outside the default test tier)
        ├── AntShowcase.Tests.fsproj       #   ProjectReference Core; PackageReference Controls(.Elmish)/SkiaViewer
        ├── CoverageTests.fs               #   FR-003 / SC-001: 96-control bijection over Catalog pages
        ├── PageRenderTests.fs             #   FR-001 / FR-004: every page renders populated, no errors
        ├── TemplateTests.fs               #   FR-005 / FR-006 / SC-002 / SC-009: template composed of catalog controls; form validation
        ├── ThemeInvarianceTests.fs        #   FR-008 / SC-003: antLight↔antDark identical tree/a11y, no theme-id branch
        ├── DeterminismTests.fs            #   FR-011 / SC-004: byte-identical same-seed evidence
        ├── DegradeTests.fs                #   FR-013 / SC-005: clean disclosed skip on no-GL
        ├── InteractionTests.fs            #   FR-014: seeded input → visible state change
        └── Main.fs

artifacts/ant-showcase/                    # evidence output (gitignored), per seed/page
└── <seed>/<page-id>/{run.json,summary.md,frame.png,state.txt}
```

**Structure Decision**: A **single `samples/AntShowcase/` tree with the proven G1 3-project split** (one
shared pure `Core` carrying the page registry + harness, one thin `App` edge, one Expecto `Tests` project),
delivered as a **new sample distinct from `samples/ControlsGallery/`** rather than by bolting an Ant mode
onto G1. Rationale (Research R8): keeping the two separate leaves G1's 52→10 Light/Dark coverage assertion
stable, lets the Ant showcase target the full 96-control surface + template pages without perturbing G1's
golden evidence, and keeps each sample independently demonstrable. The shared `Core` keeps all
deterministic logic GL-free and unit-testable (Principle IV). The sample is kept **out of the main `.slnx`**
and wired to the local NuGet feed so building/running it *is* the public-consumer-path proof (SC-006).

## Phased delivery (internal sequencing within this one feature)

- **P-A — Feed refresh + shared harness + first family page (US1 MVP slice)**: R1 feed refresh
  (`dotnet pack FS.GG.Rendering.slnx -c Release` + cache invalidation) so `FS.GG.UI.Themes.AntDesign` and
  the 96-control `FS.GG.UI.Controls` are consumable; `AntTheme`/`Model`/`DemoState`/`Host`/`Evidence` +
  the **Display & Typography** family page rendered under antLight; page-render + a first coverage row
  green. *Smallest shippable Ant-styled slice.*
- **P-B — Full catalog coverage (US1)**: all ≈13 family pages covering **all 96 controls**; `CoverageMap`
  bijection green (SC-001); `coverage-report.md` committed; `PageRenderTests` span every page.
- **P-C — Enterprise template pages (US2)**: `Templates.fs` realizing workbench/list/detail/form/result/
  exception from catalog controls; `TemplateTests` assert composed-only-of-catalog-controls (SC-002) and
  the form page's invalid-rejected / valid-succeeds behavior (SC-009).
- **P-D — Ant light/dark parity (US3)**: app-bar mode toggle; `ThemeInvarianceTests` assert identical
  tree/a11y across antLight↔antDark with only resolved visuals differing and no theme-identity branching.
- **P-E — Determinism + disclosure + interaction hardening (US4/US5)**: seeded `Scripts.fs` per page;
  determinism (byte-identical same-seed), degrade-and-disclose (no-GL), and interaction suites green;
  PROVENANCE + README finalized. Optional decision record for the new-sample-vs-extend-G1 choice.

## Complexity Tracking

> No Constitution Check violations. The one novelty over G1 is justified below, not a violation.

| Item | Why needed | Simpler alternative rejected because |
|---|---|---|
| **`PageKind = Catalog \| Template` (template pages exempt from the coverage bijection)** | Enterprise template pages reuse controls that also appear on family pages; forcing them into the "exactly one page" bijection would make coverage impossible to satisfy | Putting template controls into the bijection breaks SC-001; dropping template pages loses the headline G3 deliverable (SC-002). Tagging page kind and running the bijection over Catalog pages only — while validating template pages by a separate "composed of catalog control types" check — is the plain-F# resolution. |

No new package, no project added to `.slnx`, no public-surface/token change introduced. The R1 feed refresh
re-packs existing `src/` output; it authors no source.
