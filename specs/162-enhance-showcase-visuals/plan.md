# Implementation Plan: AntShowcase Visual Overhaul

**Branch**: `162-enhance-showcase-visuals` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/162-enhance-showcase-visuals/spec.md`

## Summary

Make `samples/AntShowcase` visually inspectable as an Ant-styled product sample, not only a
coverage proof. The implementation will replace the current nested-stack shell with explicit,
bounded top bar, navigation, content, feedback, and status regions; give dense and large controls
page-specific demonstration regions; enhance the six enterprise templates into credible workflow
pages; and add a visual-readiness evidence pass that captures every page in Ant light and Ant dark
at the declared preferred size `1600x1000`, checks screenshot completeness, produces contact
sheets, and requires reviewer-recorded defect classification before readiness can be accepted.

Ant remains a design language over the one semantic control set. The showcase continues to consume
packed `FS.GG.UI.*` packages only, keeps the live catalog bijection, and does not introduce
React/DOM/HTML/CSS or per-theme control forks.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; package-visible
surface and new reusable AntShowcase contract modules remain curated by `.fsi` files.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`,
`FS.GG.UI.Scene`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Themes.AntDesign`, and `FS.GG.UI.Testing`
packages; SkiaSharp `4.147.0-preview.3.1`; Silk.NET OpenGL/Windowing `2.23.0`; Expecto `10.2.2`.
No new runtime dependency is planned. SkiaSharp image composition may be used for contact sheets
because it is already a repository dependency.

**Storage**: Transient sample output remains under `samples/AntShowcase/artifacts/` or a caller
provided `--out` directory. Accepted feature evidence is stored under
`specs/162-enhance-showcase-visuals/readiness/`, including visual screenshots, contact sheets,
automated completeness results, reviewer defect classification, coverage output, package-feed
validation, and final readiness summary.

**Testing**: Expecto through `dotnet test`; AntShowcase tests for shell region separation, bounded
navigation labels, content containment, page registry coverage, theme preservation, page rendering
at `1600x1000` and representative `1280x800`, evidence artifact generation, degraded capture
disclosure, defect-classification readiness blocking, pure visual-readiness workflow transitions,
and app-edge effect interpreter behavior. Existing coverage, determinism, interaction, feedback,
template, date-picker overlay, and theme-invariance tests remain regression coverage. Broad
solution validation remains the release gate.

**Target Platform**: Multi-package F# UI/rendering library plus package-only AntShowcase desktop
sample. Interactive mode targets a capable SkiaSharp/OpenGL viewer host; visual evidence uses
offscreen screenshot capture where available and fails closed with explicit unavailable-capture
disclosure when screenshots cannot be produced.

**Project Type**: F# rendering/UI library with generated sample application and validation
evidence.

**Performance Goals**: Visual readiness, not frame-time improvement. The preferred inspection
size is `1600x1000`; the minimum supported inspection size is `1280x800`. The preferred readiness
run captures 19 pages x 2 themes = 38 screenshots with zero missing/degraded screenshots and zero
reviewer-recorded critical defects. Representative minimum-size screenshots prove dense pages stay
readable through scrolling or responsive layout.

**Constraints**: Do not reduce catalog coverage or hide controls to pass layout. Do not fork
controls per Ant theme. Do not claim visual readiness from deterministic or non-blank evidence
alone. Screenshot capture unavailability must be disclosed and must not produce accepted visual
readiness. Sample composition is the first fix path; lower-level library changes are allowed only
when a defect cannot be correctly fixed in the sample and must be documented as a bounded
limitation or public-surface change. Canonical theme ids are `antLight` and `antDark`; `light,dark`
are CLI aliases that must resolve to canonical ids in summaries and tests.

**Scale/Scope**: `samples/AntShowcase` core/app/tests, feature readiness artifacts, and existing
Ant docs references. `tests/Rendering.Harness` changes are only in scope if the implementation
chooses to share screenshot/contact-sheet helpers. Public package `.fsi` and surface baselines are
only in scope if a lower-level library fix is required. New reusable AntShowcase modules created
for this feature get companion `.fsi` signatures; touched legacy sample composition modules must
not broaden their public surface without adding `.fsi` or recording a bounded compatibility
follow-up before readiness.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 observable sample behavior. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Observable CLI/evidence/layout contracts are specified before implementation. New reusable visual-support modules are introduced with `.fsi` signatures before `.fs` bodies. If lower-level package surface changes become necessary, `.fsi`, semantic tests, surface baselines, and compatibility notes are required before `.fs` implementation. |
| Visibility lives in `.fsi` | PASS | New reusable AntShowcase modules created by this feature use companion `.fsi` signatures. Existing executable/test sample modules may be modified for composition, but any broadened reusable or package-visible surface must add `.fsi` or be recorded as a bounded compatibility follow-up before readiness. |
| Idiomatic simplicity | PASS | The plan extends existing sample MVU, page registry, screenshot evidence, and package-only validation paths rather than adding a new framework. |
| Elmish/MVU boundary | PASS | Stateful shell/page/theme/feedback interactions remain in the pure AntShowcase model/update boundary. The visual-readiness command adds a pure workflow boundary with `Model`, `Msg`, `Effect`, `init`, and `update`; screenshot, filesystem, GL, and contact-sheet interpretation stays at the App/evidence edge. |
| Test evidence is mandatory | PASS | Focused tests must fail before the visual fixes and pass after, including shell separation, content containment, theme surfaces, evidence completeness, and readiness blocking. |
| Observability and safe failure | PASS | Visual evidence records page id, theme, size, screenshot status, completeness status, capture availability, defect classification, and limitations; unavailable capture fails closed. |
| Tier 1 obligations | PASS | Package-only sample validation, compatibility notes, package-feed validation, readiness evidence, and regression validation are required. Public-surface obligations activate if lower-level package changes are made. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/162-enhance-showcase-visuals/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- visual-readiness-cli.md
|   |-- visual-evidence-artifacts.md
|   |-- shell-and-page-layout.md
|   |-- defect-classification.md
|   `-- page-registry-and-coverage.md
`-- readiness/
    |-- visual-evidence/
    |   |-- summary.md
    |   |-- summary.json
    |   |-- contact-sheet-light.png
    |   |-- contact-sheet-dark.png
    |   |-- light/
    |   |-- dark/
    |   |-- completeness/
    |   `-- reviewer-defects.md
    |-- minimum-size/
    |-- package-feed.md
    |-- compatibility-ledger.md
    |-- regression-validation.md
    |-- full-validation/
    `-- validation-summary.md
```

