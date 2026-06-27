# Phase 1 Data Model: Refresh fs-gg-ui Template to Current Scene API

This is a maintenance/conformance feature; there is no runtime data schema. The "entities" below are
the artifacts the work operates over and the records used to track drift and verification. They give
`/speckit-tasks` stable nouns to attach tasks to.

## Entity: Template Profile

A scaffolding configuration selected by `dotnet new fs-gg-ui --profile <p>`.

| Field | Values | Notes |
|-------|--------|-------|
| Name | `app` (default), `headless-scene`, `governed`, `sample-pack` | from `.template.config/template.json` |
| Package set | per-profile `FS.GG.UI.*` references | guarded in `Directory.Packages.props` + `Product.fsproj` |
| Seed branch | `headless` (governed/headless-scene) \| `interactive` (app/sample-pack) | selected by `//#if` directives |
| Host | none (headless CLI) \| `Viewer.runApp` (sample-pack) \| `ControlsElmish.runInteractiveApp` (app) | asserted by GovernanceTests |

**Validation rule**: each profile must independently generate, restore, build, and emit its evidence
(SC-001, SC-006). A profile passing does not imply the others pass (independent drift).

## Entity: Seed Source File

A file under `template/base/src/Product/` carrying one or both profile branches.

| File | Branches | Primary surfaces consumed |
|------|----------|---------------------------|
| `Model.fs` | both | Scene; Controls/Controls.Elmish/DesignSystem/Themes.Default/KeyboardInput (interactive) |
| `View.fs` | both | Scene `Group/Text/Rectangle/Size/SceneNode`; typed `Controls.Typed.*`, `Widget.toControl`, `Control.renderTree` (interactive) |
| `LayoutEvidence.fs` | both | `LayoutEvidenceReport`, `LayoutRegionEvidence`, `Scene`, overlap diagnostics |
| `EvidenceCommands.fs` | both | `SceneEvidence.render`; `Viewer.*` evidence/host APIs (interactive) |
| `Program.fs` | both | entry point; host selection per profile |
| `WindowOptions.fs` | interactive only | window-behavior parsing → Viewer launch request |

**Validation rule**: every Scene/Controls/Viewer construct referenced must exist in the current public
surface with the assumed shape (FR-002, SC-002). Edits stay within the correct `//#if` branch and must
not break the sibling branch (edge case: profile-guarded source).

## Entity: Version Pin (`FsSkiaUiVersion`)

The single FS.GG.UI version literal in `template/base/Directory.Packages.props`.

| Field | Value | Notes |
|-------|-------|-------|
| Current pin | `0.1.0-preview.1` (at plan time) | line 9 of `Directory.Packages.props` |
| Produced version `V` | repo `Directory.Build.props` `<Version>` → feed | the re-pin target |
| Referencing pins | all `FS.GG.UI.*` use `$(FsSkiaUiVersion)` | must stay the only literal (FR-004) |

**State transition**: `current pin → V` (FR-003). Invariant after transition: exactly one FS.GG.UI
version literal exists; zero occurrences of the superseded literal remain in template pins, seed code,
or current-pin docs (SC-003). If `current pin == V`, transition is a verified no-op (recorded).

## Entity: Bundled API-Surface Reference

`template/base/docs/api-surface/Scene/Scene.fsi` — a `copyOnly` snapshot shipped to consumers as the
Scene reference (FR-005). Conformance target: the current public Scene surface (`src/Scene/*.fsi`).

**Validation rule**: presents no construct as current that is absent from the live surface (SC-004);
framework identifiers preserved verbatim (no `sourceName` substitution).

## Tracking record: Drift Item

A unit of work produced by the Phase-0 detection run. Not a code artifact — a task-tracking shape.

| Field | Example |
|-------|---------|
| Profile | `app` |
| File / location | `View.fs` interactive branch, `RichText.view` call |
| Symptom | compiler error / evidence failure text |
| Surface | Controls.Typed.RichText |
| Current equivalent | renamed field / new shape (or "none — no drift") |
| Resolution | the edit, or "no change needed" |

## Tracking record: Profile Verification Result

Per-profile evidence that the refresh holds.

| Field | Example |
|-------|---------|
| Profile | `headless-scene` |
| Restore | ok (single consistent `V`, no NU16xx) |
| Build | 0 errors / 0 API-drift warnings |
| Evidence | `--scene-evidence` ok, `--layout-evidence` accepted |
| Governance | `build.fsx target Verify` + `Product.Tests` green |