### Source Code (repository root)

```text
samples/AntShowcase/
|-- AntShowcase.Core/
|   |-- Model.fs
|   |-- Host.fs
|   |-- PageRegistry.fs
|   |-- VisualConfig.fsi
|   |-- VisualConfig.fs
|   |-- ShellLayout.fsi
|   |-- ShellLayout.fs
|   |-- PageProfiles.fsi
|   |-- PageProfiles.fs
|   |-- VisualReadinessWorkflow.fsi
|   |-- VisualReadinessWorkflow.fs
|   |-- Shell.fs
|   |-- Pages.fs
|   |-- Templates.fs
|   |-- Evidence.fsi
|   |-- Evidence.fs
|   `-- AntTheme.fs
|-- AntShowcase.App/
|   |-- Program.fs
|   |-- Interactive.fs
|   |-- VisualReadiness.fsi
|   |-- VisualReadiness.fs
|   |-- Evidence.fs
|   `-- FeedbackStore.fs
`-- AntShowcase.Tests/
    |-- VisualTestHelpers.fs
    |-- CoverageTests.fs
    |-- PageRenderTests.fs
    |-- ThemeInvarianceTests.fs
    |-- TemplateTests.fs
    |-- InteractionTests.fs
    |-- FeedbackTests.fs
    |-- DegradeTests.fs
    |-- VisualShellTests.fs
    |-- VisualPageTests.fs
    |-- VisualTemplateTests.fs
    |-- VisualEvidenceTests.fs
    `-- VisualReadinessTests.fs

src/
|-- Controls/
|-- Controls.Elmish/
|-- Layout/
|-- SkiaViewer/
`-- Testing/

tests/
|-- Rendering.Harness/
|-- Rendering.Harness.Tests/
|-- Package.Tests/
`-- Testing.Tests/
```

**Structure Decision**: `samples/AntShowcase` owns the shell composition, page visual profiles,
template composition, visual-readiness CLI, and product evidence because this is a sample behavior
feature. New reusable visual-support modules expose their intended surface through `.fsi`; the
visual-readiness command keeps pure state decisions in `VisualReadinessWorkflow` and App-edge
effects in `VisualReadiness`. `src/*` and `tests/Rendering.Harness` stay unchanged unless visual
defects prove to be lower-level layout, control, viewer, or screenshot limitations. If such changes
are required, the implementation must update the relevant `.fsi`, semantic tests, package
compatibility notes, and surface baselines.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- `1600x1000` is the preferred inspection and evidence size; `1280x800` is the minimum supported
  inspection size.
- Visual readiness is a separate evidence claim from the existing deterministic/non-blank
  evidence.
- Shell collisions are addressed with explicit bounded regions rather than nested-stack spacing.
- Dense and large controls use page visual profiles and dedicated demonstration regions.
- Ant remains a theme/style layer over the one semantic control set.
- Automated evidence checks screenshot completeness, while defect classification is reviewer
  recorded and readiness-blocking.
- Screenshot capture unavailability reports environment-limited evidence and never accepts visual
  readiness.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and workflow transitions.

Observable contracts:

- [Visual Readiness CLI](contracts/visual-readiness-cli.md)
- [Visual Evidence Artifacts](contracts/visual-evidence-artifacts.md)
- [Shell and Page Layout](contracts/shell-and-page-layout.md)
- [Defect Classification](contracts/defect-classification.md)
- [Page Registry and Coverage](contracts/page-registry-and-coverage.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 sample-behavior classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts define CLI options, evidence artifacts, layout invariants, defect classes, and coverage expectations before implementation. New reusable visual-support modules are tasked with `.fsi` before `.fs`; public package changes remain conditional and require `.fsi` plus semantic tests. |
| Visibility lives in `.fsi` | PASS | Design keeps the planned work in sample code while adding `.fsi` for new reusable sample modules. Any package-visible lower-level fixes must follow the repository `.fsi` rule. |
| Idiomatic simplicity | PASS | Design uses bounded layout descriptors, page profiles, and existing evidence plumbing rather than a new renderer or visual diff platform. |
| Elmish/MVU boundary | PASS | Shell state, page state, theme switching, feedback, and readiness workflow decisions remain pure through explicit model/update contracts; GL, screenshots, filesystem writes, and image contact sheets are edge effects. |
| Test evidence | PASS | Quickstart and contracts require focused tests, package-only sample validation, preferred-size evidence, representative minimum-size evidence, degraded-capture tests, and full regression validation. |
| Observability and safe failure | PASS | Required artifacts expose page/theme/size counts, screenshot paths, completeness status, reviewer defect status, unavailable capture, and lower-level limitations. |
| Tier 1 obligations | PASS | Compatibility, package-feed, regression, readiness, and full-validation artifacts are required; public-surface artifacts are required only if lower-level package changes are made. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.
